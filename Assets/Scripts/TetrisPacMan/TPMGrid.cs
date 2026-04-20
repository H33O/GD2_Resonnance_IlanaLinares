using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Grille logique 12×18 du mini-jeu Tetris×Pac-Man.
/// Gère les types de cellules, le BFS pathfinding et les conversions monde ↔ grille.
/// </summary>
public class TPMGrid : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static TPMGrid Instance { get; private set; }

    // ── Types ─────────────────────────────────────────────────────────────────

    public enum CellType { Empty, Wall, PlayerBlock, TetrisBlock, Exit, Cage }

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Settings")]
    public TPMSettings settings;

    // ── État ──────────────────────────────────────────────────────────────────

    private CellType[,] cells;

    public Vector2Int PlayerStart  { get; private set; }
    public Vector2Int MonsterStart { get; private set; }
    public Vector2Int ExitCell     { get; private set; }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (settings == null) { Debug.LogError("[TPMGrid] settings manquant !"); return; }
        Init();
    }

    public void Init()
    {
        if (cells != null) return;
        cells = new CellType[settings.gridWidth, settings.gridHeight];
        BuildLevel1();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── Level 1 : labyrinthe 10×20 ────────────────────────────────────────────

    /// <summary>
    /// Layout labyrinthe inspiré Pac-Man : couloirs + zones ouvertes + chemins alternatifs.
    /// Grille 10×20 (portrait Tetris standard). Murs de bordure + obstacles intérieurs.
    /// Sortie (EXIT) en haut au centre. Joueur en bas-gauche. Cage ennemi en bas-droite.
    /// </summary>
    private void BuildLevel1()
    {
        int w = settings.gridWidth;  // 10
        int h = settings.gridHeight; // 20

        // ── Murs de bordure ───────────────────────────────────────────────────
        for (int x = 0; x < w; x++) { W(x, 0); W(x, h - 1); }
        for (int y = 0; y < h; y++) { W(0, y); W(w - 1, y); }

        // ── Zone haute — entonnoir sortie ─────────────────────────────────────
        W(1, 17); W(2, 17);
        W(7, 17); W(8, 17);

        W(1, 16); W(2, 16); W(3, 16);
        W(6, 16); W(7, 16); W(8, 16);

        // ── Zone médiane haute (rangées 13-15) ────────────────────────────────
        W(1, 15); W(2, 15);
        W(7, 15); W(8, 15);

        W(3, 14); W(4, 14);
        W(5, 14); W(6, 14);

        W(2, 13); W(3, 13);
        W(6, 13); W(7, 13);

        // ── Zone médiane (rangées 10-12) ──────────────────────────────────────
        W(1, 12); W(2, 12);
        W(7, 12); W(8, 12);

        W(3, 11); W(4, 11);
        W(5, 11); W(6, 11);

        W(2, 10); W(3, 10);
        W(6, 10); W(7, 10);

        // ── Zone médiane basse (rangées 7-9) ──────────────────────────────────
        W(1, 9); W(2, 9);
        W(7, 9); W(8, 9);

        W(3, 8); W(4, 8);
        W(5, 8); W(6, 8);

        W(2, 7); W(3, 7);
        W(6, 7); W(7, 7);

        // ── Zone basse (rangées 4-6) ──────────────────────────────────────────
        W(1, 6); W(2, 6);
        W(7, 6); W(8, 6);

        W(3, 5); W(4, 5);
        W(5, 5); W(6, 5);

        W(2, 4); W(3, 4);
        W(6, 4); W(7, 4);

        // ── Zone basse (rangées 2-3) — labyrinthe bas ────────────────────────
        W(3, 3); W(4, 3);
        W(5, 3); W(6, 3);

        // ── Cage de l'ennemi (bas-droite) ─────────────────────────────────────
        // Cage 3×2 : murs haut + droit, ouverture à gauche (7,2)
        W(7, 1); W(8, 1);
        W(8, 2);
        // (7,2) reste vide — sortie du monstre

        // ── Sortie EXIT ───────────────────────────────────────────────────────
        ExitCell = new Vector2Int(w / 2, h - 2); // (5, 18)
        cells[ExitCell.x, ExitCell.y] = CellType.Exit;

        // ── Spawns ────────────────────────────────────────────────────────────
        PlayerStart  = new Vector2Int(2, 2);
        MonsterStart = new Vector2Int(8, 2); // intérieur de la cage
    }

    private void W(int x, int y)
    {
        if (x >= 0 && x < settings.gridWidth && y >= 0 && y < settings.gridHeight)
            cells[x, y] = CellType.Wall;
    }

    // ── Cage ──────────────────────────────────────────────────────────────────

    /// <summary>Ouvre la cage de l'ennemi (transforme les Cage en Empty).</summary>
    public void OpenCage()
    {
        int w = settings.gridWidth;
        int h = settings.gridHeight;
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                if (cells[x, y] == CellType.Cage)
                    cells[x, y] = CellType.Empty;
    }

    // ── API publique ──────────────────────────────────────────────────────────

    public bool      InBounds(int x, int y)    => x >= 0 && x < settings.gridWidth && y >= 0 && y < settings.gridHeight;
    public CellType  GetCell(int x, int y)     => InBounds(x, y) ? cells[x, y] : CellType.Wall;
    public bool      IsEmpty(int x, int y)     => GetCell(x, y) == CellType.Empty;
    public bool      IsWalkable(int x, int y)  => GetCell(x, y) == CellType.Empty || GetCell(x, y) == CellType.Exit;
    public bool      IsFreeFalling(int x, int y) => GetCell(x, y) == CellType.Empty;
    public bool      IsPlayerBlock(int x, int y) => GetCell(x, y) == CellType.PlayerBlock;
    public bool      IsTetrisBlock(int x, int y) => GetCell(x, y) == CellType.TetrisBlock;

    public bool TryPlaceBlock(int x, int y)
    {
        if (!IsEmpty(x, y)) return false;
        cells[x, y] = CellType.PlayerBlock;
        return true;
    }

    public bool TryDestroyBlock(int x, int y)
    {
        if (!IsPlayerBlock(x, y)) return false;
        cells[x, y] = CellType.Empty;
        return true;
    }

    public void LockTetrisBlock(int x, int y)
    {
        if (InBounds(x, y)) cells[x, y] = CellType.TetrisBlock;
    }

    public void UnlockCell(int x, int y)
    {
        if (InBounds(x, y) && cells[x, y] == CellType.TetrisBlock)
            cells[x, y] = CellType.Empty;
    }

    /// <summary>Cherche les lignes complètes, les efface et décale les blocs. Retourne le nombre de lignes.</summary>
    public int ClearFullLines()
    {
        int w = settings.gridWidth;
        int h = settings.gridHeight;
        int cleared = 0;

        for (int y = h - 2; y >= 1; y--)
        {
            if (!IsLineFull(y)) continue;
            for (int x = 1; x < w - 1; x++) cells[x, y] = CellType.Empty;
            for (int row = y; row < h - 2; row++)
                for (int x = 1; x < w - 1; x++)
                    cells[x, row] = cells[x, row + 1];
            for (int x = 1; x < w - 1; x++) cells[x, h - 2] = CellType.Empty;
            cleared++;
            y++;
        }
        return cleared;
    }

    private bool IsLineFull(int y)
    {
        for (int x = 1; x < settings.gridWidth - 1; x++)
            if (cells[x, y] == CellType.Empty) return false;
        return true;
    }

    // ── Conversions ───────────────────────────────────────────────────────────

    public Vector3 CellToWorld(int x, int y)
    {
        float cs = settings.cellSize;
        float ox = -(settings.gridWidth  - 1) * cs * 0.5f;
        float oy = -(settings.gridHeight - 1) * cs * 0.5f;
        return new Vector3(ox + x * cs, oy + y * cs, 0f);
    }

    public Vector2Int WorldToCell(Vector3 world)
    {
        float cs = settings.cellSize;
        float ox = -(settings.gridWidth  - 1) * cs * 0.5f;
        float oy = -(settings.gridHeight - 1) * cs * 0.5f;
        return new Vector2Int(
            Mathf.RoundToInt((world.x - ox) / cs),
            Mathf.RoundToInt((world.y - oy) / cs));
    }

    // ── BFS pathfinding ───────────────────────────────────────────────────────

    private static readonly Vector2Int[] Dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    /// <summary>Premier pas du chemin BFS entre from et to, en contournant les obstacles.</summary>
    public Vector2Int? FindNextStep(Vector2Int from, Vector2Int to)
    {
        if (from == to) return null;

        var visited = new Dictionary<Vector2Int, Vector2Int>();
        var queue   = new Queue<Vector2Int>();
        visited[from] = from;
        queue.Enqueue(from);

        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            foreach (var d in Dirs)
            {
                var next = cur + d;
                if (visited.ContainsKey(next)) continue;
                if (!InBounds(next.x, next.y))  continue;
                var ct = GetCell(next.x, next.y);
                if (ct == CellType.Wall || ct == CellType.PlayerBlock || ct == CellType.TetrisBlock || ct == CellType.Cage) continue;
                visited[next] = cur;
                if (next == to) return Reconstruct(visited, from, to);
                queue.Enqueue(next);
            }
        }
        return null;
    }

    private static Vector2Int Reconstruct(Dictionary<Vector2Int, Vector2Int> visited, Vector2Int from, Vector2Int to)
    {
        var cur = to;
        while (visited[cur] != from) cur = visited[cur];
        return cur;
    }
}
