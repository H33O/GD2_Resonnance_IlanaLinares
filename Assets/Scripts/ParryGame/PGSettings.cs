using UnityEngine;

/// <summary>
/// Designer-facing parameters for the Parry Game.
/// Create via Assets ▸ Create ▸ ParryGame ▸ Settings.
/// </summary>
[CreateAssetMenu(fileName = "PGSettings", menuName = "ParryGame/Settings")]
public class PGSettings : ScriptableObject
{
    [Header("Enemy Spawning")]
    [Tooltip("Delay between enemy spawns at game start (seconds).")]
    public float spawnDelayStart = 3.0f;

    [Tooltip("Minimum spawn delay at peak difficulty.")]
    public float spawnDelayMin = 0.8f;

    [Tooltip("Seconds until minimum spawn delay is reached.")]
    public float spawnRampDuration = 90f;

    [Tooltip("Starting enemy movement speed (world units/s).")]
    public float enemySpeedStart = 1.5f;

    [Tooltip("Maximum enemy speed reached at peak difficulty.")]
    public float enemySpeedMax = 4.5f;

    [Tooltip("Z position where enemies spawn (deep, far from camera).")]
    public float enemySpawnZ = 18f;

    [Tooltip("Z position where the player stands (near camera).")]
    public float playerZ = 0f;

    [Header("Player & Parry")]
    [Tooltip("Parry window duration after tap (seconds).")]
    public float parryWindowDuration = 0.22f;

    [Tooltip("Distance (Z) within which a parry is triggered.")]
    public float parryTriggerZ = 1.2f;

    [Tooltip("Maximum player HP.")]
    public int maxHp = 3;

    [Header("Scoring")]
    [Tooltip("Base score per successful parry.")]
    public int scorePerParry = 10;

    [Tooltip("Combo multiplier bonus per consecutive parry.")]
    public float comboMultiplierStep = 0.25f;

    [Header("Camera")]
    [Tooltip("Camera vertical offset from player position.")]
    public float cameraOffsetY = 1.8f;

    [Tooltip("Camera horizontal offset (right behind player, slightly right).")]
    public float cameraOffsetX = 0.6f;

    [Tooltip("Camera Z offset behind the player.")]
    public float cameraOffsetZ = -7f;

    [Tooltip("Camera field of view (degrees).")]
    public float cameraFov = 60f;

    [Header("Game Over")]
    [Tooltip("Time scale during game-over slow-motion.")]
    [Range(0.05f, 0.5f)]
    public float slowMoScale = 0.15f;

    [Tooltip("Real-time duration of the slow-motion window.")]
    public float slowMoDuration = 0.4f;

    [Header("UI Colors")]
    public Color colorDefense  = new Color(0.30f, 0.55f, 1.00f, 1f);
    public Color colorWeapons  = new Color(1.00f, 0.38f, 0.28f, 1f);
    public Color colorHeals    = new Color(0.25f, 0.85f, 0.45f, 1f);

    [Header("Ability — Heal")]
    [Tooltip("Cooldown in seconds between heals (very rare).")]
    public float healCooldown     = 45f;

    [Tooltip("HP restored by one heal (usually 1).")]
    public int   healAmount       = 1;

    [Header("Ability — Double Strike (Weapon)")]
    [Tooltip("Cooldown in seconds between double-strike uses.")]
    public float weaponCooldown   = 20f;

    [Tooltip("Extra parry Z window added for the second strike.")]
    public float weaponExtraZ     = 2.5f;

    [Header("Ability — Shield (Defense)")]
    [Tooltip("Cooldown in seconds between shield uses (fairly short).")]
    public float shieldCooldown   = 8f;

    [Tooltip("How long the shield visual stays active before it absorbs or expires.")]
    public float shieldDuration   = 3.5f;
}
