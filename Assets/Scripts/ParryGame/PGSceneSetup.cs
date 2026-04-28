using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Procedurally builds the entire Parry Game scene at runtime.
/// Attach to an empty "SceneSetup" GameObject.
///
/// Layout (portrait 1080×1920, URP 2D):
/// - Perspective camera slightly to the right and behind the player
/// - Player sprite (personnage.png) bottom-center
/// - Enemies use ENNEMIS.png sprite
/// - Background uses fond jeu.png
/// - UI canvas: score top, hearts top-left, bottom tab bar (Défense/Armes/Soins)
/// </summary>
[DefaultExecutionOrder(-100)]
public class PGSceneSetup : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Settings (assign in Inspector)")]
    public PGSettings settings;

    [Header("Audio")]
    [Tooltip("Musique du Parry Game (parrygame music.mp3).")]
    public AudioClip parryMusic;

    [Tooltip("Son de clic UI (clic.mp3) — utilisé si l'AudioManager est absent.")]
    public AudioClip clickSfx;

    // ── Cached references ─────────────────────────────────────────────────────

    private Camera            gameCamera;
    private PGGameManager     gameManager;
    private PGEnemySpawner    spawner;
    private PGPlayerController player;
    private PGAbilitySystem   abilitySystem;
    private PGHUD             hud;
    private PGGameOverUI      gameOverUI;

    // ── Entry point ───────────────────────────────────────────────────────────

    private void Awake()
    {
        // Initialise MenuAssets si la scène est lancée directement sans passer par le menu
        if (MenuAssets.Font == null)
            MenuAssets.Init(null);

        EnsureEventSystem();
        BuildCamera();
        BuildBackground();
        BuildGameManager();
        BuildPlayer();
        BuildAbilitySystem();
        BuildSpawner();
        BuildUI();
        WireReferences();
        EnsureSceneTransition();
    }

    // ── Camera ────────────────────────────────────────────────────────────────

    private void BuildCamera()
    {
        // Réutilise la caméra déjà présente dans la scène pour éviter les doublons
        var camGO = GameObject.Find("Camera");
        if (camGO == null)
            camGO = new GameObject("Camera");

        gameCamera = camGO.GetComponent<Camera>();
        if (gameCamera == null)
            gameCamera = camGO.AddComponent<Camera>();

        // Perspective for depth effect
        gameCamera.orthographic    = false;
        gameCamera.fieldOfView     = settings != null ? settings.cameraFov : 60f;
        gameCamera.clearFlags      = CameraClearFlags.SolidColor;
        gameCamera.backgroundColor = new Color(0.04f, 0.04f, 0.08f, 1f);
        gameCamera.nearClipPlane   = 0.1f;
        gameCamera.farClipPlane    = 50f;

        // Third-person: slightly right, elevated, behind player
        float ox = settings != null ? settings.cameraOffsetX : 0.6f;
        float oy = settings != null ? settings.cameraOffsetY : 1.8f;
        float oz = settings != null ? settings.cameraOffsetZ : -7f;
        camGO.transform.position = new Vector3(ox, oy, oz);

        // Tilt down slightly toward player/enemies
        camGO.transform.rotation = Quaternion.Euler(8f, -4f, 0f);

        if (camGO.GetComponent<AudioListener>() == null)
            camGO.AddComponent<AudioListener>();
    }

    // ── Background ────────────────────────────────────────────────────────────

    private void BuildBackground()
    {
        BuildDepthMarkers();
    }

    /// <summary>Subtle grid lines receding into the distance for depth cues.</summary>
    private void BuildDepthMarkers()
    {
        int lineCount = 6;
        for (int i = 0; i < lineCount; i++)
        {
            float z = Mathf.Lerp(1f, 18f, i / (float)(lineCount - 1));
            float alpha = Mathf.Lerp(0.06f, 0.02f, i / (float)(lineCount - 1));
            BuildDepthLine(z, alpha);
        }

        // Vertical side lines for corridor feel
        BuildSideLine(-2.5f, 0.05f);
        BuildSideLine(3.0f,  0.05f);
    }

    private void BuildDepthLine(float z, float alpha)
    {
        var go = new GameObject($"DepthLine_{z:F0}");
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount     = 2;
        lr.useWorldSpace     = true;
        lr.startWidth        = 0.03f;
        lr.endWidth          = 0.03f;
        lr.startColor        = new Color(1f, 1f, 1f, alpha);
        lr.endColor          = new Color(1f, 1f, 1f, alpha);
        lr.sortingOrder      = -2;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows    = false;

        var mat = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
        lr.sharedMaterial = mat;

        lr.SetPosition(0, new Vector3(-4f, -0.8f, z));
        lr.SetPosition(1, new Vector3(5f,  -0.8f, z));
    }

    private void BuildSideLine(float x, float alpha)
    {
        var go = new GameObject($"SideLine_{x:F1}");
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount     = 2;
        lr.useWorldSpace     = true;
        lr.startWidth        = 0.03f;
        lr.endWidth          = 0.01f;
        lr.startColor        = new Color(1f, 1f, 1f, alpha);
        lr.endColor          = new Color(1f, 1f, 1f, 0f);
        lr.sortingOrder      = -2;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows    = false;

        var mat = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
        lr.sharedMaterial = mat;

        lr.SetPosition(0, new Vector3(x, -0.8f, 0f));
        lr.SetPosition(1, new Vector3(x, -0.8f, 20f));
    }

    /// <summary>Screen-space vignette drawn via a UI image on the HUD canvas.</summary>
    private void BuildVignette()
    {
        // Handled in BuildUI via BuildVignetteUI
    }

    // ── Game Manager ──────────────────────────────────────────────────────────

    private void BuildGameManager()
    {
        var go      = new GameObject("PGGameManager");
        gameManager = go.AddComponent<PGGameManager>();
        gameManager.settings   = settings;
        gameManager.parryMusic = parryMusic;

        // Bootstrap AudioManager si la scène est démarrée directement
        if (AudioManager.Instance == null)
        {
            var amGO = new GameObject("AudioManager");
            var am   = amGO.AddComponent<AudioManager>();
            am.parryMusic = parryMusic;
            am.clickSfx   = clickSfx;
        }
        else
        {
            if (parryMusic != null) AudioManager.Instance.parryMusic = parryMusic;
            if (clickSfx   != null) AudioManager.Instance.clickSfx   = clickSfx;
        }

        // ButtonClickAudio
        if (FindFirstObjectByType<ButtonClickAudio>() == null)
            new GameObject("ButtonClickAudio").AddComponent<ButtonClickAudio>();
    }

    // ── Player ────────────────────────────────────────────────────────────────

    private void BuildPlayer()
    {
        var go = new GameObject("Player");
        go.transform.position = new Vector3(0.4f, -0.6f, 0f);

        // Visuel procédural : ovale blanc + éclairs internes
        PGPlayerVisuals.Build(go.transform);

        player = go.AddComponent<PGPlayerController>();
        player.settings = settings;
    }

    // ── Enemy Spawner ─────────────────────────────────────────────────────────

    private void BuildSpawner()
    {
        var go  = new GameObject("PGEnemySpawner");
        spawner = go.AddComponent<PGEnemySpawner>();
        spawner.settings = settings;
    }

    // ── Ability System ────────────────────────────────────────────────────────

    private void BuildAbilitySystem()
    {
        var go        = new GameObject("PGAbilitySystem");
        abilitySystem = go.AddComponent<PGAbilitySystem>();
        abilitySystem.settings = settings;
    }

    // ── UI Canvas ─────────────────────────────────────────────────────────────

    private void BuildUI()
    {
        var canvasGO = new GameObject("PGCanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler                 = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight  = 1f;

        canvasGO.AddComponent<GraphicRaycaster>();

        var canvasRT = canvas.GetComponent<RectTransform>();

        // ── Points rebondissants (derrière tout) ──────────────────────────────
        var fireflyGO = new GameObject("BouncingDots");
        fireflyGO.transform.SetParent(canvasRT, false);
        var fireflyRT        = fireflyGO.AddComponent<RectTransform>();
        fireflyRT.anchorMin  = Vector2.zero;
        fireflyRT.anchorMax  = Vector2.one;
        fireflyRT.offsetMin  = fireflyRT.offsetMax = Vector2.zero;
        fireflyGO.transform.SetAsFirstSibling();
        var fireflies = fireflyGO.AddComponent<MenuBouncingDots>();
        fireflies.Init(fireflyRT);

        // ── Anomalie blanche (coins bas-gauche et bas-droite) ─────────────────
        var anomalyGO = new GameObject("PGAnomaly");
        anomalyGO.transform.SetParent(canvasRT, false);
        var anomalyRT        = anomalyGO.AddComponent<RectTransform>();
        anomalyRT.anchorMin  = Vector2.zero;
        anomalyRT.anchorMax  = Vector2.one;
        anomalyRT.offsetMin  = anomalyRT.offsetMax = Vector2.zero;
        anomalyGO.transform.SetSiblingIndex(1);   // juste au-dessus des dots
        anomalyGO.AddComponent<PGAnomalyUI>().Init(canvasRT);

        // ── HUD ───────────────────────────────────────────────────────────────
        var hudGO = new GameObject("PGHUD");
        hudGO.transform.SetParent(canvasRT, false);
        var hudRT = hudGO.AddComponent<RectTransform>();
        StretchFull(hudRT);
        hud = hudGO.AddComponent<PGHUD>();
        hud.settings = settings;
        hud.Init(canvasRT);

        // ── Game Over overlay ─────────────────────────────────────────────────
        var goOverlayGO = new GameObject("PGGameOverUI");
        goOverlayGO.transform.SetParent(canvasRT, false);
        var goRT = goOverlayGO.AddComponent<RectTransform>();
        StretchFull(goRT);
        gameOverUI = goOverlayGO.AddComponent<PGGameOverUI>();
        gameOverUI.Init(canvasRT);

        // ── Vignette (cosmétique, au-dessus de tout) ──────────────────────────
        BuildVignetteUI(canvasRT);

        // ── Feedbacks visuels (explosion, danger, combo, amélioration) ────────
        PGFeedback.Spawn(canvasRT);
    }

    private void BuildVignetteUI(RectTransform canvasRT)
    {
        var go  = new GameObject("Vignette");
        go.transform.SetParent(canvasRT, false);
        var img = go.AddComponent<Image>();

        // Radial vignette using a circle sprite inverted
        img.color = new Color(0f, 0f, 0f, 0f); // transparent center
        img.raycastTarget = false;

        var rt      = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        // Edge darkening — four thin edge panels
        string[] names = { "VigL", "VigR", "VigT", "VigB" };
        Vector2[] amin = { new(0, 0), new(0.85f, 0), new(0, 0.85f), new(0, 0) };
        Vector2[] amax = { new(0.15f, 1), new(1, 1), new(1, 1), new(1, 0.15f) };

        for (int i = 0; i < names.Length; i++)
        {
            var eGO  = new GameObject(names[i]);
            eGO.transform.SetParent(canvasRT, false);
            var eImg = eGO.AddComponent<Image>();
            eImg.color = new Color(0f, 0f, 0f, 0.35f);
            eImg.raycastTarget = false;
            var eRT       = eImg.rectTransform;
            eRT.anchorMin = amin[i];
            eRT.anchorMax = amax[i];
            eRT.offsetMin = eRT.offsetMax = Vector2.zero;
        }
    }

    // ── Wire references ───────────────────────────────────────────────────────

    private void WireReferences()
    {
        if (player != null)
            player.enemySpawner = spawner;

        if (abilitySystem != null)
            abilitySystem.enemySpawner = spawner;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    private static void EnsureSceneTransition()
    {
        if (SceneTransition.Instance != null) return;
        new GameObject("SceneTransition").AddComponent<SceneTransition>();
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
