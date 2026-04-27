using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Configure la scène Minijeu-Bulles avec la même direction artistique que le menu :
/// fond quasi-noir, grille translucide, palette noir et blanc, formes rondes.
/// S'attache à n'importe quel GameObject de la scène — aucun sprite externe requis.
/// </summary>
public class BubbleSceneSetup : MonoBehaviour
{
    // ── Palette (identique au menu) ───────────────────────────────────────────

    private static readonly Color ColBg   = new Color(0.05f, 0.05f, 0.05f, 1f);
    private static readonly Color ColGrid = new Color(1f, 1f, 1f, 0.04f);

    // ── Configuration du fond ─────────────────────────────────────────────────

    [Header("Fond")]
    [Tooltip("Sprite de fond affiché derrière la grille. Laisse vide pour utiliser la couleur unie.")]
    [SerializeField] private Sprite backgroundSprite;

    [Tooltip("Couleur appliquée sur le sprite de fond (teinte) ou couleur unie si aucun sprite.")]
    [SerializeField] private Color backgroundColor = new Color(0.05f, 0.05f, 0.05f, 1f);

    [Tooltip("Mode de remplissage du sprite de fond.")]
    [SerializeField] private BackgroundFit backgroundFit = BackgroundFit.Fill;

    public enum BackgroundFit { Fill, Contain }

    // ── Instance courante (pour mise à jour runtime) ──────────────────────────

    private static BubbleSceneSetup _instance;
    private GameObject _bgObject;

    private void Awake() => _instance = this;

    private void Start()
    {
        // Caméra : fond correspondant à la couleur choisie
        if (Camera.main != null)
            Camera.main.backgroundColor = backgroundColor;

        _bgObject = BuildWorldBackground(backgroundSprite, backgroundColor, backgroundFit);
        BuildGrid();
    }

    // ── API statique pour le LevelManager ─────────────────────────────────────

    /// <summary>
    /// Applique le fond d'un niveau en remplaçant le fond existant.
    /// Peut être appelé depuis <see cref="BubbleLevelManager"/> à chaque changement de niveau.
    /// </summary>
    public static void ApplyBackground(BubbleLevelData data)
    {
        if (_instance == null || data == null) return;
        _instance.ApplyBackgroundInternal(data.backgroundSprite, data.backgroundColor, data.backgroundFit);
    }

    private void ApplyBackgroundInternal(Sprite sprite, Color color, BackgroundFit fit)
    {
        if (_bgObject != null) Destroy(_bgObject);
        if (Camera.main != null) Camera.main.backgroundColor = color;
        _bgObject = BuildWorldBackground(sprite, color, fit);
    }

    // ── Fond monde ────────────────────────────────────────────────────────────

    /// <summary>SpriteRenderer plein écran en arrière-plan, avec sprite ou couleur unie.</summary>
    private static GameObject BuildWorldBackground(Sprite sprite, Color color, BackgroundFit fit)
    {
        Camera cam = Camera.main;
        if (cam == null) return null;

        float h  = cam.orthographicSize * 2f;
        float w  = h * cam.aspect;
        Vector3 cp = cam.transform.position;

        var go = new GameObject("BubbleBG");
        var sr = go.AddComponent<SpriteRenderer>();

        if (sprite != null)
        {
            sr.sprite      = sprite;
            sr.color       = color;
            sr.drawMode    = SpriteDrawMode.Simple;

            float spriteW = sprite.bounds.size.x;
            float spriteH = sprite.bounds.size.y;
            float scaleX  = w / spriteW;
            float scaleY  = h / spriteH;
            float scale   = fit == BackgroundFit.Fill
                ? Mathf.Max(scaleX, scaleY)
                : Mathf.Min(scaleX, scaleY);

            go.transform.localScale = new Vector3(scale, scale, 1f);
        }
        else
        {
            sr.sprite       = SpriteGenerator.CreateCircle(4);
            sr.color        = color;
            go.transform.localScale = new Vector3(w, h, 1f);
        }

        sr.sortingOrder       = -20;
        go.transform.position = new Vector3(cp.x, cp.y, 1f);
        return go;
    }

    // ── Grille monde ──────────────────────────────────────────────────────────

    /// <summary>Lignes fines translucides dans l'espace monde, comme la grille du menu.</summary>
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
