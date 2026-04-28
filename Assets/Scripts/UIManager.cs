using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Background")]
    [SerializeField] private Sprite backgroundSprite;  // Conservé pour compatibilité Inspector — ignoré au runtime

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;
    [SerializeField] private TextMeshProUGUI livesText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI finalScoreText;

    [Header("Perfect Effect")]
    [SerializeField] private TMP_FontAsset perfectFont;
    [SerializeField] private TMP_FontAsset speedUpFont;
    [SerializeField] private float popupRiseDuration = 0.8f;
    [SerializeField] private float popupRiseDistance = 60f;

    /// <summary>
    /// Police Michroma partagée pour tous les feedbacks dynamiques.
    /// Priorité : perfectFont (assigné dans l'Inspector), sinon MenuAssets.Font.
    /// </summary>
    private TMP_FontAsset FeedbackFont => perfectFont != null ? perfectFont : MenuAssets.Font;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Le fond est géré par BubbleSceneSetup ou MenuSceneSetup — UIManager ne crée plus de fond sprite.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged.AddListener(UpdateScoreUI);
            GameManager.Instance.OnLivesChanged.AddListener(UpdateLivesUI);
            GameManager.Instance.OnHighScoreChanged.AddListener(UpdateHighScoreUI);
            GameManager.Instance.OnGameOver.AddListener(ShowGameOverPanel);
            GameManager.Instance.OnDifficultyIncreased.AddListener(ShowSpeedUpEffect);

            // Synchronise l'UI avec l'état courant au cas où GameManager.Start()
            // aurait tiré ses événements avant notre abonnement.
            UpdateScoreUI(GameManager.Instance.CurrentScore);
            UpdateLivesUI(GameManager.Instance.CurrentLives);
            UpdateHighScoreUI(GameManager.Instance.HighScore);
        }

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    /// <summary>
    /// Crée un SpriteRenderer monde en fond de caméra, derrière tous les objets du jeu.
    /// Un Canvas ScreenSpaceOverlay s'affiche toujours par-dessus la caméra,
    /// donc le fond doit vivre dans l'espace monde avec un sortingOrder négatif.
    /// </summary>
    private void BuildBackground()
    {
        if (backgroundSprite == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        var go = new GameObject("Background");

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = backgroundSprite;
        sr.sortingOrder = -10;

        // Centre sur la caméra
        Vector3 camPos = cam.transform.position;
        go.transform.position = new Vector3(camPos.x, camPos.y, 0f);

        // Mise à l'échelle pour couvrir exactement la vue caméra
        float camHeight   = 2f * cam.orthographicSize;
        float camWidth    = camHeight * cam.aspect;
        float spriteHeight = backgroundSprite.bounds.size.y;
        float spriteWidth  = backgroundSprite.bounds.size.x;

        if (spriteHeight > 0f && spriteWidth > 0f)
        {
            go.transform.localScale = new Vector3(
                camWidth  / spriteWidth,
                camHeight / spriteHeight,
                1f
            );
        }
    }

    /// <summary>Affiche un texte "+X" flottant à côté du score pendant un court instant.</summary>
    public void ShowScoreGain(int amount)
    {
        if (scoreText == null) return;
        StartCoroutine(ScoreGainPopup(amount));
    }

    private IEnumerator ScoreGainPopup(int amount)
    {
        var go = new GameObject("ScoreGainPopup");
        go.transform.SetParent(scoreText.transform.parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin       = scoreText.rectTransform.anchorMin;
        rt.anchorMax       = scoreText.rectTransform.anchorMax;
        rt.pivot           = scoreText.rectTransform.pivot;
        rt.anchoredPosition = scoreText.rectTransform.anchoredPosition + new Vector2(scoreText.preferredWidth + 12f, 0f);
        rt.sizeDelta       = new Vector2(120f, 50f);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = $"+{amount}";
        tmp.fontSize  = scoreText.fontSize;
        tmp.color     = new Color(1f, 0.85f, 0.2f, 1f);
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Left;
        if (FeedbackFont != null) tmp.font = FeedbackFont;

        Vector2 startPos = rt.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < popupRiseDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popupRiseDuration;

            rt.anchoredPosition = startPos + new Vector2(0f, popupRiseDistance * t);
            float alpha = Mathf.Lerp(1f, 0f, t);
            tmp.color = new Color(1f, 0.85f, 0.2f, alpha);

            yield return null;
        }

        Destroy(go);
    }

    // ── Speed-up x2 effect ────────────────────────────────────────────────────

    private static readonly Color SpeedColor = new Color(1f, 0.85f, 0.2f, 1f);

    /// <summary>
    /// Pause le jeu, affiche "x{level}" avec grésillage, puis reprend.
    /// Utilise unscaledDeltaTime pour fonctionner avec Time.timeScale = 0.
    /// </summary>
    public void ShowSpeedUpEffect(int level)
    {
        StartCoroutine(SpeedUpRoutine(level));
    }

    private IEnumerator SpeedUpRoutine(int level)
    {
        // Bref grésillage avant la pause
        ScreenGlitch.Instance?.Trigger();
        yield return new WaitForSecondsRealtime(0.08f);

        Time.timeScale = 0f;

        Canvas canvas = scoreText != null
            ? scoreText.canvas
            : GetComponentInParent<Canvas>();
        if (canvas == null) { Time.timeScale = 1f; yield break; }

        Transform ct = canvas.transform;

        // ── Fond semi-transparent ─────────────────────────────────────────────
        var bgGO = new GameObject("SpeedUpBg");
        bgGO.transform.SetParent(ct, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color         = new Color(0f, 0f, 0f, 0.55f);
        bgImg.raycastTarget = false;
        var bgRT = bgImg.rectTransform;
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

        // ── Texte x{level} ────────────────────────────────────────────────────
        var go = new GameObject("SpeedUpText");
        go.transform.SetParent(ct, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(600f, 160f);
        rt.anchoredPosition = Vector2.zero;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = $"x{level + 1}";
        tmp.fontSize  = 120;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = SpeedColor;
        tmp.alignment = TextAlignmentOptions.Center;
        if (FeedbackFont != null) tmp.font = FeedbackFont;

        // ── Animation (unscaled) ──────────────────────────────────────────────
        float holdDuration = 1.4f;
        float elapsed      = 0f;
        float glitchTimer  = 0f;

        while (elapsed < holdDuration)
        {
            elapsed     += Time.unscaledDeltaTime;
            glitchTimer += Time.unscaledDeltaTime;
            float t      = elapsed / holdDuration;

            if (glitchTimer >= 0.07f)
            {
                glitchTimer = 0f;
                GlitchTMPVertices(tmp);
            }
            else
            {
                tmp.ForceMeshUpdate();
            }

            // Scale sin (pic +20%)
            float scale = 1f + 0.2f * Mathf.Sin(t * Mathf.PI);
            go.transform.localScale = Vector3.one * scale;

            // Fondu sur les 25% finaux
            float alpha = t > 0.75f ? Mathf.Lerp(1f, 0f, (t - 0.75f) / 0.25f) : 1f;
            tmp.color   = new Color(SpeedColor.r, SpeedColor.g, SpeedColor.b, alpha);
            bgImg.color = new Color(0f, 0f, 0f, 0.55f * alpha);

            yield return null;
        }

        Destroy(bgGO);
        Destroy(go);
        Time.timeScale = 1f;
    }



    private static readonly Color PerfectColor = new Color(1f, 0.85f, 0.2f, 1f);

    /// <summary>
    /// Affiche "PERFECT!" avec glitch sur le score et envoie des pulsations
    /// depuis l'UI du haut.
    /// </summary>
    public void ShowPerfectEffect()
    {
        StartCoroutine(PerfectRoutine());
    }

    private IEnumerator PerfectRoutine()
    {
        Canvas canvas = scoreText != null
            ? scoreText.canvas
            : GetComponentInParent<Canvas>();
        if (canvas == null) yield break;

        Transform ct = canvas.transform;

        // ── Pulsations depuis le score ────────────────────────────────────────
        Vector2 pulseOrigin = scoreText != null
            ? scoreText.rectTransform.anchoredPosition + new Vector2(scoreText.preferredWidth * 0.5f, -25f)
            : new Vector2(150f, -45f);

        for (int i = 0; i < 3; i++)
            StartCoroutine(PulseRing(ct, pulseOrigin,
                                     scoreText?.rectTransform.anchorMin ?? new Vector2(0f, 1f),
                                     scoreText?.rectTransform.anchorMax ?? new Vector2(0f, 1f),
                                     i * 0.18f));

        // ── Texte PERFECT! ───────────────────────────────────────────────────
        var go = new GameObject("PerfectText");
        go.transform.SetParent(ct, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(700f, 140f);
        rt.anchoredPosition = new Vector2(0f, 120f);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = "PERFECT!";
        tmp.fontSize  = 95;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = PerfectColor;
        tmp.alignment = TextAlignmentOptions.Center;
        if (FeedbackFont != null) tmp.font = FeedbackFont;

        // ── Animation glitch + scale + fade ──────────────────────────────────
        float totalDuration = 1.3f;
        float elapsed       = 0f;
        float glitchTimer   = 0f;

        while (elapsed < totalDuration)
        {
            elapsed     += Time.deltaTime;
            glitchTimer += Time.deltaTime;
            float t      = elapsed / totalDuration;

            // Glitch toutes les 0.07s
            if (glitchTimer >= 0.07f)
            {
                glitchTimer = 0f;
                GlitchTMPVertices(tmp);
            }
            else
            {
                tmp.ForceMeshUpdate(); // reset vertices entre les glitchs
            }

            // Scale sin — pic au milieu, retour à 1 à la fin
            float scale = 1f + 0.18f * Mathf.Sin(t * Mathf.PI);
            go.transform.localScale = Vector3.one * scale;

            // Fondu sur les 30% finaux
            float alpha = t > 0.7f ? Mathf.Lerp(1f, 0f, (t - 0.7f) / 0.3f) : 1f;
            tmp.color = new Color(PerfectColor.r, PerfectColor.g, PerfectColor.b, alpha);

            yield return null;
        }

        Destroy(go);
    }

    private void GlitchTMPVertices(TextMeshProUGUI tmp)
    {
        tmp.ForceMeshUpdate();
        TMP_TextInfo info = tmp.textInfo;

        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo ci = info.characterInfo[i];
            if (!ci.isVisible || Random.value > 0.55f) continue;

            int matIdx  = ci.materialReferenceIndex;
            int vertIdx = ci.vertexIndex;
            Vector3[] verts = info.meshInfo[matIdx].vertices;
            Vector3   off   = new Vector3(
                Random.Range(-10f, 10f),
                Random.Range(-6f,  6f),
                0f
            );
            for (int v = 0; v < 4; v++)
                verts[vertIdx + v] += off;
        }

        tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }

    private IEnumerator PulseRing(Transform parent, Vector2 origin,
                                   Vector2 anchorMin, Vector2 anchorMax,
                                   float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        var go = new GameObject("PulseRing");
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = anchorMin;
        rt.anchorMax        = anchorMax;
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(80f, 80f);
        rt.anchoredPosition = origin;

        var img = go.AddComponent<Image>();
        img.color         = new Color(PerfectColor.r, PerfectColor.g, PerfectColor.b, 0.65f);
        img.raycastTarget = false;

        float duration = 0.55f;
        float elapsed  = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t  = elapsed / duration;

            go.transform.localScale = Vector3.one * Mathf.Lerp(0.4f, 5f, t);
            img.color = new Color(PerfectColor.r, PerfectColor.g, PerfectColor.b,
                                  Mathf.Lerp(0.65f, 0f, t));

            yield return null;
        }

        Destroy(go);
    }


    /// <summary>
    /// Affiche un flash rouge plein écran et le texte "−1 VIE"
    /// lorsque le joueur touche un ennemi rouge.
    /// </summary>
    public void ShowLifeLostEffect()
    {
        StartCoroutine(LifeLostRoutine());
    }

    private static readonly Color LifeLostColor = new Color(0.95f, 0.10f, 0.08f, 1f);

    private IEnumerator LifeLostRoutine()
    {
        Canvas canvas = scoreText != null
            ? scoreText.canvas
            : GetComponentInParent<Canvas>();
        if (canvas == null) yield break;

        Transform ct = canvas.transform;

        // ── Flash rouge plein écran ───────────────────────────────────────────
        var flashGO = new GameObject("LifeLostFlash");
        flashGO.transform.SetParent(ct, false);
        var flashImg = flashGO.AddComponent<Image>();
        flashImg.color         = new Color(LifeLostColor.r, LifeLostColor.g, LifeLostColor.b, 0f);
        flashImg.raycastTarget = false;
        var flashRT = flashImg.rectTransform;
        flashRT.anchorMin = Vector2.zero;
        flashRT.anchorMax = Vector2.one;
        flashRT.offsetMin = flashRT.offsetMax = Vector2.zero;

        // ── Texte "−1 VIE" ────────────────────────────────────────────────────
        var textGO = new GameObject("LifeLostText");
        textGO.transform.SetParent(ct, false);
        var textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin        = new Vector2(0.5f, 0.5f);
        textRT.anchorMax        = new Vector2(0.5f, 0.5f);
        textRT.pivot            = new Vector2(0.5f, 0.5f);
        textRT.sizeDelta        = new Vector2(500f, 140f);
        textRT.anchoredPosition = new Vector2(0f, 80f);

        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = "−1 VIE";
        tmp.fontSize  = 90;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = new Color(LifeLostColor.r, LifeLostColor.g, LifeLostColor.b, 0f);
        tmp.alignment = TextAlignmentOptions.Center;

        // ── Animation : montée puis fondu ─────────────────────────────────────
        const float totalDuration = 1.1f;
        const float peakAlphaFlash = 0.45f;
        const float peakAlphaText  = 1.00f;
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;
            float t  = elapsed / totalDuration;
            float e  = t < 0.15f
                ? Mathf.Lerp(0f, 1f, t / 0.15f)
                : Mathf.Lerp(1f, 0f, (t - 0.15f) / 0.85f);

            flashImg.color = new Color(LifeLostColor.r, LifeLostColor.g, LifeLostColor.b, e * peakAlphaFlash);
            tmp.color      = new Color(LifeLostColor.r, LifeLostColor.g, LifeLostColor.b, e * peakAlphaText);

            // Légère montée du texte
            textRT.anchoredPosition = new Vector2(0f, 80f + 40f * t);

            yield return null;
        }

        Destroy(flashGO);
        Destroy(textGO);
    }

    private void UpdateScoreUI(int score)
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score}";
    }

    private void UpdateHighScoreUI(int highScore)
    {
        if (highScoreText != null)
            highScoreText.text = $"Record: {highScore}";
    }

    private void UpdateLivesUI(int lives)
    {
        if (livesText != null)
        {
            string hearts = "";
            for (int i = 0; i < lives; i++)
                hearts += "♥ ";
            livesText.text = hearts;
        }
    }

    private void ShowGameOverPanel()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            if (finalScoreText != null && GameManager.Instance != null)
                finalScoreText.text = $"Score Final: {GameManager.Instance.CurrentScore}";
        }
    }

    /// <summary>Appelé par le bouton Restart dans le GameOverPanel.</summary>
    public void OnRestartButton()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RestartGame();

        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // Rafraîchit l'affichage explicitement en cas de désynchronisation d'événement.
        if (GameManager.Instance != null)
            UpdateLivesUI(GameManager.Instance.CurrentLives);
    }

    /// <summary>Appelé par le bouton Menu dans le GameOverPanel — retour au menu principal.</summary>
    public void OnMenuButton()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
    }
}