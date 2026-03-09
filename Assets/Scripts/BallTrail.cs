using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Trail lumineux derrière la balle en coordonnées Canvas.
/// Pool de cercles Image réutilisés, zéro allocation par frame.
/// </summary>
public class BallTrail : MonoBehaviour
{
    // ── Configuration ─────────────────────────────────────────────────────────

    private const int   TrailLength     = 14;
    private const float SpawnInterval   = 0.025f;
    private const float StartRadius     = 18f;
    private const float EndRadius       = 4f;

    private static readonly Color TrailStart = new Color(1f, 1f, 1f, 0.50f);
    private static readonly Color TrailEnd   = new Color(1f, 1f, 1f, 0f);

    // ── État ──────────────────────────────────────────────────────────────────

    private RectTransform      container;
    private MenuBall           ball;
    private readonly List<Image>       dots      = new();
    private readonly Queue<Vector2>    positions = new();
    private float spawnTimer;

    // ── Setup ─────────────────────────────────────────────────────────────────

    /// <summary>Injecte le conteneur RectTransform.</summary>
    public void SetContainer(RectTransform rt) => container = rt;

    /// <summary>Injecte la balle à suivre.</summary>
    public void SetBall(MenuBall b) => ball = b;

    private void Start() => BuildPool();

    private void BuildPool()
    {
        for (int i = 0; i < TrailLength; i++)
        {
            var go  = new GameObject($"TrailDot_{i}");
            go.transform.SetParent(container, false);

            var img             = go.AddComponent<Image>();
            img.sprite          = SpriteGenerator.Circle();
            img.color           = Color.clear;
            img.raycastTarget   = false;

            var rt              = img.rectTransform;
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);

            dots.Add(img);
        }
    }

    // ── Boucle ────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (ball == null) return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= SpawnInterval)
        {
            spawnTimer = 0f;
            positions.Enqueue(ball.GetCanvasPosition());
            if (positions.Count > TrailLength) positions.Dequeue();
        }

        Refresh();
    }

    private void Refresh()
    {
        Vector2[] arr   = new Vector2[positions.Count];
        positions.CopyTo(arr, 0);
        int count = arr.Length;

        for (int i = 0; i < dots.Count; i++)
        {
            if (i >= count) { dots[i].color = Color.clear; continue; }

            int   idx = count - 1 - i;
            float t   = count > 1 ? (float)i / (count - 1) : 0f;
            float r   = Mathf.Lerp(StartRadius, EndRadius, t);

            dots[i].color                    = Color.Lerp(TrailStart, TrailEnd, t);
            dots[i].rectTransform.anchoredPosition = arr[idx];
            dots[i].rectTransform.sizeDelta        = Vector2.one * r * 2f;
        }
    }
}
