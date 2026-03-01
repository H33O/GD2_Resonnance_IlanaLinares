using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;
    [SerializeField] private TextMeshProUGUI livesText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI finalScoreText;

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged.AddListener(UpdateScoreUI);
            GameManager.Instance.OnLivesChanged.AddListener(UpdateLivesUI);
            GameManager.Instance.OnHighScoreChanged.AddListener(UpdateHighScoreUI);
            GameManager.Instance.OnGameOver.AddListener(ShowGameOverPanel);
        }

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    private void UpdateScoreUI(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
        }
    }

    private void UpdateHighScoreUI(int highScore)
    {
        if (highScoreText != null)
        {
            highScoreText.text = $"Record: {highScore}";
        }
    }

    private void UpdateLivesUI(int lives)
    {
        if (livesText != null)
        {
            string hearts = "";
            for (int i = 0; i < lives; i++)
            {
                hearts += "♥ ";
            }
            livesText.text = hearts;
        }
    }

    private void ShowGameOverPanel()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            
            if (finalScoreText != null && GameManager.Instance != null)
            {
                finalScoreText.text = $"Score Final: {GameManager.Instance.CurrentScore}";
            }
        }
    }

    public void OnRestartButton()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }
}