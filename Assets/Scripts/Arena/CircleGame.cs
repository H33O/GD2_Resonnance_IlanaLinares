using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Self-contained circle arena game. All parameters are live-editable in the Inspector.
/// Modify any value while in Play Mode — the ring, ball and obstacles update immediately.
/// </summary>
public class CircleGame : MonoBehaviour
{
    // ── Inspector — Arena ─────────────────────────────────────────────────────

    [Header("Arena")]
    [Tooltip("Radius of the ring in world units.")]
    [Range(2f, 10f)]
    public float ringRadius = 5.5f;

    [Tooltip("Number of line segments used to draw the ring. Higher = smoother.")]
    [Range(16, 128)]
    public int ringSegments = 80;

    [Tooltip("Visual thickness of the ring line in world units.")]
    [Range(0.02f, 0.5f)]
    public float ringThickness = 0.14f;

    [Tooltip("Color of the ring.")]
    public Color ringColor = Color.black;

    // ── Inspector — Ball ──────────────────────────────────────────────────────

    [Header("Ball")]
    [Tooltip("Radius of the ball in world units.")]
    [Range(0.05f, 0.5f)]
    public float ballRadius = 0.18f;

    [Tooltip("Color of the ball.")]
    public Color ballColor = Color.black;

    [Tooltip("Orbital speed in degrees per second.")]
    [Range(10f, 360f)]
    public float orbitSpeed = 90f;

    [Tooltip("Total duration of one jump cycle (in + out), in seconds.")]
    [Range(0.1f, 1f)]
    public float jumpDuration = 0.35f;

    [Tooltip("How many world units the ball travels inward when jumping.")]
    [Range(0.3f, 4f)]
    public float jumpInset = 1.4f;

    // ── Inspector — Obstacles ─────────────────────────────────────────────────

    [Header("Obstacles")]
    [Tooltip("Tangential width of each obstacle in world units.")]
    [Range(0.05f, 0.6f)]
    public float obstacleWidth = 0.18f;

    [Tooltip("Radial height (depth) of each obstacle in world units.")]
    [Range(0.3f, 2f)]
    public float obstacleHeight = 0.9f;

    [Tooltip("Color of the obstacles.")]
    public Color obstacleColor = Color.black;

    [Tooltip("Delay before the very first obstacle appears.")]
    [Range(0.5f, 5f)]
    public float firstSpawnDelay = 1.5f;

    [Tooltip("Seconds between each obstacle spawn.")]
    [Range(0.5f, 6f)]
    public float spawnInterval = 2.2f;

    [Tooltip("Minimum angular gap (degrees) between a new obstacle and the ball or other obstacles.")]
    [Range(20f, 150f)]
    public float minAngleGap = 65f;

    [Tooltip("How long each obstacle stays on the ring before being destroyed (seconds).")]
    [Range(2f, 20f)]
    public float obstacleLifetime = 8f;

    // ── Inspector — Score ─────────────────────────────────────────────────────

    [Header("Score")]
    [Tooltip("Score points gained per second of survival.")]
    [Range(0.1f, 10f)]
    public float scorePerSecond = 1f;

    [Tooltip("Font size of the live score display.")]
    [Range(24, 120)]
    public float scoreFontSize = 64f;

    // ── Inspector — Input ─────────────────────────────────────────────────────

    [Header("Input")]
    [Tooltip("Minimum downward swipe distance in pixels to trigger a jump.")]
    [Range(10f, 200f)]
    public float swipeThreshold = 40f;

    // ── Inspector — Collectibles ──────────────────────────────────────────────

    [Header("Collectibles")]
    [Tooltip("Size of each collectible square in world units.")]
    [Range(0.1f, 0.6f)]
    public float collectibleSize = 0.22f;

    [Tooltip("Color of the collectible squares.")]
    public Color collectibleColor = Color.white;

    [Tooltip("Seconds between each collectible spawn.")]
    [Range(1f, 10f)]
    public float collectibleSpawnInterval = 4f;

    [Tooltip("Maximum collectibles alive on the ring at the same time.")]
    [Range(1, 6)]
    public int collectibleMaxCount = 2;

