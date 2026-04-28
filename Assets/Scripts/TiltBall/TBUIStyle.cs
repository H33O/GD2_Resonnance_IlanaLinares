using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Centralise la charte graphique UI du TiltBall :
/// typographie Michroma (via MenuAssets) + fond carré noir semi-opaque.
/// Plus aucun sprite jaugenormal n'est utilisé.
/// </summary>
public static class TBUIStyle
{
    private static TMP_FontAsset _font;

    // ── Initialisation ────────────────────────────────────────────────────────

    /// <summary>
    /// Injecte la police SDF. Le paramètre jaugeSprite est ignoré (supprimé).
    /// Appelé une fois par TBSceneSetup avant toute construction UI.
    /// </summary>
    public static void Init(TMP_FontAsset font, Sprite jaugeSprite = null)
    {
        // Priorité : font injectée, sinon MenuAssets.Font (Michroma)
        _font = font != null ? font : MenuAssets.Font;
    }

    // ── Typographie ───────────────────────────────────────────────────────────

    /// <summary>Applique Michroma sur un TextMeshProUGUI.</summary>
    public static void ApplyFont(TextMeshProUGUI tmp)
    {
        if (tmp == null) return;
        var f = _font ?? MenuAssets.Font;
        if (f != null) tmp.font = f;
    }

    /// <summary>Applique la police sur tous les TMP enfants d'un Transform.</summary>
    public static void ApplyFontAll(Transform root)
    {
        if (root == null) return;
        var f = _font ?? MenuAssets.Font;
        if (f == null) return;
        foreach (var tmp in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            tmp.font = f;
    }

    // ── Fond carré noir ───────────────────────────────────────────────────────

    /// <summary>
    /// Applique un fond carré noir semi-opaque sur une Image (remplace jaugenormal).
    /// </summary>
    public static void ApplyJauge(Image img)
    {
        if (img == null) return;
        img.sprite = SpriteGenerator.CreateWhiteSquare();
        img.type   = Image.Type.Simple;
    }

    /// <summary>Applique le fond carré avec une teinte spécifique.</summary>
    public static void ApplyJauge(Image img, Color tint)
    {
        ApplyJauge(img);
        if (img != null) img.color = tint;
    }

    /// <summary>
    /// Configure une barre de progression horizontale simple sans sprite jauge.
    /// </summary>
    public static void ApplyJaugeFill(Image img, Color fillColor, float fillAmount = 1f)
    {
        if (img == null) return;
        img.sprite     = SpriteGenerator.CreateWhiteSquare();
        img.type       = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillAmount = fillAmount;
        img.color      = fillColor;
    }

    /// <summary>Retourne vrai si la police est prête à l'emploi.</summary>
    public static bool HasFont  => (_font ?? MenuAssets.Font) != null;

    /// <summary>Toujours vrai — plus de dépendance à un sprite jauge.</summary>
    public static bool HasJauge => true;
}
