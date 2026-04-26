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

    public int CurrentScore    => currentScore;
    public int CurrentLives    => currentLives;
    public int HighScore       => highScore;
    public bool IsGameActive   => isGameActive;
    public int DifficultyLevel => difficultyLevel;

    private int difficultyLevel = 0;
    private float gridStepTimer = 0f;

    /// <summary>Fires every global grid tick — all falling objects move in sync on this event.</summary>
    public event System.Action OnGridStep;

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

    private void Update()
    {
        if (!isGameActive) return;

        gridStepTimer += Time.deltaTime;
        float stepDuration = GetCurrentStepDuration();
        if (gridStepTimer >= stepDuration)
        {
            gridStepTimer -= stepDuration;
            OnGridStep?.Invoke();
        }
    }

    public void StartGame()
    {
        ClearAllCollectibles();

        currentScore    = 0;
        currentLives    = startingLives;
        difficultyLevel = 0;
        gridStepTimer   = 0f;
        isGameActive    = true;
        OnScoreChanged?.Invoke(currentScore);
        OnLivesChanged?.Invoke(currentLives);
        OnHighScoreChanged?.Invoke(highScore);
    }

    private void ClearAllCollectibles()
    {
        foreach (var c in FindObjectsOfType<Collectible>())
            Destroy(c.gameObject);

        foreach (var r in FindObjectsOfType<RedEnemy>())
            Destroy(r.gameObject);
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
        if (difficultyLevel < 5)
        {
            // Gentle ramp-up before x5: −0.06s per level (1.5s → 1.26s)
            return Mathf.Max(0.6f, 1.5f - difficultyLevel * 0.06f);
        }
        else
        {
            // Brutal jump at x5 to 0.80s, then −0.18s per level, floor 0.20s
            return Mathf.Max(0.20f, 0.80f - (difficultyLevel - 5) * 0.18f);
        }
    }

    public float GetCurrentStepDuration()
    {
        if (difficultyLevel < 5)
        {
            // Gentle ramp-up before x5: −0.02s per level (0.4s → 0.32s)
            return Mathf.Max(minStepDuration, 0.4f - difficultyLevel * 0.02f);
        }
        else
        {
            // Brutal jump at x5 to 0.18s, then −0.04s per level, floor 0.04s
            return Mathf.Max(0.04f, 0.18f - (difficultyLevel - 5) * 0.04f);
        }
    }

    /// <summary>Returns the fast enemy spawn probability for the current difficulty level.</summary>
    public float GetFastEnemyChance()
    {
        if (difficultyLevel < 5) return 0.25f;
        // Jump to 55% at x5, +12% per level, capped at 90%
        return Mathf.Min(0.90f, 0.55f + (difficultyLevel - 5) * 0.12f);
    }

    /// <summary>
    /// Returns the red enemy spawn probability for the current difficulty level.
    /// Starts appearing at difficulty 1 (5%), ramps up to 35% max.
    /// </summary>
    public float GetRedEnemyChance()
    {
        if (difficultyLevel < 1) return 0f;
        return Mathf.Min(0.35f, 0.05f + (difficultyLevel - 1) * 0.05f);
    }

    /// <summary>Returns how many objects to spawn simultaneously at the current difficulty.</summary>
    public int GetMultiSpawnCount()
    {
        if (difficultyLevel < 6) return 1;

        float roll = Random.value;
        if (difficultyLevel >= 8) return roll < 0.10f ? 3 : roll < 0.60f ? 2 : 1;
        if (difficultyLevel >= 7) return roll < 0.40f ? 2 : 1;
        return roll < 0.20f ? 2 : 1; // level 6
    }
}
