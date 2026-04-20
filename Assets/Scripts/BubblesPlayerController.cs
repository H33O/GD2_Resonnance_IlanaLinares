using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Déplace le joueur case par case sur les colonnes de la grille du Minijeu-Bulles.
/// Les colonnes sont identiques à celles du Spawner (numberOfColumns × columnSpacing).
/// Supporte clavier (éditeur) et swipe tactile (mobile).
/// Le joueur ne peut pas sortir des colonnes définies.
/// </summary>
public class BubblesPlayerController : MonoBehaviour
{
    [Header("Grid Settings — doit correspondre au Spawner")]
    [SerializeField] private int   numberOfColumns = 3;
    [SerializeField] private float columnSpacing   = 2f;

    [Header("Movement Settings")]
    [SerializeField] private float moveDuration  = 0.10f;

    [Tooltip("Distance minimale en pixels écran pour valider un swipe.")]
    [SerializeField] private float minSwipePixels = 60f;

    // ── Vitesse publique (utilisée par PlayerVisuals / autres systèmes) ────────
    /// <summary>Vitesse horizontale courante du joueur en unités par seconde.</summary>
    public float HorizontalVelocity { get; private set; }

    // ── Colonnes ──────────────────────────────────────────────────────────────

    private float[] columnPositions;
    private int     currentColumn;

    // ── Mouvement interpolé ───────────────────────────────────────────────────

    private bool    isMoving;
    private Vector3 moveFrom;
    private Vector3 moveTo;
    private float   moveTimer;

    // ── Swipe ─────────────────────────────────────────────────────────────────

    private bool    isTouching;
    private Vector2 swipeStart;
    private bool    swipeConsumed;

    // ── Init ──────────────────────────────────────────────────────────────────

    private void Awake()
    {
        BuildColumns();
    }

    private void Start()
    {
        // Démarre sur la colonne centrale
        currentColumn      = numberOfColumns / 2;
        float y            = transform.position.y;
        transform.position = new Vector3(columnPositions[currentColumn], y, 0f);
        HorizontalVelocity = 0f;
    }

    private void BuildColumns()
    {
        columnPositions    = new float[numberOfColumns];
        float totalWidth   = (numberOfColumns - 1) * columnSpacing;
        float startX       = -totalWidth / 2f;
        for (int i = 0; i < numberOfColumns; i++)
            columnPositions[i] = startX + i * columnSpacing;
    }

    // ── Update ────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsGameActive)
            return;

        HandleKeyboard();
        HandleTouch();
        AdvanceMovement();
    }

    // ── Mouvement interpolé ───────────────────────────────────────────────────

    /// <summary>Démarre le déplacement d'une colonne vers la gauche (dir = -1) ou droite (dir = +1).</summary>
    private void StartMove(int dir)
    {
        if (isMoving) return;

        int next = currentColumn + dir;
        if (next < 0 || next >= numberOfColumns) return;   // hors limites : bloqué

        currentColumn = next;
        moveFrom      = transform.position;
        moveTo        = new Vector3(columnPositions[currentColumn], transform.position.y, 0f);
        isMoving      = true;
        moveTimer     = 0f;
    }

    private void AdvanceMovement()
    {
        float previousX = transform.position.x;

        if (!isMoving)
        {
            HorizontalVelocity = 0f;
            return;
        }

        moveTimer += Time.deltaTime;
        float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(moveTimer / moveDuration));
        transform.position = Vector3.Lerp(moveFrom, moveTo, t);

        HorizontalVelocity = (transform.position.x - previousX) / Time.deltaTime;

        if (moveTimer >= moveDuration)
        {
            transform.position = moveTo;
            isMoving           = false;
        }
    }

    // ── Input clavier ─────────────────────────────────────────────────────────

    private void HandleKeyboard()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.leftArrowKey.wasPressedThisFrame  || kb.aKey.wasPressedThisFrame) StartMove(-1);
        if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) StartMove(+1);
    }

    // ── Input tactile ─────────────────────────────────────────────────────────

    private void HandleTouch()
    {
        var touchscreen = Touchscreen.current;
        if (touchscreen == null) return;

        foreach (var touch in touchscreen.touches)
        {
            var phase = touch.phase.ReadValue();

            if (phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                swipeStart    = touch.position.ReadValue();
                isTouching    = true;
                swipeConsumed = false;
                continue;
            }

            if (!isTouching || swipeConsumed) continue;

            if (phase == UnityEngine.InputSystem.TouchPhase.Moved)
            {
                float deltaX = touch.position.ReadValue().x - swipeStart.x;
                if (Mathf.Abs(deltaX) >= minSwipePixels)
                {
                    swipeConsumed = true;
                    StartMove(deltaX > 0f ? +1 : -1);
                }
            }

            if (phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                phase == UnityEngine.InputSystem.TouchPhase.Canceled)
            {
                isTouching    = false;
                swipeConsumed = false;
            }
        }
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>Déplace le joueur d'une colonne vers la gauche.</summary>
    public void MoveLeft()  => StartMove(-1);

    /// <summary>Déplace le joueur d'une colonne vers la droite.</summary>
    public void MoveRight() => StartMove(+1);

    // ── Gizmos ────────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        if (columnPositions == null || columnPositions.Length == 0) BuildColumns();

        Gizmos.color = Color.cyan;
        foreach (float x in columnPositions)
            Gizmos.DrawWireCube(new Vector3(x, transform.position.y, 0f), new Vector3(0.8f, 0.8f, 0.1f));
    }
}
