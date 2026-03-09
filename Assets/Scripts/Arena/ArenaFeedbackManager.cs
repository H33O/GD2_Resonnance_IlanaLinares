using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Centralises all visual feedback for the Circle Arena:
/// screen shake (camera offset), white flash overlay,
/// near-miss flash, perfect-dodge particles, and slow-motion.
///
/// Attach to the "FeedbackManager" GameObject.
/// </summary>
public class ArenaFeedbackManager : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Settings")]
    public ArenaSettings settings;

    [Header("Camera Shake")]
    [Tooltip("The camera whose position is offset during a shake.")]
    public Camera arenaCamera;

    [Header("Flash Overlay")]
    [Tooltip("Full-screen Image used for white flash effects.")]
    public Image flashOverlay;

    [Header("Dodge Particles")]
    [Tooltip("ParticleSystem that bursts on a perfect dodge.")]
    public ParticleSystem dodgeParticles;

    [Header("Near-Miss Particles")]
    [Tooltip("ParticleSystem that emits a subtle burst on near-miss.")]
    public ParticleSystem nearMissParticles;

    // ── Internal state ────────────────────────────────────────────────────────

    private Vector3 cameraBasePosition;
    private Coroutine shakeCoroutine;
    private Coroutine flashCoroutine;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (arenaCamera != null)
            cameraBasePosition = arenaCamera.transform.localPosition;

        if (flashOverlay != null)
            flashOverlay.color = Color.clear;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Full collision shake + bright flash triggered at game over.</summary>
    public void TriggerCollisionShake()
    {
        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ShakeRoutine(settings.shakeMagnitude, settings.shakeDuration));
    }

    /// <summary>Bright white flash triggered on collision.</summary>
    public void TriggerCollisionFlash()
    {
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRoutine(0.80f, settings.shakeDuration));
    }

    /// <summary>Subtle white flash triggered on a near miss.</summary>
    public void TriggerNearMiss(Vector3 worldPosition)
    {
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRoutine(
            settings.nearMissFlashAlpha,
            settings.nearMissFlashDuration));

        EmitAt(nearMissParticles, worldPosition);
    }

    /// <summary>Particle burst + ripple on perfect dodge.</summary>
    public void TriggerPerfectDodge(Vector3 worldPosition)
    {
        EmitAt(dodgeParticles, worldPosition, settings.dodgeParticleCount);
    }

    // ── Shake ─────────────────────────────────────────────────────────────────

    private IEnumerator ShakeRoutine(float magnitude, float duration)
    {
        if (arenaCamera == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float decay  = 1f - Mathf.Clamp01(elapsed / duration);
            float ox     = Random.Range(-1f, 1f) * magnitude * decay;
            float oy     = Random.Range(-1f, 1f) * magnitude * decay;
            arenaCamera.transform.localPosition = cameraBasePosition + new Vector3(ox, oy, 0f);
            yield return null;
        }
        arenaCamera.transform.localPosition = cameraBasePosition;
    }

    // ── Flash ─────────────────────────────────────────────────────────────────

    private IEnumerator FlashRoutine(float peakAlpha, float duration)
    {
        if (flashOverlay == null) yield break;

        flashOverlay.color = new Color(1f, 1f, 1f, peakAlpha);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float a  = Mathf.Lerp(peakAlpha, 0f, elapsed / duration);
            flashOverlay.color = new Color(1f, 1f, 1f, a);
            yield return null;
        }
        flashOverlay.color = Color.clear;
    }

    // ── Particle helper ───────────────────────────────────────────────────────

    private void EmitAt(ParticleSystem ps, Vector3 worldPos, int count = 1)
    {
        if (ps == null) return;
        ps.transform.position = worldPos;
        var emitParams = new ParticleSystem.EmitParams();
        ps.Emit(emitParams, count);
    }
}
