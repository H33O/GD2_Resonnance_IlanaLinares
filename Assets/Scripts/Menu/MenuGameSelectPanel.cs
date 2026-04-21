using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Panneau de sélection de jeu qui slide depuis la droite (EaseOutQuart, 0.4s).
/// Contient 3 boutons de jeu avec transition SceneTransition + 1 bouton Retour.
/// </summary>
public class MenuGameSelectPanel : MonoBehaviour
{
    // ── Scènes cibles ─────────────────────────────────────────────────────────

    public const string SceneGameAndWatch  = "GameAndWatch";
    public const string SceneBubbleShooter = "Minijeu-Bulles";
    public const string SceneBallGoal      = "TiltBall";

    // ── Timings ───────────────────────────────────────────────────────────────

    private const float SlideDuration   = 0.40f;
    private const float CanvasRefWidth  = 1080f;

    // ── Palette ───────────────────────────────────────────────────────────────

    private static readonly Color ColBg         = new Color(0.06f, 0.05f, 0.08f, 0.98f);
    private static readonly Color ColAccentBtn   = new Color(0.85f, 0.78f, 1.00f, 1f);
    private static readonly Color ColAccentTxt   = new Color(0.06f, 0.04f, 0.10f, 1f);
    private static readonly Color ColSecondBtn   = new Color(0.14f, 0.12f, 0.18f, 1f);
    private static readonly Color ColSecondTxt   = Color.white;
    private static readonly Color ColOutline     = new Color(1f, 1f, 1f, 0.20f);
    private static readonly Color ColSeparator   = new Color(1f, 1f, 1f, 0.14f);
    private static readonly Color ColBackBtn     = new Color(1f, 1f, 1f, 0.06f);
    private static readonly Color ColBackTxt     = new Color(1f, 1f, 1f, 0.45f);
    private static readonly Color ColTitle       = new Color(1f, 1f, 1f, 0.40f);

    // ── État ──────────────────────────────────────────────────────────────────

    private RectTransform panelRT;
    private CanvasGroup   group;
    private bool          isAnimating;

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>Crée le panneau hors écran (à droite) et retourne l'instance.</summary>
    public static MenuGameSelectPanel Create(Transform canvasParent)
    {
        var go  = new GameObject("GameSelectPanel");
        go.transform.SetParent(canvasParent, false);

        var rt       = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var cg             = go.AddComponent<CanvasGroup>();
        cg.alpha           = 1f;
        cg.blocksRaycasts  = false;
        cg.interactable    = false;

        var panel          = go.AddComponent<MenuGameSelectPanel>();
        panel.panelRT      = rt;
        panel.group        = cg;
        rt.anchoredPosition = new Vector2(CanvasRefWidth, 0f);

        panel.Build(rt);
        return panel;
    }

    // ── Construction ──────────────────────────────────────────────────────────

    private void Build(RectTransform root)
    {
        // Fond
        MakeImage("PanelBg", root, ColBg, stretch: true);

        // Titre
        MakeLabel("SelectTitle", root, "JEUX",
            anchorMin: new Vector2(0f, 0.84f), anchorMax: new Vector2(1f, 0.93f),
            size: 58f, color: ColTitle, bold: true);

        // Séparateur
        var sepImg      = MakeImage("Sep", root, ColSeparator);
        var sepRT       = sepImg.rectTransform;
        sepRT.anchorMin = new Vector2(0.08f, 0.833f);
        sepRT.anchorMax = new Vector2(0.92f, 0.833f);
        sepRT.sizeDelta = new Vector2(0f, 2f);

        // Zone des 3 boutons
        var zone          = new GameObject("BtnZone");
        zone.transform.SetParent(root, false);
        var zoneRT        = zone.AddComponent<RectTransform>();
        zoneRT.anchorMin  = new Vector2(0.5f, 0.30f);
        zoneRT.anchorMax  = new Vector2(0.5f, 0.80f);
        zoneRT.sizeDelta  = new Vector2(880f, 0f);
        zoneRT.offsetMin  = zoneRT.offsetMax = Vector2.zero;

        const float gap   = 0.04f;
        const float slotH = 1f / 3f;

        MakeGameBtn(zoneRT, "Btn_GameAndWatch",
            "GAME AND WATCH", "Arcade classic",
            new Vector2(0f, slotH * 2 + gap), new Vector2(1f, 1f),
            ColAccentBtn, ColAccentTxt,
            () => LoadGame(SceneGameAndWatch, "GAME AND WATCH"));

        MakeGameBtn(zoneRT, "Btn_BubbleShooter",
            "BUBBLE SHOOTER", "Casual",
            new Vector2(0f, slotH + gap), new Vector2(1f, slotH * 2 - gap),
            ColSecondBtn, ColSecondTxt,
            () => LoadGame(SceneBubbleShooter, "BUBBLE SHOOTER"));

        MakeGameBtn(zoneRT, "Btn_BallGoal",
            "TILT BALL", "Skill",
            new Vector2(0f, 0f), new Vector2(1f, slotH - gap),
            ColSecondBtn, ColSecondTxt,
            () => LoadGame(SceneBallGoal, "TILT BALL"));

        MakeBackButton(root);
    }

