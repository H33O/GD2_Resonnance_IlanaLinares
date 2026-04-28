using UnityEngine;

/// <summary>
/// Gestionnaire de boutique — désactivé, système XP supprimé.
/// Conservé pour éviter les erreurs de compilation sur les références existantes.
/// </summary>
public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>Crée le singleton s'il est absent.</summary>
    public static ShopManager EnsureExists()
    {
        if (Instance != null) return Instance;
        return new GameObject("ShopManager").AddComponent<ShopManager>();
    }
}
