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
    private const float CoinCountDur   = 0.70f;
    private const float PreTransferWait= 0.55f;
    private const float TransferDur    = 0.90f;
    private const float HoldAfterDur   = 1.60f;
    private const float FadeOutDur     = 0.35f;

    // Pulse du score
    private const float PulseDur       = 0.18f;
    private const float PulseScale     = 1.22f;

    // ── Palette ───────────────────────────────────────────────────────────────

    private static readonly Color ColCard     = new Color(0.07f, 0.06f, 0.13f, 0.97f);
    private static readonly Color ColEdge     = new Color(0.95f, 0.15f, 0.15f, 0.85f);
    private static readonly Color ColBorder   = new Color(1.00f, 1.00f, 1.00f, 0.10f);
    private static readonly Color ColSub      = new Color(1.00f, 1.00f, 1.00f, 0.35f);
    private static readonly Color ColScoreVal = new Color(1.00f, 0.82f, 0.18f, 1.00f);
    private static readonly Color ColCoinsVal = new Color(0.30f, 0.95f, 0.45f, 1.00f);
    private static readonly Color ColToken    = new Color(1.00f, 0.82f, 0.18f, 1.00f);
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
        int      coins    = GameEndData.CoinsEarned;
        GameType gameType = GameEndData.GameType;
        GameEndData.Consume();

        StartCoroutine(Run(score, coins, gameType));
    }

    // ── Séquence principale ───────────────────────────────────────────────────

    private IEnumerator Run(int score, int coins, GameType gameType)
    {
        // Attendre que CoinWalletWidget et QuestManager soient abonnés
        yield return null;
        yield return null;
        yield return new WaitForSeconds(WaitAfterLoad);

        // ── Notifier QuestManager avec le bon GameType ────────────────────────
        // ScoreManager.AddScoreOnly a déjà émis OnScoreAdded dans la scène de jeu,
        // mais si QuestManager n'existait pas à ce moment, on le notifie maintenant
        // via un AddScoreOnly supplémentaire — le QuestManager est abonné ici.
        // On évite le double crédit de pièces en utilisant AddScoreOnly (pas AddScore).
        if (QuestManager.Instance != null && score > 0)
        {
            // OnScoreAdded est déjà déclenché — QuestManager l'a reçu si présent.
            // Si QuestManager a été créé après AddScoreOnly (ex. démarrage direct sur scène de jeu),
            // on force une notification manuelle.
            ScoreManager.EnsureExists();
        }

        // ── 1. Construire et slide-in la card ─────────────────────────────────
        var (cardRT, cardGroup, scoreVal, coinsVal) = BuildCard();
        cardRT.gameObject.SetActive(true);

        yield return StartCoroutine(SlideIn(cardRT, cardGroup, SlideInDur));

        // ── 2. Count-up score + pulse à la fin ────────────────────────────────
        yield return StartCoroutine(CountUp(scoreVal, 0, score, ScoreCountDur, isCoin: false));
        StartCoroutine(Pulse(scoreVal.rectTransform));

        // ── 3. Count-up pièces ────────────────────────────────────────────────
        yield return new WaitForSeconds(0.30f);
        yield return StartCoroutine(CountUp(coinsVal, 0, coins, CoinCountDur, isCoin: true));

        // ── 4. Pause avant le transfert ───────────────────────────────────────
        yield return new WaitForSeconds(PreTransferWait);

        // ── 5. Tokens depuis le label pièces → wallet + crédit ────────────────
        yield return StartCoroutine(TransferCoins(coinsVal.rectTransform, coins));

        // ── 6. Hold ───────────────────────────────────────────────────────────
        yield return new WaitForSeconds(HoldAfterDur);

        // ── 7. Fade-out ───────────────────────────────────────────────────────
        yield return StartCoroutine(Fade(cardGroup, 1f, 0f, FadeOutDur));
        Destroy(cardRT.gameObject);
    }

    // ── Transfert de pièces ───────────────────────────────────────────────────

    private IEnumerator TransferCoins(RectTransform coinsLabelRT, int coins)
    {
        int count = Mathf.Clamp(coins, 1, 8);

        // Lancer les tokens en cascade depuis le label pièces
        for (int i = 0; i < count; i++)
        {
            float delay = i * (TransferDur / count) * 0.55f;
            StartCoroutine(FlyToken(coinsLabelRT, delay));
        }

        // Créditer quand les premiers tokens atteignent le wallet (~30% du vol)
        yield return new WaitForSeconds(TransferDur * 0.30f);

        ScoreManager.EnsureExists();
        ScoreManager.Instance.AddCoins(coins);

        yield return new WaitForSeconds(TransferDur * 0.70f);
    }

    /// <summary>
    /// Anime un jeton doré depuis le label de pièces jusqu'au widget wallet en haut.
    /// Chaque token part d'un point légèrement aléatoire autour du centre du label,
    /// suit un arc de Bézier et se dissout en arrivant.
    /// </summary>
    private IEnumerator FlyToken(RectTransform origin, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        var go  = new GameObject("CoinToken");
        go.transform.SetParent(canvasRT, false);

        var img = go.AddComponent<Image>();
        img.sprite = SpriteGenerator.CreateCircle(32);
        img.color  = ColToken;
        img.raycastTarget = false;

        var rt   = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(24f, 24f);

        // Départ : centre du label pièces avec léger bruit
        Vector2 startAP = WorldToCanvasAnchoredPos(origin.TransformPoint(Vector3.zero))
                        + new Vector2(Random.Range(-40f, 40f), Random.Range(-20f, 20f));

        // Arrivée : wallet en haut du canvas (anchorY ≈ 0.93)
        Vector2 endAP = new Vector2(
            Random.Range(-60f, 60f),
            (0.93f - 0.5f) * canvasRT.rect.height);

        // Point de contrôle pour arc Bézier — déviation latérale + légère montée
        Vector2 ctrl = Vector2.Lerp(startAP, endAP, 0.40f)
                     + new Vector2(Random.Range(-180f, 180f), Random.Range(40f, 160f));

        float t = 0f;
        while (t < TransferDur)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / TransferDur);
            float e = 1f - Mathf.Pow(1f - n, 3f);  // EaseOutCubic

            // Bézier quadratique
            rt.anchoredPosition =
                  Mathf.Pow(1f - e, 2f) * startAP
                + 2f * (1f - e) * e     * ctrl
                + e * e                 * endAP;

            // Fondu en approche du wallet (derniers 28%)
            float alpha = n > 0.72f ? Mathf.InverseLerp(1f, 0.72f, n) : 1f;
            img.color   = new Color(ColToken.r, ColToken.g, ColToken.b, alpha);

            // Légère rotation
            rt.localRotation = Quaternion.Euler(0f, 0f, e * 340f);

            // Scale : part petit, grossit rapidement, rétrécit en arrivant
            float s = Mathf.Sin(n * Mathf.PI) * 1.4f + 0.3f;
            rt.localScale = Vector3.one * s;

            yield return null;
        }

        Destroy(go);
    }

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

    private (RectTransform rt, CanvasGroup group,
             TextMeshProUGUI scoreVal, TextMeshProUGUI coinsVal) BuildCard()
    {
        const float W = 900f;
        const float H = 220f;

        // Racine (hors écran en haut → slide-in)
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
        rt.anchoredPosition = new Vector2(0f, H);  // hors écran initialement

        // Fond
        RawImg(rt, "Bg", ColCard, stretch: true);

        // Accent gauche rouge defeat
        var accent   = new GameObject("Accent");
        accent.transform.SetParent(rt, false);
        var accentImg = accent.AddComponent<Image>();
        accentImg.sprite = SpriteGenerator.CreateWhiteSquare();
        accentImg.color  = ColEdge;
        accentImg.raycastTarget = false;
        var accentRT = accentImg.rectTransform;
        accentRT.anchorMin = new Vector2(0f, 0f);
        accentRT.anchorMax = new Vector2(0.008f, 1f);
        accentRT.offsetMin = accentRT.offsetMax = Vector2.zero;

        // Bordure extérieure subtile
        var border = new GameObject("Border");
        border.transform.SetParent(rt, false);
        var borderImg = border.AddComponent<Image>();
        borderImg.sprite = SpriteGenerator.CreateWhiteSquare();
        borderImg.color  = ColBorder;
        borderImg.raycastTarget = false;
        var borderRT = borderImg.rectTransform;
        borderRT.anchorMin = Vector2.zero;
        borderRT.anchorMax = Vector2.one;
        borderRT.offsetMin = new Vector2(-1f, -1f);
        borderRT.offsetMax = new Vector2( 1f,  1f);
        border.transform.SetAsFirstSibling();

        // Titre
        Lbl(rt, "Title", "RÉSULTATS DE LA PARTIE",
            new Vector2(0.03f, 0.68f), new Vector2(0.97f, 1.0f),
            22f, ColSub, FontStyles.Bold);

        // Séparateur
        var sep = new GameObject("Sep");
        sep.transform.SetParent(rt, false);
        var sepImg = sep.AddComponent<Image>();
        sepImg.sprite = SpriteGenerator.CreateWhiteSquare();
        sepImg.color  = new Color(1f, 1f, 1f, 0.08f);
        sepImg.raycastTarget = false;
        var sepRT = sepImg.rectTransform;
        sepRT.anchorMin = new Vector2(0.02f, 0.67f);
        sepRT.anchorMax = new Vector2(0.98f, 0.675f);
        sepRT.offsetMin = sepRT.offsetMax = Vector2.zero;

        // Labels score
        Lbl(rt, "ScoreLbl", "SCORE",
            new Vector2(0.04f, 0.35f), new Vector2(0.46f, 0.65f),
            20f, ColLbl, FontStyles.Normal);
        var scoreVal = Lbl(rt, "ScoreVal", "0",
            new Vector2(0.04f, 0.02f), new Vector2(0.46f, 0.40f),
            64f, ColScoreVal, FontStyles.Bold);

        // Labels pièces
        Lbl(rt, "CoinsLbl", "PIÈCES GAGNÉES",
            new Vector2(0.54f, 0.35f), new Vector2(0.97f, 0.65f),
            20f, ColLbl, FontStyles.Normal);
        var coinsVal = Lbl(rt, "CoinsVal", "0 🪙",
            new Vector2(0.54f, 0.02f), new Vector2(0.97f, 0.40f),
            56f, ColCoinsVal, FontStyles.Bold);

        // Séparateur vertical
        var vsep = new GameObject("VSep");
        vsep.transform.SetParent(rt, false);
        var vsepImg = vsep.AddComponent<Image>();
        vsepImg.sprite = SpriteGenerator.CreateWhiteSquare();
        vsepImg.color  = new Color(1f, 1f, 1f, 0.07f);
        vsepImg.raycastTarget = false;
        var vsepRT = vsepImg.rectTransform;
        vsepRT.anchorMin = new Vector2(0.498f, 0.05f);
        vsepRT.anchorMax = new Vector2(0.502f, 0.62f);
        vsepRT.offsetMin = vsepRT.offsetMax = Vector2.zero;

        return (rt, group, scoreVal, coinsVal);
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

    private static IEnumerator CountUp(TextMeshProUGUI lbl, int from, int to, float dur, bool isCoin)
    {
        if (lbl == null) yield break;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / dur), 4f);
            int   v = Mathf.RoundToInt(Mathf.Lerp(from, to, e));
            lbl.text = isCoin ? $"+{v} 🪙" : v.ToString("N0");
            yield return null;
        }
        lbl.text = isCoin ? $"+{to} 🪙" : to.ToString("N0");
    }
}
