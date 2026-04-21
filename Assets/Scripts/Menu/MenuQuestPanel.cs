using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panneau des quêtes qui slide depuis la droite (EaseOutQuart, 0.4s).
/// Contient une liste de quêtes et un bouton Retour.
/// </summary>
public class MenuQuestPanel : MonoBehaviour
{
    // ── Timings ───────────────────────────────────────────────────────────────

    private const float SlideDuration  = 0.40f;
    private const float CanvasRefWidth = 1080f;

    // ── Palette ───────────────────────────────────────────────────────────────

    private static readonly Color ColBg        = new Color(0.06f, 0.05f, 0.08f, 0.98f);
    private static readonly Color ColTitle      = new Color(1f, 1f, 1f, 0.40f);
    private static readonly Color ColSeparator  = new Color(1f, 1f, 1f, 0.14f);
    private static readonly Color ColQuestBtn   = new Color(0.14f, 0.12f, 0.18f, 1f);
    private static readonly Color ColQuestTxt   = Color.white;
    private static readonly Color ColOutline    = new Color(1f, 1f, 1f, 0.20f);
    private static readonly Color ColBackBtn    = new Color(1f, 1f, 1f, 0.06f);
    private static readonly Color ColBackTxt    = new Color(1f, 1f, 1f, 0.45f);

    // ── État ──────────────────────────────────────────────────────────────────

    private RectTransform panelRT;
    private CanvasGroup   group;
    private bool          isAnimating;

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>Crée le panneau hors écran (à droite) et retourne l'instance.</summary>
    public static MenuQuestPanel Create(Transform canvasParent)
    {
        var go = new GameObject("QuestPanel");
        go.transform.SetParent(canvasParent, false);

        var rt       = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var cg            = go.AddComponent<CanvasGroup>();
        cg.alpha          = 1f;
        cg.blocksRaycasts = false;
        cg.interactable   = false;

        var panel         = go.AddComponent<MenuQuestPanel>();
        panel.panelRT     = rt;
        panel.group       = cg;
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
        MakeLabel("QuestTitle", root, "QUÊTES",
            anchorMin: new Vector2(0f, 0.84f), anchorMax: new Vector2(1f, 0.93f),
            size: 58f, color: ColTitle, bold: true);

        // Séparateur
        var sepImg      = MakeImage("Sep", root, ColSeparator);
        var sepRT       = sepImg.rectTransform;
        sepRT.anchorMin = new Vector2(0.08f, 0.833f);
        sepRT.anchorMax = new Vector2(0.92f, 0.833f);
        sepRT.sizeDelta = new Vector2(0f, 2f);

        // Zone des quêtes
        var zone       = new GameObject("QuestZone");
        zone.transform.SetParent(root, false);
        var zoneRT     = zone.AddComponent<RectTransform>();
        zoneRT.anchorMin = new Vector2(0.5f, 0.28f);
        zoneRT.anchorMax = new Vector2(0.5f, 0.82f);
        zoneRT.sizeDelta = new Vector2(880f, 0f);
        zoneRT.offsetMin = zoneRT.offsetMax = Vector2.zero;

        // 3 entrées de quête réparties verticalement
        const float gap   = 0.03f;
        const float slotH = 1f / 3f;

        MakeQuestEntry(zoneRT, "Quest_1",
            "COMPLÉTER 5 PARTIES",
            "0 / 5 parties jouées",
            new Vector2(0f, slotH * 2 + gap), new Vector2(1f, 1f));

        MakeQuestEntry(zoneRT, "Quest_2",
            "SCORE > 500",
            "Meilleur score : 0",
            new Vector2(0f, slotH + gap), new Vector2(1f, slotH * 2 - gap));

        MakeQuestEntry(zoneRT, "Quest_3",
            "ESSAYER 3 JEUX",
            "0 / 3 jeux essayés",
            new Vector2(0f, 0f), new Vector2(1f, slotH - gap));

        MakeBackButton(root);
    }

    // ── Entrée de quête ───────────────────────────────────────────────────────

    private static void MakeQuestEntry(RectTransform parent, string goName,
        string title, string progress,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var go  = new GameObject(goName);
        go.transform.SetParent(parent, false);

        var img    = go.AddComponent<Image>();
        img.sprite = SpriteGenerator.CreateWhiteSquare();
        img.color  = ColQuestBtn;
        img.raycastTarget = false;
        var rt     = img.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        // Contour
        var outGO       = new GameObject("Outline");
        outGO.transform.SetParent(rt, false);
        var outImg      = outGO.AddComponent<Image>();
        outImg.sprite   = SpriteGenerator.CreateWhiteSquare();
        outImg.color    = ColOutline;
        outImg.raycastTarget = false;
        var outRT       = outImg.rectTransform;
        outRT.anchorMin = Vector2.zero;
        outRT.anchorMax = Vector2.one;
        outRT.offsetMin = new Vector2(-1.5f, -1.5f);
        outRT.offsetMax = new Vector2( 1.5f,  1.5f);
        outGO.transform.SetAsFirstSibling();

        // Titre de la quête
        var tGO    = new GameObject("Title");
        tGO.transform.SetParent(rt, false);
        var ttmp   = tGO.AddComponent<TextMeshProUGUI>();
        ttmp.text  = title;
        ttmp.fontSize  = 38f;
        ttmp.fontStyle = FontStyles.Bold;
        ttmp.color     = ColQuestTxt;
        ttmp.alignment = TextAlignmentOptions.Left;
        ttmp.raycastTarget = false;
        var tRT    = ttmp.rectTransform;
        tRT.anchorMin = new Vector2(0f, 0.48f);
        tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(36f, 0f);
        tRT.offsetMax = new Vector2(-16f, 0f);

        // Progression
        var pGO    = new GameObject("Progress");
        pGO.transform.SetParent(rt, false);
        var ptmp   = pGO.AddComponent<TextMeshProUGUI>();
        ptmp.text  = progress;
        ptmp.fontSize  = 26f;
        ptmp.fontStyle = FontStyles.Normal;
        ptmp.color     = new Color(1f, 1f, 1f, 0.50f);
        ptmp.alignment = TextAlignmentOptions.Left;
        ptmp.raycastTarget = false;
        var pRT    = ptmp.rectTransform;
        pRT.anchorMin = Vector2.zero;
        pRT.anchorMax = new Vector2(1f, 0.50f);
        pRT.offsetMin = new Vector2(36f, 0f);
        pRT.offsetMax = new Vector2(-16f, 0f);
    }

    // ── Bouton Retour ────────────────────────────────────────────────────────

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

        var lgo  = new GameObject("Label");
        lgo.transform.SetParent(rt, false);
        var ltmp = lgo.AddComponent<TextMeshProUGUI>();
        ltmp.text      = "← RETOUR";
        ltmp.fontSize  = 38f;
        ltmp.fontStyle = FontStyles.Bold;
        ltmp.color     = ColBackTxt;
        ltmp.alignment = TextAlignmentOptions.Center;
        ltmp.raycastTarget = false;
        var lrt  = ltmp.rectTransform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;

        var btn           = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(Hide);
    }

    // ── Helpers UI ───────────────────────────────────────────────────────────

    private static Image MakeImage(string name, RectTransform parent, Color color,
        bool stretch = false)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
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
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
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

    // ── Show / Hide ──────────────────────────────────────────────────────────

    /// <summary>Slide le panneau depuis la droite, EaseOutQuart 0.4s.</summary>
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

    /// <summary>Slide le panneau hors écran vers la droite, EaseOutQuart 0.4s.</summary>
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
}
