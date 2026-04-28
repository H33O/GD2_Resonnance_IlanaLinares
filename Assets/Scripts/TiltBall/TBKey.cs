using System.Collections;
using UnityEngine;

/// <summary>
/// Clé à ramasser avant de pouvoir entrer dans le trou en niveau 2.
/// Animée (bob vertical + rotation continue).
/// </summary>
public class TBKey : MonoBehaviour
{
    // ── Constantes d'animation ────────────────────────────────────────────────

    private const float BobAmplitude  = 0.18f;
    private const float BobFrequency  = 2.2f;
    private const float RotationSpeed = 80f;

    // ── État ──────────────────────────────────────────────────────────────────

    private SpriteRenderer sr;
    private Vector3        startPosition;
    private bool           collected;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        sr            = GetComponent<SpriteRenderer>();
        startPosition = transform.position;
    }

    private void Update()
    {
        if (collected) return;

        float y = startPosition.y + Mathf.Sin(Time.time * BobFrequency) * BobAmplitude;
        transform.position = new Vector3(startPosition.x, y, startPosition.z);
        transform.Rotate(0f, 0f, RotationSpeed * Time.deltaTime);
    }

    // ── Collecte ──────────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if (other.GetComponent<TBPlayerController>() == null) return;

        collected = true;
        TBGameManager.PlayKeySfx();
        TBGameManager.Instance?.CollectKey();
        StartCoroutine(CollectAnimation());
    }

    private IEnumerator CollectAnimation()
    {
        float   t          = 0f;
        float   duration   = 0.30f;
        Vector3 startScale = transform.localScale;
        Color   startColor = sr ? sr.color : Color.white;

        while (t < duration)
        {
            t          += Time.deltaTime;
            float ratio = t / duration;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, ratio);
            if (sr) sr.color     = Color.Lerp(startColor, Color.clear, ratio);
            yield return null;
        }

        Destroy(gameObject);
    }
}
