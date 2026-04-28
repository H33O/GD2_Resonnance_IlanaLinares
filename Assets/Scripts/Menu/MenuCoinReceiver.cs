using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Créé par <see cref="MenuMainSetup"/> au démarrage de la scène Menu.
///
/// Si <see cref="GameEndData.HasPending"/> est vrai, lance la séquence :
///   1. Mini-card score/pièces slide-in depuis le haut
///   2. Tokens dorés qui volent vers le <see cref="CoinWalletWidget"/>
///   3. <see cref="ScoreManager.AddCoins"/> → le wallet affiche son propre roll-up + flash
///   4. La card fade-out et se détruit
/// </summary>
public class MenuCoinReceiver : MonoBehaviour
{
    // ── Timings ───────────────────────────────────────────────────────────────

    private const float WaitAfterLoad  = 0.80f;
    private const float SlideInDur     = 0.40f;
    private const float ScoreCountDur  = 1.00f;
    private const float HoldAfterDur   = 1.60f;
    private const float FadeOutDur     = 0.35f;
    private const float PulseDur       = 0.18f;
    private const float PulseScale     = 1.22f;

    // ── Palette ───────────────────────────────────────────────────────────────

    private static readonly Color ColCard     = new Color(0.07f, 0.06f, 0.13f, 0.97f);
    private static readonly Color ColEdge     = new Color(0.95f, 0.15f, 0.15f, 0.85f);
    private static readonly Color ColBorder   = new Color(1.00f, 1.00f, 1.00f, 0.10f);
    private static readonly Color ColSub      = new Color(1.00f, 1.00f, 1.00f, 0.35f);
    private static readonly Color ColScoreVal = new Color(1.00f, 0.82f, 0.18f, 1.00f);
    private static readonly Color ColLbl      = new Color(1.00f, 1.00f, 1.00f, 0.40f);

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>Crée le composant et l'attache au canvas du menu.</summary>
    public static MenuCoinReceiver Create(RectTransform canvasRT)
    {
        var go   = new GameObject("MenuCoinReceiver");
        go.transform.SetParent(canvasRT, false);
        var rt   = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var comp      = go.AddComponent<MenuCoinReceiver>();
        comp.canvasRT = canvasRT;
        return comp;
    }

    // ── Champs ────────────────────────────────────────────────────────────────

    private RectTransform canvasRT;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        if (!GameEndData.HasPending) return;

        int      score    = GameEndData.FinalScore;
        GameType gameType = GameEndData.GameType;
        GameEndData.Consume();

