using UnityEngine;

/// <summary>
/// Petit halo circulaire statique qui pulse doucement en opacité et en échelle.
/// Placé aléatoirement dans les limites de la caméra orthographique, il reste fixe
/// et ne se déplace pas — l'animation est uniquement locale (breathe effect).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class GAWHalo : MonoBehaviour
{
    // ── Constantes ────────────────────────────────────────────────────────────

    private const float RadiusMin      = 0.08f;
    private const float RadiusMax      = 0.22f;
    private const float AlphaMin       = 0.04f;
    private const float AlphaMax       = 0.18f;
    private const float PulseSpeedMin  = 0.4f;
    private const float PulseSpeedMax  = 1.1f;
    private const float ScalePulse     = 0.18f;   // amplitude de pulsation en scale (±)
    private const int   SortingOrder   = -9;

    // ── État ──────────────────────────────────────────────────────────────────

    private SpriteRenderer _sr;
    private float          _baseRadius;
    private float          _baseAlpha;
    private float          _speed;
    private float          _phaseOffset;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        _sr = GetComponent<SpriteRenderer>();

        _baseRadius  = Random.Range(RadiusMin, RadiusMax);
        _baseAlpha   = Random.Range(AlphaMin, AlphaMax);
        _speed       = Random.Range(PulseSpeedMin, PulseSpeedMax);
        _phaseOffset = Random.Range(0f, Mathf.PI * 2f);

        _sr.sprite       = CreateHaloSprite(64);
        _sr.color        = new Color(1f, 1f, 1f, _baseAlpha);
        _sr.sortingOrder = SortingOrder;

        transform.localScale = Vector3.one * (_baseRadius * 2f);
        transform.position   = RandomPosition();
    }

    private void Update()
    {
        float t      = Time.time * _speed + _phaseOffset;
        float pulse  = Mathf.Sin(t);                             // -1 → 1

        // Pulsation scale
        float scale  = _baseRadius * 2f * (1f + ScalePulse * pulse);
        transform.localScale = Vector3.one * scale;

        // Pulsation alpha — oscille entre AlphaMin et baseAlpha
        float alpha  = _baseAlpha * (0.55f + 0.45f * (pulse * 0.5f + 0.5f));
        _sr.color    = new Color(1f, 1f, 1f, alpha);
    }

    // ── Utilitaires ───────────────────────────────────────────────────────────

    /// <summary>
    /// Génère un sprite cercle avec dégradé radial très doux — centre lumineux, bords transparents.
    /// </summary>
    private static Sprite CreateHaloSprite(int size)
    {
        var tex        = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var pixels     = new Color[size * size];
        float center   = size * 0.5f;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx   = (x + 0.5f - center) / center;
            float dy   = (y + 0.5f - center) / center;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            // Courbe gaussian-like : décroissance rapide vers les bords
            float a    = Mathf.Clamp01(1f - dist);
            a          = a * a * a;
            pixels[y * size + x] = new Color(1f, 1f, 1f, a);
        }

        tex.SetPixels(pixels);
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Vector2 RandomPosition()
    {
        var cam = Camera.main;
        if (cam == null) return Vector2.zero;
        float h = cam.orthographicSize;
        float w = h * cam.aspect;
        return new Vector2(Random.Range(-w, w), Random.Range(-h, h));
    }
}
