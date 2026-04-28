using System.Collections;
using UnityEngine;

/// <summary>
/// Arme du joueur TiltBall.
///
/// Quand l'arme est débloquée, des mini-projectiles bleus partent du joueur
/// dans toutes les directions (en cercle) à intervalles réguliers pour tuer les ennemis.
/// Plus besoin de cibler l'ennemi le plus proche.
/// </summary>
public class TBWeapon : MonoBehaviour
{
    // ── Configuration ─────────────────────────────────────────────────────────

    private const float FireInterval    = 1.8f;   // secondes entre chaque salve
    private const float ProjectileSpeed = 9f;
    private const float ProjectileSize  = 0.22f;
    private const int   BulletsPerSalvo = 12;     // 12 directions = cercle complet (30° chacun)

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

    // ── Tir en cercle ─────────────────────────────────────────────────────────

    private IEnumerator FireRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(FireInterval);

            if (player == null || !player.IsAlive) continue;

            FireCircle();
        }
    }

    /// <summary>Tire <see cref="BulletsPerSalvo"/> projectiles en cercle depuis la position du joueur.</summary>
    private void FireCircle()
    {
        Vector2 origin     = player.transform.position;
        float   angleStep  = 360f / BulletsPerSalvo;
        float   startAngle = Random.Range(0f, angleStep);   // rotation aléatoire de la salve

        for (int i = 0; i < BulletsPerSalvo; i++)
        {
            float   angle = startAngle + i * angleStep;
            float   rad   = angle * Mathf.Deg2Rad;
            Vector2 dir   = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            FireAt(origin, dir);
        }
    }

    private static void FireAt(Vector2 origin, Vector2 direction)
    {
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
            TBGameManager.PlayEnemyDeathSfx();
            Destroy(other.gameObject);
            Destroy(gameObject);
        }
    }
}
