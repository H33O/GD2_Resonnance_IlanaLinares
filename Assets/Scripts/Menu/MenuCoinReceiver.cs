using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Traite les pièces en attente depuis <see cref="GameEndData"/> au retour dans la scène Menu.
///
/// Séquence :
///   1. Affiche une mini-card "Score de la partie" en haut du canvas.
///   2. Après un bref délai, les pièces "glissent" dans le <see cref="CoinWalletWidget"/>
///      via <see cref="ScoreManager.AddCoins"/> qui déclenche son propre roll-up + jetons.
///   3. Pulse la barre du wallet pour attirer l'attention.
///
/// Créé et géré par <see cref="MenuMainSetup"/>.
/// </summary>
public class MenuCoinReceiver : MonoBehaviour
{
    // ── Timings ───────────────────────────────────────────────────────────────

    private const float AppearDelay    = 0.60f;   // délai après le chargement du menu
    private const float FadeInDur      = 0.30f;
    private const float ScoreCountDur  = 1.00f;
    private const float PreCoinDelay   = 0.70f;   // pause avant d'envoyer les pièces
    private const float CoinAnimDur    = 0.80f;   // durée de l'anim de transfert visuel
    private const float HoldDur        = 1.80f;   // durée d'affichage après réception
    private const float FadeOutDur     = 0.40f;

    // ── Palette ───────────────────────────────────────────────────────────────

    private static readonly Color ColCard        = new Color(0.07f, 0.06f, 0.12f, 0.96f);
    private static readonly Color ColBorder      = new Color(1f, 0.82f, 0.18f, 0.60f);
    private static readonly Color ColTitle       = new Color(1f, 1f, 1f, 0.40f);
    private static readonly Color ColScore       = new Color(1f, 0.82f, 0.18f, 1f);
    private static readonly Color ColCoins       = new Color(0.35f, 0.95f, 0.50f, 1f);
    private static readonly Color ColToken       = new Color(1f, 0.82f, 0.18f, 1f);

    // ── Construction ──────────────────────────────────────────────────────────

    /// <summary>
    /// Initialise le receiver. Appeler depuis <see cref="MenuMainSetup"/> après la création du canvas.
    /// </summary>
    public static MenuCoinReceiver Create(RectTransform canvasRT)
    {
        var go = new GameObject("MenuCoinReceiver");
        go.transform.SetParent(canvasRT, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var comp = go.AddComponent<MenuCoinReceiver>();
        comp.canvasRT = canvasRT;
        return comp;
    }

    // ── Références ────────────────────────────────────────────────────────────

    private RectTransform canvasRT;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        if (!GameEndData.HasPending) return;

        int finalScore = GameEndData.FinalScore;
        int coins      = GameEndData.CoinsEarned;
        GameEndData.Consume();

        StartCoroutine(RunSequence(finalScore, coins));
    }

    // ── Séquence ──────────────────────────────────────────────────────────────

    private IEnumerator RunSequence(int finalScore, int coins)
    {
        // Attendre 2 frames : CoinWalletWidget s'abonne à OnCoinsAdded dans OnEnable
        // qui tourne au même frame que Start → on laisse passer un cycle complet.
        yield return null;
        yield return null;

        yield return new WaitForSeconds(AppearDelay);

        // ── 1. Construire la card ─────────────────────────────────────────────
        var (cardRT, group, scoreLabel, coinsLabel) = BuildCard();
        cardRT.gameObject.SetActive(true);

        // Fade in
        yield return StartCoroutine(FadeGroup(group, 0f, 1f, FadeInDur));

        // ── 2. Compteur score ─────────────────────────────────────────────────
        yield return StartCoroutine(CountUpLabel(scoreLabel, 0, finalScore, ScoreCountDur, isCoin: false));

        // ── 3. Pause puis envoi des pièces ────────────────────────────────────
        yield return new WaitForSeconds(PreCoinDelay);

        // Mettre à jour le label pièces
        coinsLabel.text = $"+{coins} 🪙";

        // Lancer l'animation de transfert visuel (tokens qui volent vers le wallet)
        // Puis appeler AddCoins qui déclenche le roll-up + flash du CoinWalletWidget
        yield return StartCoroutine(AnimateCoinTransfer(cardRT, coins, CoinAnimDur));

        // ── 4. Hold ───────────────────────────────────────────────────────────
        yield return new WaitForSeconds(HoldDur);

        // ── 5. Fade out et destruction ────────────────────────────────────────
        yield return StartCoroutine(FadeGroup(group, 1f, 0f, FadeOutDur));
        Destroy(cardRT.gameObject);
    }

