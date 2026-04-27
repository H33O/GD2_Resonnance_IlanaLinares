using System;
using UnityEngine;

/// <summary>
/// Gère les achats en boutique (coffre mystère uniquement).
///
/// Les consommables Eau / Nourriture / Sommeil ont été supprimés.
/// Le coffre mystère reste disponible comme récompense aléatoire.
/// </summary>
public class ShopManager : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────────────────────

    public static ShopManager Instance { get; private set; }

    // ── Configuration ─────────────────────────────────────────────────────────

    [Header("Prix des articles")]
    [Tooltip("Prix du coffre mystère.")]
    [SerializeField] public int ChestCost = 500;

    // ── Événements ────────────────────────────────────────────────────────────

    /// <summary>Déclenché après un achat. success = fonds suffisants, itemName = article acheté.</summary>
    public event Action<bool, string, int> OnPurchaseResult;

    /// <summary>Déclenché quand le solde de pièces change (relais de ScoreManager).</summary>
    public event Action<int> OnCoinsChanged;

    // ── Propriétés ────────────────────────────────────────────────────────────

    /// <summary>Solde actuel de pièces (lu depuis ScoreManager).</summary>
    public int Coins => ScoreManager.Instance != null ? ScoreManager.Instance.GetTotalCoins() : 0;

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

    private void OnEnable()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnCoinsAdded += ForwardCoinsChanged;
    }

    private void OnDisable()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnCoinsAdded -= ForwardCoinsChanged;
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>Tente d'ouvrir le coffre mystère.</summary>
    public void BuyChest() => TryPurchase("Coffre", ChestCost, OnChestOpened);

    // ── Logique interne ───────────────────────────────────────────────────────

    private void TryPurchase(string itemName, int cost, Action onSuccess)
    {
        int currentCoins = ScoreManager.Instance != null ? ScoreManager.Instance.GetTotalCoins() : 0;

        if (currentCoins < cost)
        {
            OnPurchaseResult?.Invoke(false, itemName, currentCoins);
            return;
        }

        ScoreManager.Instance?.SpendCoins(cost);
        onSuccess?.Invoke();
        int remaining = ScoreManager.Instance != null ? ScoreManager.Instance.GetTotalCoins() : 0;
        OnPurchaseResult?.Invoke(true, itemName, remaining);
    }

    private void OnChestOpened()
    {
        Debug.Log("[ShopManager] Coffre ouvert ! Ajoute ta récompense ici.");
    }

    private void ForwardCoinsChanged(int amount, int newTotal)
    {
        OnCoinsChanged?.Invoke(newTotal);
    }

    // ── EnsureExists ─────────────────────────────────────────────────────────

    /// <summary>Crée le singleton s'il est absent.</summary>
    public static ShopManager EnsureExists()
    {
        if (Instance != null) return Instance;
        ScoreManager.EnsureExists();
        var go = new GameObject("ShopManager");
        return go.AddComponent<ShopManager>();
    }
}
