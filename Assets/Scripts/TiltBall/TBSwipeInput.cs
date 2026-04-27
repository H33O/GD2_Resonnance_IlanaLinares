using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Détecte les swipes directionnels sur l'écran entier (4 axes : haut, bas, gauche, droite).
///
/// Utilisation :
///   Vector2 dir = TBSwipeInput.Instance?.ConsumeDirection() ?? Vector2.zero;
///
/// - Un swipe dépose une direction dans le buffer (Vector2Int).
/// - ConsumeDirection() retourne la direction en attente puis la vide.
/// - Compatible multi-touch : seul le premier doigt est suivi.
/// </summary>
public class TBSwipeInput : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public static TBSwipeInput Instance { get; private set; }

    // ── Paramètres ────────────────────────────────────────────────────────────

    /// <summary>Distance minimale en pixels pour valider un swipe.</summary>
    private const float SwipeThresholdPx = 40f;

    // ── État ──────────────────────────────────────────────────────────────────

    private int     touchId    = -1;
    private Vector2 originPx;
    private bool    swiped;

    /// <summary>Direction discrète en attente de consommation (zéro si aucune).</summary>
    public Vector2 PendingDirection { get; private set; }

    /// <summary>Retourne la direction en attente et la vide.</summary>
    public Vector2 ConsumeDirection()
    {
        var dir           = PendingDirection;
        PendingDirection  = Vector2.zero;
        return dir;
    }

    // ── Création statique ─────────────────────────────────────────────────────

    /// <summary>Crée la zone de swipe plein écran sur le Canvas fourni.</summary>
    public static TBSwipeInput Create(RectTransform canvasRT)
    {
        var go  = new GameObject("SwipeInput");
        go.transform.SetParent(canvasRT, false);

        var img           = go.AddComponent<Image>();
        img.color         = Color.clear;
        img.raycastTarget = true;

        var rt        = img.rectTransform;
        rt.anchorMin  = Vector2.zero;
        rt.anchorMax  = Vector2.one;
        rt.offsetMin  = rt.offsetMax = Vector2.zero;

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
        swiped   = false;
    }

    public void OnDrag(PointerEventData e)
    {
        if (e.pointerId != touchId || swiped) return;

        Vector2 delta = e.position - originPx;
        if (delta.magnitude < SwipeThresholdPx) return;

        // Axe dominant → direction 4-axes
        PendingDirection = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
            ? (delta.x > 0f ? Vector2.right : Vector2.left)
            : (delta.y > 0f ? Vector2.up    : Vector2.down);

        swiped = true;
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (e.pointerId != touchId) return;
        touchId = -1;
        swiped  = false;
    }

    // ── Cleanup ───────────────────────────────────────────────────────────────

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
