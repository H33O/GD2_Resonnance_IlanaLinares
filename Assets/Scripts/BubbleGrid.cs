using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gère la grille hexagonale : placement, matching (BFS), suppression et chute des bulles flottantes.
/// </summary>
public class BubbleGrid : MonoBehaviour
{
    public static BubbleGrid Instance { get; private set; }

    [Header("Grille")]
    [SerializeField] private int cols = 9;
    [SerializeField] private int maxRows = 14;
    [SerializeField] private float diameter = 0.5f;
    [SerializeField] private int startRows = 5;
    [SerializeField] private int minMatch = 3;

    [Header("Position")]
    [SerializeField] private float topOffset = 1.5f;  // marge depuis le haut de la caméra

    [Header("Couleurs actives")]
    [SerializeField] private int colorCount = 4;

    public float Diameter => diameter;
    public int ColorCount => colorCount;

    private Bubble[,] grid;
    private float topY, startX, rowH, radius;

    private void Awake()
    {
        Instance = this;
        radius = diameter * 0.5f;
        rowH = diameter * Mathf.Sqrt(3f) * 0.5f;
        grid = new Bubble[maxRows, cols];
    }

    private void Start()
    {
        Camera cam = Camera.main;
        topY   = cam.orthographicSize - topOffset;
        startX = -(cols - 1) * diameter * 0.5f;
        SpawnInitial();
    }

    // ── API publique ─────────────────────────────────────────────────────────

    /// <summary>Place le projectile dans la grille et vérifie les correspondances.</summary>
    public void PlaceProjectile(BubbleColor color, Vector3 worldPos)
    {
        (int r, int c) = NearestEmpty(worldPos);
        if (r < 0) return;
        SpawnBubble(color, r, c);
        StartCoroutine(MatchRoutine(r, c, color));
    }

    /// <summary>Retourne vrai si la grille ne contient aucune bulle.</summary>
    public bool IsEmpty()
    {
        for (int r = 0; r < maxRows; r++)
            for (int c = 0; c < cols; c++)
                if (grid[r, c] != null) return false;
        return true;
    }

    // ── Coordonnées ──────────────────────────────────────────────────────────

    public Vector3 ToWorld(int r, int c)
    {
        float x = startX + c * diameter + (r % 2 == 1 ? radius : 0f);
        float y = topY - r * rowH;
        return new Vector3(x, y, 0f);
    }

    private (int r, int c) ToGrid(Vector3 w)
    {
        int r = Mathf.Clamp(Mathf.RoundToInt((topY - w.y) / rowH), 0, maxRows - 1);
        float offset = r % 2 == 1 ? radius : 0f;
        int c = Mathf.Clamp(Mathf.RoundToInt((w.x - startX - offset) / diameter), 0, cols - 1);
        return (r, c);
    }

    private (int, int) NearestEmpty(Vector3 worldPos)
    {
        (int r, int c) = ToGrid(worldPos);
        if (Valid(r, c) && grid[r, c] == null) return (r, c);

        float best = float.MaxValue;
        int br = -1, bc = -1;
        foreach ((int nr, int nc) in Neighbors(r, c))
        {
            if (!Valid(nr, nc) || grid[nr, nc] != null) continue;
            float d = Vector3.Distance(worldPos, ToWorld(nr, nc));
            if (d < best) { best = d; br = nr; bc = nc; }
        }
        return (br, bc);
    }

    // ── Matching ─────────────────────────────────────────────────────────────

    private IEnumerator MatchRoutine(int r, int c, BubbleColor color)
    {
        yield return null; // attendre que Init() de Bubble soit appelé

        List<(int, int)> group = BFS(r, c, color);
        if (group.Count >= minMatch)
        {
            foreach ((int gr, int gc) in group)
            {
                grid[gr, gc]?.Pop();
                grid[gr, gc] = null;
            }
            BubbleGameManager.Instance?.AddScore(group.Count * 10);
            yield return new WaitForSeconds(0.25f);
            DropFloating();
        }

        BubbleGameManager.Instance?.CheckEnd();
    }

    private List<(int, int)> BFS(int startR, int startC, BubbleColor color)
    {
        var result = new List<(int, int)>();
        var visited = new HashSet<(int, int)>();
        var queue = new Queue<(int, int)>();
        queue.Enqueue((startR, startC));
        visited.Add((startR, startC));
        while (queue.Count > 0)
        {
            (int r, int c) = queue.Dequeue();
            result.Add((r, c));
            foreach ((int nr, int nc) in Neighbors(r, c))
                if (Valid(nr, nc) && !visited.Contains((nr, nc)) && grid[nr, nc]?.ColorType == color)
                { visited.Add((nr, nc)); queue.Enqueue((nr, nc)); }
        }
        return result;
    }

    private void DropFloating()
    {
        HashSet<(int, int)> anchored = CeilingBFS();
        for (int r = 0; r < maxRows; r++)
            for (int c = 0; c < cols; c++)
                if (grid[r, c] != null && !anchored.Contains((r, c)))
                {
                    grid[r, c].Fall();
                    grid[r, c] = null;
                    BubbleGameManager.Instance?.AddScore(20);
                }
    }

    private HashSet<(int, int)> CeilingBFS()
    {
        var visited = new HashSet<(int, int)>();
        var queue = new Queue<(int, int)>();
        for (int c = 0; c < cols; c++)
            if (grid[0, c] != null) { visited.Add((0, c)); queue.Enqueue((0, c)); }
        while (queue.Count > 0)
        {
            (int r, int c) = queue.Dequeue();
            foreach ((int nr, int nc) in Neighbors(r, c))
                if (Valid(nr, nc) && grid[nr, nc] != null && !visited.Contains((nr, nc)))
                { visited.Add((nr, nc)); queue.Enqueue((nr, nc)); }
        }
        return visited;
    }

    // ── Voisins hexagonaux ────────────────────────────────────────────────────

    private IEnumerable<(int, int)> Neighbors(int r, int c)
    {
        yield return (r, c - 1);
        yield return (r, c + 1);
        if (r % 2 == 0)
        {
            yield return (r - 1, c - 1); yield return (r - 1, c);
            yield return (r + 1, c - 1); yield return (r + 1, c);
        }
        else
        {
            yield return (r - 1, c); yield return (r - 1, c + 1);
            yield return (r + 1, c); yield return (r + 1, c + 1);
        }
    }

    private bool Valid(int r, int c) => r >= 0 && r < maxRows && c >= 0 && c < cols;

    // ── Spawn ─────────────────────────────────────────────────────────────────

    private void SpawnInitial()
    {
        for (int r = 0; r < startRows; r++)
            for (int c = 0; c < cols; c++)
                SpawnBubble(BubbleColorExtensions.Random(colorCount), r, c);
    }

    private void SpawnBubble(BubbleColor color, int r, int c)
    {
        if (!Valid(r, c) || grid[r, c] != null) return;

        var go = new GameObject($"Bubble_{r}_{c}");
        go.transform.SetParent(transform);
        go.transform.position = ToWorld(r, c);
        go.transform.localScale = Vector3.one * diameter;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteGenerator.Circle();
        sr.sortingOrder = 0;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;

        var b = go.AddComponent<Bubble>();
        b.Init(color, r, c);
        grid[r, c] = b;
    }
}
