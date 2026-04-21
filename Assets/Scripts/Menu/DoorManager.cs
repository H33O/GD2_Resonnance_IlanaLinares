using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Porte centrale du menu — plane blanc centré à l'écran.
///
/// Assigne <see cref="doorSprite"/> dans l'Inspector pour remplacer le plane blanc procédural.
/// </summary>
public class DoorManager : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Porte")]
    [Tooltip("Sprite de la porte. Laisser vide = plane blanc procédural.")]
    [SerializeField] public Sprite doorSprite;

    [Tooltip("Teinte appliquée sur le sprite.")]
    [SerializeField] public Color doorTint = Color.white;

    // ── Mise en page ──────────────────────────────────────────────────────────

    private const float DoorW = 500f;
    private const float DoorH = 800f;

    // ── Point d'entrée ────────────────────────────────────────────────────────

    /// <summary>Construit la porte dans le canvas fourni. Appelé par <see cref="MenuSceneSetup"/>.</summary>
    public void InitUI(RectTransform canvasRT)
    {
        BuildDoorPlane(canvasRT);
    }

    // ── Construction ──────────────────────────────────────────────────────────

    private void BuildDoorPlane(RectTransform canvasRT)
    {
        var go  = new GameObject("Door");
        go.transform.SetParent(canvasRT, false);

        var img         = go.AddComponent<Image>();
        img.sprite      = doorSprite != null ? doorSprite : SpriteGenerator.CreateWhiteSquare();
        img.color       = doorTint;
        img.raycastTarget = false;

        if (doorSprite != null)
            img.preserveAspect = true;

        var rt              = img.rectTransform;
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(DoorW, DoorH);
        rt.anchoredPosition = Vector2.zero;
    }
}

