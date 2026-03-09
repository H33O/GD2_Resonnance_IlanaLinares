using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Manages the in-game HUD: score display with a scale-pop animation,
/// and the final score on the Game Over panel.
///
/// Attach to the "HUD" GameObject. Wire both text references in the Inspector.
/// </summary>
public class ArenaHUD : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Score")]
    [Tooltip("TextMeshPro component that shows the current score.")]
    public TextMeshProUGUI scoreText;

    [Tooltip("Peak scale multiplier of the score pop animation.")]
    [Range(1f, 2f)]
    public float scorePeakScale = 1.35f;

    [Tooltip("Duration of the score pop animation (seconds).")]
    public float scorePopDuration = 0.20f;

    [Header("Game Over")]
    [Tooltip("TextMeshPro component inside the Game Over panel showing the final score.")]
    public TextMeshProUGUI finalScoreText;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        if (ArenaGameManager.Instance != null)
        {
            ArenaGameManager.Instance.OnScoreChanged += HandleScoreChanged;
            ArenaGameManager.Instance.OnGameOver     += HandleGameOver;
        }
    }

    private void OnDisable()
    {
        if (ArenaGameManager.Instance != null)
        {
            ArenaGameManager.Instance.OnScoreChanged -= HandleScoreChanged;
            ArenaGameManager.Instance.OnGameOver     -= HandleGameOver;
        }
    }

    private void Start()
    {
        if (scoreText != null)
            scoreText.text = "0";
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private Coroutine popCoroutine;

    private void HandleScoreChanged(int score)
    {
        if (scoreText == null) return;
        scoreText.text = score.ToString();

        if (popCoroutine != null) StopCoroutine(popCoroutine);
        popCoroutine = StartCoroutine(ScorePopRoutine());
    }

    private void HandleGameOver()
    {
        if (finalScoreText == null || ArenaGameManager.Instance == null) return;
        finalScoreText.text = ArenaGameManager.Instance.Score.ToString();
    }

    // ── Score pop animation ───────────────────────────────────────────────────

    private IEnumerator ScorePopRoutine()
    {
        if (scoreText == null) yield break;

        Transform t       = scoreText.transform;
        Vector3 baseScale = Vector3.one;
        float elapsed     = 0f;
        float half        = scorePopDuration * 0.5f;

        // Scale up
        while (elapsed < half)
        {
            elapsed     += Time.deltaTime;
            float s      = Mathf.Lerp(1f, scorePeakScale, elapsed / half);
            t.localScale = Vector3.one * s;
            yield return null;
        }

        elapsed = 0f;

        // Scale back down
        while (elapsed < half)
        {
            elapsed     += Time.deltaTime;
            float s      = Mathf.Lerp(scorePeakScale, 1f, elapsed / half);
            t.localScale = Vector3.one * s;
            yield return null;
        }

        t.localScale = baseScale;
    }
}
