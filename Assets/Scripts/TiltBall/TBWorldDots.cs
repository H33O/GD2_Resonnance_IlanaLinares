using UnityEngine;

/// <summary>
/// Particules de balles blanches rebondissantes en world-space pour TiltBall.
/// Reproduit le comportement de <see cref="MenuBouncingDots"/> mais utilise
/// des <see cref="SpriteRenderer"/> au lieu d'<see cref="UnityEngine.UI.Image"/>
/// pour être cohérent avec la caméra orthographique fixe de la scène TiltBall.
/// </summary>
public class TBWorldDots : MonoBehaviour
{
    // ── Paramètres ────────────────────────────────────────────────────────────

    private const int   DotCount     = 18;
    private const float DotSizeMin   = 0.08f;   // unités monde
    private const float DotSizeMax   = 0.20f;
    private const float SpeedMin     = 0.55f;
    private const float SpeedMax     = 1.80f;
    private const float AlphaMin     = 0.25f;
    private const float AlphaMax     = 0.60f;
    private const float PulseSpeed   = 1.1f;
    private const float PulseAmp     = 0.18f;

    private static readonly Color ColDot = Color.white;

    // ── État runtime ──────────────────────────────────────────────────────────

    private struct DotState
    {
        public Transform      Tr;
        public SpriteRenderer Sr;
        public Vector2        Vel;
        public float          BaseAlpha;
        public float          Phase;
    }

    private DotState[] _dots;
    private float      _halfW, _halfH;

    // ── Init ──────────────────────────────────────────────────────────────────

    /// <summary>Crée les particules. Appelé depuis <see cref="TBSceneSetup.BuildBackground"/>.</summary>
    public void Init(float halfW, float halfH)
    {
        _halfW = halfW;
        _halfH = halfH;
        _dots  = new DotState[DotCount];

        Sprite circle = SpriteGenerator.CreateCircle(32);

        for (int i = 0; i < DotCount; i++)
        {
            var go = new GameObject($"WDot_{i}");
            go.transform.SetParent(transform, false);

            var sr          = go.AddComponent<SpriteRenderer>();
            sr.sprite       = circle;
            sr.sortingOrder = -9;

            float size  = Random.Range(DotSizeMin, DotSizeMax);
            float alpha = Random.Range(AlphaMin, AlphaMax);
            sr.color = new Color(1f, 1f, 1f, alpha);
            go.transform.localScale = new Vector3(size, size, 1f);
            go.transform.position   = new Vector3(
                Random.Range(-halfW + size, halfW - size),
                Random.Range(-halfH + size, halfH - size), 0f);

            float angle = Random.Range(12f, 78f) * Mathf.Deg2Rad;
            int   sx    = Random.value > .5f ? 1 : -1;
            int   sy    = Random.value > .5f ? 1 : -1;
            float speed = Random.Range(SpeedMin, SpeedMax);

            _dots[i] = new DotState
            {
                Tr        = go.transform,
                Sr        = sr,
                Vel       = new Vector2(Mathf.Cos(angle) * sx, Mathf.Sin(angle) * sy) * speed,
                BaseAlpha = alpha,
                Phase     = Random.Range(0f, Mathf.PI * 2f),
            };
        }
    }

    // ── Update ────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (_dots == null) return;
        float t = Time.time;

        for (int i = 0; i < _dots.Length; i++)
        {
            ref var d = ref _dots[i];
            if (d.Tr == null) continue;

            float r   = d.Tr.localScale.x * .5f;
            var   pos = (Vector2)d.Tr.position + d.Vel * Time.deltaTime;

            if (pos.x - r < -_halfW) { pos.x = -_halfW + r; d.Vel.x =  Mathf.Abs(d.Vel.x); }
            else if (pos.x + r > _halfW) { pos.x =  _halfW - r; d.Vel.x = -Mathf.Abs(d.Vel.x); }

            if (pos.y - r < -_halfH) { pos.y = -_halfH + r; d.Vel.y =  Mathf.Abs(d.Vel.y); }
            else if (pos.y + r > _halfH) { pos.y =  _halfH - r; d.Vel.y = -Mathf.Abs(d.Vel.y); }

            d.Tr.position = new Vector3(pos.x, pos.y, 0f);

            float pulse = Mathf.Sin(t * PulseSpeed + d.Phase) * PulseAmp;
            d.Sr.color   = new Color(1f, 1f, 1f, Mathf.Clamp01(d.BaseAlpha + pulse));
        }
    }
}
