using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] private int startingLives = 3;
    [SerializeField] private int scoreForSpeedIncrease = 10;
    [SerializeField] private float minSpawnInterval = 0.3f;
    [SerializeField] private float minStepDuration = 0.1f;

    [Header("Current Game State")]
    [SerializeField] private int currentScore = 0;
    [SerializeField] private int currentLives;
    [SerializeField] private bool isGameActive = false;
    [SerializeField] private int highScore = 0;

    [Header("Events")]
    public UnityEvent<int> OnScoreChanged;
    public UnityEvent<int> OnLivesChanged;
    public UnityEvent<int> OnHighScoreChanged;
    public UnityEvent      OnGameOver;
    public UnityEvent<int> OnDifficultyIncreased;

    public int CurrentScore  => currentScore;
    public int CurrentLives  => currentLives;
    public int HighScore     => highScore;
    public bool IsGameActive => isGameActive;

    private int difficultyLevel = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        highScore = PlayerPrefs.GetInt("HighScore", 0);
    }

    private void Start()
    {
        StartGame();
    }

    public void StartGame()
    {
        ClearAllCollectibles();

        currentScore   = 0;
        currentLives   = startingLives;
        difficultyLevel = 0;
        isGameActive   = true;
        OnScoreChanged?.Invoke(currentScore);
        OnLivesChanged?.Invoke(currentLives);
        OnHighScoreChanged?.Invoke(highScore);
    }

    private void ClearAllCollectibles()
    {
        Collectible[] collectibles = FindObjectsOfType<Collectible>();
        foreach (Collectible collectible in collectibles)
        {
            Destroy(collectible.gameObject);
        }
    }

    public void AddScore(int amount)
    {
        if (!isGameActive) return;

        currentScore += amount;
        OnScoreChanged?.Invoke(currentScore);

        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt("HighScore", highScore);
            OnHighScoreChanged?.Invoke(highScore);
        }

        if (currentScore % scoreForSpeedIncrease == 0 && currentScore > 0)
        {
            IncreaseDifficulty();
        }
    }

    public void LoseLife()
    {
        if (!isGameActive) return;

        currentLives--;
        OnLivesChanged?.Invoke(currentLives);

        if (currentLives <= 0)
        {
            EndGame();
        }
    }

    private void IncreaseDifficulty()
    {
        difficultyLevel++;
        Spawner spawner = FindFirstObjectByType<Spawner>();
        if (spawner != null) spawner.IncreaseSpeed();
        OnDifficultyIncreased?.Invoke(difficultyLevel);
    }

    private void EndGame()
    {
        isGameActive = false;
        OnGameOver?.Invoke();
    }

    public void RestartGame()
    {
        StartGame();
    }

    public float GetCurrentSpawnInterval()
    {
        // Diminue plus lentement (0.06 par niveau) et s'arrête à 0.6s minimum
        float decrease = difficultyLevel * 0.06f;
        return Mathf.Max(0.6f, 1.5f - decrease);
    }

    public float GetCurrentStepDuration()
    {
        float decrease = difficultyLevel * 0.02f;
        return Mathf.Max(minStepDuration, 0.4f - decrease);
    }
}
