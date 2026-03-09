using System.Collections;
using UnityEngine;

/// <summary>
/// Spawns small white dot sprites behind the ball at regular intervals,
/// fading them out progressively — matching the reference image aesthetic.
///
/// Attach to the Ball GameObject. Disable the TrailRenderer sibling.
/// </summary>
public class ArenaBallTrail : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Settings")]
    public ArenaSettings settings;

    [Header("Dot Appearance")]
    [Tooltip("Diameter of the first (freshest) dot in world units.")]
    public float dotSizeStart = 0.20f;

    [Tooltip("Diameter of the last (oldest) dot in world units.")]
    public float dotSizeEnd = 0.06f;

    [Tooltip("Opacity of the first (freshest) dot.")]
    [Range(0f, 1f)]
    public float dotAlphaStart = 0.85f;

    [Tooltip("Opacity of the last (oldest) dot.")]
    [Range(0f, 1f)]
    public float dotAlphaEnd = 0.0f;

    [Tooltip("How long each dot lives before fully disappearing (seconds).")]
    public float dotLifetime = 0.55f;

    [Tooltip("Interval between dot spawns (seconds). Lower = denser trail.")]
    public float spawnInterval = 0.055f;

    [Header("Pool")]
    [Tooltip("Maximum dots alive simultaneously.")]
    [Range(4, 32)]
    public int poolSize = 20;

    // ── Pool ──────────────────────────────────────────────────────────────────

    private TrailDot[] pool;
    private int        nextSlot;

    private struct TrailDot
    {
        public GameObject    go;
        public SpriteRenderer sr;
        public bool          active;
        public float         spawnTime;
        public Vector3       worldPos;
        public int           index;    // 0 = freshest when spawned
    }

    // ── Sprite shared across all dots ─────────────────────────────────────────

    private static Sprite dotSprite;

    // ── Timer ─────────────────────────────────────────────────────────────────

    private float spawnTimer;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (dotSprite == null)
            dotSprite = CreateCircleSprite(32, Color.white);

        BuildPool();
    }

    private void Update()
    {
        bool isPlaying = ArenaGameManager.Instance == null
                      || ArenaGameManager.Instance.State == ArenaGameManager.GameState.Playing;

        if (isPlaying)
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnInterval)
            {
                spawnTimer = 0f;
                ActivateDot(transform.position);
            }
        }

        TickDots();
    }

    // ── Pool setup ────────────────────────────────────────────────────────────

    private void BuildPool()
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));

        pool = new TrailDot[poolSize];
        for (int i = 0; i < poolSize; i++)
        {
            var go = new GameObject($"TrailDot_{i}");
            go.transform.SetParent(transform.parent, false);

            var sr            = go.AddComponent<SpriteRenderer>();
            sr.sprite         = dotSprite;
            sr.sharedMaterial = mat;
            sr.color          = Color.clear;
            sr.sortingOrder   = 9;

            pool[i] = new TrailDot { go = go, sr = sr, active = false, index = i };
            go.SetActive(false);
        }
    }

    // ── Spawn ─────────────────────────────────────────────────────────────────

    private void ActivateDot(Vector3 worldPos)
    {
        ref TrailDot dot = ref pool[nextSlot];
        dot.active    = true;
        dot.spawnTime = Time.time;
        dot.worldPos  = worldPos;
        dot.go.transform.position = worldPos;
        dot.go.SetActive(true);

        nextSlot = (nextSlot + 1) % poolSize;
    }

    // ── Update all active dots ────────────────────────────────────────────────

    private void TickDots()
    {
        for (int i = 0; i < poolSize; i++)
        {
            ref TrailDot dot = ref pool[i];
            if (!dot.active) continue;

            float elapsed = Time.time - dot.spawnTime;
            float t       = Mathf.Clamp01(elapsed / dotLifetime);

            if (t >= 1f)
            {
                dot.active = false;
                dot.sr.color = Color.clear;
                dot.go.SetActive(false);
                continue;
            }

            float size  = Mathf.Lerp(dotSizeStart, dotSizeEnd, t);
            float alpha = Mathf.Lerp(dotAlphaStart, dotAlphaEnd, t * t);  // quadratic fade

            dot.go.transform.localScale = Vector3.one * size;
            dot.sr.color = new Color(1f, 1f, 1f, alpha);
        }
    }

    // ── Sprite generator ──────────────────────────────────────────────────────

    private static Sprite CreateCircleSprite(int res, Color color)
    {
        var tex    = new Texture2D(res, res, TextureFormat.RGBA32, false);
        float half = res * 0.5f;
        float r2   = half * half;

        for (int y = 0; y < res; y++)
        for (int x = 0; x < res; x++)
        {
            float dx = x - half + 0.5f;
            float dy = y - half + 0.5f;
            tex.SetPixel(x, y, (dx * dx + dy * dy) <= r2 ? color : Color.clear);
        }
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
    }
}
