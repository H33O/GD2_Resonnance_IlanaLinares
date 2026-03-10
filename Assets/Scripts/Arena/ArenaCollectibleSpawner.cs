using UnityEngine;

/// <summary>
/// Periodically spawns collectible prefabs on the arena ring during gameplay.
/// When a collectible is picked up, <see cref="ArenaCollectible"/> calls
/// <see cref="ArenaObstacleSpawner.SpawnObstacleFromCollectible"/> to add an extra obstacle.
///
/// Attach to the "GameManager" GameObject (or any persistent GameObject in the scene).
/// Wire CollectiblePrefab, Spawner, Ball, and ObstaclesRoot in the Inspector.
/// </summary>
public class ArenaCollectibleSpawner : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Settings")]
    public ArenaSettings settings;

    [Tooltip("Prefab to spawn (must have ArenaCollectible component).")]
    public GameObject collectiblePrefab;

    [Tooltip("Angular offset ahead of the ball where collectibles spawn (degrees).")]
    public float spawnAngleAhead = 200f;

    [Tooltip("Random angular jitter applied to each spawn position (±degrees).")]
    public float spawnJitter = 45f;

    [Tooltip("Seconds between each collectible spawn.")]
    public float spawnInterval = 4f;

    [Header("References")]
    [Tooltip("The ball, used to offset the spawn angle.")]
    public ArenaBall ball;

    [Tooltip("Parent transform for spawned collectibles.")]
    public Transform collectiblesRoot;

    // ── Runtime ───────────────────────────────────────────────────────────────

    private float timer;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Update()
    {
        if (ArenaGameManager.Instance == null) return;
        if (ArenaGameManager.Instance.State != ArenaGameManager.GameState.Playing) return;

        // Prefer the settings asset values when available, fall back to local fields
        float interval = (settings != null) ? settings.collectibleSpawnInterval : spawnInterval;

        timer += Time.deltaTime;
        if (timer >= interval)
        {
            timer = 0f;
            SpawnCollectible();
        }
    }

    // ── Spawning ──────────────────────────────────────────────────────────────

    /// <summary>Instantiates a collectible at a random position ahead of the ball.</summary>
    private void SpawnCollectible()
    {
        if (collectiblePrefab == null || ball == null || settings == null) return;

        float aheadAngle = settings.collectibleSpawnAngleAhead;
        float jitter     = settings.collectibleSpawnJitter;

        float angle = ball.AngleDeg
                    + aheadAngle
                    + Random.Range(-jitter, jitter);

        float rad = angle * Mathf.Deg2Rad;
        float r   = settings.orbitRadius;
        var pos   = new UnityEngine.Vector3(Mathf.Cos(rad) * r, Mathf.Sin(rad) * r, 0f);

        Transform parent = collectiblesRoot != null ? collectiblesRoot : transform;
        GameObject go    = Instantiate(collectiblePrefab, pos, UnityEngine.Quaternion.identity, parent);
        go.transform.localScale = new UnityEngine.Vector3(0.35f, 0.35f, 1f);
    }
}
