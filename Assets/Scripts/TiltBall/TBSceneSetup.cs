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
    }

    private void Start()
    {
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
                      "KillZoneTop", "KillZoneBottom", "KillZoneLeft", "KillZoneRight");

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

        Sprite fondJeuSprite = d?.backgroundSprite ?? LoadSprite("Assets/sprites/fond jeu.png");
        Sprite enemySprite   = d?.enemySprite      ?? LoadSprite("Assets/sprites/ENNEMIS.png");
        Sprite goalSprite    = d?.holeSprite        ?? LoadSprite("Assets/sprites/goal trou.png");

        SetupCamera();
        BuildBackground(fondJeuSprite);
        BuildBoundaries(d != null ? d.wallColor : ColWall);
        BuildObstaclesForLevel(levelIndex, d != null ? d.obstacleColor : ColObstacle,
                               d?.obstacleSprite, d?.obstaclePrefab);
        BuildHole(levelIndex, requireKey, d, goalSprite);
        BuildPlayer(d);
        BuildEnemies(levelIndex, enemyCount, d, enemySprite);
        if (requireKey) BuildKey(levelIndex, d);

        TBEnemySpawner.Create(levelIndex, d != null ? d.enemyColor : ColEnemy,
                              enemySprite ?? d?.enemySprite);

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

    // ── Fond — couvre exactement 10.8 × 19.2 unités ──────────────────────────

    private static void BuildBackground(Sprite fondSprite)
    {
        var go = new GameObject("Background");
        var sr = go.AddComponent<SpriteRenderer>();
        if (fondSprite != null)
        {
            sr.sprite       = fondSprite;
            sr.drawMode     = SpriteDrawMode.Tiled;
            sr.tileMode     = SpriteTileMode.Continuous;
            sr.color        = Color.white;
            sr.sortingOrder = -10;
            // Dimensionner pour couvrir exactement le monde 10.8 × 19.2
            float scaleX = (HalfW * 2f) / fondSprite.bounds.size.x;
            float scaleY = (HalfH * 2f) / fondSprite.bounds.size.y;
            go.transform.localScale = new Vector3(scaleX, scaleY, 1f);
        }
        else
        {
            sr.sprite       = SpriteGenerator.CreateWhiteSquare();
            sr.color        = ColBg;
            sr.sortingOrder = -10;
            go.transform.localScale = new Vector3(HalfW * 2f, HalfH * 2f, 1f);
        }
    }

    // ── Murs — cadre exact du monde ───────────────────────────────────────────

    private static void BuildBoundaries(Color wallColor)
    {
        float t = WallThickness;
        MakeWall("WallTop",    0f,              HalfH - t * 0.5f,  HalfW * 2f, t,         wallColor);
        MakeWall("WallBottom", 0f,             -HalfH + t * 0.5f,  HalfW * 2f, t,         wallColor);
        MakeWall("WallLeft",  -HalfW + t * 0.5f, 0f,               t,          HalfH * 2f, wallColor);
        MakeWall("WallRight",  HalfW - t * 0.5f, 0f,               t,          HalfH * 2f, wallColor);
    }

    // ── Trou / Goal — positionné dans y ∈ [−9.6, 9.6] ────────────────────────

    private static readonly float[] HoleXByLevel = {
        0.0f, -3.5f,  3.5f, -3.0f,  2.5f,
       -4.0f,  3.5f, -2.0f,  1.0f, -3.5f,
    };

    private static void BuildHole(int levelIndex, bool requireKey, TBLevelPrefabsData d, Sprite fallbackGoalSprite)
    {
        float x   = HoleXByLevel[Mathf.Clamp(levelIndex, 0, 9)];
        // Le trou est dans la moitié haute : y ∈ [6.5, 7.5] selon le niveau
        float y   = HoleY - (levelIndex % 3) * 0.4f;
        var   pos = new Vector3(x, y, 0f);

        GameObject go;
        if (d?.holePrefab != null)
        {
            go      = Object.Instantiate(d.holePrefab, pos, Quaternion.identity);
            go.name = "Hole";
            var sr  = go.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = requireKey ? d.holeLockedColor : d.holeOpenColor;
            if (go.GetComponent<TBHole>() == null) go.AddComponent<TBHole>();
        }
        else
        {
            Sprite goalSprite = d?.holeSprite ?? fallbackGoalSprite;

            go = new GameObject("Hole");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = goalSprite != null ? goalSprite : SpriteGenerator.CreateCircle(128);
            sr.color        = requireKey ? (d != null ? d.holeLockedColor : ColHoleLocked) : (d != null ? d.holeOpenColor : ColHoleOpen);
            sr.sortingOrder = 5;
            go.transform.position   = pos;
            go.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

            var col       = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius    = 0.55f;

            go.AddComponent<TBHole>();
        }
    }

    // ── Joueur ────────────────────────────────────────────────────────────────

    private static void BuildPlayer(TBLevelPrefabsData d)
    {
        var spawnPos = new Vector3(0f, PlayerSpawnY, 0f);
        GameObject go;

        if (d?.playerPrefab != null)
        {
            go      = Object.Instantiate(d.playerPrefab, spawnPos, Quaternion.identity);
            go.name = "Player";
            go.tag  = "Player";
            var sr  = go.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = d.playerColor;
            if (go.GetComponent<TBPlayerController>() == null)
                go.AddComponent<TBPlayerController>();
        }
        else
        {
            go     = new GameObject("Player");
            go.tag = "Player";
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = d?.playerSprite != null ? d.playerSprite : SpriteGenerator.CreateCircle(128);
            sr.color        = d != null ? d.playerColor : ColPlayer;
            sr.sortingOrder = 3;
            go.transform.position   = spawnPos;
            go.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            var col    = go.AddComponent<CircleCollider2D>();
            col.radius = 0.42f;
            go.AddComponent<TBPlayerController>();
        }
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

    private static void SpawnEnemy(string name, Vector2 position, TBLevelPrefabsData d, Sprite fallbackEnemySprite)
    {
        var go = new GameObject(name);
        go.tag = LevelContentTag;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = d?.enemySprite ?? fallbackEnemySprite ?? SpriteGenerator.CreateCircle(128);
        sr.color        = d != null ? d.enemyColor : ColEnemy;
        sr.sortingOrder = 2;
        go.transform.position   = position;
        go.transform.localScale = new Vector3(0.18f, 0.18f, 1f);   // petit
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType     = RigidbodyType2D.Kinematic;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;
        var col       = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.22f;
        go.AddComponent<TBEnemyController>();
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
        float t = WallThickness;

        switch (levelIndex)
        {
            // ── Niveau 0 : couloir en S simple ───────────────────────────────
            case 0:
                MakeObs("Obs1A", -1.0f, -6.0f,  8.0f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs2A",  1.0f, -3.0f,  8.0f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs3A", -1.0f,  0.0f,  8.0f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs4A",  1.0f,  3.0f,  8.0f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs5A", -1.0f,  6.0f,  8.0f, t,  obsColor, obsSprite, obsPrefab);
                break;

            // ── Niveau 1 : chicane + piliers ─────────────────────────────────
            case 1:
                MakeObs("Obs1A", -1.5f, -6.0f,  8.0f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs2A",  1.5f, -3.5f,  8.0f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs3A", -1.5f, -1.0f,  8.0f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs4A",  1.5f,  1.5f,  8.0f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs5A", -1.5f,  4.0f,  8.0f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs6A",  4.0f, -4.8f,  t, 2.0f,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs7A", -4.0f,  0.5f,  t, 2.0f,  obsColor, obsSprite, obsPrefab);
                break;

            // ── Niveau 2 : spirale ────────────────────────────────────────────
            case 2:
                MakeObs("Obs1A",  0.0f, -6.5f,  9.5f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs2A",  4.5f, -2.5f,  t, 7.0f,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs3A",  0.0f,  1.0f,  7.5f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs4A", -4.0f, -2.0f,  t, 5.5f,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs5A",  1.5f, -3.5f,  5.5f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs6A",  2.5f, -1.0f,  t, 3.5f,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs7A",  0.0f,  4.5f,  7.0f, t,  obsColor, obsSprite, obsPrefab);
                break;

            // ── Niveau 3 : grille serrée ──────────────────────────────────────
            case 3:
                MakeObs("Obs1A", -1.5f, -6.5f,  7.5f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs2A",  1.5f, -4.5f,  7.5f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs3A", -1.5f, -2.0f,  7.5f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs4A",  1.5f,  0.5f,  7.5f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs5A", -1.5f,  3.0f,  7.5f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs6A",  1.5f,  5.5f,  7.5f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs7A", -3.5f, -5.5f,  t, 2.5f,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs8A",  3.5f, -3.3f,  t, 2.5f,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs9A", -3.5f,  1.8f,  t, 2.5f,  obsColor, obsSprite, obsPrefab);
                break;

            // ── Niveau 4 : double chicane ─────────────────────────────────────
            case 4:
                MakeObs("Obs1A", -0.5f, -6.0f,  9.0f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs2A",  0.5f, -3.5f,  9.0f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs3A", -0.5f, -1.0f,  9.0f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs4A",  0.5f,  1.5f,  9.0f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs5A", -0.5f,  4.0f,  9.0f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs6A",  4.5f, -4.8f,  t, 3.5f,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs7A", -4.5f,  0.5f,  t, 3.5f,  obsColor, obsSprite, obsPrefab);
                break;

            // ── Niveau 5 : labyrinthe croisé ──────────────────────────────────
            case 5:
                MakeObs("Obs1A",  0.0f, -7.0f,  9.5f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs2A",  4.5f, -3.5f,  t, 6.5f,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs3A",  0.0f,  0.0f,  9.5f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs4A", -4.5f,  3.5f,  t, 6.5f,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs5A",  0.0f,  4.5f,  9.5f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs6A",  2.5f, -5.0f,  5.0f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs7A",  2.5f, -2.0f,  t, 4.5f,  obsColor, obsSprite, obsPrefab);
                break;

            // ── Niveau 6 : escalier ───────────────────────────────────────────
            case 6:
                MakeObs("Obs1A", -3.0f, -6.5f,  4.5f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs2A",  3.0f, -5.0f,  4.5f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs3A", -2.5f, -3.0f,  5.5f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs4A",  2.5f, -1.0f,  5.5f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs5A", -2.0f,  1.0f,  6.5f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs6A",  2.0f,  3.0f,  6.5f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs7A", -1.5f,  5.0f,  7.5f, t,  obsColor, obsSprite, obsPrefab);
                break;

            // ── Niveau 7 : zigzag serré ───────────────────────────────────────
            case 7:
                MakeObs("Obs1A", -1.5f, -7.0f,  8.5f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs2A",  1.5f, -5.5f,  8.5f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs3A", -1.5f, -3.5f,  8.5f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs4A",  1.5f, -1.5f,  8.5f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs5A", -1.5f,  0.5f,  8.5f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs6A",  1.5f,  2.5f,  8.5f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs7A", -1.5f,  4.5f,  8.5f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs8A",  4.5f, -6.3f,  t, 3.0f,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs9A", -4.5f, -2.5f,  t, 3.0f,  obsColor, obsSprite, obsPrefab);
                break;

            // ── Niveau 8 : cage centrale ──────────────────────────────────────
            case 8:
                // Cage
                MakeObs("Obs1A",  0.0f, -2.5f,  5.0f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs2A",  0.0f,  2.5f,  5.0f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs3A", -2.5f,  0.0f,  t, 5.0f,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs4A",  2.5f,  0.0f,  t, 5.0f,  obsColor, obsSprite, obsPrefab);
                // Chicanes ext.
                MakeObs("Obs5A",  0.0f, -6.0f,  9.0f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs6A",  4.5f, -4.3f,  t, 3.5f,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs7A",  0.0f,  5.0f,  9.0f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs8A", -4.5f,  3.0f,  t, 3.5f,  obsColor, obsSprite, obsPrefab);
                break;

            // ── Niveau 9 : labyrinthe maximal ─────────────────────────────────
            case 9:
                MakeObs("Obs1A", -1.5f, -7.0f,  8.5f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs2A",  1.5f, -5.5f,  8.5f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs3A", -1.5f, -3.5f,  8.5f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs4A",  1.5f, -1.5f,  8.5f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs5A", -1.5f,  0.5f,  8.5f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs6A",  1.5f,  2.5f,  8.5f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs7A", -1.5f,  4.5f,  8.5f, t,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs8A",  4.5f, -6.3f,  t, 3.5f,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs9A", -4.5f, -3.5f,  t, 3.5f,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs10A", 4.5f,  1.5f,  t, 3.5f,  obsColor, obsSprite, obsPrefab);
                break;
        }
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

        var hud = canvasGO.AddComponent<TBHud>();
        hud.Init(needKey, canvas.GetComponent<RectTransform>(), levelIndex);
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
