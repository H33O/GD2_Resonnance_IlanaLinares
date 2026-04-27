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

    /// <summary>Police JimNightshade (SDF) appliquée sur tous les textes.</summary>
    public static TMP_FontAsset Font { get; private set; }

    /// <summary>Sprite du cadenas affiché sur la porte verrouillée.</summary>
    public static Sprite LockSprite { get; private set; }

    /// <summary>Sprite "jaugenormal" affiché en fond derrière certains textes.</summary>
    public static Sprite TextBadgeSprite { get; private set; }

    // ── Initialisation ────────────────────────────────────────────────────────

    /// <summary>Appelé une fois dans MenuMainSetup.Start() avant tout Build*().</summary>
    public static void Init(Sprite buttonSprite, TMP_FontAsset font = null,
                            Sprite lockSprite = null, Sprite textBadgeSprite = null)
    {
        ButtonSprite    = buttonSprite;
        Font            = font;
        LockSprite      = lockSprite;
        TextBadgeSprite = textBadgeSprite;

        // Charger JimNightshade automatiquement si aucune font n'est fournie
        if (Font == null)
            Font = LoadJimNightshade();

        // Charger jaugenormal automatiquement si non fourni
        if (TextBadgeSprite == null)
            TextBadgeSprite = LoadJaugeNormal();
    }

    // ── Chargement jaugenormal ────────────────────────────────────────────────

    private static Sprite LoadJaugeNormal()
    {
#if UNITY_EDITOR
        // Charger la texture depuis Assets/sprites/jaugenormal.png
        var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/sprites/jaugenormal.png");
        if (tex != null)
        {
            // Chercher le sprite principal du même asset
            var sprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(
                "Assets/sprites/jaugenormal.png");
            foreach (var obj in sprites)
            {
                if (obj is Sprite sp) return sp;
            }
            // Aucun sprite importé → créer un sprite depuis la texture brute
            return Sprite.Create(tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), 100f,
                0, SpriteMeshType.FullRect,
                new Vector4(8, 8, 8, 8));  // 9-slice : 8 px de bordure
        }
#endif
        return null;
    }

    // ── Chargement JimNightshade ──────────────────────────────────────────────

    private static TMP_FontAsset LoadJimNightshade()
    {
        // Tentative 1 : Resources/
        var f = Resources.Load<TMP_FontAsset>("JimNightshade-Regular SDF");
        if (f != null) return f;

        // Tentative 2 : AssetDatabase (éditeur uniquement, pour les tests en Play Mode)
#if UNITY_EDITOR
        f = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/font/Jim_Nightshade/JimNightshade-Regular SDF.asset");
        if (f != null) return f;
#endif
        Debug.LogWarning("[MenuAssets] JimNightshade-Regular SDF introuvable. " +
                         "Pour un build, copie-la dans un dossier Resources/.");
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
    /// Applique uniquement la police JimNightshade sur un TextMeshProUGUI.
    /// Ne touche PAS à la couleur — chaque builder gère sa propre couleur.
    /// </summary>
    public static void ApplyFont(TextMeshProUGUI tmp)
    {
        if (tmp == null) return;
        if (Font != null) tmp.font = Font;
        // ⚠️  Intentionnellement sans tmp.color = ... pour préserver les couleurs des builders.
    }

    /// <summary>
    /// Applique la police et insère le badge sprite en fond derrière le texte.
    /// </summary>
    public static void ApplyFontAndBadge(TextMeshProUGUI tmp, float padding = 18f)
    {
        ApplyFont(tmp);
        UIHelper.AddTextBadge(tmp, TextBadgeSprite, padding);
    }
}
