using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central game-state machine for the Circle Game.
/// Drives score, time-scale, and broadcasts events consumed by other systems.
/// Attach to the <b>GameManager</b> GameObject in the scene.
/// </summary>
public class CGGameManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static CGGameManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Settings")]
    [Tooltip("Shared settings ScriptableObject — assign the same asset to every script.")]
    public CGSettings settings;

    // ── Events ────────────────────────────────────────────────────────────────

    public static event Action<int>  OnScoreChanged;
    public static event Action       OnGameStarted;
    public static event Action       OnGameOver;
    public static event Action       OnRestart;

    // ── State ─────────────────────────────────────────────────────────────────

    public enum GameState { Playing, Dead, GameOver }

    public GameState State { get; private set; } = GameState.Playing;
    public int       Score { get; private set; }

    // ── Life cycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        Time.timeScale = 1f;
        OnGameStarted?.Invoke();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Adds <paramref name="amount"/> to the score and notifies listeners.</summary>
    public void AddScore(int amount)
    {
        if (State != GameState.Playing) return;
        Score += amount;
        OnScoreChanged?.Invoke(Score);
    }

    /// <summary>Called by <see cref="CGPlayerBall"/> on obstacle collision.</summary>
    public void TriggerGameOver()
    {
        if (State != GameState.Playing) return;
        State = GameState.Dead;
        StartCoroutine(GameOverSequence());
    }

    /// <summary>Called by the Restart button in the Game Over UI.</summary>
    public void Restart()
    {
        Time.timeScale = 1f;
        OnRestart?.Invoke();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ── Game-over sequence ────────────────────────────────────────────────────

    private System.Collections.IEnumerator GameOverSequence()
    {
        // Brief slow-motion before game over
        Time.timeScale = settings != null ? settings.slowMoScale : 0.12f;
        float duration = settings != null ? settings.slowMoDuration : 0.30f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;

        State = GameState.GameOver;
        OnGameOver?.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
