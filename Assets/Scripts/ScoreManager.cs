using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Types de mini-jeux suivis par le système de score.
/// </summary>
public enum GameType
{
    GameAndWatch,
    BubbleShooter,
    BallAndGoal
}

/// <summary>
/// Structure de données sérialisable contenant tous les scores de chaque mini-jeu.
/// </summary>
[System.Serializable]
public class GameScoreData
{
    public List<int> gameAndWatchScores  = new List<int>();
    public List<int> bubbleShooterScores = new List<int>();
    public List<int> ballGoalScores      = new List<int>();
}

/// <summary>
/// Singleton persistant gérant la sauvegarde et le chargement des scores
/// de tous les mini-jeux via JSON dans <see cref="Application.persistentDataPath"/>.
///
/// Survit aux changements de scène (DontDestroyOnLoad).
/// Les scores ne se réinitialisent jamais entre les sessions.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static ScoreManager Instance { get; private set; }

    // ── Constantes ────────────────────────────────────────────────────────────

    private const string SaveFileName = "scores.json";

    // ── Événements ────────────────────────────────────────────────────────────

    /// <summary>Déclenché chaque fois qu'un score est ajouté (arg : type, score ajouté).</summary>
    public event System.Action<GameType, int> OnScoreAdded;

    // ── Données ───────────────────────────────────────────────────────────────

    private GameScoreData data;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>
    /// Enregistre un score pour le type de jeu donné et persiste immédiatement.
    /// </summary>
    public void AddScore(GameType type, int score)
    {
        if (score < 0) return;

        GetList(type).Add(score);
        Save();

        OnScoreAdded?.Invoke(type, score);
    }

    /// <summary>
    /// Retourne tous les scores enregistrés pour le type de jeu donné, du plus ancien au plus récent.
    /// </summary>
    public IReadOnlyList<int> GetAllScores(GameType type) => GetList(type);

    /// <summary>
    /// Retourne la somme de tous les scores enregistrés tous jeux confondus.
    /// </summary>
    public int GetTotalScore()
    {
        int total = 0;
        foreach (int s in data.gameAndWatchScores)  total += s;
        foreach (int s in data.bubbleShooterScores) total += s;
        foreach (int s in data.ballGoalScores)      total += s;
        return total;
    }

    // ── Persistance ───────────────────────────────────────────────────────────

    private void Save()
    {
        try
        {
            string json = JsonUtility.ToJson(data, prettyPrint: false);
            File.WriteAllText(SavePath, json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ScoreManager] Échec de la sauvegarde : {e.Message}");
        }
    }

    private void Load()
    {
        try
        {
            if (File.Exists(SavePath))
            {
                string json = File.ReadAllText(SavePath);
                data = JsonUtility.FromJson<GameScoreData>(json) ?? new GameScoreData();
            }
            else
            {
                data = new GameScoreData();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ScoreManager] Échec du chargement : {e.Message}");
            data = new GameScoreData();
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    private List<int> GetList(GameType type)
    {
        return type switch
        {
            GameType.GameAndWatch  => data.gameAndWatchScores,
            GameType.BubbleShooter => data.bubbleShooterScores,
            GameType.BallAndGoal   => data.ballGoalScores,
            _                      => data.gameAndWatchScores
        };
    }

    /// <summary>Crée le ScoreManager s'il est absent (démarrage direct depuis une scène de jeu).</summary>
    public static void EnsureExists()
    {
        if (Instance != null) return;
        new GameObject("ScoreManager").AddComponent<ScoreManager>();
    }
}
