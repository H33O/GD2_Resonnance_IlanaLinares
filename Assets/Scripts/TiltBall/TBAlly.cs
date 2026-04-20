using UnityEngine;

/// <summary>
/// Allié placé sur le niveau TiltBall.
/// Suit le joueur à distance fixe et bloque les ennemis au contact.
/// Spawné par TBSceneSetup si l'amélioration "Allié" a été achetée.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class TBAlly : MonoBehaviour
{
    // ── Configuration ─────────────────────────────────────────────────────────

    private const float FollowSpeed    = 4.5f;   // unités / seconde
    private const float FollowDistance = 2.2f;   // distance cible derrière le joueur
    private const float StopRadius     = 0.15f;  // zone morte (évite le tremblement)

    // ── État ──────────────────────────────────────────────────────────────────

    private Rigidbody2D        rb;
    private TBPlayerController player;

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
    }

    private void FixedUpdate()
    {
        if (player == null || !player.IsAlive) return;

        Vector2 toPlayer   = (Vector2)player.transform.position - rb.position;
        float   dist       = toPlayer.magnitude;

        // Reste à FollowDistance du joueur — avance si plus loin, recule si trop proche
        float targetDist  = FollowDistance;
        float gap         = dist - targetDist;

        if (Mathf.Abs(gap) < StopRadius)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 dir       = toPlayer.normalized;
        rb.linearVelocity = dir * (Mathf.Sign(gap) * FollowSpeed);
    }

    // ── Collision ennemis ─────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        // L'allié détruit les ennemis au contact
        if (other.GetComponent<TBEnemyController>() != null)
            Destroy(other.gameObject);
    }

    // ── Spawn statique ────────────────────────────────────────────────────────

    /// <summary>
    /// Crée un allié à la position donnée avec les visuels procéduraux.
    /// </summary>
    public static void Spawn(Vector2 position, Color color)
    {
        var go = new GameObject("Ally");
        go.tag = TBSceneSetup.LevelContentTag;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = SpriteGenerator.CreateCircle(128);
        sr.color        = color;
        sr.sortingOrder = 3;

        go.transform.position   = position;
        go.transform.localScale = new Vector3(0.75f, 0.75f, 1f);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        var col       = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.42f;

        go.AddComponent<TBAlly>();
    }
}
