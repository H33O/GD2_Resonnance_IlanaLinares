using UnityEngine;

/// <summary>
/// Represents a single obstacle pinned to the inner wall of the arena.
/// The obstacle rotates with the ring and performs near-miss detection
/// against the ArenaBall every frame.
///
/// Attach to each Obstacle prefab or placed obstacle in the scene.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ArenaObstacle : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Settings")]
    public ArenaSettings settings;

    [Header("Scene References")]
    [Tooltip("The ball instance in the scene. Auto-resolved if left empty.")]
    public ArenaBall ball;

    // ── Runtime state ─────────────────────────────────────────────────────────

    private bool nearMissTriggered;
    private bool perfectDodgeTriggered;

    // ── Angle ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Angle on the arena ring where this obstacle is pinned (degrees).
    /// Set this from ArenaObstacleSpawner or directly in the Inspector after placement.
    /// </summary>
    [Tooltip("Angle on the ring (degrees). 0 = right, 90 = top. " +
             "Set by the spawner or manually after placement.")]
    public float angleDeg;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Auto-resolve ball if not wired
        if (ball == null)
            ball = FindFirstObjectByType<ArenaBall>();

        // Auto-generate a black rectangle sprite + URP-compatible material
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (sr.sprite == null)
                sr.sprite = CreateRectSprite(40, 160, Color.black);
            sr.sharedMaterial = GetUnlitMaterial();
            sr.color = Color.black;
        }
    }

    // ── URP material ──────────────────────────────────────────────────────────

    private static Material cachedUnlitMat;

    private static Material GetUnlitMaterial()
    {
        if (cachedUnlitMat == null)
            cachedUnlitMat = new Material(
                Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
        return cachedUnlitMat;
    }

    private void Update()
    {
        if (ArenaGameManager.Instance == null) return;
        if (ArenaGameManager.Instance.State != ArenaGameManager.GameState.Playing) return;
        if (ball == null) return;

        DetectNearMiss();
    }

    // ── Near-miss detection ───────────────────────────────────────────────────

    private void DetectNearMiss()
    {
        float angularDiff = Mathf.Abs(Mathf.DeltaAngle(ball.AngleDeg, angleDeg));

        // Perfect dodge: ball is jumping AND within the tight dodge window
        if (!perfectDodgeTriggered && ball.IsJumping && angularDiff < settings.dodgeWindowDeg)
        {
            perfectDodgeTriggered = true;
            ArenaGameManager.Instance.NotifyPerfectDodge(transform.position);
            return;
        }

        // Near-miss flash: ball on the rim, close to obstacle but not colliding
        if (!nearMissTriggered
            && !ball.IsJumping
            && angularDiff < settings.nearMissWindowDeg
            && angularDiff > settings.obstacleHalfAngle)
        {
            nearMissTriggered = true;
            ArenaGameManager.Instance.NotifyNearMiss(transform.position);
        }

        // Reset flags once the ball moves away
        if (angularDiff > settings.nearMissWindowDeg + 5f)
        {
            nearMissTriggered      = false;
            perfectDodgeTriggered  = false;
        }
    }

    // ── Sprite generation ─────────────────────────────────────────────────────

    /// <summary>Generates a filled rectangle sprite at runtime.</summary>
    private static Sprite CreateRectSprite(int w, int h, Color color)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
            tex.SetPixel(x, y, color);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }
}
