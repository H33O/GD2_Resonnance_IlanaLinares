using UnityEngine;

/// <summary>
/// Draws the circular arena ring at runtime using a <see cref="LineRenderer"/>.
/// Attach to the <b>CircleArena</b> GameObject.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
[ExecuteAlways]
public class CGArenaRenderer : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Settings")]
    public CGSettings settings;

    [Header("Ring Override (optional)")]
    [Tooltip("Overrides settings.arenaRadius when > 0.")]
    public float radiusOverride = 0f;

    [Tooltip("Overrides settings.ringLineWidth when > 0.")]
    public float lineWidthOverride = 0f;

    [Header("Appearance")]
    public Color ringColor = Color.black;

    [Range(32, 128)]
    [Tooltip("Number of line segments — more = smoother.")]
    public int segments = 80;

    // ── Private ───────────────────────────────────────────────────────────────

    private LineRenderer lr;
    private float        cachedRadius;
    private float        cachedWidth;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake() => Rebuild();
    private void OnValidate() => Rebuild();

    // ── Build ─────────────────────────────────────────────────────────────────

    /// <summary>Rebuilds the line-renderer circle. Called automatically on change.</summary>
    public void Rebuild()
    {
        if (lr == null) lr = GetComponent<LineRenderer>();

        float radius = (radiusOverride > 0f) ? radiusOverride
                      : (settings != null)   ? settings.arenaRadius
                                             : 4f;

        float width  = (lineWidthOverride > 0f) ? lineWidthOverride
                      : (settings != null)       ? settings.ringLineWidth
                                                 : 0.22f;

        cachedRadius = radius;
        cachedWidth  = width;

        lr.loop               = true;
        lr.useWorldSpace      = false;
        lr.positionCount      = segments;
        lr.startWidth         = width;
        lr.endWidth           = width;
        lr.startColor         = ringColor;
        lr.endColor           = ringColor;
        lr.shadowCastingMode  = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows     = false;
        lr.sortingOrder       = 1;

        float step = 360f / segments;
        for (int i = 0; i < segments; i++)
        {
            float rad = i * step * Mathf.Deg2Rad;
            lr.SetPosition(i, new Vector3(Mathf.Cos(rad) * radius,
                                          Mathf.Sin(rad) * radius,
                                          0f));
        }
    }

    /// <summary>Returns the current ring radius used by the renderer.</summary>
    public float Radius => cachedRadius;
}
