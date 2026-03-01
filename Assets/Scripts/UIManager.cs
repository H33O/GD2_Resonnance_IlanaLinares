using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Background")]
    [SerializeField] private Sprite backgroundSprite;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;
    [SerializeField] private TextMeshProUGUI livesText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI finalScoreText;

    [Header("Score Gain Popup")]
    [SerializeField] private float popupRiseDuration = 0.8f;
    [SerializeField] private float popupRiseDistance = 60f;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        BuildBackground();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged.AddListener(UpdateScoreUI);
            GameManager.Instance.OnLivesChanged.AddListener(UpdateLivesUI);
            GameManager.Instance.OnHighScoreChanged.AddListener(UpdateHighScoreUI);
            GameManager.Instance.OnGameOver.AddListener(ShowGameOverPanel);

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
}