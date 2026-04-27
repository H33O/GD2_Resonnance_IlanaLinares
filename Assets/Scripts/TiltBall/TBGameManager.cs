using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton gérant l'état persistant du jeu TiltBall.
/// Persiste via DontDestroyOnLoad.
///
/// Flux de jeu (10 niveaux, index 0 → 9) :
///   Le joueur entre dans le trou → boutique d'améliorations → niveau suivant.
///   Niveaux impairs : clé requise avant le trou.
///   Après le niveau 9 (10ème) : victoire finale + XP ×2 → retour au menu.
///   Mort du joueur : retour au niveau 1 (index 0), score réinitialisé.
/// </summary>
public class TBGameManager : MonoBehaviour
{
    public static TBGameManager Instance { get; private set; }

    // ── Constantes ────────────────────────────────────────────────────────────

    public const int    TotalLevels  = 10;
    public const string SceneMenu   = "Menu";

    /// <summary>Multiplicateur XP accordé à la victoire finale (10 niveaux complétés).</summary>
    private const float XpVictoryMultiplier = 2f;

    // ── État ──────────────────────────────────────────────────────────────────

    public int   LevelIndex  { get; private set; }
    public bool  HasKey      { get; private set; }
    public int   Score       { get; private set; }
    public float ElapsedTime { get; private set; }

    /// <summary>Données persistantes des améliorations achetées par le joueur.</summary>
    public TBUpgradeData Upgrades { get; private set; } = new TBUpgradeData();

    /// <summary>Vrai si le niveau courant requiert la clé pour ouvrir le trou.</summary>
    public bool RequireKey => (LevelIndex % 2 == 1);

    private bool isRunning;

    // ── Événements ────────────────────────────────────────────────────────────

    public readonly UnityEvent OnKeyCollected = new UnityEvent();

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
        Screen.orientation = ScreenOrientation.Portrait;
    }

    private void Update()
    {
        if (!isRunning) return;
        ElapsedTime += Time.deltaTime;
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>Appelé par TBSceneSetup.Start() au lancement d'un niveau.</summary>
    public void StartLevel(int levelIndex)
    {
        LevelIndex  = levelIndex;
        HasKey      = false;
        ElapsedTime = 0f;
        isRunning   = true;
    }

    /// <summary>Appelé par TBKey quand le joueur ramasse la clé.</summary>
    public void CollectKey()
    {
        if (HasKey) return;
        HasKey = true;
        Score += 50;
        OnKeyCollected?.Invoke();
    }

    /// <summary>Appelé par TBPlayerController.EnterHoleRoutine quand le joueur tombe dans le trou.</summary>
    public void EnterHole()
    {
        isRunning = false;

        // Bonus de temps : moins de secondes = plus de points
        Score += Mathf.RoundToInt(Mathf.Max(0f, 60f - ElapsedTime) * 5f) + 100;

        int nextLevel = LevelIndex + 1;

        if (nextLevel >= TotalLevels)
        {
            // ── Victoire finale — 10 niveaux complétés ────────────────────────
            ScoreManager.EnsureExists();

            // XP de base (score → XP) puis doublement
            int baseXp   = Mathf.Max(1, Score / 10);
            int bonusXp  = Mathf.RoundToInt(baseXp * XpVictoryMultiplier);
            int totalXp  = bonusXp;   // on envoie directement le total doublé

            // Enregistre le score sans déclencher l'XP standard (on la gère manuellement)
            ScoreManager.Instance.AddScoreOnly(GameType.BallAndGoal, Score);
            ScoreManager.Instance.AddXP(totalXp);

            HasKey = false;
            TBWinWidget.ShowVictory(ElapsedTime, Score, totalXp, baseXp, GoToMenuDirect);
        }
        else
        {
            // Boutique d'améliorations avant le niveau suivant
            HasKey = false;
            TBUpgradeShopWidget.Show(Upgrades, () => StartCoroutine(AdvanceLevelRoutine(nextLevel)));
        }
    }

    /// <summary>Modifie le score (utilisé par TBUpgradeShopWidget après un achat).</summary>
    public void SetScore(int newScore) => Score = Mathf.Max(0, newScore);

    /// <summary>
    /// Appelé après la mort du joueur.
    /// Remet le jeu au niveau 1 (index 0) et réinitialise le score + les améliorations.
    /// </summary>
    public void RestartLevel()
    {
        HasKey     = false;
        isRunning  = false;
        Score      = 0;
        LevelIndex = 0;
        Upgrades.Reset();

        // Courte pause pour laisser l'animation de mort se terminer
        StartCoroutine(RestartFromLevel0Routine());
    }

    /// <summary>Retourne au menu principal.</summary>
    public void GoToMenu()
    {
        HasKey    = false;
        isRunning = false;
        Score     = 0;
        LevelIndex = 0;
        Upgrades.Reset();

        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadScene(SceneMenu, SceneMenu);
        else
            SceneManager.LoadScene(SceneMenu);
    }

    // ── Navigation interne ────────────────────────────────────────────────────

    private IEnumerator AdvanceLevelRoutine(int nextLevel)
    {
        yield return new WaitForSeconds(0.3f);

        LevelIndex = nextLevel;
        HasKey     = false;
        TBSceneSetup.RebuildLevel(nextLevel);
    }

    private IEnumerator RestartFromLevel0Routine()
    {
        yield return new WaitForSeconds(0.6f);   // laisse l'animation de mort finir
        TBSceneSetup.RebuildLevel(0);
    }

    private void GoToMenuDirect()
    {
        Score      = 0;
        LevelIndex = 0;
        Upgrades.Reset();

        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadScene(SceneMenu, SceneMenu);
        else
            SceneManager.LoadScene(SceneMenu);
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    /// <summary>Crée le TBGameManager si absent (démarrage direct depuis une scène de jeu).</summary>
    public static void EnsureExists()
    {
        if (Instance != null) return;

        var go = new GameObject("TBGameManager");
        go.AddComponent<TBGameManager>();
        Debug.LogWarning("[TBGameManager] Créé à la volée — démarrage direct depuis une scène de jeu.");
    }
}


