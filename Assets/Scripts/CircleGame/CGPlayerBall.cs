using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls the player ball that moves automatically around the circle.
/// A tap makes the ball jump inward, then return.
/// Colliding with an obstacle triggers game over.
/// Visuels : cercle blanc lumineux avec éclairs crépitants (DA menu).
/// Attach to the <b>PlayerBall</b> GameObject.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class CGPlayerBall : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Settings")]
    public CGSettings settings;

    [Header("References")]
    [Tooltip("The CameraController used to trigger ripple feedback on tap.")]
    public CGCameraController cameraController;

    [Tooltip("The TrailRenderer child used for motion trail.")]
    public TrailRenderer trailRenderer;

    [Tooltip("The FeedbackManager that handles ripple and flash effects.")]
    public CGFeedbackManager feedbackManager;

    // ── State ─────────────────────────────────────────────────────────────────

    /// <summary>Current angle on the circle in degrees (0 = right, CCW positive).</summary>
    public float AngleDeg { get; private set; } = 90f;

    /// <summary>Current radial distance from circle centre.</summary>
    public float CurrentRadius { get; private set; }

    private bool       isJumping;
    private float      survivalTime;
    private SpriteRenderer sr;
    private SpriteRenderer glowSR;
    private Vector3    baseScale;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    // Diamètre du joueur en unités monde (indépendant du localScale parent)
    private const float PlayerDiameter = 0.38f;

    private void Awake()
    {
        sr              = GetComponent<SpriteRenderer>();
        sr.sprite       = SpriteGenerator.CreateCircle(128);
        sr.color        = Color.white;
        sr.sortingOrder = 10;

        // Force une taille fixe lisible sans écraser les animations de squash/stretch
        transform.localScale = Vector3.one * PlayerDiameter;
        baseScale            = transform.localScale;

        CurrentRadius = settings != null ? settings.arenaRadius : 4f;
        PlaceOnRing();

        BuildGlow();

        if (GetComponent<PlayerVisuals>() == null)
            gameObject.AddComponent<PlayerVisuals>();
    }

    private void BuildGlow()
    {
        var glowGO              = new GameObject("BallGlow");
        glowGO.transform.SetParent(transform, false);
        glowGO.transform.localPosition = Vector3.zero;
        glowGO.transform.localScale    = Vector3.one * 2.0f;

        glowSR              = glowGO.AddComponent<SpriteRenderer>();
        glowSR.sprite       = SpriteGenerator.CreateCircle(128);
        glowSR.color        = new Color(1f, 1f, 1f, 0.18f);
        glowSR.sortingOrder = 9;
    }

    private void OnEnable()
    {
        CGGameManager.OnGameOver += HandleGameOver;
    }

    private void OnDisable()
    {
        CGGameManager.OnGameOver -= HandleGameOver;
    }

    private void Update()
    {
        if (CGGameManager.Instance == null) return;
        if (CGGameManager.Instance.State != CGGameManager.GameState.Playing) return;

        // Pulsation glow — reuse cached color to avoid per-frame heap allocation
        if (glowSR != null)
        {
            Color c = glowSR.color;
            c.a          = 0.15f + 0.08f * Mathf.Sin(Time.time * 3.0f);
            glowSR.color = c;
        }

        // Advance survival time and increase speed progressively
        survivalTime += Time.deltaTime;

        float t     = Mathf.Clamp01(survivalTime / settings.speedRampDuration);
        float speed = Mathf.Lerp(settings.playerSpeedStart, settings.playerSpeedMax, t);

        AngleDeg += speed * Time.deltaTime;
        AngleDeg  = AngleDeg % 360f;

        PlaceOnRing();
        HandleInput();

        // Passive score: 1 point per second of survival
        CGGameManager.Instance?.AddScore(Mathf.FloorToInt(Time.deltaTime));
    }

    // ── Positioning ───────────────────────────────────────────────────────────

    private void PlaceOnRing()
    {
        float rad  = AngleDeg * Mathf.Deg2Rad;
        transform.localPosition = new Vector3(
            Mathf.Cos(rad) * CurrentRadius,
            Mathf.Sin(rad) * CurrentRadius,
            0f);
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    private void HandleInput()
    {
        if (isJumping) return;

        bool tapped = false;

#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            tapped = true;
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            tapped = true;
#else
        tapped = Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
#endif

        if (tapped)
        {
            feedbackManager?.TriggerRipple(transform.position);
            StartCoroutine(JumpRoutine());
        }
    }

    // ── Jump ──────────────────────────────────────────────────────────────────

    private IEnumerator JumpRoutine()
    {
        isJumping = true;
        float orbitR = settings.arenaRadius;
        float innerR = orbitR * (1f - settings.jumpDistance);

        // Squash on jump start
        yield return StartCoroutine(ScaleTo(
            new Vector3(baseScale.x * settings.squashX,
                        baseScale.y * settings.squashY,
                        baseScale.z),
            settings.squashDuration));

        // Inward phase
        yield return StartCoroutine(LerpRadius(orbitR, innerR, settings.jumpDurationIn));

        // Restore normal scale
        yield return StartCoroutine(ScaleTo(baseScale, settings.squashDuration));

        // Outward phase — slight stretch on return
        yield return StartCoroutine(ScaleTo(
            new Vector3(baseScale.x * settings.squashY,
                        baseScale.y * settings.squashX,
                        baseScale.z),
            settings.squashDuration));

        yield return StartCoroutine(LerpRadius(innerR, orbitR, settings.jumpDurationOut));

        yield return StartCoroutine(ScaleTo(baseScale, settings.squashDuration));

        isJumping = false;
    }

    private IEnumerator LerpRadius(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed      += Time.deltaTime;
            float t       = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            CurrentRadius = Mathf.Lerp(from, to, t);
            PlaceOnRing();
            yield return null;
        }
        CurrentRadius = to;
        PlaceOnRing();
    }

    private IEnumerator ScaleTo(Vector3 target, float duration)
    {
        Vector3 start   = transform.localScale;
        float   elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(start, target, elapsed / duration);
            yield return null;
        }
        transform.localScale = target;
    }

    // ── Collision ─────────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Obstacle")) return;
        if (CGGameManager.Instance?.State != CGGameManager.GameState.Playing) return;

        cameraController?.TriggerShake();
        CGGameManager.Instance.TriggerGameOver();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Obstacle")) return;
        if (CGGameManager.Instance?.State != CGGameManager.GameState.Playing) return;

        cameraController?.TriggerShake();
        CGGameManager.Instance.TriggerGameOver();
    }

    // ── Game over ─────────────────────────────────────────────────────────────

    private void HandleGameOver()
    {
        if (trailRenderer != null) trailRenderer.emitting = false;
        enabled = false;
    }
}
