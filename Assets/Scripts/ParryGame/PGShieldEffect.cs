using System.Collections;
using UnityEngine;

/// <summary>
/// 3D world-space shield visual: a glowing translucent wall that appears in
/// front of the player when the Shield ability is active.
///
/// Visual language:
///   - Bright cyan-white pulsing quad in front of the player
///   - Hexagonal shimmer lines drawn via LineRenderers
///   - On absorb  → flash white, crack burst, then disappear
///   - On expire  → slow fade out
/// </summary>
public class PGShieldEffect : MonoBehaviour
{
    // ── Constants ─────────────────────────────────────────────────────────────

    private static readonly Color ColShieldBase   = new Color(0.40f, 0.85f, 1.00f, 0.38f);
    private static readonly Color ColShieldGlow   = new Color(0.60f, 1.00f, 1.00f, 0.72f);
    private static readonly Color ColAbsorbFlash  = new Color(1.00f, 1.00f, 1.00f, 1.00f);
    private static readonly Color ColShimmer      = new Color(0.80f, 1.00f, 1.00f, 0.55f);

    private const float ShieldZ       = 0.8f;   // Z in front of player (player is at Z=0)
    private const float PulseSpeed    = 2.6f;
    private const float ShimmerSpeed  = 1.8f;

    // ── Runtime objects ───────────────────────────────────────────────────────

    private Renderer       _mainQuad;
    private LineRenderer[] _shimmerLines;
    private bool           _destroyed;

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>Creates and starts the shield effect. Returns the component.</summary>
    public static PGShieldEffect Spawn()
    {
        var root = new GameObject("ShieldEffect");
        // Place in front of the player (player origin is at 0.4, -0.6, 0)
        root.transform.position = new Vector3(0.4f, 0.2f, ShieldZ);

        var effect = root.AddComponent<PGShieldEffect>();
        effect.Build();
        return effect;
    }

    // ── Build ─────────────────────────────────────────────────────────────────

    private void Build()
    {
        BuildMainPanel();
        BuildEdgeGlow();
        BuildShimmerLines();
        StartCoroutine(PulseRoutine());
        StartCoroutine(ShimmerRoutine());
    }

    /// <summary>Large translucent quad — the main shield face.</summary>
    private void BuildMainPanel()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "ShieldPanel";
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(transform, false);
        go.transform.localScale = new Vector3(2.8f, 2.4f, 1f);
        go.transform.localPosition = Vector3.zero;

        var mat = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
        mat.color = ColShieldBase;
        _mainQuad = go.GetComponent<Renderer>();
        _mainQuad.material = mat;
        _mainQuad.sortingOrder = 5;
    }

    /// <summary>Thin bright border around the shield face.</summary>
    private void BuildEdgeGlow()
    {
        // Four edge lines
        Vector3[] corners =
        {
            new(-1.4f, -1.2f, 0f),
            new( 1.4f, -1.2f, 0f),
            new( 1.4f,  1.2f, 0f),
            new(-1.4f,  1.2f, 0f),
            new(-1.4f, -1.2f, 0f), // close the loop
        };

        var go = new GameObject("ShieldEdge");
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount    = corners.Length;
        lr.useWorldSpace    = false;
        lr.loop             = false;
        lr.startWidth       = 0.06f;
        lr.endWidth         = 0.06f;
        lr.startColor       = ColShieldGlow;
        lr.endColor         = ColShieldGlow;
        lr.numCapVertices   = 4;
        lr.sortingOrder     = 8;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows   = false;
        lr.sharedMaterial   = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
        lr.SetPositions(corners);
    }

    /// <summary>Three diagonal shimmer lines that drift across the shield.</summary>
    private void BuildShimmerLines()
    {
        _shimmerLines = new LineRenderer[3];
        for (int i = 0; i < 3; i++)
        {
            var go = new GameObject($"Shimmer_{i}");
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount    = 2;
            lr.useWorldSpace    = false;
            lr.loop             = false;
            lr.startWidth       = 0.025f;
            lr.endWidth         = 0.025f;
            lr.startColor       = ColShimmer;
            lr.endColor         = Color.clear;
            lr.numCapVertices   = 2;
            lr.sortingOrder     = 9;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows   = false;
            lr.sharedMaterial   = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            _shimmerLines[i]    = lr;
        }
    }

    // ── Pulse ─────────────────────────────────────────────────────────────────

    private IEnumerator PulseRoutine()
    {
        while (!_destroyed)
        {
            float t     = Mathf.Sin(Time.time * PulseSpeed) * 0.5f + 0.5f;
            float alpha = Mathf.Lerp(0.25f, 0.55f, t);
            float scale = Mathf.Lerp(0.95f, 1.05f, t);

            if (_mainQuad != null)
            {
                var c = ColShieldBase;
                c.a   = alpha;
                _mainQuad.material.color = c;
            }
            transform.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
    }

    // ── Shimmer drift ─────────────────────────────────────────────────────────

    private IEnumerator ShimmerRoutine()
    {
        float[] offsets = { 0f, 0.33f, 0.66f };
        while (!_destroyed)
        {
            for (int i = 0; i < _shimmerLines.Length; i++)
            {
                if (_shimmerLines[i] == null) continue;
                float phase = (Time.time * ShimmerSpeed + offsets[i]) % 1f;
                float x     = Mathf.Lerp(-1.4f, 1.4f, phase);
                _shimmerLines[i].SetPosition(0, new Vector3(x - 0.3f, -1.2f, 0f));
                _shimmerLines[i].SetPosition(1, new Vector3(x + 0.3f,  1.2f, 0f));
            }
            yield return null;
        }
    }

    // ── Absorb feedback ───────────────────────────────────────────────────────

    /// <summary>Called when the shield absorbs an enemy — flash white then burst away.</summary>
    public void TriggerAbsorb()
    {
        if (_destroyed) return;
        _destroyed = true;
        StopAllCoroutines();
        StartCoroutine(AbsorbSequence());
    }

    private IEnumerator AbsorbSequence()
    {
        // Instant white flash
        if (_mainQuad != null)
            _mainQuad.material.color = ColAbsorbFlash;

        // Scale burst outward
        float t = 0f;
        while (t < 0.25f)
        {
            t += Time.unscaledDeltaTime;
            float s = Mathf.Lerp(1f, 2.4f, t / 0.25f);
            transform.localScale = new Vector3(s, s, 1f);

            // Fade out
            if (_mainQuad != null)
            {
                var c = ColAbsorbFlash;
                c.a   = Mathf.Lerp(1f, 0f, t / 0.25f);
                _mainQuad.material.color = c;
            }
            HideShimmerLines();
            yield return null;
        }

        Destroy(gameObject);
    }

    // ── Expire feedback ───────────────────────────────────────────────────────

    /// <summary>Called when the shield expires without blocking anything — slow fade.</summary>
    public void TriggerExpire()
    {
        if (_destroyed) return;
        _destroyed = true;
        StopAllCoroutines();
        StartCoroutine(ExpireSequence());
    }

    private IEnumerator ExpireSequence()
    {
        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(0.35f, 0f, t / 0.4f);
            if (_mainQuad != null)
            {
                var c = ColShieldBase;
                c.a   = a;
                _mainQuad.material.color = c;
            }
            HideShimmerLines();
            yield return null;
        }
        Destroy(gameObject);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void HideShimmerLines()
    {
        if (_shimmerLines == null) return;
        foreach (var lr in _shimmerLines)
            if (lr != null) { lr.startColor = Color.clear; lr.endColor = Color.clear; }
    }
}
