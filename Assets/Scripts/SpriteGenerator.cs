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

    // ── Porte ─────────────────────────────────────────────────────────────────

    private static Sprite _doorSprite;

    /// <summary>
    /// Retourne un sprite de porte procédural (mis en cache).
    ///
    /// Dimensions : <paramref name="w"/> × <paramref name="h"/> px.
    /// Dessin : cadre extérieur + panneau intérieur + arc de voûte + poignée ronde.
    /// Tout en blanc/gris — la couleur est appliquée par le SpriteRenderer.
    /// </summary>
    public static Sprite CreateDoor(int w = 128, int h = 200)
    {
        if (_doorSprite != null) return _doorSprite;

        var   tex    = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var   pixels = new Color32[w * h];

        // ── Helpers locaux ────────────────────────────────────────────────────

        Color32 Transparent = new Color32(0, 0, 0, 0);
        Color32 Fill        = new Color32(255, 255, 255, 255);
        Color32 Mid         = new Color32(200, 200, 200, 255);
        Color32 Dark        = new Color32(140, 140, 140, 255);

        // Initialise tout à transparent
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Transparent;

        void SetPx(int x, int y, Color32 c)
        {
            if (x < 0 || x >= w || y < 0 || y >= h) return;
            pixels[y * w + x] = c;
        }

        void FillRect(int x0, int y0, int x1, int y1, Color32 c)
        {
            for (int yy = y0; yy <= y1; yy++)
            for (int xx = x0; xx <= x1; xx++)
                SetPx(xx, yy, c);
        }

        bool InEllipse(int x, int y, float cx, float cy, float rx, float ry)
            => ((x - cx) * (x - cx)) / (rx * rx) + ((y - cy) * (y - cy)) / (ry * ry) <= 1f;

        // ── Paramètres de mise en page ─────────────────────────────────────────

        int border   = Mathf.Max(3, w / 20);   // épaisseur du cadre
        int baseH    = h / 6;                   // socle rectangulaire sous l'arc
        int archTopY = h - 1;                   // sommet de la voûte

        // Demi-largeur et centre horizontal
        float cx  = w * 0.5f;
        float ry  = (h - baseH) * 0.52f;       // rayon vertical de l'ellipse
        float rx  = (w - border * 2) * 0.5f;   // rayon horizontal

        float arcCY = baseH + ry;              // centre vertical de l'ellipse de voûte

        // ── Corps principal : rectangle du bas ────────────────────────────────
        FillRect(border, 0, w - border - 1, baseH + (int)ry, Fill);

        // ── Voûte : demi-ellipse ──────────────────────────────────────────────
        for (int yy = (int)arcCY; yy < h; yy++)
        for (int xx = 0; xx < w; xx++)
        {
            if (InEllipse(xx, yy, cx, arcCY, rx + border, ry))
                SetPx(xx, yy, Fill);
        }

        // ── Panneau intérieur (zone sombre) ────────────────────────────────────
        int panelX0 = border * 3;
        int panelX1 = w - border * 3 - 1;
        int panelY0 = border * 2;
        int panelY1 = baseH + (int)(ry * 0.55f);

        FillRect(panelX0, panelY0, panelX1, panelY1, Mid);

        // Demi-ellipse intérieure (miroir plus petite)
        float rx2 = (panelX1 - panelX0) * 0.5f;
        float ry2 = ry * 0.52f;
        float cy2 = panelY1;

        for (int yy = (int)cy2; yy < h; yy++)
        for (int xx = panelX0; xx <= panelX1; xx++)
        {
            if (InEllipse(xx, yy, cx, cy2, rx2, ry2))
                SetPx(xx, yy, Mid);
        }

        // ── Fente verticale centrale (ligne de fermeture) ──────────────────────
        int midX = w / 2;
        for (int yy = panelY0; yy <= panelY1 + (int)(ry2 * 0.85f); yy++)
            SetPx(midX, yy, Dark);

        // ── Poignée ronde ─────────────────────────────────────────────────────
        int   knobX = w / 2 + (int)(rx * 0.25f);
        int   knobY = panelY0 + (panelY1 - panelY0) / 2;
        int   knobR = Mathf.Max(2, w / 18);

        for (int yy = knobY - knobR; yy <= knobY + knobR; yy++)
        for (int xx = knobX - knobR; xx <= knobX + knobR; xx++)
        {
            float dx = xx - knobX, dy = yy - knobY;
            if (dx * dx + dy * dy <= knobR * knobR)
                SetPx(xx, yy, Dark);
        }

        // ── Seuil (bande basse) ────────────────────────────────────────────────
        FillRect(0, 0, w - 1, border - 1, Fill);

        // ── Application ───────────────────────────────────────────────────────
        tex.SetPixels32(pixels);
        tex.Apply(false, true);

        _doorSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), w);
        return _doorSprite;
    }


    /// <summary>
    /// Retourne un sprite triangle d'avertissement ⚠ procédural (mis en cache).
    /// Blanc opaque — la couleur est appliquée par le SpriteRenderer.
    /// </summary>
    public static Sprite CreateWarningTriangle(int size = 64)
    {
        int cacheKey = size + 999_000; // distinct key
        if (RingCache.TryGetValue(cacheKey, out var cached) && cached != null)
            return cached;

        var tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var pixels = new Color[size * size];
        float cx   = size * 0.5f;
        float h    = size * 0.88f;
        float base2 = size * 0.90f;

        // Triangle vertices (bottom-left, bottom-right, top-center)
        var v0 = new Vector2(cx - base2 * 0.5f, size * 0.06f);
        var v1 = new Vector2(cx + base2 * 0.5f, size * 0.06f);
        var v2 = new Vector2(cx,                size * 0.06f + h);

        float border = size * 0.10f;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            var p = new Vector2(x + 0.5f, y + 0.5f);

            // Barycentric coords to determine if inside triangle
            float d0 = SignedEdge(p, v0, v1);
            float d1 = SignedEdge(p, v1, v2);
            float d2 = SignedEdge(p, v2, v0);

            bool inside = d0 >= 0 && d1 >= 0 && d2 >= 0;
            float sdf   = inside ? Mathf.Min(d0, Mathf.Min(d1, d2)) : -1f;
            float alpha = inside ? Mathf.Clamp01(sdf + 1f) : 0f;

            // Hollow out interior (keep only a thick border + exclamation mark)
            if (inside && sdf > border)
            {
                // Exclamation mark dot
                float dotDist = Vector2.Distance(p, new Vector2(cx, size * 0.15f));
                bool  isDot   = dotDist < size * 0.07f;

                // Exclamation mark stem
                bool isStem = Mathf.Abs(p.x - cx) < size * 0.06f
                           && p.y > size * 0.24f && p.y < size * 0.62f;

                alpha = (isDot || isStem) ? 1f : 0f;
            }

            pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
        }

        tex.SetPixels(pixels);
        tex.Apply(false, true);

        var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        RingCache[cacheKey] = sprite;
        return sprite;
    }

    private static float SignedEdge(Vector2 p, Vector2 a, Vector2 b)
        => (p.x - a.x) * (b.y - a.y) - (p.y - a.y) * (b.x - a.x);

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
