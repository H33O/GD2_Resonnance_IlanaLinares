using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gère les réactions visuelles d'un bouton Canvas lorsque la balle le touche
/// ou que l'utilisateur appuie dessus : scale bounce et flash de fond.
/// </summary>
public class MenuCanvasButton : MonoBehaviour
{
    // ── Configuration ─────────────────────────────────────────────────────────

    private const float BounceScaleUp   = 1.10f;
    private const float BounceScaleDown = 0.96f;
    private const float BounceDuration  = 0.20f;
    private const float FlashDuration   = 0.22f;

    // ── État ──────────────────────────────────────────────────────────────────

    private RectTransform rt;
    private Image         bgImage;
    private bool          isAccent;
    private bool          isReacting;
    private Vector3       baseScale = Vector3.one;

    public event System.Action OnClick;

    // ── Initialisation ────────────────────────────────────────────────────────

    /// <summary>Injecte les références depuis MenuSceneSetup.</summary>
    public void Init(RectTransform rectTransform, Image image, bool accent)
    {
        rt       = rectTransform;
        bgImage  = image;
        isAccent = accent;
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>Déclenche la réaction au rebond de la balle.</summary>
    public void TriggerBounceReaction()
    {
        if (isReacting) return;
        StartCoroutine(BounceRoutine());
        StartCoroutine(FlashRoutine());
    }

    /// <summary>Déclenche la réaction à un appui utilisateur + notifie les listeners.</summary>
    public void TriggerPress()
    {
        if (!isReacting)
        {
            StartCoroutine(BounceRoutine());
            StartCoroutine(FlashRoutine());
        }
        OnClick?.Invoke();
    }

    /// <summary>Retourne le RectTransform du bouton (utilisé par MenuBall).</summary>
    public RectTransform GetRect() => rt;

    // ── Coroutines ────────────────────────────────────────────────────────────

    private IEnumerator BounceRoutine()
    {
        isReacting = true;
        float half = BounceDuration * 0.5f;

        yield return StartCoroutine(ScaleTo(BounceScaleUp,   half));
        yield return StartCoroutine(ScaleTo(BounceScaleDown, half * 0.5f));
        yield return StartCoroutine(ScaleTo(1f,              half * 0.5f));

        rt.localScale = baseScale;
        isReacting    = false;
    }

    private IEnumerator ScaleTo(float target, float duration)
    {
        float elapsed = 0f;
        float start   = rt.localScale.x;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            rt.localScale = baseScale * Mathf.Lerp(start, target, t);
            yield return null;
        }
    }

    private IEnumerator FlashRoutine()
    {
        if (bgImage == null) yield break;

        Color original = bgImage.color;
        // Flash blanc pour les boutons secondaires, flash gris foncé pour le bouton accent
        Color flash    = isAccent
            ? new Color(0.7f, 0.7f, 0.7f, 1f)
            : new Color(0.35f, 0.35f, 0.35f, 1f);

        float elapsed = 0f;
        while (elapsed < FlashDuration)
        {
            elapsed += Time.deltaTime;
            bgImage.color = Color.Lerp(flash, original, elapsed / FlashDuration);
            yield return null;
        }
        bgImage.color = original;
    }
}
