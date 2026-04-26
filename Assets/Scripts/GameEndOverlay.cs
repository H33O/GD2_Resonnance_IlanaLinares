using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Overlay de fin de partie affiché par-dessus la scène de jeu courante.
///
/// Cycle :
///   1. S'abonne à <see cref="GameManager.OnGameOver"/> au démarrage.
///   2. À la fin de partie, calcule le score et les pièces gagnées,
///      persiste via <see cref="ScoreManager"/> et écrit dans <see cref="GameEndData"/>.
///   3. Affiche une card animée (score + pièces) avec countdown et bouton "Menu".
///   4. Lance la transition vers la scène Menu via <see cref="SceneTransition"/>.
///
/// Attacher à un GameObject vide dans chaque scène de mini-jeu
/// (remplace ou complète le <see cref="GAWSessionBridge"/>).
/// </summary>
public class GameEndOverlay : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Jeu")]
    [Tooltip("Type de jeu pour la persistance du score dans ScoreManager.")]
    [SerializeField] private GameType gameType = GameType.GameAndWatch;

    [Tooltip("Passer vrai pour aller au Menu directement, faux pour passer par l'Overworld.")]
    [SerializeField] private bool returnToMenu = true;

    [Header("Timings")]
    [SerializeField] private float fadeInDuration      = 0.35f;
    [SerializeField] private float autoReturnDelay     = 5.0f;
    [SerializeField] private float coinAnimDelay       = 0.60f;   // délai avant l'anim pièces

    // ── Constantes de layout / palette ────────────────────────────────────────

    private const float CardW           = 800f;
    private const float CardH           = 680f;
    private const float CounterDuration = 1.2f;

    private static readonly Color ColOverlay    = new Color(0.03f, 0.02f, 0.08f, 0.88f);
    private static readonly Color ColCard       = new Color(0.08f, 0.07f, 0.14f, 1f);
    private static readonly Color ColCardBorder = new Color(1f, 0.82f, 0.18f, 0.55f);
    private static readonly Color ColTitle      = new Color(1f, 1f, 1f, 0.45f);
    private static readonly Color ColScore      = new Color(1f, 0.82f, 0.18f, 1f);
    private static readonly Color ColCoins      = new Color(0.35f, 0.95f, 0.50f, 1f);
    private static readonly Color ColHint       = new Color(0.6f, 0.6f, 0.6f, 1f);
    private static readonly Color ColBtn        = new Color(0.12f, 0.45f, 1.00f, 0.95f);
    private static readonly Color ColBtnTxt     = Color.white;

    // ── Références runtime ────────────────────────────────────────────────────

    private Canvas          overlayCanvas;
    private CanvasGroup     canvasGroup;
    private TextMeshProUGUI scoreValueLabel;
    private TextMeshProUGUI coinsValueLabel;
    private TextMeshProUGUI hintLabel;
    private Button          returnButton;

    private bool sessionEnded = false;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        BuildUI();

        if (GameManager.Instance != null)
            GameManager.Instance.OnGameOver.AddListener(OnGameOver);
        else
            Debug.LogWarning("[GameEndOverlay] Aucun GameManager trouvé dans la scène.");
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameOver.RemoveListener(OnGameOver);
    }

    // ── Déclenchement ─────────────────────────────────────────────────────────

    /// <summary>
    /// Point d'entrée principal. Peut être appelé directement si le GameManager
    /// de la scène n'est pas le <see cref="GameManager"/> standard.
    /// </summary>
    public void OnGameOver()
    {
        if (sessionEnded) return;
        sessionEnded = true;

        int score  = GameManager.Instance != null ? GameManager.Instance.CurrentScore : 0;
        int coins  = Mathf.Max(1, score / 10);

        // Persistance
        ScoreManager.EnsureExists();
        ScoreManager.Instance.AddScore(gameType, score);

        // Transfert vers le Menu
        GameEndData.Set(score, coins);

        StartCoroutine(ShowAndReturn(score, coins));
    }

    // ── Séquence principale ───────────────────────────────────────────────────

    private IEnumerator ShowAndReturn(int score, int coins)
    {
        overlayCanvas.gameObject.SetActive(true);
        canvasGroup.blocksRaycasts = true;

        // Fade in
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // Compteur score animé
        yield return StartCoroutine(CountUp(scoreValueLabel, 0, score, CounterDuration));

        // Compteur pièces animé (décalé)
        yield return new WaitForSecondsRealtime(coinAnimDelay);
        yield return StartCoroutine(CountUp(coinsValueLabel, 0, coins, CounterDuration * 0.7f));

        // Countdown retour auto
        bool pressed = false;
        returnButton.onClick.AddListener(() => pressed = true);

        float waited = 0f;
        while (waited < autoReturnDelay && !pressed)
        {
            waited += Time.unscaledDeltaTime;
            int remaining = Mathf.CeilToInt(autoReturnDelay - waited);
            if (hintLabel != null)
                hintLabel.text = $"Retour au menu dans {remaining}s…";
            yield return null;
        }

        if (hintLabel != null)
            hintLabel.text = "À tout de suite !";

        yield return new WaitForSecondsRealtime(0.4f);

        GoToMenu();
    }

    private void GoToMenu()
    {
        string target = returnToMenu ? MenuMainSetup.SceneName : OWGameManager.SceneOverworld;

        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadScene(target, target);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(target);
    }

    // ── Construction UI ───────────────────────────────────────────────────────

    private void BuildUI()
    {
        // Canvas plein écran au-dessus de tout
        var canvasGO           = new GameObject("GameEndCanvas");
        overlayCanvas          = canvasGO.AddComponent<Canvas>();
        overlayCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 600;

        var scaler             = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode     = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight  = 1f;

        canvasGO.AddComponent<GraphicRaycaster>();

        canvasGroup                = canvasGO.AddComponent<CanvasGroup>();
        canvasGroup.alpha          = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGO.SetActive(false);

        var root = canvasGO.GetComponent<RectTransform>();

        // Fond assombri plein écran
        MakeStretchImage("Overlay", root, ColOverlay);

        // Card centrale
        var card = MakeCenteredRect("Card", root, new Vector2(0.5f, 0.52f), new Vector2(CardW, CardH));
        var cardImg = card.gameObject.AddComponent<Image>();
        cardImg.sprite = SpriteGenerator.CreateWhiteSquare();
        cardImg.color  = ColCard;
        cardImg.raycastTarget = false;

        // Bordure dorée simulée
        var border = MakeCenteredRect("CardBorder", root, new Vector2(0.5f, 0.52f), new Vector2(CardW + 4f, CardH + 4f));
        var borderImg = border.gameObject.AddComponent<Image>();
        borderImg.sprite = SpriteGenerator.CreateWhiteSquare();
        borderImg.color  = ColCardBorder;
        borderImg.raycastTarget = false;
        border.SetSiblingIndex(card.GetSiblingIndex());   // derrière la card

        // Titre "FIN DE PARTIE"
        MakeCardLabel(card, "Title", "FIN DE PARTIE",
            new Vector2(0.05f, 0.80f), new Vector2(0.95f, 0.97f),
            52f, ColTitle, bold: true);

        // Séparateur horizontal
        var sep = new GameObject("Sep").AddComponent<Image>();
        sep.transform.SetParent(card, false);
        sep.sprite = SpriteGenerator.CreateWhiteSquare();
        sep.color  = new Color(1f, 1f, 1f, 0.12f);
        sep.raycastTarget = false;
        var sepRT = sep.rectTransform;
        sepRT.anchorMin = new Vector2(0.05f, 0.785f);
        sepRT.anchorMax = new Vector2(0.95f, 0.792f);
        sepRT.offsetMin = sepRT.offsetMax = Vector2.zero;

        // Section SCORE
        MakeCardLabel(card, "ScoreLbl", "SCORE",
            new Vector2(0.08f, 0.59f), new Vector2(0.92f, 0.71f),
            30f, ColTitle, bold: true);
        scoreValueLabel = MakeCardLabel(card, "ScoreVal", "0",
            new Vector2(0.08f, 0.46f), new Vector2(0.92f, 0.61f),
            80f, ColScore, bold: true);

        // Section PIÈCES GAGNÉES
        MakeCardLabel(card, "CoinsLbl", "PIÈCES GAGNÉES",
            new Vector2(0.08f, 0.31f), new Vector2(0.92f, 0.43f),
            30f, ColTitle, bold: true);
        coinsValueLabel = MakeCardLabel(card, "CoinsVal", "0 🪙",
            new Vector2(0.08f, 0.18f), new Vector2(0.92f, 0.33f),
            72f, ColCoins, bold: true);

        // Bouton MENU
        var btn = MakeCenteredRect("ReturnBtn", card, new Vector2(0.5f, 0.09f), new Vector2(520f, 80f));
        var btnImg = btn.gameObject.AddComponent<Image>();
        btnImg.sprite = SpriteGenerator.CreateWhiteSquare();
        btnImg.color  = ColBtn;
        returnButton  = btn.gameObject.AddComponent<Button>();
        returnButton.targetGraphic = btnImg;

        var btnColors           = returnButton.colors;
        btnColors.highlightedColor = new Color(0.25f, 0.55f, 1f, 1f);
        btnColors.pressedColor     = new Color(0.08f, 0.30f, 0.80f, 1f);
        btnColors.fadeDuration     = 0.08f;
        returnButton.colors     = btnColors;

        MakeCardLabel(btn, "BtnLabel", "RETOUR AU MENU →",
            Vector2.zero, Vector2.one, 30f, ColBtnTxt, bold: true);

        // Hint countdown (sous le bouton, au niveau du canvas)
        var hintGO = new GameObject("HintLabel");
        hintGO.transform.SetParent(root, false);
        hintLabel = hintGO.AddComponent<TextMeshProUGUI>();
        hintLabel.text      = $"Retour au menu dans {Mathf.CeilToInt(autoReturnDelay)}s…";
        hintLabel.fontSize  = 24f;
        hintLabel.color     = ColHint;
        hintLabel.alignment = TextAlignmentOptions.Center;
        hintLabel.raycastTarget = false;
        var hintRT = hintLabel.rectTransform;
        hintRT.anchorMin = new Vector2(0.1f, 0.16f);
        hintRT.anchorMax = new Vector2(0.9f, 0.21f);
        hintRT.offsetMin = hintRT.offsetMax = Vector2.zero;
    }

    // ── Helpers UI ────────────────────────────────────────────────────────────

    private static Image MakeStretchImage(string name, RectTransform parent, Color col)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img  = go.AddComponent<Image>();
        img.sprite = SpriteGenerator.CreateWhiteSquare();
        img.color  = col;
        img.raycastTarget = false;
        var rt   = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return img;
    }

    private static RectTransform MakeCenteredRect(string name, RectTransform parent,
        Vector2 anchorCenter, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchorCenter;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
        return rt;
    }

    private static TextMeshProUGUI MakeCardLabel(RectTransform parent,
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
        tmp.enableAutoSizing = false;
        var rt = tmp.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return tmp;
    }

    // ── Coroutine compteur ────────────────────────────────────────────────────

    private static IEnumerator CountUp(TextMeshProUGUI label, int from, int to, float duration)
    {
        if (label == null) yield break;

        bool isCoins = label.name == "CoinsVal";
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t  = Mathf.Clamp01(elapsed / duration);
            // EaseOutQuart
            float e  = 1f - Mathf.Pow(1f - t, 4f);
            int   v  = Mathf.RoundToInt(Mathf.Lerp(from, to, e));
            label.text = isCoins ? $"{v} 🪙" : v.ToString("N0");
            yield return null;
        }

        label.text = isCoins ? $"{to} 🪙" : to.ToString("N0");
    }
}
