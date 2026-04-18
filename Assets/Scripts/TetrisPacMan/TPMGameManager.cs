using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Machine à états centrale du mini-jeu Tetris×Pac-Man.
/// Gère le score, les coups, les conditions de victoire/défaite.
/// </summary>
public class TPMGameManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static TPMGameManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Settings")]
    public TPMSettings settings;

    // ── Événements ────────────────────────────────────────────────────────────

    public static event Action<int>  OnScoreChanged;
    public static event Action<int>  OnMovesChanged;
    public static event Action       OnVictory;
    public static event Action       OnDefeat;

    // ── État ──────────────────────────────────────────────────────────────────

    public enum GameState { Playing, Victory, Defeat }

    public GameState State  { get; private set; } = GameState.Playing;
    public int       Score  { get; private set; }
    public int       Moves  { get; private set; }

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

    /// <summary>Consomme un coup pour poser un bloc.</summary>
    public bool TrySpendMove()
    {
        if (State != GameState.Playing) return false;
        if (Moves <= 0) return false;

        Moves--;
        OnMovesChanged?.Invoke(Moves);
        CheckMoveDefeat();
        return true;
    }

    /// <summary>
    /// Ajoute des coups et du score lors de la destruction de blocs.
    /// chainSize = nombre de blocs détruits simultanément.
    /// </summary>
    public void NotifyBlocksDestroyed(int chainSize, Vector3 worldPos)
    {
        if (State != GameState.Playing) return;

        // Coups récupérés
        int movesGained = settings.movesRestoredSingle
                        + (chainSize - 1) * settings.movesRestoredChainBonus;
        Moves += movesGained;
        OnMovesChanged?.Invoke(Moves);

        // Score
        float multiplier = 1f;
        int   points     = 0;
        for (int i = 0; i < chainSize; i++)
        {
            points += Mathf.RoundToInt(settings.pointsSingle * multiplier);
            multiplier *= settings.chainScoreMultiplier;
        }
        Score += points;
        OnScoreChanged?.Invoke(Score);

        // Feedback visuel flottant
        TPMFeedbackManager.Instance?.ShowFloatingScore(points, worldPos, chainSize > 1);
    }

    /// <summary>Le joueur a atteint la sortie.</summary>
    public void NotifyReachedExit()
    {
        if (State != GameState.Playing) return;
        State = GameState.Victory;
        OnVictory?.Invoke();

        // Lance le Game & Watch après un court délai pour laisser l'UI s'afficher
        StartCoroutine(GoToGameAndWatchAfterDelay(2.5f));
    }

    private System.Collections.IEnumerator GoToGameAndWatchAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (OWGameManager.Instance != null)
            OWGameManager.Instance.GoToGameAndWatch(miniGameCompleted: true);
        else
            SceneManager.LoadScene(OWGameManager.SceneGameAndWatch);
    }

    /// <summary>Le monstre a attrapé le joueur.</summary>
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

    // ── Internes ──────────────────────────────────────────────────────────────

    private void CheckMoveDefeat()
    {
        if (Moves <= 0 && State == GameState.Playing)
            TriggerDefeat();
    }

    private void TriggerDefeat()
    {
        State = GameState.Defeat;
        OnDefeat?.Invoke();
    }
}
