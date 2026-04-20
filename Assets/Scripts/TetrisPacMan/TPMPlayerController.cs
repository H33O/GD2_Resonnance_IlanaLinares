using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Contrôleur du joueur Pac-Man — grille stricte 12×18.
///
/// ZQSD  → déplacement case par case (une pression = un pas)
/// Espace → détruire le bloc ciblé (cellule en face du regard)
///
/// Entrer sur la cellule EXIT déclenche la victoire.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class TPMPlayerController : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Settings")]
    public TPMSettings settings;

    // ── État ──────────────────────────────────────────────────────────────────

    private Vector2Int cellPos;
    private bool       isMoving;
    private Vector2Int facingDir = Vector2Int.right;

    // Visuels
    private SpriteRenderer bodySR;
    private SpriteRenderer glowSR;
    private SpriteRenderer eyeSR;
    private Transform      eyeTransform;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        BuildVisuals();
    }

    private void Start()
    {
        if (TPMGrid.Instance == null) return;
        cellPos = TPMGrid.Instance.PlayerStart;
        transform.position = TPMGrid.Instance.CellToWorld(cellPos.x, cellPos.y);
    }

    private void Update()
    {
        if (TPMGameManager.Instance?.State != TPMGameManager.GameState.Playing) return;
        HandleMovement();
        HandleDestroyInput();
        AnimateGlow();
        AnimateEye();
    }

    // ── Déplacement : ZQSD ────────────────────────────────────────────────────

    private void HandleMovement()
    {
        if (isMoving || Keyboard.current == null) return;

        Vector2Int dir = Vector2Int.zero;

        // AZERTY : Z=haut S=bas Q=gauche D=droite
        if      (Keyboard.current.zKey.wasPressedThisFrame) dir = Vector2Int.up;
        else if (Keyboard.current.sKey.wasPressedThisFrame) dir = Vector2Int.down;
        else if (Keyboard.current.qKey.wasPressedThisFrame) dir = Vector2Int.left;
        else if (Keyboard.current.dKey.wasPressedThisFrame) dir = Vector2Int.right;

        if (dir == Vector2Int.zero) return;

        facingDir = dir;
        Vector2Int target = cellPos + dir;
        if (!TPMGrid.Instance.IsWalkable(target.x, target.y)) return;

        StartCoroutine(SlideToCell(target));
    }

    private IEnumerator SlideToCell(Vector2Int target)
    {
        isMoving = true;
        Vector3 startPos = transform.position;
        Vector3 endPos   = TPMGrid.Instance.CellToWorld(target.x, target.y);
        float   duration = settings.cellSize / settings.playerSpeed;
        float   elapsed  = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, endPos,
                Mathf.SmoothStep(0f, 1f, elapsed / duration));
            yield return null;
        }

        transform.position = endPos;
        cellPos = target;
        isMoving = false;

        // Victoire si EXIT atteinte
        if (TPMGrid.Instance.GetCell(cellPos.x, cellPos.y) == TPMGrid.CellType.Exit)
            TPMGameManager.Instance?.NotifyGoalReached();
    }

    // ── Destruction bloc : Espace ─────────────────────────────────────────────

    private void HandleDestroyInput()
    {
        if (Keyboard.current == null) return;
        if (!Keyboard.current.spaceKey.wasPressedThisFrame) return;

        Vector2Int target = cellPos + facingDir;
        if (!TPMGrid.Instance.IsPlayerBlock(target.x, target.y)) return;

        Vector3 worldPos = TPMGrid.Instance.CellToWorld(target.x, target.y);
        TPMGrid.Instance.TryDestroyBlock(target.x, target.y);
        TPMBlockManager.Instance?.RemoveBlock(target.x, target.y);
        TPMFeedbackManager.Instance?.PlayDestroyEffect(worldPos);
        TPMGameManager.Instance?.NotifyBlockDestroyed(worldPos);
    }

    // ── Accesseurs ────────────────────────────────────────────────────────────

    /// <summary>Position courante du joueur sur la grille.</summary>
    public Vector2Int CellPosition => cellPos;

    /// <summary>Vrai si le joueur est en cours de déplacement.</summary>
    public bool IsMoving => isMoving;

    // ── Visuels ───────────────────────────────────────────────────────────────

    private void BuildVisuals()
    {
        bodySR               = GetComponent<SpriteRenderer>();
        bodySR.sprite        = SpriteGenerator.CreateCircle(64);
        bodySR.color         = new Color(1.00f, 0.88f, 0.00f, 1f);
        bodySR.sortingOrder  = 12;
        transform.localScale = Vector3.one * 0.70f;

        var glowGO    = new GameObject("Glow");
        glowGO.transform.SetParent(transform, false);
        glowGO.transform.localScale = Vector3.one * 1.55f;
        glowSR        = glowGO.AddComponent<SpriteRenderer>();
        glowSR.sprite = SpriteGenerator.CreateCircle(64);
        glowSR.color  = new Color(1f, 0.88f, 0f, 0.18f);
        glowSR.sortingOrder = 11;

        var eyeGO    = new GameObject("Eye");
        eyeGO.transform.SetParent(transform, false);
        eyeGO.transform.localScale    = Vector3.one * 0.26f;
        eyeGO.transform.localPosition = new Vector3(0.25f, 0.08f, 0f);
        eyeTransform = eyeGO.transform;
        eyeSR        = eyeGO.AddComponent<SpriteRenderer>();
        eyeSR.sprite = SpriteGenerator.CreateCircle(32);
        eyeSR.color  = Color.black;
        eyeSR.sortingOrder = 13;
    }

    private void AnimateGlow()
    {
        if (glowSR == null) return;
        float a = 0.14f + 0.10f * Mathf.Sin(Time.time * 4f);
        glowSR.color = new Color(1f, 0.88f, 0f, a);
    }

    private void AnimateEye()
    {
        if (eyeTransform == null) return;
        Vector3 t = new Vector3(facingDir.x, facingDir.y, 0f) * 0.25f;
        eyeTransform.localPosition = Vector3.Lerp(eyeTransform.localPosition, t, Time.deltaTime * 14f);
    }
}
