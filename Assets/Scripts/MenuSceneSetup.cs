using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

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

    [Header("Fond")]
    [Tooltip("Image de fond du menu (ex : foret.png). Si null, fond noir pur.")]
    [SerializeField] public Sprite backgroundSprite;

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

    [Header("Typographie & Icônes")]
    [Tooltip("Police Michroma SDF (Michroma-Regular SDF.asset).")]
    [SerializeField] public TMP_FontAsset michromatFont;

    [Tooltip("Sprite cadenas affiché sur la porte verrouillée (cadena.png).")]
    [SerializeField] public Sprite lockSprite;

    [Tooltip("Sprite de fond affiché derrière tous les textes (jaugenormal.png).")]
    [SerializeField] public Sprite textBadgeSprite;

    [Header("Jauges — Sprites")]
    [Tooltip("Sprite de la jauge Eau (jaugesoif.png). (Réservé pour usage futur.)")]
    [SerializeField] public Sprite gaugeWaterSprite;

    [Tooltip("Sprite de la jauge Nourriture (jaugemanger.png). (Réservé pour usage futur.)")]
    [SerializeField] public Sprite gaugeFoodSprite;

    [Tooltip("Sprite de la jauge Sommeil (jaugesommeil.png). (Réservé pour usage futur.)")]
    [SerializeField] public Sprite gaugeSleepSprite;

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

        // Charger le fond jeu automatiquement si aucun sprite n'est assigné
        if (backgroundSprite == null)
            backgroundSprite = LoadFondJeu();

        // Assets partagés pour tous les builders — JimNightshade prioritaire via MenuAssets.Init
        MenuAssets.Init(buttonSprite, null, lockSprite);

        var canvasRT = BuildCanvas();

        BuildBackground(canvasRT);
        BuildFireflies(canvasRT);
        BuildHud(canvasRT);
        BuildDoorManager(canvasRT);   // DoorManager avant MenuDoor
        BuildDoor(canvasRT);
        BuildGamesButton(canvasRT);
    }

    // ── Chargement fond jeu ───────────────────────────────────────────────────

    private static Sprite LoadFondJeu()
    {
#if UNITY_EDITOR
        var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/sprites/fond jeu.png");
        if (tex != null)
        {
            var objs = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(
                "Assets/sprites/fond jeu.png");
            foreach (var obj in objs)
                if (obj is Sprite sp) return sp;

            return Sprite.Create(tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), 100f);
        }
#endif
        return Resources.Load<Sprite>("fond jeu");
    }

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

    // ── Lucioles ──────────────────────────────────────────────────────────────

    /// <summary>Crée le système de lucioles UI par-dessus le fond, sous le HUD.</summary>
    private static void BuildFireflies(RectTransform canvasRT)
    {
        var go = new GameObject("Fireflies");
        go.transform.SetParent(canvasRT, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        go.AddComponent<MenuFireflies>().Init(rt);

        // Sibling index 1 : juste au-dessus du Background, sous tout le reste
        go.transform.SetSiblingIndex(1);
    }

    // ── Fond ──────────────────────────────────────────────────────────────────

    private void BuildBackground(RectTransform parent)
    {
        var go            = new GameObject("Background");
        go.transform.SetParent(parent, false);
        var img           = go.AddComponent<Image>();
        img.raycastTarget = false;
        StretchFull(img.rectTransform);

        if (backgroundSprite != null)
        {
            img.sprite           = backgroundSprite;
            img.color            = Color.white;
            img.preserveAspect   = false;   // plein écran, on étire volontairement
            img.type             = Image.Type.Simple;
        }
        else
        {
            img.sprite = SpriteGenerator.CreateWhiteSquare();
            img.color  = new Color(0.05f, 0.04f, 0.07f, 1f);
        }
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
        img.color  = new Color(0f, 0f, 0f, 0.55f);
        MenuAssets.ApplyButtonSprite(img);

        var rt             = img.rectTransform;
        rt.anchorMin       = new Vector2(0.5f, 0f);
        rt.anchorMax       = new Vector2(0.5f, 0f);
        rt.pivot           = new Vector2(0.5f, 0f);
        rt.sizeDelta       = new Vector2(380f, 110f);
        rt.anchoredPosition = new Vector2(0f, 28f);

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

