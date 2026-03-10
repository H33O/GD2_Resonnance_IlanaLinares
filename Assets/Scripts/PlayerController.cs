using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Grid Movement Settings")]
    [SerializeField] private int   numberOfPositions = 3;
    [SerializeField] private float positionSpacing   = 2f;
    [SerializeField] private float playerY           = -4f;

    [Tooltip("Durée de transition entre deux cases (secondes).")]
    [SerializeField] private float moveDuration      = 0.08f;

    [Header("Swipe Settings")]
    [Tooltip("Distance minimale en pixels écran pour valider un swipe.")]
    [SerializeField] private float minSwipePixels    = 80f;

    // ── Colonnes ──────────────────────────────────────────────────────────────

    private int     currentPosition;
    private float[] positions;

    // ── Mouvement ─────────────────────────────────────────────────────────────

    private bool    isMoving;
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float   moveTimer;

    // ── Swipe ─────────────────────────────────────────────────────────────────

    private bool    isDragging;
    private Vector2 dragStart;
    private bool    swipeConsumed;   // un seul move par appui, quoi qu'il arrive

    // ── Gizmos ────────────────────────────────────────────────────────────────

    private Vector2 lastDragStart;
    private Vector2 lastDragEnd;
    private bool    hasLastSwipe;
    private float   lastDeltaX;

    private Camera mainCamera;

    // ── Init ──────────────────────────────────────────────────────────────────

    private void Awake()
    {
        mainCamera = Camera.main;
        InitializePositions();
    }

    private void Start()
    {
        currentPosition    = numberOfPositions / 2;
        transform.position = new Vector3(positions[currentPosition], playerY, 0f);
        targetPosition     = transform.position;
        startPosition      = transform.position;
    }

    private void InitializePositions()
    {
        positions = new float[numberOfPositions];
        float totalWidth = (numberOfPositions - 1) * positionSpacing;
        float startX     = -totalWidth / 2f;
        for (int i = 0; i < numberOfPositions; i++)
            positions[i] = startX + i * positionSpacing;
    }

    // ── Update ────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsGameActive) return;

        HandleTouchInput();
        HandleMouseInput();
        HandleKeyboardInput();
        AdvanceMovement();
    }

    // ── Mouvement smooth case par case ────────────────────────────────────────

    private void AdvanceMovement()
    {
        if (!isMoving) return;

        moveTimer += Time.deltaTime;
        float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(moveTimer / moveDuration));
        transform.position = Vector3.LerpUnclamped(startPosition, targetPosition, t);

        if (moveTimer >= moveDuration)
        {
            transform.position = targetPosition;
            isMoving           = false;
        }
    }

    private void StartMove(int dir)
    {
        if (isMoving) return;

        int next = currentPosition + dir;
        if (next < 0 || next >= numberOfPositions) return;

        currentPosition = next;
        startPosition   = transform.position;
        targetPosition  = new Vector3(positions[currentPosition], playerY, 0f);
        isMoving        = true;
        moveTimer       = 0f;
    }

    // ── Input souris ──────────────────────────────────────────────────────────

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isDragging    = true;
            swipeConsumed = false;
            dragStart     = Input.mousePosition;
            lastDragStart = dragStart;
            hasLastSwipe  = false;
        }

        // Détection pendant le drag — un seul move par appui grâce à swipeConsumed
        if (isDragging && !swipeConsumed && Input.GetMouseButton(0))
        {
            float deltaX = ((Vector2)Input.mousePosition).x - dragStart.x;

            if (Mathf.Abs(deltaX) >= minSwipePixels)
            {
                lastDeltaX    = deltaX;
                lastDragEnd   = Input.mousePosition;
                hasLastSwipe  = true;
                swipeConsumed = true;   // bloque tout autre move jusqu'au prochain appui

                if (deltaX > 0) StartMove(+1);
                else            StartMove(-1);
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging    = false;
            swipeConsumed = false;
        }
    }

    // ── Input tactile ─────────────────────────────────────────────────────────

    private void HandleTouchInput()
    {
        if (Input.touchCount == 0) return;
        Touch touch = Input.GetTouch(0);

        switch (touch.phase)
        {
            case TouchPhase.Began:
                isDragging    = true;
                swipeConsumed = false;
                dragStart     = touch.position;
                lastDragStart = dragStart;
                hasLastSwipe  = false;
                break;

            case TouchPhase.Moved:
                if (!isDragging || swipeConsumed) break;
                float deltaX = touch.position.x - dragStart.x;

                if (Mathf.Abs(deltaX) >= minSwipePixels)
                {
                    lastDeltaX    = deltaX;
                    lastDragEnd   = touch.position;
                    hasLastSwipe  = true;
                    swipeConsumed = true;

                    if (deltaX > 0) StartMove(+1);
                    else            StartMove(-1);
                }
                break;

            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                isDragging    = false;
                swipeConsumed = false;
                break;
        }
    }

    // ── Input clavier ─────────────────────────────────────────────────────────

    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow)  || Input.GetKeyDown(KeyCode.A)) StartMove(-1);
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) StartMove(+1);
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>Déplace le joueur d'une case vers la gauche.</summary>
    public void MoveLeft()  => StartMove(-1);

    /// <summary>Déplace le joueur d'une case vers la droite.</summary>
    public void MoveRight() => StartMove(+1);

    // ── Gizmos ────────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        if (positions == null || positions.Length == 0) InitializePositions();

        Gizmos.color = Color.green;
        foreach (float posX in positions)
            Gizmos.DrawWireCube(new Vector3(posX, playerY, 0f), new Vector3(0.8f, 0.8f, 0.1f));

        Gizmos.color = Color.yellow;
        if (Application.isPlaying && currentPosition >= 0 && currentPosition < positions.Length)
            Gizmos.DrawWireSphere(new Vector3(positions[currentPosition], playerY, 0f), 0.5f);

        if (hasLastSwipe && Application.isPlaying)
        {
            if (mainCamera == null) mainCamera = Camera.main;
            Vector3 s = mainCamera.ScreenToWorldPoint(new Vector3(lastDragStart.x, lastDragStart.y, 10f));
            Vector3 e = mainCamera.ScreenToWorldPoint(new Vector3(lastDragEnd.x,   lastDragEnd.y,   10f));
            s.z = e.z = 0f;

            Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
            Gizmos.DrawSphere(s, 0.18f);
            Gizmos.color = lastDeltaX > 0 ? Color.green : Color.red;
            Gizmos.DrawLine(s, e);
        }
    }
}
