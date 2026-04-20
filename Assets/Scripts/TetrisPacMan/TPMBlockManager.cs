using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gère le pool visuel des blocs posés par le joueur.
/// Chaque cellule PlayerBlock a un GameObject associé avec la couleur choisie dans la palette.
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
    private readonly Dictionary<Vector2Int, Color>      blockColors  = new();

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

    /// <summary>
    /// Crée et anime un bloc coloré à la position de grille donnée.
    /// </summary>
    public void SpawnBlock(int x, int y, Color color)
    {
        var key = new Vector2Int(x, y);
        if (blockObjects.ContainsKey(key)) return;

        Vector3 worldPos  = TPMGrid.Instance.CellToWorld(x, y);
        var     go        = BuildBlockVisual(worldPos, color);
        blockObjects[key] = go;
        blockColors[key]  = color;

        StartCoroutine(ScaleIn(go.transform, settings.blockAnimDuration));
    }

    /// <summary>Surcharge sans couleur — utilise la couleur active de la palette.</summary>
    public void SpawnBlock(int x, int y)
    {
        Color c = TPMBlockPalette.Instance != null
            ? TPMBlockPalette.Instance.SelectedColor
            : TPMBlockPalette.BlockColors[0];
        SpawnBlock(x, y, c);
    }

    /// <summary>Détruit et anime la suppression du bloc à la position donnée.</summary>
    public void RemoveBlock(int x, int y)
    {
        var key = new Vector2Int(x, y);
        if (!blockObjects.TryGetValue(key, out var go)) return;

        blockObjects.Remove(key);
        blockColors.Remove(key);
        StartCoroutine(ScaleOutAndDestroy(go, settings.blockAnimDuration));
    }

    /// <summary>Retourne la couleur du bloc posé à cette position, ou blanc si absent.</summary>
    public Color GetBlockColor(int x, int y)
    {
        var key = new Vector2Int(x, y);
        return blockColors.TryGetValue(key, out var c) ? c : Color.white;
    }

    // ── Création visuelle ─────────────────────────────────────────────────────

    private GameObject BuildBlockVisual(Vector3 worldPos, Color color)
    {
        var root = new GameObject("Block");
        root.transform.position   = worldPos;
        root.transform.localScale = Vector3.zero;

        float cs = settings.cellSize * 0.88f;

        // Bord plus foncé
        var edgeColor = new Color(color.r * 0.62f, color.g * 0.62f, color.b * 0.62f, 1f);
        var edgeGO    = new GameObject("Edge");
        edgeGO.transform.SetParent(root.transform, false);
        edgeGO.transform.localScale = Vector3.one * (cs * 1.07f);
        var edgeSR    = edgeGO.AddComponent<SpriteRenderer>();
        edgeSR.sprite = SpriteGenerator.CreateColoredSquare(edgeColor);
        edgeSR.sortingOrder = 4;

        // Corps coloré
        var bodyGO    = new GameObject("Body");
        bodyGO.transform.SetParent(root.transform, false);
        bodyGO.transform.localScale = Vector3.one * cs;
        var bodySR    = bodyGO.AddComponent<SpriteRenderer>();
        bodySR.sprite = SpriteGenerator.CreateColoredSquare(color);
        bodySR.sortingOrder = 5;

        // Reflet blanc haut-gauche
        var shineGO   = new GameObject("Shine");
        shineGO.transform.SetParent(root.transform, false);
        shineGO.transform.localScale    = Vector3.one * (cs * 0.35f);
        shineGO.transform.localPosition = new Vector3(-cs * 0.22f, cs * 0.22f, 0f);
        var shineSR   = shineGO.AddComponent<SpriteRenderer>();
        shineSR.sprite = SpriteGenerator.CreateColoredSquare(Color.white);
        shineSR.color  = new Color(1f, 1f, 1f, 0.22f);
        shineSR.sortingOrder = 6;

        // Halo coloré
        var glowGO    = new GameObject("Glow");
        glowGO.transform.SetParent(root.transform, false);
        glowGO.transform.localScale = Vector3.one * (cs * 1.4f);
        var glowSR    = glowGO.AddComponent<SpriteRenderer>();
        glowSR.sprite = SpriteGenerator.CreateCircle(64);
        glowSR.color  = new Color(color.r, color.g, color.b, 0.18f);
        glowSR.sortingOrder = 3;

        // Pulsation du halo
        var pulse = root.AddComponent<TPMBlockGlow>();
        pulse.glowSR    = glowSR;
        pulse.baseColor = color;

        return root;
    }

    // ── Animations ────────────────────────────────────────────────────────────

    private static IEnumerator ScaleIn(Transform t, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float s = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            t.localScale = Vector3.one * s;
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    private static IEnumerator ScaleOutAndDestroy(GameObject go, float duration)
    {
        if (go == null) yield break;
        float elapsed    = 0f;
        Vector3 startScale = go.transform.localScale;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float s = 1f - Mathf.SmoothStep(0f, 1f, elapsed / duration);
            if (go != null) go.transform.localScale = startScale * s;
            yield return null;
        }
        if (go != null) Destroy(go);
    }
}

/// <summary>Pulsation du halo coloré de chaque bloc.</summary>
public class TPMBlockGlow : MonoBehaviour
{
    public SpriteRenderer glowSR;
    public Color          baseColor;

    private void Update()
    {
        if (glowSR == null) return;
        float a = 0.12f + 0.10f * Mathf.Sin(Time.time * 2.5f + transform.position.x);
        glowSR.color = new Color(baseColor.r, baseColor.g, baseColor.b, a);
    }
}
