using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows the game-over panel when the game ends and wires the Restart button.
/// Attach to the <b>GameOverPanel</b> GameObject inside the ScoreUI Canvas.
/// </summary>
public class CGGameOverUI : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("References")]
    [Tooltip("The Restart button — click to reload the scene.")]
    public Button restartButton;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        gameObject.SetActive(false); // hidden until game over

        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);
    }

    private void OnEnable()
    {
        CGGameManager.OnGameOver += Show;
    }

    private void OnDisable()
    {
        CGGameManager.OnGameOver -= Show;
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private void Show()
    {
        gameObject.SetActive(true);
    }

    private void OnRestartClicked()
    {
        CGGameManager.Instance?.Restart();
    }
}
