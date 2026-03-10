using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Central hub for all visual feedback effects:
/// — screen flash on near-miss
/// — ripple ring on tap
/// — particle burst on collect
/// — particle burst on perfect dodge
/// Attach to the <b>ParticleSystems</b> GameObject.
/// </summary>
public class CGFeedbackManager : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Settings")]
    public CGSettings settings;

    [Header("Near-Miss Flash")]
    [Tooltip("Full-screen UI Image used for the white flash overlay.")]
    public Image flashOverlay;

    [Header("Collect Particles")]
    [Tooltip("ParticleSystem fired when a collectible is picked up.")]
    public ParticleSystem collectParticles;

    [Header("Dodge Particles")]
    [Tooltip("ParticleSystem fired on a near-miss (perfect dodge).")]
    public ParticleSystem nearMissParticles;

    [Header("Ripple")]
    [Tooltip("Number of ripple rings kept in the pool.")]
    [Range(1, 12)]
    public int ripplePoolSize = 6;

    // ── Ripple pool ───────────────────────────────────────────────────────────

    private readonly Queue<LineRenderer> ripplePool = new();
    private readonly List<LineRenderer>  activeRipples = new();

    private const int RippleSegments = 32;

    // ── Flash coroutine handle ────────────────────────────────────────────────

    private Coroutine flashCoroutine;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        BuildRipplePool();
    }

    private void Update()
    {
        TickRipples();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Flashes the screen briefly white — near-miss feedback.</summary>
    public void TriggerNearMissFlash()
    {
        if (flashOverlay == null) return;
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    /// <summary>Spawns a ripple ring expanding from <paramref name="worldPos"/>.</summary>
    public void TriggerRipple(Vector3 worldPos)
    {
        if (ripplePool.Count == 0) return;
        LineRenderer lr = ripplePool.Dequeue();
        lr.gameObject.SetActive(true);
        lr.transform.position = worldPos;
        SetRippleRadius(lr, 0f);
        activeRipples.Add(lr);
    }

    /// <summary>Emits a particle burst at <paramref name="worldPos"/> for collectible pickup.</summary>
    public void TriggerCollectParticles(Vector3 worldPos)
    {
        if (collectParticles == null || settings == null) return;
        collectParticles.transform.position = worldPos;
        int count = Mathf.RoundToInt(settings.collectParticleCount * settings.particleIntensity);
        collectParticles.Emit(Mathf.Max(1, count));
    }

    /// <summary>Emits a small particle burst at <paramref name="worldPos"/> for a near-miss dodge.</summary>
    public void TriggerNearMissParticles(Vector3 worldPos)
    {
        if (nearMissParticles == null || settings == null) return;
        nearMissParticles.transform.position = worldPos;
        int count = Mathf.RoundToInt(settings.dodgeParticleCount * settings.particleIntensity);
        nearMissParticles.Emit(Mathf.Max(1, count));
    }

    // ── Flash ─────────────────────────────────────────────────────────────────

    private IEnumerator FlashRoutine()
    {
        float peak     = settings != null ? settings.nearMissFlashAlpha    : 0.30f;
        float duration = settings != null ? settings.nearMissFlashDuration : 0.12f;

        // Fade in
        float half = duration * 0.5f;
        float t    = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            flashOverlay.color = new Color(1f, 1f, 1f, Mathf.Lerp(0f, peak, t / half));
            yield return null;
        }
        // Fade out
        t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            flashOverlay.color = new Color(1f, 1f, 1f, Mathf.Lerp(peak, 0f, t / half));
            yield return null;
        }
        flashOverlay.color = Color.clear;
    }

    // ── Ripple pool ───────────────────────────────────────────────────────────

    private void BuildRipplePool()
    {
        for (int i = 0; i < ripplePoolSize; i++)
        {
            var go = new GameObject($"Ripple_{i}");
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.loop                = true;
            lr.useWorldSpace       = true;
            lr.positionCount       = RippleSegments;
            lr.startWidth          = 0.05f;
            lr.endWidth            = 0.05f;
            lr.startColor          = new Color(0f, 0f, 0f, 0.7f);
            lr.endColor            = new Color(0f, 0f, 0f, 0.7f);
            lr.shadowCastingMode   = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows      = false;
            lr.sortingOrder        = 20;
            go.SetActive(false);
            ripplePool.Enqueue(lr);
        }
    }

    private void TickRipples()
    {
        if (settings == null) return;
        float maxR = settings.rippleMaxRadius;
        float dur  = settings.rippleDuration;

        for (int i = activeRipples.Count - 1; i >= 0; i--)
        {
            var lr = activeRipples[i];
            if (lr == null) { activeRipples.RemoveAt(i); continue; }

            // Accumulate per-ripple lifetime via a helper component
            var tick = lr.GetComponent<CGRippleTick>();
            if (tick == null) tick = lr.gameObject.AddComponent<CGRippleTick>();

            tick.elapsed += Time.deltaTime;
            float t       = tick.elapsed / dur;

            if (t >= 1f)
            {
                tick.elapsed = 0f;
                lr.gameObject.SetActive(false);
                activeRipples.RemoveAt(i);
                ripplePool.Enqueue(lr);
                continue;
            }

            float r    = Mathf.Lerp(0f, maxR, t);
            float a    = Mathf.Lerp(0.7f, 0f, t);
            lr.startColor = new Color(0f, 0f, 0f, a);
            lr.endColor   = new Color(0f, 0f, 0f, a);
            SetRippleRadius(lr, r);
        }
    }

    private static void SetRippleRadius(LineRenderer lr, float r)
    {
        float step = 360f / RippleSegments;
        Vector3 orig = lr.transform.position;
        for (int i = 0; i < RippleSegments; i++)
        {
            float rad = i * step * Mathf.Deg2Rad;
            lr.SetPosition(i, orig + new Vector3(Mathf.Cos(rad) * r,
                                                  Mathf.Sin(rad) * r,
                                                  0f));
        }
    }
}

/// <summary>Tiny helper that tracks elapsed time per ripple LineRenderer.</summary>
public class CGRippleTick : MonoBehaviour
{
    public float elapsed;
}
