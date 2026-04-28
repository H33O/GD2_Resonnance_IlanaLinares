using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Génère procéduralement un niveau TiltBall en fonction de l'index (0 à 9).
///
/// Le monde est TOUJOURS calé sur 1080 × 1920 pixels (9:16) :
///   Caméra orthographique size = 9.6  →  hauteur = 19.2 u  →  1920 px
///   Largeur = 10.8 u → 1080 px
///   x ∈ [−5.4, 5.4]    y ∈ [−9.6, 9.6]
///
/// La caméra est FIXE au centre (0, 0, −10). Pas de scroll, pas de double-slide.
/// Le joueur spawn en bas (y ≈ −8), le trou en haut (y ≈ 7.5).
/// Niveaux impairs : clé requise avant le trou.
/// </summary>
public class TBSceneSetup : MonoBehaviour
{
    // ── Constantes monde (1080 × 1920, ratio 9:16) ───────────────────────────

    /// <summary>OrthoSize de la caméra → hauteur monde = 2 × 9.6 = 19.2 u = 1920 px.</summary>
    public const float OrthoSize     = 9.6f;
    /// <summary>Demi-largeur monde = 1080 / (1920 / 9.6) = 5.4 u.</summary>
    public const float HalfW         = 5.4f;
    /// <summary>Demi-hauteur monde = OrthoSize = 9.6 u.</summary>
    public const float HalfH         = 9.6f;
    public const float WallThickness = 0.28f;

