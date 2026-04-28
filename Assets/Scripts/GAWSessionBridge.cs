using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Bridge minimal pour la scène GameAndWatch.
/// Délègue entièrement la fin de partie à <see cref="GameEndScreen"/>.
///
/// Garantit que les singletons persistants (<see cref="ScoreManager"/>,
/// <see cref="QuestManager"/>) existent avant que la partie ne commence.
///
/// Crée également un bouton retour menu discret (coin haut-gauche).
/// </summary>
public class GAWSessionBridge : MonoBehaviour
{
    private const string SceneMenu = "Menu";

    private void Start()
    {
        ScoreManager.EnsureExists();
        QuestManager.EnsureExists();

        EnsureOWGameManager();
        EnsureGameEndScreen();
        EnsureEventSystem();
        BuildMenuButton();
    }

    private static void EnsureOWGameManager()
    {
        if (OWGameManager.Instance != null) return;
        new GameObject("OWGameManager").AddComponent<OWGameManager>();
    }

    private static void EnsureGameEndScreen()
    {
        if (FindFirstObjectByType<GameEndScreen>() != null) return;

        var go     = new GameObject("GameEndScreen");
        var screen = go.AddComponent<GameEndScreen>();

        var field = typeof(GameEndScreen)
            .GetField("gameType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(screen, GameType.GameAndWatch);
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    // ── Bouton pause + panneau ────────────────────────────────────────────────

    /// <summary>Crée un Canvas HUD avec le bouton II et le panneau de pause (bas-gauche).</summary>
    private static void BuildMenuButton()
    {
        // Canvas dédié (overlay par-dessus le jeu GameAndWatch)
        var canvasGO = new GameObject("GAWMenuButtonCanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        var scaler                 = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight  = 1f;

        canvasGO.AddComponent<GraphicRaycaster>();

        var canvasRT = canvas.GetComponent<RectTransform>();

        // Ordre de sibling : bouton d'abord, panneau ensuite (panneau au-dessus).
        GamePausePanel.CreatePauseButton(canvasRT);
        GamePausePanel.Create(canvasRT,
            onResume: null,
            onMenu: () =>
            {
                if (SceneTransition.Instance != null)
                    SceneTransition.Instance.LoadScene(SceneMenu, SceneMenu);
                else
                    SceneManager.LoadScene(SceneMenu);
            });

        // S'assure que le ButtonClickAudio est actif pour capter le son de clic
        if (FindFirstObjectByType<ButtonClickAudio>() == null)
            new GameObject("ButtonClickAudio").AddComponent<ButtonClickAudio>();
        else
            ButtonClickAudio.HookAllButtons();
    }
}
