using System.Collections;
using UnityEngine;

/// <summary>
/// Arme du joueur TiltBall : tire automatiquement un projectile vers l'ennemi le plus proche
/// toutes les <see cref="FireInterval"/> secondes.
/// Spawné par TBSceneSetup si l'amélioration "Arme" a été achetée.
/// S'attache au joueur et le suit.
/// </summary>
public class TBWeapon : MonoBehaviour
{
    // ── Configuration ─────────────────────────────────────────────────────────

    private const float FireInterval    = 2.0f;  // secondes entre chaque tir
    private const float ProjectileSpeed = 9f;
    private const float ProjectileSize  = 0.25f;
    private const float MaxRange        = 12f;   // portée max de détection d'ennemi

    // ── Couleurs ──────────────────────────────────────────────────────────────

    private static readonly Color ColWeapon     = new Color(0.20f, 0.80f, 1.00f, 1f);
    private static readonly Color ColProjectile = new Color(0.20f, 0.80f, 1.00f, 0.90f);

    // ── Références ────────────────────────────────────────────────────────────

    private TBPlayerController player;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        player = FindFirstObjectByType<TBPlayerController>();
        StartCoroutine(FireRoutine());
    }

    private void Update()
    {
        // Suit le joueur
        if (player != null)
            transform.position = (Vector2)player.transform.position + new Vector2(0.55f, 0.55f);
    }

    // ── Tir automatique ───────────────────────────────────────────────────────

    private IEnumerator FireRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(FireInterval);

            if (player == null || !player.IsAlive) continue;

            TBEnemyController target = FindClosestEnemy();
            if (target == null) continue;

            FireAt(target.transform.position);
        }
    }

    private static TBEnemyController FindClosestEnemy()
    {
        TBEnemyController closest  = null;
        float             minDist  = MaxRange;

        foreach (var enemy in FindObjectsByType<TBEnemyController>(FindObjectsSortMode.None))
        {
            float d = Vector2.Distance(
                enemy.transform.position,
                FindFirstObjectByType<TBPlayerController>()?.transform.position ?? Vector2.zero);

            if (d < minDist)
            {
                minDist = d;
                closest = enemy;
            }
        }

        return closest;
    }

    private void FireAt(Vector2 target)
    {
        Vector2 origin    = transform.position;
        Vector2 direction = (target - origin).normalized;

        var go = new GameObject("Projectile");
        go.tag = TBSceneSetup.LevelContentTag;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = SpriteGenerator.CreateCircle(64);
        sr.color        = ColProjectile;
        sr.sortingOrder = 4;

        go.transform.position   = origin;
        go.transform.localScale = Vector3.one * ProjectileSize;

        var col       = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.5f;

        var proj = go.AddComponent<TBProjectile>();
        proj.Init(direction, ProjectileSpeed);
    }

    // ── Spawn statique ────────────────────────────────────────────────────────

    /// <summary>Crée l'arme et l'attache au joueur.</summary>
    public static void Spawn()
    {
        var go = new GameObject("PlayerWeapon");
        go.tag = TBSceneSetup.LevelContentTag;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = SpriteGenerator.CreatePolygon(3, 64);
        sr.color        = ColWeapon;
        sr.sortingOrder = 4;

        go.transform.localScale = Vector3.one * 0.45f;

        go.AddComponent<TBWeapon>();
    }
}

/// <summary>
/// Projectile tiré par <see cref="TBWeapon"/>.
/// Se déplace en ligne droite et détruit l'ennemi au contact.
/// </summary>
public class TBProjectile : MonoBehaviour
{
    private Vector2 direction;
    private float   speed;
    private float   lifetime = 3f;

    /// <summary>Initialise la direction et la vitesse du projectile.</summary>
    public void Init(Vector2 dir, float spd)
    {
        direction = dir;
        speed     = spd;
    }

    private void Update()
    {
        transform.position = (Vector2)transform.position + direction * speed * Time.deltaTime;

        lifetime -= Time.deltaTime;
        if (lifetime <= 0f) Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<TBEnemyController>() != null)
        {
            Destroy(other.gameObject);
            Destroy(gameObject);
        }
    }
}
