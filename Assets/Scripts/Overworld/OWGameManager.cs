using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Singleton gérant l'état global de l'overworld : clé collectée, mini-jeux complétés, porte déverrouillée.
/// Persiste entre les scènes via DontDestroyOnLoad.
/// </summary>
public class OWGameManager : MonoBehaviour
{
    public static OWGameManager Instance { get; private set; }

    // ── Noms des scènes mini-jeux (doit correspondre aux Build Settings) ──────

    public const string SceneOverworld  = "Overworld";
    public const string SceneMinijeu1   = "GameAndWatch";
    public const string SceneMinijeu2   = "Minijeu-Bulles";
    public const string SceneMinijeu3   = "SlashGame";
    public const string SceneMinijeu4   = "CircleArena";

    // ── État persistant ───────────────────────────────────────────────────────

    private bool hasKey = false;
    private int  completedMiniGames = 0;

    public bool HasKey           => hasKey;
    public int  CompletedCount   => completedMiniGames;

    // ── Événements ────────────────────────────────────────────────────────────

    public UnityEvent OnKeyCollected;
    public UnityEvent OnDoorUnlocked;
    public UnityEvent<int> OnMiniGameCompleted;

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

    /// <summary>Appeler depuis un mini-jeu quand il est terminé pour revenir dans l'overworld.</summary>
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