    // ── Helpers UI ────────────────────────────────────────────────────────────

    private static void MakeGameBtn(RectTransform parent, string goName,
                                    string title, string subtitle,
                                    Vector2 anchorMin, Vector2 anchorMax,
                                    Color bgColor, Color txtColor,
                                    System.Action onClick)
    {
        var go      = new GameObject(goName);
        go.transform.SetParent(parent, false);

        var img     = go.AddComponent<Image>();
        img.sprite  = SpriteGenerator.CreateWhiteSquare();
        img.color   = bgColor;
        var rt      = img.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        // Outline
        var outGO        = new GameObject("Outline");
        outGO.transform.SetParent(rt, false);
        var outImg       = outGO.AddComponent<Image>();
        outImg.sprite    = SpriteGenerator.CreateWhiteSquare();
        outImg.color     = ColOutline;
        outImg.raycastTarget = false;
        var outRT        = outImg.rectTransform;
        outRT.anchorMin  = Vector2.zero;
        outRT.anchorMax  = Vector2.one;
        outRT.offsetMin  = new Vector2(-1.5f, -1.5f);
        outRT.offsetMax  = new Vector2( 1.5f,  1.5f);
        outGO.transform.SetAsFirstSibling();

        // Titre du jeu
        var tGO     = new GameObject("Title");
        tGO.transform.SetParent(rt, false);
        var ttmp    = tGO.AddComponent<TextMeshProUGUI>();
        ttmp.text   = title;
        ttmp.fontSize  = 46f;
        ttmp.fontStyle = FontStyles.Bold;
        ttmp.color     = txtColor;
        ttmp.alignment = TextAlignmentOptions.Left;
        ttmp.raycastTarget = false;
        var tRT     = ttmp.rectTransform;
        tRT.anchorMin = new Vector2(0f, 0.45f);
        tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(36f, 0f);
        tRT.offsetMax = new Vector2(-16f, 0f);

        // Sous-titre
        var sGO     = new GameObject("Subtitle");
        sGO.transform.SetParent(rt, false);
        var stmp    = sGO.AddComponent<TextMeshProUGUI>();
        stmp.text   = subtitle;
        stmp.fontSize  = 28f;
        stmp.fontStyle = FontStyles.Normal;
        stmp.color     = new Color(txtColor.r, txtColor.g, txtColor.b, 0.55f);
        stmp.alignment = TextAlignmentOptions.Left;
        stmp.raycastTarget = false;
        var sRT     = stmp.rectTransform;
        sRT.anchorMin = Vector2.zero;
        sRT.anchorMax = new Vector2(1f, 0.50f);
        sRT.offsetMin = new Vector2(36f, 0f);
        sRT.offsetMax = new Vector2(-16f, 0f);

        // Flèche
        var aGO     = new GameObject("Arrow");
        aGO.transform.SetParent(rt, false);
        var atmp    = aGO.AddComponent<TextMeshProUGUI>();
        atmp.text   = "›";
        atmp.fontSize  = 64f;
        atmp.color     = new Color(txtColor.r, txtColor.g, txtColor.b, 0.35f);
        atmp.alignment = TextAlignmentOptions.Right;
        atmp.raycastTarget = false;
        var aRT     = atmp.rectTransform;
        aRT.anchorMin = Vector2.zero;
        aRT.anchorMax = Vector2.one;
        aRT.offsetMin = new Vector2(0f, 0f);
        aRT.offsetMax = new Vector2(-20f, 0f);

        var btn           = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => onClick?.Invoke());
    }

    private void MakeBackButton(RectTransform root)
    {
        var go  = new GameObject("BackButton");
        go.transform.SetParent(root, false);

        var img    = go.AddComponent<Image>();
        img.sprite = SpriteGenerator.CreateWhiteSquare();
        img.color  = ColBackBtn;
        var rt     = img.rectTransform;
        rt.anchorMin      = new Vector2(0.5f, 0f);
        rt.anchorMax      = new Vector2(0.5f, 0f);
        rt.pivot          = new Vector2(0.5f, 0f);
        rt.sizeDelta      = new Vector2(340f, 80f);
        rt.anchoredPosition = new Vector2(0f, 72f);

        var lgo   = new GameObject("Label");
        lgo.transform.SetParent(rt, false);
        var ltmp  = lgo.AddComponent<TextMeshProUGUI>();
        ltmp.text = "← RETOUR";
        ltmp.fontSize  = 38f;
        ltmp.fontStyle = FontStyles.Bold;
        ltmp.color     = ColBackTxt;
        ltmp.alignment = TextAlignmentOptions.Center;
        ltmp.raycastTarget = false;
        var lrt   = ltmp.rectTransform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;

        var btn           = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(Hide);
    }

    private static Image MakeImage(string name, RectTransform parent, Color color,
                                   bool stretch = false)
    {
        var go     = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img    = go.AddComponent<Image>();
        img.sprite = SpriteGenerator.CreateWhiteSquare();
        img.color  = color;
        img.raycastTarget = false;
        if (stretch)
        {
            img.rectTransform.anchorMin = Vector2.zero;
            img.rectTransform.anchorMax = Vector2.one;
            img.rectTransform.offsetMin = img.rectTransform.offsetMax = Vector2.zero;
        }
        return img;
    }

    private static void MakeLabel(string name, RectTransform parent, string text,
                                  Vector2 anchorMin, Vector2 anchorMax,
                                  float size, Color color, bool bold = false)
    {
        var go    = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp   = go.AddComponent<TextMeshProUGUI>();
        tmp.text  = text;
        tmp.fontSize  = size;
        tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        var rt    = tmp.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // ── Show / Hide ───────────────────────────────────────────────────────────

    /// <summary>Slide le panel depuis la droite, EaseOutQuart 0.4s.</summary>
    public void Show()
    {
        if (isAnimating) return;
        isAnimating          = true;
        group.blocksRaycasts = true;
        group.interactable   = true;

        StopAllCoroutines();
        panelRT.anchoredPosition = new Vector2(CanvasRefWidth, 0f);
        StartCoroutine(Slide(CanvasRefWidth, 0f, () => isAnimating = false));
    }

    /// <summary>Slide le panel hors écran vers la droite, EaseOutQuart 0.4s.</summary>
    public void Hide()
    {
        if (isAnimating) return;
        isAnimating          = true;
        group.blocksRaycasts = false;
        group.interactable   = false;

        StopAllCoroutines();
        StartCoroutine(Slide(panelRT.anchoredPosition.x, CanvasRefWidth, () => isAnimating = false));
    }

    private IEnumerator Slide(float fromX, float toX, System.Action onComplete)
    {
        float elapsed = 0f;
        while (elapsed < SlideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t  = Mathf.Clamp01(elapsed / SlideDuration);
            float e  = 1f - Mathf.Pow(1f - t, 4f);   // EaseOutQuart
            panelRT.anchoredPosition = new Vector2(Mathf.LerpUnclamped(fromX, toX, e), 0f);
            yield return null;
        }
        panelRT.anchoredPosition = new Vector2(toX, 0f);
        onComplete?.Invoke();
    }

    // ── Chargement ────────────────────────────────────────────────────────────

    private static void LoadGame(string sceneName, string displayTitle)
    {
        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadScene(sceneName, displayTitle);
        else
            SceneManager.LoadSceneAsync(sceneName);
    }
}

