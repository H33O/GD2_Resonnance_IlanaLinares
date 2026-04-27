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

    /// <summary>
    /// Nombre de sessions valides requises pour compléter la quête.
    /// Chaque session valide (score ≥ GetMinScore) incrémente d'une unité.
    /// </summary>
    public int      RequiredCount;

    public int      RewardCoins;
    public int      RewardXP;

    /// <summary>
    /// Vrai si c'est une quête "complexe" (multi-objectifs ou score élevé requis).
    /// Les quêtes complexes accordent de l'XP → progression de niveau.
    /// </summary>
    public bool     IsComplex;
}

/// <summary>Progression persistée d'une quête.</summary>
[Serializable]
public class QuestProgress
{
    public string Id;

    /// <summary>Nombre de sessions valides déjà complétées pour cette quête.</summary>
    public int    Count;

    public bool   Completed;
    public bool   Claimed;

    // ── Helpers de ratio ──────────────────────────────────────────────────────

    /// <summary>
    /// Ratio de progression [0..1] calculé dynamiquement.
    /// Appelé avec la <see cref="QuestDefinition"/> correspondante.
    /// </summary>
    public float GetRatio(QuestDefinition def)
    {
        if (Completed) return 1f;
        if (def.RequiredCount <= 0) return 0f;
        return Mathf.Clamp01((float)Count / def.RequiredCount);
    }
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
/// Singleton persistant gérant les vagues de quêtes dynamiques basées sur le niveau joueur.
///
/// Architecture :
///   – Une <em>vague</em> est un ensemble de N quêtes générées pour le niveau courant.
///   – Quand toutes les quêtes d'une vague sont complétées, une nouvelle vague plus difficile
///     est générée automatiquement (+20% difficulté par cycle).
///   – La difficulté scale sur <see cref="PlayerLevelManager.Level"/> :
///       • RequiredCount  += Level / 2
///       • Score minimum  += Level * 10  (via <see cref="GetMinScore"/>)
///       • RewardCoins    *= 1 + 0.25 × WaveIndex
///       • RewardXP       : double pour les quêtes complexes
///   – Les quêtes complexes (IsComplex = true) accordent de l'XP → level up.
///   – Les quêtes simples récompensent uniquement en pièces.
///
/// <b>Boucle de progression :</b>
///   ScoreManager.OnScoreAdded → HandleScoreAdded → progrès mis à jour
///   → OnProgressChanged (UI) → si vague terminée → GenerateWave → OnWaveStarted
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

