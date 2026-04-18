using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Gère tous les effets de feedback visuels du jeu Tetris×Pac-Man :
/// particules de pose/destruction, score flottant et flash d'écran.
/// </summary>
public class TPMFeedbackManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static TPMFeedbackManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("References")]
    public Canvas worldCanvas;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>Effet de pose de bloc : cercle blanc qui s'évapore.</summary>
    public void PlayPlaceEffect(Vector3 worldPos)
    {
        StartCoroutine(PlaceRipple(worldPos));
    }

    /// <summary>Effet de destruction de bloc : éclats colorés.</summary>
    public void PlayDestroyEffect(Vector3 worldPos)
    {
        StartCoroutine(DestroyBurst(worldPos));
    }

    /// <summary>
    /// Affiche un score flottant au-dessus du monde.
    /// Si chain=true, applique une couleur dorée pour souligner la combo.
    /// </summary>
    public void ShowFloatingScore(int points, Vector3 worldPos, bool chain)
    {
        if (worldCanvas == null) return;
        StartCoroutine(FloatingScore(points, worldPos, chain));
    }

    // ── Effets ────────────────────────────────────────────────────────────────

    private IEnumerator PlaceRipple(Vector3 center)
    {
        var go   = new GameObject("PlaceRipple");
        go.transform.position = center;
        go.transform.localScale = Vector3.one * 0.1f;

        var lr              = go.AddComponent<LineRenderer>();
        lr.positionCount    = 32;
        lr.loop             = true;
        lr.useWorldSpace    = true;
        lr.startWidth       = 0.04f;
        lr.endWidth         = 0.04f;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows   = false;
        lr.sortingOrder     = 20;

        var mat = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
        lr.sharedMaterial = mat;

        float duration = 0.35f;
        float elapsed  = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t  = elapsed / duration;
            float r  = Mathf.Lerp(0.05f, 0.55f, t);
            float a  = Mathf.Lerp(0.9f, 0f, t);
            Color c  = new Color(0.2f, 1f, 0.4f, a);
            lr.startColor = c;
            lr.endColor   = c;

            for (int i = 0; i < 32; i++)
            {
                float angle = i / 32f * Mathf.PI * 2f;
                lr.SetPosition(i, center + new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0f));
            }
            yield return null;
        }

        Destroy(go);
    }

    private IEnumerator DestroyBurst(Vector3 center)
    {
        const int   Count    = 8;
        const float Duration = 0.40f;

        var particles = new GameObject[Count];
        var srs       = new SpriteRenderer[Count];

        for (int i = 0; i < Count; i++)
        {
            var go   = new GameObject($"Burst_{i}");
            go.transform.position = center;
            go.transform.localScale = Vector3.one * 0.12f;

            var sr   = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteGenerator.CreateCircle(32);
            sr.color  = new Color(1f, 0.7f, 0.1f, 1f);
            sr.sortingOrder = 25;

            particles[i] = go;
            srs[i]       = sr;
        }

        float elapsed = 0f;
        var   dirs    = new Vector2[Count];
        for (int i = 0; i < Count; i++)
        {
            float a = i / (float)Count * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
            dirs[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(0.8f, 1.4f);
        }

        while (elapsed < Duration)
        {
            elapsed += Time.deltaTime;
            float t  = elapsed / Duration;
            float a  = 1f - t;

            for (int i = 0; i < Count; i++)
            {
                if (particles[i] == null) continue;
                particles[i].transform.position = center + (Vector3)(dirs[i] * t * 0.5f);
                float s  = Mathf.Lerp(0.12f, 0.04f, t);
                particles[i].transform.localScale = Vector3.one * s;
                srs[i].color = new Color(1f, Mathf.Lerp(0.7f, 0.2f, t), 0.1f, a);
            }
            yield return null;
        }

        foreach (var p in particles)
            if (p != null) Destroy(p);
    }

    private IEnumerator FloatingScore(int points, Vector3 worldPos, bool chain)
    {
        if (worldCanvas == null) yield break;

        var go       = new GameObject("FloatingScore");
        go.transform.SetParent(worldCanvas.transform, false);

        // Convertit la position monde en espace canvas monde
        var rt       = go.AddComponent<RectTransform>();
        rt.position  = worldPos + Vector3.up * 0.3f;
        rt.sizeDelta = new Vector2(2f, 0.5f);

        var tmp         = go.AddComponent<TextMeshProUGUI>();
        tmp.text        = chain ? $"+{points}  COMBO!" : $"+{points}";
        tmp.fontSize    = chain ? 28f : 22f;
        tmp.color       = chain ? new Color(1f, 0.85f, 0f, 1f) : new Color(0.3f, 1f, 0.5f, 1f);
        tmp.alignment   = TextAlignmentOptions.Center;
        tmp.fontStyle   = FontStyles.Bold;

        float duration = 1.0f;
        float elapsed  = 0f;

        Vector3 startPos = rt.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t  = elapsed / duration;
            float a  = t < 0.7f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.7f) / 0.3f);

            rt.position  = startPos + Vector3.up * (t * 0.6f);
            Color c      = tmp.color;
            c.a          = a;
            tmp.color    = c;
            yield return null;
        }

        Destroy(go);
    }
}
