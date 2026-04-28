using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Référentiel d'assets partagés pour la construction procédurale du menu et des mini-jeux.
/// Initialisé par <see cref="MenuMainSetup"/> avant tout builder.
/// </summary>
public static class MenuAssets
{
    /// <summary>Sprite assigné à tous les boutons du menu.</summary>
    public static Sprite ButtonSprite { get; private set; }

    /// <summary>Police Michroma (SDF) appliquée sur tous les textes.</summary>
    public static TMP_FontAsset Font { get; private set; }

    /// <summary>Toujours null — les badges sprites ont été supprimés. Conservé pour compatibilité.</summary>
    public static Sprite LockSprite      => null;

    /// <summary>Toujours null — les badges sprites ont été supprimés. Conservé pour compatibilité.</summary>
    public static Sprite TextBadgeSprite => null;

    // ── Initialisation ────────────────────────────────────────────────────────

    /// <summary>Appelé une fois dans MenuMainSetup.Start() avant tout Build*().</summary>
    public static void Init(Sprite buttonSprite, TMP_FontAsset font = null,
                            Sprite lockSprite = null, Sprite textBadgeSprite = null)
    {
        ButtonSprite = buttonSprite;

        // Michroma est prioritaire — on ignore la font passée en paramètre
        Font = LoadMichroma();
    }

    // ── Chargement Michroma ───────────────────────────────────────────────────

    private static TMP_FontAsset LoadMichroma()
    {
        // Tentative 1 : Resources/
        var f = Resources.Load<TMP_FontAsset>("Michroma-Regular SDF");
        if (f != null) return f;

        // Tentative 2 : AssetDatabase (éditeur uniquement, pour les tests en Play Mode)
#if UNITY_EDITOR
        f = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/font/Michroma/Michroma-Regular SDF.asset");
        if (f != null) return f;

        // Tentative 3 : chemin alternatif
        f = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/font/Michroma-Regular SDF.asset");
        if (f != null) return f;
#endif
        Debug.LogWarning("[MenuAssets] Michroma-Regular SDF introuvable. " +
                         "Génère le font asset TMP depuis Assets/font/Michroma-Regular.ttf " +
                         "via Window > TextMeshPro > Font Asset Creator.");
        return null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Applique le sprite bouton sur une Image.</summary>
    public static void ApplyButtonSprite(Image img)
    {
        if (ButtonSprite == null || img == null) return;
        img.sprite = ButtonSprite;
        img.type   = Image.Type.Sliced;
        img.color  = Color.white;
    }

    /// <summary>
    /// Applique uniquement la police Michroma sur un TextMeshProUGUI.
    /// Ne touche PAS à la couleur — chaque builder gère sa propre couleur.
    /// </summary>
    public static void ApplyFont(TextMeshProUGUI tmp)
    {
        if (tmp == null) return;
        if (Font != null) tmp.font = Font;
    }

    /// <summary>Alias de compatibilité — applique la police sans badge.</summary>
    public static void ApplyFontAndBadge(TextMeshProUGUI tmp, float padding = 18f)
    {
        ApplyFont(tmp);
    }
}
