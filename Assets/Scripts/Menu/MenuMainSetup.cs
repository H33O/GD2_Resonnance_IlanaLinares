using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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

    // ── Palette fond placeholder ───────────────────────────────────────────────

    private static readonly Color ColBgTop    = Color.black;
    private static readonly Color ColBgCenter = Color.black;

    // ── Palette bouton GAMES ──────────────────────────────────────────────────

    private static readonly Color ColGamesBtnBg  = new Color(1f, 1f, 1f, 0.08f);
    private static readonly Color ColGamesBtnTxt = Color.white;

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Fond 2D")]
    [Tooltip("Sprite de fond custom. Laisse vide pour le placeholder coloré procédural.")]
    [SerializeField] public Sprite backgroundSprite;

    [Tooltip("Teinte appliquée sur le sprite de fond.")]
    [SerializeField] public Color backgroundTint = Color.white;

    [Header("Porte — Intérieur")]
    [Tooltip("Image de fond de l'overlay intérieur porte (fond_interieur porte.png).")]
    [SerializeField] public Sprite doorInteriorSprite;

    [Tooltip("Sprite du bouton UI de l'overlay intérieur porte (bouton UI.png).")]
    [SerializeField] public Sprite doorButtonSprite;

    [Tooltip("Nom de la scène chargée depuis l'overlay intérieur porte.")]
    [SerializeField] public string doorTargetScene = "CircleArena";

    [Tooltip("Libellé affiché sur le bouton de l'overlay intérieur porte.")]
    [SerializeField] public string doorButtonLabel = "JOUER";

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake() => EnsureEventSystem();

    private void Start()
    {
        ScoreManager.EnsureExists();
        PlayerLevelManager.EnsureExists();
        QuestManager.EnsureExists();

        // Initialiser les assets partagés (font JimNightshade + sprite bouton)
        MenuAssets.Init(doorButtonSprite);

        BuildBackground2D();
        var canvasRT = BuildCanvas();
        BuildHud(canvasRT);
        BuildDoorManager(canvasRT);
        BuildDoor(canvasRT);
        BuildGamesButton(canvasRT);
        BuildQuestsButton(canvasRT);
        QuestCompletionOverlay.Create(canvasRT);
        MenuCoinReceiver.Create(canvasRT);
        EnsureSceneTransition();

        // Brancher l'overlay "Quête validée" sur le QuestManager
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestCompleted += QuestCompletionOverlay.Show;

        // Brancher le feedback "Niveau +1" sur le PlayerLevelManager
        if (PlayerLevelManager.Instance != null)
            PlayerLevelManager.Instance.OnLevelUp += lvl => LevelUpToast.Show(canvasRT, lvl);
    }

    // ── Fond 2D (SpriteRenderer) ──────────────────────────────────────────────

    private void BuildBackground2D()
    {
        var cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic    = true;
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = ColBgTop;
        }

        var go = new GameObject("Background2D");
        var sr = go.AddComponent<SpriteRenderer>();

        if (backgroundSprite != null)
        {
            sr.sprite = backgroundSprite;
            sr.color  = backgroundTint;
        }
        else
        {
            // Placeholder : rectangle teinté dégradé simulé avec deux plans superposés
            sr.sprite        = SpriteGenerator.CreateWhiteSquare();
            sr.color         = ColBgCenter;
            sr.sortingOrder  = -10;
            go.transform.localScale = new Vector3(20f, 36f, 1f);
        }
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

        // Écouter les nouveaux scores pour débloquer automatiquement
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreAdded += (_, __) => dm.EvaluateUnlock();
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
        // Panneau (créé hors écran, à droite)
        var panel = MenuGameSelectPanel.Create(canvasRT);

        // Bouton "GAMES" — bas-droite, au-dessus du bas du canvas
        var btnGO = new GameObject("GamesButton");
        btnGO.transform.SetParent(canvasRT, false);

        var img      = btnGO.AddComponent<Image>();
        img.sprite   = SpriteGenerator.CreateWhiteSquare();
        img.color    = ColGamesBtnBg;

        var rt       = img.rectTransform;
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot     = new Vector2(1f, 0f);
        rt.sizeDelta = new Vector2(220f, 80f);
        // Positionné plus bas : même zone que le bas de la porte
        rt.anchoredPosition = new Vector2(-32f, 200f);

        var labelGO       = new GameObject("Label");
        labelGO.transform.SetParent(rt, false);
        var tmp           = labelGO.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text          = "GAMES";
        tmp.fontSize      = 34f;
        tmp.fontStyle     = TMPro.FontStyles.Bold;
        tmp.color         = ColGamesBtnTxt;
        tmp.alignment     = TMPro.TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        var lrt           = tmp.rectTransform;
        lrt.anchorMin     = Vector2.zero;
        lrt.anchorMax     = Vector2.one;
        lrt.offsetMin     = lrt.offsetMax = Vector2.zero;

        var btn           = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;
        bool gamesOpen = false;
        btn.onClick.AddListener(() =>
        {
            gamesOpen = !gamesOpen;
            if (gamesOpen) panel.Show(); else panel.Hide();
        });
    }

    // ── Bouton QUÊTES + panneau QuestPanel ────────────────────────────────────

    private static void BuildQuestsButton(RectTransform canvasRT)
    {
        // Panneau quêtes (hors écran, à droite)
        var questPanel = MenuQuestPanel.Create(canvasRT);

        // Bouton "QUÊTES" — bas-gauche
        var btnGO = new GameObject("QuestsButton");
        btnGO.transform.SetParent(canvasRT, false);

        var img      = btnGO.AddComponent<Image>();
        img.sprite   = SpriteGenerator.CreateWhiteSquare();
        img.color    = ColGamesBtnBg;

        var rt       = img.rectTransform;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot     = new Vector2(0f, 0f);
        rt.sizeDelta = new Vector2(220f, 80f);
        rt.anchoredPosition = new Vector2(32f, 200f);

        var labelGO       = new GameObject("Label");
        labelGO.transform.SetParent(rt, false);
        var tmp           = labelGO.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text          = "QUÊTES";
        tmp.fontSize      = 34f;
        tmp.fontStyle     = TMPro.FontStyles.Bold;
        tmp.color         = ColGamesBtnTxt;
        tmp.alignment     = TMPro.TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        var lrt           = tmp.rectTransform;
        lrt.anchorMin     = Vector2.zero;
        lrt.anchorMax     = Vector2.one;
        lrt.offsetMin     = lrt.offsetMax = Vector2.zero;

        bool open = false;
        var btn   = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() =>
        {
            open = !open;
            if (open) questPanel.Show(); else questPanel.Hide();
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
