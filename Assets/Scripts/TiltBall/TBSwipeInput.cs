using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Détecte les swipes sur l'écran entier et expose une direction normalisée.
///
/// Utilisation :
///   Vector2 dir = TBSwipeInput.Instance?.Direction ?? Vector2.zero;
///
/// - Le premier contact pose l'ancre.
/// - Le déplacement du doigt définit la direction et l'intensité (plafonné à 1).
/// - Le relâchement remet la direction à zéro.
/// - Compatible multi-touch : seul le premier doigt est suivi.
/// </summary>
public class TBSwipeInput : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public static TBSwipeInput Instance { get; private set; }

    // ── Paramètres ────────────────────────────────────────────────────────────

    /// <summary>Distance (px) à parcourir pour atteindre la vitesse maximale.</summary>
    private const float FullSpeedDistance = 80f;

    /// <summary>Zone morte en pixels — en-dessous, la direction reste nulle.</summary>
    private const float DeadZonePx = 8f;

    // ── État ──────────────────────────────────────────────────────────────────

    /// <summary>Direction normalisée [-1,1] lue par TBPlayerController.</summary>
    public Vector2 Direction { get; private set; }

    private int     touchId    = -1;
    private Vector2 originPx;               // position écran du premier contact

    // ── Création statique ─────────────────────────────────────────────────────

    /// <summary>
    /// Crée la zone de swipe (plein écran, transparent) sur le Canvas fourni.
    /// </summary>
    public static TBSwipeInput Create(RectTransform canvasRT)
    {
        var go  = new GameObject("SwipeInput");
        go.transform.SetParent(canvasRT, false);

        // Image transparente plein canvas — capte tous les événements pointer
        var img           = go.AddComponent<Image>();
        img.color         = Color.clear;
        img.raycastTarget = true;

        var rt        = img.rectTransform;
        rt.anchorMin  = Vector2.zero;
        rt.anchorMax  = Vector2.one;
        rt.offsetMin  = rt.offsetMax = Vector2.zero;

        // Place sous les autres éléments du HUD (le bouton menu reste au-dessus)
        go.transform.SetAsFirstSibling();

        var swipe  = go.AddComponent<TBSwipeInput>();
        Instance   = swipe;
        return swipe;
    }

    // ── Pointer events ────────────────────────────────────────────────────────

    public void OnPointerDown(PointerEventData e)
    {
        if (touchId != -1) return;
        touchId  = e.pointerId;
        originPx = e.position;
    }

    public void OnDrag(PointerEventData e)
    {
        if (e.pointerId != touchId) return;

        Vector2 delta = e.position - originPx;
        float   dist  = delta.magnitude;

        if (dist < DeadZonePx)
        {
            Direction = Vector2.zero;
            return;
        }

        float   intensity = Mathf.Clamp01(dist / FullSpeedDistance);
        Direction         = (delta / dist) * intensity;
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (e.pointerId != touchId) return;
        touchId   = -1;
        Direction = Vector2.zero;
    }

    // ── Cleanup ───────────────────────────────────────────────────────────────

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
