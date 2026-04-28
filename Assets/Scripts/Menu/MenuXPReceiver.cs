using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Démarre automatiquement dans la scène Menu quand <see cref="GameEndData.HasPending"/> est vrai.
///
/// Séquence :
///   1. Attend un court délai pour que tous les widgets soient construits.
///   2. Affiche une card de résultats (score + XP gagnée) qui slide du haut.
///   3. Lance des tokens XP (boules bleues) depuis la card vers la <see cref="MenuXPWidget"/>.
///   4. Crédite l'XP dans <see cref="PlayerLevelManager"/> et déclenche l'animation de la jauge.
///   5. La card disparaît en fondu.
///
/// Usage : <see cref="Create(RectTransform)"/> depuis <see cref="MenuMainSetup"/>.
/// </summary>
public class MenuXPReceiver : MonoBehaviour
{
    // ── Timings ───────────────────────────────────────────────────────────────

    private const float WaitAfterLoad  = 0.70f;
    private const float SlideInDur     = 0.38f;
    private const float CountUpDur     = 0.90f;
    private const float PreTransferWait = 0.50f;
    private const float TokenFlyDur    = 0.85f;
    private const float TokenStagger   = 0.12f;
    private const int   TokenCount     = 6;
    private const float HoldDur        = 1.20f;
    private const float FadeOutDur     = 0.30f;

    // ── Palette ───────────────────────────────────────────────────────────────

    private static readonly Color ColCard    = new Color(0.07f, 0.06f, 0.14f, 0.97f);
    private static readonly Color ColEdge    = new Color(0.25f, 0.60f, 1.00f, 0.85f);
    private static readonly Color ColLbl     = new Color(1.00f, 1.00f, 1.00f, 0.42f);
    private static readonly Color ColScore   = new Color(1.00f, 0.82f, 0.18f, 1.00f);
    private static readonly Color ColXP      = new Color(0.40f, 0.80f, 1.00f, 1.00f);
    private static readonly Color ColToken   = new Color(0.35f, 0.72f, 1.00f, 1.00f);
    private static readonly Color ColTitle   = new Color(1.00f, 1.00f, 1.00f, 0.55f);

    // ── Références ────────────────────────────────────────────────────────────

    private RectTransform _canvasRT;

    // ── Factory ───────────────────────────────────────────────────────────────

    public static MenuXPReceiver Create(RectTransform canvasRT)
    {
        var go = new GameObject("MenuXPReceiver");
        go.transform.SetParent(canvasRT, false);
        var r = go.AddComponent<MenuXPReceiver>();
        r._canvasRT = canvasRT;
        return r;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        if (!GameEndData.HasPending) return;

        int      score = GameEndData.FinalScore;
        int      xp    = GameEndData.XPEarned;
        GameEndData.Consume();

        StartCoroutine(Run(score, xp));
    }

    // ── Séquence principale ───────────────────────────────────────────────────

    private IEnumerator Run(int score, int xp)
    {
        yield return new WaitForSeconds(WaitAfterLoad);

        // ── 1. Construire la card de résultats ────────────────────────────────
        var (cardRT, cardGroup, xpLabelRT) = BuildResultCard(score, xp);
        cardRT.gameObject.SetActive(true);

        // Slide-in depuis le haut
        yield return StartCoroutine(SlideIn(cardRT, cardGroup));

        // Count-up XP dans la card
        yield return StartCoroutine(CountUpXP(xp, xpLabelRT));

        yield return new WaitForSeconds(PreTransferWait);

        // ── 2. Tokens XP → barre XP ──────────────────────────────────────────
        int xpBefore    = PlayerLevelManager.Instance?.CurrentXP ?? 0;
        int levelBefore = PlayerLevelManager.Instance?.Level     ?? 1;

        // Lancer les tokens en stagger
        int count = Mathf.Clamp(xp, 1, TokenCount);
        for (int i = 0; i < count; i++)
        {
            float delay = i * TokenStagger;
            StartCoroutine(FlyToken(xpLabelRT, delay));
        }

        // Créditer l'XP après le début du vol (~30% du trajet)
        yield return new WaitForSeconds(TokenFlyDur * 0.30f);

        PlayerLevelManager.EnsureExists();
        PlayerLevelManager.Instance.AddXP(xp);

        // Déclencher l'animation de la jauge
        var widget = FindFirstObjectByType<MenuXPWidget>();
        widget?.AnimateXPGain(xpBefore, PlayerLevelManager.Instance.CurrentXP, levelBefore);

        // Attendre la fin du vol
        yield return new WaitForSeconds(TokenFlyDur * 0.70f + TokenStagger * count);

        // ── 3. Fade-out de la card ────────────────────────────────────────────
        yield return new WaitForSeconds(HoldDur);
        yield return StartCoroutine(FadeGroup(cardGroup, 1f, 0f, FadeOutDur));
        Destroy(cardRT.gameObject);
    }

