using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gère les trois jauges de survie (Eau, Nourriture, Sommeil) et le système de cycles.
///
/// - Un cycle dure <see cref="CycleDurationSeconds"/> secondes (défaut : 600 s = 10 min).
/// - À chaque fin de cycle, chaque jauge perd <see cref="DrainPerCycle"/> points (0-100).
/// - La touche <b>E</b> avance immédiatement au cycle suivant (mode test).
/// - L'événement <see cref="OnNeedsChanged"/> est déclenché à chaque changement.
/// - L'événement <see cref="OnDayAdvanced"/> est déclenché à chaque nouveau jour.
/// </summary>
public class NeedsManager : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────────────────────

    public static NeedsManager Instance { get; private set; }

    // ── Configuration (modifiable depuis l'Inspector) ──────────────────────────

    [Header("Cycle")]
    [Tooltip("Durée d'un cycle en secondes (600 = 10 minutes).")]
    [SerializeField] public float CycleDurationSeconds = 600f;

    [Tooltip("Perte de jauge par cycle (0-100). 100 = jauge vide en exactement 1 cycle (10 min).")]
    [SerializeField] public float DrainPerCycle = 100f;

    // ── Événements ────────────────────────────────────────────────────────────

    /// <summary>Déclenché quand une ou plusieurs jauges changent. Fournit les 3 valeurs (0-100).</summary>
    public event Action<float, float, float> OnNeedsChanged;

    /// <summary>Déclenché quand un nouveau jour commence.</summary>
    public event Action<int> OnDayAdvanced;

    // ── État ──────────────────────────────────────────────────────────────────

    private float _water      = 100f;
    private float _food       = 100f;
    private float _sleep      = 100f;
    private float _cycleTimer = 0f;
    private int   _day        = 1;

    /// <summary>Valeur de la jauge Eau (0-100).</summary>
    public float Water => _water;

    /// <summary>Valeur de la jauge Nourriture (0-100).</summary>
    public float Food => _food;

    /// <summary>Valeur de la jauge Sommeil (0-100).</summary>
    public float Sleep => _sleep;

    /// <summary>Jour actuel (commence à 1).</summary>
    public int Day => _day;

    /// <summary>Progression du cycle actuel (0-1).</summary>
    public float CycleProgress => CycleDurationSeconds > 0f
        ? Mathf.Clamp01(_cycleTimer / CycleDurationSeconds)
        : 0f;

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

    private void Update()
    {
        _cycleTimer += Time.deltaTime;

        // Drain continu proportionnel au cycle : chaque frame les jauges descendent
        // de DrainPerCycle / CycleDurationSeconds * deltaTime
        if (CycleDurationSeconds > 0f)
        {
            float drainThisFrame = (DrainPerCycle / CycleDurationSeconds) * Time.deltaTime;
            _water = Mathf.Clamp(_water - drainThisFrame, 0f, 100f);
            _food  = Mathf.Clamp(_food  - drainThisFrame, 0f, 100f);
            _sleep = Mathf.Clamp(_sleep - drainThisFrame, 0f, 100f);
            OnNeedsChanged?.Invoke(_water, _food, _sleep);
        }

        // Touche E : avancer au cycle suivant immédiatement (test)
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            AdvanceCycle();

        if (_cycleTimer >= CycleDurationSeconds)
            AdvanceCycle();
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>Recharge la jauge Eau d'un montant donné (0-100 max).</summary>
    public void RefillWater(float amount)
    {
        _water = Mathf.Clamp(_water + amount, 0f, 100f);
        OnNeedsChanged?.Invoke(_water, _food, _sleep);
    }

    /// <summary>Recharge la jauge Nourriture d'un montant donné (0-100 max).</summary>
    public void RefillFood(float amount)
    {
        _food = Mathf.Clamp(_food + amount, 0f, 100f);
        OnNeedsChanged?.Invoke(_water, _food, _sleep);
    }

    /// <summary>Recharge la jauge Sommeil d'un montant donné (0-100 max).</summary>
    public void RefillSleep(float amount)
    {
        _sleep = Mathf.Clamp(_sleep + amount, 0f, 100f);
        OnNeedsChanged?.Invoke(_water, _food, _sleep);
    }

    // ── Logique interne ───────────────────────────────────────────────────────

    private void AdvanceCycle()
    {
        _cycleTimer = 0f;
        _day++;

        // Le drain est géré en continu dans Update() — pas de saut supplémentaire ici.
        OnNeedsChanged?.Invoke(_water, _food, _sleep);
        OnDayAdvanced?.Invoke(_day);
    }

    // ── EnsureExists ─────────────────────────────────────────────────────────

    /// <summary>Crée le singleton s'il n'existe pas encore.</summary>
    public static NeedsManager EnsureExists()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("NeedsManager");
        return go.AddComponent<NeedsManager>();
    }
}
