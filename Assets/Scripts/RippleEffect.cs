using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Génère des ondes circulaires (ripples) en coordonnées Canvas.
/// Pool de cercles Image pour zéro allocation par frame.
/// </summary>
public class RippleEffect : MonoBehaviour
{
    // ── Configuration ─────────────────────────────────────────────────────────

    private const int   PoolSize          = 8;
    private const float RippleDuration    = 0.55f;
    private const float MaxRadius         = 90f;
    private const float ButtonMaxRadius   = 70f;
    private const int   ButtonRippleCount = 3;
    private const float ButtonRippleDelay = 0.08f;

    private static readonly Color BorderColor = new Color(1f, 1f, 1f, 0.70f);
    private static readonly Color ButtonColor = new Color(1f, 1f, 1f, 0.90f);

    // ── État ──────────────────────────────────────────────────────────────────

    private RectTransform        container;
    private readonly Queue<Image> pool = new();

    // ── Setup ─────────────────────────────────────────────────────────────────

    /// <summary>Injecte le conteneur Canvas (appelé par MenuSceneSetup).</summary>
    public void SetContainer(RectTransform rt)
    {
        container = rt;
        BuildPool();
    }

    private void BuildPool()
    {
        for (int i = 0; i < PoolSize; i++)
        {
            var go          = new GameObject($"Ripple_{i}");
            go.transform.SetParent(container, false);
            go.SetActive(false);

            var img         = go.AddComponent<Image>();
            img.sprite      = SpriteGenerator.Circle();
            img.color       = Color.clear;
            img.raycastTarget = false;

            var rt          = img.rectTransform;
            rt.anchorMin    = new Vector2(0.5f, 0.5f);
            rt.anchorMax    = new Vector2(0.5f, 0.5f);
            rt.pivot        = new Vector2(0.5f, 0.5f);

            pool.Enqueue(img);
        }
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>Spawne un ripple à la position Canvas donnée.</summary>
    public void SpawnRipple(Vector2 canvasPosition, bool isButton)
    {
        if (isButton)
            StartCoroutine(SpawnMultiple(canvasPosition));
        else
            StartCoroutine(Animate(canvasPosition, MaxRadius, BorderColor));
    }

    // ── Coroutines ────────────────────────────────────────────────────────────

    private IEnumerator SpawnMultiple(Vector2 pos)
    {
        for (int i = 0; i < ButtonRippleCount; i++)
        {
            StartCoroutine(Animate(pos, ButtonMaxRadius, ButtonColor));
            yield return new WaitForSeconds(ButtonRippleDelay);
        }
    }

    private IEnumerator Animate(Vector2 pos, float maxRadius, Color color)
    {
        if (pool.Count == 0) yield break;

        var img = pool.Dequeue();
        img.gameObject.SetActive(true);
        img.rectTransform.anchoredPosition = pos;

        float elapsed = 0f;
        while (elapsed < RippleDuration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.Clamp01(elapsed / RippleDuration);
            float r  = Mathf.Lerp(0f, maxRadius, t);
            float a  = Mathf.Clamp01(1f - t * t) * color.a;

            img.rectTransform.sizeDelta = Vector2.one * r * 2f;
            img.color = new Color(color.r, color.g, color.b, a);

            yield return null;
        }

        img.color = Color.clear;
        img.gameObject.SetActive(false);
        pool.Enqueue(img);
    }
}
