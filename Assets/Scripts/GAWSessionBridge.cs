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

    // ── Bouton retour menu ────────────────────────────────────────────────────

    /// <summary>Crée un canvas HUD avec un bouton retour menu discret en haut-gauche.</summary>
    private static void BuildMenuButton()
    {
        // Canvas
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

        // Bouton
        const float BtnW = 220f;
        const float BtnH = 80f;

        var go  = new GameObject("MenuButton");
        go.transform.SetParent(canvasRT, false);

        var img   = go.AddComponent<Image>();
        img.sprite = SpriteGenerator.CreateWhiteSquare();
        img.color  = new Color(0f, 0f, 0f, 0.60f);

        var rt         = img.rectTransform;
        rt.anchorMin   = new Vector2(0f, 1f);
        rt.anchorMax   = new Vector2(0f, 1f);
        rt.pivot       = new Vector2(0f, 1f);
        rt.sizeDelta   = new Vector2(BtnW, BtnH);
        rt.anchoredPosition = new Vector2(20f, -48f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() =>
        {
            if (SceneTransition.Instance != null)
                SceneTransition.Instance.LoadScene(SceneMenu, SceneMenu);
            else
                SceneManager.LoadScene(SceneMenu);
        });

        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        var tmp        = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text       = "← MENU";
        tmp.fontSize   = 32f;
        tmp.color      = new Color(1f, 1f, 1f, 0.85f);
        tmp.alignment  = TextAlignmentOptions.Center;
        tmp.fontStyle  = FontStyles.Bold;
        tmp.raycastTarget = false;
        MenuAssets.ApplyFont(tmp);
        var textRT       = tmp.rectTransform;
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = textRT.offsetMax = Vector2.zero;

        // S'assure que le ButtonClickAudio est actif pour capter le son de clic
        if (FindFirstObjectByType<ButtonClickAudio>() == null)
            new GameObject("ButtonClickAudio").AddComponent<ButtonClickAudio>();
        else
            ButtonClickAudio.HookAllButtons();
    }
}
