using UnityEngine;

/// <summary>
/// All designer-facing parameters for the Slash Game.
/// Create via  Assets ▸ Create ▸ SlashGame ▸ Settings.
/// </summary>
[CreateAssetMenu(fileName = "SGSettings", menuName = "SlashGame/Settings")]
public class SGSettings : ScriptableObject
{
    // ── Slash attacks ─────────────────────────────────────────────────────────

    [Header("Slash Attacks")]
    [Tooltip("Distance from center where slashes spawn (world units).")]
    public float spawnRadius = 5.0f;

    [Tooltip("Starting speed of incoming slashes (world units/s).")]
    public float slashSpeedStart = 3.0f;

    [Tooltip("Maximum slash speed reached at peak difficulty.")]
    public float slashSpeedMax = 7.5f;

    [Tooltip("Seconds of survival until max speed is reached.")]
    public float speedRampDuration = 60f;

    [Tooltip("Initial delay between slashes (seconds).")]
    public float spawnDelayStart = 2.0f;

    [Tooltip("Minimum spawn delay at peak difficulty.")]
    public float spawnDelayMin = 0.5f;

    [Tooltip("Slash visual length (world units).")]
    public float slashLength = 1.0f;

    [Tooltip("Slash visual width.")]
    public float slashWidth = 0.08f;

    // ── Parry system ──────────────────────────────────────────────────────────

    [Header("Parry")]
    [Tooltip("Radius of the parry hitbox (world units).")]
    public float parryRadius = 1.2f;

    [Tooltip("Duration of the parry window after tap (seconds).")]
    public float parryWindowDuration = 0.18f;

    [Tooltip("Distance from center at which a parry can be triggered.")]
    public float parryTriggerDistance = 0.9f;

    // ── Energy cone ───────────────────────────────────────────────────────────

    [Header("Energy Cone")]
    [Tooltip("Energy gained per successful parry.")]
    public float energyPerParry = 0.20f;

    [Tooltip("Energy needed to fill the cone (1 = full).")]
    public float energyCapacity = 1.0f;

    [Tooltip("Points awarded when the cone fills.")]
    public int   pointsPerFill = 10;

    [Tooltip("Smooth fill animation speed.")]
    public float fillSmoothSpeed = 4.0f;

    // ── Hit-stop (freeze frame) ───────────────────────────────────────────────

    [Header("Hit-Stop")]
    [Tooltip("Time scale applied during parry hit-stop.")]
    [Range(0f, 0.3f)]
    public float hitStopTimeScale = 0.05f;

    [Tooltip("Real-time duration of parry hit-stop (seconds).")]
    public float hitStopDuration = 0.07f;

    // ── Game over slow-mo ─────────────────────────────────────────────────────

    [Header("Game Over")]
    [Tooltip("Time scale during game-over slow-motion.")]
    [Range(0.05f, 0.5f)]
    public float slowMoScale = 0.12f;

    [Tooltip("Real-time duration of the slow-motion window.")]
    public float slowMoDuration = 0.30f;

    // ── Camera shake ──────────────────────────────────────────────────────────

    [Header("Camera Shake")]
    [Tooltip("Shake magnitude on parry (world units).")]
    public float parryShakeMagnitude = 0.08f;

    [Tooltip("Shake magnitude on hit (world units).")]
    public float hitShakeMagnitude   = 0.25f;

    [Tooltip("Shake duration (seconds).")]
    public float shakeDuration = 0.20f;

    // ── Flash overlay ─────────────────────────────────────────────────────────

    [Header("Flash")]
    [Tooltip("Peak alpha of the flash on successful parry.")]
    [Range(0f, 1f)]
    public float parryFlashAlpha = 0.50f;

    [Tooltip("Peak alpha of the flash on hit.")]
    [Range(0f, 1f)]
    public float hitFlashAlpha   = 0.85f;

    [Tooltip("Duration of the flash fade.")]
    public float flashDuration   = 0.15f;

    // ── Squad & progression ───────────────────────────────────────────────────

    [Header("Squad – XP")]
    [Tooltip("XP required to level up a character (base).")]
    public float xpPerLevel = 100f;

    [Tooltip("XP gained per successful parry.")]
    public float xpPerParry = 4f;

    // ── Fury mode ─────────────────────────────────────────────────────────────

    [Header("Fury Mode")]
    [Tooltip("Number of parries needed to fill the fury meter.")]
    public int   furyComboThreshold = 8;

    [Tooltip("Duration of fury mode (seconds).")]
    public float furyDuration = 5.0f;

    [Tooltip("Score multiplier during fury mode.")]
    public float furyScoreMultiplier = 2.0f;

    // ── Tutorial ──────────────────────────────────────────────────────────────

    [Header("Tutorial")]
    [Tooltip("Speed of the first tutorial slash (slower than normal).")]
    public float tutorialSlashSpeed = 1.5f;
}
