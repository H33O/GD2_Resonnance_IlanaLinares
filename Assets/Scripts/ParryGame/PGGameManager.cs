using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central state machine for the Parry Game.
/// Manages score, HP, combo, and game-over flow.
/// </summary>
public class PGGameManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static PGGameManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Settings")]
    public PGSettings settings;

    [Header("Audio")]
    [Tooltip("Musique du Parry Game (parrygame music.mp3).")]
    public AudioClip parryMusic;

    [Tooltip("Son joué quand le joueur meurt (death_sound.mp3).")]
    public AudioClip deathSound;

    [Tooltip("Son joué quand le joueur perd un point de vie (perte de vie.mp3).")]
    public AudioClip loseLifeSound;

    // ── Events ────────────────────────────────────────────────────────────────

    public static event Action<int>   OnScoreChanged;
    public static event Action<int>   OnHpChanged;       // current HP
    public static event Action<int>   OnComboChanged;
    public static event Action        OnGameStarted;
    public static event Action        OnGameOver;
    public static event Action        OnParrySuccess;
    public static event Action        OnParryFail;

    /// <summary>Fired when a heal is used. Carries the new HP value.</summary>
    public static event Action<int>   OnHpRestored;

    /// <summary>Fired when the shield absorbs an enemy hit.</summary>
    public static event Action        OnShieldBlocked;

    // ── State ─────────────────────────────────────────────────────────────────

    public enum GameState { Playing, Dead, GameOver }

    public GameState State    { get; private set; } = GameState.Playing;
    public int       Score    { get; private set; }
    public int       Hp       { get; private set; }
    public int       Combo    { get; private set; }

    private float survivalTime;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Time.timeScale = 1f;

        Hp = settings != null ? settings.maxHp : 3;
    }

    private void Start()
    {
        if (AudioManager.Instance != null && parryMusic != null)
        {
            AudioManager.Instance.parryMusic = parryMusic;
            AudioManager.Instance.PlayMusic(parryMusic);
        }

        ButtonClickAudio.HookAllButtons();

        OnGameStarted?.Invoke();
        OnHpChanged?.Invoke(Hp);
    }

    private void Update()
    {
        if (State != GameState.Playing) return;
        survivalTime += Time.deltaTime;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Called by PGEnemyController when the player successfully parries an enemy.</summary>
    public void NotifyParry()
    {
        if (State != GameState.Playing) return;

        Combo++;
        OnComboChanged?.Invoke(Combo);

        float multiplier = 1f + (Combo - 1) * (settings != null ? settings.comboMultiplierStep : 0.25f);
        int gained = Mathf.RoundToInt((settings != null ? settings.scorePerParry : 10) * multiplier);

        Score += gained;
        OnScoreChanged?.Invoke(Score);

        OnParrySuccess?.Invoke();
    }

    /// <summary>Called when an enemy reaches the player without being parried.</summary>
    public void NotifyHit()
    {
        if (State != GameState.Playing) return;

        Combo = 0;
        OnComboChanged?.Invoke(Combo);

        Hp--;
        OnHpChanged?.Invoke(Hp);
        OnParryFail?.Invoke();

        if (Hp <= 0)
        {
            AudioManager.Instance?.PlaySfx(deathSound);
            State = GameState.Dead;
            StartCoroutine(GameOverSequence());
        }
        else
        {
            AudioManager.Instance?.PlaySfx(loseLifeSound);
        }
    }

    /// <summary>
    /// Restores <paramref name="amount"/> HP, capped at maxHp.
    /// Called by PGAbilitySystem on heal use.
    /// </summary>
    public void RestoreHp(int amount)
    {
        if (State != GameState.Playing) return;
        int maxHp = settings != null ? settings.maxHp : 3;
        Hp = Mathf.Min(Hp + amount, maxHp);
        OnHpChanged?.Invoke(Hp);
        OnHpRestored?.Invoke(Hp);
    }

    /// <summary>
    /// Called by PGAbilitySystem when the shield absorbs an incoming enemy.
    /// Triggers shield-block feedback without reducing HP.
    /// </summary>
    public void NotifyShieldBlock()
    {
        if (State != GameState.Playing) return;
        OnShieldBlocked?.Invoke();
    }

    /// <summary>Restarts the scene.</summary>
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>Returns to the main menu.</summary>
    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        string scene = MenuMainSetup.SceneName;
        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadScene(scene, scene);
        else
            SceneManager.LoadScene(scene);
    }

    // ── Current difficulty ────────────────────────────────────────────────────

    /// <summary>Normalized difficulty in [0,1] based on survival time.</summary>
    public float DifficultyNormalized =>
        settings != null
            ? Mathf.Clamp01(survivalTime / settings.spawnRampDuration)
            : 0f;

    /// <summary>Current enemy speed interpolated by difficulty.</summary>
    public float CurrentEnemySpeed =>
        settings != null
            ? Mathf.Lerp(settings.enemySpeedStart, settings.enemySpeedMax, DifficultyNormalized)
            : 2f;

    /// <summary>Current spawn delay interpolated by difficulty.</summary>
    public float CurrentSpawnDelay =>
        settings != null
            ? Mathf.Lerp(settings.spawnDelayStart, settings.spawnDelayMin, DifficultyNormalized)
            : 2f;

    // ── Game-over sequence ────────────────────────────────────────────────────

    private IEnumerator GameOverSequence()
    {
        Time.timeScale = settings != null ? settings.slowMoScale : 0.15f;
        yield return new WaitForSecondsRealtime(settings != null ? settings.slowMoDuration : 0.4f);
        Time.timeScale = 1f;

        State = GameState.GameOver;
        GameEndData.Set(Score, GameType.ParryGame);
        OnGameOver?.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
