using System.Collections;
using UnityEngine;

/// <summary>
/// Spawner d'ennemis activé à partir du niveau <see cref="ActivationLevel"/>.
/// Fait apparaître des ennemis sur les bords du niveau à intervalle croissant,
/// jusqu'à une limite max qui augmente avec le niveau.
/// Le compte d'ennemis vivants est borné à <see cref="MaxEnemiesOnScreen"/>.
/// </summary>
public class TBEnemySpawner : MonoBehaviour
{
    // ── Configuration ─────────────────────────────────────────────────────────

    public const int ActivationLevel = 3;   // Premier niveau avec spawner actif

    // Intervalle (secondes) entre chaque spawn, diminue avec les niveaux
    private const float SpawnIntervalBase = 2.5f;   // était 5.0 — spawn nettement plus rapide
    private const float SpawnIntervalMin  = 0.7f;   // était 1.8 — pression max très élevée

    // Nombre max d'ennemis sur l'écran à la fois (augmente avec le niveau)
    private const int MaxEnemiesBase = 4;    // était 3
    private const int MaxEnemiesMax  = 14;   // était 10

    // Délai initial avant le premier spawn (réduit pour entrer dans l'action plus vite)
    private const float InitialDelay = 1.2f;  // était 2.0

    // ── État ──────────────────────────────────────────────────────────────────

    private int   levelIndex;
    private float spawnInterval;
    private int   maxEnemies;
    private Color enemyColor;
    private Sprite enemySprite;

    // Positions de spawn sur les quatre bords (hors obstacles)
    private static readonly Vector2[] SpawnEdgePositions =
    {
        new Vector2(-4.0f,  8.5f),  // bord haut gauche
        new Vector2( 0.0f,  8.5f),  // bord haut centre
        new Vector2( 4.0f,  8.5f),  // bord haut droite
        new Vector2(-4.0f, -8.5f),  // bord bas gauche
        new Vector2( 0.0f, -8.5f),  // bord bas centre
        new Vector2( 4.0f, -8.5f),  // bord bas droite
        new Vector2(-5.0f,  4.0f),  // bord gauche haut
        new Vector2(-5.0f, -4.0f),  // bord gauche bas
        new Vector2( 5.0f,  4.0f),  // bord droit haut
        new Vector2( 5.0f, -4.0f),  // bord droit bas
    };

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>Crée le spawner pour un niveau donné (ne fait rien si niveau &lt; ActivationLevel).</summary>
    public static void Create(int levelIndex, Color enemyColor, Sprite enemySprite = null)
    {
        if (levelIndex < ActivationLevel) return;

        var go      = new GameObject("EnemySpawner");
        go.tag      = TBSceneSetup.LevelContentTag;
        var spawner = go.AddComponent<TBEnemySpawner>();

        spawner.levelIndex   = levelIndex;
        spawner.enemyColor   = enemyColor;
        spawner.enemySprite  = enemySprite;

        // Intervalle de spawn décroissant avec le niveau
        float t              = Mathf.InverseLerp(ActivationLevel, TBGameManager.TotalLevels - 1, levelIndex);
        spawner.spawnInterval = Mathf.Lerp(SpawnIntervalBase, SpawnIntervalMin, t);

        // Max ennemis croissant avec le niveau
        spawner.maxEnemies   = Mathf.RoundToInt(Mathf.Lerp(MaxEnemiesBase, MaxEnemiesMax, t));
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start() => StartCoroutine(SpawnRoutine());

    private IEnumerator SpawnRoutine()
    {
        yield return new WaitForSeconds(InitialDelay);

        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            int liveCount = CountLiveEnemies();
            if (liveCount < maxEnemies)
                SpawnOne();
        }
    }

    private static int CountLiveEnemies()
    {
        int count = 0;
        foreach (var go in GameObject.FindGameObjectsWithTag(TBSceneSetup.LevelContentTag))
            if (go.GetComponent<TBEnemyController>() != null)
                count++;
        return count;
    }

    private void SpawnOne()
    {
        // Choisit une position de spawn aléatoire sur les bords
        Vector2 pos = SpawnEdgePositions[Random.Range(0, SpawnEdgePositions.Length)];

        // Décale légèrement pour éviter les doublons exacts
        pos += new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f));

        var go = new GameObject($"SpawnedEnemy_{Time.frameCount}");
        go.tag = TBSceneSetup.LevelContentTag;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = enemySprite != null ? enemySprite : SpriteGenerator.CreateCircle(128);
        sr.color        = enemyColor;
        sr.sortingOrder = 2;

        go.transform.position   = pos;
        go.transform.localScale = new Vector3(0.18f, 0.18f, 1f);

        var rb            = go.AddComponent<Rigidbody2D>();
        rb.gravityScale   = 0f;
        rb.bodyType       = RigidbodyType2D.Kinematic;
        rb.constraints    = RigidbodyConstraints2D.FreezeRotation;

        var col       = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.22f;

        go.AddComponent<TBEnemyController>();
    }
}
