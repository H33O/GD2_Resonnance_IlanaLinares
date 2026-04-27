using System;
using UnityEngine;

/// <summary>
/// Gère les achats en boutique (coffre mystère uniquement).
/// La monnaie est désormais l'XP totale accumulée.
/// </summary>
public class ShopManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static ShopManager Instance { get; private set; }

    // ── Configuration ─────────────────────────────────────────────────────────

    [Header("Prix des articles")]
    [Tooltip("Coût en XP du coffre mystère.")]
    [SerializeField] public int ChestCost = 500;

    // ── Événements ────────────────────────────────────────────────────────────

    /// <summary>Déclenché après un achat. success = fonds suffisants, itemName = article acheté.</summary>
    public event Action<bool, string, int> OnPurchaseResult;

    /// <summary>Déclenché quand le total d'XP change (relais de ScoreManager).</summary>
    public event Action<int> OnCoinsChanged;

    // ── Propriétés ────────────────────────────────────────────────────────────

    /// <summary>Total d'XP actuel (lu depuis ScoreManager).</summary>
    public int Coins => ScoreManager.Instance != null ? ScoreManager.Instance.GetTotalXP() : 0;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnXPAdded += ForwardCoinsChanged;
    }

    private void OnDisable()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnXPAdded -= ForwardCoinsChanged;
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>Tente d'ouvrir le coffre mystère.</summary>
    public void BuyChest() => TryPurchase("Coffre", ChestCost, OnChestOpened);

    // ── Logique interne ───────────────────────────────────────────────────────

    private void TryPurchase(string itemName, int cost, Action onSuccess)
    {
        int currentXP = ScoreManager.Instance != null ? ScoreManager.Instance.GetTotalXP() : 0;
        if (currentXP < cost)
        {
            OnPurchaseResult?.Invoke(false, itemName, currentXP);
            return;
        }
        onSuccess?.Invoke();
        int remaining = ScoreManager.Instance != null ? ScoreManager.Instance.GetTotalXP() : 0;
        OnPurchaseResult?.Invoke(true, itemName, remaining);
    }

    private void OnChestOpened()
    {
        Debug.Log("[ShopManager] Coffre ouvert !");
    }

    private void ForwardCoinsChanged(int amount, int newTotal)
    {
        OnCoinsChanged?.Invoke(newTotal);
    }

    // ── EnsureExists ──────────────────────────────────────────────────────────

    /// <summary>Crée le singleton s'il est absent.</summary>
    public static ShopManager EnsureExists()
    {
        if (Instance != null) return Instance;
        ScoreManager.EnsureExists();
        return new GameObject("ShopManager").AddComponent<ShopManager>();
    }
}
