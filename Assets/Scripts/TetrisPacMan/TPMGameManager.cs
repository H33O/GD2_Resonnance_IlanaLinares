using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Machine à états centrale du mini-jeu Tetris×Pac-Man.
/// Victoire = joueur atteint la cellule EXIT.
/// Défaite  = monstre attrape le joueur OU plus de coups.
/// </summary>
public class TPMGameManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static TPMGameManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Settings")]
    public TPMSettings settings;

    // ── Événements ────────────────────────────────────────────────────────────

    public static event Action<int> OnScoreChanged;
    public static event Action<int> OnMovesChanged;
    public static event Action      OnVictory;
    public static event Action      OnDefeat;
    public static event Action      OnFirstBlockPlaced;  // libère l'ennemi

    // Alias rétrocompatibilité
    public static event Action<int> OnMovesSpentChanged;

    // ── État ──────────────────────────────────────────────────────────────────

    public enum GameState { Playing, Victory, Defeat }

    public GameState State      { get; private set; } = GameState.Playing;
    public int       Score      { get; private set; }
    public int       Moves      { get; private set; }
    public int       MovesSpent { get; private set; }

    private bool firstBlockPlaced;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Time.timeScale = 1f;
    }

    private void Start()
    {
        Moves = settings.startingMoves;
        OnMovesChanged?.Invoke(Moves);
        OnScoreChanged?.Invoke(Score);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>Consomme un coup pour poser un bloc. Retourne false si impossible.</summary>
    public bool TrySpendMove()
    {
        if (State != GameState.Playing || Moves <= 0) return false;

        Moves--;
        MovesSpent++;
        OnMovesChanged?.Invoke(Moves);
        OnMovesSpentChanged?.Invoke(MovesSpent);

        // Premier bloc posé → libère l'ennemi
        if (!firstBlockPlaced)
        {
            firstBlockPlaced = true;
            OnFirstBlockPlaced?.Invoke();
        }

        if (Moves <= 0) TriggerDefeat();
        return true;
    }

    /// <summary>Appelé quand un bloc est détruit par Espace.</summary>
    public void NotifyBlockDestroyed(Vector3 worldPos)
    {
        if (State != GameState.Playing) return;

        Moves += settings.movesRestoredOnDestroy;
        OnMovesChanged?.Invoke(Moves);

        int pts = settings.pointsPerBlock;
        AddScore(pts);
        TPMFeedbackManager.Instance?.ShowFloatingScore(pts, worldPos, false);
    }

    /// <summary>Appelé par le Tetris Spawner quand des lignes sont effacées.</summary>
    public void NotifyLineCleared(int lineCount, Vector3 worldPos)
    {
        if (State != GameState.Playing) return;

        Moves += lineCount * settings.movesPerLineClear;
        OnMovesChanged?.Invoke(Moves);

        int pts = lineCount * settings.scorePerLineClear * lineCount;
        AddScore(pts);
        TPMFeedbackManager.Instance?.ShowFloatingScore(pts, worldPos, lineCount > 1);
    }

    /// <summary>Le joueur a atteint la sortie EXIT → victoire.</summary>
    public void NotifyGoalReached()
    {
        if (State != GameState.Playing) return;
        State = GameState.Victory;
        OnVictory?.Invoke();
        StartCoroutine(GoToGameAndWatchAfterDelay(2.5f));
    }

    /// <summary>Le monstre a attrapé le joueur → défaite.</summary>
    public void NotifyCaught()
    {
        if (State != GameState.Playing) return;
        TriggerDefeat();
    }

    /// <summary>Redémarre la scène.</summary>
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ── Rétrocompatibilité ────────────────────────────────────────────────────

    public void NotifyBlocksDestroyed(int chainSize, Vector3 worldPos) =>
        NotifyBlockDestroyed(worldPos);

    // ── Internes ──────────────────────────────────────────────────────────────

    private void AddScore(int pts)
    {
        Score += pts;
        OnScoreChanged?.Invoke(Score);
    }

    private void TriggerDefeat()
    {
        State = GameState.Defeat;
        OnDefeat?.Invoke();
    }

    private IEnumerator GoToGameAndWatchAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (OWGameManager.Instance != null)
            OWGameManager.Instance.GoToGameAndWatch(miniGameCompleted: true);
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
