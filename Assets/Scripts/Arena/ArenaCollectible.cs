using UnityEngine;

/// <summary>
/// A collectible placed on the arena ring. When the ball passes through it,
/// the collectible is destroyed and one extra obstacle is spawned on the ring.
///
/// Attach to a GameObject on the ring. Wire Ball and Spawner in the Inspector,
/// or let Awake() auto-resolve them.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ArenaCollectible : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Settings")]
    [Tooltip("Color of the collectible sprite (white for B&W art direction).")]
    public Color collectibleColor = Color.white;

    [Tooltip("Color of the outline ring drawn around the collectible.")]
    public Color outlineColor = Color.black;

    [Tooltip("Radius of the generated circle sprite (pixels, also PPU).")]
    public int spriteResolution = 32;

    [Tooltip("Outline thickness in pixels relative to sprite resolution.")]
    [Range(1, 8)]
    public int outlineThickness = 3;

    [Header("Scene References")]
    [Tooltip("Ball instance – auto-resolved if left empty.")]
    public ArenaBall ball;

    [Tooltip("Obstacle spawner that will spawn one obstacle on collect – auto-resolved if left empty.")]
    public ArenaObstacleSpawner spawner;

    [Header("Feedback")]
    [Tooltip("ParticleSystem that bursts on collect. Auto-resolved from FX/DodgeParticles if left empty.")]
    public ParticleSystem collectParticles;

    // ── Internal ──────────────────────────────────────────────────────────────

    private bool collected;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (ball == null)
            ball = FindFirstObjectByType<ArenaBall>();

        if (spawner == null)
            spawner = FindFirstObjectByType<ArenaObstacleSpawner>();

        if (collectParticles == null)
        {
            var fxGo = GameObject.Find("DodgeParticles");
            if (fxGo != null)
                collectParticles = fxGo.GetComponent<ParticleSystem>();
        }

        // Build a filled-circle sprite with black outline
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = gameObject.AddComponent<SpriteRenderer>();

        if (sr.sprite == null)
            sr.sprite = CreateCircleWithOutline(spriteResolution, collectibleColor, outlineColor, outlineThickness);

        if (sr.sharedMaterial == null)
            sr.sharedMaterial = new Material(
                Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));

        sr.color      = Color.white; // tint is baked into the sprite texture
        sr.sortingOrder = 5;

        // Make sure the collider is a trigger
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    // ── Collision ─────────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if (ball == null) return;

        // Accept contact from the ball's own collider
        if (other.gameObject == ball.gameObject ||
            other.transform.IsChildOf(ball.transform))
        {
            Collect();
        }
    }

    // ── Logic ─────────────────────────────────────────────────────────────────

    /// <summary>Marks the collectible as picked up and notifies the spawner.</summary>
    private void Collect()
    {
        if (collected) return;
        collected = true;

        // Award score bonus via game manager
        ArenaGameManager.Instance?.NotifyCollectiblePickup();

        // Trigger particle burst at this world position
        if (collectParticles != null)
        {
            collectParticles.transform.position = transform.position;
            collectParticles.Emit(10);
        }

        spawner?.SpawnObstacleFromCollectible();
        Destroy(gameObject);
    }

    // ── Sprite generation ─────────────────────────────────────────────────────

    /// <summary>Creates a filled circle with a solid-colour outline ring.</summary>
    private static Sprite CreateCircleWithOutline(int resolution, Color fill, Color outline, int thickness)
    {
        var tex    = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        float half = resolution * 0.5f;
        float outerR2 = half * half;
        float innerR2 = (half - thickness) * (half - thickness);

        for (int y = 0; y < resolution; y++)
        for (int x = 0; x < resolution; x++)
        {
            float dx = x - half + 0.5f;
            float dy = y - half + 0.5f;
            float d2 = dx * dx + dy * dy;

            Color pixel;
            if (d2 > outerR2)
                pixel = Color.clear;
            else if (d2 > innerR2)
                pixel = outline;
            else
                pixel = fill;

            tex.SetPixel(x, y, pixel);
        }
        tex.Apply();

        return Sprite.Create(
            tex,
            new Rect(0, 0, resolution, resolution),
            new Vector2(0.5f, 0.5f),
            resolution);
    }

    // Kept for backwards compatibility
    private static Sprite CreateCircleSprite(int resolution, Color color)
        => CreateCircleWithOutline(resolution, color, Color.black, 3);
}
