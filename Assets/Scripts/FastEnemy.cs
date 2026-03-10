using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ennemi rapide (2× vitesse de la grille).
/// Visuel : boule bleue électrique avec éclairs bleus et une trainée
/// de petites boules qui se dissolvent derrière elle.
/// </summary>
public class FastEnemy : MonoBehaviour
{
    [Header("Grid Movement Settings")]
    [SerializeField] private float gridSize     = 0.5f;
    [SerializeField] private float destroyY     = -6f;
    [SerializeField] private float groundY      = -4.5f;

    [Header("Score Value")]
    [SerializeField] private int scoreValue     = 10;

    // ── Palette bleue ─────────────────────────────────────────────────────────

    private static readonly Color CoreColor  = new Color(0.30f, 0.65f, 1.00f, 1.00f);
    private static readonly Color GlowColor  = new Color(0.10f, 0.45f, 1.00f, 0.30f);
    private static readonly Color BoltInner  = new Color(0.55f, 0.85f, 1.00f, 0.95f);
    private static readonly Color BoltOuter  = new Color(0.20f, 0.60f, 1.00f, 0.00f);
    private static readonly Color TrailColor = new Color(0.25f, 0.60f, 1.00f, 0.70f);

    private const float CoreDiameter   = 0.32f;
    private const int   BoltCount      = 4;
    private const float BoltUpdateRate = 0.04f;
    private const int   TrailLength    = 6;

    // ── État ──────────────────────────────────────────────────────────────────

    private float columnX;
    private bool  columnSet    = false;
    private bool  wasCollected = false;
    private float currentGridY;

    // Visuels
    private SpriteRenderer   glowSR;
    private LineRenderer[]   bolts;
    private float            boltTimer;
    private readonly List<GameObject> trailDots = new List<GameObject>();
    private readonly Queue<Vector3>   posHistory = new Queue<Vector3>();
    private const int                 PosHistoryMax = TrailLength + 1;

