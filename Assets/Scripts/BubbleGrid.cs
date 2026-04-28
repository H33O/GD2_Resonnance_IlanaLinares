using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gère la grille hexagonale : placement, matching (BFS) et chute des bulles flottantes.
/// La grille remplit tout l'espace visible au-dessus du canon et ne descend jamais.
/// </summary>
public class BubbleGrid : MonoBehaviour
{
    public static BubbleGrid Instance { get; private set; }

    [Header("Grille")]
    [SerializeField] private int   cols     = 9;
    [SerializeField] private int   maxRows  = 20;
    [SerializeField] private float diameter = 0.5f;
    [Tooltip("Nombre minimum de bulles de même couleur connectées pour déclencher une explosion. " +
             "2 = le projectile + 1 voisin suffisent.")]
    [SerializeField] private int   minMatch = 2;

    [Header("Position")]
    [Tooltip("Marge en unités monde entre le bord supérieur de la caméra et la rangée 0.")]
    [SerializeField] private float topOffset    = 0.1f;

    [Tooltip("Hauteur réservée en bas de l'écran pour le canon (doit correspondre à BubbleShooter).")]
    [SerializeField] private float shooterZoneH = 3.2f;

    [Header("Couleurs actives")]
    [SerializeField] private int colorCount = 4;

    [Header("Sprites par couleur")]
    [Tooltip("Sprites indexés par BubbleColor : 0=Red, 1=Blue, 2=Green, 3=Yellow, 4=Purple")]
    [SerializeField] private Sprite[] bubbleSprites;

    [Header("Bulles bonus")]
    [SerializeField] private float bonusBubbleChance = 0.08f;

    // ── Accesseurs publics ────────────────────────────────────────────────────

    public float Diameter    => diameter;
    public int   ColorCount  => colorCount;

    /// <summary>
    /// Y monde du premier slot VIDE sous la grille initiale.
    /// C'est là que le premier projectile peut atterrir.
    /// Utilisé par <see cref="BubbleProjectile"/> pour calculer son seuil de détection.
    /// </summary>
    public float SpawnedBottomY => topY - spawnedRows * rowH;

    /// <summary>Retourne le sprite associé à une couleur, ou null si non configuré.</summary>
    public Sprite GetSprite(BubbleColor color)
    {
        int idx = (int)color;
        if (bubbleSprites == null || idx >= bubbleSprites.Length) return null;
        return bubbleSprites[idx];
    }

    // ── État interne ──────────────────────────────────────────────────────────

