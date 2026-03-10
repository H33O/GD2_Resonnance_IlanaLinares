using UnityEngine;

/// <summary>
/// Periodically spawns <see cref="CGCollectible"/> instances on the circular path.
/// Attach to the <b>CollectibleSpawner</b> GameObject.
/// </summary>
public class CGCollectibleSpawner : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Settings")]
    public CGSettings settings;

    [Header("References")]
    [Tooltip("Prefab for the collectible (white square dot).")]
    public CGCollectible collectiblePrefab;

    [Tooltip("PlayerBall — used to place collectibles ahead of the player.")]
    public CGPlayerBall playerBall;

    [Tooltip("Optional transform to parent spawned collectibles under.")]
    public Transform collectiblesRoot;

    // ── State ─────────────────────────────────────────────────────────────────

    private float timer;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

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

        float interval = settings != null ? settings.collectibleSpawnRate : 4f;
        timer += Time.deltaTime;
        if (timer >= interval)
        {
            timer = 0f;
            Spawn();
        }
    }

    // ── Spawning ──────────────────────────────────────────────────────────────

    private void Spawn()
    {
        if (collectiblePrefab == null || playerBall == null || settings == null) return;

        float ahead  = settings.collectibleSpawnAngleAhead;
        float jitter = settings.collectibleSpawnJitter;
        float angle  = playerBall.AngleDeg + ahead + Random.Range(-jitter, jitter);

        float rad    = angle * Mathf.Deg2Rad;
        float r      = settings.arenaRadius;
        var   pos    = new Vector3(Mathf.Cos(rad) * r, Mathf.Sin(rad) * r, 0f);

        Transform parent = collectiblesRoot != null ? collectiblesRoot : transform;
        Instantiate(collectiblePrefab, pos, Quaternion.identity, parent);
    }

    private void HandleGameOver()
    {
        enabled = false;
    }
}
