using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Halos de lumière mystiques qui pulsent lentement dans le fond du panneau GAME.
///
/// Chaque halo est un disque gradient blanc→transparent animé indépendamment :
/// - Il grossit et rapetisse selon une sinusoïde dont la période et la phase sont aléatoires
/// - Sa position dérive très lentement dans l'espace (parallaxe cosmique)
/// - Son opacité pulsante lui confère un aspect "vivant"
///
/// Tout est procédural — aucun asset externe requis.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class MenuSpaceHalos : MonoBehaviour
{
    // ── Paramètres des halos ──────────────────────────────────────────────────

    private const int   HaloCount       = 7;
    private const float SizeMin         = 280f;
    private const float SizeMax         = 820f;
    private const float AlphaMin        = 0.025f;
    private const float AlphaMax        = 0.10f;
    private const float PulsePeriodMin  = 3.5f;
    private const float PulsePeriodMax  = 9.0f;
    private const float DriftSpeed      = 6f;     // px/s, dérive spatiale lente

    // ── État runtime ──────────────────────────────────────────────────────────

    private struct HaloState
    {
        public RectTransform RT;
        public Image         Img;
        public float         BaseSize;
        public float         SizeAmp;
        public float         AlphaBase;
        public float         AlphaAmp;
        public float         Period;
        public float         Phase;
        public Vector2       DriftDir;
        public Vector2       Origin;
        public float         DriftRadius;
    }

    private HaloState[]   _halos;
    private RectTransform _rt;

    // ── Init ──────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
    }

    private void Start()
    {
        BuildHalos();
    }

    // ── Update ────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (_halos == null) return;
        float t = Time.time;

        for (int i = 0; i < _halos.Length; i++)
        {
            ref var h = ref _halos[i];
            if (h.RT == null) continue;

            // Pulsation de taille — sinus lent
            float sine   = Mathf.Sin((t / h.Period + h.Phase) * Mathf.PI * 2f);
            float size   = h.BaseSize + h.SizeAmp * sine;
            h.RT.sizeDelta = new Vector2(size, size);

            // Opacité respirante (déphasée légèrement)
            float sineA  = Mathf.Sin((t / h.Period + h.Phase + 0.25f) * Mathf.PI * 2f);
            float alpha  = h.AlphaBase + h.AlphaAmp * sineA;
            var c        = h.Img.color;
            c.a          = Mathf.Max(0f, alpha);
            h.Img.color  = c;

            // Dérive orbitale douce autour de l'origine
            float driftAngle = t * DriftSpeed / Mathf.Max(1f, h.DriftRadius) + h.Phase * 6.28f;
            var   driftPos   = h.Origin + new Vector2(
                Mathf.Cos(driftAngle) * h.DriftRadius,
                Mathf.Sin(driftAngle) * h.DriftRadius * 0.55f);
            h.RT.anchoredPosition = driftPos;
        }
    }

    // ── Construction ──────────────────────────────────────────────────────────

    private void BuildHalos()
    {
        _halos = new HaloState[HaloCount];

        // Sprite partagé entre tous les halos
        var sprite = CreateRadialSprite();

        float canvasW = 1080f;
        float canvasH = 1920f;

        for (int i = 0; i < HaloCount; i++)
        {
            var go  = new GameObject($"Halo_{i}");
            go.transform.SetParent(_rt, false);

            var img             = go.AddComponent<Image>();
            img.sprite          = sprite;
            img.color           = new Color(1f, 1f, 1f, AlphaMin);
            img.raycastTarget   = false;

            float baseSize      = Random.Range(SizeMin, SizeMax);
            float sizeAmp       = baseSize * Random.Range(0.12f, 0.28f);
            float alphaBase     = Random.Range(AlphaMin + 0.01f, (AlphaMin + AlphaMax) * 0.5f);
            float alphaAmp      = Random.Range(0.01f, AlphaMax - alphaBase);
            float period        = Random.Range(PulsePeriodMin, PulsePeriodMax);
            float phase         = Random.Range(0f, 1f);
            float driftRadius   = Random.Range(18f, 55f);

            // Position aléatoire répartie dans tout le panneau
            var origin = new Vector2(
                Random.Range(-canvasW * 0.42f, canvasW * 0.42f),
                Random.Range(-canvasH * 0.42f, canvasH * 0.42f));

            var rt              = img.rectTransform;
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = new Vector2(baseSize, baseSize);
            rt.anchoredPosition = origin;

            _halos[i] = new HaloState
            {
                RT          = rt,
                Img         = img,
                BaseSize    = baseSize,
                SizeAmp     = sizeAmp,
                AlphaBase   = alphaBase,
                AlphaAmp    = alphaAmp,
                Period      = period,
                Phase       = phase,
                DriftDir    = Random.insideUnitCircle.normalized,
                Origin      = origin,
                DriftRadius = driftRadius,
            };

            // Les halos sont derrière les boutons
            go.transform.SetSiblingIndex(i);
        }
    }

    // ── Sprite radial ─────────────────────────────────────────────────────────

    private static Sprite _cachedSprite;

    /// <summary>
    /// Génère un disque blanc → transparent (falloff quadratique)
    /// centré au milieu de la texture, taille 256×256.
    /// Mis en cache pour être partagé entre tous les halos.
    /// </summary>
    private static Sprite CreateRadialSprite()
    {
        if (_cachedSprite != null) return _cachedSprite;

        const int size = 256;
        var tex        = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode   = TextureWrapMode.Clamp;
        var pixels     = new Color[size * size];

        float cx = size * 0.5f;
        float cy = size * 0.5f;
        float r  = size * 0.5f;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx   = x + 0.5f - cx;
            float dy   = y + 0.5f - cy;
            float dist = Mathf.Sqrt(dx * dx + dy * dy) / r;

            // Falloff smooth : 1 au centre → 0 au bord
            float a = Mathf.Clamp01(1f - dist);
            a = a * a * (3f - 2f * a); // smoothstep

            pixels[y * size + x] = new Color(1f, 1f, 1f, a);
        }

        tex.SetPixels(pixels);
        tex.Apply(false, true);

        _cachedSprite = Sprite.Create(
            tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            size);

        return _cachedSprite;
    }
}