    [Tooltip("How long a collectible stays on the ring before disappearing.")]
    [Range(3f, 20f)]
    public float collectibleLifetime = 10f;

    [Tooltip("Radius offset from the ring surface (positive = outward).")]
    [Range(-1f, 1f)]
    public float collectibleRadiusOffset = 0f;

    [Header("Collectible — Layout Modes")]
    [Tooltip("How many seconds the collected layout mode stays active.")]
    [Range(3f, 20f)]
    public float layoutModeDuration = 6f;

    [Tooltip("BURST: number of obstacles spawned simultaneously.")]
    [Range(2, 6)]
    public int burstCount = 3;

    [Tooltip("BURST: angular spread between burst obstacles (degrees).")]
    [Range(20f, 120f)]
    public float burstSpread = 45f;

    [Tooltip("MIRROR: angular gap on each side of the ball where no obstacle spawns.")]
    [Range(10f, 90f)]
    public float mirrorSafeZone = 30f;

    // ── Runtime state ─────────────────────────────────────────────────────────

    private float  angleDeg     = 90f;
    private bool   isJumping    = false;
    private bool   isDead       = false;
    private float  score        = 0f;
    private float  spawnTimer   = 0f;
    private bool   firstSpawned = false;

    private readonly List<GameObject> obstacles    = new();
    private readonly List<GameObject> collectibles = new();

    // ── Collectible layout mode ───────────────────────────────────────────────

    private enum LayoutMode { Normal, Burst, Mirror }

    private LayoutMode activeLayout       = LayoutMode.Normal;
    private float      layoutModeTimer    = 0f;
    private bool       layoutModeActive   = false;
    private int        collectiblesGathered = 0;

    private float collectibleSpawnTimer = 0f;

    // ── Scene object references (updated live via OnValidate) ─────────────────

    private Transform        ballTransform;
    private SpriteRenderer   ballRenderer;
    private LineRenderer     ringLine;
    private Material         ringMat;
    private TextMeshProUGUI  scoreText;
    private TextMeshProUGUI  collectibleCountText;
    private TextMeshProUGUI  layoutModeText;
    private GameObject       gameOverPanel;
    private TextMeshProUGUI  gameOverScore;

    // ── Input ─────────────────────────────────────────────────────────────────

    private InputAction pressAction;
    private InputAction positionAction;
    private bool        pressing;
    private Vector2     pressStartPos;
    private bool        jumpQueued;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        BuildScene();

        // Button — detects press start and release
        pressAction = new InputAction("Press", InputActionType.Button);
        pressAction.AddBinding("<Mouse>/leftButton");
        pressAction.AddBinding("<Touchscreen>/primaryTouch/press");

        // Value — reads current pointer position
        positionAction = new InputAction("Position", InputActionType.Value,
                                         expectedControlType: "Vector2");
        positionAction.AddBinding("<Mouse>/position");
        positionAction.AddBinding("<Touchscreen>/primaryTouch/position");

        pressAction.started  += _ =>
        {
            pressStartPos = positionAction.ReadValue<Vector2>();
            pressing      = true;
        };
        pressAction.canceled += _ => pressing = false;

