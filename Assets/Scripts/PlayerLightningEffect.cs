using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Génère des éclairs procéduraux qui crépitent autour du joueur.
/// Le rayon de base grandit à chaque collectible ramassé.
/// S'attache au GameObject du joueur — aucun sprite externe requis.
/// </summary>
public class PlayerLightningEffect : MonoBehaviour
{
    // ── Configuration ─────────────────────────────────────────────────────────

    [Header("Éclairs")]
    [SerializeField] private int   boltCount        = 5;
    [SerializeField] private int   segmentsPerBolt  = 6;
    [SerializeField] private float boltUpdateRate   = 0.04f;   // secondes entre rafraîchissements
    [SerializeField] private float boltMinOffset    = 0.06f;   // perturbation min par segment
    [SerializeField] private float boltMaxOffset    = 0.18f;   // perturbation max par segment

    [Header("Rayon")]
    [SerializeField] private float baseRadius       = 0.35f;
    [SerializeField] private float radiusPerCollect = 0.07f;
    [SerializeField] private float maxRadius        = 1.2f;
    [SerializeField] private float growDuration     = 0.25f;

    [Header("Couleurs")]
    [SerializeField] private Color colorInner       = new Color(1f, 1f, 1f, 0.95f);
    [SerializeField] private Color colorOuter       = new Color(0.6f, 0.8f, 1f, 0f);

    // ── État ──────────────────────────────────────────────────────────────────

    private float currentRadius;
    private float targetRadius;
    private float growElapsed;
    private bool  isGrowing;

    private readonly List<LineRenderer> bolts = new();
    private float boltTimer;

    // ── Init ──────────────────────────────────────────────────────────────────

    private void Awake()
    {
        currentRadius = baseRadius;
        targetRadius  = baseRadius;
        BuildBolts();
    }

    private void BuildBolts()
    {
        for (int i = 0; i < boltCount; i++)
        {
            var go = new GameObject($"Lightning_{i}");
            go.transform.SetParent(transform, false);

            var lr                    = go.AddComponent<LineRenderer>();
            lr.positionCount          = segmentsPerBolt + 1;
            lr.useWorldSpace          = true;
            lr.startWidth             = 0.025f;
            lr.endWidth               = 0.005f;
            lr.startColor             = colorInner;
            lr.endColor               = colorOuter;
            lr.shadowCastingMode      = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows         = false;
            lr.sortingOrder           = 20;

            // Matériau simple : Sprites/Default (intégré dans URP)
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            lr.material               = mat;
            bolts.Add(lr);
        }
    }

    // ── Boucle ────────────────────────────────────────────────────────────────

    private void Update()
    {
        // Croissance smooth du rayon
        if (isGrowing)
        {
            growElapsed   += Time.deltaTime;
            float t        = Mathf.SmoothStep(0f, 1f, growElapsed / growDuration);
            currentRadius  = Mathf.Lerp(currentRadius, targetRadius, t);
            if (growElapsed >= growDuration)
            {
                currentRadius = targetRadius;
                isGrowing     = false;
            }
        }

        // Mise à jour des éclairs
        boltTimer += Time.deltaTime;
        if (boltTimer >= boltUpdateRate)
        {
            boltTimer = 0f;
            RefreshBolts();
        }
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>Appeler à chaque collectible ramassé — grandit le rayon d'éclairs.</summary>
    public void OnCollect()
    {
        float startR  = currentRadius;
        targetRadius  = Mathf.Min(targetRadius + radiusPerCollect, maxRadius);
        growElapsed   = 0f;
        isGrowing     = true;

        StartCoroutine(FlashRoutine());
    }

    // ── Éclairs ───────────────────────────────────────────────────────────────

    private void RefreshBolts()
    {
        float angleStep = 360f / boltCount;

        for (int i = 0; i < bolts.Count; i++)
        {
            float startAngle = (i * angleStep + Random.Range(-30f, 30f)) * Mathf.Deg2Rad;

            // Point de départ sur le cercle intérieur (~80% du rayon)
            Vector2 origin = PolarToWorld(startAngle, currentRadius * 0.8f);
            // Point final légèrement en dehors du rayon
            float   endAngle = startAngle + Random.Range(-0.5f, 0.5f);
            Vector2 end      = PolarToWorld(endAngle, currentRadius * 1.1f + Random.Range(0f, 0.08f));

            SetBoltPositions(bolts[i], origin, end);
        }
    }

    private void SetBoltPositions(LineRenderer lr, Vector2 start, Vector2 end)
    {
        lr.SetPosition(0, new Vector3(start.x, start.y, 0f));

        for (int s = 1; s < segmentsPerBolt; s++)
        {
            float t   = (float)s / segmentsPerBolt;
            Vector2 p = Vector2.Lerp(start, end, t);

            // Perturbation perpendiculaire
            Vector2 dir  = (end - start).normalized;
            Vector2 perp = new Vector2(-dir.y, dir.x);
            p += perp * Random.Range(-boltMaxOffset, boltMaxOffset);
            p += dir  * Random.Range(-boltMinOffset, boltMinOffset);

            lr.SetPosition(s, new Vector3(p.x, p.y, 0f));
        }

        lr.SetPosition(segmentsPerBolt, new Vector3(end.x, end.y, 0f));
    }

    private Vector2 PolarToWorld(float rad, float r)
    {
        Vector3 wp = transform.position;
        return new Vector2(wp.x + Mathf.Cos(rad) * r, wp.y + Mathf.Sin(rad) * r);
    }

    // ── Flash de collecte ─────────────────────────────────────────────────────

    private IEnumerator FlashRoutine()
    {
        // Éclairs plus épais et plus lumineux pendant 0.15 s
        foreach (var lr in bolts)
        {
            lr.startWidth = 0.055f;
            lr.endWidth   = 0.025f;
        }

        yield return new WaitForSeconds(0.15f);

        foreach (var lr in bolts)
        {
            lr.startWidth = 0.025f;
            lr.endWidth   = 0.005f;
        }
    }
}
