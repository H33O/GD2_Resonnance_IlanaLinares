using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gère le pool visuel des blocs posés par le joueur.
/// Chaque cellule <see cref="TPMGrid.CellType.PlayerBlock"/> a un GameObject associé.
/// </summary>
public class TPMBlockManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static TPMBlockManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Settings")]
    public TPMSettings settings;

    // ── État ──────────────────────────────────────────────────────────────────

    private readonly Dictionary<Vector2Int, GameObject> blockObjects = new();

    private static readonly Color BlockColor     = new Color(0.20f, 0.80f, 0.30f, 1.00f);
    private static readonly Color BlockEdge      = new Color(0.10f, 0.55f, 0.20f, 1.00f);
    private static readonly Color BlockGlowColor = new Color(0.10f, 0.90f, 0.25f, 0.15f);

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

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>Crée et anime un bloc à la position de grille donnée.</summary>
    public void SpawnBlock(int x, int y)
    {
        var key = new Vector2Int(x, y);
        if (blockObjects.ContainsKey(key)) return;

        Vector3 worldPos = TPMGrid.Instance.CellToWorld(x, y);
        var     go       = BuildBlockVisual(worldPos);
        blockObjects[key] = go;

        StartCoroutine(ScaleIn(go.transform, settings.blockAnimDuration));
    }

    /// <summary>Détruit et anime la suppression du bloc à la position donnée.</summary>
    public void RemoveBlock(int x, int y)
    {
        var key = new Vector2Int(x, y);
        if (!blockObjects.TryGetValue(key, out var go)) return;

        blockObjects.Remove(key);
        StartCoroutine(ScaleOutAndDestroy(go, settings.blockAnimDuration));
    }

    // ── Création visuelle ─────────────────────────────────────────────────────

    private GameObject BuildBlockVisual(Vector3 worldPos)
    {
        var root = new GameObject("Block");
        root.transform.position   = worldPos;
        root.transform.localScale = Vector3.zero;

        float cs = settings.cellSize * 0.88f;

        // Corps du bloc (carré arrondi simulé = carré + glow circulaire)
        var bodyGO  = new GameObject("Body");
        bodyGO.transform.SetParent(root.transform, false);
        bodyGO.transform.localScale = Vector3.one * cs;
        var bodySR  = bodyGO.AddComponent<SpriteRenderer>();
        bodySR.sprite = SpriteGenerator.CreateColoredSquare(BlockColor);
        bodySR.color  = BlockColor;
        bodySR.sortingOrder = 5;

        // Bord légèrement plus foncé
        var edgeGO  = new GameObject("Edge");
        edgeGO.transform.SetParent(root.transform, false);
        edgeGO.transform.localScale = Vector3.one * (cs * 1.06f);
        var edgeSR  = edgeGO.AddComponent<SpriteRenderer>();
        edgeSR.sprite = SpriteGenerator.CreateColoredSquare(BlockEdge);
        edgeSR.color  = BlockEdge;
        edgeSR.sortingOrder = 4;

        // Halo doux
        var glowGO  = new GameObject("Glow");
        glowGO.transform.SetParent(root.transform, false);
        glowGO.transform.localScale = Vector3.one * (cs * 1.3f);
        var glowSR  = glowGO.AddComponent<SpriteRenderer>();
        glowSR.sprite = SpriteGenerator.CreateCircle(64);
        glowSR.color  = BlockGlowColor;
        glowSR.sortingOrder = 3;

        // Pulsation permanente du halo
        root.AddComponent<TPMBlockGlow>().glowSR = glowSR;

        return root;
    }

    // ── Animations ────────────────────────────────────────────────────────────

    private static IEnumerator ScaleIn(Transform t, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float s  = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            t.localScale = Vector3.one * s;
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    private static IEnumerator ScaleOutAndDestroy(GameObject go, float duration)
    {
        if (go == null) yield break;

        float elapsed = 0f;
        Vector3 startScale = go.transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float s  = 1f - Mathf.SmoothStep(0f, 1f, elapsed / duration);
            if (go != null) go.transform.localScale = startScale * s;
            yield return null;
        }

        if (go != null) Destroy(go);
    }
}

/// <summary>Pulsation du halo de chaque bloc.</summary>
public class TPMBlockGlow : MonoBehaviour
{
    public SpriteRenderer glowSR;

    private static readonly Color GlowBase = new Color(0.10f, 0.90f, 0.25f, 0.12f);

    private void Update()
    {
        if (glowSR == null) return;
        float a    = 0.10f + 0.08f * Mathf.Sin(Time.time * 3f + transform.position.x);
        glowSR.color = new Color(GlowBase.r, GlowBase.g, GlowBase.b, a);
    }
}
