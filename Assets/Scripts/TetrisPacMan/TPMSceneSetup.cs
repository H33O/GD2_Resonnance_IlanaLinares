using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Construit la scène du mini-jeu Tetris×Pac-Man au démarrage :
/// caméra, fond, grille, managers, joueur, monstre, HUD et écran de fin.
/// Attacher à un GameObject "SceneSetup" vide.
/// </summary>
[DefaultExecutionOrder(-100)]
public class TPMSceneSetup : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Assets (assign in Inspector)")]
    public TPMSettings settings;

    // ── Références internes ───────────────────────────────────────────────────

    private Camera                gameCamera;
    private TPMGrid               grid;
    private TPMGameManager        gameManager;
    private TPMBlockManager       blockManager;
    private TPMFeedbackManager    feedback;
    private TPMGridRenderer       gridRenderer;
    private TPMPlayerController   player;
    private TPMMonster            monster;
    private TPMHUD                hud;
    private TPMGameOverUI         gameOverUI;

    // ── Entry point ───────────────────────────────────────────────────────────

    private void Awake()
    {
        BuildCamera();
        BuildBackground();
        BuildManagers();
        BuildGrid();
        BuildPlayer();
        BuildMonster();
        BuildHUD();
        BuildGameOverUI();
        WireReferences();
    }

    // ── Caméra ────────────────────────────────────────────────────────────────

    private void BuildCamera()
    {
        var camGO              = new GameObject("Camera");
        gameCamera             = camGO.AddComponent<Camera>();
        gameCamera.orthographic     = true;
        // 1080×1920 portrait : demi-hauteur = 1920 / (2 × 100 ppu) = 9.6 unités monde
        gameCamera.orthographicSize = 9.6f;
        gameCamera.clearFlags        = CameraClearFlags.SolidColor;
        gameCamera.backgroundColor   = new Color(0.04f, 0.04f, 0.08f, 1f);
        gameCamera.transform.position = new Vector3(0f, 0f, -10f);
        camGO.AddComponent<AudioListener>();
    }

    // ── Fond ──────────────────────────────────────────────────────────────────

    private void BuildBackground()
    {
        // Vignette sombre sur les bords
        var mat = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));

        var vignette            = new GameObject("Vignette");
        var sr                  = vignette.AddComponent<SpriteRenderer>();
        sr.sprite               = SpriteGenerator.CreateCircle(256);
        sr.color                = new Color(0f, 0f, 0f, 0f);
        sr.sortingOrder         = -10;
        vignette.transform.localScale = Vector3.one * 25f;
    }

    // ── Managers ──────────────────────────────────────────────────────────────

    private void BuildManagers()
    {
        // GameManager
        var gmGO       = new GameObject("GameManager");
        gameManager    = gmGO.AddComponent<TPMGameManager>();
        gameManager.settings = settings;

        // BlockManager
        var bmGO       = new GameObject("BlockManager");
        blockManager   = bmGO.AddComponent<TPMBlockManager>();
        blockManager.settings = settings;

        // FeedbackManager (a besoin d'un Canvas monde pour le score flottant)
        var fbGO       = new GameObject("FeedbackManager");
        feedback       = fbGO.AddComponent<TPMFeedbackManager>();

        // Canvas monde pour le score flottant
        var canvasGO   = new GameObject("WorldCanvas");
        var canvas     = canvasGO.AddComponent<Canvas>();
        canvas.renderMode        = RenderMode.WorldSpace;
        canvas.worldCamera       = gameCamera;
        canvas.sortingOrder      = 30;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        var canvasRT             = canvasGO.GetComponent<RectTransform>();
        canvasRT.localScale      = Vector3.one * 0.01f;
        // Ratio 1080×1920 → canvas monde proportionnel en portrait
        canvasRT.sizeDelta       = new Vector2(1080f, 1920f);

        feedback.worldCanvas     = canvas;
    }

    // ── Grille ────────────────────────────────────────────────────────────────

    private void BuildGrid()
    {
        // TPMGrid (logique)
        var gridGO    = new GameObject("Grid");
        grid          = gridGO.AddComponent<TPMGrid>();
        grid.settings = settings;
        grid.Init();   // initialise cells[] immédiatement — settings est déjà assigné

        // TPMGridRenderer (visuel)
        var rendererGO  = new GameObject("GridRenderer");
        gridRenderer    = rendererGO.AddComponent<TPMGridRenderer>();
        gridRenderer.settings = settings;
        gridRenderer.Init(); // dessine la grille maintenant que TPMGrid.Instance est prêt
    }

    // ── Joueur ────────────────────────────────────────────────────────────────

    private void BuildPlayer()
    {
        var playerGO = new GameObject("Player");
        playerGO.AddComponent<SpriteRenderer>(); // requis par TPMPlayerController
        player       = playerGO.AddComponent<TPMPlayerController>();
        player.settings    = settings;
        player.gameCamera  = gameCamera;
    }

    // ── Monstre ───────────────────────────────────────────────────────────────

    private void BuildMonster()
    {
        var monsterGO = new GameObject("Monster");
        monster       = monsterGO.AddComponent<TPMMonster>();
        monster.settings = settings;
        monster.player   = player;
    }

    // ── HUD ───────────────────────────────────────────────────────────────────

    private void BuildHUD()
    {
        var hudCanvas   = BuildUICanvas("HUDCanvas", 100);
        var hudGO       = new GameObject("HUD");
        hudGO.transform.SetParent(hudCanvas.transform, false);
        hud             = hudGO.AddComponent<TPMHUD>();

        // Barre du haut (fond semi-transparent)
        var topBar      = MakePanel(hudCanvas.transform, "TopBar",
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -50f), new Vector2(0f, 0f),
            new Color(0f, 0f, 0f, 0.65f), 0);
        topBar.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 50f);

        // Score (haut gauche)
        var scoreTMP    = MakeLabel(topBar.transform, "ScoreLabel",
            new Vector2(0.02f, 0.5f), "SCORE  000000",
            22f, new Color(0.9f, 0.9f, 1f, 1f), TextAlignmentOptions.MidlineLeft);
        scoreTMP.GetComponent<RectTransform>().sizeDelta = new Vector2(300f, 50f);

        // Coups (haut droit)
        var movesTMP    = MakeLabel(topBar.transform, "MovesLabel",
            new Vector2(0.98f, 0.5f), $"COUPS  {settings.startingMoves}",
            22f, new Color(0.2f, 1f, 0.5f, 1f), TextAlignmentOptions.MidlineRight);
        movesTMP.GetComponent<RectTransform>().sizeDelta = new Vector2(250f, 50f);

        // Barre de coups (dessous du bandeau)
        var barBG       = MakePanel(hudCanvas.transform, "MovesBarBG",
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -56f), new Vector2(0f, -50f),
            new Color(0.15f, 0.15f, 0.15f, 0.9f), 0);
        barBG.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 6f);

        var barFill     = MakePanel(barBG.transform, "MovesBarFill",
            new Vector2(0f, 0f), new Vector2(1f, 1f),
            Vector2.zero, Vector2.zero,
            new Color(0.15f, 0.85f, 0.25f, 1f), 1);
        var fillImg     = barFill.GetComponent<Image>();
        fillImg.type    = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImg.fillAmount = 1f;

        // Légende contrôles (bas)
        var hint     = MakeLabel(hudCanvas.transform, "HintLabel",
            new Vector2(0.5f, 0f), "WASD/Flèches : Déplacer   |   Espace : Poser bloc   |   E : Détruire bloc   |   Clic D : Poser/Détruire",
            14f, new Color(0.7f, 0.7f, 0.7f, 0.75f), TextAlignmentOptions.Bottom);
        var hintRT = hint.GetComponent<RectTransform>();
        hintRT.anchorMin  = new Vector2(0f, 0f);
        hintRT.anchorMax  = new Vector2(1f, 0f);
        hintRT.pivot      = new Vector2(0.5f, 0f);
        hintRT.anchoredPosition = new Vector2(0f, 8f);
        hintRT.sizeDelta  = new Vector2(0f, 30f);

        hud.Init(scoreTMP, movesTMP, fillImg, settings.startingMoves);
    }

    // ── Game over UI ──────────────────────────────────────────────────────────

    private void BuildGameOverUI()
    {
        var goCanvas   = BuildUICanvas("GameOverCanvas", 200);
        var goUIGO     = new GameObject("GameOverUI");
        goUIGO.transform.SetParent(goCanvas.transform, false);
        gameOverUI     = goUIGO.AddComponent<TPMGameOverUI>();

        // Fond semi-transparent plein écran
        var bgPanel    = MakePanel(goCanvas.transform, "Background",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color(0.1f, 0.55f, 0.2f, 0f), 0);
        var bgImg      = bgPanel.GetComponent<Image>();

        // Titre
        var title      = MakeLabel(goCanvas.transform, "Title",
            new Vector2(0.5f, 0.6f), "TITRE",
            52f, Color.white, TextAlignmentOptions.Center);
        var titleRT    = title.GetComponent<RectTransform>();
        titleRT.anchorMin = titleRT.anchorMax = new Vector2(0.5f, 0.6f);
        titleRT.pivot     = new Vector2(0.5f, 0.5f);
        titleRT.sizeDelta = new Vector2(600f, 80f);
        titleRT.anchoredPosition = Vector2.zero;

        // Score final
        var scoreFinal = MakeLabel(goCanvas.transform, "FinalScore",
            new Vector2(0.5f, 0.45f), "SCORE  000000",
            30f, new Color(0.9f, 0.9f, 1f, 1f), TextAlignmentOptions.Center);
        var scoreRT    = scoreFinal.GetComponent<RectTransform>();
        scoreRT.anchorMin = scoreRT.anchorMax = new Vector2(0.5f, 0.45f);
        scoreRT.pivot     = new Vector2(0.5f, 0.5f);
        scoreRT.sizeDelta = new Vector2(400f, 50f);
        scoreRT.anchoredPosition = Vector2.zero;

        // Bouton Rejouer
        var btnGO      = new GameObject("RestartButton");
        btnGO.transform.SetParent(goCanvas.transform, false);
        var btnRT      = btnGO.AddComponent<RectTransform>();
        btnRT.anchorMin = btnRT.anchorMax = new Vector2(0.5f, 0.3f);
        btnRT.pivot     = new Vector2(0.5f, 0.5f);
        btnRT.sizeDelta = new Vector2(220f, 55f);
        btnRT.anchoredPosition = Vector2.zero;

        var btnImg     = btnGO.AddComponent<Image>();
        btnImg.color   = new Color(1f, 1f, 1f, 0.18f);
        var btn        = btnGO.AddComponent<Button>();

        var btnLabelGO = new GameObject("Label");
        btnLabelGO.transform.SetParent(btnGO.transform, false);
        var btnLabelRT = btnLabelGO.AddComponent<RectTransform>();
        btnLabelRT.anchorMin = Vector2.zero;
        btnLabelRT.anchorMax = Vector2.one;
        btnLabelRT.offsetMin = btnLabelRT.offsetMax = Vector2.zero;
        var btnTMP     = btnLabelGO.AddComponent<TextMeshProUGUI>();
        btnTMP.text    = "REJOUER";
        btnTMP.fontSize = 24f;
        btnTMP.color   = Color.white;
        btnTMP.alignment = TextAlignmentOptions.Center;

        gameOverUI.Init(goCanvas, bgImg, title, scoreFinal, btn);
    }

    // ── Câblage ───────────────────────────────────────────────────────────────

    private void WireReferences()
    {
        // Tous les composants se trouvent via les singletons ou les champs publics.
        // Les événements statiques de TPMGameManager font le reste.
    }

    // ── Helpers UI ────────────────────────────────────────────────────────────

    private Canvas BuildUICanvas(string name, int sortingOrder)
    {
        var go      = new GameObject(name);
        var canvas  = go.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var scaler                 = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        // Portrait : on match sur la hauteur pour que tout tienne verticalement
        scaler.matchWidthOrHeight  = 1f;

        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private GameObject MakePanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax,
        Color color, int sortingOrder)
    {
        var go    = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt    = go.AddComponent<RectTransform>();
        rt.anchorMin    = anchorMin;
        rt.anchorMax    = anchorMax;
        rt.offsetMin    = offsetMin;
        rt.offsetMax    = offsetMax;
        var img   = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    private TextMeshProUGUI MakeLabel(Transform parent, string name,
        Vector2 anchorPos, string text, float fontSize,
        Color color, TextAlignmentOptions alignment)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt  = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchorPos;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(300f, 40f);
        rt.anchoredPosition = Vector2.zero;

        var tmp         = go.AddComponent<TextMeshProUGUI>();
        tmp.text        = text;
        tmp.fontSize    = fontSize;
        tmp.color       = color;
        tmp.alignment   = alignment;
        tmp.fontStyle   = FontStyles.Bold;
        return tmp;
    }
}
