using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Monte la hiérarchie UI du mini-jeu arène circulaire et initialise le GameManager.
/// Assigne une <see cref="CircleArenaConfig"/> dans l'Inspector pour tout régler sans code.
/// </summary>
public class CircleArenaSetup : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("ScriptableObject contenant tous les paramètres du jeu")]
    public CircleArenaConfig config;

    // ── Références construites au runtime ─────────────────────────────────────

    private Canvas        mainCanvas;
    private RectTransform ballRT;
    private RectTransform fxLayer;
    private Image         flashImage;
    private RectTransform uiLayer;

    // ── Cycle de vie ──────────────────────────────────────────────────────────

    private void Awake()
    {
        if (config == null)
        {
            Debug.LogError("[CircleArenaSetup] Aucune CircleArenaConfig assignée ! " +
                           "Crée-en une via Assets > Create > CircleArena > Config.");
            return;
        }

        EnsureEventSystem();
        BuildScene();

        var gm = gameObject.AddComponent<CircleArenaGameManager>();
        gm.Init(config, mainCanvas, ballRT, fxLayer, flashImage, uiLayer);
    }

    // ── Construction hiérarchie ───────────────────────────────────────────────

    private void BuildScene()
    {
        mainCanvas = BuildCanvas();

        BuildBackground();
        BuildArenaRing();
        fxLayer    = BuildLayer("FxLayer");
        ballRT     = BuildBall();
        flashImage = BuildFlashOverlay();
        uiLayer    = BuildLayer("UILayer");
    }

    private Canvas BuildCanvas()
    {
        var go     = new GameObject("ArenaCanvas");
        go.transform.SetParent(transform, false);

        var c      = go.AddComponent<Canvas>();
        c.renderMode   = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 10;

        var scaler                 = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight  = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return c;
    }

    private void BuildBackground()
    {
        var go  = new GameObject("Background");
        go.transform.SetParent(mainCanvas.transform, false);
        var img = go.AddComponent<Image>();
        img.sprite        = SpriteGenerator.CreateWhiteSquare();
        img.color         = config.backgroundColor;
        img.raycastTarget = false;
        Stretch(img.rectTransform);
    }

    private void BuildArenaRing()
    {
        var go  = new GameObject("ArenaRing");
        go.transform.SetParent(mainCanvas.transform, false);
        var img = go.AddComponent<Image>();

        img.sprite        = CreateRingSprite(512, config.arenaStrokeWidth, config.arenaRadius);
        img.color         = config.arenaColor;
        img.raycastTarget = false;

        var rt              = img.rectTransform;
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = Vector2.one * config.arenaRadius * 2f;
        rt.anchoredPosition = Vector2.zero;
    }

    private RectTransform BuildLayer(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(mainCanvas.transform, false);
        var rt = go.AddComponent<RectTransform>();
        Stretch(rt);
        return rt;
    }

    private RectTransform BuildBall()
    {
        var go  = new GameObject("ArenaBall");
        go.transform.SetParent(mainCanvas.transform, false);
        var img = go.AddComponent<Image>();
        img.sprite        = SpriteGenerator.Circle();
        img.color         = config.arenaColor;
        img.raycastTarget = false;

        var rt              = img.rectTransform;
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = Vector2.one * config.ballRadius * 2f;
        // Position initiale sur le bord intérieur du ring (angle 90°, soit en haut)
        float orbitR        = config.arenaRadius - config.ballRadius - config.arenaStrokeWidth * 0.5f - 2f;
        rt.anchoredPosition = new Vector2(0f, orbitR);
        return rt;
    }

    private Image BuildFlashOverlay()
    {
        var go  = new GameObject("FlashOverlay");
        go.transform.SetParent(mainCanvas.transform, false);
        var img = go.AddComponent<Image>();
        img.sprite        = SpriteGenerator.CreateWhiteSquare();
        img.color         = Color.clear;
        img.raycastTarget = false;
        Stretch(img.rectTransform);
        return img;
    }

    // ── Sprite ring procédural (épaisseur physique précise) ───────────────────

    /// <summary>
    /// Génère un sprite d'anneau antialiasé avec une épaisseur en pixels canvas.
    /// </summary>
    public static Sprite CreateRingSprite(int texSize, float strokePx, float radiusPx)
    {
        var tex        = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var pixels     = new Color[texSize * texSize];

        float center  = texSize * 0.5f;
        float scale   = center / radiusPx;
        float outerR  = center - 1f;
        float innerR  = outerR - strokePx * scale;

        for (int y = 0; y < texSize; y++)
        {
            for (int x = 0; x < texSize; x++)
            {
                float dist  = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f),
                                               new Vector2(center, center));
                float outer = Mathf.Clamp01(outerR - dist + 1.5f);
                float inner = Mathf.Clamp01(dist - innerR + 1.5f);
                pixels[y * texSize + x] = new Color(1f, 1f, 1f, outer * inner);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, texSize, texSize),
                             new Vector2(0.5f, 0.5f), texSize);
    }

    // ── Utilitaires ───────────────────────────────────────────────────────────

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }
}
