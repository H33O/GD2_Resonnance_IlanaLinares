using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Singleton gérant l'état global de l'overworld : clé collectée, mini-jeux complétés,
/// porte déverrouillée et <b>score total accumulé via les Game &amp; Watch</b>.
/// Persiste entre les scènes via DontDestroyOnLoad.
/// </summary>
public class OWGameManager : MonoBehaviour
{
    public static OWGameManager Instance { get; private set; }

    // ── Noms des scènes (doit correspondre aux Build Settings) ───────────────

    public const string SceneOverworld    = "Overworld";
    public const string SceneGameAndWatch = "GameAndWatch";
    public const string SceneMinijeu1     = "GameAndWatch";
    public const string SceneMinijeu2     = "Minijeu-Bulles";
    public const string SceneMinijeu3     = "SlashGame";
    public const string SceneMinijeu4     = "CircleArena";

    // ── État persistant ───────────────────────────────────────────────────────

    private bool hasKey = false;
    private int  completedMiniGames = 0;

    /// <summary>Score total cumulé depuis les parties Game &amp; Watch.</summary>
    private int totalScore = 0;

    /// <summary>Scène d'origine qui a déclenché le Game &amp; Watch (pour y retourner après).</summary>
    private string pendingReturnScene = SceneOverworld;

    public bool HasKey         => hasKey;
    public int  CompletedCount => completedMiniGames;
    public int  TotalScore     => totalScore;

    // ── Événements ────────────────────────────────────────────────────────────

    public UnityEvent OnKeyCollected;
    public UnityEvent OnDoorUnlocked;
    public UnityEvent<int> OnMiniGameCompleted;

    /// <summary>Déclenché à chaque fois que le score total change (arg : nouveau score total).</summary>
    public UnityEvent<int> OnTotalScoreChanged;

    // ── Position du joueur à sa réapparition dans l'overworld ─────────────────

    private Vector3 playerReturnPosition = Vector3.zero;
    public  Vector3 PlayerReturnPosition => playerReturnPosition;
    public  bool    HasReturnPosition    { get; private set; }

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

        EnsureSceneTransition();
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>Lance la transition vers un mini-jeu et mémorise la position de retour du joueur.</summary>
    public void EnterMiniGame(string sceneName, Vector3 returnPosition)
    {
        playerReturnPosition = returnPosition;
        HasReturnPosition    = true;

        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadScene(sceneName, sceneName);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Appeler depuis un mini-jeu terminé : passe d'abord par le Game &amp; Watch
    /// pour permettre au joueur de récupérer des points bonus, puis revient à l'overworld.
    /// </summary>
    public void GoToGameAndWatch(bool miniGameCompleted = true)
    {
        if (miniGameCompleted)
        {
            completedMiniGames++;
            OnMiniGameCompleted?.Invoke(completedMiniGames);
        }

        pendingReturnScene = SceneOverworld;

        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadScene(SceneGameAndWatch, SceneGameAndWatch);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(SceneGameAndWatch);
    }

    /// <summary>
    /// Appeler depuis le Game &amp; Watch une fois terminé.
    /// Ajoute les points collectés au total et retourne à la scène d'origine.
    /// </summary>
    public void FinishGameAndWatch(int earnedScore)
    {
        AddTotalScore(earnedScore);

        string returnScene = pendingReturnScene;
        pendingReturnScene = SceneOverworld;

        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadScene(returnScene, returnScene);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(returnScene);
    }

    /// <summary>Retour direct à l'overworld sans passer par le Game &amp; Watch.</summary>
    public void ReturnToOverworld(bool miniGameCompleted = true)
    {
        if (miniGameCompleted)
        {
            completedMiniGames++;
            OnMiniGameCompleted?.Invoke(completedMiniGames);
        }

        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadScene(SceneOverworld, SceneOverworld);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(SceneOverworld);
    }

    /// <summary>Ajoute des points au score total persistant et notifie les abonnés.</summary>
    public void AddTotalScore(int points)
    {
        if (points <= 0) return;
        totalScore += points;
        OnTotalScoreChanged?.Invoke(totalScore);
    }

    /// <summary>Collecte la clé dans l'overworld.</summary>
    public void CollectKey()
    {
        if (hasKey) return;
        hasKey = true;
        OnKeyCollected?.Invoke();
    }

    /// <summary>Tente d'ouvrir la porte verrouillée. Retourne vrai si la clé est présente.</summary>
    public bool TryUnlockDoor()
    {
        if (!hasKey) return false;
        OnDoorUnlocked?.Invoke();
        return true;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void EnsureSceneTransition()
    {
        if (SceneTransition.Instance != null) return;
        var go = new GameObject("SceneTransition");
        go.AddComponent<SceneTransition>();
        DontDestroyOnLoad(go);
    }
}
