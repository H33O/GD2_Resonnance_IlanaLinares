using System;
using UnityEngine;

/// <summary>
/// Singleton persistant gérant l'XP et le niveau par mini-jeu.
///
/// Chaque <see cref="GameType"/> possède son propre niveau et sa propre XP,
/// sauvegardés via <see cref="PlayerPrefs"/>.
///
/// Règles :
///   - 100 XP par niveau, progression infinie (aucun plafond global).
///   - La porte se déverrouille quand les 3 mini-jeux atteignent le niveau <see cref="UnlockLevel"/>.
/// </summary>
public class GameLevelManager : MonoBehaviour
{
    // ── Constantes ────────────────────────────────────────────────────────────

    /// <summary>Niveau requis par jeu pour déverrouiller la porte.</summary>
    public const int UnlockLevel = 4;

    /// <summary>XP nécessaire pour passer au niveau suivant.</summary>
    public const int XPPerLevel = 100;

    private const string KeyPrefix = "glm_";

    // ── Singleton ─────────────────────────────────────────────────────────────

    public static GameLevelManager Instance { get; private set; }

    // ── Événements ────────────────────────────────────────────────────────────

    /// <summary>Déclenché quand un jeu monte de niveau (arg : gameType, nouveau niveau).</summary>
    public event Action<GameType, int> OnGameLevelUp;

    /// <summary>Déclenché à chaque changement d'XP ou de niveau sur n'importe quel jeu.</summary>
    public event Action OnProgressChanged;

    /// <summary>Déclenché quand la condition de déverrouillage de la porte est remplie.</summary>
    public event Action OnDoorUnlocked;

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
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>Niveau actuel d'un jeu donné (minimum 1).</summary>
    public int GetLevel(GameType gameType)
        => Mathf.Max(1, PlayerPrefs.GetInt(LevelKey(gameType), 1));

    /// <summary>XP courant dans le niveau actuel d'un jeu (0–99).</summary>
    public int GetCurrentXP(GameType gameType)
        => Mathf.Max(0, PlayerPrefs.GetInt(XPKey(gameType), 0));

    /// <summary>Ratio de remplissage de la barre XP d'un jeu (0–1).</summary>
    public float GetXPRatio(GameType gameType)
        => GetCurrentXP(gameType) / (float)XPPerLevel;

    /// <summary>
    /// Ajoute de l'XP à un jeu et gère les montées de niveau en cascade.
    /// Retourne le nombre de niveaux gagnés pendant cet appel.
    /// </summary>
    public int AddXP(GameType gameType, int amount)
    {
        if (amount <= 0) return 0;

        int level = GetLevel(gameType);
        int xp    = GetCurrentXP(gameType) + amount;
        int levelsGained = 0;

        while (xp >= XPPerLevel)
        {
            xp -= XPPerLevel;
            level++;
            levelsGained++;
            OnGameLevelUp?.Invoke(gameType, level);
        }

        PlayerPrefs.SetInt(LevelKey(gameType), level);
        PlayerPrefs.SetInt(XPKey(gameType),    xp);
        PlayerPrefs.Save();

        OnProgressChanged?.Invoke();

        if (levelsGained > 0)
            CheckDoorUnlock();

        return levelsGained;
    }

    /// <summary>
    /// Vrai si tous les 3 mini-jeux concernés ont atteint le niveau <see cref="UnlockLevel"/>.
    /// </summary>
    public bool IsDoorUnlocked()
        => GetLevel(GameType.GameAndWatch) >= UnlockLevel
        && GetLevel(GameType.BubbleShooter) >= UnlockLevel
        && GetLevel(GameType.BallAndGoal) >= UnlockLevel;

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>Crée le singleton s'il est absent de la scène.</summary>
    public static void EnsureExists()
    {
        if (Instance != null) return;
        new GameObject("GameLevelManager").AddComponent<GameLevelManager>();
    }

    // ── Privé ─────────────────────────────────────────────────────────────────

    private void CheckDoorUnlock()
    {
        if (IsDoorUnlocked())
            OnDoorUnlocked?.Invoke();
    }

    private static string LevelKey(GameType gt) => $"{KeyPrefix}level_{(int)gt}";
    private static string XPKey(GameType gt)    => $"{KeyPrefix}xp_{(int)gt}";
}
