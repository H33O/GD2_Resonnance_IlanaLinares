using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Renders the energy cone gauge at the bottom of the screen.
/// The fill uses a procedural mesh that grows from the tip upward.
/// Attach to the "ConeGauge" Canvas GameObject.
/// </summary>
public class SGConeGauge : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Settings")]
    public SGSettings settings;

    [Header("Cone geometry (in UI pixels)")]
    [Tooltip("Height of the cone in pixels.")]
    public float coneHeight = 180f;

    [Tooltip("Half-width of the cone base in pixels.")]
    public float coneHalfWidth = 70f;

    [Header("References")]
    [Tooltip("Image used as the cone background outline (cone shape, hollow).")]
    public Image coneOutline;

    [Tooltip("Image used as the fill layer — must use a filled Image type.")]
    public Image coneFill;

    [Tooltip("Image used for the glow overlay on the fill.")]
    public Image coneGlow;

    // ── Internal ──────────────────────────────────────────────────────────────

    private float   displayFill;   // smoothed fill 0-1
    private float   targetFill;    // raw target 0-1
    private bool    isPulsing;
    private Coroutine pulseCoroutine;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        SGGameManager.OnEnergyChanged += HandleEnergyChanged;
        SGGameManager.OnConeFilled    += HandleConeFilled;
    }

    private void OnDisable()
    {
        SGGameManager.OnEnergyChanged -= HandleEnergyChanged;
        SGGameManager.OnConeFilled    -= HandleConeFilled;
    }

    private void Start()
    {
        SetFill(0f);
        if (coneGlow != null) coneGlow.color = Color.clear;
    }

    private void Update()
    {
        // Smooth the display fill toward target
        float speed = settings?.fillSmoothSpeed ?? 4f;
        displayFill = Mathf.MoveTowards(displayFill, targetFill, speed * Time.deltaTime);
        ApplyFill(displayFill);

        // Subtle glow pulse that intensifies as cone fills
        if (!isPulsing && coneGlow != null)
        {
            float pulse   = 0.04f + 0.06f * displayFill + 0.04f * Mathf.Sin(Time.time * 4f) * displayFill;
            Color c       = coneGlow.color;
            c.a           = pulse;
            coneGlow.color = c;
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Directly set the fill level (0-1) — used for tutorial.</summary>
    public void SetFill(float normalized)
    {
        targetFill = Mathf.Clamp01(normalized);
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void ApplyFill(float t)
    {
        if (coneFill == null) return;

        // fillAmount drives a vertical fill from bottom
        coneFill.fillAmount = t;

        // Tint fill bright at full
        float brightness   = 0.7f + 0.3f * t;
        coneFill.color     = new Color(brightness, brightness, brightness, 1f);
    }

    private void HandleEnergyChanged(float normalized)
    {
        targetFill = normalized;
    }

    private void HandleConeFilled()
    {
        if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
        pulseCoroutine = StartCoroutine(FullPulseRoutine());
    }

    private IEnumerator FullPulseRoutine()
    {
        isPulsing = true;

        float elapsed  = 0f;
        float duration = 0.50f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t  = elapsed / duration;

            // Sharp bright pulse then fade
            float a = t < 0.15f
                ? Mathf.Lerp(0f, 0.9f, t / 0.15f)
                : Mathf.Lerp(0.9f, 0f, (t - 0.15f) / 0.85f);

            if (coneGlow != null)
                coneGlow.color = new Color(1f, 1f, 1f, a);

            // Scale pulse on outline
            float scale = 1f + 0.08f * Mathf.Sin(t * Mathf.PI);
            if (coneOutline != null)
                coneOutline.transform.localScale = Vector3.one * scale;

            yield return null;
        }

        if (coneOutline != null)
            coneOutline.transform.localScale = Vector3.one;

        isPulsing = false;
    }
}
