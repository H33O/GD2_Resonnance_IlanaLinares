using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles the Game Over panel interaction.
/// Attach to the "GameOverPanel" root. Wire the Restart button in the Inspector.
/// The panel is disabled at runtime start by ArenaGameManager and enabled on game over.
/// </summary>
public class ArenaGameOverUI : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Buttons")]
    [Tooltip("Button that reloads the scene.")]
    public Button restartButton;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);
    }

    private void OnDisable()
    {
        if (restartButton != null)
            restartButton.onClick.RemoveListener(OnRestartClicked);
    }

    // ── Button handler ────────────────────────────────────────────────────────

    private void OnRestartClicked()
    {
        ArenaGameManager.Instance?.Restart();
    }
}
