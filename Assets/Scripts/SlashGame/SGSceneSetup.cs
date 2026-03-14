using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Procedurally builds the entire SlashGame scene at runtime:
/// background, player, UI canvas, energy cone, squad slots, HUD, and particle systems.
/// Attach to an empty "SceneSetup" GameObject — it wires everything automatically.
/// </summary>
[DefaultExecutionOrder(-100)]
public class SGSceneSetup : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Assets (assign in Inspector)")]
    public SGSettings  settings;
    public SGSquadData squadData;

    // ── Cached references (populated at build time) ───────────────────────────

    private SGGameManager      gameManager;
    private SGSlashSpawner     slashSpawner;
    private SGPlayerController player;
    private SGFeedbackManager  feedback;
    private SGBetsHUD          betsHUD;
    private SGHUD              hud;
    private SGTutorial         tutorial;
    private Camera             gameCamera;

    // ── Entry point ───────────────────────────────────────────────────────────

    private void Awake()
    {
        BuildCamera();
        BuildBackground();
        BuildGameManager();
        BuildSlashSpawner();
        BuildPlayer();
        BuildFeedbackManager();
        BuildUI();
        BuildParticles();
        BuildTutorial();
        WireReferences();
    }

    // ── Camera ────────────────────────────────────────────────────────────────

    private void BuildCamera()
    {
        var camGO = new GameObject("Camera");
        gameCamera             = camGO.AddComponent<Camera>();
        gameCamera.orthographic = true;
        gameCamera.orthographicSize = 6f;
        gameCamera.clearFlags       = CameraClearFlags.SolidColor;
        gameCamera.backgroundColor  = Color.black;
        gameCamera.transform.position = new Vector3(0f, 0f, -10f);
        camGO.AddComponent<AudioListener>();
    }

    // ── Background ────────────────────────────────────────────────────────────

    private void BuildBackground()
    {
        // Very subtle concentric rings for depth — same style as other games
        float[] radii  = { 1.0f, 2.0f, 3.0f, 4.5f };
        foreach (float r in radii)
        {
            var go = new GameObject($"Ring_{r}");
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount     = 48;
            lr.loop              = true;
            lr.useWorldSpace     = true;
            lr.startWidth        = 0.015f;
            lr.endWidth          = 0.015f;
            lr.startColor        = new Color(1f, 1f, 1f, 0.05f);
            lr.endColor          = new Color(1f, 1f, 1f, 0.05f);
            lr.sortingOrder      = -1;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows    = false;

            var mat = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            lr.sharedMaterial = mat;

            for (int i = 0; i < 48; i++)
            {
                float a = i / 48f * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f));
            }
        }

        // Parry zone circle — subtle visible hint for the player
        var parryGO = new GameObject("ParryZone");
        var parryLR = parryGO.AddComponent<LineRenderer>();
        float pr    = settings != null ? settings.parryRadius : 1.2f;
        parryLR.positionCount     = 48;
        parryLR.loop              = true;
        parryLR.useWorldSpace     = true;
        parryLR.startWidth        = 0.025f;
        parryLR.endWidth          = 0.025f;
        parryLR.startColor        = new Color(1f, 1f, 1f, 0.15f);
        parryLR.endColor          = new Color(1f, 1f, 1f, 0.15f);
        parryLR.sortingOrder      = 1;
        parryLR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        parryLR.receiveShadows    = false;
        var parryMat = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
        parryLR.sharedMaterial = parryMat;

        for (int i = 0; i < 48; i++)
        {
            float a = i / 48f * Mathf.PI * 2f;
            parryLR.SetPosition(i, new Vector3(Mathf.Cos(a) * pr, Mathf.Sin(a) * pr, 0f));
        }
    }

    // ── Game Manager ──────────────────────────────────────────────────────────

    private void BuildGameManager()
    {
        var go    = new GameObject("GameManager");
        gameManager          = go.AddComponent<SGGameManager>();
        gameManager.settings = settings;
        gameManager.squadData = squadData;
    }

    // ── Slash Spawner ─────────────────────────────────────────────────────────

    private void BuildSlashSpawner()
    {
        var go      = new GameObject("SlashSpawner");
        slashSpawner          = go.AddComponent<SGSlashSpawner>();
        slashSpawner.settings = settings;
    }

    // ── Player ────────────────────────────────────────────────────────────────

    private void BuildPlayer()
    {
        var go = new GameObject("Player");
        player                  = go.AddComponent<SGPlayerController>();
        player.settings         = settings;
        player.squadData        = squadData;
        player.gameCamera       = gameCamera;
    }

    // ── Feedback ──────────────────────────────────────────────────────────────

    private void BuildFeedbackManager()
    {
        var go    = new GameObject("FeedbackManager");
        feedback  = go.AddComponent<SGFeedbackManager>();
        feedback.settings   = settings;
        feedback.gameCamera = gameCamera;
    }

    // ── Particles ─────────────────────────────────────────────────────────────
    // Particles removed — feedback is geometry-based (ripple rings + flash).
    private void BuildParticles() { }

    // ── UI ────────────────────────────────────────────────────────────────────

    private void BuildUI()
    {
        // Root canvas
        var canvasGO = new GameObject("UICanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode         = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder       = 100;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var scaler                     = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode             = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution     = new Vector2(1080f, 1920f);
        scaler.screenMatchMode         = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight      = 0.5f;

        // Flash overlay (full-screen)
        var flashGO  = CreateUIImage("FlashOverlay", canvasGO.transform, Color.clear);
        var flashImg = flashGO.GetComponent<Image>();
        StretchFull(flashGO.GetComponent<RectTransform>());
        flashImg.raycastTarget = false;
        feedback.flashOverlay  = flashImg;

        // Top HUD
        BuildTopHUD(canvasGO.transform);

        // Bottom UI
        BuildBottomUI(canvasGO.transform);

        // Game over panel
        BuildGameOverPanel(canvasGO.transform);

        // Instruction text (tutorial)
        BuildInstructionText(canvasGO.transform);

        // Event system
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    private void BuildTopHUD(Transform canvasParent)
    {
        var hudGO = new GameObject("HUD");
        hudGO.transform.SetParent(canvasParent, false);
        hud = hudGO.AddComponent<SGHUD>();

        // ── Progress bar (square white bar at very top, fills with bets) ──────

        // Background track
        var progBgGO  = CreateUIImage("ProgressBarBg", hudGO.transform, new Color(0.15f, 0.15f, 0.15f));
        var progBgRT  = progBgGO.GetComponent<RectTransform>();
        progBgRT.anchorMin        = new Vector2(0f, 1f);
        progBgRT.anchorMax        = new Vector2(1f, 1f);
        progBgRT.pivot            = new Vector2(0.5f, 1f);
        progBgRT.anchoredPosition = new Vector2(0f, -40f);
        progBgRT.offsetMin        = new Vector2(60f, progBgRT.offsetMin.y);
        progBgRT.offsetMax        = new Vector2(-60f, progBgRT.offsetMax.y);
        progBgRT.sizeDelta        = new Vector2(progBgRT.sizeDelta.x, 55f);

        // Fill
        var progFillGO  = CreateUIImage("ProgressBarFill", progBgGO.transform, Color.white);
        var progFillRT  = progFillGO.GetComponent<RectTransform>();
        progFillRT.anchorMin = Vector2.zero;
        progFillRT.anchorMax = Vector2.one;
        progFillRT.offsetMin = new Vector2(4f, 4f);
        progFillRT.offsetMax = new Vector2(-4f, -4f);
        var progFillImg      = progFillGO.GetComponent<Image>();
        progFillImg.type         = Image.Type.Filled;
        progFillImg.fillMethod   = Image.FillMethod.Horizontal;
        progFillImg.fillAmount   = 0f;
        progFillImg.raycastTarget = false;

        // Wire to SGBetsHUD (built later in BuildBottomUI) — stored temporarily
        // We keep a reference here and pass it after BuildBottomUI is called.
        // Store in a local field via a tag on the image for later wiring.
        progFillGO.name = "ProgressBarFill_Ref";

        // ── Score ─────────────────────────────────────────────────────────────

        var scoreGO  = CreateTMPText("ScoreText", hudGO.transform, "0", 72);
        var scoreRT  = scoreGO.GetComponent<RectTransform>();
        scoreRT.anchorMin        = new Vector2(0.5f, 1f);
        scoreRT.anchorMax        = new Vector2(0.5f, 1f);
        scoreRT.pivot            = new Vector2(0.5f, 1f);
        scoreRT.anchoredPosition = new Vector2(0f, -130f);
        scoreRT.sizeDelta        = new Vector2(400f, 100f);
        hud.scoreText = scoreGO.GetComponent<TextMeshProUGUI>();

        // ── Combo ─────────────────────────────────────────────────────────────

        var comboGO = CreateTMPText("ComboText", hudGO.transform, "", 44);
        var comboRT = comboGO.GetComponent<RectTransform>();
        comboRT.anchorMin        = new Vector2(0.5f, 1f);
        comboRT.anchorMax        = new Vector2(0.5f, 1f);
        comboRT.pivot            = new Vector2(0.5f, 1f);
        comboRT.anchoredPosition = new Vector2(0f, -240f);
        comboRT.sizeDelta        = new Vector2(300f, 70f);
        hud.comboText = comboGO.GetComponent<TextMeshProUGUI>();

        // ── Fury label ────────────────────────────────────────────────────────

        var furyGO = CreateTMPText("FuryLabel", hudGO.transform, "FURY!", 40);
        var furyRT = furyGO.GetComponent<RectTransform>();
        furyRT.anchorMin        = new Vector2(0.5f, 1f);
        furyRT.anchorMax        = new Vector2(0.5f, 1f);
        furyRT.pivot            = new Vector2(0.5f, 1f);
        furyRT.anchoredPosition = new Vector2(0f, -300f);
        furyRT.sizeDelta        = new Vector2(300f, 60f);
        hud.furyLabel = furyGO.GetComponent<TextMeshProUGUI>();
        furyGO.SetActive(false);

        // ── XP bar ────────────────────────────────────────────────────────────

        var xpBgGO  = CreateUIImage("XPBarBg", hudGO.transform, new Color(0.15f, 0.15f, 0.15f));
        var xpBgRT  = xpBgGO.GetComponent<RectTransform>();
        xpBgRT.anchorMin        = new Vector2(0.5f, 1f);
        xpBgRT.anchorMax        = new Vector2(0.5f, 1f);
        xpBgRT.pivot            = new Vector2(0.5f, 1f);
        xpBgRT.anchoredPosition = new Vector2(0f, -360f);
        xpBgRT.sizeDelta        = new Vector2(500f, 8f);

        var xpFillGO  = CreateUIImage("XPBarFill", xpBgGO.transform, Color.white);
        var xpFillRT  = xpFillGO.GetComponent<RectTransform>();
        xpFillRT.anchorMin = Vector2.zero;
        xpFillRT.anchorMax = Vector2.one;
        xpFillRT.offsetMin = xpFillRT.offsetMax = Vector2.zero;
        var xpImg          = xpFillGO.GetComponent<Image>();
        xpImg.type         = Image.Type.Filled;
        xpImg.fillMethod   = Image.FillMethod.Horizontal;
        xpImg.fillAmount   = 0f;
        hud.xpBarFill      = xpImg;
    }

    private void BuildBottomUI(Transform canvasParent)
    {
        var bottomGO = new GameObject("BottomUI");
        bottomGO.transform.SetParent(canvasParent, false);

        // Root component for the bet bar
        betsHUD = bottomGO.AddComponent<SGBetsHUD>();

        // ── Three bet slots ───────────────────────────────────────────────────

        const float SlotSize    = 180f;
        const float SlotSpacing = 220f;
        const float SlotY       = 100f;    // distance from screen bottom
        const float StartX      = -SlotSpacing;  // centre the 3 slots

        for (int i = 0; i < 3; i++)
        {
            // Background square
            var slotGO  = CreateUIImage($"BetSlot_{i}", bottomGO.transform, new Color(0.15f, 0.15f, 0.15f));
            var slotRT  = slotGO.GetComponent<RectTransform>();
            slotRT.anchorMin        = new Vector2(0.5f, 0f);
            slotRT.anchorMax        = new Vector2(0.5f, 0f);
            slotRT.pivot            = new Vector2(0.5f, 0f);
            slotRT.anchoredPosition = new Vector2(StartX + i * SlotSpacing, SlotY);
            slotRT.sizeDelta        = new Vector2(SlotSize, SlotSize);
            slotGO.GetComponent<Image>().raycastTarget = false;

            if (i < betsHUD.betBackgrounds.Length)
                betsHUD.betBackgrounds[i] = slotGO.GetComponent<Image>();

            // Label  "0/target"
            var labelGO = CreateTMPText($"BetLabel_{i}", slotGO.transform, "0/10", 38);
            var labelRT = labelGO.GetComponent<RectTransform>();
            labelRT.anchorMin        = Vector2.zero;
            labelRT.anchorMax        = Vector2.one;
            labelRT.offsetMin        = Vector2.zero;
            labelRT.offsetMax        = Vector2.zero;
            labelRT.anchoredPosition = Vector2.zero;
            var labelTMP = labelGO.GetComponent<TextMeshProUGUI>();
            labelTMP.fontStyle       = FontStyles.Bold;

            if (i < betsHUD.betLabels.Length)
                betsHUD.betLabels[i] = labelTMP;
        }

        // ── Wire progress bar fill reference ──────────────────────────────────
        // Find the fill image created in BuildTopHUD by name
        var fillRef = canvasParent.GetComponentsInChildren<Image>(true);
        foreach (var img in fillRef)
        {
            if (img.gameObject.name == "ProgressBarFill_Ref")
            {
                betsHUD.progressBarFill = img;
                break;
            }
        }

        // ── Level-up popup (kept from original for squad progression) ──────────
        var popupGO = CreateUIImage("LevelUpPanel", canvasParent, new Color(0f, 0f, 0f, 0.9f));
        StretchFull(popupGO.GetComponent<RectTransform>());
        popupGO.SetActive(false);

        var popupTitleGO = CreateTMPText("LevelUpText", popupGO.transform, "UPGRADE", 64);
        var popupTitleRT = popupTitleGO.GetComponent<RectTransform>();
        popupTitleRT.anchorMin        = new Vector2(0.5f, 0.5f);
        popupTitleRT.anchorMax        = new Vector2(0.5f, 0.5f);
        popupTitleRT.pivot            = new Vector2(0.5f, 0.5f);
        popupTitleRT.anchoredPosition = new Vector2(0f, 80f);
        popupTitleRT.sizeDelta        = new Vector2(600f, 200f);

        var confirmBtnGO  = CreateUIImage("ConfirmButton", popupGO.transform, Color.white);
        var confirmBtnRT  = confirmBtnGO.GetComponent<RectTransform>();
        confirmBtnRT.anchorMin        = new Vector2(0.5f, 0.5f);
        confirmBtnRT.anchorMax        = new Vector2(0.5f, 0.5f);
        confirmBtnRT.pivot            = new Vector2(0.5f, 0.5f);
        confirmBtnRT.anchoredPosition = new Vector2(0f, -80f);
        confirmBtnRT.sizeDelta        = new Vector2(300f, 80f);
        confirmBtnGO.AddComponent<Button>();

        var confirmLabelGO = CreateTMPText("ConfirmLabel", confirmBtnGO.transform, "OK", 36);
        var confirmLabelRT = confirmLabelGO.GetComponent<RectTransform>();
        confirmLabelRT.anchorMin  = Vector2.zero;
        confirmLabelRT.anchorMax  = Vector2.one;
        confirmLabelRT.offsetMin  = Vector2.zero;
        confirmLabelRT.offsetMax  = Vector2.zero;
        confirmLabelGO.GetComponent<TextMeshProUGUI>().color = Color.black;
    }

    private void BuildGameOverPanel(Transform canvasParent)
    {
        var panelGO = CreateUIImage("GameOverPanel", canvasParent, new Color(0f, 0f, 0f, 0.85f));
        StretchFull(panelGO.GetComponent<RectTransform>());
        panelGO.SetActive(false);

        var goUI = panelGO.AddComponent<SGGameOverUI>();

        var cg   = panelGO.AddComponent<CanvasGroup>();
        goUI.panelGroup = cg;

        // "GAME OVER" title
        var titleGO = CreateTMPText("GameOverTitle", panelGO.transform, "GAME OVER", 80);
        var titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin        = new Vector2(0.5f, 0.5f);
        titleRT.anchorMax        = new Vector2(0.5f, 0.5f);
        titleRT.pivot            = new Vector2(0.5f, 0.5f);
        titleRT.anchoredPosition = new Vector2(0f, 200f);
        titleRT.sizeDelta        = new Vector2(700f, 120f);

        // Score
        var scoreGO = CreateTMPText("FinalScore", panelGO.transform, "0", 120);
        var scoreRT = scoreGO.GetComponent<RectTransform>();
        scoreRT.anchorMin        = new Vector2(0.5f, 0.5f);
        scoreRT.anchorMax        = new Vector2(0.5f, 0.5f);
        scoreRT.pivot            = new Vector2(0.5f, 0.5f);
        scoreRT.anchoredPosition = new Vector2(0f, 40f);
        scoreRT.sizeDelta        = new Vector2(400f, 160f);
        goUI.finalScoreText      = scoreGO.GetComponent<TextMeshProUGUI>();

        // Restart button
        var btnGO  = CreateUIImage("RestartButton", panelGO.transform, Color.white);
        var btnRT  = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin        = new Vector2(0.5f, 0.5f);
        btnRT.anchorMax        = new Vector2(0.5f, 0.5f);
        btnRT.pivot            = new Vector2(0.5f, 0.5f);
        btnRT.anchoredPosition = new Vector2(0f, -180f);
        btnRT.sizeDelta        = new Vector2(320f, 90f);

        var btn        = btnGO.AddComponent<Button>();
        goUI.restartButton = btn;

        var btnLabelGO = CreateTMPText("RestartLabel", btnGO.transform, "RESTART", 40);
        var btnLabelRT = btnLabelGO.GetComponent<RectTransform>();
        btnLabelRT.anchorMin  = Vector2.zero;
        btnLabelRT.anchorMax  = Vector2.one;
        btnLabelRT.offsetMin  = Vector2.zero;
        btnLabelRT.offsetMax  = Vector2.zero;
        var btnTMP            = btnLabelGO.GetComponent<TextMeshProUGUI>();
        btnTMP.color          = Color.black;
    }

    private void BuildInstructionText(Transform canvasParent)
    {
        var instructGO  = new GameObject("InstructionGroup");
        instructGO.transform.SetParent(canvasParent, false);
        var cg           = instructGO.AddComponent<CanvasGroup>();
        cg.alpha         = 0f;

        var rt           = instructGO.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, -260f);
        rt.sizeDelta        = new Vector2(700f, 100f);

        var textGO = CreateTMPText("InstructionText", instructGO.transform, "", 48);
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin  = Vector2.zero;
        textRT.anchorMax  = Vector2.one;
        textRT.offsetMin  = Vector2.zero;
        textRT.offsetMax  = Vector2.zero;

        if (tutorial != null)
        {
            tutorial.instructionText  = textGO.GetComponent<TextMeshProUGUI>();
            tutorial.instructionGroup = cg;
        }
    }

    // ── Tutorial ──────────────────────────────────────────────────────────────

    private void BuildTutorial()
    {
        var go   = new GameObject("Tutorial");
        tutorial = go.AddComponent<SGTutorial>();
        tutorial.squadData    = squadData;
        tutorial.slashSpawner = slashSpawner;
    }

    // ── Wire remaining cross-references ───────────────────────────────────────

    private void WireReferences()
    {
        player.slashSpawner = slashSpawner;
        player.feedback     = feedback;
    }

    // ── UI Helpers ────────────────────────────────────────────────────────────

    private static GameObject CreateUIImage(string name, Transform parent, Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img    = go.AddComponent<Image>();
        img.color  = color;
        img.sprite = SpriteGenerator.CreateWhiteSquare();
        img.raycastTarget = false;
        return go;
    }

    private static GameObject CreateTMPText(string name, Transform parent, string text, int fontSize)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp            = go.AddComponent<TextMeshProUGUI>();
        tmp.text           = text;
        tmp.fontSize       = fontSize;
        tmp.color          = Color.white;
        tmp.alignment      = TextAlignmentOptions.Center;
        tmp.raycastTarget  = false;
        return go;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
