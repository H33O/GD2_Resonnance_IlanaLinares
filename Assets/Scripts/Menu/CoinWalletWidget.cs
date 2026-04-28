using UnityEngine;

/// <summary>
/// Widget wallet — désactivé, système XP supprimé.
/// Conservé pour éviter les erreurs de compilation sur les références existantes.
/// </summary>
public class CoinWalletWidget : MonoBehaviour
{
    /// <summary>Crée le widget (no-op).</summary>
    public static CoinWalletWidget Create(RectTransform canvasRT)
    {
        var go = new GameObject("CoinWalletWidget");
        go.transform.SetParent(canvasRT, false);
        return go.AddComponent<CoinWalletWidget>();
    }
}
