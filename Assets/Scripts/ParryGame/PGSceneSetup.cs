using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
        var camGO = new GameObject("Camera");
        gameCamera = camGO.AddComponent<Camera>();

        // Perspective for depth effect
        gameCamera.orthographic  = false;
        gameCamera.fieldOfView   = settings != null ? settings.cameraFov : 60f;
        gameCamera.clearFlags    = CameraClearFlags.SolidColor;
        gameCamera.backgroundColor = new Color(0.04f, 0.04f, 0.08f, 1f);
        gameCamera.nearClipPlane = 0.1f;
        gameCamera.farClipPlane  = 50f;

        // Third-person: slightly right, elevated, behind player
        float ox = settings != null ? settings.cameraOffsetX : 0.6f;
        float oy = settings != null ? settings.cameraOffsetY : 1.8f;
        float oz = settings != null ? settings.cameraOffsetZ : -7f;
        camGO.transform.position = new Vector3(ox, oy, oz);

        // Tilt down slightly toward player/enemies
        camGO.transform.rotation = Quaternion.Euler(8f, -4f, 0f);

        camGO.AddComponent<AudioListener>();
    }

    // ── Sprite loading ────────────────────────────────────────────────────────

    private static Sprite LoadSprite(string assetPath)
    {
#if UNITY_EDITOR
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (tex != null)
        {
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(assetPath))
                if (obj is Sprite sp) return sp;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), 100f);
        }
