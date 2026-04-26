using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Widget centré en haut du menu affichant le solde de pièces persistant du joueur.
///
/// Feedbacks visuels :
///   - Jetons animés qui volent depuis le bord droit vers le widget (1 jeton par pièce, plafonné)
///   - Pulse de l'icône pièce + du montant à chaque réception
///   - Flash doré du fond du widget
///   - Compteur qui s'incrémente progressivement (roll-up)
///
/// Référence de résolution : 1080 × 1920 (portrait 9:16).
/// </summary>
public class CoinWalletWidget : MonoBehaviour
{
    // ── Layout ────────────────────────────────────────────────────────────────

    private const float WidgetW    = 340f;
    private const float WidgetH    = 96f;
    private const float MarginTop  = 48f;

    // ── Feedback ─────────────────────────────────────────────────────────────

    /// <summary>Nombre maximum de jetons visuels envoyés par lot.</summary>
    private const int   MaxCoinTokens    = 8;
    private const float CoinTokenSize    = 28f;
    private const float CoinFlightMin    = 0.55f;
    private const float CoinFlightMax    = 0.85f;
    private const float PulseDuration    = 0.30f;
    private const float PulseScalePeak   = 1.35f;
    private const float FlashDuration    = 0.45f;
    private const float RollUpSpeed      = 60f;   // pts par seconde

    // ── Palette ───────────────────────────────────────────────────────────────

    private static readonly Color ColBg         = new Color(0.04f, 0.04f, 0.08f, 0.85f);
    private static readonly Color ColBgFlash    = new Color(0.55f, 0.42f, 0.05f, 0.95f);
    private static readonly Color ColLabel      = new Color(1f, 1f, 1f, 0.45f);
    private static readonly Color ColCoin       = new Color(1.00f, 0.82f, 0.18f, 1f);
    private static readonly Color ColCoinDark   = new Color(0.75f, 0.58f, 0.06f, 1f);
    private static readonly Color ColToken      = new Color(1.00f, 0.82f, 0.18f, 1f);
    private static readonly Color ColTokenEdge  = new Color(0.80f, 0.55f, 0.00f, 1f);

    // ── Références runtime ────────────────────────────────────────────────────

    private RectTransform   widgetRT;
    private Image           widgetBg;
    private RectTransform   iconRT;
    private TextMeshProUGUI valueLabel;

    // Pool de jetons volants
    private readonly List<RectTransform> tokenPool = new List<RectTransform>();
    private RectTransform   tokenContainer;

    // État roll-up
    private float   displayedCoins;
    private int     targetCoins;
    private bool    isRollingUp;

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>Crée et initialise le widget dans le canvas fourni.</summary>
    public static CoinWalletWidget Create(RectTransform canvasRT)
    {
        var go = new GameObject("CoinWalletWidget");
        go.transform.SetParent(canvasRT, false);

        // RectTransform couvrant tout le canvas (pour que les jetons volants aient de l'espace)
        var rt       = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var widget = go.AddComponent<CoinWalletWidget>();
        widget.Build(rt, canvasRT);
        return widget;
    }

    // ── Construction ──────────────────────────────────────────────────────────

