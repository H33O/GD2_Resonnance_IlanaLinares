using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Spawns enemies that charge from depth toward the player.
/// Enemies use the ENNEMIS.png sprite asset.
/// </summary>
public class PGEnemySpawner : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Settings")]
    public PGSettings settings;

    // ── State ─────────────────────────────────────────────────────────────────

    private readonly List<PGEnemy> activeEnemies = new();
    private Coroutine spawnRoutine;
    private Sprite    enemySprite;

    // ── Public ────────────────────────────────────────────────────────────────

    public IReadOnlyList<PGEnemy> ActiveEnemies => activeEnemies;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        enemySprite = LoadSprite("Assets/sprites/ENNEMIS.png");
    }

    private void OnEnable()
    {
        PGGameManager.OnGameOver    += StopSpawning;
        PGGameManager.OnGameStarted += StartSpawning;
    }

    private void OnDisable()
    {
        PGGameManager.OnGameOver    -= StopSpawning;
        PGGameManager.OnGameStarted -= StartSpawning;
    }

    private void Start()
    {
        StartSpawning();
    }

    private void Update()
    {
        // Prune destroyed enemies
        activeEnemies.RemoveAll(e => e == null);
    }

    // ── Spawn loop ────────────────────────────────────────────────────────────

    private void StartSpawning()
    {
        if (spawnRoutine != null) StopCoroutine(spawnRoutine);
        spawnRoutine = StartCoroutine(SpawnLoop());
    }

    private void StopSpawning()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    private IEnumerator SpawnLoop()
    {
        yield return new WaitForSeconds(1.5f);

        while (true)
        {
            var gm = PGGameManager.Instance;
            if (gm == null || gm.State != PGGameManager.GameState.Playing) yield break;

            SpawnEnemy();

            float delay = gm.CurrentSpawnDelay;
            yield return new WaitForSeconds(delay);
        }
    }

    // ── Enemy creation ────────────────────────────────────────────────────────

    private void SpawnEnemy()
    {
        var gm = PGGameManager.Instance;
        if (settings == null || gm == null) return;

        float xSpread = Random.Range(-1.8f, 2.4f);
        float yPos    = Random.Range(-0.3f, 0.5f);

        var go = CreateEnemyVisual();
        go.transform.position = new Vector3(xSpread, yPos, settings.enemySpawnZ);

        var enemy = go.AddComponent<PGEnemy>();
        enemy.Init(settings.enemySpawnZ, settings.playerZ, gm.CurrentEnemySpeed);

        activeEnemies.Add(enemy);
    }

    // ── Sprite loading ─────────────────────────────────────────────────────────

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

    // ── Enemy visual ─────────────────────────────────────────────────────────

    private GameObject CreateEnemyVisual()
    {
        var go = new GameObject("Enemy");

        if (enemySprite != null)
        {
            var sr  = go.AddComponent<SpriteRenderer>();
            sr.sprite       = enemySprite;
            sr.sortingOrder = 4;

            // Taille cible à pleine approche (z=playerZ) : ~0.9u de haut.
            // RefreshScale dans PGEnemy interpole de 0.05 à 1.0 sur ce scale de base.
            // Caméra FOV 60°, z=-7 → hauteur visible ≈ 8.08u.
            // Ennemi ≈ 11% de hauteur écran ≈ 0.9u.
            const float targetHeightU = 0.9f;
            float       ppu           = enemySprite.pixelsPerUnit > 0 ? enemySprite.pixelsPerUnit : 100f;
            float       spriteH       = enemySprite.rect.height / ppu;
            float       s             = spriteH > 0 ? targetHeightU / spriteH : 0.009f;
            go.transform.localScale   = new Vector3(s, s, s);
        }
        else
        {
            // Fallback: diamant rouge ~0.5u
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "EnemyQuad";
            Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(go.transform, false);
            quad.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            quad.transform.localScale    = new Vector3(0.5f, 0.5f, 0.5f);
            var mat   = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            mat.color = new Color(0.95f, 0.25f, 0.15f, 1f);
            mat.renderQueue = 3000;
            quad.GetComponent<Renderer>().material = mat;
        }

        return go;
    }
}
