using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Construit procéduralement le menu principal sur un Canvas Screen Space Overlay.
/// Palette noir et blanc. Aucun sprite externe requis.
/// </summary>
public class MenuSceneSetup : MonoBehaviour
{
    // ── Palette ───────────────────────────────────────────────────────────────

    private static readonly Color ColBg          = new Color(0.05f, 0.05f, 0.05f, 1f);
    private static readonly Color ColTitle       = Color.white;
    private static readonly Color ColSeparator   = new Color(1f, 1f, 1f, 0.18f);
    private static readonly Color ColBtnPlay     = Color.white;
    private static readonly Color ColBtnPlayText = new Color(0.05f, 0.05f, 0.05f, 1f);
    private static readonly Color ColBtnSecond   = new Color(0.12f, 0.12f, 0.12f, 1f);
    private static readonly Color ColBtnText     = Color.white;
    private static readonly Color ColBtnOutline  = new Color(1f, 1f, 1f, 0.22f);
    private static readonly Color ColGlowBar     = new Color(1f, 1f, 1f, 0.05f);

    // ── Configuration du fond en parallaxe ───────────────────────────────────

    [Header("Fond parallaxe")]
    [Tooltip("Sprite de fond animé en parallaxe. Laisse vide pour le fond uni procédural.")]
    [SerializeField] private Sprite backgroundSprite;

    [Tooltip("Couleur teintée sur le sprite de fond.")]
    [SerializeField] private Color backgroundTint = Color.white;

    [Tooltip("Facteur d'agrandissement du sprite pour éviter de voir les bords lors du déplacement. 1.15 est une bonne valeur de départ.")]
    [SerializeField] private float backgroundOversize = 1.15f;

    [Tooltip("Amplitude max du décalage en pixels UI (espace de référence 1080×1920).")]
    [SerializeField] private float parallaxAmplitude = 40f;

    [Tooltip("Vitesse de lissage du suivi. Entre 1 (très doux) et 15 (réactif).")]
    [SerializeField] private float parallaxSmooth = 6f;

    [Tooltip("Inverse l'axe horizontal du parallaxe.")]
    [SerializeField] private bool parallaxInvertX = false;

    [Tooltip("Inverse l'axe vertical du parallaxe.")]
    [SerializeField] private bool parallaxInvertY = false;

    // ── Configuration du personnage ───────────────────────────────────────────

    [Header("Personnage")]
    [Tooltip("Sprite du personnage affiché dans la case. Laisse vide pour une case vide (placeholder).")]
    [SerializeField] private Sprite characterSprite;

    [Tooltip("Couleur teintée sur le sprite du personnage.")]
    [SerializeField] private Color characterTint = Color.white;

    [Tooltip("Largeur de la case en pixels UI (espace de référence 1080×1920).")]
    [SerializeField] private float characterWidth = 260f;

    [Tooltip("Hauteur de la case en pixels UI.")]
    [SerializeField] private float characterHeight = 340f;

    [Tooltip("Active l'animation bobbing dès le démarrage.")]
    [SerializeField] private bool characterBobEnabled = false;

    [Tooltip("Amplitude verticale du bobbing en pixels UI.")]
    [SerializeField] private float characterBobAmplitude = 18f;

    [Tooltip("Durée d'un cycle complet du bobbing en secondes.")]
    [SerializeField] private float characterBobPeriod = 2.4f;

    // ── Références ────────────────────────────────────────────────────────────

    private Canvas              canvas;
    private RectTransform       canvasRT;
    private MenuGameSelectPanel gameSelectPanel;

    // ── Init ──────────────────────────────────────────────────────────────────

    private void Awake()
    {
        EnsureEventSystem();
        EnsureSceneTransition();
    }

    private void Start()
    {
        BuildCanvas();

        // Ordre des calques (bas → haut) : fond, UI, sélection de jeu
        var layerBg  = CreateLayer("LayerBackground");
        var layerUI  = CreateLayer("LayerUI");
        var layerSel = CreateLayer("LayerGameSelect");

        BuildBackground(layerBg);
        BuildTitle(layerUI);
        BuildCharacter(layerUI);
        BuildButtons(layerUI);

        // Le panneau de sélection est construit par-dessus, caché par défaut
        gameSelectPanel = MenuGameSelectPanel.Create(layerSel);
    }

    // ── Canvas ────────────────────────────────────────────────────────────────

    private void BuildCanvas()
    {
        var go   = new GameObject("MenuCanvas");
        canvas   = go.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        var scaler                 = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight  = 0.5f;
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

        go.AddComponent<GraphicRaycaster>();
        canvasRT = canvas.GetComponent<RectTransform>();
    }

    // ── Calques ───────────────────────────────────────────────────────────────

