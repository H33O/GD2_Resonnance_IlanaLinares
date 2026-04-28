using System;
using UnityEngine;

/// <summary>
/// Singleton persistant gérant le niveau et l'XP du joueur.
///
/// Règles :
///   - 100 XP par niveau, progression infinie (aucun niveau maximum).
///   - L'XP et le niveau sont sauvegardés via <see cref="PlayerPrefs"/>.
///   - Quand <see cref="AddXP"/> fait dépasser les 100 XP, le niveau monte
///     et l'excédent est reporté au niveau suivant.
///   - La porte du 4e jeu se déverrouille au niveau 4 (géré par <see cref="DoorManager"/>).
/// </summary>
public class PlayerLevelManager : MonoBehaviour
{
    // ── Constantes ────────────────────────────────────────────────────────────

    public const  int MaxLevel   = 999;
    public const  int XPPerLevel = 100;

    private const string KeyLevel = "plm_level";
    private const string KeyXP    = "plm_xp";

    // ── Singleton ─────────────────────────────────────────────────────────────

    public static PlayerLevelManager Instance { get; private set; }

    // ── Événements ────────────────────────────────────────────────────────────

    /// <summary>Déclenché quand le joueur monte de niveau (arg : nouveau niveau).</summary>
    public event Action<int> OnLevelUp;

    /// <summary>Déclenché à chaque changement d'XP ou de niveau.</summary>
    public event Action OnProgressChanged;

    // ── État ──────────────────────────────────────────────────────────────────

    /// <summary>Niveau actuel (commence à 1, progression infinie).</summary>
    public int Level     { get; private set; } = 1;

    /// <summary>XP dans le niveau courant (0–99).</summary>
    public int CurrentXP { get; private set; } = 0;

    /// <summary>Ratio de remplissage de la barre (0–1).</summary>
    public float XPRatio => IsMaxLevel ? 1f : CurrentXP / (float)XPPerLevel;

    /// <summary>Vrai si le niveau maximum est atteint.</summary>
    public bool IsMaxLevel => Level >= MaxLevel;

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
        Load();
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>
    /// Ajoute de l'XP et gère les montées de niveau en cascade.
    /// Retourne le nombre de niveaux gagnés pendant cet appel.
    /// </summary>
    public int AddXP(int amount)
    {
        if (amount <= 0) return 0;

        int levelsGained = 0;
        CurrentXP += amount;

        while (CurrentXP >= XPPerLevel)
        {
            CurrentXP -= XPPerLevel;
            Level++;
            levelsGained++;
            OnLevelUp?.Invoke(Level);
        }

        Save();
        OnProgressChanged?.Invoke();
        return levelsGained;
    }

    /// <summary>Force un niveau précis (outil de debug).</summary>
    public void ForceLevel(int level)
    {
        Level     = Mathf.Clamp(level, 1, MaxLevel);
        CurrentXP = level >= MaxLevel ? XPPerLevel : 0;
        Save();
        if (level >= MaxLevel) OnLevelUp?.Invoke(Level);
        OnProgressChanged?.Invoke();
    }

    // ── Persistance ───────────────────────────────────────────────────────────

    private void Save()
    {
        PlayerPrefs.SetInt(KeyLevel, Level);
        PlayerPrefs.SetInt(KeyXP,    CurrentXP);
        PlayerPrefs.Save();
    }

    private void Load()
    {
        Level     = Mathf.Max(1, PlayerPrefs.GetInt(KeyLevel, 1));
        CurrentXP = Mathf.Max(0, PlayerPrefs.GetInt(KeyXP,    0));
    }

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>Crée le singleton s'il est absent de la scène.</summary>
    public static void EnsureExists()
    {
        if (Instance != null) return;
        new GameObject("PlayerLevelManager").AddComponent<PlayerLevelManager>();
    }
}
