using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Bootstrap de la scène <c>MenuScene</c>.
///
/// Construit procéduralement :
///   1. Fond 2D (SpriteRenderer, placeholder coloré — prêt à recevoir un sprite custom)
///   2. Canvas Screen Space Overlay avec CanvasScaler 1080×1920
///      - HUD haut-gauche  : Score   (<see cref="MenuMainHud"/>)
///      - HUD haut-droite  : Horloge (<see cref="MenuMainHud"/>)
///      - Bas-centre       : Porte   (<see cref="MenuDoor"/>)
///      - Bas-droite       : Bouton GAMES → panneau <see cref="MenuGameSelectPanel"/>
///   3. EventSystem (si absent)
/// </summary>
public class MenuMainSetup : MonoBehaviour
{
    // ── Constantes ────────────────────────────────────────────────────────────

    /// <summary>Nom de cette scène dans les Build Settings.</summary>
    public const string SceneName = "Menu";

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Porte — Intérieur")]
    [Tooltip("Image de fond de l'overlay intérieur porte (fond_interieur porte.png).")]
    [SerializeField] public Sprite doorInteriorSprite;

    [Tooltip("Sprite du bouton UI de l'overlay intérieur porte (bouton UI.png).")]
    [SerializeField] public Sprite doorButtonSprite;

    [Tooltip("Nom de la scène chargée depuis l'overlay intérieur porte.")]
    [SerializeField] public string doorTargetScene = "ParryGame";

    [Tooltip("Libellé affiché sur le bouton de l'overlay intérieur porte.")]
    [SerializeField] public string doorButtonLabel = "JOUER";

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake() => EnsureEventSystem();

    private void Start()
    {
        // ── Ordre critique : Score → Level ────────────────────────────────────
        ScoreManager.EnsureExists();
        PlayerLevelManager.EnsureExists();

        MenuAssets.Init(doorButtonSprite);

        BuildBackground2D();
        var canvasRT = BuildCanvas();
        BuildHud(canvasRT);
        BuildDoorManager(canvasRT);
        BuildDoor(canvasRT);
        BuildGamesButton(canvasRT);
        MenuXPBar.Create(canvasRT);           // Barre XP avec boules bleues et pulse
        MenuXPReceiver.Create(canvasRT);
        EnsureSceneTransition();

        // Feedback "Niveau +1"
        if (PlayerLevelManager.Instance != null)
            PlayerLevelManager.Instance.OnLevelUp += lvl => LevelUpToast.Show(canvasRT, lvl);
    }

    // ── Fond 2D (quadrillage + balle rebondissante) ────────────────────────────

    private void BuildBackground2D()
    {
        var cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic    = true;
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.white;
        }

