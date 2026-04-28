using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Configure la scène Minijeu-Bulles avec la même direction artistique que le menu :
/// fond quasi-noir, grille translucide, palette noir et blanc, formes rondes.
/// S'attache à n'importe quel GameObject de la scène — aucun sprite externe requis.
/// </summary>
public class BubbleSceneSetup : MonoBehaviour
{
    // ── Palette ───────────────────────────────────────────────────────────────

    private static readonly Color ColBg        = new Color(0.05f, 0.05f, 0.05f, 1f);
    private static readonly Color ColGrid       = new Color(1f, 1f, 1f, 0.04f);
    private static readonly Color ColPanelBg    = new Color(0f, 0f, 0f, 0.35f);

    /// <summary>Conservé pour compatibilité avec BubbleLevelData.</summary>
    public enum BackgroundFit { Fill, Contain }

    // ── Instance courante ─────────────────────────────────────────────────────

    private static BubbleSceneSetup _instance;
    private GameObject _bgObject;

    /// <summary>Accès à l'effet d'anomalie pour <see cref="BubbleLevelManager"/>.</summary>
    public static BubbleAnomaly Anomaly { get; private set; }

    private void Awake() => _instance = this;

    private void Start()
    {
        if (Camera.main != null)
            Camera.main.backgroundColor = ColBg;

        _bgObject = BuildWorldBackground();
        BuildGrid();
        SpawnFireflies();
        SpawnAnomaly();
    }

    // ── Lucioles ──────────────────────────────────────────────────────────────

    private void SpawnFireflies()
    {
        var go = new GameObject("BubbleFireflies");
        go.transform.SetParent(transform, false);
        go.AddComponent<BubbleFireflies>().Init();
    }

    // ── Anomalie ──────────────────────────────────────────────────────────────

    private void SpawnAnomaly()
    {
        var go = new GameObject("BubbleAnomaly");
        go.transform.SetParent(transform, false);
        Anomaly = go.AddComponent<BubbleAnomaly>();
        Anomaly.Init();
        Anomaly.SetLevel(1);
    }

    // ── Fond monde (carré noir semi-opaque) ───────────────────────────────────

    private static GameObject BuildWorldBackground()
    {
        Camera cam = Camera.main;
        if (cam == null) return null;

        float h  = cam.orthographicSize * 2f;
        float w  = h * cam.aspect;
        Vector3 cp = cam.transform.position;

        var go = new GameObject("BubbleBG");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = SpriteGenerator.CreateWhiteSquare();
        sr.color        = ColBg;
        sr.sortingOrder = -20;
        go.transform.localScale = new Vector3(w, h, 1f);
        go.transform.position   = new Vector3(cp.x, cp.y, 1f);
        return go;
    }

    // ── API statique pour le LevelManager ─────────────────────────────────────

    /// <summary>
    /// Réinitialise le fond (plus de sprite externe — fond noir pur uniquement).
    /// Conservé pour compatibilité avec <see cref="BubbleLevelManager"/>.
    /// </summary>
    public static void ApplyBackground(BubbleLevelData data)
    {
        if (_instance == null) return;
        if (_instance._bgObject != null) Destroy(_instance._bgObject);
        if (Camera.main != null) Camera.main.backgroundColor = ColBg;
        _instance._bgObject = BuildWorldBackground();
    }

    // ── Grille monde ──────────────────────────────────────────────────────────

    private void BuildGrid()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        float h  = cam.orthographicSize * 2f;
        float w  = h * cam.aspect;
        Vector3 cp = cam.transform.position;

        var root = new GameObject("BubbleGrid");

        // 5 lignes verticales
        for (int i = 1; i <= 5; i++)
        {
            float xNorm = i / 6f;
            float x     = cp.x - w * 0.5f + w * xNorm;
            MakeWorldLine(root.transform, new Vector3(x, cp.y, 0f), true, h);
        }

        // 9 lignes horizontales
        for (int i = 1; i <= 9; i++)
        {
            float yNorm = i / 10f;
            float y     = cp.y - h * 0.5f + h * yNorm;
            MakeWorldLine(root.transform, new Vector3(cp.x, y, 0f), false, w);
        }
    }

    private static void MakeWorldLine(Transform parent, Vector3 pos, bool vertical, float length)
    {
        var go = new GameObject(vertical ? "VLine" : "HLine");
        go.transform.SetParent(parent, false);
        go.transform.position = pos;

        var sr          = go.AddComponent<SpriteRenderer>();
        sr.sprite       = SpriteGenerator.CreateCircle(4);
        sr.color        = ColGrid;
        sr.sortingOrder = -15;

        go.transform.localScale = vertical
            ? new Vector3(0.02f, length, 1f)
            : new Vector3(length, 0.02f, 1f);
    }
}
