using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Panneau de sélection de jeu qui slide depuis la droite (EaseOutQuart, 0.4s).
///
/// Les noms de scènes sont assignables via l'Inspector après création du GameObject,
/// ou via les champs publics avant le premier <see cref="Show"/>.
/// </summary>
public class MenuGameSelectPanel : MonoBehaviour
{
    // ── Scènes assignables depuis l'Inspector ─────────────────────────────────

    [Header("Scènes des mini-jeux")]
    [Tooltip("Nom exact de la scène dans les Build Settings")]
    public string SceneGame1 = "GameAndWatch";
    [Tooltip("Nom exact de la scène dans les Build Settings")]
    public string SceneGame2 = "Minijeu-Bulles";
    [Tooltip("Nom exact de la scène dans les Build Settings")]
    public string SceneGame3 = "TiltBall";

    [Header("Labels affichés sur les boutons")]
    public string LabelGame1 = "GAME AND WATCH";
    public string LabelGame2 = "BUBBLE SHOOTER";
    public string LabelGame3 = "TILT BALL";

    // ── Timings ───────────────────────────────────────────────────────────────

    private const float SlideDuration  = 0.40f;
    private const float CanvasRefWidth = 1080f;

    // ── Dimensions des boutons ────────────────────────────────────────────────

    private const float BtnH    = 140f;   // hauteur fixe de chaque bouton
    private const float BtnGap  = 16f;    // espace entre boutons
    private const float BtnW    = 880f;   // largeur de la zone boutons

    // ── Palette ───────────────────────────────────────────────────────────────

    private static readonly Color ColBg       = new Color(0f, 0f, 0f, 0.96f);
    private static readonly Color ColBtnBg    = new Color(0f, 0f, 0f, 0.42f);
    private static readonly Color ColBtnFrame = new Color(1f, 1f, 1f, 0.14f);
    private static readonly Color ColBtnTxt   = Color.white;
    private static readonly Color ColArrow    = new Color(1f, 1f, 1f, 0.28f);
    private static readonly Color ColSep      = new Color(1f, 1f, 1f, 0.10f);
    private static readonly Color ColTitle    = Color.white;

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

        var cg            = go.AddComponent<CanvasGroup>();
        cg.alpha          = 1f;
        cg.blocksRaycasts = false;
        cg.interactable   = false;

        var panel         = go.AddComponent<MenuGameSelectPanel>();
        panel.panelRT     = rt;
        panel.group       = cg;
        rt.anchoredPosition = new Vector2(CanvasRefWidth, 0f);

