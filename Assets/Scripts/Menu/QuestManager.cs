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
    public List<QuestProgress>   Progresses        = new List<QuestProgress>();
    public List<QuestDefinition> ActiveWave        = new List<QuestDefinition>();
    public int                   WaveIndex         = 0;   // nombre de vagues complétées
    public QuestProgress         ParentQuestProgress = new QuestProgress();
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

    // ── Quête parentière (fixe, lancée dès le premier démarrage) ─────────────

    /// <summary>Identifiant fixe de la quête parentière — ne change jamais.</summary>
    public const string ParentQuestId = "parent_gaw_80pts_x3";

    /// <summary>Définition immuable de la quête parentière.</summary>
    public static readonly QuestDefinition ParentQuestDefinition = new QuestDefinition
    {
        Id            = ParentQuestId,
        Title         = "Maîtrise du Game & Watch",
        Description   = "Fais 3 fois le Game & Watch en atteignant 80 points.",
        TargetGame    = GameType.GameAndWatch,
        RequiredCount = 3,
        RewardCoins   = 200,
        RewardXP      = 0,
        IsComplex     = false,
    };

    // ── Catalogue de quêtes concrètes ─────────────────────────────────────────

    // Chaque entrée : (titre, description, GameType, countBase, isComplex)
    // countBase sera scalé selon la vague / le niveau au moment de la génération.
    private static readonly (string title, string descTpl, GameType game, int baseCount, bool complex)[]
    QuestCatalog =
    {
        // ── Simples — Game & Watch ─────────────────────────────────────────────
        ("Entraînement G&W",
         "Joue {N} fois à Game & Watch et récolte {coins} pièces.",
         GameType.GameAndWatch, 3, false),

        ("Retour en forme",
         "Fais {N} parties de Game & Watch pour te remettre dans le bain.",
         GameType.GameAndWatch, 4, false),

        ("Collectionneur de sessions",
         "Lance Game & Watch {N} fois d'affilée et empoche {coins} pièces.",
         GameType.GameAndWatch, 5, false),

        // ── Simples — Bubble Shooter ───────────────────────────────────────────
        ("Bulles en série",
         "Joue {N} fois au Bubble Shooter et gagne {coins} pièces.",
         GameType.BubbleShooter, 3, false),

        ("Tir précis",
         "Lance {N} parties de Bubble Shooter pour affiner ta visée.",
         GameType.BubbleShooter, 4, false),

        ("Chasseur de bulles",
         "Complète {N} sessions au Bubble Shooter et récolte ta mise.",
         GameType.BubbleShooter, 5, false),

        // ── Simples — Ball & Goal ──────────────────────────────────────────────
        ("Mise en jambes",
         "Joue {N} fois à Ball & Goal pour échauffer tes réflexes.",
         GameType.BallAndGoal, 3, false),

        ("Buteur du dimanche",
         "Marque lors de {N} parties de Ball & Goal et récolte {coins} pièces.",
         GameType.BallAndGoal, 4, false),

        ("Constance",
         "Termine {N} sessions de Ball & Goal et empoche ta récompense.",
         GameType.BallAndGoal, 5, false),

        // ── Complexes — Game & Watch ───────────────────────────────────────────
        ("Maître du rythme",
         "Joue {N} fois à Game & Watch avec un score ≥ {minScore} — tu monteras de niveau !",
         GameType.GameAndWatch, 5, true),

        ("Vétéran du G&W",
         "Enchaîne {N} sessions de Game & Watch sans baisser ta garde (score ≥ {minScore}).",
         GameType.GameAndWatch, 7, true),

        ("Légende du G&W",
         "Domine Game & Watch {N} fois de suite avec performance maximale.",
         GameType.GameAndWatch, 10, true),

        // ── Complexes — Bubble Shooter ─────────────────────────────────────────
        ("Sniper de bulles",
         "Réalise {N} parties parfaites au Bubble Shooter (score ≥ {minScore}) et monte de niveau.",
         GameType.BubbleShooter, 5, true),

        ("Éclateur pro",
         "Complète {N} sessions au Bubble Shooter avec un haut niveau de score.",
         GameType.BubbleShooter, 7, true),

        ("L'impitoyable",
         "Éclate {N} vagues de bulles avec un score ≥ {minScore} — le niveau t'attend.",
         GameType.BubbleShooter, 10, true),

        // ── Complexes — Ball & Goal ────────────────────────────────────────────
        ("Attaquant élite",
         "Marque dans {N} parties de Ball & Goal (score ≥ {minScore}) pour grimper d'un niveau.",
         GameType.BallAndGoal, 5, true),

        ("Finisseur implacable",
         "Boucle {N} sessions de Ball & Goal avec des performances de haut rang.",
         GameType.BallAndGoal, 7, true),

        ("Champion toutes catégories",
         "Domine Ball & Goal lors de {N} parties consécutives à score élevé.",
         GameType.BallAndGoal, 10, true),

        // ── Complexes — Polyvalent ─────────────────────────────────────────────
        ("Joueur universel",
         "Joue {N} fois à n'importe quel mini-jeu (score ≥ {minScore}) et passe au niveau supérieur.",
         (GameType)(-1), 6, true),

        ("Explorateur de mondes",
         "Parcours les 3 univers de jeu ({N} parties au total) pour débloquer ton évolution.",
         (GameType)(-1), 8, true),

        ("Le Complet",
         "Complète {N} sessions tous jeux confondus avec un score ≥ {minScore}.",
         (GameType)(-1), 12, true),
    };

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

    /// <summary>Progression persistée de la quête parentière.</summary>
    public QuestProgress ParentQuestProgress => _save.ParentQuestProgress;

    /// <summary>
    /// Score minimum requis pour valider une session de jeu dans une quête.
    /// Augmente de <see cref="MinScorePerLevel"/> points par niveau joueur.
    /// </summary>
    public int GetMinScore()
    {
        return BaseMinScore;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();

        // Garantir que la progression parentière est toujours initialisée
        if (_save.ParentQuestProgress == null)
            _save.ParentQuestProgress = new QuestProgress { Id = ParentQuestId };
        else if (string.IsNullOrEmpty(_save.ParentQuestProgress.Id))
            _save.ParentQuestProgress.Id = ParentQuestId;

        if (_save.ActiveWave.Count == 0)
            GenerateWave();
    }

    private void Start()
    {
        // S'abonner ici (Start) pour garantir que ScoreManager.Instance existe déjà.
        // QuestManager.Awake() peut s'exécuter avant ScoreManager.Awake() si l'ordre
        // de création des GameObjects n'est pas maîtrisé.
        SubscribeToScoreManager();
    }

    private void OnEnable()
    {
        // Ré-abonnement si le composant est re-activé (ex : retour de scène)
        SubscribeToScoreManager();
    }

    private void OnDisable()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreAdded -= HandleScoreAdded;
    }

    private void SubscribeToScoreManager()
    {
        if (ScoreManager.Instance == null) return;
        // Désabonner d'abord pour éviter les doublons
        ScoreManager.Instance.OnScoreAdded -= HandleScoreAdded;
        ScoreManager.Instance.OnScoreAdded += HandleScoreAdded;
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
        // ── Quête parentière (score fixe 80, indépendante du niveau) ──────────
        if (type == GameType.GameAndWatch && score >= 80)
        {
            var pProg = _save.ParentQuestProgress;
            if (!pProg.Completed)
            {
                pProg.Count++;
                if (pProg.Count >= ParentQuestDefinition.RequiredCount)
                {
                    pProg.Completed = true;
                    OnQuestCompleted?.Invoke(ParentQuestDefinition);
                }
                Save();
                OnProgressChanged?.Invoke();
            }
        }

        // Session invalide si score inférieur au minimum requis (quêtes de vague)
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

        int waveIdx = _save.WaveIndex;

        var templates = BuildTemplates(1, waveIdx);
        Shuffle(templates);

        int count = Mathf.Min(QuestsPerWave, templates.Count);
        for (int i = 0; i < count; i++)
            _save.ActiveWave.Add(templates[i]);

        foreach (var def in _save.ActiveWave)
            _save.Progresses.Add(new QuestProgress { Id = def.Id });

        Save();
    }

    /// <summary>
    /// Construit la liste complète des modèles de quêtes possibles à partir du catalogue.
    /// Chaque modèle a un Id unique par vague (index_waveIdx).
    ///
    /// Scaling :
    ///   – count    = baseCount + waveIdx / 2  (arrondi bas), plafonné à 15
    ///   – coins    *= 1 + CoinScaleFactor × waveIdx
    ///   – xp       = BaseXPComplex, fixe (c'est l'XP vers le level up)
    /// </summary>
    private List<QuestDefinition> BuildTemplates(int level, int waveIdx)
    {
        float coinMult = 1f + CoinScaleFactor * waveIdx;
        int   minScore = GetMinScore();

        var list = new List<QuestDefinition>();

        for (int i = 0; i < QuestCatalog.Length; i++)
        {
            var (title, descTpl, game, baseCount, complex) = QuestCatalog[i];

            // Scaling du count selon la vague
            int count = Mathf.Clamp(baseCount + waveIdx / 2, baseCount, 15);

            // Récompenses
            int coins = complex
                ? Mathf.RoundToInt(BaseCoinsComplex * coinMult * 1.5f)
                : Mathf.RoundToInt(BaseCoinsSimple  * coinMult);
            int xp = complex ? BaseXPComplex : 0;

            // Interpolation de la description
            string desc = descTpl
                .Replace("{N}",       count.ToString())
                .Replace("{coins}",   coins.ToString())
                .Replace("{minScore}", minScore.ToString());

            list.Add(new QuestDefinition
            {
                Id            = $"q{i}_{waveIdx}",
                Title         = title,
                Description   = desc,
                TargetGame    = game,
                RequiredCount = count,
                RewardCoins   = coins,
                RewardXP      = xp,
                IsComplex     = complex,
            });
        }

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
