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
    [SerializeField] private float topOffset = 0.2f;  // marge depuis le haut de la caméra

    [Header("Descente")]
    [SerializeField] private int shotsPerDescend = 5;      // tous les N tirs, la grille descend
    [SerializeField] private float descendDuration = 1.2f; // durée de l'animation de descente (s)

    [Header("Couleurs actives")]
    [SerializeField] private int colorCount = 4;

    [Header("Sprites par couleur")]
    [Tooltip("Sprites indexés par BubbleColor : 0=Red, 1=Blue, 2=Green, 3=Yellow, 4=Purple")]
    [SerializeField] private Sprite[] bubbleSprites;

    [Header("Bulles bonus")]
    [SerializeField] private float bonusBubbleChance = 0.08f; // probabilité par bulle

    public float Diameter => diameter;
    public int ColorCount => colorCount;

    /// <summary>Retourne le sprite associé à une couleur, ou null si non configuré.</summary>
    public Sprite GetSprite(BubbleColor color)
    {
        int index = (int)color;
        if (bubbleSprites == null || index >= bubbleSprites.Length) return null;
        return bubbleSprites[index];
    }

    private Bubble[,] grid;
    private float topY, startX, rowH, radius;
    private int placementCount;
    private int fallingBubbleCount;

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
        startX = -(cols - 1) * diameter * 0.5f - radius * 0.5f;
        SpawnInitial();
    }

    // ── API publique ─────────────────────────────────────────────────────────

    /// <summary>Place le projectile dans la grille et vérifie les correspondances.</summary>
    public void PlaceProjectile(BubbleColor color, Vector3 worldPos)
    {
        (int r, int c) = NearestEmpty(worldPos);
        if (r < 0)
        {
            BubbleGameManager.Instance?.CheckEnd();
            return;
        }
        SpawnBubble(color, r, c);

        placementCount++;
        bool shouldDescend = shotsPerDescend > 0 && placementCount % shotsPerDescend == 0;

        // La descente est programmée APRÈS la fin du MatchRoutine pour éviter
        // toute race condition avec le BFS (la grille ne doit pas bouger pendant le BFS).
        StartCoroutine(MatchRoutine(r, c, color, shouldDescend));
    }

    /// <summary>Returns true only when the grid is empty AND no bubble is still falling.</summary>
    public bool IsEmpty()
    {
        if (fallingBubbleCount > 0) return false;
        for (int r = 0; r < maxRows; r++)
            for (int c = 0; c < cols; c++)
                if (grid[r, c] != null) return false;
        return true;
    }

    /// <summary>
    /// Applique les paramètres d'un niveau : réinitialise la grille avec les nouvelles données.
    /// Appelé par <see cref="BubbleLevelManager"/> au démarrage de chaque niveau.
    /// </summary>
    public void ApplyLevelData(BubbleLevelData data)
    {
        if (data == null) return;

        startRows         = data.startRows;
        colorCount        = Mathf.Clamp(data.colorCount, 2, 5);
        shotsPerDescend   = data.shotsPerDescend;
        descendDuration   = data.descendDuration;
        bonusBubbleChance = data.bonusBubbleChance;
        bubbleSprites     = data.bubbleSprites;

        // Réinitialise le compteur de placements pour la descente
        placementCount = 0;

        // Vide la grille courante et respawn
        ClearGrid();
        SpawnInitial();
    }

    /// <summary>Détruit tous les objets Bubble de la grille et vide le tableau logique.</summary>
    private void ClearGrid()
    {
        for (int r = 0; r < maxRows; r++)
            for (int c = 0; c < cols; c++)
            {
                if (grid[r, c] != null)
                    Destroy(grid[r, c].gameObject);
                grid[r, c] = null;
            }
        fallingBubbleCount = 0;
    }

    /// <summary>Called by Bubble when its fall animation finishes.</summary>
    public void OnBubbleFallComplete()
    {
        fallingBubbleCount = Mathf.Max(0, fallingBubbleCount - 1);
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
        // Cherche parmi toutes les cellules vides ET ancrées (adjacentes à une bulle existante
        // ou en rangée 0) la plus proche de la position du projectile.
        // Cela garantit qu'aucune bulle ne se retrouve isolée (bug du "premier tir qui disparaît").
        float best = float.MaxValue;
        int br = -1, bc = -1;

        for (int r = 0; r < maxRows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (grid[r, c] != null) continue;

                bool anchored = (r == 0);
                if (!anchored)
                {
                    foreach ((int nr, int nc) in Neighbors(r, c))
                        if (Valid(nr, nc) && grid[nr, nc] != null) { anchored = true; break; }
                }
                if (!anchored) continue;

                float dist = Vector3.Distance(worldPos, ToWorld(r, c));
                if (dist < best) { best = dist; br = r; bc = c; }
            }
        }
        return (br, bc);
    }

    // ── Matching ─────────────────────────────────────────────────────────────

    private IEnumerator MatchRoutine(int r, int c, BubbleColor color, bool descendAfter = false)
    {
        yield return null; // attendre un frame pour que Bubble.Awake() s'exécute

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

        // Vérifie la fin de partie avant de descendre
        BubbleGameManager.Instance?.CheckEnd();

        // Descend uniquement si le jeu est encore actif (pas de victoire/défaite entre-temps)
        if (descendAfter
            && BubbleGameManager.Instance != null
            && BubbleGameManager.Instance.IsGameActive)
        {
            DescendGrid();
        }
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
        int dropCount = 0;
        for (int r = 0; r < maxRows; r++)
            for (int c = 0; c < cols; c++)
                if (grid[r, c] != null && !anchored.Contains((r, c)))
                {
                    fallingBubbleCount++;
                    grid[r, c].Fall();
                    grid[r, c] = null;
                    BubbleGameManager.Instance?.AddScore(20);
                    dropCount++;
                }

        if (dropCount >= 1)
        {
            BubbleGameManager.Instance?.ShowPerfect(dropCount);
            float intensity = Mathf.Lerp(0.05f, 0.18f, Mathf.Clamp01(dropCount / 10f));
            BubbleGameManager.Instance?.ShakeCamera(0.3f, intensity);
        }

        // Notifie le LevelManager si la grille est entièrement vidée
        StartCoroutine(CheckGridClearedRoutine());
    }

    private IEnumerator CheckGridClearedRoutine()
    {
        // Attendre que toutes les bulles en chute aient touché le sol
        yield return new WaitUntil(() => fallingBubbleCount <= 0);
        if (IsEmpty())
            BubbleLevelManager.Instance?.OnGridCleared();
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

    // ── Descente de grille ────────────────────────────────────────────────────

    /// <summary>Descend toute la grille d'une rangée avec animation fluide,
    /// puis vérifie la défaite une fois l'animation terminée.</summary>
    private void DescendGrid()
    {
        // Décaler le tableau logique de bas en haut
        for (int r = maxRows - 1; r > 0; r--)
        {
            for (int c = 0; c < cols; c++)
            {
                grid[r, c] = grid[r - 1, c];
                if (grid[r, c] != null)
                    grid[r, c].UpdateGridPosition(r, c);
            }
        }

        // Vider la rangée 0 — aucune nouvelle bulle
        for (int c = 0; c < cols; c++)
            grid[0, c] = null;

        // Lancer les animations visuelles
        for (int r = 1; r < maxRows; r++)
            for (int c = 0; c < cols; c++)
                grid[r, c]?.MoveTo(ToWorld(r, c), descendDuration);

        // Vérifier la défaite après la fin de l'animation
        StartCoroutine(CheckDefeatAfterAnimation());
    }

    private IEnumerator CheckDefeatAfterAnimation()
    {
        yield return new WaitForSeconds(descendDuration);

        if (BubbleGameManager.Instance == null || !BubbleGameManager.Instance.IsGameActive)
            yield break;

        // Défaite quand une bulle atteint la dernière rangée de la grille
        for (int c = 0; c < cols; c++)
        {
            if (grid[maxRows - 1, c] != null)
            {
                BubbleGameManager.Instance.TriggerDefeat();
                yield break;
            }
        }
    }

    // ── Spawn ─────────────────────────────────────────────────────────────────

    private void SpawnInitial()
    {
        for (int r = 0; r < startRows; r++)
            for (int c = 0; c < cols; c++)
                SpawnBubble(BubbleColorExtensions.Random(colorCount), r, c);

        SpawnBonusBubbles();
    }

    /// <summary>Randomly upgrades some bubbles in the initial grid to bonus bubbles.</summary>
    private void SpawnBonusBubbles()
    {
        for (int r = 0; r < startRows; r++)
            for (int c = 0; c < cols; c++)
            {
                if (grid[r, c] == null) continue;
                if (Random.value > bonusBubbleChance) continue;

                var bonus = grid[r, c].gameObject.AddComponent<BonusBubble>();
                bonus.Init(grid[r, c].ColorType, 5);
            }
    }

    /// <summary>Pops a bonus bubble that was directly hit by a matching projectile,
    /// then checks for newly floating bubbles.</summary>
    public void RemoveBonusBubble(Bubble b)
    {
        if (b == null) return;
        StartCoroutine(BonusPopRoutine(b));
    }

    private IEnumerator BonusPopRoutine(Bubble b)
    {
        int r = b.Row, c = b.Col;
        if (Valid(r, c) && grid[r, c] == b)
            grid[r, c] = null;
        b.Pop();

        yield return new WaitForSeconds(0.3f);
        DropFloating();
        BubbleGameManager.Instance?.CheckEnd();
    }

    private void SpawnBubble(BubbleColor color, int r, int c)
    {
        if (!Valid(r, c) || grid[r, c] != null) return;

        var go = new GameObject($"Bubble_{r}_{c}");
        go.transform.SetParent(transform);
        go.transform.position = ToWorld(r, c);
        go.transform.localScale = Vector3.one * diameter;

        var sr = go.AddComponent<SpriteRenderer>();
        int colorIndex = (int)color;
        bool hasSpriteForColor = bubbleSprites != null && colorIndex < bubbleSprites.Length && bubbleSprites[colorIndex] != null;
        sr.sprite = hasSpriteForColor ? bubbleSprites[colorIndex] : SpriteGenerator.Circle();
        sr.sortingOrder = 0;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;

        var b = go.AddComponent<Bubble>();
        b.Init(color, r, c, hasSpriteForColor);
        grid[r, c] = b;
    }
}