    /// <summary>Joueur : bas de l'écran avec marge.</summary>
    private const float PlayerSpawnY = -7.8f;
    /// <summary>Trou : haut de l'écran avec marge (doit rester dans [-HalfH, HalfH]).</summary>
    private const float HoleY        =  7.5f;

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Tooltip("ScriptableObject de sprites. Si null, des sprites procéduraux sont utilisés.")]
    [SerializeField] public TBLevelPrefabsData prefabsData;

    [Header("Audio")]
    [Tooltip("Musique du TiltBall (fightgame music.mp3).")]
    [SerializeField] public AudioClip fightMusic;

    [Tooltip("Son de clic UI (clic.mp3) — utilisé si l'AudioManager est absent.")]
    [SerializeField] public AudioClip clickSfx;

    [Tooltip("Son joué quand une amélioration est achetée.")]
    [SerializeField] public AudioClip upgradeSfx;

    [Tooltip("Son joué quand le joueur entre dans le goal.")]
    [SerializeField] public AudioClip goalSfx;

    [Tooltip("Son joué quand le joueur meurt.")]
    [SerializeField] public AudioClip deathSfx;

    [Tooltip("Son joué quand un ennemi est tué.")]
    [SerializeField] public AudioClip enemyDeathSfx;

    // ── Référence statique (accessible par RebuildLevel) ──────────────────────

    private static TBLevelPrefabsData s_prefabsData;

    // ── Palette fallback ──────────────────────────────────────────────────────

    private static readonly Color ColBg         = new Color(0.06f, 0.06f, 0.10f, 1f);
    private static readonly Color ColWall        = new Color(0.88f, 0.88f, 0.92f, 1f);
    private static readonly Color ColObstacle    = new Color(0.20f, 0.20f, 0.30f, 1f);
    private static readonly Color ColPlayer      = new Color(0.18f, 0.82f, 0.22f, 1f);
    private static readonly Color ColEnemy       = new Color(0.90f, 0.14f, 0.14f, 1f);
    private static readonly Color ColHoleOpen    = new Color(0.55f, 0.27f, 0.07f, 1f);
    private static readonly Color ColHoleLocked  = new Color(0.30f, 0.15f, 0.03f, 1f);
    private static readonly Color ColKey         = new Color(1.00f, 0.85f, 0.00f, 1f);

    // ── Tag du contenu de niveau ──────────────────────────────────────────────

    public const string LevelContentTag = "Obstacle";

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        TBGameManager.EnsureExists();
        if (prefabsData != null) s_prefabsData = prefabsData;

        // Injecte la musique dans le TBGameManager (persistant DontDestroyOnLoad)
        if (TBGameManager.Instance != null && fightMusic != null)
            TBGameManager.Instance.fightMusic = fightMusic;

        // Injecte les SFX dans le TBGameManager
        if (TBGameManager.Instance != null)
        {
            if (upgradeSfx    != null) TBGameManager.Instance.upgradeSfx    = upgradeSfx;
            if (goalSfx       != null) TBGameManager.Instance.goalSfx       = goalSfx;
            if (deathSfx      != null) TBGameManager.Instance.deathSfx      = deathSfx;
            if (enemyDeathSfx != null) TBGameManager.Instance.enemyDeathSfx = enemyDeathSfx;
        }

        // Bootstrap AudioManager si la scène est démarrée directement
        if (AudioManager.Instance == null)
        {
            var amGO = new GameObject("AudioManager");
            var am   = amGO.AddComponent<AudioManager>();
            am.tiltBallMusic = fightMusic;
            am.clickSfx      = clickSfx;
        }
        else
        {
            if (fightMusic != null) AudioManager.Instance.tiltBallMusic = fightMusic;
            if (clickSfx   != null) AudioManager.Instance.clickSfx      = clickSfx;
        }

        // ButtonClickAudio
        if (FindFirstObjectByType<ButtonClickAudio>() == null)
            new GameObject("ButtonClickAudio").AddComponent<ButtonClickAudio>();
    }

    private void Start()
    {
        // Démarre la musique ici (Start) pour garantir que tous les Awake()
        // — TBGameManager, AudioManager — ont déjà été exécutés.
        if (fightMusic != null)
            AudioManager.Instance?.PlayMusic(fightMusic);

        int levelIndex = TBGameManager.Instance != null ? TBGameManager.Instance.LevelIndex : 0;
        BuildLevel(levelIndex);
        TBGameManager.Instance?.StartLevel(levelIndex);
        EnsureEventSystem();
        BuildHud(levelIndex);
    }

    // ── Point d'entrée public ─────────────────────────────────────────────────

    /// <summary>Détruit l'ancien contenu et reconstruit le niveau dans la même scène.</summary>
    public static void RebuildLevel(int levelIndex)
    {
        foreach (var go in GameObject.FindGameObjectsWithTag(LevelContentTag))
            Destroy(go);

        // Retire TBCameraFollow si présent (ne sert plus)
        var cam = Camera.main;
        if (cam != null)
        {
            var follow = cam.GetComponent<TBCameraFollow>();
            if (follow != null) Destroy(follow);
        }

        DestroyByName("Player", "Background", "Hole", "Key",
                      "HUD", "WallTop", "WallBottom", "WallLeft", "WallRight",
                      "EnemySpawner",
                      "KillZoneTop", "KillZoneBottom", "KillZoneLeft", "KillZoneRight",
                      "TBFireflies", "TBGrid");

        for (int i = 1; i <= 10; i++)
            DestroyByName($"Enemy{i}", $"Obs{i}A", $"Obs{i}B", $"Obs{i}C");

        var setup = new GameObject("__LevelSetup__").AddComponent<TBSceneSetup>();
        setup.BuildLevel(levelIndex);

        TBGameManager.Instance?.StartLevel(levelIndex);
        setup.BuildHud(levelIndex);
        EnsureEventSystem();

        Destroy(setup.gameObject);
    }

    private static void DestroyByName(params string[] names)
    {
        foreach (var n in names)
        {
            var go = GameObject.Find(n);
            if (go != null) Destroy(go);
        }
    }

    // ── Construction d'un niveau ──────────────────────────────────────────────

    public void BuildLevel(int levelIndex)
    {
        bool requireKey   = (levelIndex % 2 == 1);
        int  enemyCount   = Mathf.Min(1 + levelIndex / 2, 5);
        float moveInterval = Mathf.Lerp(0.50f, 0.20f, levelIndex / 9f);

        TBGrid.MoveInterval = moveInterval;
        TBGrid.MaxY         = HalfH - WallThickness - 0.5f;

        var d = s_prefabsData;
        TBUIStyle.Init(d?.uiFont, d?.jaugeSprite);

        SetupCamera();
        BuildBackground(null);   // fond noir procédural, sprite ignoré
        BuildBoundaries(d != null ? d.wallColor : ColWall);
        BuildObstaclesForLevel(levelIndex, d != null ? d.obstacleColor : ColObstacle,
                               d?.obstacleSprite, d?.obstaclePrefab);
        BuildHole(levelIndex, requireKey, d, null);   // glow vert, sprite ignoré
        BuildPlayer(d);
        BuildEnemies(levelIndex, enemyCount, d, null);  // balle rouge procédurale
        if (requireKey) BuildKey(levelIndex, d);

        TBEnemySpawner.Create(levelIndex, new Color(1f, 0.07f, 0.07f, 1f), null);

        SpawnUpgrades();
    }

    // ── Spawn des améliorations ───────────────────────────────────────────────

    private static void SpawnUpgrades()
    {
        var upgrades = TBGameManager.Instance?.Upgrades;
        if (upgrades == null) return;

        var allyPositions = new Vector2[]
        {
            new Vector2(-1.5f, PlayerSpawnY + 2.5f),
            new Vector2( 1.5f, PlayerSpawnY + 2.5f),
            new Vector2( 0.0f, PlayerSpawnY + 4.0f),
        };
        for (int i = 0; i < upgrades.AllyCount; i++)
            TBAlly.Spawn(allyPositions[i], new Color(0.20f, 0.80f, 1.00f, 1f));

        if (upgrades.HasWeapon)  TBWeapon.Spawn();
        if (upgrades.BarrierCount > 0) TBBarrier.SpawnAll(upgrades.BarrierCount);
    }

    // ── Chargement sprite ─────────────────────────────────────────────────────

    private static Sprite LoadSprite(string assetPath)
    {
#if UNITY_EDITOR
        var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (tex != null)
        {
            var objs = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (var obj in objs)
                if (obj is Sprite sp) return sp;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), 100f);
        }
