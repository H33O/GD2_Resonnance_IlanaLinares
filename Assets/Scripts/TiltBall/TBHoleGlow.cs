using UnityEngine;

/// <summary>
/// Pulse vert lumineux sur le goal/trou de TiltBall.
/// Deux couches : un ring vert qui grandit/rétrécit, et un halo diffus plus large.
/// S'attache automatiquement sur le GameObject "Hole" via <see cref="TBSceneSetup"/>.
/// </summary>
public class TBHoleGlow : MonoBehaviour
{
    // ── Paramètres ────────────────────────────────────────────────────────────

    private const float RingPulseSpeed  = 2.2f;
    private const float RingSizeMin     = 0.90f;   // facteur par rapport au body
    private const float RingSizeMax     = 1.20f;   // réduit de 1.55 → 1.20 (−50 %)
    private const float RingAlphaMin    = 0.22f;
    private const float RingAlphaMax    = 0.70f;

    private const float HaloPulseSpeed  = 1.6f;
    private const float HaloSizeMin     = 1.00f;
    private const float HaloSizeMax     = 1.25f;   // réduit de 2.00 → 1.25 (−50 %)
    private const float HaloAlphaMin    = 0.06f;
    private const float HaloAlphaMax    = 0.22f;

    private static readonly Color ColGreen     = new Color(0.10f, 0.95f, 0.30f, 1f);
    private static readonly Color ColGreenHalo = new Color(0.10f, 1.00f, 0.35f, 1f);

    // ── Champs runtime ────────────────────────────────────────────────────────

    private SpriteRenderer _bodySr;
    private SpriteRenderer _ringSr;
    private Transform      _ringTr;
    private SpriteRenderer _haloSr;
    private Transform      _haloTr;
    private float          _ringPhase;
    private float          _haloPhase;

    // ── Init ──────────────────────────────────────────────────────────────────

    /// <summary>Initialise les couches de glow sur le body existant.</summary>
    public void Init(SpriteRenderer bodyRenderer)
    {
        _bodySr    = bodyRenderer;
        _bodySr.color = ColGreen;

        _ringPhase = 0f;
        _haloPhase = Mathf.PI * 0.5f;   // décalage pour que les deux ne soient pas en phase

        BuildRing();
        BuildHalo();
    }

    // ── Update ────────────────────────────────────────────────────────────────

    private void Update()
    {
        AnimateRing();
        AnimateHalo();
    }

    // ── Construction ──────────────────────────────────────────────────────────

    private void BuildRing()
    {
        var go = new GameObject("HoleRing");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale    = Vector3.one;

        var sr          = go.AddComponent<SpriteRenderer>();
        sr.sprite       = SpriteGenerator.CreateRing(64, 0.20f);
        sr.sortingOrder = _bodySr.sortingOrder - 1;

        _ringSr = sr;
        _ringTr = go.transform;
    }

    private void BuildHalo()
    {
        var go = new GameObject("HoleHalo");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale    = Vector3.one;

        var sr          = go.AddComponent<SpriteRenderer>();
        sr.sprite       = SpriteGenerator.CreateCircle(64);
        sr.sortingOrder = _bodySr.sortingOrder - 2;

        _haloSr = sr;
        _haloTr = go.transform;
    }

    // ── Animation ─────────────────────────────────────────────────────────────

    private void AnimateRing()
    {
        if (_ringSr == null) return;
        float t     = Mathf.Sin(Time.time * RingPulseSpeed + _ringPhase) * .5f + .5f;
        float size  = Mathf.Lerp(RingSizeMin, RingSizeMax, t);
        float alpha = Mathf.Lerp(RingAlphaMin, RingAlphaMax, t);
        _ringTr.localScale = new Vector3(size, size, 1f);
        _ringSr.color      = new Color(ColGreen.r, ColGreen.g, ColGreen.b, alpha);
    }

    private void AnimateHalo()
    {
        if (_haloSr == null) return;
        float t     = Mathf.Sin(Time.time * HaloPulseSpeed + _haloPhase) * .5f + .5f;
        float size  = Mathf.Lerp(HaloSizeMin, HaloSizeMax, t);
        float alpha = Mathf.Lerp(HaloAlphaMin, HaloAlphaMax, t);
        _haloTr.localScale = new Vector3(size, size, 1f);
        _haloSr.color      = new Color(ColGreenHalo.r, ColGreenHalo.g, ColGreenHalo.b, alpha);
    }
}
