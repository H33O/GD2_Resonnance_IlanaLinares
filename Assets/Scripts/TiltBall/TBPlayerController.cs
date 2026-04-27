using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Déplacement du joueur case par case piloté par le swipe (TBSwipeInput)
/// ou le clavier WASD / flèches en éditeur.
///
/// Un swipe = une case dans la direction de l'axe dominant.
/// Le déplacement est interpolé visuellement sur MoveInterval secondes.
/// Le joueur est bloqué par les murs et obstacles (CanMoveTo via Physics2D).
/// Les triggers (trou, clé, ennemi) sont détectés par OnTriggerEnter2D.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class TBPlayerController : MonoBehaviour
{
    // ── État ──────────────────────────────────────────────────────────────────

    private Rigidbody2D    rb;
    private SpriteRenderer sr;
    private bool           isDead;
    private bool           isEnteringHole;
    private bool           isMoving;
    private Vector2        gridPos;

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

    private void Start()
    {
        gridPos     = SnapToGrid(rb.position);
        rb.position = gridPos;
    }

    private void Update()
    {
        if (!IsAlive || isMoving) return;

        Vector2 dir = ReadInput();
        if (dir == Vector2.zero) return;

        Vector2 target = gridPos + dir * TBGrid.StepSize;
        if (!CanMoveTo(target)) return;

        gridPos = target;
        StartCoroutine(SlideTo(gridPos));
    }

    // ── Déplacement interpolé ─────────────────────────────────────────────────

    private IEnumerator SlideTo(Vector2 target)
    {
        isMoving = true;

        Vector2 start    = rb.position;
        float   elapsed  = 0f;
        float   duration = TBGrid.MoveInterval;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            rb.MovePosition(Vector2.Lerp(start, target, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }

        rb.MovePosition(target);
        isMoving = false;
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lit la direction depuis le swipe (TBSwipeInput en build)
    /// ou le clavier WASD / flèches (éditeur).
    /// Retourne un vecteur cardinal (Vector2.up/down/left/right) ou zéro.
    /// </summary>
    private static Vector2 ReadInput()
    {
#if UNITY_EDITOR
        var kb = Keyboard.current;
        if (kb == null) return Vector2.zero;

        if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) return Vector2.right;
        if (kb.leftArrowKey.wasPressedThisFrame  || kb.aKey.wasPressedThisFrame) return Vector2.left;
        if (kb.upArrowKey.wasPressedThisFrame    || kb.wKey.wasPressedThisFrame) return Vector2.up;
        if (kb.downArrowKey.wasPressedThisFrame  || kb.sKey.wasPressedThisFrame) return Vector2.down;

        return Vector2.zero;
#else
        if (TBSwipeInput.Instance != null)
            return TBSwipeInput.Instance.ConsumeDirection();

        return Vector2.zero;
#endif
    }

    // ── Vérification mouvement ────────────────────────────────────────────────

    private bool CanMoveTo(Vector2 target)
    {
        if (Mathf.Abs(target.x) > TBGrid.MaxX) return false;
        if (Mathf.Abs(target.y) > TBGrid.MaxY) return false;

        var hits = Physics2D.OverlapCircleAll(target, TBGrid.CheckRadius);
        foreach (var hit in hits)
        {
            if (hit.isTrigger)               continue;
            if (hit.gameObject == gameObject) continue;
            return false;
        }
        return true;
    }

    // ── Utilitaire grille ─────────────────────────────────────────────────────

    private static Vector2 SnapToGrid(Vector2 pos)
    {
        return new Vector2(
            Mathf.Round(pos.x / TBGrid.StepSize) * TBGrid.StepSize,
            Mathf.Round(pos.y / TBGrid.StepSize) * TBGrid.StepSize
        );
    }

    // ── Mort ──────────────────────────────────────────────────────────────────

    /// <summary>Déclenche la mort du joueur (appelée par TBEnemyController).</summary>
    public void Die()
    {
        if (!IsAlive) return;
        isDead = true;
        StopAllCoroutines();
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
        isEnteringHole = true;
        StopAllCoroutines();
        rb.linearVelocity = Vector2.zero;
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
