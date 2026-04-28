using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central game-state controller for the Circle Arena mini-game.
/// Place on an empty "GameManager" GameObject in the scene.
/// Wire all Inspector references manually or via the scene setup.
/// </summary>
public class ArenaGameManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static ArenaGameManager Instance { get; private set; }

    // ── Inspector references ──────────────────────────────────────────────────

    [Header("Settings")]
    [Tooltip("ScriptableObject with all tunable parameters.")]
    public ArenaSettings settings;

    [Header("Scene References")]
    [Tooltip("The ArenaBall component on the ball GameObject.")]
    public ArenaBall ball;

    [Tooltip("The obstacle spawner responsible for placing obstacles on the ring.")]
    public ArenaObstacleSpawner spawner;

    [Tooltip("Feedback manager driving shake, flash, slow-motion effects.")]
    public ArenaFeedbackManager feedback;

    [Tooltip("HUD controller managing score display.")]
    public ArenaHUD hud;

    [Tooltip("Game Over panel root (disabled at start).")]
    public GameObject gameOverPanel;

    // ── State ─────────────────────────────────────────────────────────────────

    public enum GameState { Playing, Dead, GameOver }

    public GameState State { get; private set; } = GameState.Playing;

    public float SurvivalTime { get; private set; }
    public int   Score        { get; private set; }

    // ── Events ────────────────────────────────────────────────────────────────

    public event System.Action<int>  OnScoreChanged;
    public event System.Action       OnGameOver;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    private void Update()
    {
        if (State != GameState.Playing) return;

        SurvivalTime += Time.deltaTime;
        UpdateScore();
    }

    // ── Score ─────────────────────────────────────────────────────────────────

    private int lastScore = -1;

    /// <summary>Called every frame while playing to compute and broadcast score.</summary>
    private void UpdateScore()
    {
        Score = Mathf.FloorToInt(SurvivalTime);
        if (Score == lastScore) return;
        lastScore = Score;
        OnScoreChanged?.Invoke(Score);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Called when the ball collects a collectible. Awards a score bonus.</summary>
    public void NotifyCollectiblePickup()
    {
        if (State != GameState.Playing) return;
        Score += settings.collectibleScoreBonus;
        OnScoreChanged?.Invoke(Score);
    }

    /// <summary>Called by ArenaBall when it collides with an obstacle.</summary>
    public void NotifyCollision()
    {
        if (State != GameState.Playing) return;
        StartCoroutine(GameOverSequence());
    }

    /// <summary>Called by obstacle near-miss detection.</summary>
    public void NotifyNearMiss(Vector3 worldPosition)
    {
        if (State != GameState.Playing) return;
        feedback?.TriggerNearMiss(worldPosition);
    }

    /// <summary>Called by obstacle near-miss detection when inside perfect dodge window.</summary>
    public void NotifyPerfectDodge(Vector3 worldPosition)
    {
        if (State != GameState.Playing) return;
        feedback?.TriggerPerfectDodge(worldPosition);
    }

    /// <summary>Restarts the current scene.</summary>
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ── Game over sequence ────────────────────────────────────────────────────

    private IEnumerator GameOverSequence()
    {
        State = GameState.Dead;

        feedback?.TriggerCollisionShake();
        feedback?.TriggerCollisionFlash();

        Time.timeScale = settings.slowMoScale;
        yield return new WaitForSecondsRealtime(settings.slowMoDuration);
        Time.timeScale = 1f;

        State = GameState.GameOver;
        OnGameOver?.Invoke();

        // Persistance du score Arena (Ball & Goal)
        ScoreManager.EnsureExists();
        ScoreManager.Instance.AddScore(GameType.BallAndGoal, Score);
        GameEndData.Set(Score, GameType.BallAndGoal);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }
}
