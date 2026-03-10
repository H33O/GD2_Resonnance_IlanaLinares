using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fabrique de sprites procéduraux avec mise en cache globale.
/// Chaque texture n'est créée qu'une seule fois sur le GPU, quel que soit
/// le nombre de GameObjects qui l'utilisent.
/// </summary>
public static class SpriteGenerator
{
    // ── Clé de cache polygon ──────────────────────────────────────────────────

    private readonly struct PolygonKey : System.IEquatable<PolygonKey>
    {
        public readonly int Sides;
        public readonly int Size;
        public PolygonKey(int sides, int size) { Sides = sides; Size = size; }
        public bool Equals(PolygonKey o) => Sides == o.Sides && Size == o.Size;
        public override bool Equals(object o) => o is PolygonKey k && Equals(k);
        public override int GetHashCode() => Sides * 1000 + Size;
    }

    // ── Caches ────────────────────────────────────────────────────────────────

    private static readonly Dictionary<int, Sprite>          CircleCache  = new();
    private static readonly Dictionary<PolygonKey, Sprite>   PolygonCache = new();
    private static readonly Dictionary<int, Sprite>          RingCache    = new();
    private static Sprite                                     _whiteSquare;
    private static readonly Dictionary<Color, Sprite>        ColoredSquareCache = new();
    private static Material                                   _additiveMat;

    // ── Cercle ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Retourne un sprite cercle blanc antialiasé de la taille donnée.
    /// La texture est créée une seule fois puis réutilisée (cache par taille).
    /// </summary>
    public static Sprite CreateCircle(int size = 64)
    {
        if (CircleCache.TryGetValue(size, out var cached) && cached != null)
            return cached;

        var   tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var   pixels = new Color[size * size];
        float center = size * 0.5f;
        float radius = center - 1f;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx    = x + 0.5f - center;
            float dy    = y + 0.5f - center;
            float dist  = Mathf.Sqrt(dx * dx + dy * dy);
            float alpha = Mathf.Clamp01(radius - dist + 1f);
            pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
        }

        tex.SetPixels(pixels);
        tex.Apply(false, true);

        var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        CircleCache[size] = sprite;
        return sprite;
    }

    /// <summary>Alias mis en cache (taille 64). Conservé pour compatibilité.</summary>
    public static Sprite Circle() => CreateCircle(64);

    // ── Polygone ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Retourne un sprite polygone convexe à N côtés (mis en cache par (sides, size)).
    /// Utilise 64×64 par défaut — suffisant pour des formes de décoration.
    /// </summary>
    public static Sprite CreatePolygon(int sides, int size = 64)
    {
        var key = new PolygonKey(sides, size);
        if (PolygonCache.TryGetValue(key, out var cached) && cached != null)
            return cached;

        var   tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var   pixels = new Color[size * size];
        float center = size * 0.5f;
        float radius = center - 1.5f;

        // Pré-calcule les normales de chaque arête (évite sqrt en boucle interne)
        float   angleStep = 2f * Mathf.PI / sides;
        float   offset    = -Mathf.PI * 0.5f;
        var     verts     = new Vector2[sides];
        var     normals   = new Vector2[sides];  // normales intérieures
        var     dists     = new float[sides];    // décalage de chaque arête

        for (int i = 0; i < sides; i++)
        {
            float a = offset + i * angleStep;
            verts[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
        }
        for (int i = 0; i < sides; i++)
        {
            Vector2 edge = verts[(i + 1) % sides] - verts[i];
            // Normale intérieure (vers le centre)
            normals[i] = new Vector2(-edge.y, edge.x).normalized;
            dists[i]   = Vector2.Dot(normals[i], verts[i]);
        }

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            var   p      = new Vector2(x + 0.5f - center, y + 0.5f - center);
            float minSdf = float.MaxValue;
            bool  inside = true;

            for (int i = 0; i < sides; i++)
            {
                float sdf = Vector2.Dot(normals[i], p) - dists[i];
                if (sdf > 0f) inside = false;
                if (sdf < minSdf) minSdf = sdf;  // < 0 quand inside
            }

            // minSdf < 0 → intérieur ; distance au bord = -minSdf
            float alpha = inside ? 1f : Mathf.Clamp01(1f + minSdf);
            pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
        }

        tex.SetPixels(pixels);
        tex.Apply(false, true);

        var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        PolygonCache[key] = sprite;
        return sprite;
    }

    // ── Anneau ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Retourne un sprite anneau (cercle creux) de la taille donnée.
    /// Mis en cache par taille.
    /// </summary>
    public static Sprite CreateRing(int size = 128, float thicknessRatio = 0.12f)
    {
        // Clé entière : encode taille + épaisseur (on utilise toujours la même dans l'intro)
        int cacheKey = size * 10000 + Mathf.RoundToInt(thicknessRatio * 1000);
        if (RingCache.TryGetValue(cacheKey, out var cached) && cached != null)
            return cached;

        var   tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var   pixels = new Color[size * size];
        float center = size * 0.5f;
        float outerR = center - 1f;
        float innerR = outerR * (1f - thicknessRatio * 8f);

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx   = x + 0.5f - center;
            float dy   = y + 0.5f - center;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            float outer = Mathf.Clamp01(outerR - dist + 1f);
            float inner = Mathf.Clamp01(dist - innerR + 1f);
            pixels[y * size + x] = new Color(1f, 1f, 1f, outer * inner);
        }

        tex.SetPixels(pixels);
        tex.Apply(false, true);

        var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        RingCache[cacheKey] = sprite;
        return sprite;
    }

    // ── Carré blanc ───────────────────────────────────────────────────────────

    /// <summary>Retourne un sprite carré blanc 4×4 (mis en cache).</summary>
    public static Sprite CreateWhiteSquare()
    {
        if (_whiteSquare != null) return _whiteSquare;

        var tex    = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        var pixels = new Color[16];
        for (int i = 0; i < 16; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply(false, true);

        _whiteSquare = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
        return _whiteSquare;
    }

    // ── Carré coloré ──────────────────────────────────────────────────────────

    /// <summary>Retourne un sprite carré de la couleur donnée (mis en cache par couleur).</summary>
    public static Sprite CreateColoredSquare(Color color)
    {
        if (ColoredSquareCache.TryGetValue(color, out var cached) && cached != null)
            return cached;

        var tex    = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        var pixels = new Color[16];
        for (int i = 0; i < 16; i++) pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply(false, true);

        var sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
        ColoredSquareCache[color] = sprite;
        return sprite;
    }

    // ── Matériau additif partagé ──────────────────────────────────────────────

    /// <summary>
    /// Retourne le matériau additif partagé utilisé par tous les LineRenderer d'éclairs.
    /// Un seul objet Material pour l'ensemble du projet.
    /// </summary>
    public static Material GetAdditiveMaterial()
    {
        if (_additiveMat != null) return _additiveMat;

        _additiveMat = new Material(Shader.Find("Sprites/Default"));
        _additiveMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        _additiveMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        return _additiveMat;
    }
}
