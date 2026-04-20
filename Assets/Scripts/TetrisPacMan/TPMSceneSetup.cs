using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Construit la scène du mini-jeu Tetris×Pac-Man (prototype PC) :
/// caméra, fond, grille 12×18, managers, joueur, monstre en cage, HUD, écran de fin.
/// </summary>
[DefaultExecutionOrder(-100)]
public class TPMSceneSetup : MonoBehaviour
{
    [Header("Assets (assign in Inspector)")]
    public TPMSettings settings;

    private Camera             gameCamera;
    private TPMGrid            grid;
    private TPMGameManager     gameManager;
    private TPMBlockManager    blockManager;
    private TPMFeedbackManager feedback;
    private TPMGridRenderer    gridRenderer;
    private TPMTetrisSpawner   tetrisSpawner;
    private TPMPlayerController player;
    private TPMMonster          monster;
    private TPMHUD              hud;
    private TPMGameOverUI       gameOverUI;

    private void Awake()
    {
        BuildCamera();
        BuildBackground();
        BuildManagers();
        BuildGrid();
        BuildTetrisSpawner();
        BuildPlayer();
        BuildMonster();
        BuildHUD();
        BuildGameOverUI();
    }

    // ── Caméra ────────────────────────────────────────────────────────────────

    private void BuildCamera()
    {
        var camGO = new GameObject("Camera");
        gameCamera = camGO.AddComponent<Camera>();
        gameCamera.orthographic     = true;
        // orthographicSize = demi-hauteur en unités monde.
        // Grille 10×20 avec cellSize=1.0 → hauteur monde = 20u.
        // On ajoute 0.5u de marge en haut/bas → taille totale 21u → orthographicSize = 10.5.
        // La caméra est légèrement décalée vers le haut pour laisser de la place au HUD.
        gameCamera.orthographicSize   = 11.5f;
        gameCamera.clearFlags         = CameraClearFlags.SolidColor;
        gameCamera.backgroundColor    = new Color(0.05f, 0.05f, 0.10f, 1f);
        // Décalage vertical : centre la grille avec une légère remontée pour le HUD bas
        gameCamera.transform.position = new Vector3(0f, 0.5f, -10f);
        camGO.AddComponent<AudioListener>();
    }

    // ── Fond ──────────────────────────────────────────────────────────────────

    private void BuildBackground()
    {
        var bgGO  = new GameObject("Background");
        var bgSR  = bgGO.AddComponent<SpriteRenderer>();
        bgSR.sprite = SpriteGenerator.CreateColoredSquare(new Color(0.05f, 0.05f, 0.10f, 1f));
        bgSR.sortingOrder = -20;
        bgGO.transform.localScale = Vector3.one * 30f;
    }

    // ── Managers ──────────────────────────────────────────────────────────────

    private void BuildManagers()
    {
        var gmGO    = new GameObject("GameManager");
        gameManager = gmGO.AddComponent<TPMGameManager>();
        gameManager.settings = settings;

        var bmGO    = new GameObject("BlockManager");
        blockManager = bmGO.AddComponent<TPMBlockManager>();
        blockManager.settings = settings;

        var fbGO    = new GameObject("FeedbackManager");
        feedback    = fbGO.AddComponent<TPMFeedbackManager>();

        var wCanvasGO = new GameObject("WorldCanvas");
        var wCanvas   = wCanvasGO.AddComponent<Canvas>();
        wCanvas.renderMode   = RenderMode.WorldSpace;
        wCanvas.worldCamera  = gameCamera;
        wCanvas.sortingOrder = 30;
        wCanvasGO.AddComponent<CanvasScaler>();
        wCanvasGO.AddComponent<GraphicRaycaster>();
        wCanvasGO.GetComponent<RectTransform>().localScale = Vector3.one * 0.01f;
        wCanvasGO.GetComponent<RectTransform>().sizeDelta  = new Vector2(1080f, 1920f);
        feedback.worldCanvas = wCanvas;
    }

    // ── Grille ────────────────────────────────────────────────────────────────

    private void BuildGrid()
    {
        var gridGO    = new GameObject("Grid");
        grid          = gridGO.AddComponent<TPMGrid>();
        grid.settings = settings;
        grid.Init();

        var rendererGO  = new GameObject("GridRenderer");
        gridRenderer    = rendererGO.AddComponent<TPMGridRenderer>();
        gridRenderer.settings = settings;
        gridRenderer.Init();
    }

    // ── Tetris Spawner ────────────────────────────────────────────────────────

    private void BuildTetrisSpawner()
    {
        var spawnGO    = new GameObject("TetrisSpawner");
        tetrisSpawner  = spawnGO.AddComponent<TPMTetrisSpawner>();
        tetrisSpawner.settings = settings;
    }

