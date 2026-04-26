using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ennemi rouge à éviter dans le Game &amp; Watch.
///
/// Comportement :
///   - Tombe à la vitesse normale de la grille (1 step par tick, comme Collectible).
///   - Si le joueur le touche → perte d'une vie + feedback visuel.
///   - S'il atteint le sol sans être touché → disparaît silencieusement (pas de pénalité).
///
/// Visuel : sphère rouge avec halo pulsant et mini-éclairs rouges.
/// </summary>
public class RedEnemy : MonoBehaviour
{
    [Header("Grid Movement Settings")]
    [SerializeField] private float gridSize = 0.5f;
    [SerializeField] private float destroyY = -6f;
    [SerializeField] private float groundY  = -4.5f;

    // ── Palette rouge ─────────────────────────────────────────────────────────

    private static readonly Color CoreColor  = new Color(0.95f, 0.15f, 0.10f, 1.00f);
    private static readonly Color GlowColor  = new Color(1.00f, 0.10f, 0.05f, 0.30f);
    private static readonly Color BoltInner  = new Color(1.00f, 0.55f, 0.45f, 0.95f);
    private static readonly Color BoltOuter  = new Color(1.00f, 0.10f, 0.05f, 0.00f);

    private const float CoreDiameter   = 0.32f;
    private const int   BoltCount      = 3;
    private const float BoltUpdateRate = 0.05f;

    // ── État ──────────────────────────────────────────────────────────────────

    private float columnX;
    private bool  columnSet    = false;
    private bool  wasHit       = false;
    private float currentGridY;

    // Visuels
    private SpriteRenderer glowSR;
    private LineRenderer[] bolts;
    private float          boltTimer;

    // ── Awake ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        BuildBody();
        BuildGlow();
        BuildBolts();
        BuildSkullMarker();
    }

    private void BuildBody()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite       = SpriteGenerator.CreateCircle(128);
        sr.color        = CoreColor;
        sr.sortingOrder = 8;
        transform.localScale = Vector3.one * CoreDiameter;
    }

    private void BuildGlow()
    {
        var go = new GameObject("Glow");
        go.transform.SetParent(transform, false);
        go.transform.localScale = Vector3.one * 2.4f;

        glowSR              = go.AddComponent<SpriteRenderer>();
        glowSR.sprite       = SpriteGenerator.CreateCircle(128);
        glowSR.color        = GlowColor;
        glowSR.sortingOrder = 7;
    }

    private void BuildBolts()
    {
        bolts = new LineRenderer[BoltCount];
        var mat = SpriteGenerator.GetAdditiveMaterial();

        for (int i = 0; i < BoltCount; i++)
        {
            var go = new GameObject($"Bolt_{i}");
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
            lr.sortingOrder     = 9;
            bolts[i]            = lr;
        }
    }

    /// <summary>Petite croix "✕" blanche au centre pour signaler le danger.</summary>
    private void BuildSkullMarker()
    {
        // Ligne horizontale
        BuildMarkerLine("MarkerH", new Vector3(-0.28f, 0f, 0f), new Vector3(0.28f, 0f, 0f));
        // Ligne diagonale gauche-droite
        BuildMarkerLine("MarkerD1", new Vector3(-0.22f, -0.22f, 0f), new Vector3(0.22f, 0.22f, 0f));
        // Ligne diagonale droite-gauche
        BuildMarkerLine("MarkerD2", new Vector3(0.22f, -0.22f, 0f), new Vector3(-0.22f, 0.22f, 0f));
    }

    private void BuildMarkerLine(string name, Vector3 localA, Vector3 localB)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(transform, false);

        var lr              = go.AddComponent<LineRenderer>();
        lr.positionCount    = 2;
        lr.useWorldSpace    = false;
        lr.startWidth       = 0.035f;
        lr.endWidth         = 0.035f;
        lr.startColor       = Color.white;
        lr.endColor         = Color.white;
        lr.material         = SpriteGenerator.GetAdditiveMaterial();
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows   = false;
        lr.sortingOrder     = 10;
        lr.SetPosition(0, localA);
        lr.SetPosition(1, localB);
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>Snaps l'ennemi à sa colonne et s'abonne au tick global de grille.</summary>
    public void SetColumn(float xPosition)
    {
        columnX      = xPosition;
        columnSet    = true;
        currentGridY = Mathf.Round(transform.position.y / gridSize) * gridSize;
        transform.position = new Vector3(columnX, currentGridY, transform.position.z);
    }

    /// <summary>No-op — le rythme est piloté par le tick global du GameManager.</summary>
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
    }

    private void Update()
    {
        // Halo pulsant
        if (glowSR != null)
        {
            float a = 0.25f + 0.18f * Mathf.Sin(Time.time * 6f);
            glowSR.color = new Color(GlowColor.r, GlowColor.g, GlowColor.b, a);
        }

        // Rafraîchissement éclairs
        boltTimer += Time.deltaTime;
        if (boltTimer >= BoltUpdateRate)
        {
            boltTimer = 0f;
            RefreshBolts();
        }
    }

    private void RefreshBolts()
    {
        float worldR   = CoreDiameter * 0.5f;
        float angleStep = 360f / BoltCount;
        Vector3 origin = transform.position;

        for (int i = 0; i < BoltCount; i++)
        {
            float baseRad = (i * angleStep + Random.Range(-60f, 60f)) * Mathf.Deg2Rad;
            Vector3 start = origin + new Vector3(Mathf.Cos(baseRad), Mathf.Sin(baseRad), 0f) * worldR * 0.7f;
            float   endA  = baseRad + Random.Range(-0.8f, 0.8f);
            Vector3 end   = origin + new Vector3(Mathf.Cos(endA), Mathf.Sin(endA), 0f) * (worldR * 2f + Random.Range(0f, worldR * 0.4f));

            bolts[i].SetPosition(0, start);
            for (int s = 1; s < 4; s++)
            {
                float t   = (float)s / 4f;
                Vector3 p = Vector3.Lerp(start, end, t);
                Vector3 perp = Vector3.Cross((end - start).normalized, Vector3.forward);
                p += perp * Random.Range(-0.06f, 0.06f);
                bolts[i].SetPosition(s, p);
            }
            bolts[i].SetPosition(4, end);
        }
    }

    // ── Grid step ─────────────────────────────────────────────────────────────

    private void OnStep()
    {
        if (!columnSet || wasHit) return;

        currentGridY -= gridSize;
        transform.position = new Vector3(columnX, currentGridY, transform.position.z);

        if (currentGridY <= groundY)
        {
            // Atteint le sol sans être touché → disparaît sans pénalité
            wasHit = true;
            Destroy(gameObject);
        }
        else if (currentGridY < destroyY)
        {
            Destroy(gameObject);
        }
    }

    // ── Trigger ───────────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !wasHit)
            HitPlayer();
    }

    private void HitPlayer()
    {
        wasHit = true;
        GameManager.Instance?.LoseLife();
        UIManager.Instance?.ShowLifeLostEffect();
        Destroy(gameObject);
    }
}
