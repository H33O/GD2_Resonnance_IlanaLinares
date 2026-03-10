using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns animated ring-shaped ripple sprites at a given world position.
/// Uses a simple pool of LineRenderer rings to avoid allocations.
///
/// Attach to the "RippleEffect" child GameObject under the Ball or FX root.
/// Wire the ArenaSettings asset to control radius and duration.
/// </summary>
public class ArenaRippleEffect : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Settings")]
    public ArenaSettings settings;

    [Header("Appearance")]
    [Tooltip("Number of segments used to draw each ripple ring.")]
    [Range(12, 64)]
    public int ringSegments = 32;

    [Tooltip("Width of the ripple ring line.")]
    public float lineWidth = 0.04f;

    [Tooltip("Color of the ripple (alpha is animated).")]
    public Color rippleColor = Color.white;

    [Header("Pool")]
    [Tooltip("Number of simultaneous ripples supported.")]
    [Range(2, 12)]
    public int poolSize = 6;

    // ── Pool ──────────────────────────────────────────────────────────────────

    private readonly Queue<LineRenderer> pool = new();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        BuildPool();
    }

    // ── Pool setup ────────────────────────────────────────────────────────────

    private void BuildPool()
    {
        // One shared material for all ripple rings — avoids creating N GPU objects
        var sharedMat = new Material(
            Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));

        for (int i = 0; i < poolSize; i++)
        {
            var go = new GameObject($"Ripple_{i}");
            go.transform.SetParent(transform, false);
            go.SetActive(false);

            var lr              = go.AddComponent<LineRenderer>();
            lr.useWorldSpace    = true;
            lr.loop             = true;
            lr.positionCount    = ringSegments;
            lr.startWidth       = lineWidth;
            lr.endWidth         = lineWidth;
            lr.sharedMaterial   = sharedMat;
            lr.startColor       = Color.clear;
            lr.endColor         = Color.clear;
            lr.sortingOrder     = 5;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows    = false;

            pool.Enqueue(lr);
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Spawns a ripple ring expanding outward from the given world position.</summary>
    public void SpawnAt(Vector3 worldPosition)
    {
        if (settings == null || pool.Count == 0) return;
        StartCoroutine(AnimateRipple(pool.Dequeue(), worldPosition));
    }

    // ── Animation ─────────────────────────────────────────────────────────────

    private IEnumerator AnimateRipple(LineRenderer lr, Vector3 centre)
    {
        lr.gameObject.SetActive(true);

        float elapsed  = 0f;
        float duration = settings.rippleDuration;
        float maxR     = settings.rippleMaxRadius;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t  = elapsed / duration;
            float r  = Mathf.Lerp(0f, maxR, t);
            float a  = Mathf.Lerp(rippleColor.a, 0f, t * t);

            Color c = new Color(rippleColor.r, rippleColor.g, rippleColor.b, a);
            lr.startColor = c;
            lr.endColor   = c;

            // Rebuild ring positions
            for (int i = 0; i < ringSegments; i++)
            {
                float ang = (float)i / ringSegments * Mathf.PI * 2f;
                lr.SetPosition(i, centre + new Vector3(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r, 0f));
            }

            yield return null;
        }

        lr.startColor = Color.clear;
        lr.endColor   = Color.clear;
        lr.gameObject.SetActive(false);
        pool.Enqueue(lr);
    }
}
