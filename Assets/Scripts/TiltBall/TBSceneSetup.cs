using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Génère procéduralement un niveau TiltBall en fonction de l'index (0 à 11).
///
/// Règles :
///   - Joueur spawne toujours en BAS (y ≈ -8.0).
///   - Trou toujours en HAUT (y ≈ +8.0).
///   - Niveaux impairs : clé requise avant d'entrer dans le trou.
///   - Ennemis : 1 au niveau 0, +1 tous les 2 niveaux, max 6 initiaux.
///   - Spawner d'ennemis activé dès le niveau 3.
///   - Niveaux 0-3  : monde standard (OrthoSize 9.6, hauteur [-9.6, 9.6]).
///   - Niveaux 4-11 : double slide (monde ×2 vertical, caméra suit le joueur).
///   - Labyrinthes de plus en plus denses.
///
/// Monde portrait 9:16, caméra orthographique size 9.6 :
///   Largeur  = 10.8 u → x ∈ [-5.4, 5.4]
///   Hauteur std  = 19.2 u → y ∈ [-9.6,  9.6]
///   Hauteur ext  = 38.4 u → y ∈ [-19.2, 19.2]
/// </summary>
public class TBSceneSetup : MonoBehaviour
{
    // ── Constantes monde ──────────────────────────────────────────────────────

    public const float OrthoSize        = 9.6f;
    public const float HalfW            = 5.4f;
    public const float HalfH            = 9.6f;
    public const float HalfHExtended    = 19.2f;  // double slide
    public const float WallThickness    = 0.28f;

