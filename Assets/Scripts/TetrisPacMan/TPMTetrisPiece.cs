using UnityEngine;

/// <summary>
/// Représente une pièce Tetris en cours de chute.
/// Stocke la forme, la rotation et la position pivot sur la grille.
/// Toutes les pièces classiques des 7 tetrominoes sont définies ici.
/// </summary>
public class TPMTetrisPiece
{
    // ── Définitions des 7 pièces (coordonnées relatives au pivot, rotation 0) ─

    // Chaque pièce = tableau de 4 offsets (x,y) depuis le pivot
    public static readonly Vector2Int[][] Shapes = new Vector2Int[][]
    {
        // I (cyan)
        new[] { new Vector2Int(-1,0), new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0) },
        // O (jaune)
        new[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(1,1) },
        // T (violet)
        new[] { new Vector2Int(-1,0), new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(0,1) },
        // S (vert)
        new[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(-1,1), new Vector2Int(0,1) },
        // Z (rouge)
        new[] { new Vector2Int(-1,0), new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(1,1) },
        // J (bleu)
        new[] { new Vector2Int(-1,0), new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(-1,1) },
        // L (orange)
        new[] { new Vector2Int(-1,0), new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(1,1) },
    };

    public static readonly Color[] Colors = new Color[]
    {
        new Color(0.00f, 0.90f, 0.95f, 1f), // I — cyan
        new Color(1.00f, 0.90f, 0.00f, 1f), // O — jaune
        new Color(0.75f, 0.20f, 0.90f, 1f), // T — violet
        new Color(0.10f, 0.85f, 0.20f, 1f), // S — vert
        new Color(1.00f, 0.18f, 0.10f, 1f), // Z — rouge
        new Color(0.10f, 0.30f, 0.95f, 1f), // J — bleu
        new Color(1.00f, 0.55f, 0.00f, 1f), // L — orange
    };

    // ── Données de la pièce ───────────────────────────────────────────────────

    public int        ShapeIndex  { get; private set; }
    public Color      PieceColor  { get; private set; }
    public Vector2Int Pivot       { get; private set; }

    private Vector2Int[] localCells; // offsets courants (après rotations)

    // ── Construction ──────────────────────────────────────────────────────────

    public TPMTetrisPiece(int shapeIndex, Vector2Int spawnPivot)
    {
        ShapeIndex = shapeIndex;
        PieceColor = Colors[shapeIndex];
        Pivot      = spawnPivot;

        // Copie défensive des offsets
        var src = Shapes[shapeIndex];
        localCells = new Vector2Int[src.Length];
        for (int i = 0; i < src.Length; i++)
            localCells[i] = src[i];
    }

    // ── Accesseurs ────────────────────────────────────────────────────────────

    /// <summary>Retourne les positions monde-grille de toutes les cellules de la pièce.</summary>
    public Vector2Int[] GetCells()
    {
        var result = new Vector2Int[localCells.Length];
        for (int i = 0; i < localCells.Length; i++)
            result[i] = Pivot + localCells[i];
        return result;
    }

    // ── Mouvements ────────────────────────────────────────────────────────────

    /// <summary>Tente de déplacer la pièce. Retourne vrai si possible (sans collision).</summary>
    public bool TryMove(Vector2Int delta, TPMGrid grid)
    {
        Vector2Int newPivot = Pivot + delta;
        if (!CanFit(newPivot, localCells, grid)) return false;
        Pivot = newPivot;
        return true;
    }

    /// <summary>Tente de faire tourner la pièce (sens horaire). Retourne vrai si possible.</summary>
    public bool TryRotate(TPMGrid grid)
    {
        var rotated = Rotate90CW(localCells);

        // Wall kick simple : essaie le pivot central, puis ±1 sur x
        Vector2Int[] kicks = { Vector2Int.zero, Vector2Int.right, Vector2Int.left };
        foreach (var kick in kicks)
        {
            if (CanFit(Pivot + kick, rotated, grid))
            {
                Pivot      += kick;
                localCells  = rotated;
                return true;
            }
        }
        return false;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool CanFit(Vector2Int pivot, Vector2Int[] cells, TPMGrid grid)
    {
        foreach (var offset in cells)
        {
            Vector2Int pos = pivot + offset;
            if (!grid.InBounds(pos.x, pos.y)) return false;
            if (!grid.IsFreeFalling(pos.x, pos.y)) return false;
        }
        return true;
    }

    private static Vector2Int[] Rotate90CW(Vector2Int[] cells)
    {
        var result = new Vector2Int[cells.Length];
        for (int i = 0; i < cells.Length; i++)
            result[i] = new Vector2Int(cells[i].y, -cells[i].x);
        return result;
    }
}
