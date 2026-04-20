using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gère le cycle complet des pièces Tetris :
/// spawn intelligent, chute automatique, verrouillage, effacement de lignes, ghost piece.
///
/// INPUTS (clavier PC) :
///   ← → flèches    → déplacer bloc horizontalement
///   ↑ flèche       → rotation
///   ↓ flèche       → soft drop (chute rapide)
///   ↓ maintenu     → chute instantanée (hard drop)
///
/// Note : ZQSD est réservé au joueur.
/// </summary>
public class TPMTetrisSpawner : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static TPMTetrisSpawner Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Settings")]
    public TPMSettings settings;

    // ── Événements ────────────────────────────────────────────────────────────

    /// <summary>Déclenché à chaque nouveau spawn. Payload = index de la pièce suivante.</summary>
    public static event System.Action<int> OnNextPieceChanged;

    // ── État ──────────────────────────────────────────────────────────────────

    private TPMTetrisPiece    activePiece;
    private int               nextShapeIndex;

    private readonly List<GameObject> activeCellGOs = new();
    private readonly List<GameObject> ghostCellGOs  = new();
    private readonly Dictionary<Vector2Int, GameObject> lockedGOs = new();
    private readonly Dictionary<Vector2Int, Color>      lockedColors = new();

    private float fallTimer;
    private float moveHTimer;
    private bool  isSoftDropping;
    private bool  hardDropThisFrame;

    private const float MoveHInitialDelay = 0.20f;
    private const float MoveHRepeatDelay  = 0.10f;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        nextShapeIndex = PickSmartShape();
        SpawnNext();
    }

    private void Update()
    {
        if (activePiece == null) return;
        if (TPMGameManager.Instance?.State != TPMGameManager.GameState.Playing) return;

        HandleHorizontalInput();
        HandleRotationInput();
        HandleFallInput();
        FallTick();

        RefreshActiveVisuals();
        RefreshGhostVisuals();
    }

    // ── Spawn intelligent ─────────────────────────────────────────────────────

    /// <summary>
    /// Analyse la grille et choisit la forme la plus adaptée :
    /// — zones étroites / couloirs   → favorise I (index 0)
    /// — zones irrégulières          → favorise T (2) ou L/J (5/6)
    /// — sinon aléatoire pondéré
    /// </summary>
    private int PickSmartShape()
    {
        if (TPMGrid.Instance == null) return Random.Range(0, TPMTetrisPiece.Shapes.Length);

        int w = settings.gridWidth;
        int h = settings.gridHeight;

        // Mesure la largeur maximale consécutive de cellules vides sur les 4 rangées du haut
        int maxFreeRun = 0;
        for (int y = h - 4; y < h - 1; y++)
        {
            int run = 0;
            for (int x = 1; x < w - 1; x++)
            {
                if (TPMGrid.Instance.IsEmpty(x, y)) run++;
                else run = 0;
                maxFreeRun = Mathf.Max(maxFreeRun, run);
            }
        }

        // Zone étroite (run ≤ 2) → pièce I longue
        if (maxFreeRun <= 2) return 0; // I

        // Zone irrégulière (run = 3) → T, L ou J
        if (maxFreeRun <= 3)
        {
            int[] irregular = { 2, 5, 6 }; // T, J, L
            return irregular[Random.Range(0, irregular.Length)];
        }

        // Zone ouverte → aléatoire
        return Random.Range(0, TPMTetrisPiece.Shapes.Length);
    }

    private void SpawnNext()
    {
        int shapeIdx   = nextShapeIndex;
        nextShapeIndex = PickSmartShape();
        OnNextPieceChanged?.Invoke(nextShapeIndex);

        int spawnX = settings.gridWidth / 2;
        int spawnY = settings.gridHeight - 2;

        activePiece = new TPMTetrisPiece(shapeIdx, new Vector2Int(spawnX, spawnY));
        fallTimer   = 0f;

        foreach (var pos in activePiece.GetCells())
        {
            if (!TPMGrid.Instance.IsFreeFalling(pos.x, pos.y))
            {
                // Grille pleine → défaite
                TPMGameManager.Instance?.NotifyCaught();
                return;
            }
        }
    }

    // ── Chute ─────────────────────────────────────────────────────────────────

    private void FallTick()
    {
        float interval = isSoftDropping
            ? settings.tetrisSoftDropInterval
            : settings.tetrisFallInterval;

        fallTimer += Time.deltaTime;
        if (fallTimer < interval) return;
        fallTimer = 0f;

        if (!activePiece.TryMove(Vector2Int.down, TPMGrid.Instance))
            LockPiece();
    }

    // ── Inputs : flèches uniquement ───────────────────────────────────────────

    private void HandleHorizontalInput()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return;

        bool left  = kb.leftArrowKey.isPressed;
        bool right = kb.rightArrowKey.isPressed;

        Vector2Int dir = Vector2Int.zero;
        if (left  && !right) dir = Vector2Int.left;
        if (right && !left)  dir = Vector2Int.right;

        if (dir == Vector2Int.zero) { moveHTimer = 0f; return; }

        bool firstPress = (left  && kb.leftArrowKey.wasPressedThisFrame)
                       || (right && kb.rightArrowKey.wasPressedThisFrame);

        if (firstPress)
        {
            activePiece.TryMove(dir, TPMGrid.Instance);
            moveHTimer = 0f;
        }
        else
        {
            moveHTimer += Time.deltaTime;
            if (moveHTimer >= MoveHInitialDelay + MoveHRepeatDelay)
            {
                activePiece.TryMove(dir, TPMGrid.Instance);
                moveHTimer = MoveHInitialDelay;
            }
        }
    }

    private void HandleRotationInput()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return;

        if (kb.upArrowKey.wasPressedThisFrame)
            activePiece.TryRotate(TPMGrid.Instance);
    }

    private void HandleFallInput()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return;
        isSoftDropping = kb.downArrowKey.isPressed;

        // Hard drop : double-tap flèche bas (ou maintien prolongé > 0.5s déjà géré par soft drop)
    }

    // ── Verrouillage ──────────────────────────────────────────────────────────

    private void LockPiece()
    {
        if (activePiece == null) return;

        Color color = activePiece.PieceColor;

        foreach (var pos in activePiece.GetCells())
        {
            if (!TPMGrid.Instance.InBounds(pos.x, pos.y)) continue;
            TPMGrid.Instance.LockTetrisBlock(pos.x, pos.y);
            var go = BuildLockedVisual(TPMGrid.Instance.CellToWorld(pos.x, pos.y), color);
            lockedGOs[pos]     = go;
            lockedColors[pos]  = color;
        }

        ClearActiveVisuals();
        ClearGhostVisuals();
        CheckAndClearLines();
        activePiece = null;

        StartCoroutine(SpawnAfterDelay(0.08f));
    }

    private IEnumerator SpawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (TPMGameManager.Instance?.State == TPMGameManager.GameState.Playing)
            SpawnNext();
    }

    // ── Effacement de lignes ──────────────────────────────────────────────────

    private void CheckAndClearLines()
    {
        var fullRows = FindFullRows();
        if (fullRows.Count == 0) return;

        var toRemove = new List<Vector2Int>();
        foreach (var kv in lockedGOs)
        {
            if (!fullRows.Contains(kv.Key.y)) continue;
            if (kv.Value != null) Destroy(kv.Value);
            toRemove.Add(kv.Key);
        }
        foreach (var k in toRemove) { lockedGOs.Remove(k); lockedColors.Remove(k); }

        int cleared = TPMGrid.Instance.ClearFullLines();
        RebuildLockedVisuals();

        Vector3 center = TPMGrid.Instance.CellToWorld(settings.gridWidth / 2, settings.gridHeight / 2);
        TPMGameManager.Instance?.NotifyLineCleared(cleared, center);
        TPMFeedbackManager.Instance?.PlayDestroyEffect(center);
    }

    private List<int> FindFullRows()
    {
        var rows = new List<int>();
        int w = settings.gridWidth;
        int h = settings.gridHeight;

        for (int y = 1; y < h - 1; y++)
        {
            bool full = true;
            for (int x = 1; x < w - 1; x++)
            {
                if (TPMGrid.Instance.GetCell(x, y) == TPMGrid.CellType.Empty) { full = false; break; }
            }
            if (full) rows.Add(y);
        }
        return rows;
    }

    private void RebuildLockedVisuals()
    {
        foreach (var kv in lockedGOs) if (kv.Value != null) Destroy(kv.Value);
        lockedGOs.Clear();

        int w = settings.gridWidth;
        int h = settings.gridHeight;

        for (int x = 0; x < w; x++)
        for (int y = 0; y < h; y++)
        {
            if (TPMGrid.Instance.GetCell(x, y) != TPMGrid.CellType.TetrisBlock) continue;
            var key   = new Vector2Int(x, y);
            Color c   = lockedColors.TryGetValue(key, out var col) ? col : new Color(0.55f, 0.55f, 0.60f, 1f);
            lockedGOs[key] = BuildLockedVisual(TPMGrid.Instance.CellToWorld(x, y), c);
        }
    }

    // ── Visuels pièce active ──────────────────────────────────────────────────

    private void RefreshActiveVisuals()
    {
        if (activePiece == null) { ClearActiveVisuals(); return; }
        var cells = activePiece.GetCells();

        while (activeCellGOs.Count < cells.Length) activeCellGOs.Add(BuildCellVisual(activePiece.PieceColor, 12));
        while (activeCellGOs.Count > cells.Length) { Destroy(activeCellGOs[^1]); activeCellGOs.RemoveAt(activeCellGOs.Count - 1); }

        for (int i = 0; i < cells.Length; i++)
            activeCellGOs[i].transform.position = TPMGrid.Instance.CellToWorld(cells[i].x, cells[i].y);
    }

    private void ClearActiveVisuals()
    {
        foreach (var go in activeCellGOs) if (go != null) Destroy(go);
        activeCellGOs.Clear();
    }

    // ── Ghost piece ───────────────────────────────────────────────────────────

    private void RefreshGhostVisuals()
    {
        if (activePiece == null) { ClearGhostVisuals(); return; }

        var ghost = new TPMTetrisPiece(activePiece.ShapeIndex, activePiece.Pivot);
        while (ghost.TryMove(Vector2Int.down, TPMGrid.Instance)) { }

        var gCells = ghost.GetCells();
        var aCells = activePiece.GetCells();
        bool same  = true;
        for (int i = 0; i < gCells.Length; i++) if (gCells[i] != aCells[i]) { same = false; break; }
        if (same) { ClearGhostVisuals(); return; }

        Color ghostColor = new Color(activePiece.PieceColor.r, activePiece.PieceColor.g, activePiece.PieceColor.b, 0.22f);

        while (ghostCellGOs.Count < gCells.Length) ghostCellGOs.Add(BuildCellVisual(ghostColor, 8));
        while (ghostCellGOs.Count > gCells.Length) { Destroy(ghostCellGOs[^1]); ghostCellGOs.RemoveAt(ghostCellGOs.Count - 1); }

        for (int i = 0; i < gCells.Length; i++)
        {
            ghostCellGOs[i].transform.position = TPMGrid.Instance.CellToWorld(gCells[i].x, gCells[i].y);
            foreach (var sr in ghostCellGOs[i].GetComponentsInChildren<SpriteRenderer>())
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0.20f);
        }
    }

    private void ClearGhostVisuals()
    {
        foreach (var go in ghostCellGOs) if (go != null) Destroy(go);
        ghostCellGOs.Clear();
    }

    // ── Helpers visuels ───────────────────────────────────────────────────────

    private GameObject BuildCellVisual(Color color, int sortOrder)
    {
        float cs = settings.cellSize * 0.90f;
        var root = new GameObject("TCell");

        var edgeGO  = new GameObject("Edge");
        edgeGO.transform.SetParent(root.transform, false);
        edgeGO.transform.localScale = Vector3.one * cs;
        var edgeSR  = edgeGO.AddComponent<SpriteRenderer>();
        edgeSR.sprite = SpriteGenerator.CreateColoredSquare(new Color(color.r * 0.58f, color.g * 0.58f, color.b * 0.58f, color.a));
        edgeSR.sortingOrder = sortOrder - 1;

        var bodyGO  = new GameObject("Body");
        bodyGO.transform.SetParent(root.transform, false);
        bodyGO.transform.localScale = Vector3.one * (cs * 0.87f);
        var bodySR  = bodyGO.AddComponent<SpriteRenderer>();
        bodySR.sprite = SpriteGenerator.CreateColoredSquare(color);
        bodySR.color  = color;
        bodySR.sortingOrder = sortOrder;

        var hlGO    = new GameObject("Highlight");
        hlGO.transform.SetParent(root.transform, false);
        hlGO.transform.localScale    = Vector3.one * (cs * 0.28f);
        hlGO.transform.localPosition = new Vector3(-cs * 0.24f, cs * 0.24f, 0f);
        var hlSR    = hlGO.AddComponent<SpriteRenderer>();
        hlSR.sprite = SpriteGenerator.CreateCircle(32);
        hlSR.color  = new Color(1f, 1f, 1f, 0.32f);
        hlSR.sortingOrder = sortOrder + 1;

        return root;
    }

    private GameObject BuildLockedVisual(Vector3 pos, Color color)
    {
        var go = BuildCellVisual(color, 7);
        go.transform.position = pos;
        return go;
    }

    // ── Accesseur pour le HUD ─────────────────────────────────────────────────

    /// <summary>Couleur et forme de la pièce suivante (pour l'affichage UI).</summary>
    public (int shapeIndex, Color color) GetNextPieceInfo() =>
        (nextShapeIndex, TPMTetrisPiece.Colors[nextShapeIndex]);
}
