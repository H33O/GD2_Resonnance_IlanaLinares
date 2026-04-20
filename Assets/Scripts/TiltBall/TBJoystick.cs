using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Joystick virtuel affiché en bas à droite de l'écran.
/// Se crée entièrement en code, aucun prefab requis.
///
/// Utilisation :
///   Vector2 dir = TBJoystick.Instance?.Direction ?? Vector2.zero;
///
/// Direction est un Vector2 dans [-1, 1] avec zone morte.
/// Le joystick suit le premier doigt posé (multi-touch safe).
/// </summary>
public class TBJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public static TBJoystick Instance { get; private set; }

    // ── Paramètres ────────────────────────────────────────────────────────────

    private const float BaseRadius = 120f;   // rayon de la base (px référence 1080)
    private const float KnobRadius = 50f;    // rayon du bouton mobile
    private const float DeadZone   = 0.10f;  // zone morte normalisée

    // ── Couleurs ──────────────────────────────────────────────────────────────

    private static readonly Color ColBase = new Color(1f, 1f, 1f, 0.10f);
    private static readonly Color ColKnob = new Color(1f, 1f, 1f, 0.38f);

    // ── État ──────────────────────────────────────────────────────────────────

    /// <summary>Direction normalisée [-1,1] lue par TBPlayerController.</summary>
    public Vector2 Direction { get; private set; }

    private RectTransform baseRT;
    private RectTransform knobRT;
    private int           touchId = -1;

    // ── Création statique ─────────────────────────────────────────────────────

    /// <summary>Crée le joystick sur le Canvas fourni (moitié droite du bas).</summary>
    public static TBJoystick Create(RectTransform canvasRT)
    {
        // Zone de touch : moitié droite, 25% bas de l'écran
        var zoneGO = new GameObject("Joystick");
        zoneGO.transform.SetParent(canvasRT, false);

        var zone              = zoneGO.AddComponent<Image>();
        zone.color            = Color.clear;
        zone.raycastTarget    = true;

        var zoneRT            = zone.rectTransform;
        zoneRT.anchorMin      = new Vector2(0.50f, 0f);
        zoneRT.anchorMax      = new Vector2(1.00f, 0.28f);
        zoneRT.offsetMin      = zoneRT.offsetMax = Vector2.zero;

        // Base (cercle semi-transparent) — ancrée au centre de la zone
        var baseGO = new GameObject("Base");
        baseGO.transform.SetParent(zoneGO.transform, false);

        var baseImg              = baseGO.AddComponent<Image>();
        baseImg.sprite           = SpriteGenerator.CreateCircle(128);
        baseImg.color            = ColBase;
        baseImg.raycastTarget    = false;

        var bRT                  = baseImg.rectTransform;
        bRT.anchorMin            = new Vector2(0.5f, 0.5f);
        bRT.anchorMax            = new Vector2(0.5f, 0.5f);
        bRT.sizeDelta            = new Vector2(BaseRadius * 2f, BaseRadius * 2f);
        bRT.anchoredPosition     = Vector2.zero;

        // Knob
        var knobGO = new GameObject("Knob");
        knobGO.transform.SetParent(zoneGO.transform, false);

        var knobImg              = knobGO.AddComponent<Image>();
        knobImg.sprite           = SpriteGenerator.CreateCircle(128);
        knobImg.color            = ColKnob;
        knobImg.raycastTarget    = false;

        var kRT                  = knobImg.rectTransform;
        kRT.anchorMin            = new Vector2(0.5f, 0.5f);
        kRT.anchorMax            = new Vector2(0.5f, 0.5f);
        kRT.sizeDelta            = new Vector2(KnobRadius * 2f, KnobRadius * 2f);
        kRT.anchoredPosition     = Vector2.zero;

        var js      = zoneGO.AddComponent<TBJoystick>();
        js.baseRT   = bRT;
        js.knobRT   = kRT;
        Instance    = js;

        return js;
    }

    // ── Touch / Pointer events ────────────────────────────────────────────────

    public void OnPointerDown(PointerEventData e)
    {
        if (touchId != -1) return;
        touchId = e.pointerId;
        UpdateKnob(e.position);
    }

    public void OnDrag(PointerEventData e)
    {
        if (e.pointerId != touchId) return;
        UpdateKnob(e.position);
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (e.pointerId != touchId) return;
        touchId                 = -1;
        Direction               = Vector2.zero;
        knobRT.anchoredPosition = Vector2.zero;
    }

    // ── Calcul direction ──────────────────────────────────────────────────────

    private void UpdateKnob(Vector2 screenPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            baseRT.parent as RectTransform, screenPos,
            null, out Vector2 localPos);

        Vector2 offset  = localPos - (Vector2)baseRT.localPosition;
        float   dist    = offset.magnitude;
        Vector2 norm    = dist > 0f ? offset / dist : Vector2.zero;

        float clamped           = Mathf.Min(dist, BaseRadius);
        knobRT.anchoredPosition = norm * clamped;

        float ratio = clamped / BaseRadius;
        Direction   = ratio > DeadZone ? norm * ratio : Vector2.zero;
    }

    // ── Cleanup ───────────────────────────────────────────────────────────────

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