    // ── Joueur ────────────────────────────────────────────────────────────────

    private void BuildPlayer()
    {
        var pGO    = new GameObject("Player");
        pGO.tag    = "Player";
        pGO.AddComponent<SpriteRenderer>();
        player     = pGO.AddComponent<TPMPlayerController>();
        player.settings = settings;
    }

    // ── Monstre ───────────────────────────────────────────────────────────────

    private void BuildMonster()
    {
        var mGO      = new GameObject("Monster");
        monster      = mGO.AddComponent<TPMMonster>();
        monster.settings = settings;
        monster.player   = player;
    }

    // ── HUD ───────────────────────────────────────────────────────────────────

    private void BuildHUD()
    {
        var hudCanvas = BuildUICanvas("HUDCanvas", 100);

        // ── EventSystem ───────────────────────────────────────────────────────
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        var hudGO = new GameObject("HUD");
        hudGO.transform.SetParent(hudCanvas.transform, false);
        hud = hudGO.AddComponent<TPMHUD>();

        // ── Bandeau supérieur ─────────────────────────────────────────────────
        var topBar    = MakePanel(hudCanvas.transform, "TopBar",
            new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero,
            new Color(0f, 0f, 0f, 0.80f), 0);
        topBar.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 90f);

        // SCORE (gauche)
        var scoreLabel  = MakeLabel(topBar.transform, "ScoreLabel", "SCORE: 0",
            28f, Color.white, TextAlignmentOptions.Left);
        var slRT        = scoreLabel.GetComponent<RectTransform>();
        slRT.anchorMin  = new Vector2(0f, 0f); slRT.anchorMax = new Vector2(0.38f, 1f);
        slRT.pivot      = new Vector2(0f, 0.5f);
        slRT.offsetMin  = new Vector2(18f, 0f); slRT.offsetMax = Vector2.zero;

        // COUPS (centre-droite)
        var movesLabel  = MakeLabel(topBar.transform, "MovesLabel", $"COUPS: {settings.startingMoves}",
            28f, Color.white, TextAlignmentOptions.Right);
        var mlRT        = movesLabel.GetComponent<RectTransform>();
        mlRT.anchorMin  = new Vector2(0.38f, 0f); mlRT.anchorMax = new Vector2(0.72f, 1f);
        mlRT.pivot      = new Vector2(1f, 0.5f);
        mlRT.offsetMin  = Vector2.zero; mlRT.offsetMax = new Vector2(-4f, 0f);

        // Icône dorée
        var iconGO      = new GameObject("CoinIcon");
        iconGO.transform.SetParent(topBar.transform, false);
        var iconRT      = iconGO.AddComponent<RectTransform>();
        iconRT.anchorMin = iconRT.anchorMax = new Vector2(0.72f, 0.5f);
        iconRT.pivot     = new Vector2(0f, 0.5f);
        iconRT.sizeDelta = new Vector2(30f, 30f);
        var iconImg     = iconGO.AddComponent<Image>();
        iconImg.sprite  = SpriteGenerator.CreateCircle(32);
        iconImg.color   = new Color(1f, 0.82f, 0.05f, 1f);

        // ── Zone NEXT BLOC (droite du bandeau) ────────────────────────────────
        var nextContainer = new GameObject("NextContainer");
        nextContainer.transform.SetParent(topBar.transform, false);
        var ncRT          = nextContainer.AddComponent<RectTransform>();
        ncRT.anchorMin    = new Vector2(0.75f, 0f);
        ncRT.anchorMax    = new Vector2(1f, 1f);
        ncRT.offsetMin    = Vector2.zero;
        ncRT.offsetMax    = new Vector2(-8f, 0f);

        // Label "NEXT"
        var nextLabelGO   = new GameObject("NextLabel");
        nextLabelGO.transform.SetParent(nextContainer.transform, false);
        var nlRT          = nextLabelGO.AddComponent<RectTransform>();
        nlRT.anchorMin    = new Vector2(0f, 0.6f); nlRT.anchorMax = new Vector2(1f, 1f);
        nlRT.offsetMin    = nlRT.offsetMax = Vector2.zero;
        var nextTMP       = nextLabelGO.AddComponent<TextMeshProUGUI>();
        nextTMP.text      = "NEXT";
        nextTMP.fontSize  = 16f;
        nextTMP.color     = new Color(0.7f, 0.7f, 0.8f, 1f);
        nextTMP.alignment = TextAlignmentOptions.Center;
        nextTMP.fontStyle = FontStyles.Bold;

