using UnityEngine;

/// <summary>
/// Affiche la grille de jeu (lignes de séparation de cellules, murs fixes, sortie).
/// Construit les visuels au démarrage de façon procédurale.
/// </summary>
public class TPMGridRenderer : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Settings")]
    public TPMSettings settings;

    // ── Couleurs ──────────────────────────────────────────────────────────────

    private static readonly Color GridLineColor = new Color(1f, 1f, 1f, 0.06f);
    private static readonly Color WallColor     = new Color(0.50f, 0.52f, 0.60f, 1.00f);
    private static readonly Color WallEdgeColor = new Color(0.35f, 0.37f, 0.45f, 1.00f);
    private static readonly Color ExitColor     = new Color(1.00f, 0.85f, 0.10f, 1.00f);
    private static readonly Color ExitGlowColor = new Color(1.00f, 0.85f, 0.10f, 0.20f);

    // ── Sortie animée ─────────────────────────────────────────────────────────

    private SpriteRenderer exitGlowSR;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        // Init() est appelé explicitement par TPMSceneSetup après que TPMGrid
        // a été initialisé. Si ce Start() est appelé avant, on l'ignore.
    }

    /// <summary>
    /// Construction explicite des visuels. Doit être appelé après TPMGrid.Init().
    /// </summary>
    public void Init()
    {
        if (TPMGrid.Instance == null) return;
        DrawGridLines();
        DrawWalls();
        DrawExit();
    }

    private void Update()
    {
        if (exitGlowSR == null) return;
        float a = 0.15f + 0.12f * Mathf.Sin(Time.time * settings.exitPulseSpeed);
        exitGlowSR.color = new Color(ExitGlowColor.r, ExitGlowColor.g, ExitGlowColor.b, a);
    }

    // ── Construction ──────────────────────────────────────────────────────────

    private void DrawGridLines()
    {
        int   w  = settings.gridWidth;
        int   h  = settings.gridHeight;
        float cs = settings.cellSize;

        float totalW  = (w - 1) * cs;
        float totalH  = (h - 1) * cs;
        float offsetX = -totalW * 0.5f;
        float offsetY = -totalH * 0.5f;

        var mat = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));

        // Lignes verticales
        for (int x = 0; x <= w - 1; x++)
        {
            var go = new GameObject($"VLine_{x}");
            go.transform.SetParent(transform, false);
            var lr               = go.AddComponent<LineRenderer>();
            lr.positionCount     = 2;
            lr.useWorldSpace     = true;
            lr.startWidth        = 0.012f;
            lr.endWidth          = 0.012f;
            lr.startColor        = GridLineColor;
            lr.endColor          = GridLineColor;
            lr.sharedMaterial    = mat;
            lr.sortingOrder      = -2;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows    = false;

            float fx = offsetX + x * cs;
            lr.SetPosition(0, new Vector3(fx, offsetY - cs * 0.5f, 0f));
            lr.SetPosition(1, new Vector3(fx, offsetY + totalH + cs * 0.5f, 0f));
        }

        // Lignes horizontales
        for (int y = 0; y <= h - 1; y++)
        {
            var go = new GameObject($"HLine_{y}");
            go.transform.SetParent(transform, false);
            var lr               = go.AddComponent<LineRenderer>();
            lr.positionCount     = 2;
            lr.useWorldSpace     = true;
            lr.startWidth        = 0.012f;
            lr.endWidth          = 0.012f;
            lr.startColor        = GridLineColor;
            lr.endColor          = GridLineColor;
            lr.sharedMaterial    = mat;
            lr.sortingOrder      = -2;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows    = false;

            float fy = offsetY + y * cs;
            lr.SetPosition(0, new Vector3(offsetX - cs * 0.5f, fy, 0f));
            lr.SetPosition(1, new Vector3(offsetX + totalW + cs * 0.5f, fy, 0f));
        }
    }

    private void DrawWalls()
    {
        int w  = settings.gridWidth;
        int h  = settings.gridHeight;
        float cs = settings.cellSize * 0.90f;

        for (int x = 0; x < w; x++)
        for (int y = 0; y < h; y++)
        {
            if (TPMGrid.Instance.GetCell(x, y) != TPMGrid.CellType.Wall) continue;

            Vector3 pos = TPMGrid.Instance.CellToWorld(x, y);

            // Bord
            var edgeGO  = new GameObject($"WallEdge_{x}_{y}");
            edgeGO.transform.SetParent(transform, false);
            edgeGO.transform.position   = pos;
            edgeGO.transform.localScale = Vector3.one * (cs * 1.04f);
            var edgeSR  = edgeGO.AddComponent<SpriteRenderer>();
            edgeSR.sprite = SpriteGenerator.CreateColoredSquare(WallEdgeColor);
            edgeSR.sortingOrder = 0;

            // Corps
            var wallGO  = new GameObject($"Wall_{x}_{y}");
            wallGO.transform.SetParent(transform, false);
            wallGO.transform.position   = pos;
            wallGO.transform.localScale = Vector3.one * cs;
            var wallSR  = wallGO.AddComponent<SpriteRenderer>();
            wallSR.sprite = SpriteGenerator.CreateColoredSquare(WallColor);
            wallSR.sortingOrder = 1;
        }
    }

    private void DrawExit()
    {
        Vector2Int ec  = TPMGrid.Instance.ExitCell;
        Vector3    pos = TPMGrid.Instance.CellToWorld(ec.x, ec.y);
        float      cs  = settings.cellSize * 0.82f;

        // Corps de la sortie
        var exitGO  = new GameObject("Exit");
        exitGO.transform.SetParent(transform, false);
        exitGO.transform.position   = pos;
        exitGO.transform.localScale = Vector3.one * cs;
        var exitSR  = exitGO.AddComponent<SpriteRenderer>();
        exitSR.sprite = SpriteGenerator.CreatePolygon(8, 64);
        exitSR.color  = ExitColor;
        exitSR.sortingOrder = 2;

        // Halo pulsant
        var glowGO  = new GameObject("ExitGlow");
        glowGO.transform.SetParent(transform, false);
        glowGO.transform.position   = pos;
        glowGO.transform.localScale = Vector3.one * (cs * 1.5f);
        exitGlowSR  = glowGO.AddComponent<SpriteRenderer>();
        exitGlowSR.sprite = SpriteGenerator.CreateCircle(64);
        exitGlowSR.color  = ExitGlowColor;
        exitGlowSR.sortingOrder = 1;

        // Label "EXIT"
        var labelGO  = new GameObject("ExitLabel");
        labelGO.transform.SetParent(exitGO.transform, false);
        labelGO.transform.localScale    = Vector3.one * (1f / cs) * 0.35f;
        labelGO.transform.localPosition = Vector3.zero;
        var tmp      = labelGO.AddComponent<TextMesh>();
        tmp.text     = "EXIT";
        tmp.fontSize = 22;
        tmp.color    = Color.black;
        tmp.anchor   = TextAnchor.MiddleCenter;
        tmp.alignment = TextAlignment.Center;
        var mf       = labelGO.GetComponent<MeshRenderer>();
        if (mf != null) mf.sortingOrder = 3;
    }
}
