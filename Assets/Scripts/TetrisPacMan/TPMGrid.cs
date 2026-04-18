using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gère la grille logique du jeu : obstacles fixes, blocs posés par le joueur,
/// cellule de départ/sortie et conversion coordonnées monde ↔ cellule.
/// </summary>
public class TPMGrid : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static TPMGrid Instance { get; private set; }

    // ── Types ─────────────────────────────────────────────────────────────────

    public enum CellType { Empty, Wall, PlayerBlock, Exit }

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Settings")]
    public TPMSettings settings;

    // ── État ──────────────────────────────────────────────────────────────────

    private CellType[,] cells;

    /// <summary>Position de départ du joueur (coordonnées grille).</summary>
    public Vector2Int PlayerStart { get; private set; }

    /// <summary>Position de la sortie (coordonnées grille).</summary>
    public Vector2Int ExitCell { get; private set; }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // settings est assigné par TPMSceneSetup juste après AddComponent,
        // donc disponible dès Start (après la fin du premier Awake frame).
        if (settings == null)
        {
            Debug.LogError("[TPMGrid] settings non assigné !");
            return;
        }
        cells = new CellType[settings.gridWidth, settings.gridHeight];
        BuildLevel();
    }

    /// <summary>
    /// Initialisation explicite appelée par TPMSceneSetup pour garantir
    /// que settings est disponible avant le premier accès à la grille.
    /// </summary>
    public void Init()
    {
        if (cells != null) return; // déjà initialisé
        cells = new CellType[settings.gridWidth, settings.gridHeight];
        BuildLevel();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── Construction du niveau ────────────────────────────────────────────────

    /// <summary>
    /// Construit la carte du niveau 1 calibrée pour une grille 9×15 (1080×1920 portrait) :
    /// murs de bordure, obstacles fixes intérieurs, départ et sortie.
    /// </summary>
    private void BuildLevel()
    {
        int w = settings.gridWidth;   // 9
        int h = settings.gridHeight;  // 15

        // Murs de bordure
        for (int x = 0; x < w; x++)
        {
            cells[x, 0]     = CellType.Wall;
            cells[x, h - 1] = CellType.Wall;
        }
        for (int y = 0; y < h; y++)
        {
            cells[0, y]     = CellType.Wall;
            cells[w - 1, y] = CellType.Wall;
        }

        // ── Obstacles intérieurs — level design 9×15 ──────────────────────────
        // Couloir gauche bloquant
        SetWall(2, 3); SetWall(2, 4); SetWall(2, 5);

        // Îlot central haut
        SetWall(4, 4); SetWall(5, 4);
        SetWall(4, 5);

        // Mur diagonal milieu-droite
        SetWall(6, 6); SetWall(6, 7); SetWall(6, 8);

        // Îlot bas-gauche
        SetWall(2, 8); SetWall(3, 8);

        // Couloir central bas
        SetWall(4, 10); SetWall(5, 10); SetWall(4, 11);

        // Mur droit milieu
        SetWall(6, 3); SetWall(7, 3);

        // Bloc isolé
        SetWall(3, 6);

        // ── Départ et sortie ─────────────────────────────────────────────────
        // Départ joueur : coin bas-gauche dégagé
        PlayerStart = new Vector2Int(1, 1);

        // Sortie : coin haut-droit (max tension avec le monstre qui part du même côté)
        ExitCell = new Vector2Int(w - 2, h - 2);
        cells[ExitCell.x, ExitCell.y] = CellType.Exit;
    }

    private void SetWall(int x, int y) => cells[x, y] = CellType.Wall;

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>Retourne vrai si la cellule est dans les limites de la grille.</summary>
    public bool InBounds(int x, int y) =>
        x >= 0 && x < settings.gridWidth && y >= 0 && y < settings.gridHeight;

    /// <summary>Retourne le type de la cellule.</summary>
    public CellType GetCell(int x, int y) =>
        InBounds(x, y) ? cells[x, y] : CellType.Wall;

    /// <summary>Retourne vrai si la cellule est traversable (vide ou sortie).</summary>
    public bool IsWalkable(int x, int y)
    {
        CellType t = GetCell(x, y);
        return t == CellType.Empty || t == CellType.Exit;
    }

    /// <summary>Retourne vrai si la cellule est vide (peut recevoir un bloc).</summary>
    public bool IsEmpty(int x, int y) => GetCell(x, y) == CellType.Empty;

    /// <summary>Retourne vrai si la cellule contient un bloc posé par le joueur.</summary>
    public bool IsPlayerBlock(int x, int y) => GetCell(x, y) == CellType.PlayerBlock;

    /// <summary>Place un bloc du joueur sur une cellule vide.</summary>
    public bool TryPlaceBlock(int x, int y)
    {
        if (!IsEmpty(x, y)) return false;
        cells[x, y] = CellType.PlayerBlock;
        return true;
    }

    /// <summary>Détruit un bloc posé par le joueur. Retourne vrai si réussi.</summary>
    public bool TryDestroyBlock(int x, int y)
    {
        if (!IsPlayerBlock(x, y)) return false;
        cells[x, y] = CellType.Empty;
        return true;
    }

    /// <summary>
    /// Convertit des coordonnées grille en position monde (centre de la cellule).
    /// </summary>
    public Vector3 CellToWorld(int x, int y)
    {
        float cs      = settings.cellSize;
        float offsetX = -(settings.gridWidth  - 1) * cs * 0.5f;
        float offsetY = -(settings.gridHeight - 1) * cs * 0.5f;
        return new Vector3(offsetX + x * cs, offsetY + y * cs, 0f);
    }

    /// <summary>
    /// Convertit une position monde en coordonnées grille (arrondi au plus proche).
    /// </summary>
    public Vector2Int WorldToCell(Vector3 worldPos)
    {
        float cs      = settings.cellSize;
        float offsetX = -(settings.gridWidth  - 1) * cs * 0.5f;
        float offsetY = -(settings.gridHeight - 1) * cs * 0.5f;
        int x = Mathf.RoundToInt((worldPos.x - offsetX) / cs);
        int y = Mathf.RoundToInt((worldPos.y - offsetY) / cs);
        return new Vector2Int(x, y);
    }

    // ── BFS pathfinding (utilisé par le monstre) ──────────────────────────────

    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    /// <summary>
    /// Retourne le premier pas du chemin BFS du monstre vers la cible,
    /// en contournant les murs et les blocs. Retourne null si aucun chemin.
    /// </summary>
    public Vector2Int? FindNextStep(Vector2Int from, Vector2Int to)
    {
        if (from == to) return null;

        var visited = new Dictionary<Vector2Int, Vector2Int>();
        var queue   = new Queue<Vector2Int>();

        visited[from] = from;
        queue.Enqueue(from);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            foreach (Vector2Int dir in Directions)
            {
                Vector2Int next = current + dir;
                if (visited.ContainsKey(next)) continue;
                if (!InBounds(next.x, next.y))  continue;

                CellType ct = GetCell(next.x, next.y);
                // Le monstre ne traverse pas les murs ni les blocs du joueur
                if (ct == CellType.Wall || ct == CellType.PlayerBlock) continue;

                visited[next] = current;
                if (next == to)
                    return ReconstructFirst(visited, from, to);

                queue.Enqueue(next);
            }
        }

        return null; // Chemin bloqué
    }

    private static Vector2Int ReconstructFirst(
        Dictionary<Vector2Int, Vector2Int> visited, Vector2Int from, Vector2Int to)
    {
        Vector2Int current = to;
        while (visited[current] != from)
            current = visited[current];
        return current;
    }
}
