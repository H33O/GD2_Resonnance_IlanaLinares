using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central state machine for the Slash Game.
/// Manages score, energy, XP, squad, fury mode, and game-over flow.
/// </summary>
public class SGGameManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static SGGameManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Settings")]
    public SGSettings settings;

    [Header("Squad")]
    public SGSquadData squadData;

    // ── Events ────────────────────────────────────────────────────────────────

    public static event Action<int>   OnScoreChanged;
    public static event Action<float> OnEnergyChanged;      // 0-1 normalized
    public static event Action        OnConeFilled;
    public static event Action<float> OnXpChanged;          // 0-1 normalized
    public static event Action<int>   OnLevelUpReady;       // passes character index
    public static event Action<int>   OnCharacterUnlocked;  // passes newly unlocked index
    public static event Action        OnFuryStarted;
    public static event Action        OnFuryEnded;
    public static event Action        OnGameStarted;
    public static event Action        OnGameOver;
    public static event Action<int>   OnComboChanged;

    // ── State ─────────────────────────────────────────────────────────────────

    public enum GameState { Tutorial, Playing, Dead, GameOver }

    public GameState State { get; private set; } = GameState.Tutorial;

    public int   Score        { get; private set; }
    public float Energy       { get; private set; }
    public float XpNormalized { get; private set; }
    public int   Combo        { get; private set; }
    public bool  FuryActive   { get; private set; }

    private float rawXp;
    private float furyTimer;
    private float survivalTime;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Time.timeScale = 1f;

        // Reset squad each run so unlocks are re-earned through score
        squadData?.ResetForSession();
    }

    private void Start()
    {
        OnGameStarted?.Invoke();
    }

    private void Update()
    {
        if (State != GameState.Playing) return;

        survivalTime += Time.deltaTime;

        if (FuryActive)
        {
            furyTimer -= Time.deltaTime;
            if (furyTimer <= 0f)
                EndFury();
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Transitions from tutorial to live gameplay.</summary>
    public void BeginPlay()
    {
        State = GameState.Playing;
    }

    /// <summary>
    /// Called by <see cref="SGSlash"/> on a successful parry.
    /// Handles score, energy, XP, combo, lucky/fury character abilities.
    /// </summary>
    public void NotifyParry(Vector3 worldPos)
    {
        if (State != GameState.Playing && State != GameState.Tutorial) return;

        // ── Combo ──────────────────────────────────────────────────────────
        Combo++;
        OnComboChanged?.Invoke(Combo);

        // ── Score ──────────────────────────────────────────────────────────
        int basePoints = 1;

        bool luckyDouble = squadData != null
            && squadData.IsUnlocked(SGCharacterType.Lucky)
            && UnityEngine.Random.value < 0.15f;

        float multiplier = FuryActive ? settings.furyScoreMultiplier : 1f;
        int gained = Mathf.RoundToInt(basePoints * multiplier * (luckyDouble ? 2f : 1f));

        Score += gained;
        OnScoreChanged?.Invoke(Score);

        // ── Score-based character unlock ───────────────────────────────────
        if (squadData != null)
        {
            int newUnlock = squadData.TryUnlockForScore(Score);
            if (newUnlock >= 0)
                OnCharacterUnlocked?.Invoke(newUnlock);
        }

        // ── Energy ─────────────────────────────────────────────────────────
        float energyGain = settings.energyPerParry;
        Energy = Mathf.Clamp(Energy + energyGain, 0f, settings.energyCapacity);
        OnEnergyChanged?.Invoke(Energy / settings.energyCapacity);

        if (Mathf.Approximately(Energy, settings.energyCapacity))
        {
            Energy = 0f;
            OnEnergyChanged?.Invoke(0f);
            Score += settings.pointsPerFill;
            OnScoreChanged?.Invoke(Score);
            OnConeFilled?.Invoke();

            // Re-check unlocks after cone bonus
            if (squadData != null)
            {
                int newUnlock = squadData.TryUnlockForScore(Score);
                if (newUnlock >= 0)
                    OnCharacterUnlocked?.Invoke(newUnlock);
            }
        }

        // ── XP ─────────────────────────────────────────────────────────────
        rawXp += settings.xpPerParry;
        if (rawXp >= settings.xpPerLevel)
        {
            rawXp -= settings.xpPerLevel;
            // Pick a random unlocked character to offer upgrade
            int candidate = squadData != null
                ? squadData.GetRandomUnlockedIndex()
                : 0;
            OnLevelUpReady?.Invoke(candidate);
        }

        XpNormalized = Mathf.Clamp01(rawXp / settings.xpPerLevel);
        OnXpChanged?.Invoke(XpNormalized);

        // ── Fury combo (Fury character) ────────────────────────────────────
        if (squadData != null && squadData.IsUnlocked(SGCharacterType.Fury)
            && Combo >= settings.furyComboThreshold && !FuryActive)
        {
            StartFury();
        }
    }

    /// <summary>Called when the player misses a parry and is hit.</summary>
    public void NotifyHit()
    {
        if (State != GameState.Playing && State != GameState.Tutorial) return;

        Combo = 0;
        OnComboChanged?.Invoke(Combo);
        State = GameState.Dead;
        StartCoroutine(GameOverSequence());
    }

    /// <summary>Auto-parry triggered by Guardian character.</summary>
    public void NotifyAutoParry(Vector3 worldPos)
    {
        NotifyParry(worldPos);
    }

    /// <summary>Restarts the scene.</summary>
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ── Fury ──────────────────────────────────────────────────────────────────

    private void StartFury()
    {
        FuryActive = true;
        furyTimer  = settings.furyDuration;
        Combo      = 0;
        OnFuryStarted?.Invoke();
    }

    private void EndFury()
    {
        FuryActive = false;
        OnFuryEnded?.Invoke();
    }

    // ── Game-over sequence ────────────────────────────────────────────────────

    private IEnumerator GameOverSequence()
    {
        Time.timeScale = settings.slowMoScale;
        yield return new WaitForSecondsRealtime(settings.slowMoDuration);
        Time.timeScale = 1f;

        State = GameState.GameOver;
        GameEndData.Set(Score, GameType.SlashGame);
        OnGameOver?.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
