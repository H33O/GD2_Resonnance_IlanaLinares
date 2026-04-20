using UnityEngine;

/// <summary>
/// Ennemi qui se déplace case par case sur la grille, à la même cadence que le joueur.
/// En mode poursuite (joueur dans la zone de détection) : avance vers le joueur.
/// En mode errance : choisit une cible aléatoire sur la grille.
/// Tue le joueur au contact via trigger.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class TBEnemyController : MonoBehaviour
{
    // ── État ──────────────────────────────────────────────────────────────────

    private Rigidbody2D        rb;
    private TBPlayerController player;
    private Vector2            gridPos;
    private float              moveTimer;
    private Vector2            wanderTarget;

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
        player       = FindFirstObjectByType<TBPlayerController>();
        gridPos      = SnapToGrid(rb.position);
        rb.position  = gridPos;
        PickNewWanderTarget();

        // Décalage aléatoire pour éviter que tous les ennemis bougent en même temps.
        moveTimer = Random.Range(0f, TBGrid.MoveInterval);
    }

    private void Update()
    {
        moveTimer -= Time.deltaTime;
        if (moveTimer > 0f) return;

        moveTimer = TBGrid.MoveInterval;

        Vector2 dir = ChooseDirection();
        if (dir == Vector2.zero) return;

        Vector2 target = gridPos + dir * TBGrid.StepSize;

        if (CanMoveTo(target))
        {
            gridPos = target;
            rb.MovePosition(gridPos);
        }
        else
        {
            // Cible de errance devenue invalide : en choisir une nouvelle.
            PickNewWanderTarget();
        }
    }

    // ── Décision de mouvement ─────────────────────────────────────────────────

    /// <summary>Choisit la direction de déplacement selon le mode actif.</summary>
    private Vector2 ChooseDirection()
    {
        float detectionRange = TBLevelBootstrap.Settings != null
            ? TBLevelBootstrap.Settings.enemyDetectionRange
            : 10f;

        bool inChase = player != null
            && player.IsAlive
            && Vector2.Distance(gridPos, (Vector2)player.transform.position) < detectionRange;

        Vector2 goalPos = inChase ? (Vector2)player.transform.position : wanderTarget;

        // Actualiser la cible d'errance si atteinte.
        if (!inChase && Vector2.Distance(gridPos, wanderTarget) < TBGrid.StepSize * 0.5f)
            PickNewWanderTarget();

        return DominantDirection(goalPos - gridPos);
    }

    /// <summary>Retourne l'axe dominant (4 directions) d'un vecteur.</summary>
    private static Vector2 DominantDirection(Vector2 delta)
    {
        if (delta == Vector2.zero) return Vector2.zero;

        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            return delta.x > 0f ? Vector2.right : Vector2.left;
        else
            return delta.y > 0f ? Vector2.up : Vector2.down;
    }

    // ── Grille ────────────────────────────────────────────────────────────────

    private static Vector2 SnapToGrid(Vector2 pos)
    {
        return new Vector2(
            Mathf.Round(pos.x / TBGrid.StepSize) * TBGrid.StepSize,
            Mathf.Round(pos.y / TBGrid.StepSize) * TBGrid.StepSize
        );
    }

    /// <summary>
    /// Vérifie si l'ennemi peut se déplacer vers target.
    /// Bloque sur les murs (non-trigger) et les bords de l'écran.
    /// </summary>
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

    // ── Errance ───────────────────────────────────────────────────────────────

    private void PickNewWanderTarget()
    {
        wanderTarget = new Vector2(
            Mathf.Round(Random.Range(-TBGrid.MaxX, TBGrid.MaxX) / TBGrid.StepSize) * TBGrid.StepSize,
            Mathf.Round(Random.Range(-TBGrid.MaxY, TBGrid.MaxY) / TBGrid.StepSize) * TBGrid.StepSize
        );
    }

    // ── Contact joueur ────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        var p = other.GetComponent<TBPlayerController>();
        if (p != null) p.Die();
    }
}