    private void Build(RectTransform root, RectTransform canvasRT)
    {
        // Conteneur des jetons volants (au-dessus des autres éléments)
        var tcGO       = new GameObject("TokenContainer");
        tcGO.transform.SetParent(root, false);
        tokenContainer = tcGO.AddComponent<RectTransform>();
        tokenContainer.anchorMin = Vector2.zero;
        tokenContainer.anchorMax = Vector2.one;
        tokenContainer.offsetMin = tokenContainer.offsetMax = Vector2.zero;

        // Pool de jetons
        for (int i = 0; i < MaxCoinTokens; i++)
            tokenPool.Add(MakeCoinToken(tokenContainer));

        // Widget fond centré haut
        var bgGO   = new GameObject("CoinBar");
        bgGO.transform.SetParent(root, false);

        widgetBg   = bgGO.AddComponent<Image>();
        widgetBg.sprite = SpriteGenerator.CreateWhiteSquare();
        widgetBg.color  = ColBg;
        widgetBg.raycastTarget = false;

        widgetRT   = widgetBg.rectTransform;
        widgetRT.anchorMin      = new Vector2(0.5f, 1f);
        widgetRT.anchorMax      = new Vector2(0.5f, 1f);
        widgetRT.pivot          = new Vector2(0.5f, 1f);
        widgetRT.sizeDelta      = new Vector2(WidgetW, WidgetH);
        widgetRT.anchoredPosition = new Vector2(0f, -MarginTop);

        // Icône pièce (disque doré)
        var iconGO  = new GameObject("CoinIcon");
        iconGO.transform.SetParent(bgGO.transform, false);

        var iconImg = iconGO.AddComponent<Image>();
        iconImg.sprite = SpriteGenerator.CreateWhiteSquare();
        iconImg.color  = ColCoin;
        iconImg.raycastTarget = false;

        iconRT = iconImg.rectTransform;
        iconRT.anchorMin = new Vector2(0f, 0.5f);
        iconRT.anchorMax = new Vector2(0f, 0.5f);
        iconRT.pivot     = new Vector2(0f, 0.5f);
        iconRT.sizeDelta = new Vector2(42f, 42f);
        iconRT.anchoredPosition = new Vector2(20f, 0f);

        // Reflet sur l'icône pièce
        var glintGO = new GameObject("CoinGlint");
        glintGO.transform.SetParent(iconGO.transform, false);
        var glintImg = glintGO.AddComponent<Image>();
        glintImg.sprite = SpriteGenerator.CreateWhiteSquare();
        glintImg.color  = new Color(1f, 1f, 1f, 0.35f);
        glintImg.raycastTarget = false;
        var glintRT = glintImg.rectTransform;
        glintRT.anchorMin = new Vector2(0.5f, 0.55f);
        glintRT.anchorMax = new Vector2(0.85f, 0.90f);
        glintRT.offsetMin = glintRT.offsetMax = Vector2.zero;

        // Label "PIÈCES"
        MakeLabel("CoinsLabel", bgGO.transform,
            "PIÈCES",
            anchorMin: new Vector2(0.22f, 0.55f), anchorMax: new Vector2(0.95f, 1f),
            size: 18f, color: ColLabel);

        // Valeur numérique
        var valGO  = new GameObject("CoinsValue");
        valGO.transform.SetParent(bgGO.transform, false);

        var valTmp = valGO.AddComponent<TextMeshProUGUI>();
        valTmp.text       = "0";
        valTmp.fontSize   = 40f;
        valTmp.fontStyle  = FontStyles.Bold;
        valTmp.color      = ColCoin;
        valTmp.alignment  = TextAlignmentOptions.BottomLeft;
        valTmp.raycastTarget = false;

        var valRT  = valTmp.rectTransform;
        valRT.anchorMin = new Vector2(0.22f, 0f);
        valRT.anchorMax = new Vector2(0.95f, 0.65f);
        valRT.offsetMin = new Vector2(0f, 4f);
        valRT.offsetMax = Vector2.zero;

        valueLabel = valTmp;

        // Abonnement au ScoreManager
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnCoinsAdded += OnCoinsAdded;

        // Initialiser l'affichage avec le solde actuel
        int current    = ScoreManager.Instance != null ? ScoreManager.Instance.GetTotalCoins() : 0;
        displayedCoins = current;
        targetCoins    = current;
        RefreshLabel();
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnCoinsAdded += OnCoinsAdded;
    }