// ── (dead code removed) ──────────────────────────────────────────────────────
#if DEAD_CODE_REMOVED_NEVER_COMPILED
    // ── Création statique ─────────────────────────────────────────────────────

    /// <summary>Crée et retourne le panneau, positionné hors écran (droite), invisible.</summary>
    public static MenuGameSelectPanel Create(Transform canvasParent)
    {
        var go    = new GameObject("GameSelectPanel");
        go.transform.SetParent(canvasParent, false);

        var rt         = go.AddComponent<RectTransform>();
        rt.anchorMin   = Vector2.zero;
        rt.anchorMax   = Vector2.one;
        rt.offsetMin   = rt.offsetMax = Vector2.zero;

        var cg                = go.AddComponent<CanvasGroup>();
        cg.alpha              = 1f;
        cg.blocksRaycasts     = false;
        cg.interactable       = false;

        var panel             = go.AddComponent<MenuGameSelectPanel>();
        panel.panelRT         = rt;
        panel.group           = cg;

        // Déplace le panel hors écran à droite
        rt.anchoredPosition   = new Vector2(CanvasRefWidth, 0f);

        panel.Build(rt);
        return panel;
    }

    // ── Construction ──────────────────────────────────────────────────────────

    private void Build(RectTransform root)
    {
        // Fond opaque
        var bg         = new GameObject("PanelBg");
        bg.transform.SetParent(root, false);
        var bgImg      = bg.AddComponent<Image>();
        bgImg.sprite   = SpriteGenerator.CreateWhiteSquare();
        bgImg.color    = ColBg;
        bgImg.raycastTarget = false;
        Stretch(bgImg.rectTransform);

        // Titre
        var titleGO      = new GameObject("SelectTitle");
        titleGO.transform.SetParent(root, false);
        var tmp          = titleGO.AddComponent<TextMeshProUGUI>();
        tmp.text         = "GAMES";
        tmp.fontSize     = 52f;
        tmp.fontStyle    = FontStyles.Bold;
        tmp.color        = ColTitle;
        tmp.alignment    = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        var titleRT      = tmp.rectTransform;
        titleRT.anchorMin = new Vector2(0f, 0.82f);
        titleRT.anchorMax = new Vector2(1f, 0.92f);
        titleRT.offsetMin = titleRT.offsetMax = Vector2.zero;

        // Séparateur
        var sep       = new GameObject("Sep");
        sep.transform.SetParent(root, false);
        var sepImg    = sep.AddComponent<Image>();
        sepImg.sprite = SpriteGenerator.CreateWhiteSquare();
        sepImg.color  = ColSeparator;
        sepImg.raycastTarget = false;
        var sepRT     = sepImg.rectTransform;
        sepRT.anchorMin = new Vector2(0.08f, 0.81f);
        sepRT.anchorMax = new Vector2(0.92f, 0.81f);
        sepRT.sizeDelta = new Vector2(0f, 2f);

        // Zone boutons
        var zone       = new GameObject("BtnZone");
        zone.transform.SetParent(root, false);
        var zoneRT     = zone.AddComponent<RectTransform>();
        zoneRT.anchorMin = new Vector2(0.5f, 0.28f);
        zoneRT.anchorMax = new Vector2(0.5f, 0.79f);
        zoneRT.sizeDelta = new Vector2(640f, 0f);
        zoneRT.offsetMin = zoneRT.offsetMax = Vector2.zero;

        const float gap   = 0.025f;
        const float slotH = 1f / 3f;

        // Bouton 1 — accent (Game and Watch)
        MakeGameBtn(zoneRT, "Btn_GameAndWatch", "GAME AND WATCH",
                    new Vector2(0f, slotH * 2 + gap), new Vector2(1f, slotH * 3),
                    ColBtnAccent, ColBtnAccentTx, 50f, isAccent: true,
                    () => LoadGame(SceneGameAndWatch, "GAME AND WATCH"));

        // Bouton 2 — Bubble Shooter
        MakeGameBtn(zoneRT, "Btn_BubbleShooter", "BUBBLE SHOOTER",
                    new Vector2(0f, slotH * 1 + gap), new Vector2(1f, slotH * 2 - gap),
                    ColBtnSecond, ColBtnText, 50f,
                    () => LoadGame(SceneBubbleShooter, "BUBBLE SHOOTER"));

        // Bouton 3 — Ball & Goal
        MakeGameBtn(zoneRT, "Btn_BallGoal", "BALL & GOAL",
                    new Vector2(0f, 0f), new Vector2(1f, slotH * 1 - gap),
                    ColBtnSecond, ColBtnText, 50f,
                    () => LoadGame(SceneBallGoal, "BALL & GOAL"));

        // Bouton retour
        MakeBackButton(root);
    }

    // ── Helpers de construction ───────────────────────────────────────────────

    private static void MakeGameBtn(RectTransform parent, string goName, string label,
                                    Vector2 anchorMin, Vector2 anchorMax,
                                    Color bgColor, Color txtColor, float fontSize,
                                    System.Action onClick, bool isAccent = false)
    {
        var go  = new GameObject(goName);
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
            var outGO           = new GameObject("Outline");
            outGO.transform.SetParent(rt, false);
            var outImg          = outGO.AddComponent<Image>();
            outImg.sprite       = SpriteGenerator.CreateWhiteSquare();
            outImg.color        = ColBtnOutline;
            outImg.raycastTarget = false;
            var outRT           = outImg.rectTransform;
            outRT.anchorMin     = Vector2.zero;
            outRT.anchorMax     = Vector2.one;
            outRT.offsetMin     = new Vector2(-1.5f, -1.5f);
            outRT.offsetMax     = new Vector2( 1.5f,  1.5f);
            outGO.transform.SetAsFirstSibling();
        }

        var lgo  = new GameObject("Label");
        lgo.transform.SetParent(rt, false);
        var ltmp = lgo.AddComponent<TextMeshProUGUI>();
        ltmp.text           = label;
        ltmp.fontSize       = fontSize;
        ltmp.fontStyle      = FontStyles.Bold;
        ltmp.color          = txtColor;
        ltmp.alignment      = TextAlignmentOptions.Center;
        ltmp.raycastTarget  = false;
        var lrt             = ltmp.rectTransform;
        lrt.anchorMin       = Vector2.zero;
        lrt.anchorMax       = Vector2.one;
        lrt.offsetMin       = lrt.offsetMax = Vector2.zero;

        var btn           = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => onClick?.Invoke());
    }

    private void MakeBackButton(RectTransform root)
    {
        var go  = new GameObject("BackButton");
        go.transform.SetParent(root, false);

        var img         = go.AddComponent<Image>();
        img.sprite      = SpriteGenerator.CreateWhiteSquare();
        img.color       = ColBackBtn;
        var rt          = img.rectTransform;
        rt.anchorMin    = new Vector2(0.5f, 0.06f);
        rt.anchorMax    = new Vector2(0.5f, 0.14f);
        rt.sizeDelta    = new Vector2(380f, 0f);
        rt.offsetMin    = new Vector2(rt.offsetMin.x, 0f);
        rt.offsetMax    = new Vector2(rt.offsetMax.x, 0f);

        var lgo  = new GameObject("Label");
        lgo.transform.SetParent(rt, false);
        var ltmp = lgo.AddComponent<TextMeshProUGUI>();
        ltmp.text          = "← RETOUR";
        ltmp.fontSize      = 42f;
        ltmp.fontStyle     = FontStyles.Bold;
        ltmp.color         = ColBackTxt;
        ltmp.alignment     = TextAlignmentOptions.Center;
        ltmp.raycastTarget = false;
        var lrt            = ltmp.rectTransform;
        lrt.anchorMin      = Vector2.zero;
        lrt.anchorMax      = Vector2.one;
        lrt.offsetMin      = lrt.offsetMax = Vector2.zero;

        var btn           = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(Hide);
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // ── Affichage — slide depuis la droite ────────────────────────────────────

    /// <summary>Slide le panel depuis la droite vers x=0 (EaseOutQuart, 0.4s).</summary>
    public void Show()
    {
        if (isAnimating) return;
        isAnimating           = true;
        group.blocksRaycasts  = true;
        group.interactable    = true;

        LeanTween.cancel(panelRT.gameObject);
        panelRT.anchoredPosition = new Vector2(CanvasRefWidth, 0f);

        LeanTween.moveX(panelRT, 0f, SlideDuration)
                 .setEaseOutQuart()
                 .setOnComplete(() => isAnimating = false);
    }

    /// <summary>Slide le panel vers la droite hors écran (EaseOutQuart).</summary>
    public void Hide()
    {
        if (isAnimating) return;
        isAnimating          = true;
        group.blocksRaycasts = false;
        group.interactable   = false;

        LeanTween.cancel(panelRT.gameObject);

        LeanTween.moveX(panelRT, CanvasRefWidth, SlideDuration)
                 .setEaseOutQuart()
                 .setOnComplete(() => isAnimating = false);
    }

    // ── Chargement de scène ───────────────────────────────────────────────────

    private static void LoadGame(string sceneName, string displayTitle)
    {
        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadScene(sceneName, displayTitle);
        else
            SceneLoader.Instance?.LoadAsync(sceneName);
    }
}

                locked: true);

        // Bouton retour — coin bas-centre
        MakeBackButton(root);
    }

    // ── Helpers de construction ───────────────────────────────────────────────

    private void MakeBtn(RectTransform parent, string goName, string label,
                         Vector2 anchorMin, Vector2 anchorMax,
                         Color bgColor, Color txtColor, float fontSize,
                         bool isAccent = false, bool locked = false,
                         System.Action onClick = null)
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

        // Contour sur les boutons non-accent et non-verrouillés
        if (!isAccent)
        {
            var outGO       = new GameObject("Outline");
            outGO.transform.SetParent(rt, false);
            var outImg      = outGO.AddComponent<Image>();
            outImg.sprite   = SpriteGenerator.CreateWhiteSquare();
            outImg.color    = locked ? new Color(1f,1f,1f,0.08f) : ColBtnOutline;
            outImg.raycastTarget = false;
            var outRT       = outImg.rectTransform;
            outRT.anchorMin = Vector2.zero;
            outRT.anchorMax = Vector2.one;
            outRT.offsetMin = new Vector2(-1.5f, -1.5f);
            outRT.offsetMax = new Vector2( 1.5f,  1.5f);
            outGO.transform.SetAsFirstSibling();
        }

        // Label
        var lgo         = new GameObject("Label");
        lgo.transform.SetParent(rt, false);
        var ltmp        = lgo.AddComponent<TextMeshProUGUI>();
        ltmp.text       = label;
        ltmp.fontSize   = fontSize;
        ltmp.fontStyle  = FontStyles.Bold;
        ltmp.color      = txtColor;
        ltmp.alignment  = TextAlignmentOptions.Center;
        ltmp.raycastTarget = false;
        var lrt         = ltmp.rectTransform;
        lrt.anchorMin   = Vector2.zero;
        lrt.anchorMax   = Vector2.one;
        lrt.offsetMin   = lrt.offsetMax = Vector2.zero;

        // Bouton Unity — désactivé si verrouillé
        if (!locked && onClick != null)
        {
            var btn           = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());
        }
    }

    private void MakeBackButton(RectTransform root)
    {
        var go      = new GameObject("BackButton");
        go.transform.SetParent(root, false);

        var img         = go.AddComponent<Image>();
        img.sprite      = SpriteGenerator.CreateWhiteSquare();
        img.color       = ColBackBtn;
        var rt          = img.rectTransform;
        rt.anchorMin    = new Vector2(0.5f, 0.04f);
        rt.anchorMax    = new Vector2(0.5f, 0.14f);
        rt.sizeDelta    = new Vector2(380f, 0f);
        rt.offsetMin    = new Vector2(rt.offsetMin.x, 0f);
        rt.offsetMax    = new Vector2(rt.offsetMax.x, 0f);

        var lgo         = new GameObject("Label");
        lgo.transform.SetParent(rt, false);
        var ltmp        = lgo.AddComponent<TextMeshProUGUI>();
        ltmp.text       = "← RETOUR";
        ltmp.fontSize   = 42f;
        ltmp.fontStyle  = FontStyles.Bold;
        ltmp.color      = ColBackTxt;
        ltmp.alignment  = TextAlignmentOptions.Center;
        ltmp.raycastTarget = false;
        var lrt         = ltmp.rectTransform;
        lrt.anchorMin   = Vector2.zero;
        lrt.anchorMax   = Vector2.one;
        lrt.offsetMin   = lrt.offsetMax = Vector2.zero;

        var btn           = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(Hide);
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // ── Affichage ─────────────────────────────────────────────────────────────

    /// <summary>Affiche le panneau avec un fondu entrant.</summary>
    public void Show() => StartCoroutine(Fade(0f, 1f, true));

    /// <summary>Masque le panneau avec un fondu sortant.</summary>
    public void Hide() => StartCoroutine(Fade(1f, 0f, false));

    private System.Collections.IEnumerator Fade(float from, float to, bool show)
    {
        const float duration = 0.22f;
        if (show)
        {
            group.blocksRaycasts = true;
            group.interactable   = true;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed      += Time.deltaTime;
            group.alpha   = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        group.alpha = to;

        if (!show)
        {
            group.blocksRaycasts = false;
            group.interactable   = false;
        }
    }

    // ── Actions ───────────────────────────────────────────────────────────────

    private void OnGame1()
    {
        const string sceneName = "TiltBall";
        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadScene(sceneName, "TILT BALL");
        else
            SceneManager.LoadScene(sceneName);
    }

    private void OnGame2()
    {
        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadScene(OWGameManager.SceneMinijeu2, "BULLES");
        else
            SceneManager.LoadScene(OWGameManager.SceneMinijeu2);
    }

    private void OnGame3()
    {
        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadScene(OWGameManager.SceneMinijeu4, "ARÈNE");
        else
            SceneManager.LoadScene(OWGameManager.SceneMinijeu4);
    }
}
#endif
