using UnityEngine;

/// <summary>
/// All designer-facing gameplay parameters for the Circle Game.
/// Assign one instance to every script that needs it via the Inspector.
/// Create via  Assets ▸ Create ▸ CircleGame ▸ Settings.
/// </summary>
[CreateAssetMenu(fileName = "CGSettings", menuName = "CircleGame/Settings")]
public class CGSettings : ScriptableObject
{
    // ── Arena ─────────────────────────────────────────────────────────────────

    [Header("Arena")]
    [Tooltip("Radius of the circular ring path (world units).")]
    public float arenaRadius = 4.0f;

    [Tooltip("Width of the ring line rendered by ArenaLineRenderer (world units).")]
    public float ringLineWidth = 0.22f;

    // ── Player ────────────────────────────────────────────────────────────────

    [Header("Player – Speed")]
    [Tooltip("Starting angular speed in degrees per second.")]
    public float playerSpeedStart = 90f;

    [Tooltip("Maximum angular speed reached over time.")]
    public float playerSpeedMax = 280f;

    [Tooltip("Seconds of survival needed to reach max speed.")]
    public float speedRampDuration = 70f;

    [Header("Player – Jump")]
    [Tooltip("How far inward the ball jumps (fraction of arenaRadius; 0 = none, 1 = centre).")]
    [Range(0.1f, 0.95f)]
    public float jumpDistance = 0.50f;

    [Tooltip("Duration of the inward jump phase (seconds).")]
    public float jumpDurationIn = 0.20f;

    [Tooltip("Duration of the outward return phase (seconds).")]
    public float jumpDurationOut = 0.28f;

    [Header("Player – Squash & Stretch")]
    [Tooltip("Horizontal scale factor applied at jump start.")]
    [Range(0.3f, 1f)]
    public float squashX = 0.65f;

    [Tooltip("Vertical scale factor applied at jump start.")]
    [Range(1f, 2.5f)]
    public float squashY = 1.4f;

    [Tooltip("Duration of one squash-or-stretch lerp (seconds).")]
    public float squashDuration = 0.07f;

    // ── Obstacles ─────────────────────────────────────────────────────────────

    [Header("Obstacles – Spawning")]
    [Tooltip("Initial delay between auto-spawned obstacles (seconds).")]
    public float obstacleSpawnDelayStart = 1.8f;

    [Tooltip("Minimum spawn delay reached at peak difficulty.")]
    public float obstacleSpawnDelayMin = 0.50f;

    [Tooltip("Angular offset ahead of the ball where new obstacles appear (degrees).")]
    public float obstacleSpawnAngleAhead = 150f;

    [Tooltip("Random angular jitter around the spawn angle (±degrees).")]
    public float obstacleSpawnJitter = 30f;

    [Header("Obstacles – Rotation")]
    [Tooltip("Rotation speed of the entire ObstacleContainer (degrees per second). Negative = clockwise.")]
    public float obstacleRotationSpeed = 0f;

    [Header("Obstacles – Geometry")]
    [Tooltip("Width of each obstacle rectangle (world units).")]
    public float obstacleWidth = 0.22f;

    [Tooltip("Height (depth inward) of each obstacle rectangle (world units).")]
    public float obstacleHeight = 0.80f;

    [Tooltip("Angular half-width used for collision detection (degrees).")]
    public float obstacleHalfAngle = 8f;

    // ── Collectibles ──────────────────────────────────────────────────────────

    [Header("Collectibles")]
    [Tooltip("Seconds between automatic collectible spawns.")]
    public float collectibleSpawnRate = 4f;

    [Tooltip("Angular offset ahead of the ball where collectibles appear (degrees).")]
    public float collectibleSpawnAngleAhead = 200f;

    [Tooltip("Random jitter around the spawn angle (±degrees).")]
    public float collectibleSpawnJitter = 45f;

    [Tooltip("Score points awarded per collectible pickup.")]
    public int collectibleScoreBonus = 5;

    [Tooltip("Extra obstacles spawned each time a collectible is collected.")]
    [Range(1, 5)]
    public int obstaclesPerCollectible = 1;

    // ── Feedback – Camera Shake ───────────────────────────────────────────────

    [Header("Feedback – Camera Shake")]
    [Tooltip("Maximum camera offset magnitude during a shake (world units).")]
    public float cameraShakeStrength = 0.20f;

    [Tooltip("Duration of the camera shake (seconds).")]
    public float cameraShakeDuration = 0.35f;

    // ── Feedback – Slow Motion ────────────────────────────────────────────────

    [Header("Feedback – Slow Motion")]
    [Tooltip("Time scale applied during pre-game-over slow-motion.")]
    [Range(0.05f, 0.5f)]
    public float slowMoScale = 0.12f;

    [Tooltip("Real-time duration of the slow-motion window (seconds).")]
    public float slowMoDuration = 0.30f;

    // ── Feedback – Near Miss Flash ────────────────────────────────────────────

    [Header("Feedback – Near Miss Flash")]
    [Tooltip("Angular distance from obstacle edge that triggers the near-miss flash (degrees).")]
    public float nearMissWindowDeg = 22f;

    [Tooltip("Peak alpha of the screen flash on near miss.")]
    [Range(0f, 1f)]
    public float nearMissFlashAlpha = 0.30f;

    [Tooltip("Duration of the near-miss flash fade (seconds).")]
    public float nearMissFlashDuration = 0.12f;

    // ── Feedback – Particles ──────────────────────────────────────────────────

    [Header("Feedback – Particles")]
    [Tooltip("Particle emit count when a collectible is picked up.")]
    [Range(1, 40)]
    public int collectParticleCount = 12;

    [Tooltip("Particle emit count on a perfect-dodge.")]
    [Range(1, 20)]
    public int dodgeParticleCount = 6;

    [Tooltip("Overall particle intensity multiplier (scales emit counts).")]
    [Range(0.1f, 3f)]
    public float particleIntensity = 1f;

    // ── Feedback – Ripple ─────────────────────────────────────────────────────

    [Header("Feedback – Ripple")]
    [Tooltip("Maximum radius of the tap-ripple ring (world units).")]
    public float rippleMaxRadius = 0.70f;

    [Tooltip("Duration of a ripple expansion (seconds).")]
    public float rippleDuration = 0.25f;
}
