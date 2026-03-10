using System.Collections;
using UnityEngine;

/// <summary>
/// Handles camera shake on collision.
/// Attach to the <b>CameraController</b> GameObject (which holds the Main Camera).
/// </summary>
public class CGCameraController : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Settings")]
    public CGSettings settings;

    [Header("References")]
    [Tooltip("The camera to shake. Defaults to Camera.main if left empty.")]
    public Camera targetCamera;

    // ── State ─────────────────────────────────────────────────────────────────

    private Vector3 originPos;
    private bool    isShaking;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera != null) originPos = targetCamera.transform.localPosition;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Triggers a screen shake with inspector-defined strength and duration.</summary>
    public void TriggerShake()
    {
        if (!isShaking) StartCoroutine(ShakeRoutine());
    }

    /// <summary>Triggers a shake with explicit parameters (overrides settings for this call).</summary>
    public void TriggerShake(float magnitude, float duration)
    {
        StartCoroutine(ShakeRoutine(magnitude, duration));
    }

    // ── Shake routine ─────────────────────────────────────────────────────────

    private IEnumerator ShakeRoutine()
    {
        float mag = settings != null ? settings.cameraShakeStrength : 0.20f;
        float dur = settings != null ? settings.cameraShakeDuration : 0.35f;
        yield return ShakeRoutine(mag, dur);
    }

    private IEnumerator ShakeRoutine(float magnitude, float duration)
    {
        isShaking = true;
        float elapsed = 0f;
        if (targetCamera == null) { isShaking = false; yield break; }

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t  = 1f - (elapsed / duration);          // fades out linearly
            float ox = Random.Range(-1f, 1f) * magnitude * t;
            float oy = Random.Range(-1f, 1f) * magnitude * t;
            targetCamera.transform.localPosition = originPos + new Vector3(ox, oy, 0f);
            yield return null;
        }

        targetCamera.transform.localPosition = originPos;
        isShaking = false;
    }
}
