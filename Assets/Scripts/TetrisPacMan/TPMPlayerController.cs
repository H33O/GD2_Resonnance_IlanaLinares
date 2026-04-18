using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Contrôleur du joueur dans le mini-jeu Tetris×Pac-Man.
/// Déplacement par grille, pose et destruction de blocs via les inputs.
/// Clavier : WASD/Flèches pour déplacer, Espace pour poser un bloc,
///           E ou Q pour détruire un bloc adjacent ciblé.
/// Souris/Tactile : clic sur une cellule adjacente pour se déplacer,
///                  clic droit pour poser/détruire un bloc.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class TPMPlayerController : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Settings")]
    public TPMSettings settings;

    [Header("References")]
    public Camera gameCamera;

    // ── État ──────────────────────────────────────────────────────────────────

    private Vector2Int cellPos;
    private bool       isMoving;
    private Vector2Int facingDir = Vector2Int.right;

    // Visuels
    private SpriteRenderer bodySR;
    private SpriteRenderer glowSR;
    private SpriteRenderer eyeSR;

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

        HandleKeyboardMovement();
        HandleKeyboardBlockActions();
        HandleMouseInput();

        AnimateGlow();
        AnimateEye();
    }

    // ── Déplacement ───────────────────────────────────────────────────────────

    private void HandleKeyboardMovement()
    {
        if (isMoving) return;
        if (Keyboard.current == null) return;

        Vector2Int dir = Vector2Int.zero;

        if (Keyboard.current.upArrowKey.wasPressedThisFrame    || Keyboard.current.wKey.wasPressedThisFrame) dir = Vector2Int.up;
        else if (Keyboard.current.downArrowKey.wasPressedThisFrame  || Keyboard.current.sKey.wasPressedThisFrame) dir = Vector2Int.down;
        else if (Keyboard.current.leftArrowKey.wasPressedThisFrame  || Keyboard.current.aKey.wasPressedThisFrame) dir = Vector2Int.left;
        else if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame) dir = Vector2Int.right;

        if (dir != Vector2Int.zero)
        {
            facingDir = dir;
            TryMove(dir);
        }
    }

    private void TryMove(Vector2Int dir)
    {
        Vector2Int target = cellPos + dir;
        if (!TPMGrid.Instance.IsWalkable(target.x, target.y)) return;

        StartCoroutine(SlideToCell(target));
    }

    private IEnumerator SlideToCell(Vector2Int target)
    {
        isMoving = true;

        Vector3 startPos = transform.position;
        Vector3 endPos   = TPMGrid.Instance.CellToWorld(target.x, target.y);

        float dist = settings.cellSize;
        float duration = dist / settings.playerSpeed;
        float elapsed  = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
            yield return null;
        }

        transform.position = endPos;
        cellPos = target;
        isMoving = false;

        // Vérification sortie
        if (TPMGrid.Instance.GetCell(cellPos.x, cellPos.y) == TPMGrid.CellType.Exit)
            TPMGameManager.Instance?.NotifyReachedExit();
    }

    // ── Actions sur les blocs ─────────────────────────────────────────────────

    private void HandleKeyboardBlockActions()
    {
        if (Keyboard.current == null) return;

        // Espace : poser un bloc dans la direction du regard
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            PlaceBlockInFacing();

        // E : détruire le bloc dans la direction du regard
        if (Keyboard.current.eKey.wasPressedThisFrame)
            DestroyBlockInFacing();
    }

    private void PlaceBlockInFacing()
    {
        Vector2Int target = cellPos + facingDir;
        if (!TPMGrid.Instance.InBounds(target.x, target.y)) return;
        if (!TPMGrid.Instance.IsEmpty(target.x, target.y))  return;
        if (!TPMGameManager.Instance.TrySpendMove())         return;

        TPMGrid.Instance.TryPlaceBlock(target.x, target.y);
        TPMBlockManager.Instance?.SpawnBlock(target.x, target.y);
        TPMFeedbackManager.Instance?.PlayPlaceEffect(
            TPMGrid.Instance.CellToWorld(target.x, target.y));
    }

    private void DestroyBlockInFacing()
    {
        Vector2Int target = cellPos + facingDir;
        if (!TPMGrid.Instance.IsPlayerBlock(target.x, target.y)) return;

        Vector3 worldPos = TPMGrid.Instance.CellToWorld(target.x, target.y);
        TPMGrid.Instance.TryDestroyBlock(target.x, target.y);
        TPMBlockManager.Instance?.RemoveBlock(target.x, target.y);
        TPMFeedbackManager.Instance?.PlayDestroyEffect(worldPos);
        TPMGameManager.Instance?.NotifyBlocksDestroyed(1, worldPos);
    }

    // ── Souris / Tactile ──────────────────────────────────────────────────────

    private void HandleMouseInput()
    {
        if (gameCamera == null || Mouse.current == null) return;

        // Clic gauche : déplacement
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2Int clicked = ScreenToCell(Mouse.current.position.ReadValue());
            Vector2Int delta   = clicked - cellPos;

            // Déplacement uniquement si cellule adjacente
            if (Mathf.Abs(delta.x) + Mathf.Abs(delta.y) == 1)
            {
                facingDir = delta;
                TryMove(delta);
            }
        }

        // Clic droit : poser ou détruire bloc
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            Vector2Int clicked = ScreenToCell(Mouse.current.position.ReadValue());
            Vector2Int delta   = clicked - cellPos;

            if (Mathf.Abs(delta.x) + Mathf.Abs(delta.y) == 1)
            {
                facingDir = delta;
                if (TPMGrid.Instance.IsPlayerBlock(clicked.x, clicked.y))
                {
                    Vector3 worldPos = TPMGrid.Instance.CellToWorld(clicked.x, clicked.y);
                    TPMGrid.Instance.TryDestroyBlock(clicked.x, clicked.y);
                    TPMBlockManager.Instance?.RemoveBlock(clicked.x, clicked.y);
                    TPMFeedbackManager.Instance?.PlayDestroyEffect(worldPos);
                    TPMGameManager.Instance?.NotifyBlocksDestroyed(1, worldPos);
                }
                else if (TPMGrid.Instance.IsEmpty(clicked.x, clicked.y))
                {
                    if (!TPMGameManager.Instance.TrySpendMove()) return;
                    TPMGrid.Instance.TryPlaceBlock(clicked.x, clicked.y);
                    TPMBlockManager.Instance?.SpawnBlock(clicked.x, clicked.y);
                    TPMFeedbackManager.Instance?.PlayPlaceEffect(
                        TPMGrid.Instance.CellToWorld(clicked.x, clicked.y));
                }
            }
        }
    }

    private Vector2Int ScreenToCell(Vector2 screenPos)
    {
        Vector3 world = gameCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -gameCamera.transform.position.z));
        return TPMGrid.Instance.WorldToCell(world);
    }

    // ── Accesseur ─────────────────────────────────────────────────────────────

    /// <summary>Position courante du joueur sur la grille.</summary>
    public Vector2Int CellPosition => cellPos;

    // ── Visuels ───────────────────────────────────────────────────────────────

    private void BuildVisuals()
    {
        // Corps principal : cercle cyan vif (style arcade)
        bodySR         = GetComponent<SpriteRenderer>();
        bodySR.sprite  = SpriteGenerator.CreateCircle(64);
        bodySR.color   = new Color(0.15f, 0.85f, 1.00f, 1f);
        bodySR.sortingOrder = 10;
        transform.localScale = Vector3.one * 0.68f;

        // Halo de brillance
        var glowGO       = new GameObject("Glow");
        glowGO.transform.SetParent(transform, false);
        glowGO.transform.localScale = Vector3.one * 1.6f;
        glowSR           = glowGO.AddComponent<SpriteRenderer>();
        glowSR.sprite    = SpriteGenerator.CreateCircle(64);
        glowSR.color     = new Color(0.1f, 0.7f, 1f, 0.15f);
        glowSR.sortingOrder = 9;

        // Œil (direction)
        var eyeGO        = new GameObject("Eye");
        eyeGO.transform.SetParent(transform, false);
        eyeGO.transform.localScale    = Vector3.one * 0.28f;
        eyeGO.transform.localPosition = new Vector3(0.28f, 0.1f, 0f);
        eyeSR            = eyeGO.AddComponent<SpriteRenderer>();
        eyeSR.sprite     = SpriteGenerator.CreateCircle(32);
        eyeSR.color      = Color.black;
        eyeSR.sortingOrder = 11;
    }

    private void AnimateGlow()
    {
        if (glowSR == null) return;
        float a    = 0.12f + 0.08f * Mathf.Sin(Time.time * 4f);
        glowSR.color = new Color(0.1f, 0.7f, 1f, a);
    }

    private void AnimateEye()
    {
        if (eyeSR == null) return;
        // L'œil suit la direction du regard
        Vector3 offset = new Vector3(facingDir.x, facingDir.y, 0f) * 0.28f;
        eyeSR.transform.localPosition = Vector3.Lerp(
            eyeSR.transform.localPosition, offset, Time.deltaTime * 12f);
    }
}
