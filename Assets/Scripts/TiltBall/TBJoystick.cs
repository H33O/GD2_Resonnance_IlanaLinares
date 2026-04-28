using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Joystick virtuel affiché en bas au centre de l'écran.
/// Se crée entièrement en code, aucun prefab requis.
///
/// Utilisation :
///   Vector2 dir = TBJoystick.Instance?.Direction ?? Vector2.zero;
///
/// Direction est un Vector2 normalisé dans [-1, 1] avec zone morte.
/// Le joystick suit le premier doigt posé (multi-touch safe).
/// </summary>
public class TBJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public static TBJoystick Instance { get; private set; }

    // ── Paramètres ────────────────────────────────────────────────────────────

    private const float BaseRadius = 120f;
    private const float KnobRadius = 50f;
    private const float DeadZone   = 0.12f;

    // ── Couleurs ──────────────────────────────────────────────────────────────

    private static readonly Color ColBase = new Color(1f, 1f, 1f, 0.12f);
    private static readonly Color ColKnob = new Color(1f, 1f, 1f, 0.40f);

    // ── État ──────────────────────────────────────────────────────────────────

    /// <summary>Direction normalisée [-1,1] lue par TBPlayerController.</summary>
    public Vector2 Direction { get; private set; }

    private RectTransform visualRT;   // conteneur centré — sert de référence pour UpdateKnob
    private RectTransform knobRT;
    private int           touchId = -1;

    // ── Création statique ─────────────────────────────────────────────────────

    /// <summary>Crée le joystick centré en bas du Canvas.</summary>
    public static TBJoystick Create(RectTransform canvasRT)
    {
        // ── Zone de touch : toute la largeur, 22% bas de l'écran ──────────────
        var zoneGO = new GameObject("Joystick");
        zoneGO.transform.SetParent(canvasRT, false);

        var zoneImg           = zoneGO.AddComponent<Image>();
        zoneImg.color         = Color.clear;
        zoneImg.raycastTarget = true;

        var zRT          = zoneImg.rectTransform;
        zRT.anchorMin    = new Vector2(0f,  0f);
        zRT.anchorMax    = new Vector2(1f,  0.22f);
        zRT.offsetMin    = zRT.offsetMax = Vector2.zero;

        // ── Conteneur visuel centré dans la zone ──────────────────────────────
        var visualGO = new GameObject("JoystickVisual");
        visualGO.transform.SetParent(zoneGO.transform, false);

        var visualRT         = visualGO.AddComponent<RectTransform>();
        visualRT.anchorMin   = new Vector2(0.5f, 0.5f);
        visualRT.anchorMax   = new Vector2(0.5f, 0.5f);
        visualRT.pivot       = new Vector2(0.5f, 0.5f);
        visualRT.sizeDelta   = Vector2.zero;
        visualRT.anchoredPosition = Vector2.zero;

        // ── Base ──────────────────────────────────────────────────────────────
        var baseGO = new GameObject("Base");
        baseGO.transform.SetParent(visualGO.transform, false);

        var baseImg           = baseGO.AddComponent<Image>();
        baseImg.sprite        = SpriteGenerator.CreateCircle(128);
        baseImg.color         = ColBase;
        baseImg.raycastTarget = false;

        var bRT               = baseImg.rectTransform;
        bRT.anchorMin         = new Vector2(0.5f, 0.5f);
        bRT.anchorMax         = new Vector2(0.5f, 0.5f);
        bRT.pivot             = new Vector2(0.5f, 0.5f);
        bRT.sizeDelta         = new Vector2(BaseRadius * 2f, BaseRadius * 2f);
        bRT.anchoredPosition  = Vector2.zero;

        // ── Knob ──────────────────────────────────────────────────────────────
        var knobGO = new GameObject("Knob");
        knobGO.transform.SetParent(visualGO.transform, false);

        var knobImg           = knobGO.AddComponent<Image>();
        knobImg.sprite        = SpriteGenerator.CreateCircle(128);
        knobImg.color         = ColKnob;
        knobImg.raycastTarget = false;

        var kRT               = knobImg.rectTransform;
        kRT.anchorMin         = new Vector2(0.5f, 0.5f);
        kRT.anchorMax         = new Vector2(0.5f, 0.5f);
        kRT.pivot             = new Vector2(0.5f, 0.5f);
        kRT.sizeDelta         = new Vector2(KnobRadius * 2f, KnobRadius * 2f);
        kRT.anchoredPosition  = Vector2.zero;

        var js            = zoneGO.AddComponent<TBJoystick>();
        js.visualRT       = visualRT;   // pivot (0.5,0.5) centré → localPos (0,0) = centre
        js.knobRT         = kRT;
        Instance          = js;

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
        touchId               = -1;
        Direction             = Vector2.zero;
        knobRT.anchoredPosition = Vector2.zero;
    }

    // ── Calcul direction ──────────────────────────────────────────────────────

    private void UpdateKnob(Vector2 screenPos)
    {
        // Convertit la position écran en local du conteneur visuel.
        // visualRT a un pivot (0.5, 0.5) au centre → localPos (0,0) = centre exact du joystick.
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            visualRT, screenPos, null, out Vector2 localPos);

        float   dist  = localPos.magnitude;
        Vector2 norm  = dist > 0f ? localPos / dist : Vector2.zero;

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
