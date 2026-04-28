using UnityEngine;

/// <summary>
/// Effets visuels d'un ennemi TiltBall :
/// - Corps rouge vif
/// - Halo rouge pulsant (ring autour du corps)
/// - Traînée d'échos (copies fantômes qui s'estompent derrière lui)
/// Attache ce composant sur le même GameObject que <see cref="TBEnemyController"/>.
/// </summary>
public class TBEnemyVisuals : MonoBehaviour
{
    // ── Paramètres ────────────────────────────────────────────────────────────

    private const int   EchoCount      = 5;
    private const float EchoSpacing    = 0.07f;   // secondes entre chaque snapshot
    private const float EchoAlphaMax   = 0.42f;
    private const float HaloPulseSpeed = 3.5f;
    private const float HaloAlphaMin   = 0.18f;
    private const float HaloAlphaMax   = 0.55f;
    private const float HaloSizeMin    = 1.08f;
    private const float HaloSizeMax    = 1.60f;

    private static readonly Color ColEnemy = new Color(1f, 0.07f, 0.07f, 1f);
    private static readonly Color ColHalo  = new Color(1f, 0.10f, 0.10f, 1f);
    private static readonly Color ColEcho  = new Color(1f, 0.12f, 0.12f, 1f);

    // ── État runtime ──────────────────────────────────────────────────────────

    private SpriteRenderer _bodySr;
    private SpriteRenderer _haloSr;
    private Transform      _haloTr;
    private float          _haloPhase;

    private struct EchoEntry
    {
        public Transform      Tr;
        public SpriteRenderer Sr;
    }

    private EchoEntry[] _echos;
    private Vector2[]   _echoPos;   // ring buffer de positions
    private int         _echoHead;
    private float       _echoTimer;

    // ── Init ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// À appeler depuis <see cref="TBEnemyController"/>.Start() après que le SpriteRenderer est configuré.
    /// </summary>
    public void Init(SpriteRenderer bodyRenderer)
    {
        _bodySr       = bodyRenderer;
        _bodySr.color = ColEnemy;
        _haloPhase    = Random.Range(0f, Mathf.PI * 2f);

        BuildHalo();
        BuildEchos(bodyRenderer.sprite);
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Update()
    {
        AnimateHalo();
        TickEchos();
    }

    private void OnDestroy()
    {
        if (_echos == null) return;
        foreach (var e in _echos)
            if (e.Tr != null) Destroy(e.Tr.gameObject);
    }

    // ── Halo ──────────────────────────────────────────────────────────────────

    private void BuildHalo()
    {
        var go = new GameObject("EnemyHalo");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale    = Vector3.one;

        var sr          = go.AddComponent<SpriteRenderer>();
        sr.sprite       = SpriteGenerator.CreateRing(64, 0.22f);
        sr.sortingOrder = _bodySr.sortingOrder - 1;

        _haloSr = sr;
        _haloTr = go.transform;
    }

    private void AnimateHalo()
    {
        if (_haloSr == null) return;

        float t     = Mathf.Sin(Time.time * HaloPulseSpeed + _haloPhase) * 0.5f + 0.5f;
        float size  = Mathf.Lerp(HaloSizeMin, HaloSizeMax, t);
        float alpha = Mathf.Lerp(HaloAlphaMin, HaloAlphaMax, t);

        _haloTr.localScale = new Vector3(size, size, 1f);
        _haloSr.color      = new Color(ColHalo.r, ColHalo.g, ColHalo.b, alpha);
    }

    // ── Échos ─────────────────────────────────────────────────────────────────

    private void BuildEchos(Sprite sprite)
    {
        _echos    = new EchoEntry[EchoCount];
        _echoPos  = new Vector2[EchoCount];
        _echoHead = 0;

        Vector3   worldPos   = transform.position;
        Vector3   worldScale = transform.lossyScale;
        int       sortOrder  = _bodySr.sortingOrder - 2;
        Transform parentTr   = transform.parent != null ? transform.parent : transform;

        Sprite spr = sprite != null ? sprite : SpriteGenerator.CreateCircle(128);

        for (int i = 0; i < EchoCount; i++)
        {
            var go = new GameObject($"EnemyEcho_{i}");
            go.transform.SetParent(parentTr, false);
            go.transform.position   = worldPos;
            go.transform.localScale = worldScale;

            var sr          = go.AddComponent<SpriteRenderer>();
            sr.sprite       = spr;
            sr.sortingOrder = sortOrder;
            sr.color        = new Color(ColEcho.r, ColEcho.g, ColEcho.b, 0f);

            _echos[i]   = new EchoEntry { Tr = go.transform, Sr = sr };
            _echoPos[i] = worldPos;
        }
    }

    private void TickEchos()
    {
        if (_echos == null) return;

        _echoTimer += Time.deltaTime;
        if (_echoTimer >= EchoSpacing)
        {
            _echoTimer = 0f;
            _echoPos[_echoHead] = transform.position;
            _echoHead = (_echoHead + 1) % EchoCount;
        }

        for (int i = 0; i < EchoCount; i++)
        {
            int   slot  = (_echoHead - 1 - i + EchoCount) % EchoCount;
            float alpha = EchoAlphaMax * (1f - (float)i / EchoCount);

            ref var e = ref _echos[i];
            if (e.Tr == null) continue;

            e.Tr.position = _echoPos[slot];
            e.Sr.color    = new Color(ColEcho.r, ColEcho.g, ColEcho.b, alpha);
        }
    }
}