    // ── Animation de transfert ────────────────────────────────────────────────

    private IEnumerator AnimateCoinTransfer(RectTransform cardRT, int coins, float duration)
    {
        int tokenCount = Mathf.Clamp(coins, 1, 8);
        var walletTargetAnchor = new Vector2(0.5f, 0.95f);

        for (int i = 0; i < tokenCount; i++)
        {
            float delay = i * (duration / tokenCount) * 0.6f;
            StartCoroutine(FlyToken(cardRT, walletTargetAnchor, delay, duration));
        }

        // Créditer les pièces au moment où les premiers tokens arrivent au wallet
        yield return new WaitForSeconds(duration * 0.3f);

        ScoreManager.EnsureExists();
        ScoreManager.Instance.AddCoins(coins);

        yield return new WaitForSeconds(duration * 0.7f);
    }

    private IEnumerator FlyToken(RectTransform origin, Vector2 targetAnchor, float delay, float flightDur)
    {
        yield return new WaitForSeconds(delay);

        var tokenGO = new GameObject("FlyToken");
        tokenGO.transform.SetParent(canvasRT, false);

        var img    = tokenGO.AddComponent<Image>();
        img.sprite = SpriteGenerator.CreateWhiteSquare();
        img.color  = ColToken;
        img.raycastTarget = false;

        var rt     = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(22f, 22f);

        // Départ : position de la card en coordonnées canvas
        Vector3 startWorld  = origin.TransformPoint(Vector3.zero);
        Vector3 endWorld    = new Vector3(
            canvasRT.rect.width  * targetAnchor.x,
            canvasRT.rect.height * targetAnchor.y, 0f);

        // Convertir en anchoredPosition locale du canvas
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT,
            RectTransformUtility.WorldToScreenPoint(null, startWorld),
            null,
            out Vector2 startAP);

        Vector2 endAP = new Vector2(
            (targetAnchor.x - 0.5f) * canvasRT.rect.width,
            (targetAnchor.y - 0.5f) * canvasRT.rect.height);

        Vector2 ctrl = Vector2.Lerp(startAP, endAP, 0.5f)
                     + new Vector2(Random.Range(-120f, 120f), Random.Range(80f, 240f));

        float elapsed = 0f;
        while (elapsed < flightDur)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.Clamp01(elapsed / flightDur);
            float e  = 1f - Mathf.Pow(1f - t, 3f);   // EaseOutCubic

            rt.anchoredPosition = Mathf.Pow(1f - e, 2f) * startAP
                                + 2f * (1f - e) * e * ctrl
                                + e * e * endAP;

            float alpha = t > 0.75f ? Mathf.Lerp(1f, 0f, (t - 0.75f) / 0.25f) : 1f;
            img.color   = new Color(ColToken.r, ColToken.g, ColToken.b, alpha);