    private void OnDisable()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnCoinsAdded -= OnCoinsAdded;
    }

    private void OnDestroy()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnCoinsAdded -= OnCoinsAdded;
    }

    private void Update()
    {
        if (!isRollingUp) return;

        displayedCoins = Mathf.MoveTowards(displayedCoins, targetCoins, RollUpSpeed * Time.deltaTime);
        RefreshLabel();

        if (Mathf.Approximately(displayedCoins, targetCoins))
        {
            displayedCoins = targetCoins;
            isRollingUp    = false;
            RefreshLabel();
        }
    }

    // ── Réception de pièces ───────────────────────────────────────────────────

    private void OnCoinsAdded(int amount, int newTotal)
    {
        // Guard : le widget peut être détruit avant que ScoreManager n'ait fini d'émettre
        if (this == null) return;

        targetCoins = newTotal;
        isRollingUp = true;

        // Jetons volants
        int tokenCount = Mathf.Clamp(amount, 1, MaxCoinTokens);
        StartCoroutine(LaunchTokens(tokenCount));

        // Pulse + Flash
        StopCoroutine(nameof(PulseIcon));
        StopCoroutine(nameof(FlashBackground));
        StartCoroutine(PulseIcon());
        StartCoroutine(FlashBackground());
    }

    // ── Coroutines de feedback ────────────────────────────────────────────────

    private IEnumerator LaunchTokens(int count)
    {
        for (int i = 0; i < count; i++)
        {
            // Trouver un jeton disponible dans le pool
            var token = GetFreeToken();
            if (token == null) break;

            // Position de départ : bord droit, hauteur aléatoire dans le tiers bas-centre
            float startX = 600f;
            float startY = Random.Range(-400f, 200f);
            token.anchoredPosition = new Vector2(startX, startY);
            token.localScale       = Vector3.one;
            token.gameObject.SetActive(true);

            StartCoroutine(FlyToken(token));

            yield return new WaitForSeconds(Random.Range(0.04f, 0.12f));
        }
    }

    private IEnumerator FlyToken(RectTransform token)
    {
        float duration = Random.Range(CoinFlightMin, CoinFlightMax);
        float elapsed  = 0f;

        Vector2 startPos = token.anchoredPosition;
        Vector2 endPos   = widgetRT.anchoredPosition + new Vector2(Random.Range(-20f, 20f), -WidgetH * 0.5f);

        // Point de contrôle pour un arc naturel
        Vector2 control = Vector2.Lerp(startPos, endPos, 0.5f) + new Vector2(Random.Range(-80f, 80f), Random.Range(80f, 200f));

        var img = token.GetComponent<Image>();

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.Clamp01(elapsed / duration);
            float e  = 1f - Mathf.Pow(1f - t, 3f);   // EaseOutCubic

            // Courbe de Bézier quadratique
            Vector2 pos = Mathf.Pow(1f - e, 2f) * startPos
                        + 2f * (1f - e) * e * control
                        + e * e * endPos;

            token.anchoredPosition = pos;

            // Fade out sur le dernier 25%
            float alpha = t > 0.75f ? Mathf.Lerp(1f, 0f, (t - 0.75f) / 0.25f) : 1f;
            img.color   = new Color(ColToken.r, ColToken.g, ColToken.b, alpha);

            // Léger spin
            token.localRotation = Quaternion.Euler(0f, 0f, e * 360f);

            yield return null;
        }

        token.gameObject.SetActive(false);
        img.color = ColToken;
        token.localRotation = Quaternion.identity;
    }

    private IEnumerator PulseIcon()
    {
        float elapsed = 0f;
        Vector3 baseScale = Vector3.one;

        while (elapsed < PulseDuration)
        {
            elapsed += Time.deltaTime;
            float t  = elapsed / PulseDuration;
            float s  = t < 0.5f
                       ? Mathf.Lerp(1f, PulseScalePeak, t * 2f)
                       : Mathf.Lerp(PulseScalePeak, 1f, (t - 0.5f) * 2f);

            if (iconRT != null)
                iconRT.localScale = baseScale * s;

            yield return null;
        }

        if (iconRT != null)
            iconRT.localScale = baseScale;
    }

    private IEnumerator FlashBackground()
    {
        float elapsed = 0f;

        while (elapsed < FlashDuration)
        {
            elapsed += Time.deltaTime;
            float t  = elapsed / FlashDuration;
            float e  = t < 0.3f
                       ? Mathf.Lerp(0f, 1f, t / 0.3f)
                       : Mathf.Lerp(1f, 0f, (t - 0.3f) / 0.7f);

            if (widgetBg != null)
                widgetBg.color = Color.Lerp(ColBg, ColBgFlash, e);

            yield return null;
        }

        if (widgetBg != null)
            widgetBg.color = ColBg;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void RefreshLabel()
    {
        if (valueLabel != null)
            valueLabel.text = Mathf.FloorToInt(displayedCoins).ToString("N0");
    }

    private RectTransform GetFreeToken()
    {
        foreach (var t in tokenPool)
            if (!t.gameObject.activeSelf) return t;
        return null;
    }

    private static RectTransform MakeCoinToken(RectTransform parent)
    {
        var go  = new GameObject("CoinToken");
        go.transform.SetParent(parent, false);
        go.SetActive(false);

        var img    = go.AddComponent<Image>();
        img.sprite = SpriteGenerator.CreateWhiteSquare();
        img.color  = ColToken;
        img.raycastTarget = false;

        var rt     = img.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(CoinTokenSize, CoinTokenSize);

        return rt;
    }

    private static void MakeLabel(string name, Transform parent,
        string text, Vector2 anchorMin, Vector2 anchorMax,
        float size, Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.BottomLeft;
        tmp.raycastTarget = false;

        var rt   = tmp.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = new Vector2(0f, 0f);
        rt.offsetMax = Vector2.zero;
    }
}
