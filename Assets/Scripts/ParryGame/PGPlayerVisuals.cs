using UnityEngine;

/// <summary>
/// Construit et anime le visuel procédural du joueur du Parry Game :
/// ovale blanc simple + éclairs internes qui pulsent de l'intérieur vers l'extérieur.
/// S'attache sur le GameObject racine du joueur via <see cref="Build"/>.
/// </summary>
public class PGPlayerVisuals : MonoBehaviour
{
    // ── Constantes ────────────────────────────────────────────────────────────

    private const int   BoltCount      = 6;
    private const float BoltLength     = 0.90f;   // plus long pour couvrir l'ovale agrandi
    private const float PulseSpeed     = 3.5f;
    private const float GlowPulseSpeed = 2.2f;

    private static readonly Color ColBody      = new Color(0.97f, 0.97f, 1.00f, 1f);
    private static readonly Color ColGlow      = new Color(0.75f, 0.88f, 1.00f, 0.22f);
    private static readonly Color ColBolt      = new Color(0.70f, 0.90f, 1.00f, 1f);

    // ── Runtime ───────────────────────────────────────────────────────────────

    private SpriteRenderer _glowSR;
    private Transform      _glowTR;
    private LineRenderer[] _bolts;
    private float          _phase;

    // ── Construction statique ─────────────────────────────────────────────────

    /// <summary>
    /// Crée le visuel joueur sur <paramref name="root"/> et attache ce composant.
    /// </summary>
    public static PGPlayerVisuals Build(Transform root)
    {
        // ── Glow diffus (couche derrière le corps) ────────────────────────────
        var glowGO = new GameObject("PlayerGlow");
        glowGO.transform.SetParent(root, false);
        glowGO.transform.localPosition = Vector3.zero;
        glowGO.transform.localScale    = new Vector3(2.2f, 1.65f, 1f);

        var glowSR          = glowGO.AddComponent<SpriteRenderer>();
        glowSR.sprite       = SpriteGenerator.CreateCircle(64);
        glowSR.color        = ColGlow;
        glowSR.sortingOrder = 3;

        // ── Corps ovale blanc ─────────────────────────────────────────────────
        var bodyGO = new GameObject("PlayerBody");
        bodyGO.transform.SetParent(root, false);
        bodyGO.transform.localPosition = Vector3.zero;
        bodyGO.transform.localScale    = new Vector3(1.10f, 1.55f, 1f); // ovale vertical agrandi

        var bodySR          = bodyGO.AddComponent<SpriteRenderer>();
        bodySR.sprite       = SpriteGenerator.CreateCircle(128);
        bodySR.color        = ColBody;
        bodySR.sortingOrder = 5;

        // ── Éclairs (LineRenderers) ───────────────────────────────────────────
        var bolts = new LineRenderer[BoltCount];
        for (int i = 0; i < BoltCount; i++)
            bolts[i] = BuildBolt(root, i);

        // ── Composant d'animation ─────────────────────────────────────────────
        var vis        = root.gameObject.AddComponent<PGPlayerVisuals>();
        vis._glowSR    = glowSR;
        vis._glowTR    = glowGO.transform;
        vis._bolts     = bolts;
        vis._phase     = 0f;

        return vis;
    }

    private static LineRenderer BuildBolt(Transform root, int index)
    {
        var go = new GameObject($"Bolt{index}");
        go.transform.SetParent(root, false);
        go.transform.localPosition = Vector3.zero;

        var lr               = go.AddComponent<LineRenderer>();
        lr.positionCount     = 4;
        lr.useWorldSpace     = false;
        lr.startWidth        = 0.025f;
        lr.endWidth          = 0.005f;
        lr.startColor        = new Color(ColBolt.r, ColBolt.g, ColBolt.b, 0f);
        lr.endColor          = new Color(ColBolt.r, ColBolt.g, ColBolt.b, 0f);
        lr.sortingOrder      = 6;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows    = false;
        lr.numCapVertices    = 2;
        lr.textureMode       = LineTextureMode.Stretch;

        var mat = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
        lr.sharedMaterial = mat;

        return lr;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Update()
    {
        _phase += Time.deltaTime;
        AnimateGlow();
        AnimateBolts();
    }

    // ── Animation ─────────────────────────────────────────────────────────────

    private void AnimateGlow()
    {
        if (_glowSR == null) return;
        float t     = Mathf.Sin(_phase * GlowPulseSpeed) * 0.5f + 0.5f;
        float alpha = Mathf.Lerp(0.10f, 0.35f, t);
        float scale = Mathf.Lerp(1.40f, 1.70f, t);
        _glowSR.color      = new Color(ColGlow.r, ColGlow.g, ColGlow.b, alpha);
        _glowTR.localScale = new Vector3(scale, scale * 0.75f, 1f);
    }

    private void AnimateBolts()
    {
        if (_bolts == null) return;

        for (int i = 0; i < _bolts.Length; i++)
        {
            var lr = _bolts[i];
            if (lr == null) continue;

            // Chaque éclair a une phase décalée et une direction angulaire fixe
            float angleBase = (360f / BoltCount) * i;
            float phasedT   = Mathf.Sin(_phase * PulseSpeed + i * 1.3f) * 0.5f + 0.5f;

            // Pulsation depuis l'intérieur : origin part du centre, tip va vers l'extérieur
            float alpha  = Mathf.Lerp(0f, 0.90f, phasedT);
            float length = Mathf.Lerp(0.05f, BoltLength, phasedT);

            // Zigzag éclair : 4 points avec offsets latéraux
            float angleRad = angleBase * Mathf.Deg2Rad;
            var   dir      = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0f);
            var   perp     = new Vector3(-dir.y, dir.x, 0f);

            float jitter = length * 0.25f;
            lr.SetPosition(0, Vector3.zero);
            lr.SetPosition(1, dir * length * 0.33f + perp * jitter * (i % 2 == 0 ? 1f : -1f));
            lr.SetPosition(2, dir * length * 0.66f + perp * jitter * (i % 2 == 0 ? -0.5f : 0.5f));
            lr.SetPosition(3, dir * length);

            var col         = new Color(ColBolt.r, ColBolt.g, ColBolt.b, alpha);
            var colFade     = new Color(ColBolt.r, ColBolt.g, ColBolt.b, 0f);
            lr.startColor   = colFade;
            lr.endColor     = col;
        }
    }
}
