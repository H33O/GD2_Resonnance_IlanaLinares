using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the <b>ObstacleContainer</b> GameObject.
/// Rotates the container, auto-spawns new obstacles over time, and handles
/// difficulty escalation triggered by collectible pickups.
/// Attach to the <b>ObstacleContainer</b> GameObject.
/// </summary>
public class CGObstacleContainer : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Settings")]
    public CGSettings settings;

    [Header("References")]
    [Tooltip("Prefab for auto-spawned obstacles.")]
    public CGObstacle obstaclePrefab;

    [Tooltip("The PlayerBall — used to position new obstacles ahead of the player.")]
    public CGPlayerBall playerBall;

    [Header("Pre-placed Obstacles")]
    [Tooltip("Obstacles already placed as children in the scene hierarchy.")]
    public List<CGObstacle> prePlacedObstacles = new();

    // ── State ─────────────────────────────────────────────────────────────────

    private float spawnTimer;
    private float currentSpawnDelay;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        currentSpawnDelay = settings != null ? settings.obstacleSpawnDelayStart : 1.8f;
    }

    private void OnEnable()
    {
        CGGameManager.OnGameOver += HandleGameOver;
    }

    private void OnDisable()
    {
        CGGameManager.OnGameOver -= HandleGameOver;
    }

    private void Update()
    {
        if (CGGameManager.Instance?.State != CGGameManager.GameState.Playing) return;

        // Rotate the entire container
        float rotSpeed = settings != null ? settings.obstacleRotationSpeed : 0f;
        transform.Rotate(0f, 0f, rotSpeed * Time.deltaTime);

        // Auto-spawn new obstacles
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= currentSpawnDelay)
        {
            spawnTimer = 0f;
            SpawnObstacle();
            RampDifficulty();
        }
    }

    // ── Spawning ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Spawns a single obstacle at a random angle ahead of the player.
    /// </summary>
    public void SpawnObstacle()
    {
        if (obstaclePrefab == null || playerBall == null || settings == null) return;

        float ahead  = settings.obstacleSpawnAngleAhead;
        float jitter = settings.obstacleSpawnJitter;
        float angle  = playerBall.AngleDeg + ahead + Random.Range(-jitter, jitter);

        float rad = angle * Mathf.Deg2Rad;
        var pos   = new Vector3(Mathf.Cos(rad) * settings.arenaRadius,
                                Mathf.Sin(rad) * settings.arenaRadius,
                                0f);

        // The obstacle is instantiated in world space then parented to this container
        CGObstacle obs = Instantiate(obstaclePrefab, transform);
        obs.transform.localPosition = new Vector3(
            Mathf.Cos(rad) * settings.arenaRadius,
            Mathf.Sin(rad) * settings.arenaRadius,
            0f);
        obs.transform.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);
        obs.AngleDeg = angle;
    }

    /// <summary>
    /// Spawns N extra obstacles when a collectible is collected.
    /// </summary>
    public void SpawnFromCollectible()
    {
        int count = settings != null ? settings.obstaclesPerCollectible : 1;
        for (int i = 0; i < count; i++)
            SpawnObstacle();
    }

    // ── Difficulty ────────────────────────────────────────────────────────────

    private void RampDifficulty()
    {
        if (settings == null) return;
        // Reduce spawn delay toward the minimum as the game progresses
        currentSpawnDelay = Mathf.Max(
            currentSpawnDelay - 0.04f,
            settings.obstacleSpawnDelayMin);
    }

    private void HandleGameOver()
    {
        enabled = false;
    }
}
