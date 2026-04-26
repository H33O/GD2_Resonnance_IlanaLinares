using System;
using UnityEngine;

/// <summary>
/// Gère la boutique et le coffre du menu.
///
/// Responsabilités :
///   - Stocker le solde de pièces du joueur (séparé du <see cref="ScoreManager"/>).
///   - Traiter les achats (Eau, Nourriture, Sommeil, Coffre).
///   - Déclencher <see cref="OnPurchaseResult"/> après chaque tentative.
/// </summary>
public class ShopManager : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────────────────────

    public static ShopManager Instance { get; private set; }

    // ── Configuration (modifiable depuis l'Inspector) ──────────────────────────

    [Header("Prix des articles")]
    [Tooltip("Prix d'une recharge d'eau.")]
    [SerializeField] public int WaterCost    = 30;

    [Tooltip("Prix d'une recharge de nourriture.")]
    [SerializeField] public int FoodCost     = 40;

    [Tooltip("Prix d'une recharge de sommeil.")]
    [SerializeField] public int SleepCost    = 50;

    [Tooltip("Prix du coffre mystère.")]
    [SerializeField] public int ChestCost    = 500;

    [Header("Quantité rechargée par achat (0-100)")]
    [SerializeField] public float RefillAmount = 50f;

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

    /// <summary>Tente d'acheter une recharge d'eau.</summary>
    public void BuyWater()  => TryPurchase("Eau",        WaterCost, () =>
        NeedsManager.Instance?.RefillWater(RefillAmount));

    /// <summary>Tente d'acheter une recharge de nourriture.</summary>
    public void BuyFood()   => TryPurchase("Nourriture", FoodCost,  () =>
        NeedsManager.Instance?.RefillFood(RefillAmount));

    /// <summary>Tente d'acheter une recharge de sommeil.</summary>
    public void BuySleep()  => TryPurchase("Sommeil",    SleepCost, () =>
        NeedsManager.Instance?.RefillSleep(RefillAmount));

    /// <summary>Tente d'ouvrir le coffre.</summary>
    public void BuyChest()  => TryPurchase("Coffre",     ChestCost, OnChestOpened);

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
        // Espace réservé — place ta récompense de coffre ici.
        Debug.Log("[ShopManager] Coffre ouvert ! Ajoute ta récompense ici.");
    }

    private void ForwardCoinsChanged(int amount, int newTotal)
    {
        OnCoinsChanged?.Invoke(newTotal);
    }

    // ── EnsureExists ─────────────────────────────────────────────────────────

    /// <summary>Crée le singleton s'il n'existe pas encore.</summary>
    public static ShopManager EnsureExists()
    {
        if (Instance != null) return Instance;
        ScoreManager.EnsureExists();
        var go = new GameObject("ShopManager");
        return go.AddComponent<ShopManager>();
    }
}
