using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// ── Types publics ──────────────────────────────────────────────────────────────

/// <summary>Définition d'une quête générée dynamiquement.</summary>
[Serializable]
public class QuestDefinition
{
    public string   Id;
    public string   Title;
    public string   Description;
    public GameType TargetGame;
    public int      RequiredCount;
    public int      RewardCoins;
    public int      RewardXP;
    /// <summary>Vrai si c'est une quête "complexe" (multi-objectifs ou score élevé requis).</summary>
    public bool     IsComplex;
}

/// <summary>Progression persistée d'une quête.</summary>
[Serializable]
public class QuestProgress
{
    public string Id;
    public int    Count;
    public bool   Completed;
    public bool   Claimed;
}

/// <summary>Sauvegarde complète du système de quêtes.</summary>
[Serializable]
public class QuestSaveData
{
    public List<QuestProgress>   Progresses   = new List<QuestProgress>();
    public List<QuestDefinition> ActiveWave   = new List<QuestDefinition>();
    public int                   WaveIndex    = 0;   // nombre de vagues complétées
}

/// <summary>
/// Singleton persistant gérant les vagues de quêtes dynamiques.
///
/// Architecture :
///   – Une <em>vague</em> est un ensemble de N quêtes générées pour le niveau courant du joueur.
///   – Quand toutes les quêtes d'une vague sont complétées, une nouvelle vague plus difficile
///     est générée automatiquement.
///   – La difficulté scale sur <see cref="PlayerLevelManager.Level"/> :
///       • RequiredCount  += Level / 2
///       • Score minimum  += Level * 10  (via <see cref="GetMinScore"/>)
///       • RewardCoins    *= 1 + 0.25 * WaveIndex
///       • RewardXP       : les quêtes complexes donnent le double
///   – Les quêtes complexes (IsComplex = true) font progresser le niveau directement
///     via <see cref="PlayerLevelManager.AddXP"/>.
///
/// Le score minimum global est exposé via <see cref="GetMinScore"/> pour que
/// les systèmes en aval puissent adapter leurs règles de validation.
/// </summary>
public class QuestManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static QuestManager Instance { get; private set; }

    // ── Constantes ────────────────────────────────────────────────────────────

    private const string SaveFileName        = "quests.json";
    private const int    BaseMinScore        = 80;    // score minimal au niveau 1
    private const int    MinScorePerLevel    = 10;    // bonus de score minimum par niveau
    private const int    QuestsPerWave       = 5;     // nombre de quêtes par vague
    private const int    BaseCoinsSimple     = 50;
    private const int    BaseCoinsComplex    = 150;
    private const int    BaseXPSimple        = 20;
    private const int    BaseXPComplex       = 60;
    private const float  CoinScaleFactor     = 0.25f; // +25% par vague complétée

    // ── Événements ────────────────────────────────────────────────────────────

    /// <summary>Déclenché quand une quête vient d'être complétée. Arg : la définition.</summary>
    public event Action<QuestDefinition> OnQuestCompleted;

    /// <summary>Déclenché quand une nouvelle vague est générée. Arg : index de la vague.</summary>
    public event Action<int> OnWaveStarted;

    /// <summary>Déclenché à chaque changement de progression (count, complétion).</summary>
    public event Action OnProgressChanged;

    // ── Données ───────────────────────────────────────────────────────────────

    private QuestSaveData _save;

    // ── Propriétés publiques ──────────────────────────────────────────────────

    /// <summary>Vague active (lecture seule).</summary>
    public IReadOnlyList<QuestDefinition> ActiveWave => _save.ActiveWave;

    /// <summary>Indice de la vague actuelle (commence à 0).</summary>
    public int WaveIndex => _save.WaveIndex;

    /// <summary>
    /// Score minimum requis pour valider une session de jeu dans une quête.
    /// Augmente avec le niveau du joueur.
    /// </summary>
    public int GetMinScore()
    {
        int level = PlayerLevelManager.Instance?.Level ?? 1;
        return BaseMinScore + (level - 1) * MinScorePerLevel;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
        if (_save.ActiveWave.Count == 0)
            GenerateWave();
    }

    private void OnEnable()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreAdded += HandleScoreAdded;
    }

    private void OnDisable()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreAdded -= HandleScoreAdded;
    }

    // ── Callback niveau ───────────────────────────────────────────────────────

    /// <summary>Appelé par <see cref="PlayerLevelManager"/> après un gain de niveau.</summary>
    public void OnPlayerLevelChanged(int newLevel)
    {
        // La difficulté augmente automatiquement à la prochaine vague ;
        // ici on peut forcer une notification de changement si souhaité.
        OnProgressChanged?.Invoke();
    }

    // ── Réception d'un score de jeu ───────────────────────────────────────────

    private void HandleScoreAdded(GameType type, int score)
    {
        int minScore = GetMinScore();
        if (score < minScore)
        {
            // Session invalide mais on notifie quand même l'UI pour l'animer éventuellement
            return;
        }

        bool anyCompleted = false;

        foreach (var def in _save.ActiveWave)
        {
            var prog = GetProgress(def.Id);
            if (prog.Completed) continue;

            bool matches = (int)def.TargetGame == -1 || def.TargetGame == type;
            if (!matches) continue;

            prog.Count++;

            if (prog.Count >= def.RequiredCount)
            {
                prog.Completed = true;
                anyCompleted   = true;

                // Récompense monétaire
                ScoreManager.EnsureExists();
                ScoreManager.Instance.AddCoins(def.RewardCoins);

                // Récompense XP (uniquement pour les quêtes complexes)
                if (def.IsComplex)
                    PlayerLevelManager.Instance?.AddXP(def.RewardXP);

                OnQuestCompleted?.Invoke(def);
            }
        }

        Save();
        OnProgressChanged?.Invoke();

        // Vérifier si toute la vague est terminée
        if (IsWaveComplete())
        {
            _save.WaveIndex++;
            GenerateWave();
            OnWaveStarted?.Invoke(_save.WaveIndex);
        }
    }

    // ── Génération de vague ───────────────────────────────────────────────────

    /// <summary>
    /// Génère une nouvelle vague de <see cref="QuestsPerWave"/> quêtes
    /// basée sur le niveau joueur et l'indice de vague courant.
    /// </summary>
    private void GenerateWave()
    {
        _save.ActiveWave.Clear();
        _save.Progresses.Clear();

        int level     = PlayerLevelManager.Instance?.Level ?? 1;
        int waveIdx   = _save.WaveIndex;

        // Modèles de quêtes disponibles pour le générateur
        var templates = BuildTemplates(level, waveIdx);

        // Mélanger pour varier l'ordre
        Shuffle(templates);

        // Prendre les N premiers
        int count = Mathf.Min(QuestsPerWave, templates.Count);
        for (int i = 0; i < count; i++)
            _save.ActiveWave.Add(templates[i]);

        // Créer les entrées de progression
        foreach (var def in _save.ActiveWave)
            _save.Progresses.Add(new QuestProgress { Id = def.Id });

        Save();
    }

    /// <summary>
    /// Construit la liste complète des modèles de quêtes possibles pour un niveau et une vague donnés.
    ///
    /// Règles de scaling :
    ///   – RequiredCount de base : 3 + waveIdx / 2  (arrondi bas), plafonné à 12
    ///   – Les quêtes complexes ont RequiredCount × 1.5, arrondi supérieur, plafonné à 20
    ///   – RewardCoins *= (1 + CoinScaleFactor × waveIdx)
    ///   – Score minimum global : BaseMinScore + (level-1) × MinScorePerLevel (géré dans <see cref="GetMinScore"/>)
    /// </summary>
    private List<QuestDefinition> BuildTemplates(int level, int waveIdx)
    {
        int baseCount   = Mathf.Clamp(3 + waveIdx / 2, 3, 12);
        float coinMult  = 1f + CoinScaleFactor * waveIdx;

        int SimpleCoins(int @base) => Mathf.RoundToInt(@base * coinMult);
        int ComplexCoins(int @base) => Mathf.RoundToInt(@base * coinMult * 1.5f);
        int ComplexCount()          => Mathf.Clamp(Mathf.CeilToInt(baseCount * 1.5f), 4, 20);

        var list = new List<QuestDefinition>
        {
            // ── Quêtes simples ─────────────────────────────────────────────
            Quest($"gaw_{waveIdx}_s",
                "Game & Watch ×" + baseCount,
                $"Joue {baseCount} fois à Game & Watch (score ≥ {GetMinScore()}).",
                GameType.GameAndWatch, baseCount,
                SimpleCoins(BaseCoinsSimple), BaseXPSimple, isComplex: false),

            Quest($"bubble_{waveIdx}_s",
                "Bulles ×" + baseCount,
                $"Joue {baseCount} fois au Bubble Shooter (score ≥ {GetMinScore()}).",
                GameType.BubbleShooter, baseCount,
                SimpleCoins(BaseCoinsSimple), BaseXPSimple, isComplex: false),

            Quest($"ball_{waveIdx}_s",
                "Ball & Goal ×" + baseCount,
                $"Joue {baseCount} fois à Ball & Goal (score ≥ {GetMinScore()}).",
                GameType.BallAndGoal, baseCount,
                SimpleCoins(BaseCoinsSimple), BaseXPSimple, isComplex: false),

            // ── Quêtes complexes ────────────────────────────────────────────
            Quest($"gaw_{waveIdx}_c",
                "Expert Game & Watch",
                $"Joue {ComplexCount()} fois à Game & Watch (score ≥ {GetMinScore()}). Gagne de l'XP !",
                GameType.GameAndWatch, ComplexCount(),
                ComplexCoins(BaseCoinsComplex), BaseXPComplex, isComplex: true),

            Quest($"bubble_{waveIdx}_c",
                "Expert Bulles",
                $"Joue {ComplexCount()} fois au Bubble Shooter (score ≥ {GetMinScore()}). Gagne de l'XP !",
                GameType.BubbleShooter, ComplexCount(),
                ComplexCoins(BaseCoinsComplex), BaseXPComplex, isComplex: true),

            Quest($"ball_{waveIdx}_c",
                "Expert Ball & Goal",
                $"Joue {ComplexCount()} fois à Ball & Goal (score ≥ {GetMinScore()}). Gagne de l'XP !",
                GameType.BallAndGoal, ComplexCount(),
                ComplexCoins(BaseCoinsComplex), BaseXPComplex, isComplex: true),

            Quest($"any_{waveIdx}_c",
                "Joueur polyvalent",
                $"Joue {ComplexCount()} fois à n'importe quel mini-jeu. Gagne de l'XP !",
                (GameType)(-1), ComplexCount(),
                ComplexCoins(BaseCoinsComplex), BaseXPComplex, isComplex: true),
        };

        return list;
    }

    private static QuestDefinition Quest(string id, string title, string desc,
        GameType target, int count, int coins, int xp, bool isComplex)
        => new QuestDefinition
        {
            Id            = id,
            Title         = title,
            Description   = desc,
            TargetGame    = target,
            RequiredCount = count,
            RewardCoins   = coins,
            RewardXP      = xp,
            IsComplex     = isComplex,
        };

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool IsWaveComplete()
    {
        foreach (var def in _save.ActiveWave)
        {
            var prog = GetProgress(def.Id);
            if (!prog.Completed) return false;
        }
        return _save.ActiveWave.Count > 0;
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j     = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>Retourne la progression de la quête identifiée par <paramref name="id"/>.</summary>
    public QuestProgress GetProgress(string id)
    {
        foreach (var p in _save.Progresses)
            if (p.Id == id) return p;

        var np = new QuestProgress { Id = id };
        _save.Progresses.Add(np);
        return np;
    }

    /// <summary>Nombre de quêtes complétées dans la vague active.</summary>
    public int CompletedCount()
    {
        int n = 0;
        foreach (var def in _save.ActiveWave)
            if (GetProgress(def.Id).Completed) n++;
        return n;
    }

    // ── Persistance ───────────────────────────────────────────────────────────

    private void Save()
    {
        try   { File.WriteAllText(SavePath, JsonUtility.ToJson(_save, false)); }
        catch (Exception e) { Debug.LogError($"[QuestManager] Save failed: {e.Message}"); }
    }

    private void Load()
    {
        try
        {
            _save = File.Exists(SavePath)
                ? JsonUtility.FromJson<QuestSaveData>(File.ReadAllText(SavePath)) ?? new QuestSaveData()
                : new QuestSaveData();
        }
        catch (Exception e)
        {
            Debug.LogError($"[QuestManager] Load failed: {e.Message}");
            _save = new QuestSaveData();
        }
    }

    private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    // ── EnsureExists ──────────────────────────────────────────────────────────

    /// <summary>Crée le singleton s'il est absent.</summary>
    public static QuestManager EnsureExists()
    {
        if (Instance != null) return Instance;
        return new GameObject("QuestManager").AddComponent<QuestManager>();
    }
}
