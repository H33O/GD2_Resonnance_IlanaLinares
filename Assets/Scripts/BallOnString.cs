using UnityEngine;

/// <summary>
/// Boule attachée au joueur par un fil simulant un pendule.
/// Maintenir Espace recule la boule vers le bas ; relâcher la lance vers le haut.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class BallOnString : MonoBehaviour
{
    private enum BallState { Idle, Charging, Launched }

    [Header("Fil")]
    [SerializeField] private float stringLength = 2.5f;
    [SerializeField] private float lineWidth = 0.07f;
    [SerializeField] private Color lineColor = Color.white;

    [Header("Pendule")]
    [SerializeField] private float pendulumGravity = 20f;
    [SerializeField] private float pendulumDamping = 0.98f;
    [SerializeField] private float playerInfluence = 3f;

    [Header("Tir")]
    [SerializeField] private float pullbackOffsetY = -2f;
    [SerializeField] private float pullbackOffsetX = 0.8f;
    [SerializeField] private float pullbackSpeed = 7f;
    [SerializeField] private float maxChargeTime = 1.5f;
    [SerializeField] private float minLaunchForce = 8f;
    [SerializeField] private float maxLaunchForce = 22f;

    [Header("Réinitialisation")]
    [SerializeField] private float resetDelay = 4f;
    [SerializeField] private float offScreenMargin = 1.5f;

    private Rigidbody2D ballRigidbody;
    private CircleCollider2D ballCollider;
    private LineRenderer lineRenderer;
    private BubblesPlayerController playerController;
    private Transform playerTransform;

    private BallState currentState = BallState.Idle;
    private float pendulumAngle;
    private float angularVelocity;
    private float chargeTimer;
    private float resetTimer;
    private float previousPlayerX;
    private float pullbackSideSign;

    private void Awake()
    {
        ballRigidbody = GetComponent<Rigidbody2D>();
        ballCollider = GetComponent<CircleCollider2D>();
        SetupLineRenderer();
        SetupVisual();
    }

    private void Start()
    {
        playerController = FindFirstObjectByType<BubblesPlayerController>();
        if (playerController != null)
        {
            playerTransform = playerController.transform;
            previousPlayerX = playerTransform.position.x;
        }

        ballRigidbody.bodyType = RigidbodyType2D.Kinematic;
        ballCollider.isTrigger = false;
        ResetToIdle();
    }

    private void SetupLineRenderer()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.useWorldSpace = true;
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
        lineRenderer.sortingLayerName = "Default";
        lineRenderer.sortingOrder = 1;
    }

    private void SetupVisual()
    {
        if (!TryGetComponent<SpriteRenderer>(out _))
        {
            SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteGenerator.Circle();
            sr.color = new Color(1f, 0.55f, 0.1f);
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 2;
        }
    }

    private void Update()
    {
        switch (currentState)
        {
            case BallState.Idle:
                SimulatePendulum();
                if (Input.GetKeyDown(KeyCode.Space)) BeginCharge();
                break;

            case BallState.Charging:
                chargeTimer += Time.deltaTime;
                UpdatePullback();
                if (Input.GetKeyUp(KeyCode.Space)) Launch();
                break;

            case BallState.Launched:
                resetTimer += Time.deltaTime;
                if (resetTimer >= resetDelay || IsOffScreen()) ResetToIdle();
                break;
        }

        UpdateStringVisual();
    }

    private void SimulatePendulum()
    {
        if (playerTransform == null) return;

        float deltaX = playerTransform.position.x - previousPlayerX;
        previousPlayerX = playerTransform.position.x;

        float angularAcceleration = -(pendulumGravity / stringLength) * Mathf.Sin(pendulumAngle);
        angularVelocity += angularAcceleration * Time.deltaTime;
        angularVelocity -= deltaX * playerInfluence;
        angularVelocity *= Mathf.Pow(pendulumDamping, Time.deltaTime * 60f);

        pendulumAngle += angularVelocity * Time.deltaTime;
        pendulumAngle = Mathf.Clamp(pendulumAngle, -Mathf.PI * 0.6f, Mathf.PI * 0.6f);

        PositionBallAtAngle(pendulumAngle);
    }

    private void BeginCharge()
    {
        currentState = BallState.Charging;
        chargeTimer = 0f;
        pullbackSideSign = (playerController != null && playerController.HorizontalVelocity > 0f) ? -1f : 1f;
    }

    private void UpdatePullback()
    {
        if (playerTransform == null) return;

        Vector3 pullbackTarget = playerTransform.position
            + new Vector3(pullbackSideSign * pullbackOffsetX, pullbackOffsetY, 0f);

        transform.position = Vector3.Lerp(transform.position, pullbackTarget, pullbackSpeed * Time.deltaTime);
    }

    /// <summary>Lance la boule vers le haut avec une force proportionnelle au temps de charge.</summary>
    private void Launch()
    {
        currentState = BallState.Launched;
        resetTimer = 0f;

        ballRigidbody.bodyType = RigidbodyType2D.Dynamic;
        ballRigidbody.linearVelocity = Vector2.zero;

        Vector2 launchDir = new Vector2(-pullbackSideSign * 0.25f, 1f).normalized;
        float chargeRatio = Mathf.Clamp01(chargeTimer / maxChargeTime);
        float force = Mathf.Lerp(minLaunchForce, maxLaunchForce, chargeRatio);

        ballRigidbody.AddForce(launchDir * force, ForceMode2D.Impulse);
    }

    /// <summary>Remet la boule en position de départ sous le joueur.</summary>
    private void ResetToIdle()
    {
        currentState = BallState.Idle;
        chargeTimer = 0f;
        resetTimer = 0f;
        pendulumAngle = 0f;
        angularVelocity = 0f;

        ballRigidbody.bodyType = RigidbodyType2D.Kinematic;
        ballRigidbody.linearVelocity = Vector2.zero;
        ballRigidbody.angularVelocity = 0f;
        lineRenderer.enabled = true;

        if (playerTransform != null)
        {
            previousPlayerX = playerTransform.position.x;
            PositionBallAtAngle(0f);
        }
    }

    private void PositionBallAtAngle(float angle)
    {
        if (playerTransform == null) return;
        Vector2 anchor = playerTransform.position;
        transform.position = new Vector3(
            anchor.x + Mathf.Sin(angle) * stringLength,
            anchor.y - Mathf.Cos(angle) * stringLength,
            0f
        );
    }

    private void UpdateStringVisual()
    {
        if (currentState == BallState.Launched)
        {
            lineRenderer.enabled = false;
            return;
        }

        lineRenderer.enabled = true;
        if (playerTransform != null)
        {
            lineRenderer.SetPosition(0, playerTransform.position);
            lineRenderer.SetPosition(1, transform.position);
        }
    }

    private bool IsOffScreen()
    {
        Camera cam = Camera.main;
        if (cam == null) return false;
        Vector3 vp = cam.WorldToViewportPoint(transform.position);
        return vp.x < -offScreenMargin || vp.x > 1f + offScreenMargin
            || vp.y < -offScreenMargin || vp.y > 1f + offScreenMargin;
    }
}