        panel.Build(rt);
        return panel;
    }

    // ── Construction ──────────────────────────────────────────────────────────

    private void Build(RectTransform root)
    {
        // ── Fond plein écran quasi-noir ───────────────────────────────────────
        var bgImg            = root.gameObject.AddComponent<Image>();
        bgImg.sprite         = SpriteGenerator.CreateWhiteSquare();
        bgImg.color          = ColBg;
        bgImg.raycastTarget  = false;

        // ── Halos mystiques dans le fond ──────────────────────────────────────
        root.gameObject.AddComponent<MenuSpaceHalos>();

        // ── Titre "GAME" ──────────────────────────────────────────────────────
        MakeLabel("SelectTitle", root, "GAME",
            anchorMin: new Vector2(0f, 0.78f), anchorMax: new Vector2(1f, 0.86f),
            size: 58f, color: ColTitle, bold: true);

        // ── Séparateur fin ────────────────────────────────────────────────────
        var sepImg      = MakeImage("Sep", root, ColSep);
        var sepRT       = sepImg.rectTransform;
        sepRT.anchorMin = new Vector2(0.08f, 0.775f);
        sepRT.anchorMax = new Vector2(0.92f, 0.775f);
        sepRT.sizeDelta = new Vector2(0f, 1f);

        // ── Zone boutons : 3 jeux ─────────────────────────────────────────────
        float zoneH = BtnH * 3f + BtnGap * 2f;

        var zone   = new GameObject("BtnZone");
        zone.transform.SetParent(root, false);
        var zoneRT = zone.AddComponent<RectTransform>();
        zoneRT.anchorMin        = new Vector2(0.5f, 0.5f);
        zoneRT.anchorMax        = new Vector2(0.5f, 0.5f);
        zoneRT.pivot            = new Vector2(0.5f, 0.5f);
        zoneRT.sizeDelta        = new Vector2(BtnW, zoneH);
        zoneRT.anchoredPosition = new Vector2(0f, 0f);

        var vlg = zone.AddComponent<VerticalLayoutGroup>();
        vlg.spacing              = BtnGap;
        vlg.childAlignment       = TextAnchor.UpperCenter;
        vlg.childControlWidth    = true;
        vlg.childControlHeight   = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = RectOffset_Zero();

        MakeGameBtn(zoneRT, "Btn_Game1", LabelGame1, () => LoadGame(SceneGame1, LabelGame1));
        MakeGameBtn(zoneRT, "Btn_Game2", LabelGame2, () => LoadGame(SceneGame2, LabelGame2));
        MakeGameBtn(zoneRT, "Btn_Game3", LabelGame3, () => LoadGame(SceneGame3, LabelGame3));
    }

    // ── Helpers UI ────────────────────────────────────────────────────────────

    /// <summary>
    /// Crée un bouton de jeu : cadre noir à faible opacité + bordure fine + label Michroma.
    /// Aucun sprite "bouton UI" — fond procédural uniquement.
    /// </summary>
    private static void MakeGameBtn(RectTransform parent, string goName,
                                    string title, System.Action onClick)
    {
        // ── Conteneur ──────────────────────────────────────────────────────────
        var go  = new GameObject(goName);
        go.transform.SetParent(parent, false);

        var rt       = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, BtnH);

        // ── Fond : carré noir faible opacité ──────────────────────────────────
        var bgImg           = go.AddComponent<Image>();
        bgImg.sprite        = SpriteGenerator.CreateWhiteSquare();
        bgImg.color         = ColBtnBg;
        bgImg.type          = Image.Type.Simple;
        bgImg.raycastTarget = true;

        // ── Bordure fine (frame) ───────────────────────────────────────────────
        var frameGO         = new GameObject("Frame");
        frameGO.transform.SetParent(rt, false);
        var frameImg        = frameGO.AddComponent<Image>();
        frameImg.sprite     = SpriteGenerator.CreateWhiteSquare();
        frameImg.color      = ColBtnFrame;
        frameImg.raycastTarget = false;

        // La bordure est 1 px plus grande que le fond sur chaque côté
        var frameRT         = frameImg.rectTransform;
        frameRT.anchorMin   = Vector2.zero;
        frameRT.anchorMax   = Vector2.one;
        frameRT.offsetMin   = new Vector2(-1f, -1f);
        frameRT.offsetMax   = new Vector2( 1f,  1f);
        frameGO.transform.SetAsFirstSibling();

        // ── Label Michroma centré ──────────────────────────────────────────────
        var lgo  = new GameObject("Label");
        lgo.transform.SetParent(rt, false);
        var ltmp = lgo.AddComponent<TextMeshProUGUI>();
        ltmp.text              = title;
        ltmp.fontSize          = 42f;
        ltmp.fontStyle         = FontStyles.Bold;
        ltmp.color             = ColBtnTxt;
        ltmp.alignment         = TextAlignmentOptions.Center;
        ltmp.enableWordWrapping = false;
        ltmp.raycastTarget     = false;
        MenuAssets.ApplyFont(ltmp);

        var lrt       = ltmp.rectTransform;
        lrt.anchorMin = new Vector2(0.04f, 0f);
        lrt.anchorMax = new Vector2(0.88f, 1f);
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;

        // ── Flèche droite ─────────────────────────────────────────────────────
        var aGO  = new GameObject("Arrow");
        aGO.transform.SetParent(rt, false);
        var atmp = aGO.AddComponent<TextMeshProUGUI>();
        atmp.text              = "›";
        atmp.fontSize          = 60f;
        atmp.color             = ColArrow;
        atmp.alignment         = TextAlignmentOptions.Right;
        atmp.enableWordWrapping = false;
        atmp.raycastTarget     = false;
        MenuAssets.ApplyFont(atmp);

        var aRT       = atmp.rectTransform;
        aRT.anchorMin = Vector2.zero;
        aRT.anchorMax = Vector2.one;
        aRT.offsetMin = Vector2.zero;
        aRT.offsetMax = new Vector2(-18f, 0f);

        // ── Bouton ────────────────────────────────────────────────────────────
        var btn             = go.AddComponent<Button>();
        btn.targetGraphic   = bgImg;
        var colors          = btn.colors;
        colors.normalColor      = ColBtnBg;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.10f);
        colors.pressedColor     = new Color(1f, 1f, 1f, 0.18f);
        colors.selectedColor    = ColBtnBg;
        colors.fadeDuration     = 0.08f;
        btn.colors          = colors;
        btn.onClick.AddListener(() => onClick?.Invoke());
    }

    private void MakeBackButton(RectTransform root)
    {
        // Intentionnellement vide — le bouton GAMES du menu principal sert de toggle.
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
        MenuAssets.ApplyFont(tmp);
        var rt    = tmp.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static RectOffset RectOffset_Zero() => new RectOffset(0, 0, 0, 0);

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

    /// <summary>Lance le chargement de scène via SceneTransition si disponible.</summary>
    private static void LoadGame(string sceneName, string displayTitle)
    {
        if (string.IsNullOrEmpty(sceneName)) return;

        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadScene(sceneName, displayTitle);
        else
            SceneManager.LoadSceneAsync(sceneName);
    }
}
