using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Construit la scène fixe du niveau 1 de Tetris×Pac-Man directement dans l'éditeur.
/// Tous les GameObjects sont sérialisés dans le fichier .unity : la scène n'est plus procédurale au runtime.
///
/// Menu : Tools ▸ TetrisPacMan ▸ Build Level 1 Scene
/// </summary>
public static class TPMSceneBuilder
{
    private const string ScenePath    = "Assets/Scenes/TetrisPacMan.unity";
    private const string SettingsPath = "Assets/ScriptableObjects/TPMSettings.asset";

    // ── Couleurs ───────────────────────────────────────────────────────────────

    private static readonly Color WallColor     = new Color(0.50f, 0.52f, 0.60f, 1.00f);
    private static readonly Color WallEdgeColor = new Color(0.35f, 0.37f, 0.45f, 1.00f);
    private static readonly Color ExitColor     = new Color(0.10f, 0.90f, 0.20f, 1.00f);
    private static readonly Color ExitEdgeColor = new Color(0.05f, 0.60f, 0.10f, 1.00f);
    private static readonly Color GridLineColor = new Color(1f, 1f, 1f, 0.05f);
    private static readonly Color BgColor       = new Color(0.06f, 0.07f, 0.14f, 1f);

    // ── Grille ────────────────────────────────────────────────────────────────

    private const int   GridW    = 9;
    private const int   GridH    = 16;
    private const float CellSize = 1.18f;

    // ── Level 1 layout ────────────────────────────────────────────────────────

    private static readonly (int x, int y)[] WallCells = BuildWallList();

    private static (int, int)[] BuildWallList()
    {
        var list = new List<(int, int)>();
        for (int x = 0; x < GridW; x++) { list.Add((x, 0)); list.Add((x, GridH - 1)); }
        for (int y = 1; y < GridH - 1; y++) { list.Add((0, y)); list.Add((GridW - 1, y)); }

        // Plateforme haute-gauche avec crochet
        list.Add((1, 13)); list.Add((2, 13)); list.Add((3, 13)); list.Add((4, 13));
        list.Add((3, 14));
        // Plateforme haute-droite
        list.Add((5, 12)); list.Add((6, 12)); list.Add((7, 12));
        // Plateforme milieu-gauche
        list.Add((1, 10)); list.Add((2, 10)); list.Add((3, 10)); list.Add((4, 10));
        // Plateforme milieu-droite avec crochet
        list.Add((5, 11)); list.Add((6, 11)); list.Add((7, 11));
        list.Add((7, 10));
        // Plateformes centrale haute
        list.Add((2, 9)); list.Add((3, 9));
        list.Add((5, 8)); list.Add((6, 8)); list.Add((7, 8));
        // Bloc isolé
        list.Add((4, 7));
        // Plateforme milieu-bas gauche
        list.Add((1, 6)); list.Add((2, 6)); list.Add((3, 6));
        list.Add((1, 5));
        // Plateforme milieu-bas droite
        list.Add((5, 6)); list.Add((6, 6));
        // Plateforme basse-gauche en L
        list.Add((1, 4)); list.Add((2, 4));
        list.Add((1, 3));
        // Plateforme basse centrale
        list.Add((4, 3)); list.Add((5, 3)); list.Add((6, 3));

        return list.ToArray();
    }

    private const int ExitX = 7, ExitY = 14;
    private const int PlayerStartX = 2, PlayerStartY = 8;
    private const int MonsterStartX = 7, MonsterStartY = 2;

    // ── Entry points ──────────────────────────────────────────────────────────