#endif
        string name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        return Resources.Load<Sprite>(name);
    }

    // ── Caméra — fixe au centre ───────────────────────────────────────────────

    private static void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        cam.orthographic      = true;
        cam.orthographicSize  = OrthoSize;
        cam.clearFlags        = CameraClearFlags.SolidColor;
        cam.backgroundColor   = Color.black;
        cam.transform.position = new Vector3(0f, 0f, -10f);   // fixe, jamais déplacée

        // Supprime TBCameraFollow si présent — la caméra est fixe dans ce mode
        var follow = cam.GetComponent<TBCameraFollow>();
        if (follow != null) Destroy(follow);
    }

    // ── Fond noir + particules blanches ──────────────────────────────────────

    private static void BuildBackground(Sprite _)   // sprite ignoré — fond procédural
    {
        // Fond noir pur (la caméra est déjà clearFlags=SolidColor/black, mais on
        // ajoute un SR pour être certain même si clearFlags change)
        var bg  = new GameObject("Background");
        var sr  = bg.AddComponent<SpriteRenderer>();
        sr.sprite       = SpriteGenerator.CreateWhiteSquare();
        sr.color        = new Color(0f, 0f, 0f, 1f);
        sr.sortingOrder = -11;
        bg.transform.localScale = new Vector3(HalfW * 2f, HalfH * 2f, 1f);

        // Particules blanches rebondissantes (identique au Menu)
        var dotsGO = new GameObject("TBFireflies");
        dotsGO.AddComponent<TBWorldDots>().Init(HalfW, HalfH);

        // Grille translucide
        BuildWorldGrid();
    }

    private static void BuildWorldGrid()
    {
        float h = HalfH * 2f;
        float w = HalfW * 2f;

        var root = new GameObject("TBGrid");

        for (int i = 1; i <= 5; i++)
        {
            float x = -HalfW + w * (i / 6f);
            MakeAmbientLine(root.transform, new Vector3(x, 0f, 0f), true,  h);
        }

        for (int i = 1; i <= 9; i++)
        {
            float y = -HalfH + h * (i / 10f);
            MakeAmbientLine(root.transform, new Vector3(0f, y, 0f), false, w);
        }
    }

    private static void MakeAmbientLine(Transform parent, Vector3 pos, bool vertical, float length)
    {
        var go = new GameObject(vertical ? "VLine" : "HLine");
        go.transform.SetParent(parent, false);
        go.transform.position = pos;

        var sr          = go.AddComponent<SpriteRenderer>();
        sr.sprite       = SpriteGenerator.CreateCircle(4);
        sr.color        = new Color(1f, 1f, 1f, 0.04f);
        sr.sortingOrder = -9;

        go.transform.localScale = vertical
            ? new Vector3(0.02f, length, 1f)
            : new Vector3(length, 0.02f, 1f);
    }

    // ── Murs — cadre exact du monde ───────────────────────────────────────────

    private static void BuildBoundaries(Color wallColor)
    {
        float t = WallThickness;
        // Le centre du mur est décalé vers l'extérieur afin que le bord interne
        // soit exactement aligné sur le bord écran (±HalfH / ±HalfW = 1920/1080 px).
        MakeWall("WallTop",    0f,               HalfH + t * 0.5f,  HalfW * 2f + t * 2f, t,           wallColor);
        MakeWall("WallBottom", 0f,              -HalfH - t * 0.5f,  HalfW * 2f + t * 2f, t,           wallColor);
        MakeWall("WallLeft",  -HalfW - t * 0.5f, 0f,                t,                   HalfH * 2f + t * 2f, wallColor);
        MakeWall("WallRight",  HalfW + t * 0.5f, 0f,                t,                   HalfH * 2f + t * 2f, wallColor);
    }

    // ── Trou / Goal ───────────────────────────────────────────────────────────

    private static readonly float[] HoleXByLevel = {
        0.0f, -3.5f,  3.5f, -3.0f,  2.5f,
       -4.0f,  3.5f, -2.0f,  1.0f, -3.5f,
    };

    private static void BuildHole(int levelIndex, bool requireKey,
                                   TBLevelPrefabsData d, Sprite _)   // sprite ignoré
    {
        float x   = HoleXByLevel[Mathf.Clamp(levelIndex, 0, 9)];
        float y   = HoleY - (levelIndex % 3) * 0.4f;
        var   pos = new Vector3(x, y, 0f);

        GameObject go;
        if (d?.holePrefab != null)
        {
            go      = Object.Instantiate(d.holePrefab, pos, Quaternion.identity);
            go.name = "Hole";
            if (go.GetComponent<TBHole>() == null) go.AddComponent<TBHole>();
        }
        else
        {
            go = new GameObject("Hole");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = SpriteGenerator.CreateCircle(128);
            sr.color        = new Color(0.10f, 0.95f, 0.30f, 1f);   // vert — remplacé par TBHoleGlow
            sr.sortingOrder = 5;
            go.transform.position   = pos;
            go.transform.localScale = new Vector3(0.90f, 0.90f, 1f);

            var col       = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius    = 0.45f;

            go.AddComponent<TBHole>();
        }

        // Glow vert pulsant (remplace la couleur locked/open par une couleur unique verte)
        var holeSr = go.GetComponent<SpriteRenderer>();
        if (holeSr != null)
            go.AddComponent<TBHoleGlow>().Init(holeSr);
    }

    // ── Joueur — balle blanche brillante ─────────────────────────────────────

    private static void BuildPlayer(TBLevelPrefabsData d)
    {
        var spawnPos = new Vector3(0f, PlayerSpawnY, 0f);
        GameObject go;

        if (d?.playerPrefab != null)
        {
            go      = Object.Instantiate(d.playerPrefab, spawnPos, Quaternion.identity);
            go.name = "Player";
            go.tag  = "Player";
            if (go.GetComponent<TBPlayerController>() == null)
                go.AddComponent<TBPlayerController>();
        }
        else
        {
            go     = new GameObject("Player");
            go.tag = "Player";

            var sr          = go.AddComponent<SpriteRenderer>();
            sr.sprite       = SpriteGenerator.CreateCircle(128);
            sr.color        = Color.white;
            sr.sortingOrder = 3;
            go.transform.position   = spawnPos;
            go.transform.localScale = new Vector3(0.85f, 0.85f, 1f);

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            var col       = go.AddComponent<CircleCollider2D>();
            col.radius    = 0.42f;
            col.isTrigger = true;  // l'ennemi traverse le joueur librement
            go.AddComponent<TBPlayerController>();
        }

        // Halo blanc pulsant permanent (géré par TBUpgradeFX)
        AttachUpgradeFX(go);
    }

    /// <summary>Attache <see cref="TBUpgradeFX"/> au joueur et l'initialise avec son SpriteRenderer.</summary>
    private static void AttachUpgradeFX(GameObject player)
    {
        var sr = player.GetComponent<SpriteRenderer>();
        if (sr == null) return;
        var fx = player.AddComponent<TBUpgradeFX>();
        fx.Init(sr);
    }

    // ── Ennemis — positions dans y ∈ [−8, 6] pour rester dans le monde ────────

    private static readonly Vector2[][] EnemyPositionsByLevel =
    {
        /* 0 */ new[] { new Vector2( 2.5f,  0.0f) },
        /* 1 */ new[] { new Vector2(-2.5f,  2.0f), new Vector2( 2.5f, -3.0f) },
        /* 2 */ new[] { new Vector2(-3.0f,  3.5f), new Vector2( 3.0f, -1.5f) },
        /* 3 */ new[] { new Vector2( 3.0f,  4.5f), new Vector2(-3.0f, -2.5f), new Vector2( 0.0f,  1.5f) },
        /* 4 */ new[] { new Vector2(-3.5f,  4.0f), new Vector2( 3.5f, -1.0f), new Vector2(-1.0f, -5.0f) },
        /* 5 */ new[] { new Vector2( 3.5f,  5.0f), new Vector2(-3.0f,  0.5f), new Vector2( 1.5f, -3.5f), new Vector2(-2.0f,-6.5f) },
        /* 6 */ new[] { new Vector2(-4.0f,  5.5f), new Vector2( 4.0f,  1.5f), new Vector2(-2.0f, -2.5f), new Vector2( 2.0f,-6.0f) },
        /* 7 */ new[] { new Vector2( 3.5f,  5.5f), new Vector2(-3.5f,  2.0f), new Vector2( 3.0f, -1.5f), new Vector2(-3.0f,-5.5f), new Vector2( 0.0f,-7.0f) },
        /* 8 */ new[] { new Vector2(-4.0f,  5.0f), new Vector2( 4.0f,  2.0f), new Vector2(-2.0f, -1.0f), new Vector2( 2.0f,-5.0f), new Vector2( 0.0f,-7.0f) },
        /* 9 */ new[] { new Vector2( 4.0f,  5.5f), new Vector2(-4.0f,  2.5f), new Vector2( 3.0f, -0.5f), new Vector2(-3.0f,-4.5f), new Vector2( 0.5f,-7.0f) },
    };

    private static void BuildEnemies(int levelIndex, int enemyCount, TBLevelPrefabsData d, Sprite fallbackEnemySprite)
    {
        var positions = EnemyPositionsByLevel[Mathf.Clamp(levelIndex, 0, 9)];
        int count     = Mathf.Min(enemyCount, positions.Length);
        for (int i = 0; i < count; i++)
            SpawnEnemy($"Enemy{i + 1}", positions[i], d, fallbackEnemySprite);
    }

    private static void SpawnEnemy(string name, Vector2 position, TBLevelPrefabsData d, Sprite _)
    {
        // ── Corps ─────────────────────────────────────────────────────────────
        var go = new GameObject(name);
        go.tag = LevelContentTag;

        var sr          = go.AddComponent<SpriteRenderer>();
        sr.sprite       = SpriteGenerator.CreateCircle(128);
        sr.color        = new Color(1f, 0.07f, 0.07f, 1f);   // rouge vif — TBEnemyVisuals le confirmera
        sr.sortingOrder = 2;

        go.transform.position   = position;
        go.transform.localScale = new Vector3(0.75f, 0.75f, 1f);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType     = RigidbodyType2D.Kinematic;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;

        var col       = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.50f;

        // ── Yeux noirs ────────────────────────────────────────────────────────
        BuildEnemyEye(go.transform,  0.22f, 0.20f);   // œil gauche
        BuildEnemyEye(go.transform, -0.22f, 0.20f);   // œil droit

        go.AddComponent<TBEnemyController>();
    }

    /// <summary>Crée un petit œil noir en local-space de l'ennemi.</summary>
    private static void BuildEnemyEye(Transform parent, float localX, float localY)
    {
        var eye = new GameObject("Eye");
        eye.transform.SetParent(parent, false);
        eye.transform.localPosition = new Vector3(localX, localY, 0f);
        eye.transform.localScale    = new Vector3(0.16f, 0.18f, 1f);

        var sr          = eye.AddComponent<SpriteRenderer>();
        sr.sprite       = SpriteGenerator.CreateCircle(32);
        sr.color        = new Color(0f, 0f, 0f, 0.90f);
        sr.sortingOrder = 3;   // au-dessus du corps
    }

    // ── Clé — positions dans y ∈ [−5, 4] ─────────────────────────────────────

    private static readonly Vector2[] KeyPositionsByLevel =
    {
        Vector2.zero,               // 0 — pas de clé
        new Vector2(-1.5f, -2.5f),  // 1
        Vector2.zero,               // 2
        new Vector2( 2.0f,  1.5f),  // 3
        Vector2.zero,               // 4
        new Vector2(-2.5f,  2.0f),  // 5
        Vector2.zero,               // 6
        new Vector2( 2.5f,  3.0f),  // 7
        Vector2.zero,               // 8
        new Vector2(-3.0f,  1.5f),  // 9
    };

    private static void BuildKey(int levelIndex, TBLevelPrefabsData d)
    {
        Vector2 pos = KeyPositionsByLevel[Mathf.Clamp(levelIndex, 0, 9)];
        var go = new GameObject("Key");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = d?.keySprite != null ? d.keySprite : SpriteGenerator.CreatePolygon(4, 64);
        sr.color        = d != null ? d.keyColor : ColKey;
        sr.sortingOrder = 2;
        go.transform.position   = new Vector3(pos.x, pos.y, 0f);
        go.transform.localScale = new Vector3(0.65f, 0.65f, 1f);
        var col       = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size      = Vector2.one;
        go.AddComponent<TBKey>();
    }

    // ── Labyrinthe — obstacles dans y ∈ [−9.3, 9.3] ─────────────────────────

    private static void BuildObstaclesForLevel(int levelIndex, Color obsColor, Sprite obsSprite, GameObject obsPrefab)
    {
        // Aucun obstacle de level design — terrain ouvert sur tous les niveaux.
    }

    // ── HUD ───────────────────────────────────────────────────────────────────

    private void BuildHud(int levelIndex)
    {
        var existing = GameObject.Find("HUD");
        if (existing != null) Destroy(existing);

        bool needKey = (levelIndex % 2 == 1);

        var canvasGO = new GameObject("HUD");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        var scaler                 = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight  = 1f;

        canvasGO.AddComponent<GraphicRaycaster>();

        var canvasRT = canvas.GetComponent<RectTransform>();

        // ── HUD principal ─────────────────────────────────────────────────────
        var hud = canvasGO.AddComponent<TBHud>();
        hud.Init(needKey, canvasRT, levelIndex);
    }

    // ── Builders helpers ──────────────────────────────────────────────────────

    private static void MakeWall(string name, float x, float y, float w, float h, Color color)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = SpriteGenerator.CreateWhiteSquare();
        sr.color        = color;
        sr.sortingOrder = 1;
        go.transform.position   = new Vector3(x, y, 0f);
        go.transform.localScale = new Vector3(w, h, 1f);
        go.AddComponent<BoxCollider2D>();
    }

    private static void MakeObs(string name, float x, float y, float w, float h,
                                Color color, Sprite sprite, GameObject prefab = null)
    {
        GameObject go;
        if (prefab != null)
        {
            go      = Object.Instantiate(prefab, new Vector3(x, y, 0f), Quaternion.identity);
            go.name = name;
            var sr  = go.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = color;
            go.transform.localScale = new Vector3(w, h, 1f);
            if (go.GetComponent<BoxCollider2D>() == null)
                go.AddComponent<BoxCollider2D>();
        }
        else
        {
            go = new GameObject(name);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = sprite != null ? sprite : SpriteGenerator.CreateWhiteSquare();
            sr.color        = color;
            sr.sortingOrder = 1;
            go.transform.position   = new Vector3(x, y, 0f);
            go.transform.localScale = new Vector3(w, h, 1f);
            go.AddComponent<BoxCollider2D>();
        }
        go.tag = LevelContentTag;
    }

    // ── EventSystem ───────────────────────────────────────────────────────────

    public static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }
}