    // ── Awake ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        BuildBody();
        BuildGlow();
        BuildBolts();
        BuildTrail();
    }

    private void BuildBody()
    {
        var sr          = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite       = SpriteGenerator.CreateCircle(128);
        sr.color        = CoreColor;
        sr.sortingOrder = 8;
        transform.localScale = Vector3.one * CoreDiameter;
    }

    private void BuildGlow()
    {
        var go             = new GameObject("Glow");
        go.transform.SetParent(transform, false);
        go.transform.localScale = Vector3.one * 2.2f;

        glowSR              = go.AddComponent<SpriteRenderer>();
        glowSR.sprite       = SpriteGenerator.CreateCircle(128);
        glowSR.color        = GlowColor;
        glowSR.sortingOrder = 7;
    }

    private void BuildBolts()
    {
        bolts = new LineRenderer[BoltCount];
        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);

        for (int i = 0; i < BoltCount; i++)
        {
            var go              = new GameObject($"Bolt_{i}");
            go.transform.SetParent(transform, false);

            var lr              = go.AddComponent<LineRenderer>();
            lr.positionCount    = 5;
            lr.useWorldSpace    = true;
            lr.startWidth       = 0.016f;
            lr.endWidth         = 0.003f;
            lr.startColor       = BoltInner;
            lr.endColor         = BoltOuter;
            lr.material         = mat;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows   = false;
            lr.sortingOrder     = 9;
            bolts[i]            = lr;
        }
    }

    private void BuildTrail()
    {
        float baseDiam = CoreDiameter;
        for (int i = 0; i < TrailLength; i++)
        {
            var go          = new GameObject($"Trail_{i}");
            // trail dots vivent dans l'espace monde — pas enfants du transform
            go.transform.position = transform.position;

            float t        = (float)(i + 1) / TrailLength;
            float scale    = Mathf.Lerp(baseDiam * 0.55f, baseDiam * 0.12f, t);
            float alpha    = Mathf.Lerp(0.65f, 0.05f, t);
            go.transform.localScale = Vector3.one * scale;

            var sr          = go.AddComponent<SpriteRenderer>();
            sr.sprite       = SpriteGenerator.CreateCircle(128);
            sr.color        = new Color(TrailColor.r, TrailColor.g, TrailColor.b, alpha);
            sr.sortingOrder = 6 - i;

            trailDots.Add(go);
        }
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>Snaps the enemy to its column and registers it to the global grid tick.</summary>
    public void SetColumn(float xPosition)
    {
        columnX      = xPosition;
        columnSet    = true;
        currentGridY = Mathf.Round(transform.position.y / gridSize) * gridSize;
        transform.position = new Vector3(columnX, currentGridY, transform.position.z);
    }

    /// <summary>No-op — step rate is driven by the global GameManager tick.</summary>
    public void SetStepDuration(float duration) { }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGridStep += OnStep;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGridStep -= OnStep;

        // Nettoie les trail dots orphelins
        foreach (var dot in trailDots)
            if (dot != null) Destroy(dot);
        trailDots.Clear();
    }

    private void Update()
    {
        // Glow pulsant
        if (glowSR != null)
        {
            float a      = 0.22f + 0.12f * Mathf.Sin(Time.time * 5.5f);
            glowSR.color = new Color(GlowColor.r, GlowColor.g, GlowColor.b, a);
        }

        // Enregistre l'historique de position pour la trainée
        posHistory.Enqueue(transform.position);
        if (posHistory.Count > PosHistoryMax) posHistory.Dequeue();

        UpdateTrail();

        // Rafraîchissement éclairs
        boltTimer += Time.deltaTime;
        if (boltTimer >= BoltUpdateRate)
        {
            boltTimer = 0f;
            RefreshBolts();
        }
    }

    private void UpdateTrail()
    {
        Vector3[] hist = posHistory.ToArray();
        int count = hist.Length;

        for (int i = 0; i < trailDots.Count; i++)
        {
            int histIdx = count - 2 - i;
            if (histIdx >= 0 && histIdx < count)
                trailDots[i].transform.position = hist[histIdx];
            else
                trailDots[i].transform.position = transform.position;
        }
    }

    private void RefreshBolts()
    {
        float worldR = CoreDiameter * 0.5f;
        float angle  = 360f / BoltCount;
        Vector3 origin = transform.position;

        for (int i = 0; i < BoltCount; i++)
        {
            float baseRad = (i * angle + Random.Range(-50f, 50f)) * Mathf.Deg2Rad;
            Vector3 start = origin + new Vector3(Mathf.Cos(baseRad), Mathf.Sin(baseRad), 0f) * worldR * 0.7f;
            float   endA  = baseRad + Random.Range(-0.7f, 0.7f);
            Vector3 end   = origin + new Vector3(Mathf.Cos(endA), Mathf.Sin(endA), 0f) * (worldR * 1.8f + Random.Range(0f, worldR * 0.5f));

            bolts[i].SetPosition(0, start);
            for (int s = 1; s < 4; s++)
            {
                float t   = (float)s / 4f;
                Vector3 p = Vector3.Lerp(start, end, t);
                Vector3 perp = Vector3.Cross((end - start).normalized, Vector3.forward);
                p += perp * Random.Range(-0.05f, 0.05f);
                bolts[i].SetPosition(s, p);
            }
            bolts[i].SetPosition(4, end);
        }
    }

    // ── Grid step ─────────────────────────────────────────────────────────────

    private void OnStep()
    {
        if (!columnSet || wasCollected) return;

        currentGridY -= gridSize * 2f;   // 2× vitesse
        transform.position = new Vector3(columnX, currentGridY, transform.position.z);

        if (currentGridY <= groundY)
            MissedEnemy();
        else if (currentGridY < destroyY)
            Destroy(gameObject);
    }

    // ── Trigger ───────────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !wasCollected)
            Collect();
    }

    private void Collect()
    {
        wasCollected = true;

        if (GameManager.Instance != null)
            GameManager.Instance.AddScore(scoreValue);

        UIManager.Instance?.ShowScoreGain(scoreValue);
        UIManager.Instance?.ShowPerfectEffect();
        ScreenGlitch.Instance?.Trigger();

        Destroy(gameObject);
    }

    private void MissedEnemy()
    {
        wasCollected = true;
        GameManager.Instance?.LoseLife();
        Destroy(gameObject);
    }
}
