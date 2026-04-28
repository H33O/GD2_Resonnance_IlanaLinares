using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Handles the Game Over panel interaction.
/// Attach to the "GameOverPanel" root. Wire the Restart and Menu buttons in the Inspector.
/// The panel is disabled at runtime start by ArenaGameManager and enabled on game over.
/// </summary>
public class ArenaGameOverUI : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Buttons")]
    [Tooltip("Button that reloads the scene.")]
    public Button restartButton;

    [Tooltip("Button that returns to the main menu.")]
    public Button menuButton;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);
        if (menuButton != null)
            menuButton.onClick.AddListener(OnMenuClicked);
    }

    private void OnDisable()
    {
        if (restartButton != null)
            restartButton.onClick.RemoveListener(OnRestartClicked);
        if (menuButton != null)
            menuButton.onClick.RemoveListener(OnMenuClicked);
    }

    // ── Button handlers ───────────────────────────────────────────────────────

    private void OnRestartClicked()
    {
        ArenaGameManager.Instance?.Restart();
    }

    /// <summary>Returns to the menu. <see cref="GameEndData"/> is already set by <see cref="ArenaGameManager"/>.</summary>
    private void OnMenuClicked()
    {
        string scene = MenuMainSetup.SceneName;
        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadScene(scene, scene);
        else
            SceneManager.LoadScene(scene);
    }
}