        // Grille 4×2 pour la pièce suivante
        var nextGridGO    = new GameObject("NextPieceGrid");
        nextGridGO.transform.SetParent(nextContainer.transform, false);
        var nextGridRT    = nextGridGO.AddComponent<RectTransform>();
        nextGridRT.anchorMin    = new Vector2(0f, 0f);
        nextGridRT.anchorMax    = new Vector2(1f, 0.58f);
        nextGridRT.offsetMin    = nextGridRT.offsetMax = Vector2.zero;

        // ── Contrôles (bas d'écran) ───────────────────────────────────────────
        var ctrlLabel   = MakeLabel(hudCanvas.transform, "Controls",
            "ZQSD: déplacer   Espace: détruire bloc\n← →: bloc Tetris   ↑: rotation   ↓: soft drop",
            14f, new Color(0.55f, 0.55f, 0.60f, 0.85f), TextAlignmentOptions.Center);
        var ctrlRT      = ctrlLabel.GetComponent<RectTransform>();
        ctrlRT.anchorMin = new Vector2(0f, 0f); ctrlRT.anchorMax = new Vector2(1f, 0f);
        ctrlRT.pivot     = new Vector2(0.5f, 0f);
        ctrlRT.sizeDelta = new Vector2(0f, 44f);
        ctrlRT.anchoredPosition = new Vector2(0f, 6f);
        ctrlLabel.enableWordWrapping = true;

        hud.Init(scoreLabel, movesLabel, settings.startingMoves, nextGridRT, settings);
    }

    // ── Game over UI ──────────────────────────────────────────────────────────

    private void BuildGameOverUI()
    {
        var goCanvas  = BuildUICanvas("GameOverCanvas", 200);
        var goUIGO    = new GameObject("GameOverUI");
        goUIGO.transform.SetParent(goCanvas.transform, false);
        gameOverUI    = goUIGO.AddComponent<TPMGameOverUI>();

        var bgPanel   = MakePanel(goCanvas.transform, "Background",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color(0.1f, 0.55f, 0.2f, 0f), 0);
        var bgImg     = bgPanel.GetComponent<Image>();

        var title     = MakeLabel(goCanvas.transform, "Title", "TITRE",
            52f, Color.white, TextAlignmentOptions.Center);
        var tRT       = title.GetComponent<RectTransform>();
        tRT.anchorMin = tRT.anchorMax = new Vector2(0.5f, 0.6f);
        tRT.pivot     = new Vector2(0.5f, 0.5f);
        tRT.sizeDelta = new Vector2(600f, 80f);

        var scoreF    = MakeLabel(goCanvas.transform, "FinalScore", "SCORE  000000",
            30f, new Color(0.9f, 0.9f, 1f, 1f), TextAlignmentOptions.Center);
        var sRT       = scoreF.GetComponent<RectTransform>();
        sRT.anchorMin = sRT.anchorMax = new Vector2(0.5f, 0.45f);
        sRT.pivot     = new Vector2(0.5f, 0.5f);
        sRT.sizeDelta = new Vector2(400f, 50f);

        var btnGO     = new GameObject("RestartButton");
        btnGO.transform.SetParent(goCanvas.transform, false);
        var btnRT     = btnGO.AddComponent<RectTransform>();
        btnRT.anchorMin = btnRT.anchorMax = new Vector2(0.5f, 0.3f);
        btnRT.pivot     = new Vector2(0.5f, 0.5f);
        btnRT.sizeDelta = new Vector2(220f, 55f);
        btnGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.18f);
        var btn       = btnGO.AddComponent<Button>();

        var lblGO     = new GameObject("Label");
        lblGO.transform.SetParent(btnGO.transform, false);
        var lblRT     = lblGO.AddComponent<RectTransform>();
        lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;
        var lblTMP    = lblGO.AddComponent<TextMeshProUGUI>();
        lblTMP.text   = "REJOUER"; lblTMP.fontSize = 24f;
        lblTMP.color  = Color.white; lblTMP.alignment = TextAlignmentOptions.Center;

        gameOverUI.Init(goCanvas, bgImg, title, scoreF, btn);
    }

    // ── Helpers UI ────────────────────────────────────────────────────────────

    private Canvas BuildUICanvas(string name, int sortingOrder)
    {
        var go     = new GameObject(name);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight  = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private GameObject MakePanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax, Color color, int _)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt  = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
        go.AddComponent<Image>().color = color;
        return go;
    }

    private TextMeshProUGUI MakeLabel(Transform parent, string name,
        string text, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt  = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(300f, 40f);
        var tmp      = go.AddComponent<TextMeshProUGUI>();
        tmp.text     = text; tmp.fontSize  = fontSize;
        tmp.color    = color; tmp.alignment = alignment; tmp.fontStyle = FontStyles.Bold;
        return tmp;
    }
}
