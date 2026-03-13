using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shows the game-over panel with final score and restart button.
/// Attach to the "GameOverPanel" GameObject (disabled at start).
/// </summary>
public class SGGameOverUI : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("References")]
    public TextMeshProUGUI finalScoreText;
    public Button          restartButton;
    public CanvasGroup     panelGroup;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        gameObject.SetActive(false);
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);
    }

    private void OnEnable()  => SGGameManager.OnGameOver += HandleGameOver;
    private void OnDisable() => SGGameManager.OnGameOver -= HandleGameOver;

    // ── Handlers ──────────────────────────────────────────────────────────────

    private void HandleGameOver()
    {
        gameObject.SetActive(true);

        if (finalScoreText != null && SGGameManager.Instance != null)
            finalScoreText.text = SGGameManager.Instance.Score.ToString();

        if (panelGroup != null)
        {
            panelGroup.alpha = 0f;
            StartCoroutine(FadeIn());
        }
    }

    private void OnRestartClicked() => SGGameManager.Instance?.Restart();

    // ── Fade in ───────────────────────────────────────────────────────────────

    private IEnumerator FadeIn()
    {
        if (panelGroup == null) yield break;
        float elapsed = 0f;
        float dur     = 0.35f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            panelGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / dur);
            yield return null;
        }
        panelGroup.alpha = 1f;
    }
}
