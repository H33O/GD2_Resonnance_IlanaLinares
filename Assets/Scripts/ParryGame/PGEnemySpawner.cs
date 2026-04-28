using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns enemies that charge from depth toward the player.
/// Enemies use the ENNEMIS.png sprite asset.
/// </summary>
public class PGEnemySpawner : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Settings")]
    public PGSettings settings;

    [Header("Audio")]
    [Tooltip("Son joué quand un ennemi est tué (ennemy death sound.mp3).")]
    public AudioClip enemyDeathSound;

    // ── State ─────────────────────────────────────────────────────────────────

    private readonly List<PGEnemy> activeEnemies = new();
    private Coroutine spawnRoutine;

    // ── Public ────────────────────────────────────────────────────────────────

    public IReadOnlyList<PGEnemy> ActiveEnemies => activeEnemies;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Plus de sprite ENNEMIS.png — visuel 100 % procédural via PGEnemyVisuals
    }

    private void OnEnable()
    {
        PGGameManager.OnGameOver    += StopSpawning;
        PGGameManager.OnGameStarted += StartSpawning;
    }

    private void OnDisable()
    {
        PGGameManager.OnGameOver    -= StopSpawning;
        PGGameManager.OnGameStarted -= StartSpawning;
    }

    private void Start()
    {
        StartSpawning();
    }

    private void Update()
    {
        // Prune destroyed enemies
        activeEnemies.RemoveAll(e => e == null);
    }

    // ── Spawn loop ────────────────────────────────────────────────────────────

    private void StartSpawning()
    {
        if (spawnRoutine != null) StopCoroutine(spawnRoutine);
        spawnRoutine = StartCoroutine(SpawnLoop());
    }

    private void StopSpawning()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    private IEnumerator SpawnLoop()
    {
        yield return new WaitForSeconds(1.5f);

        while (true)
        {
            var gm = PGGameManager.Instance;
            if (gm == null || gm.State != PGGameManager.GameState.Playing) yield break;

            SpawnEnemy();

            float delay = gm.CurrentSpawnDelay;
            yield return new WaitForSeconds(delay);
        }
    }

    // ── Enemy creation ────────────────────────────────────────────────────────

    private void SpawnEnemy()
    {
        var gm = PGGameManager.Instance;
        if (settings == null || gm == null) return;

        float xSpread = Random.Range(-1.8f, 2.4f);
        float yPos    = Random.Range(-0.3f, 0.5f);

        var go = CreateEnemyVisual();
        go.transform.position = new Vector3(xSpread, yPos, settings.enemySpawnZ);

        var enemy = go.AddComponent<PGEnemy>();
        enemy.Init(settings.enemySpawnZ, settings.playerZ, gm.CurrentEnemySpeed);
        enemy.enemyDeathSound = enemyDeathSound;

        activeEnemies.Add(enemy);
    }

    // ── Enemy visual ─────────────────────────────────────────────────────────

    private static GameObject CreateEnemyVisual()
    {
        var go = new GameObject("Enemy");
        // Taille de base — PGEnemy.RefreshScale interpole de 0.05 à 1.0 sur ce scale.
        go.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
        PGEnemyVisuals.Build(go.transform);
        return go;
    }
}
