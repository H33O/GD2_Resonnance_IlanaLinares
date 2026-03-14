using System.Collections;
using UnityEngine;

/// <summary>
/// Represents one incoming sword slash.
/// Moves toward the player center, checks for parry or hit, and
/// provides visual feedback on both outcomes.
/// Attach to every slash prefab spawned by <see cref="SGSlashSpawner"/>.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class SGSlash : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Settings")]
    public SGSettings settings;

    [Tooltip("If true, this slash is part of the tutorial and cannot kill the player.")]
    public bool isTutorialSlash;

    // ── Internal state ────────────────────────────────────────────────────────

    private LineRenderer lineRenderer;
    private Vector3      direction;
    private float        speed;
    private bool         alive = true;
    private bool         parried;
    private SGSlashSpawner spawner;

    // Tail glow LineRenderer (additive layer on top)
    private LineRenderer tailGlow;

    // ── Constants ─────────────────────────────────────────────────────────────

    private const float ParryKillDistance    = 0.35f;
    private const int   GlowCount            = 4;   // ghost copies for glow effect

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        ConfigureLineRenderer(lineRenderer, settings?.slashWidth ?? 0.08f, new Color(1f, 1f, 1f, 1f));

        // Tail glow — slightly wider, additive, more transparent
        var glowGO = new GameObject("SlashGlow");
        glowGO.transform.SetParent(transform, false);
        tailGlow = glowGO.AddComponent<LineRenderer>();
        ConfigureLineRenderer(tailGlow, (settings?.slashWidth ?? 0.08f) * 3f, new Color(1f, 1f, 1f, 0.25f));
        tailGlow.sortingOrder = lineRenderer.sortingOrder - 1;
    }

    private void Update()
    {
        if (!alive) return;

        float dist = transform.position.magnitude;
        if (dist < ParryKillDistance)
        {
            // Reached center without being parried → hit
            OnHit();
            return;
        }

        transform.position += direction * speed * Time.deltaTime;
        UpdateVisual();
    }

    // ── Setup ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Initialises the slash with spawn position and movement speed.
    /// Called by <see cref="SGSlashSpawner"/> immediately after instantiation.
    /// </summary>
    public void Init(Vector3 spawnPos, float moveSpeed, SGSettings cfg, bool tutorial = false, SGSlashSpawner ownerSpawner = null)
    {
        settings         = cfg;
        speed            = moveSpeed;
        isTutorialSlash  = tutorial;
        spawner          = ownerSpawner;
        transform.position = spawnPos;
        direction          = -spawnPos.normalized; // always moves toward center (0,0)

        // Orient the slash perpendicular to movement
        float angleDeg = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);

        UpdateVisual();
    }

    // ── Visual ────────────────────────────────────────────────────────────────

    private void UpdateVisual()
    {
        float len  = settings != null ? settings.slashLength : 1.0f;
        float half = len * 0.5f;

        // Main blade
        lineRenderer.SetPosition(0, transform.position + transform.right * half);
        lineRenderer.SetPosition(1, transform.position - transform.right * half);

        // Tail glow (slightly offset behind the slash, in movement direction)
        Vector3 behind = transform.position - direction * 0.2f;
        tailGlow.SetPosition(0, behind + transform.right * half * 1.1f);
        tailGlow.SetPosition(1, behind - transform.right * half * 1.1f);
    }

    // ── Parry ─────────────────────────────────────────────────────────────────

    /// <summary>Called by <see cref="SGPlayerController"/> when the player taps during the parry window.</summary>
    public void TryParry(Vector3 playerPos)
    {
        if (!alive || parried) return;

        // Check distance — must be within parry radius
        float parryRadius = settings?.parryRadius ?? 1.2f;
        if (Vector3.Distance(transform.position, playerPos) > parryRadius) return;

        OnParried();
    }

    private void OnParried()
    {
        if (!alive) return;
        alive  = true; // keep alive briefly for death anim
        parried = true;

        SGGameManager.Instance?.NotifyParry(transform.position);
        StartCoroutine(ParryDeathRoutine());
    }

    private void OnHit()
    {
        if (!alive) return;
        alive = false;

        NotifySpawnerDestroyed();

        if (!isTutorialSlash)
            SGGameManager.Instance?.NotifyHit();

        Destroy(gameObject, 0.05f);
    }

    // ── Death coroutines ──────────────────────────────────────────────────────

    private IEnumerator ParryDeathRoutine()
    {
        alive = false;

        NotifySpawnerDestroyed();

        // Flash to full bright then fade out
        float elapsed  = 0f;
        float duration = 0.18f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t  = elapsed / duration;
            float a  = Mathf.Lerp(1f, 0f, t);

            Color c = new Color(1f, 1f, 1f, a);
            lineRenderer.startColor = c;
            lineRenderer.endColor   = c;
            tailGlow.startColor     = new Color(1f, 1f, 1f, a * 0.4f);
            tailGlow.endColor       = new Color(1f, 1f, 1f, a * 0.4f);

            // Scale up slightly on impact
            float scale = Mathf.Lerp(1.2f, 0.8f, t);
            transform.localScale = new Vector3(scale, scale, 1f);

            yield return null;
        }

        Destroy(gameObject);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Removes this slash from the spawner's active list immediately.</summary>
    private void NotifySpawnerDestroyed()
    {
        spawner?.NotifySlashDestroyed(this);
    }

    private static void ConfigureLineRenderer(LineRenderer lr, float width, Color color)
    {
        lr.positionCount       = 2;
        lr.useWorldSpace       = true;
        lr.startWidth          = width;
        lr.endWidth            = width * 0.5f;
        lr.startColor          = color;
        lr.endColor            = color;
        lr.numCapVertices      = 4;
        lr.sortingOrder        = 10;
        lr.shadowCastingMode   = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows      = false;

        var mat = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
        lr.sharedMaterial = mat;
    }
}
