using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

/// <summary>
/// Gère les entrées tactiles et souris pour le jeu Tetris×Pac-Man mobile.
/// 
/// — Tap sur la grille       : déplace le joueur vers la cellule tapée (chemin BFS)
///                              ou pose/retire un bloc selon la palette sélectionnée
/// — Palette (bas d'écran)   : sélection de la couleur du bloc à poser
/// 
/// Règle de priorité :
///   1. Si la cellule est vide  → poser un bloc de la couleur sélectionnée (coûte 1 coup)
///   2. Si la cellule a un bloc joueur → le retirer
///   3. Si la cellule est traversable → déplacer le joueur vers elle (BFS)
/// </summary>
[RequireComponent(typeof(TPMPlayerController))]
public class TPMTouchInput : MonoBehaviour
{
    // ── Références ────────────────────────────────────────────────────────────

    public Camera gameCamera;

    private TPMPlayerController player;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        player = GetComponent<TPMPlayerController>();
    }

    private void Update()
    {
        if (TPMGameManager.Instance?.State != TPMGameManager.GameState.Playing) return;

        HandleTouch();
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    private void HandleTouch()
    {
        // Support tactile réel (mobile) ET souris (éditeur/PC)
        bool tapped     = false;
        Vector2 tapPos  = Vector2.zero;

        // Écran tactile
        var touchscreen = Touchscreen.current;
        if (touchscreen != null && touchscreen.primaryTouch.press.wasPressedThisFrame)
        {
            tapped = true;
            tapPos = touchscreen.primaryTouch.position.ReadValue();
        }

        // Souris (fallback éditeur)
        if (!tapped && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            tapped = true;
            tapPos = Mouse.current.position.ReadValue();
        }

        if (!tapped) return;

        // Ne pas traiter si le tap est sur un élément UI (la palette par ex.)
        if (IsPointerOverUI(tapPos)) return;

        ProcessTap(tapPos);
    }

    private void ProcessTap(Vector2 screenPos)
    {
        if (gameCamera == null || TPMGrid.Instance == null) return;

        Vector3 world = gameCamera.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, -gameCamera.transform.position.z));
        Vector2Int cell = TPMGrid.Instance.WorldToCell(world);

        if (!TPMGrid.Instance.InBounds(cell.x, cell.y)) return;

        var cellType = TPMGrid.Instance.GetCell(cell.x, cell.y);

        switch (cellType)
        {
            case TPMGrid.CellType.PlayerBlock:
                // Tap sur un bloc posé → le retirer
                TryRemoveBlock(cell);
                break;

            case TPMGrid.CellType.Empty:
                // Tap sur cellule vide → poser un bloc
                TryPlaceBlock(cell);
                break;

            default:
                // Mur, Tetris, Exit → ignorer
                break;
        }
    }

    // ── Pose de bloc ──────────────────────────────────────────────────────────

    private void TryPlaceBlock(Vector2Int cell)
    {
        if (TPMBlockPalette.Instance == null) return;
        if (!TPMGameManager.Instance.TrySpendMove()) return;

        Color color = TPMBlockPalette.Instance.SelectedColor;
        TPMGrid.Instance.TryPlaceBlock(cell.x, cell.y);
        TPMBlockManager.Instance?.SpawnBlock(cell.x, cell.y, color);
        TPMFeedbackManager.Instance?.PlayPlaceEffect(
            TPMGrid.Instance.CellToWorld(cell.x, cell.y));
    }

    // ── Retrait de bloc ───────────────────────────────────────────────────────

    private void TryRemoveBlock(Vector2Int cell)
    {
        Vector3 worldPos = TPMGrid.Instance.CellToWorld(cell.x, cell.y);
        TPMGrid.Instance.TryDestroyBlock(cell.x, cell.y);
        TPMBlockManager.Instance?.RemoveBlock(cell.x, cell.y);
        TPMFeedbackManager.Instance?.PlayDestroyEffect(worldPos);
        TPMGameManager.Instance?.NotifyBlocksDestroyed(1, worldPos);
    }

    // ── Détection UI ──────────────────────────────────────────────────────────

    private static bool IsPointerOverUI(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;

        var pointerData = new UnityEngine.EventSystems.PointerEventData(EventSystem.current)
        {
            position = screenPos
        };
        var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);
        return results.Count > 0;
    }
}