    [MenuItem("Tools/TetrisPacMan/Build Level 1 Scene")]
    public static void BuildScene()
    {
        var scene    = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var settings = LoadOrCreateSettings();

        // Caméra
        var camGO           = new GameObject("Camera");
        var cam             = camGO.AddComponent<Camera>();
        cam.orthographic    = true;
        cam.orthographicSize = 9.6f;
        cam.clearFlags      = CameraClearFlags.SolidColor;
        cam.backgroundColor = BgColor;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        camGO.AddComponent<AudioListener>();
        camGO.tag = "MainCamera";

        // Fond
        var bgGO      = new GameObject("Background");
        var bgSR      = bgGO.AddComponent<SpriteRenderer>();
        bgSR.sprite   = SpriteGenerator.CreateColoredSquare(BgColor);
        bgSR.sortingOrder = -20;
        bgGO.transform.localScale = Vector3.one * 25f;

        // Grille fixe (murs, lignes, EXIT)
        var gridParent = new GameObject("Grid");
        BuildGridLines(gridParent.transform);
        BuildWalls(gridParent.transform);
        BuildExit(gridParent.transform);
        BuildStartMarker(gridParent.transform);

        // Managers runtime
        BuildManagers(settings, cam);

        // Joueur + input tactile
        BuildPlayerGO(settings, cam);

        // Monstre
        BuildMonsterGO(settings);

        // EventSystem
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // HUD + palette
        BuildHUDCanvas(settings);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Level 1 — Scène construite",
            $"Scène fixe sauvegardée :\n{ScenePath}\n\nTous les GameObjects sont persistants et éditables dans l'Inspector.",
            "Super !");
    }

    [MenuItem("Tools/TetrisPacMan/Add To Build Settings")]
    public static void AddToBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in scenes)
            if (s.path == ScenePath) { Debug.Log("Déjà dans les Build Settings."); return; }
        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log($"[TPMSceneBuilder] Ajouté : {ScenePath}");
    }

    // ── Construction ──────────────────────────────────────────────────────────

    private static TPMSettings LoadOrCreateSettings()
    {
        var s = AssetDatabase.LoadAssetAtPath<TPMSettings>(SettingsPath);
        if (s != null) return s;
        s = ScriptableObject.CreateInstance<TPMSettings>();
        s.gridWidth = GridW; s.gridHeight = GridH; s.cellSize = CellSize;
        AssetDatabase.CreateAsset(s, SettingsPath);
        AssetDatabase.SaveAssets();
        return s;
    }

    private static void BuildGridLines(Transform parent)
    {
        float totalW  = (GridW - 1) * CellSize;
        float totalH  = (GridH - 1) * CellSize;
        float ox      = -totalW * 0.5f;
        float oy      = -totalH * 0.5f;
        var mat       = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
        var linesRoot = new GameObject("GridLines");
        linesRoot.transform.SetParent(parent, false);

        for (int x = 0; x < GridW; x++)
        {
            var go = new GameObject($"VLine_{x}");
            go.transform.SetParent(linesRoot.transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2; lr.useWorldSpace = true;
            lr.startWidth = lr.endWidth = 0.012f;
            lr.startColor = lr.endColor = GridLineColor;
            lr.sharedMaterial = mat; lr.sortingOrder = -2;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            float fx = ox + x * CellSize;
            lr.SetPosition(0, new Vector3(fx, oy - CellSize * 0.5f, 0f));
            lr.SetPosition(1, new Vector3(fx, oy + totalH + CellSize * 0.5f, 0f));
        }
        for (int y = 0; y < GridH; y++)
        {
            var go = new GameObject($"HLine_{y}");
            go.transform.SetParent(linesRoot.transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2; lr.useWorldSpace = true;
            lr.startWidth = lr.endWidth = 0.012f;
            lr.startColor = lr.endColor = GridLineColor;
            lr.sharedMaterial = mat; lr.sortingOrder = -2;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            float fy = oy + y * CellSize;
            lr.SetPosition(0, new Vector3(ox - CellSize * 0.5f, fy, 0f));
            lr.SetPosition(1, new Vector3(ox + totalW + CellSize * 0.5f, fy, 0f));
        }
    }

    private static void BuildWalls(Transform parent)
    {
        float cs        = CellSize * 0.90f;
        var wallsRoot   = new GameObject("Walls");
        wallsRoot.transform.SetParent(parent, false);

        foreach (var (wx, wy) in WallCells)
        {
            Vector3 pos = CellToWorld(wx, wy);

            var eGO  = new GameObject($"WE_{wx}_{wy}");
            eGO.transform.SetParent(wallsRoot.transform, false);
            eGO.transform.position   = pos;
            eGO.transform.localScale = Vector3.one * (cs * 1.04f);
            eGO.AddComponent<SpriteRenderer>().sprite = SpriteGenerator.CreateColoredSquare(WallEdgeColor);
            eGO.GetComponent<SpriteRenderer>().sortingOrder = 0;

            var wGO  = new GameObject($"W_{wx}_{wy}");
            wGO.transform.SetParent(wallsRoot.transform, false);
            wGO.transform.position   = pos;
            wGO.transform.localScale = Vector3.one * cs;
            wGO.AddComponent<SpriteRenderer>().sprite = SpriteGenerator.CreateColoredSquare(WallColor);
            wGO.GetComponent<SpriteRenderer>().sortingOrder = 1;
        }
    }

    private static void BuildExit(Transform parent)
    {
        float cs      = CellSize * 0.90f;
        Vector3 pos   = CellToWorld(ExitX, ExitY);
        var exitRoot  = new GameObject("Exit");
        exitRoot.transform.SetParent(parent, false);

        var bgGO  = new GameObject("ExitBG");
        bgGO.transform.SetParent(exitRoot.transform, false);
        bgGO.transform.position   = pos;
        bgGO.transform.localScale = Vector3.one * (cs * 1.06f);
        bgGO.AddComponent<SpriteRenderer>().sprite = SpriteGenerator.CreateColoredSquare(ExitEdgeColor);

        var exitGO  = new GameObject("ExitBody");
        exitGO.transform.SetParent(exitRoot.transform, false);
        exitGO.transform.position   = pos;
        exitGO.transform.localScale = Vector3.one * cs;
        var exitSR  = exitGO.AddComponent<SpriteRenderer>();
        exitSR.sprite = SpriteGenerator.CreateColoredSquare(ExitColor);
        exitSR.sortingOrder = 1;
        exitGO.AddComponent<TPMExitPulse>().sr = exitSR;

        var labelGO = new GameObject("ExitLabel");
        labelGO.transform.SetParent(exitRoot.transform, false);
        labelGO.transform.position   = pos + new Vector3(0f, cs * 0.7f, 0f);
        labelGO.transform.localScale = Vector3.one * 0.26f;
        var tm = labelGO.AddComponent<TextMesh>();
        tm.text = "EXIT"; tm.fontSize = 14; tm.fontStyle = FontStyle.Bold;
        tm.color = Color.white; tm.anchor = TextAnchor.MiddleCenter;
    }

    private static void BuildStartMarker(Transform parent)
    {
        Vector3 pos = CellToWorld(PlayerStartX, PlayerStartY);
        var mGO = new GameObject("StartMarker");
        mGO.transform.SetParent(parent, false);
        mGO.transform.position   = pos + new Vector3(-CellSize * 0.85f, 0f, 0f);
        mGO.transform.localScale = Vector3.one * 0.22f;
        var tm = mGO.AddComponent<TextMesh>();
        tm.text = "START"; tm.fontSize = 14; tm.fontStyle = FontStyle.Bold;
        tm.color = new Color(0.9f, 0.9f, 0.9f, 0.6f); tm.anchor = TextAnchor.MiddleCenter;
    }

    private static void BuildManagers(TPMSettings settings, Camera cam)
    {
        var gmGO    = new GameObject("GameManager");
        var gm      = gmGO.AddComponent<TPMGameManager>();
        gm.settings = settings;

        var bmGO    = new GameObject("BlockManager");
        var bm      = bmGO.AddComponent<TPMBlockManager>();
        bm.settings = settings;

        var fbGO    = new GameObject("FeedbackManager");
        var fb      = fbGO.AddComponent<TPMFeedbackManager>();
        var wCanvasGO   = new GameObject("WorldCanvas");
        var wCanvas     = wCanvasGO.AddComponent<Canvas>();
        wCanvas.renderMode  = RenderMode.WorldSpace;
        wCanvas.worldCamera = cam;
        wCanvas.sortingOrder = 30;
        wCanvasGO.AddComponent<CanvasScaler>();
        wCanvasGO.AddComponent<GraphicRaycaster>();
        wCanvasGO.GetComponent<RectTransform>().localScale = Vector3.one * 0.01f;
        wCanvasGO.GetComponent<RectTransform>().sizeDelta  = new Vector2(1080f, 1920f);
        fb.worldCanvas = wCanvas;

        var spawnGO      = new GameObject("TetrisSpawner");
        var sp           = spawnGO.AddComponent<TPMTetrisSpawner>();
        sp.settings      = settings;

        var gridGO       = new GameObject("GridLogic");
        var grid         = gridGO.AddComponent<TPMGrid>();
        grid.settings    = settings;
    }

    private static void BuildPlayerGO(TPMSettings settings, Camera cam)
    {
        var pGO      = new GameObject("Player");
        pGO.tag      = "Player";
        pGO.AddComponent<SpriteRenderer>();
        var pc       = pGO.AddComponent<TPMPlayerController>();
        pc.settings  = settings;
        var ti           = pGO.AddComponent<TPMTouchInput>();
        ti.gameCamera    = cam;
        pGO.transform.position = CellToWorld(PlayerStartX, PlayerStartY);
    }

    private static void BuildMonsterGO(TPMSettings settings)
    {
        var mGO          = new GameObject("Monster");
        var monster      = mGO.AddComponent<TPMMonster>();
        monster.settings = settings;
        mGO.transform.position = CellToWorld(MonsterStartX, MonsterStartY);
    }

    private static void BuildHUDCanvas(TPMSettings settings)
    {
        var canvasGO  = new GameObject("HUDCanvas");
        var canvas    = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler                 = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight  = 1f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Bandeau top
        var topBar    = MakePanel(canvas.transform, "TopBar",
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.72f));
        topBar.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 80f);

        var scoreLabel = MakeLabel(topBar.transform, "ScoreLabel", "SCORE: 0", 30f, Color.white, TextAlignmentOptions.Left);
        var slRT       = scoreLabel.GetComponent<RectTransform>();
        slRT.anchorMin = new Vector2(0f, 0f); slRT.anchorMax = new Vector2(0.5f, 1f);
        slRT.pivot     = new Vector2(0f, 0.5f);
        slRT.offsetMin = new Vector2(20f, 0f); slRT.offsetMax = Vector2.zero;

        var mcGO   = new GameObject("MovesContainer");
        mcGO.transform.SetParent(topBar.transform, false);
        var mcRT   = mcGO.AddComponent<RectTransform>();
        mcRT.anchorMin = new Vector2(0.5f, 0f); mcRT.anchorMax = new Vector2(1f, 1f);
        mcRT.pivot     = new Vector2(1f, 0.5f);
        mcRT.offsetMin = Vector2.zero; mcRT.offsetMax = new Vector2(-20f, 0f);

        var movesLabel = MakeLabel(mcGO.transform, "MovesLabel", $"COUPS: {settings.startingMoves}", 30f, Color.white, TextAlignmentOptions.Right);
        var mlRT       = movesLabel.GetComponent<RectTransform>();
        mlRT.anchorMin = new Vector2(0f, 0f); mlRT.anchorMax = new Vector2(1f, 1f);
        mlRT.pivot     = new Vector2(1f, 0.5f);
        mlRT.offsetMin = Vector2.zero; mlRT.offsetMax = new Vector2(-44f, 0f);

        var iconGO   = new GameObject("CoinIcon");
        iconGO.transform.SetParent(mcGO.transform, false);
        var iconRT   = iconGO.AddComponent<RectTransform>();
        iconRT.anchorMin = iconRT.anchorMax = new Vector2(1f, 0.5f);
        iconRT.pivot     = new Vector2(1f, 0.5f);
        iconRT.sizeDelta = new Vector2(34f, 34f);
        var iconImg  = iconGO.AddComponent<Image>();
        iconImg.sprite = SpriteGenerator.CreateCircle(32);
        iconImg.color  = new Color(1f, 0.80f, 0.05f, 1f);

        var hudGO  = new GameObject("HUD");
        hudGO.transform.SetParent(canvasGO.transform, false);
        var hud    = hudGO.AddComponent<TPMHUD>();
        hud.Init(scoreLabel, movesLabel, settings.startingMoves);

        // Palette blocs
        var paletteGO = new GameObject("Palette");
        paletteGO.transform.SetParent(canvasGO.transform, false);
        var palette   = paletteGO.AddComponent<TPMBlockPalette>();
        palette.Build(canvas);

        // Game Over
        BuildGameOverCanvas();
    }

    private static void BuildGameOverCanvas()
    {
        var goCanvasGO  = new GameObject("GameOverCanvas");
        var goCanvas    = goCanvasGO.AddComponent<Canvas>();
        goCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        goCanvas.sortingOrder = 200;
        var sc = goCanvasGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1080f, 1920f);
        sc.matchWidthOrHeight  = 1f;
        goCanvasGO.AddComponent<GraphicRaycaster>();

        var bgPanel  = MakePanel(goCanvas.transform, "Background",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0f));
        var bgImg    = bgPanel.GetComponent<Image>();

        var title    = MakeLabel(goCanvas.transform, "Title", "TITRE", 52f, Color.white, TextAlignmentOptions.Center);
        var tRT      = title.GetComponent<RectTransform>();
        tRT.anchorMin = tRT.anchorMax = new Vector2(0.5f, 0.6f);
        tRT.pivot     = new Vector2(0.5f, 0.5f); tRT.sizeDelta = new Vector2(600f, 80f);

        var scoreF   = MakeLabel(goCanvas.transform, "FinalScore", "SCORE  000000", 30f, Color.white, TextAlignmentOptions.Center);
        var sRT      = scoreF.GetComponent<RectTransform>();
        sRT.anchorMin = sRT.anchorMax = new Vector2(0.5f, 0.45f);
        sRT.pivot     = new Vector2(0.5f, 0.5f); sRT.sizeDelta = new Vector2(400f, 50f);

        var btnGO    = new GameObject("RestartButton");
        btnGO.transform.SetParent(goCanvas.transform, false);
        var btnRT    = btnGO.AddComponent<RectTransform>();
        btnRT.anchorMin = btnRT.anchorMax = new Vector2(0.5f, 0.3f);
        btnRT.pivot     = new Vector2(0.5f, 0.5f); btnRT.sizeDelta = new Vector2(220f, 55f);
        var btnImg   = btnGO.AddComponent<Image>();
        btnImg.color = new Color(1f, 1f, 1f, 0.18f);
        var btn      = btnGO.AddComponent<Button>();

        var lblGO    = new GameObject("Label");
        lblGO.transform.SetParent(btnGO.transform, false);
        var lblRT    = lblGO.AddComponent<RectTransform>();
        lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;
        var lblTMP   = lblGO.AddComponent<TextMeshProUGUI>();
        lblTMP.text  = "REJOUER"; lblTMP.fontSize = 24f;
        lblTMP.color = Color.white; lblTMP.alignment = TextAlignmentOptions.Center;

        var goUIGO   = new GameObject("GameOverUI");
        goUIGO.transform.SetParent(goCanvasGO.transform, false);
        goUIGO.AddComponent<TPMGameOverUI>().Init(goCanvas, bgImg, title, scoreF, btn);
    }

    // ── Helpers UI ────────────────────────────────────────────────────────────

    private static GameObject MakePanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt  = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
        go.AddComponent<Image>().color = color;
        return go;
    }

    private static TextMeshProUGUI MakeLabel(Transform parent, string name,
        string text, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt  = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(400f, 50f);
        var tmp      = go.AddComponent<TextMeshProUGUI>();
        tmp.text     = text; tmp.fontSize = fontSize;
        tmp.color    = color; tmp.alignment = alignment; tmp.fontStyle = FontStyles.Bold;
        return tmp;
    }

    private static Vector3 CellToWorld(int x, int y)
    {
        float ox = -(GridW - 1) * CellSize * 0.5f;
        float oy = -(GridH - 1) * CellSize * 0.5f;
        return new Vector3(ox + x * CellSize, oy + y * CellSize, 0f);
    }
}
