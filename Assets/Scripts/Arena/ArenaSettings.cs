using UnityEngine;

/// <summary>
/// ScriptableObject holding every tunable parameter of the Circle Arena mini-game.
/// Create via Assets ▸ Create ▸ CircleArena ▸ Arena Settings.
/// </summary>
[CreateAssetMenu(fileName = "ArenaSettings", menuName = "CircleArena/Arena Settings")]
public class ArenaSettings : ScriptableObject
{
    // ── Arena ─────────────────────────────────────────────────────────────────

    [Header("Arena")]
    [Tooltip("Inner orbit radius the ball travels along (world units).")]
    public float orbitRadius = 4.0f;

    // ── Ball ──────────────────────────────────────────────────────────────────

    [Header("Ball – Movement")]
    [Tooltip("Starting angular speed in degrees per second.")]
    public float ballSpeedStart = 90f;

    [Tooltip("Maximum angular speed reached over time.")]
    public float ballSpeedMax = 280f;

    [Tooltip("Seconds of gameplay needed to reach maximum speed.")]
    public float speedRampDuration = 70f;

    [Header("Ball – Jump")]
    [Tooltip("How far inward the ball jumps expressed as a fraction of orbitRadius (0=none, 1=centre).")]
    [Range(0.1f, 0.95f)]
    public float jumpDepthRatio = 0.50f;

    [Tooltip("Duration of the inward phase of the jump (seconds).")]
    public float jumpDurationIn = 0.20f;

    [Tooltip("Duration of the outward return phase of the jump (seconds).")]
    public float jumpDurationOut = 0.28f;

    [Header("Ball – Squash & Stretch")]
    [Tooltip("Horizontal squash factor applied at the start of a jump.")]
    [Range(0.4f, 1f)]
    public float squashX = 0.65f;

    [Tooltip("Vertical stretch factor applied at the start of a jump.")]
    [Range(1f, 2f)]
    public float squashY = 1.38f;

    [Tooltip("Duration of one squash/stretch lerp phase (seconds).")]
    public float squashDuration = 0.07f;

    // ── Trail ─────────────────────────────────────────────────────────────────

    [Header("Trail")]
    [Tooltip("Number of trail segments rendered behind the ball.")]
    [Range(4, 32)]
    public int trailSegments = 14;

    [Tooltip("Time between each trail snapshot (seconds).")]
    public float trailInterval = 0.018f;

    [Tooltip("Peak opacity of the first trail segment.")]
    [Range(0f, 1f)]
    public float trailMaxAlpha = 0.45f;

    // ── Obstacles ─────────────────────────────────────────────────────────────

    [Header("Obstacles – Spawning")]
    [Tooltip("Initial delay between obstacle spawns (seconds).")]
    public float obstacleSpawnDelayStart = 1.8f;

    [Tooltip("Minimum spawn delay reached at peak difficulty.")]
    public float obstacleSpawnDelayMin = 0.50f;

    [Tooltip("Angular offset ahead of the ball where obstacles spawn (degrees).")]
    public float obstacleSpawnAngleAhead = 150f;

    [Tooltip("Random angular jitter applied to each spawn position (±degrees).")]
    public float obstacleSpawnJitter = 30f;

    [Header("Obstacles – Geometry")]
    [Tooltip("Half-angle of the obstacle's collision zone (degrees). Acts as the hit window.")]
    public float obstacleHalfAngle = 8f;

    // ── Near-miss & Dodge ─────────────────────────────────────────────────────

    [Header("Near Miss")]
    [Tooltip("Angular window inside which a jump counts as a perfect dodge (degrees).")]
    public float dodgeWindowDeg = 12f;

    [Tooltip("Angular window that triggers the near-miss white flash (degrees).")]
    public float nearMissWindowDeg = 22f;

    // ── Feedback ──────────────────────────────────────────────────────────────

    [Header("Feedback – Screen Shake")]
    [Tooltip("Magnitude of the camera shake on collision (world units).")]
    public float shakeMagnitude = 0.18f;

    [Tooltip("Duration of the screen shake (seconds).")]
    public float shakeDuration = 0.35f;

    [Header("Feedback – Slow Motion")]
    [Tooltip("Time scale applied during the pre-game-over slow-motion effect.")]
    [Range(0.05f, 0.5f)]
    public float slowMoScale = 0.12f;

    [Tooltip("Real-time duration of the slow-motion window before game over (seconds).")]
    public float slowMoDuration = 0.30f;

    [Header("Feedback – Flash")]
    [Tooltip("Peak alpha of the near-miss white flash overlay.")]
    [Range(0f, 1f)]
    public float nearMissFlashAlpha = 0.25f;

    [Tooltip("Duration of the near-miss flash fade (seconds).")]
    public float nearMissFlashDuration = 0.12f;

    [Header("Feedback – Particles")]
    [Tooltip("Number of particles emitted on a perfect dodge.")]
    [Range(1, 30)]
    public int dodgeParticleCount = 8;

    [Tooltip("Outward speed of dodge particles (world units/second).")]
    public float dodgeParticleSpeed = 3.5f;

    [Tooltip("Lifetime of each dodge particle (seconds).")]
    public float dodgeParticleLifetime = 0.35f;

    [Header("Feedback – Ripple")]
    [Tooltip("Maximum radius of the tap ripple ring (world units).")]
    public float rippleMaxRadius = 0.7f;

    [Tooltip("Duration of a single ripple expansion (seconds).")]
    public float rippleDuration = 0.28f;
}
