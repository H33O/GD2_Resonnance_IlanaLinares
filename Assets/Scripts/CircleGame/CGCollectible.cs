using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Collectible électrique sur la piste circulaire.
/// Visuel : même style que FastEnemy mais en blanc pur — boule blanche,
/// halo blanc, éclairs blancs, trainée de petites boules blanches.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
public class CGCollectible : MonoBehaviour
{
    // ── Palette blanche ───────────────────────────────────────────────────────

    private static readonly Color CoreColor  = new Color(1.00f, 1.00f, 1.00f, 1.00f);
    private static readonly Color GlowColor  = new Color(0.90f, 0.95f, 1.00f, 0.28f);
    private static readonly Color BoltInner  = new Color(1.00f, 1.00f, 1.00f, 0.95f);
    private static readonly Color BoltOuter  = new Color(0.80f, 0.90f, 1.00f, 0.00f);
    private static readonly Color TrailColor = new Color(0.90f, 0.95f, 1.00f, 0.65f);

    private const float CoreDiameter   = 0.28f;
    private const int   BoltCount      = 4;
    private const float BoltUpdateRate = 0.05f;
    private const int   TrailLength    = 5;

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Settings")]
    public CGSettings settings;

    [Header("References")]
    public CGObstacleContainer obstacleContainer;
    public CGFeedbackManager   feedbackManager;

    // ── État ──────────────────────────────────────────────────────────────────

    private bool           collected;
    private SpriteRenderer glowSR;
    private LineRenderer[] bolts;
    private float          boltTimer;

    private readonly List<GameObject> trailDots  = new List<GameObject>();
    private readonly Queue<Vector3>   posHistory = new Queue<Vector3>();
    private const int                 PosHistoryMax = TrailLength + 1;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        var sr          = GetComponent<SpriteRenderer>();
        sr.sprite       = SpriteGenerator.CreateCircle(128);
        sr.color        = CoreColor;
        sr.sortingOrder = 5;
        transform.localScale = Vector3.one * CoreDiameter;

        var col       = GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.5f;

        BuildGlow();
        BuildBolts();
        BuildTrail();
    }

    private void BuildGlow()
    {
        var go             = new GameObject("Glow");
        go.transform.SetParent(transform, false);
        go.transform.localScale = Vector3.one * 2.2f;

        glowSR              = go.AddComponent<SpriteRenderer>();
        glowSR.sprite       = SpriteGenerator.CreateCircle(128);
        glowSR.color        = GlowColor;
        glowSR.sortingOrder = 4;
    }

    private void BuildBolts()
    {
        bolts = new LineRenderer[BoltCount];
        var mat = SpriteGenerator.GetAdditiveMaterial();

        for (int i = 0; i < BoltCount; i++)
        {
            var go              = new GameObject($"Bolt_{i}");
            go.transform.SetParent(transform, false);

            var lr              = go.AddComponent<LineRenderer>();
            lr.positionCount    = 5;
            lr.useWorldSpace    = true;
            lr.startWidth       = 0.018f;
            lr.endWidth         = 0.003f;
            lr.startColor       = BoltInner;
            lr.endColor         = BoltOuter;
            lr.material         = mat;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows   = false;
            lr.sortingOrder     = 6;
            bolts[i]            = lr;
        }
    }

    private void BuildTrail()
    {
        for (int i = 0; i < TrailLength; i++)
        {
            var go     = new GameObject($"Trail_{i}");
            go.transform.position = transform.position;

            float t    = (float)(i + 1) / TrailLength;
            float scale = Mathf.Lerp(CoreDiameter * 0.55f, CoreDiameter * 0.12f, t);
            float alpha = Mathf.Lerp(0.60f, 0.04f, t);
            go.transform.localScale = Vector3.one * scale;

            var sr          = go.AddComponent<SpriteRenderer>();
            sr.sprite       = SpriteGenerator.CreateCircle(128);
            sr.color        = new Color(TrailColor.r, TrailColor.g, TrailColor.b, alpha);
            sr.sortingOrder = 3 - i;

            trailDots.Add(go);
        }
    }

    private void OnDisable()
    {
        foreach (var dot in trailDots)
            if (dot != null) Destroy(dot);
        trailDots.Clear();
    }

    private void Update()
    {
        // Pulsation halo
        float a      = 0.20f + 0.10f * Mathf.Sin(Time.time * 4.5f);
        glowSR.color = new Color(GlowColor.r, GlowColor.g, GlowColor.b, a);

        // Historique de position pour la trainée
        posHistory.Enqueue(transform.position);
        if (posHistory.Count > PosHistoryMax) posHistory.Dequeue();
        UpdateTrail();

        // Éclairs
        boltTimer += Time.deltaTime;
        if (boltTimer >= BoltUpdateRate)
        {
            boltTimer = 0f;
            RefreshBolts();
        }
    }

    private void UpdateTrail()
    {
        Vector3[] hist  = posHistory.ToArray();
        int       count = hist.Length;
        for (int i = 0; i < trailDots.Count; i++)
        {
            int idx = count - 2 - i;
            trailDots[i].transform.position = idx >= 0 ? hist[idx] : transform.position;
        }
    }

    private void RefreshBolts()
    {
        float   worldR = CoreDiameter * 0.5f;
        float   angle  = 360f / BoltCount;
        Vector3 origin = transform.position;

        for (int i = 0; i < BoltCount; i++)
        {
            float   baseRad = (i * angle + Random.Range(-45f, 45f)) * Mathf.Deg2Rad;
            Vector3 start   = origin + new Vector3(Mathf.Cos(baseRad), Mathf.Sin(baseRad), 0f) * worldR * 0.7f;
            float   endA    = baseRad + Random.Range(-0.6f, 0.6f);
            Vector3 end     = origin + new Vector3(Mathf.Cos(endA), Mathf.Sin(endA), 0f) * (worldR * 1.7f + Random.Range(0f, worldR * 0.5f));

            bolts[i].SetPosition(0, start);
            for (int s = 1; s < 4; s++)
            {
                float   t    = (float)s / 4f;
                Vector3 p    = Vector3.Lerp(start, end, t);
                Vector3 perp = Vector3.Cross((end - start).normalized, Vector3.forward);
                p += perp * Random.Range(-0.04f, 0.04f);
                bolts[i].SetPosition(s, p);
            }
            bolts[i].SetPosition(4, end);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if (!other.CompareTag("Player")) return;
        Collect();
    }

    // ── Collecte ──────────────────────────────────────────────────────────────

    private void Collect()
    {
        collected = true;

        int bonus = settings != null ? settings.collectibleScoreBonus : 5;
        CGGameManager.Instance?.AddScore(bonus);

        feedbackManager?.TriggerCollectParticles(transform.position);

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            player.GetComponent<PlayerVisuals>()?.OnCollect();

        obstacleContainer?.SpawnFromCollectible();

        Destroy(gameObject);
    }
}
