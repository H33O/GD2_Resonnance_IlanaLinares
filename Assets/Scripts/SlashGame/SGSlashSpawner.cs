using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns incoming slash attacks procedurally.
/// Manages difficulty ramp (spawn delay and speed over time).
/// Respects the tutorial flow by pausing until the tutorial step completes.
/// </summary>
public class SGSlashSpawner : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Settings")]
    public SGSettings settings;

    // ── State ─────────────────────────────────────────────────────────────────

    private float survivalTime;
    private bool  spawning;

    // Track active slashes so the player controller can iterate them
    private readonly List<SGSlash> activeSlashes = new();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        SGGameManager.OnGameStarted += HandleGameStarted;
        SGGameManager.OnGameOver    += HandleGameOver;
    }

    private void OnDisable()
    {
        SGGameManager.OnGameStarted -= HandleGameStarted;
        SGGameManager.OnGameOver    -= HandleGameOver;
    }

    private void Update()
    {
        if (SGGameManager.Instance == null) return;
        var state = SGGameManager.Instance.State;
        if (state != SGGameManager.GameState.Playing) return;

        survivalTime += Time.deltaTime;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Starts the automatic spawn loop for live gameplay.</summary>
    public void StartSpawning()
    {
        if (spawning) return;
        spawning = true;
        StartCoroutine(SpawnLoop());
    }

    /// <summary>Stops the spawn loop (e.g. on game over).</summary>
    public void StopSpawning()
    {
        spawning = false;
        StopAllCoroutines();
    }

    /// <summary>
    /// Spawns one slash from a given angle, used by tutorial controller.
    /// Returns the spawned slash component.
    /// </summary>
    public SGSlash SpawnTutorialSlash(float angleDeg)
    {
        return SpawnSlash(angleDeg, settings.tutorialSlashSpeed, tutorial: true);
    }

    /// <summary>Read-only list of currently active slashes for parry detection.</summary>
    public IReadOnlyList<SGSlash> ActiveSlashes => activeSlashes;

    /// <summary>Called by a slash to notify it is destroyed.</summary>
    public void NotifySlashDestroyed(SGSlash slash)
    {
        activeSlashes.Remove(slash);
    }

    // ── Spawn loop ────────────────────────────────────────────────────────────

    private IEnumerator SpawnLoop()
    {
        while (spawning)
        {
            float delay = Mathf.Lerp(
                settings.spawnDelayStart,
                settings.spawnDelayMin,
                Mathf.Clamp01(survivalTime / settings.speedRampDuration));

            yield return new WaitForSeconds(delay);

            if (!spawning) yield break;
            if (SGGameManager.Instance?.State != SGGameManager.GameState.Playing) yield break;

            float angle = Random.Range(0f, 360f);
            float speed = Mathf.Lerp(
                settings.slashSpeedStart,
                settings.slashSpeedMax,
                Mathf.Clamp01(survivalTime / settings.speedRampDuration));

            SpawnSlash(angle, speed);
        }
    }

    // ── Internal helpers ──────────────────────────────────────────────────────

    private SGSlash SpawnSlash(float angleDeg, float speed, bool tutorial = false)
    {
        float rad    = angleDeg * Mathf.Deg2Rad;
        float radius = settings.spawnRadius;
        var   pos    = new Vector3(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius, 0f);

        var go    = new GameObject($"Slash_{angleDeg:F0}");
        var slash = go.AddComponent<SGSlash>();
        slash.Init(pos, speed, settings, tutorial, this);

        activeSlashes.Add(slash);
        return slash;
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void HandleGameStarted() { }

    private void HandleGameOver() => StopSpawning();
}