    private RectTransform CreateLayer(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(canvas.transform, false);
        var rt = go.AddComponent<RectTransform>();
        StretchFull(rt);
        return rt;
    }

    // ── Fond ──────────────────────────────────────────────────────────────────

    private void BuildBackground(RectTransform parent)
    {
        // Fond uni — toujours présent comme base opaque
        var baseGO            = new GameObject("Background");
        baseGO.transform.SetParent(parent, false);
        var baseImg           = baseGO.AddComponent<Image>();
        baseImg.sprite        = SpriteGenerator.CreateWhiteSquare();
        baseImg.color         = ColBg;
        baseImg.raycastTarget = false;
        StretchFull(baseImg.rectTransform);

        if (backgroundSprite == null) return;

        var go             = new GameObject("BackgroundParallax");
        go.transform.SetParent(parent, false);
        var img            = go.AddComponent<Image>();
        img.sprite         = backgroundSprite;
        img.color          = backgroundTint;
        img.preserveAspect = false;
        img.raycastTarget  = false;

        var rt               = img.rectTransform;
        rt.anchorMin         = new Vector2(0.5f, 0.5f);
        rt.anchorMax         = new Vector2(0.5f, 0.5f);
        rt.pivot             = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition  = Vector2.zero;
        rt.sizeDelta         = new Vector2(1080f * backgroundOversize, 1920f * backgroundOversize);

        var parallax         = go.AddComponent<MenuParallaxBackground>();
        parallax.amplitude   = parallaxAmplitude;
        parallax.smoothSpeed = parallaxSmooth;
        parallax.invertX     = parallaxInvertX;
        parallax.invertY     = parallaxInvertY;
    }

    // ── Titre ─────────────────────────────────────────────────────────────────

    private void BuildTitle(RectTransform parent)
    {
        var zone = MakeZone("TitleZone", parent, new Vector2(0f, 0.55f), new Vector2(1f, 0.90f));

        // Barre de glow
        var glowGO = MakeImage("GlowBar", zone);
        glowGO.GetComponent<Image>().color = ColGlowBar;
        var glowRT = glowGO.GetComponent<RectTransform>();
        glowRT.anchorMin = new Vector2(0f, 0.40f);
        glowRT.anchorMax = new Vector2(1f, 0.74f);
        glowRT.offsetMin = glowRT.offsetMax = Vector2.zero;

        // Titre principal
        var titleGO = MakeText("TitleText", zone, "RÉSONNANCE", 96f, FontStyles.Bold, ColTitle,
                               new Vector2(0f, 0.30f), new Vector2(1f, 1f));
        titleGO.AddComponent<TitleGlitch>();

        // Séparateur
        var sep = MakeImage("Separator", parent);
        sep.GetComponent<Image>().color = ColSeparator;
        var sepRT = sep.GetComponent<RectTransform>();
        sepRT.anchorMin        = new Vector2(0.12f, 0.53f);
        sepRT.anchorMax        = new Vector2(0.88f, 0.53f);
        sepRT.sizeDelta        = new Vector2(0f, 2f);
        sepRT.anchoredPosition = Vector2.zero;
    }

    // ── Personnage ────────────────────────────────────────────────────────────

    private void BuildCharacter(RectTransform parent)
    {
        var go  = new GameObject("CharacterSlot");
        go.transform.SetParent(parent, false);

        var rt              = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(characterWidth, characterHeight);
        rt.anchoredPosition = new Vector2(220f, 80f);

        var img            = go.AddComponent<Image>();
        img.raycastTarget  = false;
        img.preserveAspect = true;

        if (characterSprite != null)
        {
            img.sprite = characterSprite;
            img.color  = characterTint;
        }
        else
        {
            img.sprite = SpriteGenerator.CreateWhiteSquare();
            img.color  = new Color(1f, 1f, 1f, 0.08f);
            BuildCharacterPlaceholder(rt);
        }

        var slot         = go.AddComponent<MenuCharacterSlot>();
        slot.amplitude   = characterBobAmplitude;
        slot.period      = characterBobPeriod;
        slot.enabled     = characterBobEnabled;
    }

    private static void BuildCharacterPlaceholder(RectTransform parent)
    {
        var h          = new GameObject("PH_H");
        h.transform.SetParent(parent, false);
        var hi         = h.AddComponent<Image>();
        hi.sprite      = SpriteGenerator.CreateWhiteSquare();
        hi.color       = new Color(1f, 1f, 1f, 0.25f);
        hi.raycastTarget = false;
        var hrt        = hi.rectTransform;
        hrt.anchorMin  = new Vector2(0.25f, 0.5f);
        hrt.anchorMax  = new Vector2(0.75f, 0.5f);
        hrt.sizeDelta  = new Vector2(0f, 4f);

        var v          = new GameObject("PH_V");
        v.transform.SetParent(parent, false);
        var vi         = v.AddComponent<Image>();
        vi.sprite      = SpriteGenerator.CreateWhiteSquare();
        vi.color       = new Color(1f, 1f, 1f, 0.25f);
        vi.raycastTarget = false;
        var vrt        = vi.rectTransform;
        vrt.anchorMin  = new Vector2(0.5f, 0.25f);
        vrt.anchorMax  = new Vector2(0.5f, 0.75f);
        vrt.sizeDelta  = new Vector2(4f, 0f);
    }

