using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD du menu principal.
///
/// Layout :
///   - En haut à gauche  : <see cref="MenuScorePanel"/> (score total persistant + bouton DÉTAILS)
///   - En haut à droite  : widget Horloge (fond semi-transparent, heure système en temps réel)
///
/// Référence de résolution : 1080 × 1920 (portrait 9:16).
/// </summary>
public class MenuMainHud : MonoBehaviour
{
    // ── Constantes de mise en page ────────────────────────────────────────────

    private const float WidgetW  = 260f;
    private const float WidgetH  = 110f;
    private const float MarginX  = 32f;
    private const float MarginY  = 48f;

    // ── Palette ───────────────────────────────────────────────────────────────

    private static readonly Color ColWidgetBg = new Color(0.04f, 0.04f, 0.08f, 0.78f);
    private static readonly Color ColLabel    = new Color(1f, 1f, 1f, 0.45f);
    private static readonly Color ColClock    = new Color(0.90f, 0.90f, 1.00f, 1f);

    // ── Références ────────────────────────────────────────────────────────────

    private TextMeshProUGUI clockLabel;

    // ── Initialisation ────────────────────────────────────────────────────────

    /// <summary>Construit les widgets HUD dans le <paramref name="canvasRT"/> fourni.</summary>
    public void Init(RectTransform canvasRT)
    {
        ScoreManager.EnsureExists();
        BuildScorePanel(canvasRT);
        BuildClockWidget(canvasRT);
        CoinWalletWidget.Create(canvasRT);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (clockLabel != null)
        {
            var now = System.DateTime.Now;
            clockLabel.text = $"{now.Hour:00}:{now.Minute:00}";
        }
    }

    // ── Construction du ScorePanel (haut-gauche) ──────────────────────────────

    private void BuildScorePanel(RectTransform canvasRT)
    {
        var go      = new GameObject("MenuScorePanelRoot");
        go.transform.SetParent(canvasRT, false);
        go.AddComponent<RectTransform>();

        var panel   = go.AddComponent<MenuScorePanel>();
        panel.Init(canvasRT);
    }

    // ── Construction du widget Horloge (haut-droite) ──────────────────────────

    private void BuildClockWidget(RectTransform parent)
    {
        var widget = MakeWidget("ClockWidget", parent,
            anchorMin: new Vector2(1f, 1f),
            anchorMax: new Vector2(1f, 1f),
            pivot    : new Vector2(1f, 1f),
            offset   : new Vector2(-MarginX, -MarginY));

        MakeText("ClockLabel", widget.rt, "HEURE",
            fontSize : 22f, style: FontStyles.Bold, color: ColLabel,
            anchorMin: new Vector2(0f, 0.55f), anchorMax: Vector2.one,
            offsetMin: Vector2.zero,            offsetMax: new Vector2(-14f, 0f));

        var valueGO = MakeText("ClockValue", widget.rt, "00:00",
            fontSize : 42f, style: FontStyles.Bold, color: ColClock,
            anchorMin: Vector2.zero,             anchorMax: new Vector2(1f, 0.62f),
            offsetMin: new Vector2(8f, 4f),      offsetMax: new Vector2(-14f, 0f),
            alignment: TextAlignmentOptions.BottomRight);

        clockLabel = valueGO.GetComponent<TextMeshProUGUI>();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private readonly struct WidgetRef
    {
        public readonly GameObject go;
        public readonly RectTransform rt;
        public WidgetRef(GameObject g, RectTransform r) { go = g; rt = r; }
    }

    private static WidgetRef MakeWidget(string name, RectTransform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 offset)
    {
        var go             = new GameObject(name);
        go.transform.SetParent(parent, false);

        var img            = go.AddComponent<Image>();
        img.sprite         = SpriteGenerator.CreateWhiteSquare();
        img.color          = ColWidgetBg;
        img.raycastTarget  = false;

        var rt             = img.rectTransform;
        rt.anchorMin       = anchorMin;
        rt.anchorMax       = anchorMax;
        rt.pivot           = pivot;
        rt.sizeDelta       = new Vector2(WidgetW, WidgetH);
        rt.anchoredPosition = offset;

        return new WidgetRef(go, rt);
    }

    private static GameObject MakeText(string name, RectTransform parent,
        string text, float fontSize, FontStyles style, Color color,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin = default, Vector2 offsetMax = default,
        TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft)
    {
        var go             = new GameObject(name);
        go.transform.SetParent(parent, false);

        var tmp            = go.AddComponent<TextMeshProUGUI>();
        tmp.text           = text;
        tmp.fontSize       = fontSize;
        tmp.fontStyle      = style;
        tmp.color          = color;
        tmp.alignment      = alignment;
        tmp.raycastTarget  = false;

        var rt         = tmp.rectTransform;
        rt.anchorMin   = anchorMin;
        rt.anchorMax   = anchorMax;
        rt.offsetMin   = offsetMin;
        rt.offsetMax   = offsetMax;

        return go;
    }
}
