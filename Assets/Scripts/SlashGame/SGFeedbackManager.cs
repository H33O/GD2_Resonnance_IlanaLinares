using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Centralises all visual feedback for the Slash Game:
/// screen flash, camera shake, parry ripple, hit-stop, guardian shield flash.
/// Particle systems are intentionally removed — all feedback is geometry-based.
/// Attach to the "FeedbackManager" GameObject.
/// </summary>
public class SGFeedbackManager : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Settings")]
    public SGSettings settings;

    [Header("Camera")]
    public Camera gameCamera;

    [Header("Flash Overlay")]
    public Image flashOverlay;

    // ── Internal state ────────────────────────────────────────────────────────

    private Vector3    cameraBasePos;
    private Coroutine  shakeCoroutine;
    private Coroutine  flashCoroutine;
    private Coroutine  hitStopCoroutine;

    // Ripple pool — geometry-based rings, no particles
    private readonly Queue<LineRenderer> ripplePool   = new();
    private readonly List<(LineRenderer lr, float spawnTime, Color color)> activeRipples = new();
    private const int   RipplePoolSize = 8;
    private const int   RippleSegments = 32;
    private static Material rippleMat;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (gameCamera != null)
            cameraBasePos = gameCamera.transform.localPosition;

        if (flashOverlay != null)
            flashOverlay.color = Color.clear;

        BuildRipplePool();
    }

    private void OnEnable()
    {
        SGGameManager.OnConeFilled        += HandleConeFilled;
        SGGameManager.OnFuryStarted       += HandleFuryStarted;
        SGGameManager.OnCharacterUnlocked += HandleCharacterUnlocked;
    }

    private void OnDisable()
    {
        SGGameManager.OnConeFilled        -= HandleConeFilled;
        SGGameManager.OnFuryStarted       -= HandleFuryStarted;
        SGGameManager.OnCharacterUnlocked -= HandleCharacterUnlocked;
    }

    private void Update() => TickRipples();

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Full parry feedback: flash, shake, hit-stop, ripple ring.</summary>
    public void TriggerParry(Vector3 worldPos)
    {
        TriggerFlash(settings?.parryFlashAlpha ?? 0.5f);
        TriggerShake(settings?.parryShakeMagnitude ?? 0.08f);
        TriggerHitStop();
        SpawnRipple(worldPos, Color.white);
    }

    /// <summary>Hit feedback on game over.</summary>
    public void TriggerHit()
    {
        TriggerFlash(settings?.hitFlashAlpha ?? 0.85f);
        TriggerShake(settings?.hitShakeMagnitude ?? 0.25f);
    }

    /// <summary>Guardian shield auto-parry: colored ripple.</summary>
    public void TriggerGuardianShield(Vector3 worldPos)
    {
        SpawnRipple(worldPos, SGCharacterDefs.Colors[(int)SGCharacterType.Guardian]);
    }

    // ── Flash ─────────────────────────────────────────────────────────────────

    private void TriggerFlash(float peakAlpha)
    {
        if (flashOverlay == null) return;
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRoutine(peakAlpha));
    }

    private IEnumerator FlashRoutine(float peak)
    {
        float duration = settings?.flashDuration ?? 0.15f;
        flashOverlay.color = new Color(1f, 1f, 1f, peak);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float a  = Mathf.Lerp(peak, 0f, elapsed / duration);
            flashOverlay.color = new Color(1f, 1f, 1f, a);
            yield return null;
        }
        flashOverlay.color = Color.clear;
    }

    // ── Shake ─────────────────────────────────────────────────────────────────

    private void TriggerShake(float magnitude)
    {
        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ShakeRoutine(magnitude));
    }

    private IEnumerator ShakeRoutine(float magnitude)
    {
        if (gameCamera == null) yield break;
        float duration = settings?.shakeDuration ?? 0.20f;
        float elapsed  = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float decay = 1f - Mathf.Clamp01(elapsed / duration);
            float ox    = Random.Range(-1f, 1f) * magnitude * decay;
            float oy    = Random.Range(-1f, 1f) * magnitude * decay;
            gameCamera.transform.localPosition = cameraBasePos + new Vector3(ox, oy, 0f);
            yield return null;
        }
        gameCamera.transform.localPosition = cameraBasePos;
    }

    // ── Hit-stop ──────────────────────────────────────────────────────────────

    private void TriggerHitStop()
    {
        if (hitStopCoroutine != null) StopCoroutine(hitStopCoroutine);
        hitStopCoroutine = StartCoroutine(HitStopRoutine());
    }

    private IEnumerator HitStopRoutine()
    {
        float scale    = settings?.hitStopTimeScale ?? 0.05f;
        float duration = settings?.hitStopDuration  ?? 0.07f;
        Time.timeScale = scale;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }

    // ── Ripple ring pool ──────────────────────────────────────────────────────

    private void BuildRipplePool()
    {
        if (rippleMat == null)
            rippleMat = new Material(
                Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));

        for (int i = 0; i < RipplePoolSize; i++)
        {
            var go = new GameObject($"SGRipple_{i}");
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount     = RippleSegments;
            lr.loop              = true;
            lr.useWorldSpace     = true;
            lr.startWidth        = 0.05f;
            lr.endWidth          = 0.05f;
            lr.startColor        = Color.clear;
            lr.endColor          = Color.clear;
            lr.sortingOrder      = 20;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows    = false;
            lr.sharedMaterial    = rippleMat;
            go.SetActive(false);
            ripplePool.Enqueue(lr);
        }
    }

    private void SpawnRipple(Vector3 origin, Color color)
    {
        if (ripplePool.Count == 0) return;
        var lr = ripplePool.Dequeue();
        lr.gameObject.SetActive(true);
        lr.transform.position = origin;
        SetRipple(lr, 0f, origin, 0f, color);
        activeRipples.Add((lr, Time.time, color));
    }

    private void TickRipples()
    {
        float maxR = settings?.parryRadius ?? 1.2f;
        const float dur = 0.40f;
        float now  = Time.time;

        for (int i = activeRipples.Count - 1; i >= 0; i--)
        {
            var (lr, spawnTime, color) = activeRipples[i];
            if (lr == null) { activeRipples.RemoveAt(i); continue; }

            float t = (now - spawnTime) / dur;
            if (t >= 1f)
            {
                lr.startColor = Color.clear;
                lr.endColor   = Color.clear;
                lr.gameObject.SetActive(false);
                ripplePool.Enqueue(lr);
                activeRipples.RemoveAt(i);
                continue;
            }

            SetRipple(lr, Mathf.Lerp(0f, maxR, t), lr.transform.position,
                      Mathf.Lerp(0.9f, 0f, t), color);
        }
    }

    private static void SetRipple(LineRenderer lr, float r, Vector3 origin, float alpha, Color color)
    {
        Color c = color; c.a = alpha;
        lr.startColor = c;
        lr.endColor   = c;

        float step = 360f / RippleSegments;
        for (int i = 0; i < RippleSegments; i++)
        {
            float rad = i * step * Mathf.Deg2Rad;
            lr.SetPosition(i, origin + new Vector3(Mathf.Cos(rad) * r, Mathf.Sin(rad) * r, 0f));
        }
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void HandleConeFilled()
    {
        TriggerFlash(0.35f);
        // Burst of 3 expanding rings at center
        for (int k = 0; k < 3; k++)
        {
            int capturedK = k;
            StartCoroutine(DelayedRipple(capturedK * 0.08f));
        }
    }

    private IEnumerator DelayedRipple(float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnRipple(Vector3.zero, Color.white);
    }

    private void HandleFuryStarted()
    {
        TriggerFlash(0.30f);
        TriggerShake(0.12f);
    }

    private void HandleCharacterUnlocked(int index)
    {
        // Colored ripple burst matching the newly unlocked character
        Color c = index < SGCharacterDefs.Colors.Length
            ? SGCharacterDefs.Colors[index] : Color.white;
        SpawnRipple(Vector3.zero, c);
        TriggerFlash(0.20f);
    }
}
