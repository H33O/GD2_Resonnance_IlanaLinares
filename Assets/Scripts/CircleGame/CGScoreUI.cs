using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Displays the live score and animates a pop on each increment.
/// Attach to the <b>ScoreUI</b> GameObject (or its ScoreText child).
/// </summary>
public class CGScoreUI : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("References")]
    [Tooltip("TextMeshPro component that shows the live score.")]
    public TextMeshProUGUI scoreText;

    [Tooltip("TextMeshPro shown on the game-over panel with the final score.")]
    public TextMeshProUGUI finalScoreText;

    [Header("Score Pop Animation")]
    [Tooltip("Peak scale multiplier when the score increases.")]
    public float popScale = 1.35f;

    [Tooltip("Duration of the pop animation (seconds).")]
    public float popDuration = 0.18f;

    // ── State ─────────────────────────────────────────────────────────────────

    private Vector3    baseScale;
    private Coroutine  popCoroutine;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (scoreText != null)
        {
            baseScale          = scoreText.transform.localScale;
            scoreText.text     = "0";
        }
        if (finalScoreText != null) finalScoreText.text = "0";
    }

    private void OnEnable()
    {
        CGGameManager.OnScoreChanged += HandleScoreChanged;
        CGGameManager.OnGameOver     += HandleGameOver;
    }

    private void OnDisable()
    {
        CGGameManager.OnScoreChanged -= HandleScoreChanged;
        CGGameManager.OnGameOver     -= HandleGameOver;
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private void HandleScoreChanged(int score)
    {
        if (scoreText != null) scoreText.text = score.ToString();
        PopScore();
    }

    private void HandleGameOver()
    {
        int final = CGGameManager.Instance != null ? CGGameManager.Instance.Score : 0;
        if (finalScoreText != null) finalScoreText.text = final.ToString();
    }

    // ── Pop animation ─────────────────────────────────────────────────────────

    private void PopScore()
    {
        if (scoreText == null) return;
        if (popCoroutine != null) StopCoroutine(popCoroutine);
        popCoroutine = StartCoroutine(PopRoutine());
    }

    private IEnumerator PopRoutine()
    {
        float half = popDuration * 0.5f;
        Vector3 bigScale = baseScale * popScale;

        // Scale up
        float t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            scoreText.transform.localScale = Vector3.Lerp(baseScale, bigScale, t / half);
            yield return null;
        }

        // Scale back down
        t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            scoreText.transform.localScale = Vector3.Lerp(bigScale, baseScale, t / half);
            yield return null;
        }

        scoreText.transform.localScale = baseScale;
    }
}
