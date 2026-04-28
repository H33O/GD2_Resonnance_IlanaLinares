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

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Audio")]
    [Tooltip("Musique du TiltBall (fightgame music.mp3).")]
    public AudioClip fightMusic;

    [Tooltip("Son joué quand une amélioration est achetée.")]
    public AudioClip upgradeSfx;

    [Tooltip("Son joué quand le joueur ramasse la clé.")]
    public AudioClip keySfx;

    [Tooltip("Son joué quand le joueur entre dans le goal.")]
    public AudioClip goalSfx;

    [Tooltip("Son joué quand le joueur meurt.")]
    public AudioClip deathSfx;

    [Tooltip("Son joué quand un ennemi est tué.")]
    public AudioClip enemyDeathSfx;

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

    /// <summary>Nombre d'améliorations achetées pendant la boutique courante (reset à chaque niveau).</summary>
    public int PendingUpgradeCount { get; private set; }

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

        ButtonClickAudio.HookAllButtons();
    }

    private void Update()
    {
        if (!isRunning) return;
        ElapsedTime += Time.deltaTime;

#if UNITY_EDITOR
        // Debug : T → sauter directement au dernier niveau (index TotalLevels - 1)
        if (Input.GetKeyDown(KeyCode.T))
        {
            int lastLevel = TotalLevels - 1;
            if (LevelIndex != lastLevel)
            {
                LevelIndex = lastLevel;
                HasKey     = false;
                TBSceneSetup.RebuildLevel(lastLevel);
                Debug.Log($"[TBDebug] Saut au dernier niveau (index {lastLevel}).");
            }
        }
#endif
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>Appelé par TBSceneSetup.Start() au lancement d'un niveau.</summary>
    public void StartLevel(int levelIndex)
    {
        LevelIndex           = levelIndex;
        HasKey               = false;
        ElapsedTime          = 0f;
        isRunning            = true;
        PendingUpgradeCount  = 0;
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

            HasKey = false;

            // Victoire finale = 50 XP base × 2 (bonus difficulté) = 100 XP fixes
            // 150 XP de base × 2 = 300 XP → niveau 4 atteint en une complétion
            const int XpVictoryFixed = 150;
            int xp = Mathf.RoundToInt(XpVictoryFixed * XpVictoryMultiplier);
            GameEndData.SetWithXP(Score, xp, GameType.BallAndGoal);

            TBWinWidget.ShowVictory(ElapsedTime, Score, xp, XpVictoryFixed, GoToMenuDirect);
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

    /// <summary>Incrémente le compteur d'améliorations achetées dans la boutique courante.</summary>
    public void RegisterUpgradePurchase() => PendingUpgradeCount++;

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

        // Déclenche les feedbacks d'amélioration une fois le joueur recréé
        if (PendingUpgradeCount > 0)
        {
            yield return null;   // attend un frame que BuildPlayer() soit exécuté
            var player = FindFirstObjectByType<TBPlayerController>();
            Vector3 pos = player != null ? player.transform.position : Vector3.zero;

            for (int i = 0; i < PendingUpgradeCount; i++)
            {
                TBUpgradeFX.TriggerUpgrade(pos);
                yield return new WaitForSeconds(0.18f);
            }

            PendingUpgradeCount = 0;
        }
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

    /// <summary>Joue le son d'amélioration via l'AudioManager.</summary>
    public static void PlayUpgradeSfx()    => AudioManager.Instance?.PlaySfx(Instance?.upgradeSfx);

    /// <summary>Joue le son de collecte de clé via l'AudioManager.</summary>
    public static void PlayKeySfx()        => AudioManager.Instance?.PlaySfx(Instance?.keySfx);

    /// <summary>Joue le son d'entrée dans le goal via l'AudioManager.</summary>
    public static void PlayGoalSfx()       => AudioManager.Instance?.PlaySfx(Instance?.goalSfx);

    /// <summary>Joue le son de mort du joueur via l'AudioManager.</summary>
    public static void PlayDeathSfx()     => AudioManager.Instance?.PlaySfx(Instance?.deathSfx);

    /// <summary>Joue le son de mort d'un ennemi via l'AudioManager.</summary>
    public static void PlayEnemyDeathSfx() => AudioManager.Instance?.PlaySfx(Instance?.enemyDeathSfx);
}


