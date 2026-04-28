using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Données persistantes du niveau joueur.
/// </summary>
[Serializable]
public class PlayerLevelData
{
    public int Level = 1;
    public int XP    = 0;
}

/// <summary>
/// Singleton persistant gérant le niveau et l'XP du joueur.
///
/// Chaque palier demande exactement <see cref="XPPerLevel"/> XP (100).
///   Level 1 → 2 : 100 XP
///   Level 2 → 3 : 100 XP
///   Level 3 → 4 : 100 XP
///   Level 4      : niveau maximum — porte ParryGame déverrouillée.
///
/// L'XP est accordée via <see cref="AddXP(int)"/>.
/// </summary>
public class PlayerLevelManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static PlayerLevelManager Instance { get; private set; }

    // ── Constantes ────────────────────────────────────────────────────────────

    private const string SaveFileName = "player_level.json";

    /// <summary>XP requis pour chaque palier (fixe).</summary>
    public const int XPPerLevel = 100;

    /// <summary>Niveau maximum déverrouillant le ParryGame.</summary>
    public const int MaxLevel = 4;

    // ── Événements ────────────────────────────────────────────────────────────

    /// <summary>Déclenché quand le joueur monte de niveau. Arg : nouveau niveau.</summary>
    public event Action<int> OnLevelUp;

    /// <summary>Déclenché quand l'XP ou le niveau change.</summary>
    public event Action OnProgressChanged;

    // ── Données ───────────────────────────────────────────────────────────────

    private PlayerLevelData _data = new PlayerLevelData();

    // ── Propriétés ────────────────────────────────────────────────────────────

    /// <summary>Niveau actuel du joueur (1 à <see cref="MaxLevel"/>).</summary>
    public int Level => _data.Level;

    /// <summary>XP actuelle dans le palier courant (0–99).</summary>
    public int CurrentXP => _data.XP;

    /// <summary>XP requis pour passer au niveau suivant (toujours 100).</summary>
    public int XPToNextLevel => XPPerLevel;

    /// <summary>Ratio [0..1] de remplissage du palier courant.</summary>
    public float XPRatio => Mathf.Clamp01((float)_data.XP / XPPerLevel);

    /// <summary>Vrai quand le niveau maximum est atteint.</summary>
    public bool IsMaxLevel => _data.Level >= MaxLevel;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>
    /// Ajoute <paramref name="amount"/> XP. Monte de niveau autant de fois
    /// que nécessaire, plafonne à <see cref="MaxLevel"/>.
    /// </summary>
    public void AddXP(int amount)
    {
        if (amount <= 0 || IsMaxLevel) return;

        _data.XP += amount;

        while (_data.XP >= XPPerLevel && _data.Level < MaxLevel)
        {
            _data.XP -= XPPerLevel;
            _data.Level++;
            OnLevelUp?.Invoke(_data.Level);
        }

        // Plafonner l'XP au max si on est déjà niveau max
        if (_data.Level >= MaxLevel)
            _data.XP = 0;

        Save();
        OnProgressChanged?.Invoke();
    }

    // ── Persistance ───────────────────────────────────────────────────────────

    private void Save()
    {
        try   { File.WriteAllText(SavePath, JsonUtility.ToJson(_data, false)); }
        catch (Exception e) { Debug.LogError($"[PlayerLevelManager] Save failed: {e.Message}"); }
    }

    private void Load()
    {
        try
        {
            _data = File.Exists(SavePath)
                ? JsonUtility.FromJson<PlayerLevelData>(File.ReadAllText(SavePath)) ?? new PlayerLevelData()
                : new PlayerLevelData();
        }
        catch (Exception e)
        {
            Debug.LogError($"[PlayerLevelManager] Load failed: {e.Message}");
            _data = new PlayerLevelData();
        }
    }

    private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    /// <summary>
    /// Force immédiatement le joueur au niveau cible avec 0 XP dans le palier.
    /// Usage debug uniquement — déclenche <see cref="OnLevelUp"/> et <see cref="OnProgressChanged"/>.
    /// </summary>
    public void ForceLevel(int targetLevel)
    {
        targetLevel    = Mathf.Max(1, targetLevel);
        _data.Level    = targetLevel;
        _data.XP       = 0;
        Save();
        OnLevelUp?.Invoke(_data.Level);
        OnProgressChanged?.Invoke();
    }

    // ── EnsureExists ──────────────────────────────────────────────────────────

    /// <summary>Crée le singleton s'il est absent.</summary>
    public static PlayerLevelManager EnsureExists()
    {
        if (Instance != null) return Instance;
        return new GameObject("PlayerLevelManager").AddComponent<PlayerLevelManager>();
    }
}
