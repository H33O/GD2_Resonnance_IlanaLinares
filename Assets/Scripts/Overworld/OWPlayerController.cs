using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Contrôleur du joueur pour l'overworld 2D vertical :
/// déplacement horizontal libre + saut avec physique Rigidbody2D.
/// Supporte clavier, souris (swipe) et tactile.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class OWPlayerController : MonoBehaviour
{
    // ── Paramètres de mouvement ───────────────────────────────────────────────

    [Header("Horizontal Movement")]
    [SerializeField] private float moveSpeed       = 5f;
    [SerializeField] private float accelerationTime = 0.08f;

    [Header("Jump")]
    [SerializeField] private float jumpForce         = 12f;
    [SerializeField] private float fallMultiplier     = 2.5f;
    [SerializeField] private float lowJumpMultiplier  = 2.0f;
    [SerializeField] private float coyoteTime         = 0.12f;
    [SerializeField] private float jumpBufferTime     = 0.15f;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float     groundCheckDistance = 0.08f;

    [Header("Mobile Swipe")]
    [SerializeField] private float minSwipePixels = 60f;

    // ── Références ────────────────────────────────────────────────────────────

    private Rigidbody2D   rb;
    private CapsuleCollider2D col;

    // ── État interne ──────────────────────────────────────────────────────────

    private float horizontalInput;
    private float currentVelocityX;
    private bool  isGrounded;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private bool  jumpHeld;

    // ── Swipe / Drag (mobile + souris) ───────────────────────────────────────

    private bool    isDragging;
    private Vector2 dragStart;
    private bool    swipeJumpConsumed;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        rb  = GetComponent<Rigidbody2D>();
        col = GetComponent<CapsuleCollider2D>();

        rb.gravityScale  = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void Update()
    {
        CheckGround();
        GatherInput();
        HandleJumpBuffer();
        HandleCoyoteTime();
    }

    private void FixedUpdate()
    {
        ApplyHorizontalMovement();
        ApplyFallMultiplier();
    }

    // ── Détection du sol ─────────────────────────────────────────────────────

    private void CheckGround()
    {
        Vector2 origin = (Vector2)transform.position + col.offset + Vector2.down * (col.size.y * 0.5f);
        isGrounded     = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer);

        if (isGrounded) coyoteTimer = coyoteTime;
    }

    // ── Collecte des inputs ───────────────────────────────────────────────────

    private void GatherInput()
    {
        // Clavier
        float keyboardX = 0f;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.leftArrowKey.isPressed  || Keyboard.current.aKey.isPressed)  keyboardX -= 1f;
            if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed)  keyboardX += 1f;

            if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame ||
                Keyboard.current.wKey.wasPressedThisFrame)
                jumpBufferTimer = jumpBufferTime;

            jumpHeld = Keyboard.current.spaceKey.isPressed ||
                       Keyboard.current.upArrowKey.isPressed ||
                       Keyboard.current.wKey.isPressed;
        }

        horizontalInput = keyboardX;

        // Souris
        HandleMouseSwipe();

        // Tactile
        HandleTouchInput();
    }

    private void HandleMouseSwipe()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            isDragging        = true;
            swipeJumpConsumed = false;
            dragStart         = Mouse.current.position.ReadValue();
        }

        if (isDragging && Mouse.current.leftButton.isPressed)
        {
            Vector2 current = Mouse.current.position.ReadValue();
            Vector2 delta   = current - dragStart;

            // Mouvement horizontal en continu pendant le drag
            if (Mathf.Abs(delta.x) > 10f)
                horizontalInput = Mathf.Sign(delta.x);

            // Swipe vers le haut → saut (une seule fois par appui)
            if (!swipeJumpConsumed && delta.y > minSwipePixels)
            {
                swipeJumpConsumed = true;
                jumpBufferTimer   = jumpBufferTime;
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isDragging        = false;
            swipeJumpConsumed = false;
        }
    }

    private void HandleTouchInput()
    {
        if (Touchscreen.current == null || Touchscreen.current.touches.Count == 0) return;

        var touch = Touchscreen.current.touches[0];

        if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
        {
            isDragging        = true;
            swipeJumpConsumed = false;
            dragStart         = touch.position.ReadValue();
        }

        if (isDragging && (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Moved ||
                           touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Stationary))
        {
            Vector2 current = touch.position.ReadValue();
            Vector2 delta   = current - dragStart;

            if (Mathf.Abs(delta.x) > 10f)
                horizontalInput = Mathf.Sign(delta.x);

            if (!swipeJumpConsumed && delta.y > minSwipePixels)
            {
                swipeJumpConsumed = true;
                jumpBufferTimer   = jumpBufferTime;
            }
        }

        if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Ended ||
            touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled)
        {
            isDragging        = false;
            swipeJumpConsumed = false;
        }
    }

    // ── Logique de saut ───────────────────────────────────────────────────────

    private void HandleJumpBuffer()
    {
        if (jumpBufferTimer > 0f)
        {
            jumpBufferTimer -= Time.deltaTime;
            if (CanJump())
                ExecuteJump();
        }
    }

    private void HandleCoyoteTime()
    {
        if (!isGrounded)
            coyoteTimer -= Time.deltaTime;
    }

    private bool CanJump() => coyoteTimer > 0f;

    private void ExecuteJump()
    {
        rb.linearVelocity   = new Vector2(rb.linearVelocity.x, jumpForce);
        coyoteTimer         = 0f;
        jumpBufferTimer     = 0f;
    }

    // ── Mouvement horizontal ──────────────────────────────────────────────────

    private void ApplyHorizontalMovement()
    {
        float targetVX = horizontalInput * moveSpeed;
        currentVelocityX = Mathf.Lerp(currentVelocityX, targetVX, 1f - Mathf.Exp(-Time.fixedDeltaTime / accelerationTime));
        rb.linearVelocity = new Vector2(currentVelocityX, rb.linearVelocity.y);
    }

    // ── Multiplicateur de chute ───────────────────────────────────────────────

    private void ApplyFallMultiplier()
    {
        if (rb.linearVelocity.y < 0f)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0f && !jumpHeld)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1f) * Time.fixedDeltaTime;
        }
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        if (col == null) col = GetComponent<CapsuleCollider2D>();
        Vector2 origin = (Vector2)transform.position + col.offset + Vector2.down * (col.size.y * 0.5f);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(origin, origin + Vector2.down * groundCheckDistance);
    }
}
