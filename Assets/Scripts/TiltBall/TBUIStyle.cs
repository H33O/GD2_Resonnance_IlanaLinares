using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Centralise la charte graphique UI du TiltBall :
/// typographie JimNightshade + sprite jaugenormal sur tous les supports.
///
/// Initialisé une fois via <see cref="Init"/> (appelé par TBSceneSetup.BuildLevel),
/// puis disponible partout avec <see cref="ApplyFont"/> et <see cref="ApplyJauge"/>.
/// </summary>
public static class TBUIStyle
{
    private static TMP_FontAsset _font;
    private static Sprite        _jauge;

    // ── Initialisation ────────────────────────────────────────────────────────

    /// <summary>
    /// Injecte la police SDF et le sprite jauge depuis TBLevelPrefabsData.
    /// Appelé une fois par TBSceneSetup avant toute construction UI.
    /// </summary>
    public static void Init(TMP_FontAsset font, Sprite jaugeSprite)
    {
        if (font        != null) _font  = font;
        if (jaugeSprite != null) _jauge = jaugeSprite;
    }

    // ── Typographie ───────────────────────────────────────────────────────────

    /// <summary>Applique JimNightshade sur un TextMeshProUGUI.</summary>
    public static void ApplyFont(TextMeshProUGUI tmp)
    {
        if (tmp == null || _font == null) return;
        tmp.font = _font;
    }

    /// <summary>Applique la police sur tous les TMP enfants d'un Transform.</summary>
    public static void ApplyFontAll(Transform root)
    {
        if (root == null || _font == null) return;
        foreach (var tmp in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            tmp.font = _font;
    }

    // ── Sprite jauge ─────────────────────────────────────────────────────────

    /// <summary>
    /// Applique jaugenormal en mode Sliced sur un fond Image.
    /// Préserve les bords arrondis à toute taille.
    /// </summary>
    public static void ApplyJauge(Image img)
    {
        if (img == null || _jauge == null) return;
        img.sprite = _jauge;
        img.type   = Image.Type.Sliced;
    }

    /// <summary>Applique jaugenormal + teinte sur un fond Image.</summary>
    public static void ApplyJauge(Image img, Color tint)
    {
        ApplyJauge(img);
        if (img != null) img.color = tint;
    }

    /// <summary>
    /// Configure une barre de progression horizontale avec le sprite jauge.
    /// </summary>
    public static void ApplyJaugeFill(Image img, Color fillColor, float fillAmount = 1f)
    {
        if (img == null || _jauge == null) return;
        img.sprite     = _jauge;
        img.type       = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillAmount = fillAmount;
        img.color      = fillColor;
    }

    /// <summary>Retourne vrai si la police est prête à l'emploi.</summary>
    public static bool HasFont  => _font  != null;

    /// <summary>Retourne vrai si le sprite jauge est prêt à l'emploi.</summary>
    public static bool HasJauge => _jauge != null;
}