        BuildGridBackground();
        BuildBouncingBall();
    }

    private static void BuildGridBackground()
    {
        var go = new GameObject("GridBackground");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = CreateGridSprite();
        sr.sortingOrder = -10;
        go.transform.localScale = new Vector3(20f, 36f, 1f);
    }

    /// <summary>Génère un sprite de quadrillage blanc sur fond blanc.</summary>
    private static Sprite CreateGridSprite()
    {
        const int size      = 256;
        const int cellSize  = 32;
        const int lineWidth = 1;

        var tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode   = TextureWrapMode.Repeat;
        var pixels = new Color[size * size];

        var bgColor   = Color.white;
        var lineColor = new Color(0.82f, 0.82f, 0.82f, 1f);

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            bool onLine = (x % cellSize) < lineWidth || (y % cellSize) < lineWidth;
            pixels[y * size + x] = onLine ? lineColor : bgColor;
        }

        tex.SetPixels(pixels);
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static void BuildBouncingBall()
    {
        var go = new GameObject("MenuBouncingBall");
        go.AddComponent<MenuBouncingBall>();
    }

    // ── Canvas ────────────────────────────────────────────────────────────────

    private static RectTransform BuildCanvas()
    {
        var go   = new GameObject("MenuMainCanvas");
        var c    = go.AddComponent<Canvas>();
        c.renderMode   = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 0;

        var scaler                 = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight  = 1f;

        go.AddComponent<GraphicRaycaster>();
        return c.GetComponent<RectTransform>();
    }

    // ── HUD (Score + Horloge) ─────────────────────────────────────────────────

    private static void BuildHud(RectTransform canvasRT)
    {
        var go  = new GameObject("MenuMainHud");
        go.transform.SetParent(canvasRT, false);

        var rt  = go.AddComponent<RectTransform>();
        StretchFull(rt);

        var hud = go.AddComponent<MenuMainHud>();
        hud.Init(rt);
    }

    // ── DoorManager (verrou + overlay intérieur porte) ────────────────────────

    private void BuildDoorManager(RectTransform canvasRT)
    {
        var go      = new GameObject("DoorManagerRoot");
        go.transform.SetParent(canvasRT, false);

        var rt      = go.AddComponent<RectTransform>();
        StretchFull(rt);

        var dm             = go.AddComponent<DoorManager>();
        dm.interiorSprite  = doorInteriorSprite;
        dm.buttonSprite    = doorButtonSprite;
        dm.TargetScene     = doorTargetScene;
        dm.ButtonLabel     = doorButtonLabel;
        dm.Init(rt);
        // L'abonnement au PlayerLevelManager est géré dans DoorManager.Start()
    }

    // ── Porte (bas-centre) ────────────────────────────────────────────────────

    private static void BuildDoor(RectTransform canvasRT)
    {
        var go  = new GameObject("MenuDoorRoot");
        go.transform.SetParent(canvasRT, false);

        var rt  = go.AddComponent<RectTransform>();
        StretchFull(rt);

        var door = go.AddComponent<MenuDoor>();
        door.Init(rt);
    }

    // ── Bouton GAMES + panneau GameSelectPanel ────────────────────────────────

    private static void BuildGamesButton(RectTransform canvasRT)
    {
        var panel = MenuGameSelectPanel.Create(canvasRT);

        var btnGO = new GameObject("GamesButton");
        btnGO.transform.SetParent(canvasRT, false);

        // Fond : cadre noir faible opacité, pas de sprite bouton UI
        var img    = btnGO.AddComponent<Image>();
        img.sprite = SpriteGenerator.CreateWhiteSquare();
        img.color  = new Color(0f, 0f, 0f, 0.45f);

        var rt       = img.rectTransform;
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot     = new Vector2(1f, 0f);
        rt.sizeDelta = new Vector2(220f, 80f);
        rt.anchoredPosition = new Vector2(-32f, 200f);

        // Bordure fine
        var frameGO  = new GameObject("Frame");
        frameGO.transform.SetParent(rt, false);
        var frameImg = frameGO.AddComponent<Image>();
        frameImg.sprite       = SpriteGenerator.CreateWhiteSquare();
        frameImg.color        = new Color(1f, 1f, 1f, 0.14f);
        frameImg.raycastTarget = false;
        var frameRT  = frameImg.rectTransform;
        frameRT.anchorMin = Vector2.zero;
        frameRT.anchorMax = Vector2.one;
        frameRT.offsetMin = new Vector2(-1f, -1f);
        frameRT.offsetMax = new Vector2( 1f,  1f);
        frameGO.transform.SetAsFirstSibling();

        // Label Michroma
        var labelGO       = new GameObject("Label");
        labelGO.transform.SetParent(rt, false);
        var tmp           = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text          = "GAMES";
        tmp.fontSize      = 34f;
        tmp.fontStyle     = FontStyles.Bold;
        tmp.color         = Color.white;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        MenuAssets.ApplyFont(tmp);
        var lrt      = tmp.rectTransform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;

        var btn           = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors        = btn.colors;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.10f);
        colors.pressedColor     = new Color(1f, 1f, 1f, 0.18f);
        colors.fadeDuration     = 0.08f;
        btn.colors        = colors;

        bool gamesOpen = false;
        btn.onClick.AddListener(() =>
        {
            gamesOpen = !gamesOpen;
            if (gamesOpen) panel.Show(); else panel.Hide();
        });
    }

    // ── EventSystem ───────────────────────────────────────────────────────────

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    // ── SceneTransition ───────────────────────────────────────────────────────

    private static void EnsureSceneTransition()
    {
        if (SceneTransition.Instance != null) return;
        new GameObject("SceneTransition").AddComponent<SceneTransition>();
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