    // ── Boutons ───────────────────────────────────────────────────────────────

    private void BuildButtons(RectTransform parent)
    {
        var zone       = MakeZone("ButtonsZone", parent, new Vector2(0.5f, 0.12f), new Vector2(0.5f, 0.50f));
        zone.sizeDelta = new Vector2(640f, 0f);

        // JOUER — bouton principal unique
        var play = BuildButton("PlayButton", "JOUER", zone,
                               new Vector2(0f, 0.55f), new Vector2(1f, 1f),
                               ColBtnPlay, ColBtnPlayText, 72f, isAccent: true);
        play.onClick.AddListener(OnPlay);

        // QUIT — bouton secondaire
        var quit = BuildButton("QuitButton", "QUITTER", zone,
                               new Vector2(0f, 0f), new Vector2(1f, 0.44f),
                               ColBtnSecond, ColBtnText, 52f);
        quit.onClick.AddListener(OnQuit);
    }

    private Button BuildButton(string goName, string label, RectTransform parent,
                               Vector2 anchorMin, Vector2 anchorMax,
                               Color bgColor, Color textColor, float fontSize,
                               bool isAccent = false)
    {
        var go      = new GameObject(goName);
        go.transform.SetParent(parent, false);

        var img         = go.AddComponent<Image>();
        img.sprite      = SpriteGenerator.CreateWhiteSquare();
        img.color       = bgColor;
        var rt          = img.rectTransform;
        rt.anchorMin    = anchorMin;
        rt.anchorMax    = anchorMax;
        rt.offsetMin    = rt.offsetMax = Vector2.zero;

        if (!isAccent)
        {
            var outGO       = MakeImage("Outline", rt);
            outGO.GetComponent<Image>().color = ColBtnOutline;
            var outRT       = outGO.GetComponent<RectTransform>();
            outRT.anchorMin = Vector2.zero;
            outRT.anchorMax = Vector2.one;
            outRT.offsetMin = new Vector2(-1.5f, -1.5f);
            outRT.offsetMax = new Vector2( 1.5f,  1.5f);
            outGO.transform.SetAsFirstSibling();
        }

        MakeText("Label", rt, label, fontSize, FontStyles.Bold, textColor,
                 Vector2.zero, Vector2.one, raycast: false);

        var btn           = go.AddComponent<Button>();
        btn.targetGraphic = img;
        return btn;
    }

    // ── Actions ───────────────────────────────────────────────────────────────

    private void OnPlay()  => gameSelectPanel?.Show();

    private void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static GameObject MakeImage(string name, RectTransform parent)
    {
        var go             = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img            = go.AddComponent<Image>();
        img.sprite         = SpriteGenerator.CreateWhiteSquare();
        img.raycastTarget  = false;
        return go;
    }

    private static GameObject MakeText(string name, RectTransform parent,
                                       string text, float fontSize, FontStyles style,
                                       Color color, Vector2 anchorMin, Vector2 anchorMax,
                                       float characterSpacing = 0f, bool raycast = false)
    {
        var go               = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp              = go.AddComponent<TextMeshProUGUI>();
        tmp.text             = text;
        tmp.fontSize         = fontSize;
        tmp.fontStyle        = style;
        tmp.color            = color;
        tmp.alignment        = TextAlignmentOptions.Center;
        tmp.characterSpacing = characterSpacing;
        tmp.raycastTarget    = raycast;
        var rt               = tmp.rectTransform;
        rt.anchorMin         = anchorMin;
        rt.anchorMax         = anchorMax;
        rt.offsetMin         = rt.offsetMax = Vector2.zero;
        return go;
    }

    private static RectTransform MakeZone(string name, RectTransform parent,
                                          Vector2 anchorMin, Vector2 anchorMax)
    {
        var go       = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt       = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // ── EventSystem / SceneTransition ─────────────────────────────────────────

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        new GameObject("EventSystem").AddComponent<EventSystem>()
                                     .gameObject.AddComponent<StandaloneInputModule>();
    }

    private void EnsureSceneTransition()
    {
        if (SceneTransition.Instance != null) return;
        new GameObject("SceneTransition").AddComponent<SceneTransition>();
    }
}




