using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Pont entre la scène GameAndWatch et le reste du jeu.
/// — Attend la fin de la partie GameAndWatch (via <see cref="GameManager"/>).
/// — Transfère le score final à <see cref="OWGameManager.FinishGameAndWatch"/>.
/// — Affiche un écran de récap avant de déclencher le retour à l'Overworld.
/// Attacher à un GameObject vide dans la scène GameAndWatch.
/// </summary>
public class GAWSessionBridge : MonoBehaviour
{
    // ── Constantes ────────────────────────────────────────────────────────────

    private const float RecapDisplayDuration = 3.5f;
    private const float FadeInDuration       = 0.40f;
    private const float AutoReturnDelay      = 1.5f;

    // ── État ──────────────────────────────────────────────────────────────────

    private bool sessionEnded = false;

    // Visuels
    private Canvas          recapCanvas;
    private CanvasGroup     canvasGroup;
    private TextMeshProUGUI scoreLabel;
    private TextMeshProUGUI hintLabel;
    private Button          continueButton;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        // S'abonner à la fin de partie du GameManager de la scène GameAndWatch
        // On utilise Start car GameManager peut ne pas être prêt dans Awake
    }

    private void Start()
    {
        EnsureOWGameManager();
        BuildRecapUI();

        // S'abonner à l'événement GameOver du GameManager existant
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameOver.AddListener(OnGameAndWatchOver);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameOver.RemoveListener(OnGameAndWatchOver);
    }

    // ── Callback fin de session ───────────────────────────────────────────────

    /// <summary>
    /// Appelé automatiquement par le <see cref="GameManager.OnGameOver"/> de la scène.
    /// Peut aussi être appelé manuellement si le joueur quitte volontairement.
    /// </summary>
    public void OnGameAndWatchOver()
    {
        if (sessionEnded) return;
        sessionEnded = true;

        int earned = GameManager.Instance != null ? GameManager.Instance.CurrentScore : 0;

        // Persistance du score Game & Watch
        ScoreManager.EnsureExists();
        ScoreManager.Instance.AddScore(GameType.GameAndWatch, earned);

        StartCoroutine(ShowRecapThenReturn(earned));
    }

    // ── Séquence de récapitulatif ─────────────────────────────────────────────

    private IEnumerator ShowRecapThenReturn(int earned)
    {
        // Mise à jour du label
        if (scoreLabel != null)
            scoreLabel.text = $"+{earned}\npoints récupérés !";

        // Fade in de l'écran de récap
        recapCanvas.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < FadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / FadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // Attendre soit le bouton, soit l'expiration du timer
        float waited = 0f;
        bool  buttonPressed = false;
        continueButton.onClick.AddListener(() => buttonPressed = true);

        while (waited < RecapDisplayDuration && !buttonPressed)
        {
            waited += Time.unscaledDeltaTime;

            // Mise à jour du hint de countdown
            int remaining = Mathf.CeilToInt(RecapDisplayDuration - waited);
            if (hintLabel != null)
                hintLabel.text = buttonPressed
                    ? "À tout de suite !"
                    : $"Retour automatique dans {remaining}s…";

            yield return null;
        }

        yield return new WaitForSecondsRealtime(AutoReturnDelay * 0.4f);

        // Transfert du score et retour
        if (OWGameManager.Instance != null)
            OWGameManager.Instance.FinishGameAndWatch(earned);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(OWGameManager.SceneOverworld);
    }

    // ── Construction de l'UI de récap ────────────────────────────────────────

    private void BuildRecapUI()
    {
        // Canvas plein écran
        var canvasGO      = new GameObject("GAWRecapCanvas");
        recapCanvas       = canvasGO.AddComponent<Canvas>();
        recapCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        recapCanvas.sortingOrder = 500;

        var scaler        = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        canvasGroup       = canvasGO.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGO.SetActive(false);

        // Fond semi-transparent
        var bg            = new GameObject("Background");
        bg.transform.SetParent(canvasGO.transform, false);
        var bgImg         = bg.AddComponent<Image>();
        bgImg.color       = new Color(0.03f, 0.03f, 0.10f, 0.90f);
        var bgRT          = bgImg.rectTransform;
        bgRT.anchorMin    = Vector2.zero;
        bgRT.anchorMax    = Vector2.one;
        bgRT.offsetMin    = bgRT.offsetMax = Vector2.zero;

        // Icône "Game & Watch" — étoile dorée décorative
        var iconGO        = new GameObject("Icon");
        iconGO.transform.SetParent(canvasGO.transform, false);
        var iconRT        = iconGO.AddComponent<RectTransform>();
        iconRT.anchorMin  = iconRT.anchorMax = new Vector2(0.5f, 0.65f);
        iconRT.pivot      = new Vector2(0.5f, 0.5f);
        iconRT.sizeDelta  = new Vector2(120f, 120f);
        iconRT.anchoredPosition = Vector2.zero;
        var iconImg       = iconGO.AddComponent<Image>();
        iconImg.color     = new Color(1f, 0.85f, 0.1f, 1f);

        // Score label
        var scoreLabelGO  = new GameObject("ScoreLabel");
        scoreLabelGO.transform.SetParent(canvasGO.transform, false);
        scoreLabel        = scoreLabelGO.AddComponent<TextMeshProUGUI>();
        scoreLabel.text   = "+0\npoints récupérés !";
        scoreLabel.fontSize = 52f;
        scoreLabel.color  = new Color(1f, 0.85f, 0.1f, 1f);
        scoreLabel.alignment = TextAlignmentOptions.Center;
        scoreLabel.fontStyle = FontStyles.Bold;
        scoreLabel.enableWordWrapping = false;
        var scoreRT       = scoreLabel.rectTransform;
        scoreRT.anchorMin = scoreRT.anchorMax = new Vector2(0.5f, 0.50f);
        scoreRT.pivot     = new Vector2(0.5f, 0.5f);
        scoreRT.sizeDelta = new Vector2(800f, 160f);
        scoreRT.anchoredPosition = Vector2.zero;

        // Hint / countdown
        var hintGO        = new GameObject("HintLabel");
        hintGO.transform.SetParent(canvasGO.transform, false);
        hintLabel         = hintGO.AddComponent<TextMeshProUGUI>();
        hintLabel.text    = $"Retour automatique dans {Mathf.CeilToInt(RecapDisplayDuration)}s…";
        hintLabel.fontSize = 22f;
        hintLabel.color   = new Color(0.7f, 0.7f, 0.7f, 1f);
        hintLabel.alignment = TextAlignmentOptions.Center;
        var hintRT        = hintLabel.rectTransform;
        hintRT.anchorMin  = hintRT.anchorMax = new Vector2(0.5f, 0.36f);
        hintRT.pivot      = new Vector2(0.5f, 0.5f);
        hintRT.sizeDelta  = new Vector2(700f, 50f);
        hintRT.anchoredPosition = Vector2.zero;

        // Bouton "Continuer"
        var btnGO         = new GameObject("ContinueButton");
        btnGO.transform.SetParent(canvasGO.transform, false);
        var btnRT         = btnGO.AddComponent<RectTransform>();
        btnRT.anchorMin   = btnRT.anchorMax = new Vector2(0.5f, 0.26f);
        btnRT.pivot       = new Vector2(0.5f, 0.5f);
        btnRT.sizeDelta   = new Vector2(300f, 65f);
        btnRT.anchoredPosition = Vector2.zero;
        var btnImg        = btnGO.AddComponent<Image>();
        btnImg.color      = new Color(0.15f, 0.55f, 1f, 0.9f);
        continueButton    = btnGO.AddComponent<Button>();

        var btnLabelGO    = new GameObject("Label");
        btnLabelGO.transform.SetParent(btnGO.transform, false);
        var btnLabelRT    = btnLabelGO.AddComponent<RectTransform>();
        btnLabelRT.anchorMin = Vector2.zero;
        btnLabelRT.anchorMax = Vector2.one;
        btnLabelRT.offsetMin = btnLabelRT.offsetMax = Vector2.zero;
        var btnTMP        = btnLabelGO.AddComponent<TextMeshProUGUI>();
        btnTMP.text       = "Continuer →";
        btnTMP.fontSize   = 26f;
        btnTMP.color      = Color.white;
        btnTMP.alignment  = TextAlignmentOptions.Center;
        btnTMP.fontStyle  = FontStyles.Bold;
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private void EnsureOWGameManager()
    {
        if (OWGameManager.Instance != null) return;
        var go = new GameObject("OWGameManager");
        go.AddComponent<OWGameManager>();
        Debug.LogWarning("[GAWSessionBridge] OWGameManager créé à la volée.");
    }
}