    private Bubble[,] grid;
    private float     topY, startX, rowH, radius;
    private int       spawnedRows;          // nombre de rangées réellement spawn
    private int       fallingBubbleCount;
    private bool      isTransitioning;      // vrai pendant un changement de niveau

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        Instance = this;
        radius   = diameter * 0.5f;
        rowH     = diameter * Mathf.Sqrt(3f) * 0.5f;
        grid     = new Bubble[maxRows, cols];
    }

    private void Start()
    {
        ComputeLayout();
        // Si un BubbleLevelManager est présent, c'est lui qui se charge du spawn initial via ApplyLevel(0).
        // On évite le double spawn en ne l'appelant qu'en l'absence de level manager.
        if (BubbleLevelManager.Instance == null)
            SpawnInitial();
    }

    // ── Calcul du layout ──────────────────────────────────────────────────────

    /// <summary>
    /// Calcule <c>topY</c> et <c>startX</c> à partir des dimensions de la caméra.
    /// Appelé dans Start et dans ApplyLevelData (pour recalculer après changement de caméra).
    /// </summary>
    private void ComputeLayout()
    {
        Camera cam = Camera.main;
        float  h   = cam.orthographicSize;

        // Rangée 0 : son centre est à 'radius' sous le bord supérieur + marge configurée.
        topY   = h - topOffset - radius;

        // Centrage horizontal exact : col 0 au centre gauche, col (cols-1) au centre droit.
        startX = -(cols - 1) * diameter * 0.5f;
    }

    // ── API publique ──────────────────────────────────────────────────────────

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
        StartCoroutine(MatchRoutine(r, c, color));
    }

    /// <summary>Retourne true uniquement quand la grille est vide et aucune bulle ne tombe.</summary>
    public bool IsEmpty()
    {
        if (fallingBubbleCount > 0) return false;
        for (int r = 0; r < maxRows; r++)
            for (int c = 0; c < cols; c++)
                if (grid[r, c] != null) return false;
        return true;
    }

    /// <summary>
    /// Applique les paramètres d'un niveau et respawn la grille complète.
    /// Appelé par <see cref="BubbleLevelManager"/> au démarrage de chaque niveau.
    /// </summary>
    public void ApplyLevelData(BubbleLevelData data)
    {
        if (data == null) return;

        colorCount        = Mathf.Clamp(data.colorCount, 2, 5);
        bonusBubbleChance = data.bonusBubbleChance;
        bubbleSprites     = data.bubbleSprites;

        ClearGrid();
        ComputeLayout();
        SpawnInitial();

        // Autorise à nouveau la détection de grille vide.
        isTransitioning = false;
    }

    /// <summary>Appelé par <see cref="Bubble"/> quand son animation de chute est terminée.</summary>
    public void OnBubbleFallComplete()
    {
        fallingBubbleCount = Mathf.Max(0, fallingBubbleCount - 1);
    }

    // ── Coordonnées ───────────────────────────────────────────────────────────

    /// <summary>Convertit une position de grille (rangée, colonne) en coordonnées monde.</summary>
    public Vector3 ToWorld(int r, int c)
    {
        float x = startX + c * diameter + (r % 2 == 1 ? radius : 0f);
        float y = topY - r * rowH;
        return new Vector3(x, y, 0f);
    }

    private (int, int) NearestEmpty(Vector3 worldPos)
    {
        // Cherche la cellule vide la plus proche, à condition qu'elle soit ancrée :
        // en rangée 0, ou adjacente à une bulle existante.
        float best = float.MaxValue;
        int   br = -1, bc = -1;

        for (int r = 0; r < maxRows; r++)
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
        return (br, bc);
    }

    // ── Matching ──────────────────────────────────────────────────────────────

    private IEnumerator MatchRoutine(int r, int c, BubbleColor color)
    {
        yield return null;  // attendre un frame pour que Bubble.Awake() s'exécute

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
        var result  = new List<(int, int)>();
        var visited = new HashSet<(int, int)>();
        var queue   = new Queue<(int, int)>();
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
        {
            if (grid[r, c] != null && !anchored.Contains((r, c)))
            {
                fallingBubbleCount++;
                grid[r, c].Fall();
                grid[r, c] = null;
                BubbleGameManager.Instance?.AddScore(20);
                dropCount++;
            }
        }

        if (dropCount >= 1)
        {
            BubbleGameManager.Instance?.ShowPerfect(dropCount);
            float intensity = Mathf.Lerp(0.05f, 0.18f, Mathf.Clamp01(dropCount / 10f));
            BubbleGameManager.Instance?.ShakeCamera(0.3f, intensity);
        }

        StartCoroutine(CheckGridClearedRoutine());
    }

    private IEnumerator CheckGridClearedRoutine()
    {
        yield return new WaitUntil(() => fallingBubbleCount <= 0);
        // Ne pas déclencher OnGridCleared pendant une transition de niveau.
        if (!isTransitioning && IsEmpty())
            BubbleLevelManager.Instance?.OnGridCleared();
    }

    /// <summary>
    /// BFS depuis le plafond (rangée 0) : retourne toutes les bulles connectées.
    /// Toute bulle absente de ce set est "floating" et doit tomber.
    /// </summary>
    private HashSet<(int, int)> CeilingBFS()
    {
        var visited = new HashSet<(int, int)>();
        var queue   = new Queue<(int, int)>();

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
            yield return (r - 1, c);     yield return (r - 1, c + 1);
            yield return (r + 1, c);     yield return (r + 1, c + 1);
        }
    }

    private bool Valid(int r, int c) => r >= 0 && r < maxRows && c >= 0 && c < cols;

    // ── Spawn ─────────────────────────────────────────────────────────────────

    private void SpawnInitial()
    {
        Camera cam = Camera.main;
        float  h   = cam.orthographicSize;

        // Espace total disponible entre le plafond et le haut de la zone canon.
        float bottomLimit = -h + shooterZoneH;
        float available   = (topY + radius) - bottomLimit;

        // On remplit 60 % de la hauteur disponible.
        // Les 40 % restants forment une zone d'atterrissage vide et visible
        // où le projectile peut se coller sans sortir de l'écran.
        int maxVisible  = Mathf.FloorToInt(available / rowH);
        spawnedRows     = Mathf.Clamp(Mathf.FloorToInt(maxVisible * 0.60f), 1, maxRows);

        for (int r = 0; r < spawnedRows; r++)
            for (int c = 0; c < cols; c++)
                SpawnBubble(BubbleColorExtensions.Random(colorCount), r, c);

        SpawnBonusBubbles();
    }

    /// <summary>Convertit aléatoirement certaines bulles du spawn initial en bulles bonus.</summary>
    private void SpawnBonusBubbles()
    {
        for (int r = 0; r < spawnedRows; r++)
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

    /// <summary>Détruit tous les GameObjects Bubble et réinitialise le tableau logique.</summary>
    private void ClearGrid()
    {
        isTransitioning = true;
        StopAllCoroutines();

        for (int r = 0; r < maxRows; r++)
        for (int c = 0; c < cols; c++)
        {
            if (grid[r, c] != null) Destroy(grid[r, c].gameObject);
            grid[r, c] = null;
        }
        fallingBubbleCount = 0;
        spawnedRows        = 0;
    }

    private void SpawnBubble(BubbleColor color, int r, int c)
    {
        if (!Valid(r, c) || grid[r, c] != null) return;

        var go = new GameObject($"Bubble_{r}_{c}");
        go.transform.SetParent(transform);
        go.transform.position   = ToWorld(r, c);
        go.transform.localScale = Vector3.one * diameter;

        var  sr           = go.AddComponent<SpriteRenderer>();
        int  colorIndex   = (int)color;
        bool hasSprite    = bubbleSprites != null
                            && colorIndex < bubbleSprites.Length
                            && bubbleSprites[colorIndex] != null;
        sr.sprite       = hasSprite ? bubbleSprites[colorIndex] : SpriteGenerator.Circle();
        sr.sortingOrder = 0;

        var col    = go.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;

        var b = go.AddComponent<Bubble>();
        b.Init(color, r, c, hasSprite);
        grid[r, c] = b;
    }
}