#endif
        return Resources.Load<Sprite>(System.IO.Path.GetFileNameWithoutExtension(assetPath));
    }

    // ── Background ────────────────────────────────────────────────────────────

    private void BuildBackground()
    {
        BuildGround();
        BuildDepthMarkers();
        BuildVignette();
    }

    /// <summary>
    /// Construit le fond en espace UI (RawImage plein écran) pour que fond jeu.png
    /// couvre exactement les 1080×1920 indépendamment de la caméra perspective.
    /// Appelé depuis BuildUI après création du canvas.
    /// </summary>
    private void BuildBackgroundUI(RectTransform canvasRT)
    {
        var go  = new GameObject("Background");
        go.transform.SetParent(canvasRT, false);
        go.transform.SetAsFirstSibling();   // derrière tout le reste

        Sprite fondSprite = LoadSprite("Assets/sprites/fond jeu.png");

        if (fondSprite != null)
        {
            var raw = go.AddComponent<RawImage>();
            raw.texture         = fondSprite.texture;
            raw.color           = Color.white;
            raw.raycastTarget   = false;
            var rt        = raw.rectTransform;
            rt.anchorMin  = Vector2.zero;
            rt.anchorMax  = Vector2.one;
            rt.offsetMin  = rt.offsetMax = Vector2.zero;
        }
        else
        {
            var img = go.AddComponent<Image>();
            img.color         = new Color(0.08f, 0.06f, 0.12f, 1f);
            img.raycastTarget = false;
            var rt        = img.rectTransform;
            rt.anchorMin  = Vector2.zero;
            rt.anchorMax  = Vector2.one;
            rt.offsetMin  = rt.offsetMax = Vector2.zero;
        }
    }

    /// <summary>A flat ground plane for depth markers — kept as subtle décor.</summary>
    private void BuildGround()
    {
        // Le fond principal est maintenant en UI (BuildBackgroundUI).
        // On garde juste un plan discret pour les lignes de profondeur.
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
        // Added to canvas after canvas build — handled in BuildUI
    }

    // ── Game Manager ──────────────────────────────────────────────────────────

    private void BuildGameManager()
    {
        var go      = new GameObject("PGGameManager");
        gameManager = go.AddComponent<PGGameManager>();
        gameManager.settings = settings;
    }

    // ── Player ────────────────────────────────────────────────────────────────

    private void BuildPlayer()
    {
        var go = new GameObject("Player");

        // Position: bottom-center slightly right of camera center,
        // at Z=0 (front of scene)
        go.transform.position = new Vector3(0.4f, -0.6f, 0f);

        // Player body — silhouette (dark capsule shape simulated with two quads)
        BuildPlayerVisual(go.transform);

        player = go.AddComponent<PGPlayerController>();
        player.settings = settings;
    }

    private void BuildPlayerVisual(Transform parent)
    {
        Sprite playerSprite = LoadSprite("Assets/sprites/personnage.png");

        if (playerSprite != null)
        {
            var go  = new GameObject("PlayerSprite");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0f, 0f);

            // Caméra perspective FOV 60° à z=-7 → hauteur visible ≈ 8.08u à z=0.
            // Le joueur doit occuper ~18% de la hauteur écran ≈ 1.45u de haut.
            // Sprite PPU=100, on cible 1.45u → scale = 1.45 / (texHeight/100).
            // On utilise 0.012 comme facteur universel (ajustable si PPU différent).
            const float targetHeightU = 1.45f;
            float       ppu           = playerSprite.pixelsPerUnit > 0 ? playerSprite.pixelsPerUnit : 100f;
            float       spriteHeightU = playerSprite.rect.height / ppu;
            float       s             = spriteHeightU > 0 ? targetHeightU / spriteHeightU : 0.012f;

            go.transform.localScale = new Vector3(s, s, s);
            var sr     = go.AddComponent<SpriteRenderer>();
            sr.sprite  = playerSprite;
            sr.sortingOrder = 5;
        }
        else
        {
            BuildPlayerVisualFallback(parent);
        }
    }

    private void BuildPlayerVisualFallback(Transform parent)
    {
        // Body (tall quad)
        var body = GameObject.CreatePrimitive(PrimitiveType.Quad);
        body.name = "PlayerBody";
        Destroy(body.GetComponent<Collider>());
        body.transform.SetParent(parent, false);
        body.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        body.transform.localScale    = new Vector3(0.55f, 1.1f, 1f);
        var bodyMat = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
        bodyMat.color = new Color(0.12f, 0.10f, 0.18f, 1f);
        body.GetComponent<Renderer>().material = bodyMat;

        // Head (small circle quad)
        var head = GameObject.CreatePrimitive(PrimitiveType.Quad);
        head.name = "PlayerHead";
        Destroy(head.GetComponent<Collider>());
        head.transform.SetParent(parent, false);
        head.transform.localPosition = new Vector3(0.05f, 1.1f, 0f);
        head.transform.localScale    = new Vector3(0.38f, 0.38f, 1f);
        var headMat = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
        headMat.color = new Color(0.18f, 0.15f, 0.25f, 1f);
        head.GetComponent<Renderer>().material = headMat;

        // Weapon arm
        var arm = GameObject.CreatePrimitive(PrimitiveType.Quad);
        arm.name = "WeaponArm";
        Destroy(arm.GetComponent<Collider>());
        arm.transform.SetParent(parent, false);
        arm.transform.localPosition = new Vector3(0.35f, 0.55f, -0.05f);
        arm.transform.localRotation = Quaternion.Euler(0f, 0f, -35f);
        arm.transform.localScale    = new Vector3(0.10f, 0.85f, 1f);
        var armMat = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
        armMat.color = new Color(0.75f, 0.70f, 0.85f, 1f);
        arm.GetComponent<Renderer>().material = armMat;

        // Weapon blade
        var blade = GameObject.CreatePrimitive(PrimitiveType.Quad);
        blade.name = "WeaponBlade";
        Destroy(blade.GetComponent<Collider>());
        blade.transform.SetParent(parent, false);
        blade.transform.localPosition = new Vector3(0.6f, 0.9f, -0.05f);
        blade.transform.localRotation = Quaternion.Euler(0f, 0f, -55f);
        blade.transform.localScale    = new Vector3(0.07f, 0.7f, 1f);
        var bladeMat = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
        bladeMat.color = new Color(0.9f, 0.9f, 1f, 1f);
        blade.GetComponent<Renderer>().material = bladeMat;
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

        // ── Fond plein écran (derrière tout) ──────────────────────────────────
        BuildBackgroundUI(canvasRT);

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
