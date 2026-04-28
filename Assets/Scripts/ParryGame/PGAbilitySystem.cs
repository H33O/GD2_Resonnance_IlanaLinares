using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Manages the three purchasable abilities in the Parry Game:
///   - Heal       : restores 1 HP, long cooldown (very rare)
///   - Weapon     : double-strike hitting enemies at two Z depths, then greys out
///   - Shield     : absorbs the next enemy hit, short cooldown
///
/// Exposes static events so PGHUD can react and update button states.
/// </summary>
public class PGAbilitySystem : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static PGAbilitySystem Instance { get; private set; }

    // ── Events (HUD subscribes) ───────────────────────────────────────────────

    /// <summary>Fired each frame while an ability is on cooldown. (ability, 0-1 progress)</summary>
    public static event Action<AbilityType, float> OnCooldownProgress;

    /// <summary>Fired when an ability becomes ready again.</summary>
    public static event Action<AbilityType>        OnAbilityReady;

    /// <summary>Fired when an ability is activated.</summary>
    public static event Action<AbilityType>        OnAbilityUsed;

    // ── Settings ──────────────────────────────────────────────────────────────

    public PGSettings    settings;
    public PGEnemySpawner enemySpawner;

    [Header("Audio")]
    [Tooltip("Son joué quand le joueur utilise Défense ou Soin (amelioration_sound.mp3).")]
    public AudioClip ameliorationSound;

    [Tooltip("Son joué quand le joueur parry via l'arme (parry sound.mp3).")]
    public AudioClip parrySound;

    // ── Ability kinds ─────────────────────────────────────────────────────────

    public enum AbilityType { Heal, Weapon, Shield }

    // ── Internal state ────────────────────────────────────────────────────────

    private bool      _healReady   = true;
    private bool      _weaponReady = true;
    private bool      _shieldReady = true;

    /// <summary>Whether the shield is currently active (blocking the next hit).</summary>
    public bool ShieldActive { get; private set; }

    // Reference to the 3D shield visual in world space
    private PGShieldEffect _shieldEffect;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── Public API (called by PGHUD buttons) ──────────────────────────────────

    /// <summary>Attempts to activate the Heal ability.</summary>
    public void UseHeal()
    {
        if (!_healReady) return;
        var gm = PGGameManager.Instance;
        if (gm == null || gm.State != PGGameManager.GameState.Playing) return;

        int amount = settings != null ? settings.healAmount : 1;
        gm.RestoreHp(amount);

        AudioManager.Instance?.PlaySfx(ameliorationSound);

        _healReady = false;
        OnAbilityUsed?.Invoke(AbilityType.Heal);
        StartCoroutine(CooldownRoutine(AbilityType.Heal,
            settings != null ? settings.healCooldown : 45f));
    }

    /// <summary>Attempts to activate the Weapon (double-strike) ability.</summary>
    public void UseWeapon()
    {
        if (!_weaponReady) return;
        var gm = PGGameManager.Instance;
        if (gm == null || gm.State != PGGameManager.GameState.Playing) return;

        _weaponReady = false;
        OnAbilityUsed?.Invoke(AbilityType.Weapon);
        AudioManager.Instance?.PlaySfx(parrySound);
        StartCoroutine(DoubleStrikeRoutine());
        StartCoroutine(CooldownRoutine(AbilityType.Weapon,
            settings != null ? settings.weaponCooldown : 20f));
    }

    /// <summary>Attempts to activate the Shield (defense) ability.</summary>
    public void UseShield()
    {
        if (!_shieldReady || ShieldActive) return;
        var gm = PGGameManager.Instance;
        if (gm == null || gm.State != PGGameManager.GameState.Playing) return;

        _shieldReady = false;
        ShieldActive = true;
        OnAbilityUsed?.Invoke(AbilityType.Shield);
        AudioManager.Instance?.PlaySfx(ameliorationSound);
        StartCoroutine(ShieldRoutine());
    }

    /// <summary>
    /// Called by PGEnemy when it reaches the player, before dealing damage.
    /// Returns true if the shield absorbed the hit.
    /// </summary>
    public bool TryAbsorbWithShield()
    {
        if (!ShieldActive) return false;
        ShieldActive = false;
        _shieldEffect?.TriggerAbsorb();
        PGGameManager.Instance?.NotifyShieldBlock();
        StartCoroutine(CooldownRoutine(AbilityType.Shield,
            settings != null ? settings.shieldCooldown : 8f));
        return true;
    }

    // ── Internal routines ─────────────────────────────────────────────────────

    private IEnumerator CooldownRoutine(AbilityType type, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            OnCooldownProgress?.Invoke(type, elapsed / duration);
            yield return null;
        }

        switch (type)
        {
            case AbilityType.Heal:   _healReady   = true; break;
            case AbilityType.Weapon: _weaponReady = true; break;
            case AbilityType.Shield: _shieldReady = true; break;
        }
        OnAbilityReady?.Invoke(type);
    }

    private IEnumerator DoubleStrikeRoutine()
    {
        float triggerZ = settings != null ? settings.parryTriggerZ   : 1.2f;
        float extraZ   = settings != null ? settings.weaponExtraZ     : 2.5f;
        if (enemySpawner == null) yield break;

        // ── First strike — normal parry window ────────────────────────────────
        HitEnemiesAtZ(triggerZ);

        // Brief visual gap between the two strikes
        yield return new WaitForSeconds(0.18f);

        // ── Second strike — extended Z (hits enemies that are farther back) ───
        HitEnemiesAtZ(triggerZ + extraZ);
    }

    private void HitEnemiesAtZ(float maxZ)
    {
        var enemies = enemySpawner.ActiveEnemies;
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            var e = enemies[i];
            if (e == null) continue;
            if (!e.IsParryable(maxZ)) continue;
            e.TriggerParry();
        }
    }

    private IEnumerator ShieldRoutine()
    {
        float duration = settings != null ? settings.shieldDuration : 3.5f;

        // Spawn the 3D shield visual
        _shieldEffect = PGShieldEffect.Spawn();

        float elapsed = 0f;
        while (elapsed < duration && ShieldActive)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Shield expired without absorbing anything
        if (ShieldActive)
        {
            ShieldActive = false;
            _shieldEffect?.TriggerExpire();
            StartCoroutine(CooldownRoutine(AbilityType.Shield,
                settings != null ? settings.shieldCooldown : 8f));
        }
    }

    // ── Public queries ────────────────────────────────────────────────────────

    /// <summary>Whether the given ability is currently ready to use.</summary>
    public bool IsReady(AbilityType type) => type switch
    {
        AbilityType.Heal   => _healReady,
        AbilityType.Weapon => _weaponReady,
        AbilityType.Shield => _shieldReady && !ShieldActive,
        _                  => false,
    };
}
