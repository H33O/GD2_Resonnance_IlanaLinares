using UnityEngine;

/// <summary>
/// An individual obstacle attached to the inner edge of the circular arena.
/// Detects near-misses and notifies <see cref="CGFeedbackManager"/>.
/// Attach to each <b>Obstacle_XX</b> prefab or scene object.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class CGObstacle : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Settings")]
    public CGSettings settings;

    [Header("References")]
    public CGFeedbackManager feedbackManager;
    public CGPlayerBall      playerBall;

    [Header("Obstacle Data")]
    [Tooltip("Angle on the ring where this obstacle is placed (degrees).")]
    public float AngleDeg;

    // ── State ─────────────────────────────────────────────────────────────────

    private bool nearMissTriggered;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        var sr = GetComponent<SpriteRenderer>();
        sr.color        = Color.black;
        sr.sortingOrder = 3;

        var col    = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
        if (settings != null)
            col.size = new Vector2(settings.obstacleWidth, settings.obstacleHeight);
    }

    private void Update()
    {
        if (playerBall == null || settings == null) return;
        if (CGGameManager.Instance?.State != CGGameManager.GameState.Playing) return;

        // Calculate angular separation to the player (accounting for rotation of parent)
        float worldAngle = AngleDeg + transform.parent.eulerAngles.z;
        float diff       = Mathf.Abs(Mathf.DeltaAngle(playerBall.AngleDeg, worldAngle));

        bool playerOnRing = Mathf.Approximately(
            playerBall.CurrentRadius,
            settings.arenaRadius);

        // Near-miss window — only triggers when ball is near the ring surface
        if (!nearMissTriggered
            && playerBall.CurrentRadius > settings.arenaRadius * 0.85f
            && diff < settings.nearMissWindowDeg
            && diff > settings.obstacleHalfAngle)
        {
            nearMissTriggered = true;
            feedbackManager?.TriggerNearMissFlash();
        }

        // Reset near-miss flag once the player is well clear
        if (nearMissTriggered && diff > settings.nearMissWindowDeg * 1.5f)
            nearMissTriggered = false;
    }
}