        StartCoroutine(Run(score, gameType));
    }

    // ── Séquence principale ───────────────────────────────────────────────────

    private IEnumerator Run(int score, GameType gameType)
    {
        yield return null;
        yield return null;
        yield return new WaitForSeconds(WaitAfterLoad);

        if (QuestManager.Instance != null && score > 0)
            ScoreManager.EnsureExists();

        var (cardRT, cardGroup, scoreVal) = BuildCard();
        cardRT.gameObject.SetActive(true);

        yield return StartCoroutine(SlideIn(cardRT, cardGroup, SlideInDur));

        yield return StartCoroutine(CountUp(scoreVal, 0, score, ScoreCountDur));
        StartCoroutine(Pulse(scoreVal.rectTransform));

        yield return new WaitForSeconds(HoldAfterDur);

        yield return StartCoroutine(Fade(cardGroup, 1f, 0f, FadeOutDur));
        Destroy(cardRT.gameObject);
    }

    // ── Transfert de pièces ───────────────────────────────────────────────────

    private Vector2 WorldToCanvasAnchoredPos(Vector3 worldPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT,
            RectTransformUtility.WorldToScreenPoint(null, worldPos),
            null,
            out Vector2 local);
        return local;
    }

    // ── Construction de la card ───────────────────────────────────────────────

    private (RectTransform rt, CanvasGroup group, TextMeshProUGUI scoreVal) BuildCard()
    {
        const float W = 900f;
        const float H = 180f;

        var go = new GameObject("ResultCard");
        go.transform.SetParent(canvasRT, false);
        go.SetActive(false);

        var group   = go.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 1f);
        rt.anchorMax        = new Vector2(0.5f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.sizeDelta        = new Vector2(W, H);
        rt.anchoredPosition = new Vector2(0f, H);

        RawImg(rt, "Bg", ColCard, stretch: true);

        var accent    = new GameObject("Accent");
        accent.transform.SetParent(rt, false);
        var accentImg = accent.AddComponent<Image>();
        accentImg.sprite = SpriteGenerator.CreateWhiteSquare();
        accentImg.color  = ColEdge;
        accentImg.raycastTarget = false;
        var accentRT = accentImg.rectTransform;
        accentRT.anchorMin = new Vector2(0f, 0f);
        accentRT.anchorMax = new Vector2(0.008f, 1f);
        accentRT.offsetMin = accentRT.offsetMax = Vector2.zero;

        Lbl(rt, "Title", "RÉSULTATS",
            new Vector2(0.03f, 0.68f), new Vector2(0.97f, 1.0f),
            22f, ColSub, FontStyles.Bold);

        Lbl(rt, "ScoreLbl", "SCORE",
            new Vector2(0.04f, 0.35f), new Vector2(0.97f, 0.65f),
            20f, ColLbl, FontStyles.Normal);
        var scoreVal = Lbl(rt, "ScoreVal", "0",
            new Vector2(0.04f, 0.02f), new Vector2(0.97f, 0.40f),
            64f, ColScoreVal, FontStyles.Bold);

        return (rt, group, scoreVal);
    }

    // ── Helpers UI ────────────────────────────────────────────────────────────

    private static void RawImg(RectTransform parent, string name, Color col, bool stretch)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = SpriteGenerator.CreateWhiteSquare();
        img.color  = col;
        img.raycastTarget = false;
        var rt   = img.rectTransform;
        if (stretch)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }

    private static TextMeshProUGUI Lbl(RectTransform parent, string name, string text,
        Vector2 aMin, Vector2 aMax, float size, Color col, FontStyles style)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text             = text;
        tmp.fontSize         = size;
        tmp.fontStyle        = style;
        tmp.color            = col;
        tmp.alignment        = TextAlignmentOptions.Center;
        tmp.raycastTarget    = false;
        tmp.enableAutoSizing = false;
        var rt = tmp.rectTransform;
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return tmp;
    }

    // ── Coroutines ────────────────────────────────────────────────────────────

    /// <summary>
    /// Fait grossir puis revenir le score : scale 1 → PulseScale → 1,
    /// avec deux rebonds rapides pour un effet "punch".
    /// </summary>
    private static IEnumerator Pulse(RectTransform rt)
    {
        Vector3 base3 = rt.localScale;

        // Frappe 1 (forte)
        yield return StartCoroutinePulseHalf(rt, base3, PulseScale, PulseDur);
        yield return StartCoroutinePulseHalf(rt, base3 * PulseScale, 1f, PulseDur * 0.8f);

        // Frappe 2 (légère)
        yield return StartCoroutinePulseHalf(rt, base3, 1f + (PulseScale - 1f) * 0.4f, PulseDur * 0.6f);
        yield return StartCoroutinePulseHalf(rt, base3 * (1f + (PulseScale - 1f) * 0.4f), 1f, PulseDur * 0.5f);

        rt.localScale = base3;
    }

    private static IEnumerator StartCoroutinePulseHalf(RectTransform rt,
        Vector3 from, float toFactor, float dur)
    {
        Vector3 to = rt.localScale * toFactor;
        float   t  = 0f;
        while (t < dur)
        {
            t            += Time.deltaTime;
            float e       = Mathf.Clamp01(t / dur);
            rt.localScale = Vector3.Lerp(from, to, e);
            yield return null;
        }
        rt.localScale = to;
    }

    /// <summary>Slide depuis hors-écran (y = +H) jusqu'à anchoredPosition (0, -targetY).</summary>
    private static IEnumerator SlideIn(RectTransform rt, CanvasGroup group, float dur)
    {
        float cardH   = rt.sizeDelta.y;
        float targetY = -cardH * 0.02f;   // légèrement sous le bord supérieur
        float startY  = cardH;

        float t = 0f;
        group.alpha = 1f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / dur), 3f);  // EaseOutCubic
            rt.anchoredPosition = new Vector2(0f, Mathf.Lerp(startY, targetY, e));
            yield return null;
        }
        rt.anchoredPosition = new Vector2(0f, targetY);
    }

    private static IEnumerator Fade(CanvasGroup g, float from, float to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t      += Time.deltaTime;
            g.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / dur));
            yield return null;
        }
        g.alpha = to;
    }

    private static IEnumerator CountUp(TextMeshProUGUI lbl, int from, int to, float dur)
    {
        if (lbl == null) yield break;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / dur), 4f);
            int   v = Mathf.RoundToInt(Mathf.Lerp(from, to, e));
            lbl.text = v.ToString("N0");
            yield return null;
        }
        lbl.text = to.ToString("N0");
    }
}
