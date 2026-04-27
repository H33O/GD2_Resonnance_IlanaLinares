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
/// Seuil XP par palier : chaque palier demande <c>BaseXP + Level × StepXP</c>.
///   Level 1 → 2 : 100 XP
///   Level 2 → 3 : 150 XP
///   Level 3 → 4 : 200 XP
///   … +50 XP par palier
///
/// L'XP est accordée directement via <see cref="AddXP(int)"/>.
/// Le gain de niveau déclenche <see cref="OnLevelUp"/>.
/// </summary>
public class PlayerLevelManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static PlayerLevelManager Instance { get; private set; }

    // ── Constantes ────────────────────────────────────────────────────────────

    private const string SaveFileName = "player_level.json";

    /// <summary>XP de base requis pour le premier palier.</summary>
    public const int BaseXP = 100;

    /// <summary>XP supplémentaire requis par palier supérieur.</summary>
    public const int StepXP = 50;

    // ── Événements ────────────────────────────────────────────────────────────

    /// <summary>Déclenché quand le joueur monte de niveau. Arg : nouveau niveau.</summary>
    public event Action<int> OnLevelUp;

    /// <summary>Déclenché quand l'XP ou le niveau change.</summary>
    public event Action OnProgressChanged;

    // ── Données ───────────────────────────────────────────────────────────────

    private PlayerLevelData _data = new PlayerLevelData();

    // ── Propriétés ────────────────────────────────────────────────────────────

    /// <summary>Niveau actuel du joueur (commence à 1).</summary>
    public int Level => _data.Level;

    /// <summary>XP actuelle dans le palier courant.</summary>
    public int CurrentXP => _data.XP;

    /// <summary>XP totale requise pour passer au niveau suivant depuis le niveau actuel.</summary>
    public int XPToNextLevel => BaseXP + (_data.Level - 1) * StepXP;

    /// <summary>Ratio [0..1] de remplissage de la barre XP.</summary>
    public float XPRatio => XPToNextLevel > 0 ? Mathf.Clamp01((float)_data.XP / XPToNextLevel) : 1f;

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
    /// Ajoute <paramref name="amount"/> XP au joueur.
    /// Monte le niveau autant de fois que nécessaire si l'XP dépasse le seuil.
    /// </summary>
    public void AddXP(int amount)
    {
        if (amount <= 0) return;

        _data.XP += amount;
        bool leveledUp = false;

        while (_data.XP >= XPToNextLevel)
        {
            _data.XP -= XPToNextLevel;
            _data.Level++;
            leveledUp = true;
            OnLevelUp?.Invoke(_data.Level);
        }

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

    // ── EnsureExists ──────────────────────────────────────────────────────────

    /// <summary>Crée le singleton s'il est absent.</summary>
    public static PlayerLevelManager EnsureExists()
    {
        if (Instance != null) return Instance;
        return new GameObject("PlayerLevelManager").AddComponent<PlayerLevelManager>();
    }
}
