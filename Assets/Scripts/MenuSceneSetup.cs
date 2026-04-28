using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Construit procéduralement le menu principal sur un Canvas Screen Space Overlay.
///
/// Layout :
///   - Fond noir
///   - HUD haut-gauche (Score cliquable) + haut-droite (Horloge) via <see cref="MenuMainHud"/>
///   - Porte blanche centrée (plane assignable via Inspector) via <see cref="DoorManager"/>
///   - Bouton GAME bas-droite → panneau slide via <see cref="MenuGameSelectPanel"/>
/// </summary>
public class MenuSceneSetup : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Porte")]
    [Tooltip("Sprite assigné au plane central de la porte.")]
    [SerializeField] public Sprite doorSprite;

    [Header("Porte — Overlay intérieur")]
    [Tooltip("Image de fond de l'overlay intérieur porte (fond_interieur porte.png).")]
    [SerializeField] public Sprite doorInteriorSprite;

    [Tooltip("Sprite du bouton UI central de l'overlay (bouton UI.png).")]
    [SerializeField] public Sprite doorButtonSprite;

    [Tooltip("Nom de la scène chargée depuis l'overlay intérieur porte.")]
    [SerializeField] public string doorTargetScene = "CircleArena";

    [Tooltip("Libellé affiché sur le bouton de l'overlay intérieur porte.")]
    [SerializeField] public string doorButtonLabel = "JOUER";

    [Header("Boutons")]
    [Tooltip("Sprite 9-slice appliqué à tous les boutons du menu (ex : bouton UI.png).")]
    [SerializeField] public Sprite buttonSprite;

    // ── Init ──────────────────────────────────────────────────────────────────

    private void Awake()
    {
        EnsureEventSystem();
        EnsureSceneTransition();
    }

    private void Start()
    {
        // Singletons persistants (DontDestroyOnLoad)
        NeedsManager.EnsureExists();
        ShopManager.EnsureExists();
        ScoreManager.EnsureExists();

        // Assets partagés pour tous les builders — Michroma via MenuAssets.Init
        MenuAssets.Init(buttonSprite);

        var canvasRT = BuildCanvas();

        BuildBackground(canvasRT);
        BuildBouncingDots(canvasRT);
        BuildHud(canvasRT);
        BuildDoorManager(canvasRT);   // DoorManager avant MenuDoor
        BuildDoor(canvasRT);
        BuildGamesButton(canvasRT);
    }

    // ── Chargement fond jeu ───────────────────────────────────────────────────
    // Supprimé — plus de sprite de fond externe.

    // ── Canvas ────────────────────────────────────────────────────────────────

    private static RectTransform BuildCanvas()
    {
        var go  = new GameObject("MenuCanvas");
        var c   = go.AddComponent<Canvas>();
        c.renderMode   = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 0;

        var scaler                 = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight  = 1f;

        go.AddComponent<GraphicRaycaster>();
        return c.GetComponent<RectTransform>();
    }

    // ── Fond ──────────────────────────────────────────────────────────────────

    private void BuildBackground(RectTransform parent)
    {
        var go            = new GameObject("Background");
        go.transform.SetParent(parent, false);
        var img           = go.AddComponent<Image>();
        img.raycastTarget = false;
        img.sprite        = SpriteGenerator.CreateWhiteSquare();
        img.color         = new Color(0.05f, 0.05f, 0.05f, 1f);
        StretchFull(img.rectTransform);
    }

    // ── Points blancs rebondissants ───────────────────────────────────────────

    private static void BuildBouncingDots(RectTransform canvasRT)
    {
        var go = new GameObject("BouncingDots");
        go.transform.SetParent(canvasRT, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.AddComponent<MenuBouncingDots>().Init(rt);
        go.transform.SetSiblingIndex(1);
    }

    // ── HUD (Score + Horloge) ─────────────────────────────────────────────────

    private static void BuildHud(RectTransform canvasRT)
    {
        var go = new GameObject("MenuMainHud");
        go.transform.SetParent(canvasRT, false);
        var rt = go.AddComponent<RectTransform>();
        StretchFull(rt);
        go.AddComponent<MenuMainHud>().Init(rt);
    }

    // ── DoorManager (verrou + overlay intérieur porte) ────────────────────────

    private void BuildDoorManager(RectTransform canvasRT)
    {
        var go = new GameObject("DoorManagerRoot");
        go.transform.SetParent(canvasRT, false);
        var rt = go.AddComponent<RectTransform>();
        StretchFull(rt);

        var dm             = go.AddComponent<DoorManager>();
        dm.interiorSprite  = doorInteriorSprite;
        dm.buttonSprite    = doorButtonSprite;
        dm.TargetScene     = doorTargetScene;
        dm.ButtonLabel     = doorButtonLabel;
        dm.Init(rt);

        // Réévaluer automatiquement à chaque score enregistré
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreAdded += (_, __) => dm.EvaluateUnlock();
    }

    // ── Porte (centre écran) ──────────────────────────────────────────────────

    private void BuildDoor(RectTransform canvasRT)
    {
        var go = new GameObject("MenuDoorRoot");
        go.transform.SetParent(canvasRT, false);
        var rt = go.AddComponent<RectTransform>();
        StretchFull(rt);

        var door            = go.AddComponent<MenuDoor>();
        door.DoorSprite     = doorSprite;
        door.Init(rt);
    }

    // ── Bouton GAME + panneau de sélection ────────────────────────────────────

    private static void BuildGamesButton(RectTransform canvasRT)
    {
        var panel = MenuGameSelectPanel.Create(canvasRT);

        var btnGO = new GameObject("GameButton");
        btnGO.transform.SetParent(canvasRT, false);

        var img    = btnGO.AddComponent<Image>();
        img.sprite = SpriteGenerator.CreateWhiteSquare();
        img.color  = new Color(0f, 0f, 0f, 0.45f);
        img.type   = Image.Type.Simple;

        var rt             = img.rectTransform;
        rt.anchorMin       = new Vector2(0.5f, 0f);
        rt.anchorMax       = new Vector2(0.5f, 0f);
        rt.pivot           = new Vector2(0.5f, 0f);
        rt.sizeDelta       = new Vector2(380f, 110f);
        rt.anchoredPosition = new Vector2(0f, 180f);

        var lgo       = new GameObject("Label");
        lgo.transform.SetParent(rt, false);
        var tmp       = lgo.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text      = "GAME";
        tmp.fontSize  = 34f;
        tmp.fontStyle = TMPro.FontStyles.Bold;
        tmp.color     = Color.white;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        var lrt       = tmp.rectTransform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;

        var btn           = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors        = btn.colors;
        colors.highlightedColor = new Color(0.2f, 0.2f, 0.2f, 0.80f);
        colors.pressedColor     = new Color(0.4f, 0.4f, 0.4f, 1.00f);
        colors.fadeDuration     = 0.08f;
        btn.colors = colors;
        btn.onClick.AddListener(panel.Show);
    }

    // ── Utilitaires ───────────────────────────────────────────────────────────

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    private static void EnsureSceneTransition()
    {
        if (SceneTransition.Instance != null) return;
        new GameObject("SceneTransition").AddComponent<SceneTransition>();
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}

