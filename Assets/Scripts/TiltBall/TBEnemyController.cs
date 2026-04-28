using UnityEngine;

/// <summary>
/// Ennemi qui pourchasse et traverse le joueur.
///
/// Comportement :
/// - Poursuite permanente en continu, sans grille ni timer.
/// - L'ennemi se déplace librement à travers le joueur (pas de blocage).
/// - Le kill se déclenche quand l'ennemi entre dans le rayon de collision du joueur.
/// - Anti-collage : après le kill (ou si le joueur est mort), l'ennemi s'arrête.
/// - L'ennemi est bloqué par les murs et obstacles (colliders solides),
///   mais traverse le joueur qui est en trigger.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class TBEnemyController : MonoBehaviour
{
    // ── Constantes ────────────────────────────────────────────────────────────

    /// <summary>Vitesse de poursuite en unités par seconde.</summary>
    private const float ChaseSpeed = 1.5f;

    /// <summary>
    /// Distance de kill : quand l'ennemi est à moins de cette distance
    /// du centre du joueur, le joueur meurt. Doit être inférieure
    /// au rayon du collider du joueur pour simuler une vraie traversée.
    /// </summary>
    private const float KillDistance = 0.30f;

    /// <summary>
    /// Délai minimum en secondes entre deux checks de kill pour le même ennemi.
    /// Évite de déclencher Die() plusieurs fois d'affilée si le joueur respawn lentement.
    /// </summary>
    private const float KillCooldown = 1.0f;

    // ── État ──────────────────────────────────────────────────────────────────

    private Rigidbody2D        rb;
    private TBPlayerController player;
    private float              killTimer;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        rb              = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType     = RigidbodyType2D.Kinematic;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;
    }

    private void Start()
    {
        player = FindFirstObjectByType<TBPlayerController>();

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            gameObject.AddComponent<TBEnemyVisuals>().Init(sr);
    }

    private void FixedUpdate()
    {
        if (player == null || !player.IsAlive) return;

        killTimer = Mathf.Max(0f, killTimer - Time.fixedDeltaTime);

        Vector2 enemyPos  = rb.position;
        Vector2 playerPos = (Vector2)player.transform.position;
        Vector2 toPlayer  = playerPos - enemyPos;
        float   dist      = toPlayer.magnitude;

        // ── Kill par traversée ────────────────────────────────────────────────
        // L'ennemi est passé à travers le centre du joueur → mort
        if (dist < KillDistance && killTimer <= 0f)
        {
            killTimer = KillCooldown;
            TBExplosionFX.Spawn(transform.position);
            player.Die();
            return;
        }

        // ── Poursuite continue ────────────────────────────────────────────────
        if (dist < 0.01f) return;

        Vector2 dir  = toPlayer / dist;
        Vector2 next = enemyPos + dir * (ChaseSpeed * Time.fixedDeltaTime);
        rb.MovePosition(next);
    }
}
