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

    private const float BtnH    = 180f;   // hauteur fixe de chaque bouton
    private const float BtnGap  = 20f;    // espace entre boutons
    private const float BtnW    = 880f;   // largeur de la zone boutons

    // ── Palette ───────────────────────────────────────────────────────────────

    private static readonly Color ColBg        = new Color(0.06f, 0.05f, 0.08f, 0.98f);
    private static readonly Color ColAccentBtn  = new Color(0.85f, 0.78f, 1.00f, 1f);
    private static readonly Color ColAccentTxt  = new Color(0.06f, 0.04f, 0.10f, 1f);
    private static readonly Color ColSecondBtn  = new Color(0.14f, 0.12f, 0.18f, 1f);
    private static readonly Color ColSecondTxt  = Color.white;
    private static readonly Color ColOutline    = new Color(1f, 1f, 1f, 0.20f);
    private static readonly Color ColSeparator  = new Color(1f, 1f, 1f, 0.14f);
    private static readonly Color ColBackBtn    = new Color(1f, 1f, 1f, 0.06f);
    private static readonly Color ColBackTxt    = new Color(1f, 1f, 1f, 0.45f);
    private static readonly Color ColTitle      = new Color(1f, 1f, 1f, 0.40f);

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
        // Fond plein écran
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

        // Zone boutons : centrée horizontalement, taille fixe
        // Hauteur totale = 3 boutons + 2 gaps
        float zoneH = BtnH * 3f + BtnGap * 2f;

        var zone   = new GameObject("BtnZone");
        zone.transform.SetParent(root, false);
        var zoneRT = zone.AddComponent<RectTransform>();
        zoneRT.anchorMin = new Vector2(0.5f, 0.5f);
        zoneRT.anchorMax = new Vector2(0.5f, 0.5f);
        zoneRT.pivot     = new Vector2(0.5f, 0.5f);
        zoneRT.sizeDelta = new Vector2(BtnW, zoneH);
        zoneRT.anchoredPosition = new Vector2(0f, 20f);

        // VerticalLayoutGroup pour empiler les boutons proprement
        var vlg = zone.AddComponent<VerticalLayoutGroup>();
        vlg.spacing              = BtnGap;
        vlg.childAlignment       = TextAnchor.UpperCenter;
        vlg.childControlWidth    = true;
        vlg.childControlHeight   = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = RectOffset_Zero();

        MakeGameBtn(zoneRT, "Btn_Game1", LabelGame1, ColAccentBtn, ColAccentTxt,
            () => LoadGame(SceneGame1, LabelGame1));

        MakeGameBtn(zoneRT, "Btn_Game2", LabelGame2, ColSecondBtn, ColSecondTxt,
            () => LoadGame(SceneGame2, LabelGame2));

        MakeGameBtn(zoneRT, "Btn_Game3", LabelGame3, ColSecondBtn, ColSecondTxt,
            () => LoadGame(SceneGame3, LabelGame3));

        MakeBackButton(root);
    }

    // ── Helpers UI ────────────────────────────────────────────────────────────

    private static void MakeGameBtn(RectTransform parent, string goName,
                                    string title,
                                    Color bgColor, Color txtColor,
                                    System.Action onClick)
    {
        var go  = new GameObject(goName);
        go.transform.SetParent(parent, false);

        var img    = go.AddComponent<Image>();
        img.sprite = SpriteGenerator.CreateWhiteSquare();
        img.color  = bgColor;

        // Hauteur fixe, la largeur est contrôlée par le VerticalLayoutGroup
        var rt        = img.rectTransform;
        rt.sizeDelta  = new Vector2(0f, BtnH);

        // Outline
        var outGO    = new GameObject("Outline");
        outGO.transform.SetParent(rt, false);
        var outImg   = outGO.AddComponent<Image>();
        outImg.sprite = SpriteGenerator.CreateWhiteSquare();
        outImg.color  = ColOutline;
        outImg.raycastTarget = false;
        var outRT    = outImg.rectTransform;
        outRT.anchorMin = Vector2.zero;
        outRT.anchorMax = Vector2.one;
        outRT.offsetMin = new Vector2(-1.5f, -1.5f);
        outRT.offsetMax = new Vector2( 1.5f,  1.5f);
        outGO.transform.SetAsFirstSibling();

        // Label centré
        var lgo  = new GameObject("Label");
        lgo.transform.SetParent(rt, false);
        var ltmp = lgo.AddComponent<TextMeshProUGUI>();
        ltmp.text      = title;
        ltmp.fontSize  = 46f;
        ltmp.fontStyle = FontStyles.Bold;
        ltmp.color     = txtColor;
        ltmp.alignment = TextAlignmentOptions.Center;
        ltmp.raycastTarget = false;
        var lrt  = ltmp.rectTransform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(20f, 0f);
        lrt.offsetMax = new Vector2(-20f, 0f);

        // Flèche droite
        var aGO  = new GameObject("Arrow");
        aGO.transform.SetParent(rt, false);
        var atmp = aGO.AddComponent<TextMeshProUGUI>();
        atmp.text      = "›";
        atmp.fontSize  = 64f;
        atmp.color     = new Color(txtColor.r, txtColor.g, txtColor.b, 0.35f);
        atmp.alignment = TextAlignmentOptions.Right;
        atmp.raycastTarget = false;
        var aRT  = atmp.rectTransform;
        aRT.anchorMin = Vector2.zero;
        aRT.anchorMax = Vector2.one;
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
