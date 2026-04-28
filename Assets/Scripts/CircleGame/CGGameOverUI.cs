using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Shows the game-over panel when the game ends and wires the Restart and Menu buttons.
/// Attach to the <b>GameOverPanel</b> GameObject inside the ScoreUI Canvas.
/// </summary>
public class CGGameOverUI : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("References")]
    [Tooltip("The Restart button — click to reload the scene.")]
    public Button restartButton;

    [Tooltip("The Menu button — click to return to the main menu. Auto-created if null.")]
    public Button menuButton;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        gameObject.SetActive(false);

        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);

        if (menuButton != null)
            menuButton.onClick.AddListener(OnMenuClicked);
    }

    private void OnEnable()  => CGGameManager.OnGameOver += Show;
    private void OnDisable() => CGGameManager.OnGameOver -= Show;

    // ── Handlers ──────────────────────────────────────────────────────────────

    private void Show()
    {
        gameObject.SetActive(true);
    }

    private void OnRestartClicked()
    {
        CGGameManager.Instance?.Restart();
    }

    /// <summary>Returns to the menu, passing the current score via <see cref="GameEndData"/>.</summary>
    private void OnMenuClicked()
    {
        string scene = MenuMainSetup.SceneName;
        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadScene(scene, scene);
        else
            SceneManager.LoadScene(scene);
    }
}
