using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Grille minimaliste avec petits points blancs rebondissant sur les murs du canvas.
/// Remplace les lucioles dans la scène Menu.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class MenuBouncingDots : MonoBehaviour
{
    // ── Paramètres grille ─────────────────────────────────────────────────────

    private const int   DotCount      = 18;
    private const float DotMinSize    = 5f;
    private const float DotMaxSize    = 10f;
    private const float SpeedMin      = 60f;
    private const float SpeedMax      = 160f;

    // ── Palette ───────────────────────────────────────────────────────────────

    private static readonly Color DotColor    = new Color(1f, 1f, 1f, 0.55f);
    private static readonly Color GridColor   = new Color(1f, 1f, 1f, 0.04f);

    // ── Constantes grille ─────────────────────────────────────────────────────

    private const int   GridCols      = 6;
    private const int   GridRows      = 10;
    private const float GridLineWidth = 1f;

    // ── État runtime ──────────────────────────────────────────────────────────

    private RectTransform _canvasRT;

    private struct DotState
    {
        public RectTransform RT;
        public Vector2       Velocity;
    }

    private DotState[] _dots;

    // ── Init ──────────────────────────────────────────────────────────────────

    /// <summary>Construit la grille et les points dans le <paramref name="canvasRT"/> fourni.</summary>
    public void Init(RectTransform canvasRT)
    {
        _canvasRT = canvasRT;
        BuildGrid(canvasRT);
        BuildDots(canvasRT);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (_dots == null || _canvasRT == null) return;

        float halfW = _canvasRT.rect.width  * 0.5f;
        float halfH = _canvasRT.rect.height * 0.5f;

        for (int i = 0; i < _dots.Length; i++)
        {
            ref var dot = ref _dots[i];
            if (dot.RT == null) continue;

            float radius = dot.RT.sizeDelta.x * 0.5f;
            var   pos    = dot.RT.anchoredPosition + dot.Velocity * Time.deltaTime;

            // Rebond sur les bords gauche/droite
            if (pos.x - radius < -halfW)
            {
                pos.x       = -halfW + radius;
                dot.Velocity.x = Mathf.Abs(dot.Velocity.x);
            }
            else if (pos.x + radius > halfW)
            {
                pos.x       = halfW - radius;
                dot.Velocity.x = -Mathf.Abs(dot.Velocity.x);
            }

            // Rebond sur les bords haut/bas
            if (pos.y - radius < -halfH)
            {
                pos.y       = -halfH + radius;
                dot.Velocity.y = Mathf.Abs(dot.Velocity.y);
            }
            else if (pos.y + radius > halfH)
            {
                pos.y       = halfH - radius;
                dot.Velocity.y = -Mathf.Abs(dot.Velocity.y);
            }

            dot.RT.anchoredPosition = pos;
        }
    }

    // ── Construction grille ───────────────────────────────────────────────────

    private static void BuildGrid(RectTransform parent)
    {
        var root = new GameObject("Grid");
        root.transform.SetParent(parent, false);
        var rootRT       = root.AddComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = rootRT.offsetMax = Vector2.zero;

        // Lignes verticales
        for (int c = 1; c < GridCols; c++)
        {
            float xNorm = (float)c / GridCols;
            var line    = MakeGridLine(root.transform, vertical: true);
            var rt      = line.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(xNorm, 0f);
            rt.anchorMax = new Vector2(xNorm, 1f);
            rt.sizeDelta = new Vector2(GridLineWidth, 0f);
            rt.anchoredPosition = Vector2.zero;
        }

        // Lignes horizontales
        for (int r = 1; r < GridRows; r++)
        {
            float yNorm = (float)r / GridRows;
            var line    = MakeGridLine(root.transform, vertical: false);
            var rt      = line.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, yNorm);
            rt.anchorMax = new Vector2(1f, yNorm);
            rt.sizeDelta = new Vector2(0f, GridLineWidth);
            rt.anchoredPosition = Vector2.zero;
        }
    }

    private static GameObject MakeGridLine(Transform parent, bool vertical)
    {
        var go  = new GameObject(vertical ? "VLine" : "HLine");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite       = SpriteGenerator.CreateWhiteSquare();
        img.color        = GridColor;
        img.raycastTarget = false;
        return go;
    }

    // ── Construction points ───────────────────────────────────────────────────

    private void BuildDots(RectTransform parent)
    {
        _dots = new DotState[DotCount];

        float halfW = parent.rect.width  * 0.5f;
        float halfH = parent.rect.height * 0.5f;

        for (int i = 0; i < DotCount; i++)
        {
            var go  = new GameObject($"Dot_{i}");
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.sprite       = SpriteGenerator.CreateCircle(32);
            img.raycastTarget = false;

            float size = Random.Range(DotMinSize, DotMaxSize);

            // Opacité légèrement variable pour profondeur visuelle
            float alpha = Random.Range(0.30f, 0.65f);
            img.color = new Color(DotColor.r, DotColor.g, DotColor.b, alpha);

            var rt           = img.rectTransform;
            rt.anchorMin     = new Vector2(0.5f, 0.5f);
            rt.anchorMax     = new Vector2(0.5f, 0.5f);
            rt.pivot         = new Vector2(0.5f, 0.5f);
            rt.sizeDelta     = new Vector2(size, size);
            rt.anchoredPosition = new Vector2(
                Random.Range(-halfW + size, halfW - size),
                Random.Range(-halfH + size, halfH - size));

            // Vélocité aléatoire avec angle non-axial pour éviter les trajectoires horizontales plates
            float angle   = Random.Range(15f, 75f) * Mathf.Deg2Rad;
            int   signX   = Random.value > 0.5f ? 1 : -1;
            int   signY   = Random.value > 0.5f ? 1 : -1;
            float speed   = Random.Range(SpeedMin, SpeedMax);
            var   velocity = new Vector2(Mathf.Cos(angle) * signX, Mathf.Sin(angle) * signY) * speed;

            _dots[i] = new DotState { RT = rt, Velocity = velocity };
        }
    }
}