    // ── Animation card ────────────────────────────────────────────────────────

    private IEnumerator SlideIn(RectTransform rt, CanvasGroup group)
    {
        float cardH = rt.sizeDelta.y;
        rt.anchoredPosition = new Vector2(0f, cardH);
        group.alpha = 0f;

        float t = 0f;
        while (t < SlideInDur)
        {
            t += Time.deltaTime;
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / SlideInDur), 3f);
            rt.anchoredPosition = new Vector2(0f, Mathf.Lerp(cardH, 0f, e));
            group.alpha = e;
            yield return null;
        }
        rt.anchoredPosition = Vector2.zero;
        group.alpha = 1f;
    }

    private IEnumerator CountUpXP(int xp, RectTransform labelRT)
    {
        var lbl = labelRT?.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (lbl == null) yield break;

        float t = 0f;
        while (t < CountUpDur)
        {
            t += Time.deltaTime;
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / CountUpDur), 4f);
            int   v = Mathf.RoundToInt(Mathf.Lerp(0, xp, e));
            lbl.text = $"+{v} XP";
            yield return null;
        }
        lbl.text = $"+{xp} XP";
    }

    // ── Token XP volant ───────────────────────────────────────────────────────

    private IEnumerator FlyToken(RectTransform origin, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        var go = new GameObject("XPToken");
        go.transform.SetParent(_canvasRT, false);

        var img = go.AddComponent<Image>();
        img.sprite = SpriteGenerator.CreateCircle(32);
        img.color  = ColToken;
        img.raycastTarget = false;

        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(18f, 18f);

        // Départ : depuis la zone XP de la card avec bruit
        Vector2 startAP = ToCanvasPos(origin.TransformPoint(Vector3.zero))
                        + new Vector2(Random.Range(-50f, 50f), Random.Range(-15f, 15f));

        // Arrivée : centre de la barre XP du widget
        var widget = FindFirstObjectByType<MenuXPWidget>();
        Vector2 endAP = widget != null
            ? ToCanvasPos(widget.GetBarWorldCenter())
            : new Vector2(0f, (_canvasRT.rect.height * 0.5f) - 100f);

        // Arc de Bézier avec déviation aléatoire
        Vector2 ctrl = Vector2.Lerp(startAP, endAP, 0.45f)
                     + new Vector2(Random.Range(-130f, 130f), Random.Range(30f, 120f));

        float t = 0f;
        while (t < TokenFlyDur)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / TokenFlyDur);
            float e = 1f - Mathf.Pow(1f - n, 3f);

            // Bézier quadratique
            rt.anchoredPosition =
                Mathf.Pow(1f - e, 2f) * startAP
              + 2f * (1f - e) * e     * ctrl
              + e * e                 * endAP;

            // Fondu en arrivée
            float alpha = n > 0.70f ? Mathf.InverseLerp(1f, 0.70f, n) : 1f;
            img.color = new Color(ColToken.r, ColToken.g, ColToken.b, alpha);

            // Rotation + scale pulsé
            rt.localRotation = Quaternion.Euler(0f, 0f, e * 300f);
            float s = Mathf.Sin(n * Mathf.PI) * 1.3f + 0.35f;
            rt.localScale = Vector3.one * s;

            yield return null;
        }

        Destroy(go);
    }

    // ── Construction de la card ───────────────────────────────────────────────

    private (RectTransform rt, CanvasGroup group, RectTransform xpLblRT) BuildResultCard(int score, int xp)
    {
        const float W = 360f;
        const float H = 200f;

        var go = new GameObject("ResultCard");
        go.transform.SetParent(_canvasRT, false);
        go.SetActive(false);

        var group = go.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 1f);
        rt.anchorMax        = new Vector2(0.5f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.sizeDelta        = new Vector2(W, H);
        rt.anchoredPosition = new Vector2(0f, H);

        // Fond
        var bg = MakeImg(rt, "Bg", ColCard);
        Stretch(bg.rectTransform);

        // Accent bleu gauche
        var edge = MakeImg(rt, "Edge", ColEdge);
        var eRT = edge.rectTransform;
        eRT.anchorMin = new Vector2(0f, 0f); eRT.anchorMax = new Vector2(0f, 1f);
        eRT.pivot     = new Vector2(0f, 0.5f);
        eRT.sizeDelta = new Vector2(5f, 0f);
        eRT.anchoredPosition = Vector2.zero;

        // Titre
        var titleLbl = MakeLbl(rt, "Title", "RÉCOLTE D'XP",
            new Vector2(0.06f, 0.80f), new Vector2(0.95f, 1.00f),
            15f, ColTitle, TMPro.FontStyles.Bold);

        // Label score
        MakeLbl(rt, "ScoreLbl", "SCORE",
            new Vector2(0.06f, 0.54f), new Vector2(0.48f, 0.76f),
            13f, ColLbl, TMPro.FontStyles.Normal);
        MakeLbl(rt, "ScoreVal", score.ToString("N0"),
            new Vector2(0.06f, 0.28f), new Vector2(0.48f, 0.56f),
            30f, ColScore, TMPro.FontStyles.Bold);

        // Séparateur vertical
        var sep = MakeImg(rt, "Sep", new Color(1f, 1f, 1f, 0.08f));
        var sRT = sep.rectTransform;
        sRT.anchorMin = new Vector2(0.50f, 0.12f); sRT.anchorMax = new Vector2(0.502f, 0.86f);
        sRT.offsetMin = sRT.offsetMax = Vector2.zero;

        // Label XP (cible des tokens)
        MakeLbl(rt, "XPLbl", "XP GAGNÉ",
            new Vector2(0.54f, 0.54f), new Vector2(0.97f, 0.76f),
            13f, ColLbl, TMPro.FontStyles.Normal);

        var xpContainer = new GameObject("XPValContainer");
        xpContainer.transform.SetParent(rt, false);
        var xpRT = xpContainer.AddComponent<RectTransform>();
        xpRT.anchorMin = new Vector2(0.54f, 0.28f);
        xpRT.anchorMax = new Vector2(0.97f, 0.56f);
        xpRT.offsetMin = xpRT.offsetMax = Vector2.zero;

        MakeLbl(xpRT, "XPVal", "+0 XP",
            Vector2.zero, Vector2.one,
            28f, ColXP, TMPro.FontStyles.Bold);

        // Hint bas
        MakeLbl(rt, "Hint", "→ rechargement de la jauge XP",
            new Vector2(0.06f, 0.00f), new Vector2(0.95f, 0.24f),
            11f, ColLbl, TMPro.FontStyles.Italic);

        return (rt, group, xpRT);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Vector2 ToCanvasPos(Vector3 worldPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRT,
            RectTransformUtility.WorldToScreenPoint(null, worldPos),
            null,
            out Vector2 local);
        return local;
    }

    private static IEnumerator FadeGroup(CanvasGroup g, float from, float to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            g.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / dur));
            yield return null;
        }
        g.alpha = to;
    }

    private static Image MakeImg(Transform parent, string name, Color col)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = SpriteGenerator.CreateWhiteSquare();
        img.color  = col;
        img.raycastTarget = false;
        return img;
    }

    private static TMPro.TextMeshProUGUI MakeLbl(Transform parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax,
        float size, Color col, TMPro.FontStyles style)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var lbl = go.AddComponent<TMPro.TextMeshProUGUI>();
        lbl.text      = text;
        lbl.fontSize  = size;
        lbl.fontStyle = style;
        lbl.color     = col;
        lbl.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
        lbl.raycastTarget = false;
        MenuAssets.ApplyFont(lbl);
        var rt = lbl.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return lbl;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
