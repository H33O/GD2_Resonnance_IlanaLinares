using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns obstacle prefabs onto the arena ring during gameplay.
/// Pre-placed obstacles in the scene hierarchy are also registered on Start.
///
/// Attach to the "ObstacleSpawner" GameObject.
/// The obstacle prefab must have an <see cref="ArenaObstacle"/> component.
/// </summary>
public class ArenaObstacleSpawner : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Settings")]
    public ArenaSettings settings;

    [Header("References")]
    [Tooltip("Prefab instantiated for each new obstacle.")]
    public GameObject obstaclePrefab;

    [Tooltip("Ball instance in the scene.")]
    public ArenaBall ball;

    [Tooltip("Parent transform that holds all spawned obstacles.")]
    public Transform obstaclesRoot;

    [Header("Pre-placed Obstacles")]
    [Tooltip("Obstacles already present in the scene at startup. " +
             "They will be registered and have their ball reference injected.")]
    public List<ArenaObstacle> prePlacedObstacles = new List<ArenaObstacle>();

    // ── Runtime ───────────────────────────────────────────────────────────────

    private float spawnTimer;
    private float currentDelay;
    private float survivalTime => ArenaGameManager.Instance != null
                                  ? ArenaGameManager.Instance.SurvivalTime
                                  : 0f;

    private readonly List<ArenaObstacle> activeObstacles = new();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        currentDelay = settings.obstacleSpawnDelayStart;

        // Inject ball reference into pre-placed obstacles
        foreach (var obs in prePlacedObstacles)
        {
            if (obs == null) continue;
            obs.ball     = ball;
            obs.settings = settings;
            activeObstacles.Add(obs);
        }
    }

    private void Update()
    {
        if (ArenaGameManager.Instance == null) return;
        if (ArenaGameManager.Instance.State != ArenaGameManager.GameState.Playing) return;

        spawnTimer += Time.deltaTime;

        if (spawnTimer >= currentDelay)
        {
            spawnTimer = 0f;
            SpawnObstacle();
            RefreshSpawnDelay();
        }
    }

    // ── Spawning ──────────────────────────────────────────────────────────────

    /// <summary>Instantiates a new obstacle at a random position ahead of the ball.</summary>
    private void SpawnObstacle()
    {
        if (obstaclePrefab == null || ball == null) return;

        float spawnAngle = ball.AngleDeg
                         + settings.obstacleSpawnAngleAhead
                         + Random.Range(-settings.obstacleSpawnJitter, settings.obstacleSpawnJitter);

        Transform parent = obstaclesRoot != null ? obstaclesRoot : transform;
        GameObject go    = Instantiate(obstaclePrefab, parent);

        PlaceOnRing(go.transform, spawnAngle);

        var obs      = go.GetComponent<ArenaObstacle>();
        if (obs != null)
        {
            obs.ball     = ball;
            obs.settings = settings;
            obs.angleDeg = spawnAngle;
            activeObstacles.Add(obs);
        }
    }

    /// <summary>Positions and rotates a transform so it sits on the ring wall.</summary>
    public void PlaceOnRing(Transform t, float angle)
    {
        float rad  = angle * Mathf.Deg2Rad;
        float edge = settings.orbitRadius;
        t.position        = new Vector3(Mathf.Cos(rad) * edge, Mathf.Sin(rad) * edge, 0f);
        t.localRotation   = Quaternion.Euler(0f, 0f, angle - 90f);
    }

    /// <summary>Recalculates spawn delay based on current difficulty progression.</summary>
    private void RefreshSpawnDelay()
    {
        float progress = Mathf.Clamp01(survivalTime / settings.speedRampDuration);
        currentDelay   = Mathf.Lerp(
            settings.obstacleSpawnDelayStart,
            settings.obstacleSpawnDelayMin,
            progress);
    }

    /// <summary>Removes and destroys all dynamically spawned obstacles.</summary>
    public void ClearSpawned()
    {
        foreach (var obs in activeObstacles)
        {
            if (obs != null && !prePlacedObstacles.Contains(obs))
                Destroy(obs.gameObject);
        }
        activeObstacles.RemoveAll(o => !prePlacedObstacles.Contains(o));
    }
}