        pressAction.Enable();
        positionAction.Enable();
    }

    private void OnDestroy()
    {
        pressAction?.Disable();
        pressAction?.Dispose();
        positionAction?.Disable();
        positionAction?.Dispose();
    }

    /// <summary>
    /// Called by Unity whenever a value changes in the Inspector (edit or play mode).
    /// Updates all live visuals without restarting.
    /// </summary>
    private void OnValidate()
    {
        // Ring
        if (ringLine != null)
        {
            RefreshRingPoints();
            ringLine.startWidth = ringThickness;
            ringLine.endWidth   = ringThickness;
            if (ringMat != null) ringMat.color = ringColor;
        }

        // Ball
        if (ballTransform != null)
            ballTransform.localScale = Vector3.one * (ballRadius * 2f);
        if (ballRenderer != null)
            ballRenderer.color = ballColor;

        // Score font
        if (scoreText != null)
            scoreText.fontSize = scoreFontSize;

        // Obstacle color
        foreach (var obs in obstacles)
        {
            if (obs == null) continue;
            var sr = obs.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = obstacleColor;
        }
    }

    private void Update()
    {
        if (isDead) return;

        // Score
        score += scorePerSecond * Time.deltaTime;
        if (scoreText != null)
            scoreText.text = Mathf.FloorToInt(score).ToString();

        // Layout mode countdown
        if (layoutModeActive)
        {
            layoutModeTimer -= Time.deltaTime;
            if (layoutModeTimer <= 0f)
            {
                layoutModeActive = false;
                activeLayout     = LayoutMode.Normal;
                RefreshLayoutUI();
            }
        }

        // Obstacle spawn
        spawnTimer += Time.deltaTime;
        float delay = firstSpawned ? spawnInterval : firstSpawnDelay;
        if (spawnTimer >= delay)
        {
            spawnTimer   = 0f;
            firstSpawned = true;
            SpawnObstacleForMode();
        }

        // Collectible spawn
        collectibles.RemoveAll(c => c == null);
        collectibleSpawnTimer += Time.deltaTime;
        if (collectibleSpawnTimer >= collectibleSpawnInterval
            && collectibles.Count < collectibleMaxCount)
        {
            collectibleSpawnTimer = 0f;
            SpawnCollectible();
        }

        // Ball orbit
        if (!isJumping)
        {
            angleDeg += orbitSpeed * Time.deltaTime;
            PlaceBallOnRing(ringRadius);
        }

        // Jump input — trigger on downward swipe
        if (pressing && !jumpQueued && !isJumping)
        {
            Vector2 current = positionAction.ReadValue<Vector2>();
            if (pressStartPos.y - current.y > swipeThreshold)
            {
                pressing   = false;
                jumpQueued = true;
            }
        }

        if (jumpQueued && !isJumping)
            StartCoroutine(JumpRoutine());
        jumpQueued = false;

        // Collision
        CheckCollision();
        CheckCollectibles();

        // Clean up destroyed refs
        obstacles.RemoveAll(o => o == null);
    }

    // ── Ball placement ────────────────────────────────────────────────────────

    private void PlaceBallOnRing(float radius)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        ballTransform.position = new Vector3(Mathf.Cos(rad) * radius,
                                             Mathf.Sin(rad) * radius, 0f);
    }

    // ── Jump ──────────────────────────────────────────────────────────────────

    private IEnumerator JumpRoutine()
    {
        isJumping = true;
        float innerRadius = ringRadius - jumpInset;
        float half        = jumpDuration * 0.5f;
        float t           = 0f;

        // Jump inward
        while (t < half)
        {
            t += Time.deltaTime;
            float r = Mathf.Lerp(ringRadius, innerRadius, t / half);
            PlaceBallOnRing(r);
            yield return null;
        }

        // Return to ring
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float r = Mathf.Lerp(innerRadius, ringRadius, t / half);
            PlaceBallOnRing(r);
            yield return null;
        }

        PlaceBallOnRing(ringRadius);
        isJumping = false;
    }

    // ── Obstacle spawning ─────────────────────────────────────────────────────

    /// <summary>Spawns obstacles according to the active layout mode.</summary>
    private void SpawnObstacleForMode()
    {
        switch (activeLayout)
        {
            case LayoutMode.Burst:
                float center = FindSafeAngle();
                for (int i = 0; i < burstCount; i++)
                {
                    float offset = (i - (burstCount - 1) * 0.5f) * burstSpread;
                    SpawnObstacle(center + offset);
                }
                break;

            case LayoutMode.Mirror:
                float base1 = FindSafeAngle(mirrorSafeZone);
                float base2 = base1 + 180f;
                SpawnObstacle(base1);
                SpawnObstacle(base2);
                break;

            default: // Normal
                SpawnObstacle(FindSafeAngle());
                break;
        }
    }

    private void SpawnObstacle(float angle)
    {
        float rad = angle * Mathf.Deg2Rad;

        var go = new GameObject("Obstacle");
        go.transform.SetParent(transform);
        go.tag = "Obstacle";
        go.transform.position = new Vector3(Mathf.Cos(rad) * ringRadius,
                                            Mathf.Sin(rad) * ringRadius, 0f);
        go.transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);

        var sr            = go.AddComponent<SpriteRenderer>();
        sr.sprite         = MakeRectSprite(obstacleWidth, obstacleHeight);
        sr.color          = obstacleColor;
        sr.sortingOrder   = 2;
        sr.sharedMaterial = GetUnlitMat();

        var col       = go.AddComponent<BoxCollider2D>();
        col.size      = new Vector2(obstacleWidth, obstacleHeight);
        col.isTrigger = true;

        obstacles.Add(go);
        Destroy(go, obstacleLifetime);
    }

    // ── Collectible spawning ──────────────────────────────────────────────────

    private void SpawnCollectible()
    {
        float angle = FindSafeAngle();
        float rad   = angle * Mathf.Deg2Rad;
        float r     = ringRadius + collectibleRadiusOffset;

        var go = new GameObject("Collectible");
        go.transform.SetParent(transform);
        go.transform.position = new Vector3(Mathf.Cos(rad) * r,
                                            Mathf.Sin(rad) * r, 0f);
        go.transform.localScale = Vector3.one * collectibleSize;

        var sr            = go.AddComponent<SpriteRenderer>();
        sr.sprite         = MakeRectSprite(1f, 1f);   // 1×1 unit, scaled by localScale
        sr.color          = collectibleColor;
        sr.sortingOrder   = 5;
        sr.sharedMaterial = GetUnlitMat();

        var col       = go.AddComponent<BoxCollider2D>();
        col.size      = Vector2.one;
        col.isTrigger = true;

        collectibles.Add(go);
        Destroy(go, collectibleLifetime);
    }

    // ── Collectible pickup ────────────────────────────────────────────────────

    private void CheckCollectibles()
    {
        Vector2 ballPos = ballTransform.position;

        for (int i = collectibles.Count - 1; i >= 0; i--)
        {
            var c = collectibles[i];
            if (c == null) continue;

            var col = c.GetComponent<BoxCollider2D>();
            if (col != null && col.OverlapPoint(ballPos))
            {
                Collect(c);
                collectibles.RemoveAt(i);
            }
        }
    }

    private void Collect(GameObject collectible)
    {
        Destroy(collectible);
        collectiblesGathered++;

        // Cycle through layout modes: Normal → Burst → Mirror → Normal …
        activeLayout     = (LayoutMode)(collectiblesGathered % 3);
        layoutModeActive = true;
        layoutModeTimer  = layoutModeDuration;

        // Clear current obstacles so the new mode feels immediate
        foreach (var obs in obstacles)
            if (obs != null) Destroy(obs);
        obstacles.Clear();
        spawnTimer = spawnInterval; // force immediate re-spawn

        RefreshLayoutUI();
    }

    private void RefreshLayoutUI()
    {
        if (collectibleCountText != null)
            collectibleCountText.text = collectiblesGathered.ToString();

        if (layoutModeText != null)
        {
            layoutModeText.text = layoutModeActive
                ? activeLayout switch
                {
                    LayoutMode.Burst  => "BURST",
                    LayoutMode.Mirror => "MIRROR",
                    _                 => ""
                }
                : "";
        }
    }

    private float FindSafeAngle()
    {
        const int  attempts = 30;
        for (int i = 0; i < attempts; i++)
        {
            float candidate = Random.Range(0f, 360f);
            if (IsAngleSafe(candidate))
                return candidate;
        }
        return Random.Range(0f, 360f); // fallback
    }

    private bool IsAngleSafe(float candidate)
    {
        if (Mathf.Abs(Mathf.DeltaAngle(candidate, angleDeg)) < minAngleGap)
            return false;
        foreach (var obs in obstacles)
        {
            if (obs == null) continue;
            float a = Mathf.Atan2(obs.transform.position.y,
                                  obs.transform.position.x) * Mathf.Rad2Deg;
            if (Mathf.Abs(Mathf.DeltaAngle(candidate, a)) < minAngleGap * 0.5f)
                return false;
        }
        return true;
    }

    // ── Collision ─────────────────────────────────────────────────────────────

    private void CheckCollision()
    {
        if (isJumping) return;

        Vector2 ballPos = ballTransform.position;
        foreach (var obs in obstacles)
        {
            if (obs == null) continue;
            var col = obs.GetComponent<BoxCollider2D>();
            if (col != null && col.OverlapPoint(ballPos))
            {
                Die();
                return;
            }
        }
    }

    private void Die()
    {
        isDead = true;
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            if (gameOverScore != null)
                gameOverScore.text = "Score  " + Mathf.FloorToInt(score);
        }
    }

    // ── Scene construction ────────────────────────────────────────────────────

    private void BuildScene()
    {
        BuildRing();
        BuildBall();
        BuildUI();
    }

    private void BuildRing()
    {
        var go = new GameObject("Ring");
        go.transform.SetParent(transform);

        ringLine               = go.AddComponent<LineRenderer>();
        ringLine.loop          = true;
        ringLine.useWorldSpace = true;
        ringLine.startWidth    = ringThickness;
        ringLine.endWidth      = ringThickness;
        ringLine.sortingOrder  = 1;

        ringMat       = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
        ringMat.color = ringColor;
        ringLine.sharedMaterial = ringMat;

        RefreshRingPoints();
    }

    private void RefreshRingPoints()
    {
        if (ringLine == null) return;
        ringLine.positionCount = ringSegments;
        var pts = new Vector3[ringSegments];
        for (int i = 0; i < ringSegments; i++)
        {
            float a = i / (float)ringSegments * 2f * Mathf.PI;
            pts[i]  = new Vector3(Mathf.Cos(a) * ringRadius, Mathf.Sin(a) * ringRadius, 0f);
        }
        ringLine.SetPositions(pts);
    }

    private void BuildBall()
    {
        var go = new GameObject("Ball");
        go.transform.SetParent(transform);

        ballRenderer              = go.AddComponent<SpriteRenderer>();
        ballRenderer.sprite       = MakeCircleSprite(64, Color.white);
        ballRenderer.sharedMaterial = GetUnlitMat();
        ballRenderer.color        = ballColor;
        ballRenderer.sortingOrder = 10;

        go.transform.localScale = Vector3.one * (ballRadius * 2f);
        ballTransform = go.transform;
        PlaceBallOnRing(ringRadius);
    }

    private void BuildUI()
    {
        var canvasGO = new GameObject("Canvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Live score
        var scoreGO = new GameObject("Score");
        scoreGO.transform.SetParent(canvasGO.transform, false);
        scoreText           = scoreGO.AddComponent<TextMeshProUGUI>();
        scoreText.text      = "0";
        scoreText.fontSize  = scoreFontSize;
        scoreText.color     = Color.black;
        scoreText.alignment = TextAlignmentOptions.Center;
        var sr2 = scoreText.rectTransform;
        sr2.anchorMin = new Vector2(0.5f, 1f); sr2.anchorMax = new Vector2(0.5f, 1f);
        sr2.pivot     = new Vector2(0.5f, 1f);
        sr2.anchoredPosition = new Vector2(0f, -40f); sr2.sizeDelta = new Vector2(200f, 80f);

        // Game-over panel
        gameOverPanel = new GameObject("GameOver");
        gameOverPanel.transform.SetParent(canvasGO.transform, false);
        gameOverPanel.AddComponent<UnityEngine.UI.Image>().color = new Color(1f, 1f, 1f, 0.95f);
        var pr = gameOverPanel.GetComponent<RectTransform>();
        pr.anchorMin = Vector2.zero; pr.anchorMax = Vector2.one;
        pr.offsetMin = Vector2.zero; pr.offsetMax = Vector2.zero;

        AddLabel(gameOverPanel, "GAME OVER", 72, FontStyles.Bold,   new Vector2(0f,  70f), new Vector2(500f, 110f));
        gameOverScore = AddLabel(gameOverPanel, "Score  0", 48, FontStyles.Normal, new Vector2(0f, -20f), new Vector2(400f, 70f));

        var btnGO = new GameObject("Restart");
        btnGO.transform.SetParent(gameOverPanel.transform, false);
        btnGO.AddComponent<UnityEngine.UI.Image>().color = Color.black;
        var btn = btnGO.AddComponent<UnityEngine.UI.Button>();
        btn.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().name));
        var br = btnGO.GetComponent<RectTransform>();
        br.anchorMin = new Vector2(0.5f, 0.5f); br.anchorMax = new Vector2(0.5f, 0.5f);
        br.pivot     = new Vector2(0.5f, 0.5f);
        br.anchoredPosition = new Vector2(0f, -120f); br.sizeDelta = new Vector2(220f, 60f);
        AddLabel(btnGO, "RESTART", 32, FontStyles.Bold, Vector2.zero, Vector2.zero, true, Color.white);

        gameOverPanel.SetActive(false);
    }

    /// <summary>Helper to add a TextMeshProUGUI label as a child.</summary>
    private TextMeshProUGUI AddLabel(
        GameObject parent, string text, float size, FontStyles style,
        Vector2 anchoredPos, Vector2 sizeDelta,
        bool stretchFill = false, Color? color = null)
    {
        var go  = new GameObject(text.Split(' ')[0]);
        go.transform.SetParent(parent.transform, false);
        var tmp       = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.fontStyle = style;
        tmp.color     = color ?? Color.black;
        tmp.alignment = TextAlignmentOptions.Center;
        var r = tmp.rectTransform;
        if (stretchFill)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        }
        else
        {
            r.anchorMin = new Vector2(0.5f, 0.5f); r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot     = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = anchoredPos; r.sizeDelta = sizeDelta;
        }
        return tmp;
    }

    // ── Scene Gizmos (visible in Edit Mode) ──────────────────────────────────

    private void OnDrawGizmos()
    {
        // Draw the ring outline so it's visible in the Scene view without Play Mode
        Gizmos.color = ringColor;
        int   segs = Mathf.Max(16, ringSegments);
        float step = 2f * Mathf.PI / segs;
        for (int i = 0; i < segs; i++)
        {
            float a0 = i * step, a1 = (i + 1) * step;
            Vector3 p0 = transform.position + new Vector3(Mathf.Cos(a0) * ringRadius,
                                                           Mathf.Sin(a0) * ringRadius, 0f);
            Vector3 p1 = transform.position + new Vector3(Mathf.Cos(a1) * ringRadius,
                                                           Mathf.Sin(a1) * ringRadius, 0f);
            Gizmos.DrawLine(p0, p1);
        }

        // Draw the ball starting position
        Gizmos.color = ballColor == Color.clear ? Color.gray : ballColor;
        float startRad = 90f * Mathf.Deg2Rad;
        Vector3 ballPos = transform.position + new Vector3(Mathf.Cos(startRad) * ringRadius,
                                                            Mathf.Sin(startRad) * ringRadius, 0f);
        Gizmos.DrawSphere(ballPos, ballRadius);
    }

    // ── Material & Sprite helpers ─────────────────────────────────────────────

    private static Material unlitMat;

    /// <summary>Returns a shared URP 2D unlit sprite material.</summary>
    private static Material GetUnlitMat()
    {
        if (unlitMat == null)
            unlitMat = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
        return unlitMat;
    }

    /// <summary>Generates a filled circle sprite (resolution × resolution px, PPU = resolution).</summary>
    private static Sprite MakeCircleSprite(int res, Color color)
    {
        var   tex  = new Texture2D(res, res, TextureFormat.RGBA32, false);
        float half = res * 0.5f, r2 = half * half;
        for (int y = 0; y < res; y++)
        for (int x = 0; x < res; x++)
        {
            float dx = x - half + 0.5f, dy = y - half + 0.5f;
            tex.SetPixel(x, y, dx * dx + dy * dy <= r2 ? color : Color.clear);
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
    }

    /// <summary>Generates a white filled rectangle sprite (worldW × worldH world units, PPU = 100).</summary>
    private static Sprite MakeRectSprite(float worldW, float worldH)
    {
        const float ppu = 100f;
        int w = Mathf.Max(1, Mathf.RoundToInt(worldW * ppu));
        int h = Mathf.Max(1, Mathf.RoundToInt(worldH * ppu));
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
            tex.SetPixel(x, y, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), ppu);
    }
}
