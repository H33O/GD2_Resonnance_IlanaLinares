using System.Collections;
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
    private static readonly Color ColGrid        = new Color(1f, 1f, 1f, 0.04f);
    private static readonly Color ColTitle       = Color.white;
    private static readonly Color ColSubtitle    = new Color(1f, 1f, 1f, 0.40f);
    private static readonly Color ColSeparator   = new Color(1f, 1f, 1f, 0.18f);
    private static readonly Color ColBtnPlay     = Color.white;
    private static readonly Color ColBtnPlayText = new Color(0.05f, 0.05f, 0.05f, 1f);
    private static readonly Color ColBtnSecond   = new Color(0.12f, 0.12f, 0.12f, 1f);
    private static readonly Color ColBtnText     = Color.white;
    private static readonly Color ColBtnOutline  = new Color(1f, 1f, 1f, 0.22f);
    private static readonly Color ColGlowBar     = new Color(1f, 1f, 1f, 0.05f);

    // ── Références ────────────────────────────────────────────────────────────

    private Canvas        canvas;
    private RectTransform canvasRT;
    private MenuBall      ball;
    private RippleEffect  ripple;

    // ── Init ──────────────────────────────────────────────────────────────────

    private void Awake()
    {
        EnsureEventSystem();
        EnsureSceneTransition();
    }

    private void Start()
    {
        BuildCanvas();

        // Ordre des calques (bas → haut) : fond, grille, effets, balle, UI
        var layerBg      = CreateLayer("LayerBackground");
        var layerGrid    = CreateLayer("LayerGrid");
        var layerEffects = CreateLayer("LayerEffects");
        var layerBall    = CreateLayer("LayerBall");
        var layerUI      = CreateLayer("LayerUI");

        BuildBackground(layerBg);
        BuildGrid(layerGrid);
        BuildRippleSystem(layerEffects);
        BuildTrailSystem(layerEffects);
        BuildBall(layerBall);
        BuildTitle(layerUI);
        BuildButtons(layerUI);

        // Masque l'UI pendant l'intro, puis la révèle via fondu
        var uiGroup = layerUI.gameObject.AddComponent<CanvasGroup>();
        uiGroup.alpha = 0f;
        uiGroup.blocksRaycasts = false;

        // Lance l'intro cinématique par-dessus le canvas du menu
        var introCanvas = BuildIntroCanvas();
        var intro       = gameObject.AddComponent<IntroTransition>();
        intro.Play(introCanvas, () => StartCoroutine(RevealMenu(uiGroup)));
    }

    // ── Canvas d'intro (par-dessus le menu) ──────────────────────────────────

    private Canvas BuildIntroCanvas()
    {
        var go    = new GameObject("IntroCanvas");
        var c     = go.AddComponent<Canvas>();
        c.renderMode   = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 10;

        var scaler                 = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight  = 0.5f;
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

        go.AddComponent<GraphicRaycaster>();
        return c;
    }

    // ── Révélation du menu après l'intro ──────────────────────────────────────

    private IEnumerator RevealMenu(CanvasGroup uiGroup)
    {
        uiGroup.blocksRaycasts = true;
        float elapsed = 0f, duration = 0.6f;
        while (elapsed < duration)
        {
            elapsed   += Time.deltaTime;
            uiGroup.alpha = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            yield return null;
        }
        uiGroup.alpha = 1f;
    }

    // ── Canvas ────────────────────────────────────────────────────────────────

    private void BuildCanvas()
    {
        var go   = new GameObject("MenuCanvas");
        canvas   = go.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
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
        var go       = new GameObject(name);
        go.transform.SetParent(canvas.transform, false);
        var rt       = go.AddComponent<RectTransform>();
        StretchFull(rt);
        return rt;
    }

    // ── Fond ──────────────────────────────────────────────────────────────────

    private void BuildBackground(RectTransform parent)
    {
        var go          = new GameObject("Background");
        go.transform.SetParent(parent, false);
        var img         = go.AddComponent<Image>();
        img.sprite      = SpriteGenerator.CreateWhiteSquare();
        img.color       = ColBg;
        img.raycastTarget = false;
        StretchFull(img.rectTransform);
    }

    // ── Grille ────────────────────────────────────────────────────────────────

    private void BuildGrid(RectTransform parent)
    {
        for (int i = 1; i <= 5; i++) MakeLine(parent, true,  i / 6f);
        for (int i = 1; i <= 9; i++) MakeLine(parent, false, i / 10f);
    }

    private void MakeLine(RectTransform parent, bool vertical, float t)
    {
        var go  = MakeImage($"Line{(vertical ? 'V' : 'H')}_{t:F2}", parent);
        var img = go.GetComponent<Image>();
        img.color = ColGrid;
        var rt    = img.rectTransform;

        if (vertical)
        {
            rt.anchorMin = new Vector2(t, 0f);
            rt.anchorMax = new Vector2(t, 1f);
            rt.sizeDelta = new Vector2(1f, 0f);
        }
        else
        {
            rt.anchorMin = new Vector2(0f, t);
            rt.anchorMax = new Vector2(1f, t);
            rt.sizeDelta = new Vector2(0f, 1f);
        }
        rt.anchoredPosition = Vector2.zero;
    }

    // ── Ripple ────────────────────────────────────────────────────────────────

    private void BuildRippleSystem(RectTransform parent)
    {
        var go = new GameObject("RippleSystem");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        StretchFull(rt);
        ripple = go.AddComponent<RippleEffect>();
        ripple.SetContainer(rt);
    }

    // ── Trail ─────────────────────────────────────────────────────────────────

    private void BuildTrailSystem(RectTransform parent)
    {
        var go = new GameObject("TrailSystem");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        StretchFull(rt);
        go.AddComponent<BallTrail>().SetContainer(rt);
    }

    // ── Balle ─────────────────────────────────────────────────────────────────

    private void BuildBall(RectTransform parent)
    {
        var go          = new GameObject("Ball");
        go.transform.SetParent(parent, false);

        var rt          = go.AddComponent<RectTransform>();
        rt.anchorMin    = new Vector2(0.5f, 0.5f);
        rt.anchorMax    = new Vector2(0.5f, 0.5f);
        rt.pivot        = new Vector2(0.5f, 0.5f);
        rt.sizeDelta    = new Vector2(44f, 44f);
        rt.anchoredPosition = new Vector2(-150f, 100f);

        ball = go.AddComponent<MenuBall>();
        ball.SetCanvas(canvas);

        canvas.GetComponentInChildren<BallTrail>()?.SetBall(ball);
        ball.OnBounce += (pos, isBtn) => ripple?.SpawnRipple(pos, isBtn);
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

        // Titre principal (occupe toute la zone, plus de sous-titre)
        var titleGO = MakeText("TitleText", zone, "RÉSONNANCE", 96f, FontStyles.Bold, ColTitle,
                               new Vector2(0f, 0.30f), new Vector2(1f, 1f));
        titleGO.AddComponent<TitleGlitch>();

        // Séparateur
        var sep = MakeImage("Separator", parent);
        sep.GetComponent<Image>().color = ColSeparator;
        var sepRT = sep.GetComponent<RectTransform>();
        sepRT.anchorMin     = new Vector2(0.12f, 0.53f);
        sepRT.anchorMax     = new Vector2(0.88f, 0.53f);
        sepRT.sizeDelta     = new Vector2(0f, 2f);
        sepRT.anchoredPosition = Vector2.zero;
    }

    // ── Boutons ───────────────────────────────────────────────────────────────

    private void BuildButtons(RectTransform parent)
    {
        var zone = MakeZone("ButtonsZone", parent, new Vector2(0.5f, 0.08f), new Vector2(0.5f, 0.51f));
        zone.sizeDelta = new Vector2(640f, 0f);

        // SONANTIA — bouton principal accent → Game and Watch
        var sonantia = BuildButton("SonantiaButton", "SONANTIA", zone,
                                   new Vector2(0f, 0.80f), new Vector2(1f, 1f),
                                   ColBtnPlay, ColBtnPlayText, 64f, isAccent: true);

        // ÉCHO — bouton secondaire
        var echo  = BuildButton("EchoButton",   "ÉCHO",    zone,
                                new Vector2(0f, 0.59f), new Vector2(1f, 0.74f),
                                ColBtnSecond, ColBtnText, 46f);

        // ARÈNE — bouton secondaire
        var arena = BuildButton("ArenaButton",  "ARÈNE",   zone,
                                new Vector2(0f, 0.38f), new Vector2(1f, 0.53f),
                                ColBtnSecond, ColBtnText, 46f);

        // SLASH — bouton secondaire
        var slash = BuildButton("SlashButton",  "SLASH",   zone,
                                new Vector2(0f, 0.17f), new Vector2(1f, 0.32f),
                                ColBtnSecond, ColBtnText, 46f);

        // QUIT — bouton secondaire
        var quit  = BuildButton("QuitButton",   "QUIT",    zone,
                                new Vector2(0f, 0f),    new Vector2(1f, 0.11f),
                                ColBtnSecond, ColBtnText, 46f);

        ball.RegisterButton(sonantia);
        ball.RegisterButton(echo);
        ball.RegisterButton(arena);
        ball.RegisterButton(slash);
        ball.RegisterButton(quit);

        sonantia.OnClick += OnSonantia;
        echo.OnClick     += OnEcho;
        arena.OnClick    += OnArena;
        slash.OnClick    += OnSlash;
        quit.OnClick     += OnQuit;
    }

    private MenuCanvasButton BuildButton(string goName, string label, RectTransform parent,
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

        // Contour pour les boutons secondaires
        if (!isAccent)
        {
            var outGO  = MakeImage("Outline", rt);
            outGO.GetComponent<Image>().color = ColBtnOutline;
            var outRT  = outGO.GetComponent<RectTransform>();
            outRT.anchorMin = Vector2.zero;
            outRT.anchorMax = Vector2.one;
            outRT.offsetMin = new Vector2(-1.5f, -1.5f);
            outRT.offsetMax = new Vector2( 1.5f,  1.5f);
            outGO.transform.SetAsFirstSibling();
        }

        // Label
        MakeText("Label", rt, label, fontSize, FontStyles.Bold, textColor,
                 Vector2.zero, Vector2.one, raycast: false);

        var btn           = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var mcb = go.AddComponent<MenuCanvasButton>();
        mcb.Init(rt, img, isAccent);
        btn.onClick.AddListener(() => mcb.TriggerPress());

        return mcb;
    }

    // ── Actions ───────────────────────────────────────────────────────────────

    /// <summary>Lance le premier jeu "Sonantia" (Game and Watch).</summary>
    private void OnSonantia()
    {
        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadScene("GameAndWatch", "SONANTIA");
        else
            SceneManager.LoadScene("GameAndWatch");
    }

    private void OnArena()
    {
        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadScene("CircleArena", "ARÈNE");
        else
            SceneManager.LoadScene("CircleArena");
    }

    private void OnSlash()
    {
        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadScene("SlashGame", "SLASH");
        else
            SceneManager.LoadScene("SlashGame");
    }

    private void OnEcho()
    {
        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadScene("Minijeu-Bulles", "ÉCHO");
        else
            SceneManager.LoadScene("Minijeu-Bulles");
    }

    private void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ── Helpers de construction ───────────────────────────────────────────────

    private static GameObject MakeImage(string name, RectTransform parent)
    {
        var go  = new GameObject(name);
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
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);

        var tmp              = go.AddComponent<TextMeshProUGUI>();
        tmp.text             = text;
        tmp.fontSize         = fontSize;
        tmp.fontStyle        = style;
        tmp.color            = color;
        tmp.alignment        = TextAlignmentOptions.Center;
        tmp.characterSpacing = characterSpacing;
        tmp.raycastTarget    = raycast;

        var rt       = tmp.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

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