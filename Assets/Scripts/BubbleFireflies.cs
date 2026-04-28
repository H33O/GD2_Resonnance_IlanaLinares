using UnityEngine;

/// <summary>
/// Lucioles blanches procédurales en world-space pour la scène Minijeu-Bulles.
/// Reproduit l'esthétique de <see cref="MenuBouncingDots"/> mais utilise des
/// SpriteRenderer afin d'être cohérent avec le pipeline world-space de la scène.
/// </summary>
public class BubbleFireflies : MonoBehaviour
{
    // ── Paramètres ────────────────────────────────────────────────────────────

    private const int   FireflyCount  = 18;
    private const float SizeMin       = 0.05f;
    private const float SizeMax       = 0.14f;
    private const float SpeedMin      = 0.5f;
    private const float SpeedMax      = 1.6f;
    private const float AlphaMin      = 0.18f;
    private const float AlphaMax      = 0.60f;
    private const float PulseSpeed    = 1.2f;   // vitesse de clignotement (rad/s)
    private const float PulseStrength = 0.20f;  // amplitude alpha +/-

    // ── État runtime ──────────────────────────────────────────────────────────

    private struct FireflyState
    {
        public Transform   Tr;
        public SpriteRenderer Sr;
        public Vector2     Velocity;
        public float       BaseAlpha;
        public float       PhaseOffset;
    }

    private FireflyState[] _fireflies;
    private float          _halfW, _halfH;
    private Sprite         _circle;

    // ── Init ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Crée les lucioles dans le world-space de la caméra principale.
    /// À appeler une seule fois depuis <see cref="BubbleSceneSetup"/>.
    /// </summary>
    public void Init()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        _halfH = cam.orthographicSize;
        _halfW = _halfH * cam.aspect;

        _circle    = SpriteGenerator.CreateCircle(32);
        _fireflies = new FireflyState[FireflyCount];

        for (int i = 0; i < FireflyCount; i++)
        {
            var go = new GameObject($"Firefly_{i}");
            go.transform.SetParent(transform, false);

            var sr          = go.AddComponent<SpriteRenderer>();
            sr.sprite       = _circle;
            sr.sortingOrder = -10;

            float size      = Random.Range(SizeMin, SizeMax);
            go.transform.localScale = new Vector3(size, size, 1f);

            float alpha = Random.Range(AlphaMin, AlphaMax);
            sr.color = new Color(1f, 1f, 1f, alpha);

            go.transform.position = new Vector3(
                Random.Range(-_halfW + size, _halfW - size),
                Random.Range(-_halfH + size, _halfH - size),
                0f);

            // Vélocité non-axiale
            float angle = Random.Range(15f, 75f) * Mathf.Deg2Rad;
            int   sx    = Random.value > 0.5f ? 1 : -1;
            int   sy    = Random.value > 0.5f ? 1 : -1;
            float speed = Random.Range(SpeedMin, SpeedMax);
            var   vel   = new Vector2(Mathf.Cos(angle) * sx, Mathf.Sin(angle) * sy) * speed;

            _fireflies[i] = new FireflyState
            {
                Tr          = go.transform,
                Sr          = sr,
                Velocity    = vel,
                BaseAlpha   = alpha,
                PhaseOffset = Random.Range(0f, Mathf.PI * 2f),
            };
        }
    }

    // ── Update ────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (_fireflies == null) return;

        float t = Time.time;

        for (int i = 0; i < _fireflies.Length; i++)
        {
            ref var f = ref _fireflies[i];
            if (f.Tr == null) continue;

            float size = f.Tr.localScale.x * 0.5f;
            var   pos  = (Vector2)f.Tr.position + f.Velocity * Time.deltaTime;

            // Rebond sur les bords caméra
            if (pos.x - size < -_halfW) { pos.x = -_halfW + size; f.Velocity.x =  Mathf.Abs(f.Velocity.x); }
            else if (pos.x + size > _halfW) { pos.x =  _halfW - size; f.Velocity.x = -Mathf.Abs(f.Velocity.x); }

            if (pos.y - size < -_halfH) { pos.y = -_halfH + size; f.Velocity.y =  Mathf.Abs(f.Velocity.y); }
            else if (pos.y + size > _halfH) { pos.y =  _halfH - size; f.Velocity.y = -Mathf.Abs(f.Velocity.y); }

            f.Tr.position = new Vector3(pos.x, pos.y, 0f);

            // Pulsation alpha
            float pulse = Mathf.Sin(t * PulseSpeed + f.PhaseOffset) * PulseStrength;
            float a     = Mathf.Clamp01(f.BaseAlpha + pulse);
            f.Sr.color  = new Color(1f, 1f, 1f, a);
        }
    }
}
