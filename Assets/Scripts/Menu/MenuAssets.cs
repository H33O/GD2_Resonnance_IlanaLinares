using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Référentiel d'assets partagés pour la construction procédurale du menu et des mini-jeux.
/// Initialisé par <see cref="MenuSceneSetup"/> avant tout builder.
/// </summary>
public static class MenuAssets
{
    /// <summary>Sprite assigné à tous les boutons du menu (ex : "bouton UI.png").</summary>
    public static Sprite ButtonSprite { get; private set; }

    /// <summary>Police Michroma (SDF) appliquée sur tous les textes.</summary>
    public static TMP_FontAsset Font { get; private set; }

    /// <summary>Sprite du cadenas affiché sur la porte verrouillée (cadena.png).</summary>
    public static Sprite LockSprite { get; private set; }

    /// <summary>Sprite "jaugenormal" affiché en fond derrière tous les textes.</summary>
    public static Sprite TextBadgeSprite { get; private set; }

    // ── Initialisation ────────────────────────────────────────────────────────

    /// <summary>Doit être appelé une fois dans MenuSceneSetup.Start() avant tout Build*().</summary>
    public static void Init(Sprite buttonSprite, TMP_FontAsset font = null,
                            Sprite lockSprite = null, Sprite textBadgeSprite = null)
    {
        ButtonSprite    = buttonSprite;
        Font            = font;
        LockSprite      = lockSprite;
        TextBadgeSprite = textBadgeSprite;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Applique le sprite bouton sur une <see cref="Image"/> existante.
    /// </summary>
    public static void ApplyButtonSprite(Image img)
    {
        if (ButtonSprite == null || img == null) return;
        img.sprite = ButtonSprite;
        img.type   = Image.Type.Sliced;
        img.color  = Color.white;
    }

    /// <summary>
    /// Applique la police Michroma et la couleur noire sur un <see cref="TextMeshProUGUI"/>.
    /// </summary>
    public static void ApplyFont(TextMeshProUGUI tmp)
    {
        if (tmp == null) return;
        if (Font != null) tmp.font = Font;
        tmp.color = Color.black;
    }

    /// <summary>
    /// Applique la police Michroma, met le texte en noir, et insère
    /// le sprite <see cref="TextBadgeSprite"/> (jaugenormal) en fond derrière le texte.
    /// À appeler après avoir parenté le TMP à son parent RectTransform.
    /// </summary>
    public static void ApplyFontAndBadge(TextMeshProUGUI tmp, float padding = 18f)
    {
        ApplyFont(tmp);
        UIHelper.AddTextBadge(tmp, TextBadgeSprite, padding);
    }
}
