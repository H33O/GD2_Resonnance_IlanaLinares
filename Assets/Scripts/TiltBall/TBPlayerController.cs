using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Déplacement continu du joueur piloté par le swipe (TBSwipeInput)
/// ou le clavier WASD / flèches en éditeur.
///
/// Le joueur est stoppé par les murs via le moteur physique (Rigidbody2D Dynamic).
/// Les triggers (trou, clé, ennemi) sont détectés par OnTriggerEnter2D.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class TBPlayerController : MonoBehaviour
{
    // ── Vitesse ───────────────────────────────────────────────────────────────

    private const float MoveSpeed = 6.5f;   // unités monde / seconde

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
        rb.bodyType     = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.linearDamping        = 8f;
        rb.angularDamping       = 0f;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void FixedUpdate()
    {
        if (!IsAlive)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 dir = ReadInput();
        rb.linearVelocity = dir * MoveSpeed;

        // Clamp la position dans les limites du monde — rayon du collider inclus
        const float ColliderRadius = 0.42f;
        float lim  = TBSceneSetup.HalfW - TBSceneSetup.WallThickness - ColliderRadius;
        float limY = TBSceneSetup.HalfH - TBSceneSetup.WallThickness - ColliderRadius;

        Vector2 pos     = rb.position;
        Vector2 clamped = new Vector2(
            Mathf.Clamp(pos.x, -lim,  lim),
            Mathf.Clamp(pos.y, -limY, limY));

        if (clamped != pos)
        {
            rb.MovePosition(clamped);

            var v = rb.linearVelocity;
            if (Mathf.Abs(clamped.x) >= lim)  v.x = 0f;
            if (Mathf.Abs(clamped.y) >= limY) v.y = 0f;
            rb.linearVelocity = v;
        }
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lit la direction depuis le swipe (TBSwipeInput)
    /// ou le clavier WASD / flèches (éditeur).
    /// Retourne un vecteur normalisé ou réduit selon l'intensité du swipe.
    /// </summary>
    private static Vector2 ReadInput()
    {
#if UNITY_EDITOR
        var kb = Keyboard.current;
        if (kb == null) return Vector2.zero;

        var raw = Vector2.zero;
        if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) raw.x += 1f;
        if (kb.leftArrowKey.isPressed  || kb.aKey.isPressed) raw.x -= 1f;
        if (kb.upArrowKey.isPressed    || kb.wKey.isPressed) raw.y += 1f;
        if (kb.downArrowKey.isPressed  || kb.sKey.isPressed) raw.y -= 1f;

        return raw.magnitude > 0f ? raw.normalized : Vector2.zero;
#else
        if (TBSwipeInput.Instance != null)
            return TBSwipeInput.Instance.Direction;

        return Vector2.zero;
#endif
    }

    // ── Mort ──────────────────────────────────────────────────────────────────

    /// <summary>Déclenche la mort du joueur (appelée par TBEnemyController).</summary>
    public void Die()
    {
        if (!IsAlive) return;
        isDead = true;
        rb.linearVelocity = Vector2.zero;
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
        isEnteringHole    = true;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType       = RigidbodyType2D.Kinematic;
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

