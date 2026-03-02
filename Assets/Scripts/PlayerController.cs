using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Grid Movement Settings")]
    [SerializeField] private int   numberOfPositions = 3;
    [SerializeField] private float positionSpacing   = 2f;
    [SerializeField] private float playerY           = -4f;
    [SerializeField] private float moveDuration      = 0.15f;

    [Header("Swipe Settings")]
    [Tooltip("Distance minimale en unités monde entre entry et exit pour valider un swipe.")]
    [SerializeField] private float minSwipeDistance = 0.25f;

    private int     currentPosition = 1;
    private float[] positions;
    private bool    isMoving = false;
    private Vector3 targetPosition;
    private float   moveTimer;

    private Camera    mainCamera;
    private Transform swipeEntryPoint;
    private Transform swipeExitPoint;

    // Persistance gizmos
    private Vector3 lastEntryWorld;
    private Vector3 lastExitWorld;
    private float   lastDeltaX;
    private bool    hasLastSwipe = false;

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
        Debug.Log("[PlayerController] Start — input souris uniquement.");
    }

    private void InitializePositions()
    {
        positions = new float[numberOfPositions];
        float totalWidth = (numberOfPositions - 1) * positionSpacing;
        float startX     = -totalWidth / 2f;
        for (int i = 0; i < numberOfPositions; i++)
            positions[i] = startX + i * positionSpacing;
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
                isMoving           = false;
            }
        }

        HandleMouseInput();
        HandleKeyboardInput();
    }

    /// <summary>
    /// Swipe souris : bouton gauche enfoncé = entry point, bouton gauche relâché = exit point.
    /// Compare les X des deux transforms et se déplace gauche ou droite.
    /// </summary>
    private void HandleMouseInput()
    {
        // ── Bouton enfoncé → point d'entrée ───────────────────────────────────
        if (Input.GetMouseButtonDown(0))
        {
            ClearSwipePoints();

            Vector3 entryWorld = ScreenToWorld(Input.mousePosition);
            lastEntryWorld = entryWorld;
            hasLastSwipe   = false;

            var go = new GameObject("SwipeEntryPoint");
            go.transform.position = entryWorld;
            swipeEntryPoint       = go.transform;

            Debug.Log($"[PlayerController] ▶ MOUSE DOWN | screen {(Vector2)Input.mousePosition} → world {entryWorld}");
        }

        // ── Bouton relâché → point de sortie + évaluation ────────────────────
        if (Input.GetMouseButtonUp(0))
        {
            if (swipeEntryPoint == null)
            {
                Debug.LogWarning("[PlayerController] MOUSE UP sans EntryPoint.");
                return;
            }

            Vector3 exitWorld = ScreenToWorld(Input.mousePosition);
            lastExitWorld = exitWorld;

            var go = new GameObject("SwipeExitPoint");
            go.transform.position = exitWorld;
            swipeExitPoint        = go.transform;

            Debug.Log($"[PlayerController] ■ MOUSE UP  | screen {(Vector2)Input.mousePosition} → world {exitWorld}");

            EvaluateSwipe();
            ClearSwipePoints();
        }
    }

    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow)  || Input.GetKeyDown(KeyCode.A)) MoveLeft();
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) MoveRight();
    }

    /// <summary>Compare les X des deux points et décide gauche / droite.</summary>
    private void EvaluateSwipe()
    {
        if (swipeEntryPoint == null || swipeExitPoint == null) return;

        float deltaX = swipeExitPoint.position.x - swipeEntryPoint.position.x;
        lastDeltaX   = deltaX;
        hasLastSwipe = true;

        Debug.Log($"[PlayerController] EvaluateSwipe | entry.x={swipeEntryPoint.position.x:F3}  " +
                  $"exit.x={swipeExitPoint.position.x:F3}  deltaX={deltaX:F3}  seuil=±{minSwipeDistance}");

        if (deltaX > minSwipeDistance)
        {
            Debug.Log("[PlayerController] → SWIPE DROITE");
            MoveRight();
        }
        else if (deltaX < -minSwipeDistance)
        {
            Debug.Log("[PlayerController] → SWIPE GAUCHE");
            MoveLeft();
        }
        else
        {
            Debug.Log($"[PlayerController] → Ignoré : |deltaX|={Mathf.Abs(deltaX):F3} < {minSwipeDistance}");
        }
    }

    private Vector3 ScreenToWorld(Vector3 screenPos)
    {
        Vector3 world = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
        return new Vector3(world.x, world.y, 0f);
    }

    private void ClearSwipePoints()
    {
        if (swipeEntryPoint != null) { Destroy(swipeEntryPoint.gameObject); swipeEntryPoint = null; }
        if (swipeExitPoint  != null) { Destroy(swipeExitPoint.gameObject);  swipeExitPoint  = null; }
    }

    /// <summary>Déplace le joueur vers la gauche.</summary>
    public void MoveLeft()
    {
        if (isMoving)             { Debug.Log("[PlayerController] MoveLeft bloqué — en mouvement"); return; }
        if (currentPosition <= 0) { Debug.Log("[PlayerController] MoveLeft bloqué — bord gauche");  return; }
        currentPosition--;
        Debug.Log($"[PlayerController] MoveLeft ✓ → colonne {currentPosition}");
        StartMove();
    }

    /// <summary>Déplace le joueur vers la droite.</summary>
    public void MoveRight()
    {
        if (isMoving)                                 { Debug.Log("[PlayerController] MoveRight bloqué — en mouvement"); return; }
        if (currentPosition >= numberOfPositions - 1) { Debug.Log("[PlayerController] MoveRight bloqué — bord droit");  return; }
        currentPosition++;
        Debug.Log($"[PlayerController] MoveRight ✓ → colonne {currentPosition}");
        StartMove();
    }

    private void StartMove()
    {
        targetPosition = new Vector3(positions[currentPosition], playerY, 0f);
        isMoving       = true;
        moveTimer      = 0f;
    }

    private void OnDrawGizmos()
    {
        if (positions == null || positions.Length == 0) InitializePositions();

        // Colonnes de grille
        Gizmos.color = Color.green;
        foreach (float posX in positions)
            Gizmos.DrawWireCube(new Vector3(posX, playerY, 0f), new Vector3(0.8f, 0.8f, 0.1f));

        // Colonne courante
        Gizmos.color = Color.yellow;
        if (Application.isPlaying && currentPosition >= 0 && currentPosition < positions.Length)
            Gizmos.DrawWireSphere(new Vector3(positions[currentPosition], playerY, 0f), 0.5f);

        // Points live
        if (swipeEntryPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(swipeEntryPoint.position, 0.18f);
        }
        if (swipeExitPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(swipeExitPoint.position, 0.18f);
        }

        // Dernier swipe persistant
        if (hasLastSwipe && Application.isPlaying)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
            Gizmos.DrawSphere(lastEntryWorld, 0.22f);

            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.4f);
            Gizmos.DrawSphere(lastExitWorld, 0.22f);

            Gizmos.color = lastDeltaX > 0 ? Color.green : Color.red;
            Gizmos.DrawLine(lastEntryWorld, lastExitWorld);

            // Seuil minimum autour du point d'entrée
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(lastEntryWorld, minSwipeDistance);
        }
    }
}