            rt.localRotation = Quaternion.Euler(0f, 0f, e * 360f);
            yield return null;
        }

        Destroy(tokenGO);
    }

    // ── Construction de la card ───────────────────────────────────────────────

    private (RectTransform card, CanvasGroup group,
             TextMeshProUGUI scoreLabel, TextMeshProUGUI coinsLabel) BuildCard()
    {
        const float CardW = 720f;
        const float CardH = 260f;

        // Racine card (haut-centre, juste sous le wallet)
        var cardGO  = new GameObject("ScoreCard");
        cardGO.transform.SetParent(canvasRT, false);
        cardGO.SetActive(false);

        var cardGroup = cardGO.AddComponent<CanvasGroup>();
        cardGroup.alpha = 0f;

        var cardRT  = cardGO.AddComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0.5f, 1f);
        cardRT.anchorMax = new Vector2(0.5f, 1f);
        cardRT.pivot     = new Vector2(0.5f, 1f);
        cardRT.sizeDelta = new Vector2(CardW, CardH);
        cardRT.anchoredPosition = new Vector2(0f, -180f);  // sous le CoinWalletWidget

        // Fond bordure
        var borderGO  = new GameObject("Border");
        borderGO.transform.SetParent(cardRT, false);
        var borderImg = borderGO.AddComponent<Image>();
        borderImg.sprite = SpriteGenerator.CreateWhiteSquare();
        borderImg.color  = ColBorder;
        borderImg.raycastTarget = false;
        var borderRT = borderImg.rectTransform;
        borderRT.anchorMin = Vector2.zero;
        borderRT.anchorMax = Vector2.one;
        borderRT.offsetMin = new Vector2(-3f, -3f);
        borderRT.offsetMax = new Vector2( 3f,  3f);

        // Fond card
        var bgGO  = new GameObject("Bg");
        bgGO.transform.SetParent(cardRT, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.sprite = SpriteGenerator.CreateWhiteSquare();
        bgImg.color  = ColCard;
        bgImg.raycastTarget = false;
        var bgRT = bgImg.rectTransform;
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

        // Titre
        MakeLabel(cardRT, "Title", "PARTIE TERMINÉE",
            new Vector2(0.05f, 0.72f), new Vector2(0.95f, 1f),
            26f, ColTitle, bold: true);

        // Score
        MakeLabel(cardRT, "ScoreLbl", "SCORE",
            new Vector2(0.05f, 0.50f), new Vector2(0.48f, 0.72f),
            22f, ColTitle, bold: false);

        var scoreLabel = MakeLabel(cardRT, "ScoreVal", "0",
            new Vector2(0.05f, 0.22f), new Vector2(0.48f, 0.54f),
            56f, ColScore, bold: true);

        // Pièces
        MakeLabel(cardRT, "CoinsLbl", "PIÈCES",
            new Vector2(0.52f, 0.50f), new Vector2(0.95f, 0.72f),
            22f, ColTitle, bold: false);

        var coinsLabel = MakeLabel(cardRT, "CoinsVal", "0 🪙",
            new Vector2(0.52f, 0.22f), new Vector2(0.95f, 0.54f),
            56f, ColCoins, bold: true);

        // Séparateur vertical
        var sepGO  = new GameObject("VSep");
        sepGO.transform.SetParent(cardRT, false);
        var sepImg = sepGO.AddComponent<Image>();
        sepImg.sprite = SpriteGenerator.CreateWhiteSquare();
        sepImg.color  = new Color(1f, 1f, 1f, 0.10f);
        sepImg.raycastTarget = false;
        var sepRT = sepImg.rectTransform;
        sepRT.anchorMin = new Vector2(0.495f, 0.1f);
        sepRT.anchorMax = new Vector2(0.505f, 0.9f);
        sepRT.offsetMin = sepRT.offsetMax = Vector2.zero;

        return (cardRT, cardGroup, scoreLabel, coinsLabel);
    }

    // ── Helpers UI ────────────────────────────────────────────────────────────

    private static TextMeshProUGUI MakeLabel(RectTransform parent,
        string name, string text,
        Vector2 anchorMin, Vector2 anchorMax,
        float size, Color color, bool bold)
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
        var rt = tmp.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return tmp;
    }

    // ── Coroutines utilitaires ────────────────────────────────────────────────

    private static IEnumerator FadeGroup(CanvasGroup group, float from, float to, float dur)
    {
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed    += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / dur));
            yield return null;
        }
        group.alpha = to;
    }

    private static IEnumerator CountUpLabel(TextMeshProUGUI label,
        int from, int to, float dur, bool isCoin)
    {
        if (label == null) yield break;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t  = Mathf.Clamp01(elapsed / dur);
            float e  = 1f - Mathf.Pow(1f - t, 4f);
            int   v  = Mathf.RoundToInt(Mathf.Lerp(from, to, e));
            label.text = isCoin ? $"+{v} 🪙" : v.ToString("N0");
            yield return null;
        }
        label.text = isCoin ? $"+{to} 🪙" : to.ToString("N0");
    }
}