    /// <summary>
    /// Déclenché à chaque changement de progression (count, complétion).
    /// Abonnez l'UI ici pour se rafraîchir en temps réel.
    /// </summary>
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
    /// Augmente de <see cref="MinScorePerLevel"/> points par niveau joueur.
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
        // La difficulté augmente à la prochaine vague.
        // On notifie l'UI pour qu'elle mette à jour le score minimum affiché.
        OnProgressChanged?.Invoke();
    }

    // ── Réception d'un score de jeu ───────────────────────────────────────────

    private void HandleScoreAdded(GameType type, int score)
    {
        // Session invalide si score inférieur au minimum requis
        if (score < GetMinScore()) return;

        bool anyCompleted = false;

        foreach (var def in _save.ActiveWave)
        {
            var prog = GetProgress(def.Id);
            if (prog.Completed) continue;

            // -1 signifie "tout type de jeu" (quête polyvalente)
            bool matches = (int)def.TargetGame == -1 || def.TargetGame == type;
            if (!matches) continue;

            prog.Count++;

            if (prog.Count >= def.RequiredCount)
            {
                CompleteQuest(def, prog);
                anyCompleted = true;
            }
        }

        Save();
        OnProgressChanged?.Invoke();

        if (IsWaveComplete())
            AdvanceWave();
    }

    // ── Complétion d'une quête ────────────────────────────────────────────────

    /// <summary>
    /// Marque la quête comme terminée, distribue les récompenses et déclenche les événements.
    ///
    /// Règles de récompenses :
    ///   – Toutes les quêtes donnent des pièces.
    ///   – Les quêtes complexes accordent de l'XP → peuvent déclencher un level up.
    /// </summary>
    private void CompleteQuest(QuestDefinition def, QuestProgress prog)
    {
        prog.Completed = true;

        // Récompense pièces (toujours)
        ScoreManager.EnsureExists();
        ScoreManager.Instance.AddCoins(def.RewardCoins);

        // Récompense XP uniquement pour les quêtes complexes → potentiel level up
        if (def.IsComplex && def.RewardXP > 0)
            PlayerLevelManager.Instance?.AddXP(def.RewardXP);

        OnQuestCompleted?.Invoke(def);
    }

    // ── Avancement de vague ───────────────────────────────────────────────────

    /// <summary>
    /// Passe à la vague suivante avec une difficulté augmentée.
    /// Génère une nouvelle liste de quêtes basée sur le niveau joueur actuel.
    /// </summary>
    private void AdvanceWave()
    {
        _save.WaveIndex++;
        GenerateWave();
        Save();
        OnWaveStarted?.Invoke(_save.WaveIndex);
    }

    // ── Génération de vague ───────────────────────────────────────────────────

    /// <summary>
    /// Génère une nouvelle vague de <see cref="QuestsPerWave"/> quêtes
    /// basée sur le niveau joueur et l'indice de vague courant.
    ///
    /// Scaling de difficulté :
    ///   – RequiredCount de base : 3 + waveIdx / 2  (arrondi bas), plafonné à 12
    ///   – Quêtes complexes : RequiredCount × 1.5, plafonné à 20
    ///   – RewardCoins × (1 + CoinScaleFactor × waveIdx)
    ///   – Score minimum global : BaseMinScore + (level-1) × MinScorePerLevel
    /// </summary>
    private void GenerateWave()
    {
        _save.ActiveWave.Clear();
        _save.Progresses.Clear();

        int level   = PlayerLevelManager.Instance?.Level ?? 1;
        int waveIdx = _save.WaveIndex;

        var templates = BuildTemplates(level, waveIdx);
        Shuffle(templates);

        int count = Mathf.Min(QuestsPerWave, templates.Count);
        for (int i = 0; i < count; i++)
            _save.ActiveWave.Add(templates[i]);

        foreach (var def in _save.ActiveWave)
            _save.Progresses.Add(new QuestProgress { Id = def.Id });

        Save();
    }

    /// <summary>
    /// Construit la liste complète des modèles de quêtes possibles.
    /// Chaque modèle est unique (Id unique par vague).
    /// </summary>
    private List<QuestDefinition> BuildTemplates(int level, int waveIdx)
    {
        int baseCount  = Mathf.Clamp(3 + waveIdx / 2, 3, 12);
        float coinMult = 1f + CoinScaleFactor * waveIdx;

        int SimpleCoins(int b)  => Mathf.RoundToInt(b * coinMult);
        int ComplexCoins(int b) => Mathf.RoundToInt(b * coinMult * 1.5f);
        int ComplexCount()      => Mathf.Clamp(Mathf.CeilToInt(baseCount * 1.5f), 4, 20);
        int minScore            = GetMinScore();

        return new List<QuestDefinition>
        {
            // ── Quêtes simples (pièces uniquement) ─────────────────────────
            Quest($"gaw_{waveIdx}_s",
                "Game & Watch ×" + baseCount,
                $"Joue {baseCount} fois à Game & Watch (score ≥ {minScore}).",
                GameType.GameAndWatch, baseCount,
                SimpleCoins(BaseCoinsSimple), 0, isComplex: false),

            Quest($"bubble_{waveIdx}_s",
                "Bulles ×" + baseCount,
                $"Joue {baseCount} fois au Bubble Shooter (score ≥ {minScore}).",
                GameType.BubbleShooter, baseCount,
                SimpleCoins(BaseCoinsSimple), 0, isComplex: false),

            Quest($"ball_{waveIdx}_s",
                "Ball & Goal ×" + baseCount,
                $"Joue {baseCount} fois à Ball & Goal (score ≥ {minScore}).",
                GameType.BallAndGoal, baseCount,
                SimpleCoins(BaseCoinsSimple), 0, isComplex: false),

            // ── Quêtes complexes (pièces + XP → potentiel level up) ────────
            Quest($"gaw_{waveIdx}_c",
                "Expert Game & Watch",
                $"Joue {ComplexCount()} fois à Game & Watch (score ≥ {minScore}).",
                GameType.GameAndWatch, ComplexCount(),
                ComplexCoins(BaseCoinsComplex), BaseXPComplex, isComplex: true),

            Quest($"bubble_{waveIdx}_c",
                "Expert Bulles",
                $"Joue {ComplexCount()} fois au Bubble Shooter (score ≥ {minScore}).",
                GameType.BubbleShooter, ComplexCount(),
                ComplexCoins(BaseCoinsComplex), BaseXPComplex, isComplex: true),

            Quest($"ball_{waveIdx}_c",
                "Expert Ball & Goal",
                $"Joue {ComplexCount()} fois à Ball & Goal (score ≥ {minScore}).",
                GameType.BallAndGoal, ComplexCount(),
                ComplexCoins(BaseCoinsComplex), BaseXPComplex, isComplex: true),

            Quest($"any_{waveIdx}_c",
                "Joueur Polyvalent",
                $"Joue {ComplexCount()} fois à n'importe quel mini-jeu (score ≥ {minScore}).",
                (GameType)(-1), ComplexCount(),
                ComplexCoins(BaseCoinsComplex), BaseXPComplex, isComplex: true),
        };
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
        if (_save.ActiveWave.Count == 0) return false;
        foreach (var def in _save.ActiveWave)
            if (!GetProgress(def.Id).Completed) return false;
        return true;
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
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

    /// <summary>
    /// Force la génération d'une nouvelle vague (debug / test uniquement).
    /// </summary>
    public void ForceNewWave()
    {
        AdvanceWave();
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