    // Spawn du joueur (bas) et position du trou (haut)
    private const float PlayerSpawnY    = -8.0f;
    private const float HoleY           = 8.0f;
    private const float HoleYExtended   = 17.5f;  // double slide

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Tooltip("ScriptableObject de sprites. Si null, des sprites procéduraux sont utilisés.")]
    [SerializeField] public TBLevelPrefabsData prefabsData;

    // ── Référence statique (accessible par RebuildLevel) ──────────────────────

    private static TBLevelPrefabsData s_prefabsData;

    // ── Palette fallback ──────────────────────────────────────────────────────

    private static readonly Color ColBg         = new Color(0.06f, 0.06f, 0.10f, 1f);
    private static readonly Color ColWall        = new Color(0.88f, 0.88f, 0.92f, 1f);
    private static readonly Color ColObstacle    = new Color(0.20f, 0.20f, 0.30f, 1f);
    private static readonly Color ColPlayer      = Color.white;
    private static readonly Color ColEnemy       = new Color(0.90f, 0.14f, 0.14f, 1f);
    private static readonly Color ColHole        = new Color(0.04f, 0.04f, 0.06f, 1f);
    private static readonly Color ColHoleLocked  = new Color(0.45f, 0.22f, 0.02f, 1f);
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

        // Retire TBCameraFollow si présent
        var cam = Camera.main;
        if (cam != null)
        {
            var follow = cam.GetComponent<TBCameraFollow>();
            if (follow != null) Destroy(follow);
        }

        DestroyByName("Player", "Background", "Hole", "Key",
                      "HUD", "WallTop", "WallBottom", "WallLeft", "WallRight",
                      "WallTopExt", "WallBottomExt", "EnemySpawner");

        for (int i = 1; i <= 12; i++)
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
        bool  requireKey    = (levelIndex % 2 == 1);
        int   enemyCount    = Mathf.Min(1 + levelIndex / 2, 6);
        float moveInterval  = Mathf.Lerp(0.45f, 0.18f, levelIndex / 11f);
        bool  isDoubleSlide = (levelIndex >= TBCameraFollow.ActivationLevel);

        TBGrid.MoveInterval = moveInterval;

        float halfH = isDoubleSlide ? HalfHExtended : HalfH;
        float holeY = isDoubleSlide ? HoleYExtended : HoleY;

        var d = s_prefabsData;

        SetupCamera(d != null ? d.backgroundColor : ColBg, isDoubleSlide);
        BuildBackground(d != null ? d.backgroundColor : ColBg, halfH);
        BuildBoundaries(d != null ? d.wallColor : ColWall, halfH);
        BuildObstaclesForLevel(levelIndex, d != null ? d.obstacleColor : ColObstacle,
                               d != null ? d.obstacleSprite : null,
                               d != null ? d.obstaclePrefab : null,
                               halfH);
        BuildHole(levelIndex, requireKey, d, holeY);
        BuildPlayer(d);
        BuildEnemies(levelIndex, enemyCount, d);
        if (requireKey) BuildKey(levelIndex, d, halfH);

        // Spawner à partir du niveau 3
        TBEnemySpawner.Create(levelIndex,
            d != null ? d.enemyColor : ColEnemy,
            d?.enemySprite);

        // Caméra suivante à partir du niveau 4
        TBCameraFollow.AttachIfNeeded(levelIndex, halfH);

        // Mise à jour TBGrid pour tenir compte de la hauteur étendue
        TBGrid.MaxY = halfH - WallThickness - 0.5f;

        // Améliorations achetées
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

        if (upgrades.HasWeapon)
            TBWeapon.Spawn();

        if (upgrades.BarrierCount > 0)
            TBBarrier.SpawnAll(upgrades.BarrierCount);
    }

    // ── Caméra ────────────────────────────────────────────────────────────────

    private static void SetupCamera(Color bgColor, bool isDoubleSlide)
    {
        var cam = Camera.main;
        if (cam == null) return;
        cam.orthographic     = true;
        cam.orthographicSize = OrthoSize;    // toujours 9.6, c'est le monde qui s'étend
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = bgColor;
        cam.transform.position = new Vector3(0f, isDoubleSlide ? PlayerSpawnY : 0f, -10f);
    }

    // ── Fond ──────────────────────────────────────────────────────────────────

    private static void BuildBackground(Color bgColor, float halfH)
    {
        var go = new GameObject("Background");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = SpriteGenerator.CreateWhiteSquare();
        sr.color        = bgColor;
        sr.sortingOrder = -10;
        go.transform.localScale = new Vector3(HalfW * 2f, halfH * 2f, 1f);
    }

    // ── Murs de bordure ───────────────────────────────────────────────────────

    private static void BuildBoundaries(Color wallColor, float halfH)
    {
        float t = WallThickness;
        MakeWall("WallTop",    0f,           halfH - t * 0.5f,  HalfW * 2f, t, wallColor);
        MakeWall("WallBottom", 0f,          -halfH + t * 0.5f,  HalfW * 2f, t, wallColor);
        MakeWall("WallLeft",  -HalfW + t * 0.5f, 0f,            t, halfH * 2f, wallColor);
        MakeWall("WallRight",  HalfW - t * 0.5f, 0f,            t, halfH * 2f, wallColor);
    }

    // ── Trou / Goal ───────────────────────────────────────────────────────────

    private static readonly float[] HoleXByLevel = {
        0.0f, -3.5f,  3.5f, -3.0f,  2.5f, -4.0f,
        3.5f, -2.0f,  1.0f, -3.5f,  4.0f,  0.0f,
    };

    private static void BuildHole(int levelIndex, bool requireKey, TBLevelPrefabsData d, float holeY)
    {
        float x   = HoleXByLevel[Mathf.Clamp(levelIndex, 0, 11)];
        var   pos = new Vector3(x, holeY, 0f);

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
            go = new GameObject("Hole");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = d?.holeSprite != null ? d.holeSprite : SpriteGenerator.CreateCircle(128);
            sr.color        = requireKey
                ? (d != null ? d.holeLockedColor : ColHoleLocked)
                : (d != null ? d.holeOpenColor   : ColHole);
            sr.sortingOrder = 1;
            go.transform.position   = pos;
            go.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
            var col       = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius    = 0.5f;
            go.AddComponent<TBHole>();
        }
    }

    // ── Joueur — spawn en bas ─────────────────────────────────────────────────

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

    // ── Ennemis initiaux ──────────────────────────────────────────────────────

    // Positions initiales : étalées sur le niveau, loin du joueur (bas) et du trou (haut)
    private static readonly Vector2[][] EnemyPositionsByLevel =
    {
        /* 0  */ new[] { new Vector2( 2.5f,  0.0f) },
        /* 1  */ new[] { new Vector2(-2.5f,  2.0f), new Vector2( 2.5f, -3.0f) },
        /* 2  */ new[] { new Vector2(-3.0f,  4.0f), new Vector2( 3.0f, -1.0f) },
        /* 3  */ new[] { new Vector2( 3.0f,  5.0f), new Vector2(-3.0f, -2.0f), new Vector2( 0.0f,  2.5f) },
        /* 4  */ new[] { new Vector2(-3.5f, 10.0f), new Vector2( 3.5f,  0.0f), new Vector2(-1.0f, -5.0f) },
        /* 5  */ new[] { new Vector2( 3.5f, 12.0f), new Vector2(-3.0f,  5.0f), new Vector2( 1.5f, -2.0f), new Vector2(-2.0f,-10.0f) },
        /* 6  */ new[] { new Vector2(-4.0f, 14.0f), new Vector2( 4.0f,  7.0f), new Vector2(-2.0f,  0.5f), new Vector2( 2.0f, -6.0f) },
        /* 7  */ new[] { new Vector2( 3.5f, 15.0f), new Vector2(-3.5f,  8.0f), new Vector2( 3.0f,  1.0f), new Vector2(-3.0f, -7.0f), new Vector2( 0.0f, -13.0f) },
        /* 8  */ new[] { new Vector2(-4.0f, 16.0f), new Vector2( 4.0f,  9.0f), new Vector2(-2.0f,  2.5f), new Vector2( 2.0f, -5.0f), new Vector2( 0.0f,-12.0f) },
        /* 9  */ new[] { new Vector2( 4.0f, 16.0f), new Vector2(-4.0f,  9.0f), new Vector2( 3.0f,  2.0f), new Vector2(-3.0f, -6.0f), new Vector2( 1.0f,-13.0f), new Vector2(-1.0f,-16.5f) },
        /* 10 */ new[] { new Vector2(-4.0f, 17.0f), new Vector2( 4.0f, 10.0f), new Vector2(-3.0f,  3.0f), new Vector2( 3.0f, -4.0f), new Vector2( 0.0f,-12.0f), new Vector2(-2.0f,-16.0f) },
        /* 11 */ new[] { new Vector2( 4.0f, 17.0f), new Vector2(-4.0f, 10.0f), new Vector2( 3.0f,  4.0f), new Vector2(-3.0f, -3.5f), new Vector2( 1.0f,-11.0f), new Vector2(-1.0f,-15.0f) },
    };

    private static void BuildEnemies(int levelIndex, int enemyCount, TBLevelPrefabsData d)
    {
        var positions = EnemyPositionsByLevel[Mathf.Clamp(levelIndex, 0, 11)];
        int count     = Mathf.Min(enemyCount, positions.Length);
        for (int i = 0; i < count; i++)
            SpawnEnemy($"Enemy{i + 1}", positions[i], d);
    }

    private static void SpawnEnemy(string name, Vector2 position, TBLevelPrefabsData d)
    {
        var go = new GameObject(name);
        go.tag = LevelContentTag;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = d?.enemySprite != null ? d.enemySprite : SpriteGenerator.CreateCircle(128);
        sr.color        = d != null ? d.enemyColor : ColEnemy;
        sr.sortingOrder = 2;
        go.transform.position   = position;
        go.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType     = RigidbodyType2D.Kinematic;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;
        var col       = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.5f;
        go.AddComponent<TBEnemyController>();
    }

    // ── Clé ───────────────────────────────────────────────────────────────────

    // Position de la clé : mi-chemin entre joueur et trou, dans un couloir accessible
    private static readonly Vector2[] KeyPositionsByLevel =
    {
        Vector2.zero,               // 0 — pas de clé
        new Vector2(-1.5f, -1.0f),  // 1
        Vector2.zero,               // 2 — pas de clé
        new Vector2( 2.0f,  2.5f),  // 3
        Vector2.zero,               // 4 — pas de clé
        new Vector2(-2.5f,  4.0f),  // 5
        Vector2.zero,               // 6 — pas de clé
        new Vector2( 2.5f,  7.0f),  // 7
        Vector2.zero,               // 8 — pas de clé
        new Vector2(-3.0f,  5.5f),  // 9
        Vector2.zero,               // 10 — pas de clé
        new Vector2( 0.0f,  8.0f),  // 11
    };

    private static void BuildKey(int levelIndex, TBLevelPrefabsData d, float halfH)
    {
        Vector2 pos = KeyPositionsByLevel[Mathf.Clamp(levelIndex, 0, 11)];
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

    // ── Labyrinthe par niveau ──────────────────────────────────────────────────
    //
    // Convention MakeObs(name, cx, cy, width, height)
    // Le joueur spawn à y = -8, le trou à y = +8 (ou +17.5 en double slide).
    // On crée des couloirs en S, chicanes et spirales pour forcer le joueur
    // à trouver un chemin entre les murs.

    private static void BuildObstaclesForLevel(int levelIndex, Color obsColor, Sprite obsSprite, GameObject obsPrefab, float halfH)
    {
        float t = WallThickness;

        switch (levelIndex)
        {
            // ── Niveau 0 : Couloir en S simple ───────────────────────────────
            case 0:
                MakeObs("Obs1A", -0.8f, -5.5f,  7.5f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs2A",  0.8f, -2.5f,  7.5f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs3A", -0.8f,  0.5f,  7.5f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs4A",  0.8f,  3.5f,  7.5f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs5A", -0.8f,  6.5f,  7.5f, t,   obsColor, obsSprite, obsPrefab);
                break;

            // ── Niveau 1 : Chicane + piliers ─────────────────────────────────
            case 1:
                MakeObs("Obs1A", -1.2f, -6.0f,  8.0f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs2A",  1.2f, -3.0f,  8.0f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs3A", -1.2f,  0.0f,  8.0f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs4A",  1.2f,  3.0f,  8.0f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs5A", -1.2f,  6.0f,  8.0f, t,   obsColor, obsSprite, obsPrefab);
                // Piliers
                MakeObs("Obs6A",  4.0f, -4.5f,  t, 2.0f,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs7A", -4.0f,  1.5f,  t, 2.0f,   obsColor, obsSprite, obsPrefab);
                break;

            // ── Niveau 2 : Labyrinthe en spirale ─────────────────────────────
            case 2:
                MakeObs("Obs1A",  0.0f, -6.5f,  9.5f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs2A",  4.5f,  0.5f,  t, 6.0f,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs3A",  0.0f,  3.0f,  7.5f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs4A", -4.0f, -2.0f,  t, 6.5f,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs5A",  1.5f, -3.5f,  5.5f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs6A",  2.5f,  0.0f,  t, 4.5f,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs7A",  0.0f,  6.5f,  7.0f, t,   obsColor, obsSprite, obsPrefab);
                break;

            // ── Niveau 3 : Grille serrée ──────────────────────────────────────
            case 3:
                // Horizontaux
                MakeObs("Obs1A", -1.5f, -6.0f,  7.0f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs2A",  1.5f, -3.5f,  7.0f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs3A", -1.5f, -1.0f,  7.0f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs4A",  1.5f,  1.5f,  7.0f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs5A", -1.5f,  4.0f,  7.0f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs6A",  1.5f,  6.5f,  7.0f, t,   obsColor, obsSprite, obsPrefab);
                // Verticaux coupés
                MakeObs("Obs7A", -3.5f, -4.8f,  t, 3.0f,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs8A",  3.5f, -2.3f,  t, 3.0f,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs9A", -3.5f,  2.8f,  t, 3.0f,   obsColor, obsSprite, obsPrefab);
                break;

            // ── Niveau 4 : Double slide — grand labyrinthe en S ───────────────
            case 4:
                // Section basse (joueur spawn ici)
                MakeObs("Obs1A", -1.0f,-13.0f,  8.5f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs2A",  1.0f,-10.0f,  8.5f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs3A", -1.0f, -7.0f,  8.5f, t,   obsColor, obsSprite, obsPrefab);
                // Section médiane
                MakeObs("Obs4A",  1.0f, -4.0f,  8.5f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs5A", -1.0f, -1.0f,  8.5f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs6A",  1.0f,  2.0f,  8.5f, t,   obsColor, obsSprite, obsPrefab);
                // Section haute (trou ici)
                MakeObs("Obs7A", -1.0f,  5.0f,  8.5f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs8A",  1.0f,  8.0f,  8.5f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs9A", -1.0f, 11.0f,  8.5f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs10A", 1.0f, 14.0f,  8.5f, t,   obsColor, obsSprite, obsPrefab);
                // Piliers latéraux
                MakeObs("Obs11A", 4.5f, -5.5f,  t, 4.0f,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs12A",-4.5f,  1.5f,  t, 4.0f,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs13A", 4.5f,  8.5f,  t, 4.0f,   obsColor, obsSprite, obsPrefab);
                break;

            // ── Niveau 5 : Spirale géante ─────────────────────────────────────
            case 5:
                // Cadre extérieur spirale
                MakeObs("Obs1A",  0.0f,-14.0f, 10.0f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs2A",  4.5f, -7.0f,  t, 14.0f,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs3A",  0.0f,  0.0f, 10.0f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs4A", -4.5f,  7.0f,  t, 14.0f,  obsColor, obsSprite, obsPrefab);
                MakeObs("Obs5A",  0.0f, 14.0f, 10.0f, t,   obsColor, obsSprite, obsPrefab);
                // Spirale intérieure
                MakeObs("Obs6A",  2.5f,-10.5f,  5.5f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs7A",  2.5f, -5.5f,  t, 8.0f,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs8A", -0.5f, -1.8f,  5.5f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs9A", -2.5f,  5.5f,  t, 6.5f,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs10A", 0.0f, 10.5f,  6.0f, t,   obsColor, obsSprite, obsPrefab);
                break;

            // ── Niveau 6 : Chicane très serrée ────────────────────────────────
            case 6:
                for (int i = 0; i < 12; i++)
                {
                    float cy   = -15.0f + i * 2.8f;
                    float side = (i % 2 == 0) ? -0.8f : 0.8f;
                    MakeObs($"Obs{i+1}A", side, cy, 8.8f, t, obsColor, obsSprite, obsPrefab);
                }
                // Piliers alternés
                MakeObs("ObsP1",  4.2f, -9.0f, t, 3.5f,   obsColor, obsSprite, obsPrefab);
                MakeObs("ObsP2", -4.2f, -3.0f, t, 3.5f,   obsColor, obsSprite, obsPrefab);
                MakeObs("ObsP3",  4.2f,  3.0f, t, 3.5f,   obsColor, obsSprite, obsPrefab);
                MakeObs("ObsP4", -4.2f,  9.0f, t, 3.5f,   obsColor, obsSprite, obsPrefab);
                break;

            // ── Niveau 7 : Croisillons + pièges ─────────────────────────────
            case 7:
                // Croisillons horizontaux
                MakeObs("Obs1A",  0.0f,-14.5f,  9.0f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs2A",  0.0f,-10.5f,  9.0f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs3A",  0.0f, -6.5f,  9.0f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs4A",  0.0f, -2.5f,  9.0f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs5A",  0.0f,  1.5f,  9.0f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs6A",  0.0f,  5.5f,  9.0f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs7A",  0.0f,  9.5f,  9.0f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("Obs8A",  0.0f, 13.5f,  9.0f, t,   obsColor, obsSprite, obsPrefab);
                // Verticaux décalés alternés
                MakeObs("ObsV1",  3.5f,-12.5f,  t, 3.5f,   obsColor, obsSprite, obsPrefab);
                MakeObs("ObsV2", -3.5f, -8.5f,  t, 3.5f,   obsColor, obsSprite, obsPrefab);
                MakeObs("ObsV3",  3.5f, -4.5f,  t, 3.5f,   obsColor, obsSprite, obsPrefab);
                MakeObs("ObsV4", -3.5f, -0.5f,  t, 3.5f,   obsColor, obsSprite, obsPrefab);
                MakeObs("ObsV5",  3.5f,  3.5f,  t, 3.5f,   obsColor, obsSprite, obsPrefab);
                MakeObs("ObsV6", -3.5f,  7.5f,  t, 3.5f,   obsColor, obsSprite, obsPrefab);
                MakeObs("ObsV7",  3.5f, 11.5f,  t, 3.5f,   obsColor, obsSprite, obsPrefab);
                break;

            // ── Niveau 8 : Labyrinthe en grille dense ─────────────────────────
            case 8:
                // Horizontaux tous les 2.5 u
                for (int i = 0; i < 15; i++)
                {
                    float cy   = -17.0f + i * 2.5f;
                    float side = (i % 2 == 0) ? -1.0f : 1.0f;
                    MakeObs($"Obs{i+1}A", side, cy, 8.0f, t, obsColor, obsSprite, obsPrefab);
                }
                // Colonnes latérales partielles
                MakeObs("ObsC1",  4.0f,-10.0f, t, 5.0f,   obsColor, obsSprite, obsPrefab);
                MakeObs("ObsC2", -4.0f, -2.5f, t, 5.0f,   obsColor, obsSprite, obsPrefab);
                MakeObs("ObsC3",  4.0f,  5.0f, t, 5.0f,   obsColor, obsSprite, obsPrefab);
                MakeObs("ObsC4", -4.0f, 12.5f, t, 5.0f,   obsColor, obsSprite, obsPrefab);
                break;

            // ── Niveau 9 : Spirale + grille fusionnées ─────────────────────────
            case 9:
                // Couche spirale externe
                MakeObs("ObsS1",  0.0f,-16.0f, 10.0f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("ObsS2",  4.5f, -8.0f,  t, 16.0f,  obsColor, obsSprite, obsPrefab);
                MakeObs("ObsS3",  0.0f,  0.0f, 10.0f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("ObsS4", -4.5f,  8.0f,  t, 16.0f,  obsColor, obsSprite, obsPrefab);
                MakeObs("ObsS5",  0.0f, 16.0f, 10.0f, t,   obsColor, obsSprite, obsPrefab);
                // Intérieur chicane serrée
                MakeObs("ObsI1",  2.0f,-12.0f,  5.0f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("ObsI2",  2.0f, -8.0f,  t, 5.0f,   obsColor, obsSprite, obsPrefab);
                MakeObs("ObsI3", -1.0f, -4.5f,  5.0f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("ObsI4", -1.0f,  4.0f,  t, 5.0f,   obsColor, obsSprite, obsPrefab);
                MakeObs("ObsI5",  2.0f,  8.0f,  5.0f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("ObsI6",  2.0f, 12.0f,  t, 5.0f,   obsColor, obsSprite, obsPrefab);
                break;

            // ── Niveau 10 : Labyrinthe extrême — chicane dense + colonnes ──────
            case 10:
                for (int i = 0; i < 18; i++)
                {
                    float cy   = -17.5f + i * 2.1f;
                    float side = (i % 2 == 0) ? -1.2f : 1.2f;
                    MakeObs($"Obs{i+1}A", side, cy, 8.4f, t, obsColor, obsSprite, obsPrefab);
                }
                // Colonnes bloquantes intérieures
                MakeObs("ObsC1",  3.0f,-11.0f, t, 4.0f,   obsColor, obsSprite, obsPrefab);
                MakeObs("ObsC2", -3.0f, -4.0f, t, 4.0f,   obsColor, obsSprite, obsPrefab);
                MakeObs("ObsC3",  3.0f,  3.0f, t, 4.0f,   obsColor, obsSprite, obsPrefab);
                MakeObs("ObsC4", -3.0f, 10.0f, t, 4.0f,   obsColor, obsSprite, obsPrefab);
                MakeObs("ObsC5",  3.0f, 17.0f, t, 2.5f,   obsColor, obsSprite, obsPrefab);
                break;

            // ── Niveau 11 : Boss — labyrinthe ultime ──────────────────────────
            case 11:
                // Couche externe
                MakeObs("ObsE1",  0.0f,-17.5f, 10.0f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("ObsE2",  4.5f, -8.5f,  t, 18.0f,  obsColor, obsSprite, obsPrefab);
                MakeObs("ObsE3",  0.0f,  0.5f, 10.0f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("ObsE4", -4.5f,  9.5f,  t, 18.0f,  obsColor, obsSprite, obsPrefab);
                MakeObs("ObsE5",  0.0f, 17.5f, 10.0f, t,   obsColor, obsSprite, obsPrefab);
                // Couche intermédiaire
                MakeObs("ObsM1",  2.0f,-13.0f,  6.5f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("ObsM2",  2.0f, -9.0f,  t, 6.5f,   obsColor, obsSprite, obsPrefab);
                MakeObs("ObsM3", -1.0f, -5.0f,  6.5f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("ObsM4", -1.0f,  5.0f,  t, 6.5f,   obsColor, obsSprite, obsPrefab);
                MakeObs("ObsM5",  2.0f,  9.0f,  6.5f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("ObsM6",  2.0f, 13.0f,  t, 6.5f,   obsColor, obsSprite, obsPrefab);
                // Chicane intérieure serrée
                MakeObs("ObsI1", -0.5f,-11.0f,  4.5f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("ObsI2",  0.5f, -7.0f,  4.5f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("ObsI3", -0.5f, -3.0f,  4.5f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("ObsI4",  0.5f,  3.0f,  4.5f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("ObsI5", -0.5f,  7.0f,  4.5f, t,   obsColor, obsSprite, obsPrefab);
                MakeObs("ObsI6",  0.5f, 11.0f,  4.5f, t,   obsColor, obsSprite, obsPrefab);
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
{
    // ── Constantes monde ──────────────────────────────────────────────────────

    public const float OrthoSize     = 9.6f;
    public const float HalfW         = 5.4f;
    public const float HalfH         = 9.6f;
    public const float WallThickness = 0.28f;

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Tooltip("ScriptableObject de sprites. Si null, des sprites procéduraux sont utilisés.")]
    [SerializeField] public TBLevelPrefabsData prefabsData;

    // ── Référence statique (accessible par RebuildLevel) ──────────────────────

    private static TBLevelPrefabsData s_prefabsData;

    // ── Palette fallback (utilisée si prefabsData == null) ────────────────────

    private static readonly Color ColBg         = new Color(0.06f, 0.06f, 0.10f, 1f);
    private static readonly Color ColWall        = new Color(0.88f, 0.88f, 0.92f, 1f);
    private static readonly Color ColObstacle    = new Color(0.20f, 0.20f, 0.30f, 1f);
    private static readonly Color ColPlayer      = Color.white;
    private static readonly Color ColEnemy       = new Color(0.90f, 0.14f, 0.14f, 1f);
    private static readonly Color ColHole        = new Color(0.04f, 0.04f, 0.06f, 1f);
    private static readonly Color ColHoleLocked  = new Color(0.45f, 0.22f, 0.02f, 1f);
    private static readonly Color ColKey         = new Color(1.00f, 0.85f, 0.00f, 1f);

    // ── Tag du contenu de niveau (pour tout détruire entre deux niveaux) ──────

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

    /// <summary>
    /// Détruit l'ancien contenu et reconstruit le niveau dans la même scène.
    /// </summary>
    public static void RebuildLevel(int levelIndex)
    {
        foreach (var go in GameObject.FindGameObjectsWithTag(LevelContentTag))
            Destroy(go);

        DestroyByName("Player", "Background", "Hole", "Key",
                      "HUD", "WallTop", "WallBottom", "WallLeft", "WallRight");

        for (int i = 1; i <= 9; i++)
            DestroyByName($"Enemy{i}", $"Obs{i}A", $"Obs{i}B");

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
        bool requireKey    = (levelIndex % 2 == 1);
        int  enemyCount    = Mathf.Min(1 + levelIndex / 2, 4);
        float moveInterval = Mathf.Lerp(0.50f, 0.25f, levelIndex / 7f);
        TBGrid.MoveInterval = moveInterval;

        // Récupère les couleurs depuis prefabsData ou le fallback statique
        var d = s_prefabsData;

        SetupCamera(d != null ? d.backgroundColor : ColBg);
        BuildBackground(d != null ? d.backgroundColor : ColBg);
        BuildBoundaries(d != null ? d.wallColor : ColWall);
        BuildObstaclesForLevel(levelIndex, d != null ? d.obstacleColor : ColObstacle,
                               d != null ? d.obstacleSprite : null,
                               d != null ? d.obstaclePrefab : null);
        BuildHole(levelIndex, requireKey, d);
        BuildPlayer(d);
        BuildEnemies(levelIndex, enemyCount, d);
        if (requireKey) BuildKey(levelIndex, d);

        // Améliorations achetées — appliquées dès le niveau suivant l'achat
        SpawnUpgrades();
    }

    // ── Spawn des améliorations ───────────────────────────────────────────────

    private static void SpawnUpgrades()
    {
        var upgrades = TBGameManager.Instance?.Upgrades;
        if (upgrades == null) return;

        // Alliés
        var allyPositions = new Vector2[]
        {
            new Vector2(-1.5f, 7.0f),
            new Vector2( 1.5f, 7.0f),
            new Vector2( 0.0f, 6.2f),
        };
        for (int i = 0; i < upgrades.AllyCount; i++)
            TBAlly.Spawn(allyPositions[i], new Color(0.20f, 0.80f, 1.00f, 1f));

        // Arme
        if (upgrades.HasWeapon)
            TBWeapon.Spawn();

        // Barrières
        if (upgrades.BarrierCount > 0)
            TBBarrier.SpawnAll(upgrades.BarrierCount);
    }

    // ── Caméra ────────────────────────────────────────────────────────────────

    private static void SetupCamera(Color bgColor)
    {
        var cam = Camera.main;
        if (cam == null) return;
        cam.orthographic     = true;
        cam.orthographicSize = OrthoSize;
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = bgColor;
        cam.transform.position = new Vector3(0f, 0f, -10f);
    }

    // ── Fond ──────────────────────────────────────────────────────────────────

    private static void BuildBackground(Color bgColor)
    {
        var go = new GameObject("Background");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = SpriteGenerator.CreateWhiteSquare();
        sr.color        = bgColor;
        sr.sortingOrder = -10;
        go.transform.localScale = new Vector3(HalfW * 2f, HalfH * 2f, 1f);
    }

    // ── Murs de bordure ───────────────────────────────────────────────────────

    private static void BuildBoundaries(Color wallColor)
    {
        float t = WallThickness;
        MakeWall("WallTop",    0f,                HalfH - t * 0.5f,  HalfW * 2f, t, wallColor);
        MakeWall("WallBottom", 0f,               -HalfH + t * 0.5f,  HalfW * 2f, t, wallColor);
        MakeWall("WallLeft",  -HalfW + t * 0.5f, 0f,                 t, HalfH * 2f, wallColor);
        MakeWall("WallRight",  HalfW - t * 0.5f, 0f,                 t, HalfH * 2f, wallColor);
    }

    // ── Trou / Goal ───────────────────────────────────────────────────────────

    /// <summary>Position du goal pour chaque niveau — partie du level design procédural.</summary>
    private static readonly Vector2[] HolePositionsByLevel =
    {
        new Vector2( 0.0f, -7.5f),   // 0 : bas centre
        new Vector2( 3.5f, -6.0f),   // 1 : bas droite
        new Vector2(-3.5f,  1.5f),   // 2 : milieu gauche
        new Vector2( 3.5f,  4.5f),   // 3 : haut droite
        new Vector2(-2.5f, -7.0f),   // 4 : bas gauche
        new Vector2( 2.5f,  6.5f),   // 5 : haut droite
        new Vector2( 0.0f,  7.0f),   // 6 : haut centre
        new Vector2(-3.5f, -5.0f),   // 7 : bas gauche
    };

    private static void BuildHole(int levelIndex, bool requireKey, TBLevelPrefabsData d)
    {
        Vector2 pos = HolePositionsByLevel[Mathf.Clamp(levelIndex, 0, 7)];

        GameObject go;

        if (d?.holePrefab != null)
        {
            // Instancie le prefab — le SpriteRenderer, le Collider et TBHole doivent y être
            go = Object.Instantiate(d.holePrefab, new Vector3(pos.x, pos.y, 0f), Quaternion.identity);
            go.name = "Hole";

            // Applique les couleurs sur le SpriteRenderer si présent
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = requireKey
                    ? d.holeLockedColor
                    : d.holeOpenColor;

            // S'assure que le TBHole est bien présent
            if (go.GetComponent<TBHole>() == null)
                go.AddComponent<TBHole>();
        }
        else
        {
            go = new GameObject("Hole");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = d?.holeSprite != null ? d.holeSprite : SpriteGenerator.CreateCircle(128);
            sr.color        = requireKey
                ? (d != null ? d.holeLockedColor : ColHoleLocked)
                : (d != null ? d.holeOpenColor   : ColHole);
            sr.sortingOrder = 1;

            go.transform.position   = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

            var col       = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius    = 0.5f;

            go.AddComponent<TBHole>();
        }
    }

    // ── Joueur ────────────────────────────────────────────────────────────────

    private static void BuildPlayer(TBLevelPrefabsData d)
    {
        GameObject go;

        if (d?.playerPrefab != null)
        {
            go      = Object.Instantiate(d.playerPrefab, new Vector3(0f, 7.8f, 0f), Quaternion.identity);
            go.name = "Player";
            go.tag  = "Player";

            // Applique la couleur sur le SpriteRenderer si présent
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = d.playerColor;

            // S'assure que TBPlayerController est bien présent
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

            go.transform.position   = new Vector3(0f, 7.8f, 0f);
            go.transform.localScale = new Vector3(0.85f, 0.85f, 1f);

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;

            var col    = go.AddComponent<CircleCollider2D>();
            col.radius = 0.42f;

            go.AddComponent<TBPlayerController>();
        }
    }

    // ── Ennemis ───────────────────────────────────────────────────────────────

    private static readonly Vector2[][] EnemyPositionsByLevel =
    {
        new[] { new Vector2( 2.5f,  0.0f) },
        new[] { new Vector2(-2.5f,  1.5f) },
        new[] { new Vector2(-3.0f,  4.0f), new Vector2( 3.0f, -1.0f) },
        new[] { new Vector2( 3.0f,  5.0f), new Vector2(-3.0f, -2.0f) },
        new[] { new Vector2(-3.0f,  5.5f), new Vector2( 3.0f,  0.0f), new Vector2(-1.0f, -5.0f) },
        new[] { new Vector2( 3.0f,  6.0f), new Vector2(-3.0f,  0.5f), new Vector2( 1.0f, -5.5f) },
        new[] { new Vector2(-4.0f,  5.5f), new Vector2( 4.0f,  2.5f), new Vector2(-2.0f, -1.5f), new Vector2( 2.0f, -5.0f) },
        new[] { new Vector2( 3.5f,  7.0f), new Vector2(-3.5f,  3.5f), new Vector2( 3.5f,  0.0f), new Vector2(-3.5f, -4.5f) },
    };

    private static void BuildEnemies(int levelIndex, int enemyCount, TBLevelPrefabsData d)
    {
        var positions = EnemyPositionsByLevel[Mathf.Clamp(levelIndex, 0, 7)];
        int count     = Mathf.Min(enemyCount, positions.Length);
        for (int i = 0; i < count; i++)
            SpawnEnemy($"Enemy{i + 1}", positions[i], d);
    }

    private static void SpawnEnemy(string name, Vector2 position, TBLevelPrefabsData d)
    {
        var go = new GameObject(name);
        go.tag = LevelContentTag;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = d?.enemySprite != null ? d.enemySprite : SpriteGenerator.CreateCircle(128);
        sr.color        = d != null ? d.enemyColor : ColEnemy;
        sr.sortingOrder = 2;

        go.transform.position   = position;
        go.transform.localScale = new Vector3(0.75f, 0.75f, 1f);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType     = RigidbodyType2D.Kinematic;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;

        var col       = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.5f;

        go.AddComponent<TBEnemyController>();
    }

    // ── Clé ───────────────────────────────────────────────────────────────────

    private static readonly Vector2[] KeyPositionsByLevel =
    {
        Vector2.zero,              // 0 — pas de clé
        new Vector2(-2.0f,  1.5f),
        Vector2.zero,              // 2 — pas de clé
        new Vector2( 2.5f,  4.5f),
        Vector2.zero,              // 4 — pas de clé
        new Vector2(-3.5f, -1.5f),
        Vector2.zero,              // 6 — pas de clé
        new Vector2( 0.0f,  3.0f),
    };

    private static void BuildKey(int levelIndex, TBLevelPrefabsData d)
    {
        Vector2 pos = KeyPositionsByLevel[Mathf.Clamp(levelIndex, 0, 7)];

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

    // ── Obstacles par niveau ──────────────────────────────────────────────────

    private static void BuildObstaclesForLevel(int levelIndex, Color obsColor, Sprite obsSprite, GameObject obsPrefab)
    {
        float t = WallThickness;

        switch (levelIndex)
        {
            case 0:
                MakeObs("Obs1A", -1.6f,  5.5f, 6.4f, t, obsColor, obsSprite, obsPrefab);
                MakeObs("Obs2A",  1.6f,  2.2f, 6.4f, t, obsColor, obsSprite, obsPrefab);
                MakeObs("Obs3A", -1.6f, -1.1f, 6.4f, t, obsColor, obsSprite, obsPrefab);
                MakeObs("Obs4A",  1.6f, -4.4f, 6.4f, t, obsColor, obsSprite, obsPrefab);
                break;

            case 1:
                MakeObs("Obs1A", -2.0f,  6.0f, 7.0f, t, obsColor, obsSprite, obsPrefab);
                MakeObs("Obs2A",  2.0f,  2.8f, 7.0f, t, obsColor, obsSprite, obsPrefab);
                MakeObs("Obs3A", -2.0f, -0.5f, 7.0f, t, obsColor, obsSprite, obsPrefab);
                MakeObs("Obs4A",  2.0f, -3.8f, 7.0f, t, obsColor, obsSprite, obsPrefab);
                MakeObs("Obs5A",  0.0f, -6.5f, 5.0f, t, obsColor, obsSprite, obsPrefab);
                break;

            case 2:
                MakeObs("Obs1A",  0.0f,  3.0f, t, 4.0f, obsColor, obsSprite, obsPrefab);
                MakeObs("Obs2A", -2.5f,  6.5f, 4.0f, t, obsColor, obsSprite, obsPrefab);
                MakeObs("Obs3A",  2.5f,  1.0f, 4.0f, t, obsColor, obsSprite, obsPrefab);
                MakeObs("Obs4A", -2.5f, -2.5f, 4.0f, t, obsColor, obsSprite, obsPrefab);
                MakeObs("Obs5A",  2.5f, -5.5f, 4.0f, t, obsColor, obsSprite, obsPrefab);
                break;

            case 3:
                MakeObs("Obs1A", -1.0f,  7.0f, 7.0f, t, obsColor, obsSprite, obsPrefab);
                MakeObs("Obs2A", -4.5f,  4.5f, t, 5.0f, obsColor, obsSprite, obsPrefab);
                MakeObs("Obs3A",  1.5f,  2.0f, 7.5f, t, obsColor, obsSprite, obsPrefab);
                MakeObs("Obs4A",  4.5f, -0.5f, t, 5.0f, obsColor, obsSprite, obsPrefab);
                MakeObs("Obs5A", -1.0f, -4.0f, 7.0f, t, obsColor, obsSprite, obsPrefab);
                break;

            case 4:
                MakeObs("Obs1A", -2.0f,  4.0f, t, 5.0f, obsColor, obsSprite, obsPrefab);
                MakeObs("Obs2A",  2.0f,  0.0f, t, 5.0f, obsColor, obsSprite, obsPrefab);
                MakeObs("Obs3A",  1.0f,  6.5f, 6.0f, t, obsColor, obsSprite, obsPrefab);
                MakeObs("Obs4A", -1.0f,  1.5f, 6.0f, t, obsColor, obsSprite, obsPrefab);
                MakeObs("Obs5A",  1.0f, -3.0f, 6.0f, t, obsColor, obsSprite, obsPrefab);
                MakeObs("Obs6A",  0.0f, -6.5f, 4.0f, t, obsColor, obsSprite, obsPrefab);
                break;

            case 5:
                MakeObs("Obs1A",  0.0f,  7.5f, 8.0f, t, obsColor, obsSprite, obsPrefab);
                MakeObs("Obs2A",  4.0f,  4.0f, t, 7.0f, obsColor, obsSprite, obsPrefab);
                MakeObs("Obs3A", -1.0f,  0.5f, 9.0f, t, obsColor, obsSprite, obsPrefab);
                MakeObs("Obs4A", -4.0f, -3.0f, t, 7.0f, obsColor, obsSprite, obsPrefab);
                MakeObs("Obs5A",  1.0f, -6.5f, 8.0f, t, obsColor, obsSprite, obsPrefab);
                break;

            case 6:
                MakeObs("Obs1A", -3.5f,  6.5f, 1.8f, 1.8f, obsColor, obsSprite, obsPrefab);
                MakeObs("Obs2A",  0.0f,  6.5f, 1.8f, 1.8f, obsColor, obsSprite, obsPrefab);
                MakeObs("Obs3A",  3.5f,  6.5f, 1.8f, 1.8f, obsColor, obsSprite, obsPrefab);
                MakeObs("Obs4A", -1.75f, 3.5f, 1.8f, 1.8f, obsColor, obsSprite, obsPrefab);
                MakeObs("Obs5A",  1.75f, 3.5f, 1.8f, 1.8f, obsColor, obsSprite, obsPrefab);
                MakeObs("Obs6A", -3.5f,  0.5f, 1.8f, 1.8f, obsColor, obsSprite, obsPrefab);
                MakeObs("Obs7A",  0.0f,  0.5f, 1.8f, 1.8f, obsColor, obsSprite, obsPrefab);
                MakeObs("Obs8A",  3.5f,  0.5f, 1.8f, 1.8f, obsColor, obsSprite, obsPrefab);
                MakeObs("Obs9A", -1.75f, -2.5f, 1.8f, 1.8f, obsColor, obsSprite, obsPrefab);
                break;

            case 7:
                MakeObs("Obs1A",  3.5f,  7.8f, 3.5f, t, obsColor, obsSprite, obsPrefab);
                MakeObs("Obs2A", -4.5f,  7.8f, t, 3.5f, obsColor, obsSprite, obsPrefab);
                MakeObs("Obs3A", -1.0f,  5.5f, 7.5f, t, obsColor, obsSprite, obsPrefab);
                MakeObs("Obs4A",  4.5f,  3.2f, t, 4.5f, obsColor, obsSprite, obsPrefab);
                MakeObs("Obs5A",  1.0f,  1.0f, 7.5f, t, obsColor, obsSprite, obsPrefab);
                MakeObs("Obs6A", -4.5f, -1.3f, t, 4.5f, obsColor, obsSprite, obsPrefab);
                MakeObs("Obs7A", -1.0f, -3.5f, 7.5f, t, obsColor, obsSprite, obsPrefab);
                MakeObs("Obs8A",  4.5f, -5.8f, t, 4.5f, obsColor, obsSprite, obsPrefab);
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

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = color;

            // Ajuste l'échelle pour correspondre aux dimensions demandées
            go.transform.localScale = new Vector3(w, h, 1f);

            // S'assure qu'un BoxCollider2D (non-trigger) est présent
            if (go.GetComponent<BoxCollider2D>() == null)
                go.AddComponent<BoxCollider2D>();
        }
        else
        {
            go     = new GameObject(name);
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
