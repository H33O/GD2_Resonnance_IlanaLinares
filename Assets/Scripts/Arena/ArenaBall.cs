using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls the ball orbiting the arena, handles jump input, squash-stretch,
/// trail rendering, and collision detection against placed obstacles.
///
/// Attach to the "Ball" GameObject. Wire the TrailRenderer and the
/// ArenaSettings asset in the Inspector.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class ArenaBall : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Settings")]
    public ArenaSettings settings;

    [Header("References")]
    [Tooltip("TrailRenderer sitting on a child of this GameObject.")]
    public TrailRenderer trail;

    [Tooltip("Ripple effect spawner triggered on each tap.")]
    public ArenaRippleEffect ripple;

    [Header("Orbit")]
    [Tooltip("Starting angle in degrees (0 = right, 90 = top).")]
    public float startAngleDeg = 90f;

    // ── Runtime state ─────────────────────────────────────────────────────────

    private float angleDeg;
    private float currentOrbitRadius;
    private bool  isJumping;

    private SpriteRenderer spriteRenderer;
    private Vector3        baseScale;

    private float CurrentSpeed
    {
        get
        {
            float t = ArenaGameManager.Instance != null ? ArenaGameManager.Instance.SurvivalTime : 0f;
            return Mathf.Lerp(settings.ballSpeedStart, settings.ballSpeedMax,
                              Mathf.Clamp01(t / settings.speedRampDuration));
        }
    }

    private float OrbitRadius     => settings.orbitRadius;
    private float InnerJumpRadius => OrbitRadius * (1f - settings.jumpDepthRatio);

    // ── Input ─────────────────────────────────────────────────────────────────

    private InputAction tapAction;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        spriteRenderer     = GetComponent<SpriteRenderer>();
        baseScale          = transform.localScale;
        angleDeg           = startAngleDeg;
        currentOrbitRadius = OrbitRadius;

        // White circle sprite generated at runtime
        spriteRenderer.sprite         = CreateCircleSprite(64, Color.white);
        spriteRenderer.sharedMaterial = GetUnlitMaterial();
        spriteRenderer.color          = Color.white;

        // Use press (not tap) so it fires immediately on mouse down / touch begin
        tapAction = new InputAction("Tap", InputActionType.Button);
        tapAction.AddBinding("<Mouse>/leftButton");
        tapAction.AddBinding("<Touchscreen>/primaryTouch/press");
        tapAction.Enable();
        tapAction.performed += _ => tapQueued = true;
    }

    private void OnDestroy()
    {
        tapAction?.Disable();
        tapAction?.Dispose();
    }

    private bool tapQueued;

    private void Update()
    {
        bool isPlaying = ArenaGameManager.Instance == null
                      || ArenaGameManager.Instance.State == ArenaGameManager.GameState.Playing;

        if (!isPlaying) return;

        if (!isJumping)
            angleDeg += CurrentSpeed * Time.deltaTime;

        ApplyPosition();

        if (tapQueued && !isJumping)
        {
            tapQueued = false;
            StartCoroutine(JumpRoutine());
        }
        else
        {
            tapQueued = false; // discard tap if already jumping
        }
    }

    // ── Orbit position ────────────────────────────────────────────────────────

    private void ApplyPosition()
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        transform.localPosition = new Vector3(
            Mathf.Cos(rad) * currentOrbitRadius,
            Mathf.Sin(rad) * currentOrbitRadius,
            0f);
    }

    /// <summary>Current angle on the orbit in degrees. Read by obstacle detectors.</summary>
    public float AngleDeg => angleDeg;

    /// <summary>Current distance from arena centre. Used for collision logic.</summary>
    public float CurrentRadius => currentOrbitRadius;

    /// <summary>True while the ball is in the middle of a jump.</summary>
    public bool IsJumping => isJumping;

    // ── Jump ──────────────────────────────────────────────────────────────────

    private IEnumerator JumpRoutine()
    {
        isJumping = true;

        ripple?.SpawnAt(transform.position);
        yield return StartCoroutine(SquashLerp(settings.squashX, settings.squashY, settings.squashDuration));

        // Inward phase
        float startR = currentOrbitRadius;
        float innerR = InnerJumpRadius;
        float elapsed = 0f;
        while (elapsed < settings.jumpDurationIn)
        {
            elapsed           += Time.deltaTime;
            float t            = Mathf.SmoothStep(0f, 1f, elapsed / settings.jumpDurationIn);
            currentOrbitRadius = Mathf.Lerp(startR, innerR, t);
            angleDeg          += CurrentSpeed * Time.deltaTime;
            ApplyPosition();
            yield return null;
        }
        currentOrbitRadius = innerR;

        yield return StartCoroutine(SquashLerp(settings.squashY, settings.squashX, settings.squashDuration));

        // Outward phase
        elapsed = 0f;
        while (elapsed < settings.jumpDurationOut)
        {
            elapsed           += Time.deltaTime;
            float t            = Mathf.SmoothStep(0f, 1f, elapsed / settings.jumpDurationOut);
            currentOrbitRadius = Mathf.Lerp(innerR, OrbitRadius, t);
            angleDeg          += CurrentSpeed * Time.deltaTime;
            ApplyPosition();
            yield return null;
        }
        currentOrbitRadius = OrbitRadius;

        yield return StartCoroutine(SquashLerp(1f, 1f, settings.squashDuration));
        isJumping = false;
    }

    // ── Squash & Stretch ──────────────────────────────────────────────────────

    private IEnumerator SquashLerp(float targetX, float targetY, float duration)
    {
        Vector3 startScale = transform.localScale;
        Vector3 endScale   = new Vector3(
            baseScale.x * targetX,
            baseScale.y * targetY,
            baseScale.z);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed             += Time.deltaTime;
            float t              = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }
        transform.localScale = endScale;
    }

    // ── Collision (trigger-based) ─────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Obstacle"))
            ArenaGameManager.Instance?.NotifyCollision();
    }

    // ── Sprite generation ─────────────────────────────────────────────────────

    private static Material cachedBallMat;

    private static Material GetUnlitMaterial()
    {
        if (cachedBallMat == null)
            cachedBallMat = new Material(
                Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
        return cachedBallMat;
    }

    /// <summary>Generates a filled circle sprite at runtime (resolution×resolution px).</summary>
    private static Sprite CreateCircleSprite(int resolution, Color color)
    {
        var tex    = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        float half = resolution * 0.5f;
        float r2   = half * half;

        for (int y = 0; y < resolution; y++)
        for (int x = 0; x < resolution; x++)
        {
            float dx = x - half + 0.5f;
            float dy = y - half + 0.5f;
            tex.SetPixel(x, y, (dx * dx + dy * dy) <= r2 ? color : Color.clear);
        }
        tex.Apply();

        return Sprite.Create(
            tex,
            new Rect(0, 0, resolution, resolution),
            new Vector2(0.5f, 0.5f),
            resolution);   // PPU = resolution → 1 unit in world space
    }
}
