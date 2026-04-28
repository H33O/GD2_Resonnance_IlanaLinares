using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Joystick virtuel dynamique pour TiltBall.
///
/// Comportement :
///   - Quand le joueur pose le doigt n'importe où sur la zone de touch, la BASE du joystick
///     se repositionne instantanément à l'endroit du toucher. C'est ce nouveau point
///     qui devient l'origine des calculs de direction.
///   - Le joueur peut ensuite glisser dans n'importe quelle direction depuis ce point,
///     comme un joystick classique.
///   - Si le joueur lève le doigt et en repose un autre ailleurs, la base se déplace
///     de nouveau au nouvel endroit.
///
/// Direction est un Vector2 normalisé dans [-1, 1] avec zone morte.
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

    private RectTransform zoneRT;       // zone de touch (toute la largeur, 22% bas)
    private RectTransform visualRT;     // conteneur visuel — repositionné à chaque PointerDown
    private RectTransform knobRT;
    private int           touchId = -1;

    // ── Création statique ─────────────────────────────────────────────────────

    /// <summary>Crée le joystick couvrant toute la zone basse du Canvas.</summary>
    public static TBJoystick Create(RectTransform canvasRT)
    {
        // ── Zone de touch : toute la largeur, 22% bas de l'écran ──────────────
        var zoneGO = new GameObject("Joystick");
        zoneGO.transform.SetParent(canvasRT, false);

        var zoneImg           = zoneGO.AddComponent<Image>();
        zoneImg.color         = Color.clear;
        zoneImg.raycastTarget = true;

        var zRT       = zoneImg.rectTransform;
        zRT.anchorMin = new Vector2(0f,  0f);
        zRT.anchorMax = new Vector2(1f,  0.22f);
        zRT.offsetMin = zRT.offsetMax = Vector2.zero;

        // ── Conteneur visuel : ancré en bas-centre par défaut ─────────────────
        var visualGO         = new GameObject("JoystickVisual");
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

        var bRT              = baseImg.rectTransform;
        bRT.anchorMin        = new Vector2(0.5f, 0.5f);
        bRT.anchorMax        = new Vector2(0.5f, 0.5f);
        bRT.pivot            = new Vector2(0.5f, 0.5f);
        bRT.sizeDelta        = new Vector2(BaseRadius * 2f, BaseRadius * 2f);
        bRT.anchoredPosition = Vector2.zero;

        // ── Knob ──────────────────────────────────────────────────────────────
        var knobGO = new GameObject("Knob");
        knobGO.transform.SetParent(visualGO.transform, false);

        var knobImg           = knobGO.AddComponent<Image>();
        knobImg.sprite        = SpriteGenerator.CreateCircle(128);
        knobImg.color         = ColKnob;
        knobImg.raycastTarget = false;

        var kRT              = knobImg.rectTransform;
        kRT.anchorMin        = new Vector2(0.5f, 0.5f);
        kRT.anchorMax        = new Vector2(0.5f, 0.5f);
        kRT.pivot            = new Vector2(0.5f, 0.5f);
        kRT.sizeDelta        = new Vector2(KnobRadius * 2f, KnobRadius * 2f);
        kRT.anchoredPosition = Vector2.zero;

        var js         = zoneGO.AddComponent<TBJoystick>();
        js.zoneRT      = zRT;
        js.visualRT    = visualRT;
        js.knobRT      = kRT;
        Instance       = js;

        return js;
    }

    // ── Touch / Pointer events ────────────────────────────────────────────────

    public void OnPointerDown(PointerEventData e)
    {
        if (touchId != -1) return;
        touchId = e.pointerId;

        // Reposition de la base au point de contact
        RepositionBase(e.position);
        // Knob centré sur la base (pas encore de drag)
        knobRT.anchoredPosition = Vector2.zero;
        Direction               = Vector2.zero;
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

    // ── Repositionnement de la base ───────────────────────────────────────────

    /// <summary>
    /// Déplace le conteneur visuel pour que la base soit centrée sur <paramref name="screenPos"/>.
    /// Utilise le parent (zone) comme référence de coordonnées locales.
    /// </summary>
    private void RepositionBase(Vector2 screenPos)
    {
        // Convertit la position écran en local de la zone de touch
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            zoneRT, screenPos, null, out Vector2 localInZone);

        // Convertit en anchoredPosition dans la zone
        // La zone a des anchors étirés → son pivot est en bas-gauche par défaut.
        // On positionne le visual en coordonnées locales de la zone directement.
        visualRT.localPosition = localInZone;
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
