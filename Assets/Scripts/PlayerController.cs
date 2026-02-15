using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Grid Movement Settings")]
    [SerializeField] private int numberOfPositions = 3;
    [SerializeField] private float positionSpacing = 2f;
    [SerializeField] private float playerY = -4f;
    [SerializeField] private float moveDuration = 0.15f;

    private int currentPosition = 1;
    private float[] positions;
    private bool isMoving = false;
    private Vector3 targetPosition;
    private float moveTimer;

    private Camera mainCamera;
    private Vector2 lastTouchPosition;

    private void Awake()
    {
        mainCamera = Camera.main;
        InitializePositions();
    }

    private void Start()
    {
        currentPosition = numberOfPositions / 2;
        transform.position = new Vector3(positions[currentPosition], playerY, 0f);
        targetPosition = transform.position;
    }

    private void InitializePositions()
    {
        positions = new float[numberOfPositions];
        float totalWidth = (numberOfPositions - 1) * positionSpacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < numberOfPositions; i++)
        {
            positions[i] = startX + (i * positionSpacing);
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsGameActive)
            return;

        if (isMoving)
        {
            moveTimer += Time.deltaTime;
            float t = Mathf.Clamp01(moveTimer / moveDuration);
            transform.position = Vector3.Lerp(transform.position, targetPosition, t);

            if (t >= 1f)
            {
                transform.position = targetPosition;
                isMoving = false;
            }
        }

        HandleKeyboardInput();
    }

    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            MoveLeft();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            MoveRight();
        }
    }

    public void OnTouch(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            lastTouchPosition = context.ReadValue<Vector2>();
            ProcessTouchInput();
        }
    }

    public void OnTouchPosition(InputAction.CallbackContext context)
    {
        lastTouchPosition = context.ReadValue<Vector2>();
    }

    private void ProcessTouchInput()
    {
        if (isMoving) return;

        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(lastTouchPosition.x, lastTouchPosition.y, 10f));
        
        if (worldPosition.x < positions[currentPosition] - positionSpacing / 2f)
        {
            MoveLeft();
        }
        else if (worldPosition.x > positions[currentPosition] + positionSpacing / 2f)
        {
            MoveRight();
        }
    }

    public void MoveLeft()
    {
        if (isMoving || currentPosition <= 0) return;

        currentPosition--;
        StartMove();
    }

    public void MoveRight()
    {
        if (isMoving || currentPosition >= numberOfPositions - 1) return;

        currentPosition++;
        StartMove();
    }

    private void StartMove()
    {
        targetPosition = new Vector3(positions[currentPosition], playerY, 0f);
        isMoving = true;
        moveTimer = 0f;
    }

    private void OnDrawGizmos()
    {
        if (positions == null || positions.Length == 0)
        {
            InitializePositions();
        }

        Gizmos.color = Color.green;
        foreach (float posX in positions)
        {
            Vector3 pos = new Vector3(posX, playerY, 0f);
            Gizmos.DrawWireCube(pos, new Vector3(0.8f, 0.8f, 0.1f));
        }

        Gizmos.color = Color.yellow;
        if (Application.isPlaying && currentPosition >= 0 && currentPosition < positions.Length)
        {
            Vector3 currentPos = new Vector3(positions[currentPosition], playerY, 0f);
            Gizmos.DrawWireSphere(currentPos, 0.5f);
        }
    }
}
