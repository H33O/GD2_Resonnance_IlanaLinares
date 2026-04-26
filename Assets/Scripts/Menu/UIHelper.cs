using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Utilitaires UI partagés entre tous les scripts de construction procédurale.
/// </summary>
public static class UIHelper
{
    /// <summary>
    /// Insère une <see cref="Image"/> avec le sprite <paramref name="badgeSprite"/> comme fond
    /// derrière le <paramref name="tmp"/> donné, dimensionnée légèrement plus grande que le texte
    /// grâce au <paramref name="padding"/> (px de chaque côté).
    ///
    /// L'image est ajoutée comme premier sibling du même parent que le TMP,
    /// afin d'apparaître derrière.
    /// </summary>
    public static void AddTextBadge(TextMeshProUGUI tmp, Sprite badgeSprite, float padding = 18f)
    {
        if (tmp == null || badgeSprite == null) return;

        var srcRT = tmp.rectTransform;
        var parent = srcRT.parent as RectTransform;
        if (parent == null) return;

        var badgeGO = new GameObject($"{tmp.gameObject.name}_Badge");
        badgeGO.transform.SetParent(parent, false);

        // Placer derrière le TMP dans la hiérarchie
        int idx = srcRT.GetSiblingIndex();
        badgeGO.transform.SetSiblingIndex(idx);   // TMP sera idx+1

        var img          = badgeGO.AddComponent<Image>();
        img.sprite       = badgeSprite;
        img.type         = Image.Type.Sliced;
        img.color        = Color.white;
        img.raycastTarget = false;
        img.preserveAspect = false;

        // Copier les anchors du TMP + padding
        var badgeRT      = img.rectTransform;
        badgeRT.anchorMin = srcRT.anchorMin;
        badgeRT.anchorMax = srcRT.anchorMax;
        badgeRT.pivot     = srcRT.pivot;
        badgeRT.offsetMin = srcRT.offsetMin - new Vector2(padding, padding);
        badgeRT.offsetMax = srcRT.offsetMax + new Vector2(padding, padding);
    }
}
