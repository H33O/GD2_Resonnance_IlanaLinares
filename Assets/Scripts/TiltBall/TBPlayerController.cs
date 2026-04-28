using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Déplacement du joueur piloté par le joystick virtuel (<see cref="TBJoystick"/>)
/// ou le clavier WASD / flèches en éditeur.
///
/// Le joueur se déplace de façon continue (non tour-par-tour) :
/// la direction du joystick est lue chaque frame et appliquée directement
/// en <see cref="Rigidbody2D.MovePosition"/>, avec une vitesse fixe en unités/s.
/// Les murs et obstacles bloquent le mouvement via Physics2D.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class TBPlayerController : MonoBehaviour
{
    // ── Constantes ────────────────────────────────────────────────────────────

    /// <summary>Vitesse de déplacement du joueur en unités par seconde.</summary>
    private const float MoveSpeed = 5f;

    // ── État ──────────────────────────────────────────────────────────────────

    private Rigidbody2D    rb;
    private SpriteRenderer sr;
    private bool           isDead;
    private bool           isEnteringHole;

    public bool IsAlive => !isDead && !isEnteringHole;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        rb              = GetComponent<Rigidbody2D>();
        sr              = GetComponent<SpriteRenderer>();
        rb.bodyType     = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void FixedUpdate()
    {
        if (!IsAlive) return;

        Vector2 dir = ReadInput();
        if (dir == Vector2.zero) return;

        Vector2 next = rb.position + dir.normalized * (MoveSpeed * Time.fixedDeltaTime);

        // Bloque contre les murs et obstacles solides
        if (!IsBlockedAt(next))
            rb.MovePosition(Clamp(next));
    }

    /// <summary>Maintient le joueur dans les limites du monde.</summary>
    private static Vector2 Clamp(Vector2 pos)
    {
        float margin = 0.5f;
        return new Vector2(
            Mathf.Clamp(pos.x, -(TBSceneSetup.HalfW - margin), TBSceneSetup.HalfW - margin),
            Mathf.Clamp(pos.y, -(TBSceneSetup.HalfH - margin), TBSceneSetup.HalfH - margin)
        );
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lit la direction depuis le joystick virtuel et/ou le clavier WASD / flèches.
    /// Les deux sources sont actives en même temps (éditeur et build).
    /// Retourne un vecteur normalisé ou zéro.
    /// </summary>
    private static Vector2 ReadInput()
    {
        // ── Joystick virtuel (tactile) ────────────────────────────────────────
        Vector2 dir = TBJoystick.Instance != null ? TBJoystick.Instance.Direction : Vector2.zero;

        // ── Clavier (éditeur ou build avec clavier) ───────────────────────────
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) dir += Vector2.right;
            if (kb.leftArrowKey.isPressed  || kb.aKey.isPressed) dir += Vector2.left;
            if (kb.upArrowKey.isPressed    || kb.wKey.isPressed) dir += Vector2.up;
            if (kb.downArrowKey.isPressed  || kb.sKey.isPressed) dir += Vector2.down;
        }

        return dir.magnitude > 1f ? dir.normalized : dir;
    }

    // ── Collision ─────────────────────────────────────────────────────────────

    /// <summary>Retourne true si la position cible est bloquée par un collider solide.</summary>
    private bool IsBlockedAt(Vector2 pos)
    {
        var hits = Physics2D.OverlapCircleAll(pos, TBGrid.CheckRadius);
        foreach (var hit in hits)
        {
            if (hit.isTrigger)               continue;
            if (hit.gameObject == gameObject) continue;
            return true;
        }
        return false;
    }

    // ── Contact ennemi — côté joueur ──────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Filet de sécurité : le kill principal est dans TBEnemyController.
        if (other.GetComponent<TBEnemyController>() != null)
        {
            TBExplosionFX.Spawn(transform.position);
            Die();
        }
    }

    // ── Mort ──────────────────────────────────────────────────────────────────

    /// <summary>Déclenche la mort du joueur.</summary>
    public void Die()
    {
        if (!IsAlive) return;
        isDead = true;
        StopAllCoroutines();
        rb.linearVelocity = Vector2.zero;
        TBGameManager.PlayDeathSfx();
        StartCoroutine(DieRoutine());
    }

    private IEnumerator DieRoutine()
    {
        float t        = 0f;
        float duration = 0.50f;
        Color startCol = sr.color;

        while (t < duration)
        {
            t          += Time.deltaTime;
            float ratio = t / duration;
            sr.color             = Color.Lerp(startCol, new Color(1f, 0.15f, 0.1f, 0f), ratio);
            transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, ratio);
            yield return null;
        }

        TBGameManager.Instance?.RestartLevel();
    }

    // ── Entrée dans le trou ───────────────────────────────────────────────────

    /// <summary>Anime la chute dans le trou, puis signale au GameManager.</summary>
    public void EnterHole(Vector2 holeCenter)
    {
        if (!IsAlive) return;
        isEnteringHole = true;
        StopAllCoroutines();
        rb.linearVelocity = Vector2.zero;
        TBGameManager.PlayGoalSfx();
        StartCoroutine(EnterHoleRoutine(holeCenter));
    }

    private IEnumerator EnterHoleRoutine(Vector2 holeCenter)
    {
        float   t          = 0f;
        float   duration   = 0.40f;
        Vector2 startPos   = rb.position;
        Vector3 startScale = transform.localScale;

        while (t < duration)
        {
            t += Time.deltaTime;
            float ratio = t / duration;
            rb.MovePosition(Vector2.Lerp(startPos, holeCenter, ratio));
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, ratio);
            yield return null;
        }

        TBGameManager.Instance?.EnterHole();
    }
}
