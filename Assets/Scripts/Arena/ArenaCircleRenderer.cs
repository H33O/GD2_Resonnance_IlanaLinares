using UnityEngine;

/// <summary>
/// Draws the circular arena wall at runtime using a LineRenderer.
/// Adjust Radius, Segments, and LineWidth in the Inspector.
/// The GameObject can be placed in the scene and is immediately visible in the Scene view.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class ArenaCircleRenderer : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Geometry")]
    [Tooltip("Radius of the circle (world units). Should match ArenaSettings.orbitRadius.")]
    public float radius = 4.0f;

    [Tooltip("Number of line segments used to approximate the circle. Higher = smoother.")]
    [Range(16, 128)]
    public int segments = 64;

    [Header("Appearance")]
    [Tooltip("Width of the circle line (world units).")]
    public float lineWidth = 0.22f;

    [Tooltip("Color of the arena ring.")]
    public Color ringColor = Color.white;

    // ── Internal ──────────────────────────────────────────────────────────────

    private LineRenderer lineRenderer;
    private float lastRadius;
    private int   lastSegments;
    private float lastWidth;
    private Color lastColor;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        ApplySettings();
    }

    private void OnValidate()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();
        ApplySettings();
    }

    private void Update()
    {
        // Rebuild only when parameters change (editor or runtime tweaks).
        if (radius != lastRadius || segments != lastSegments
            || lineWidth != lastWidth || ringColor != lastColor)
            ApplySettings();
    }

    // ── Geometry ──────────────────────────────────────────────────────────────

    /// <summary>Rebuilds the LineRenderer positions and visual settings.</summary>
    public void ApplySettings()
    {
        if (lineRenderer == null) return;

        lineRenderer.loop           = true;
        lineRenderer.useWorldSpace  = false;
        lineRenderer.positionCount  = segments;
        lineRenderer.startWidth     = lineWidth;
        lineRenderer.endWidth       = lineWidth;
        lineRenderer.startColor     = ringColor;
        lineRenderer.endColor       = ringColor;

        if (lineRenderer.sharedMaterial == null)
            lineRenderer.sharedMaterial = new Material(
                Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));

        for (int i = 0; i < segments; i++)
        {
            float ang = (float)i / segments * Mathf.PI * 2f;
            lineRenderer.SetPosition(i, new Vector3(Mathf.Cos(ang) * radius, Mathf.Sin(ang) * radius, 0f));
        }

        lastRadius   = radius;
        lastSegments = segments;
        lastWidth    = lineWidth;
        lastColor    = ringColor;
    }
}
