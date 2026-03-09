using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Cerveau du mini-jeu arène circulaire.
/// Tous les paramètres viennent d'un <see cref="CircleArenaConfig"/> ScriptableObject.
/// </summary>
public class CircleArenaGameManager : MonoBehaviour
{
    // ── Config & références ───────────────────────────────────────────────────

    private CircleArenaConfig cfg;
    private Canvas        canvas;
    private RectTransform ballRT;
    private RectTransform fxLayer;
    private Image         flashImage;
    private RectTransform uiLayer;

    // ── Dérivés ───────────────────────────────────────────────────────────────

    private float orbitRadius;   // rayon d'orbite (bord intérieur du ring)

    // ── État ──────────────────────────────────────────────────────────────────

    private enum State { Playing, Dead, Over }
    private State state = State.Playing;

    private float ballAngle   = 90f;
    private float ballOrbitR;
    private bool  isJumping;
    private float survivalTime;
    private int   score;
    private int   lastScore = -1;

    // ── Obstacles ─────────────────────────────────────────────────────────────

    private struct ObstacleData
    {
        public float         angle;
        public RectTransform rt;
        public bool          dodged;
    }
    private readonly List<ObstacleData> obstacles = new();
    private float spawnTimer;
    private float currentSpawnDelay;

    // ── UI ────────────────────────────────────────────────────────────────────

    private TextMeshProUGUI scoreText;
    private RectTransform   scoreRT;

    // ── Trail ─────────────────────────────────────────────────────────────────

    private struct TrailDot { public RectTransform rt; public Image img; public Vector2 pos; }
    private readonly List<TrailDot> trail = new();
    private float trailTimer;
    private const float TrailInterval = 0.018f;

    // ── Pool particules / ripples ─────────────────────────────────────────────

    private readonly Queue<Image> pool = new();
    private const int PoolSize = 48;

    // ── Shake ─────────────────────────────────────────────────────────────────

    private float   shakeTimer;
    private float   shakeMag;
    private Vector2 shakeOffset;

    // ── Init ──────────────────────────────────────────────────────────────────

    /// <summary>Appelé par <see cref="CircleArenaSetup"/> après construction de la scène.</summary>
    public void Init(CircleArenaConfig config, Canvas canvas,
                     RectTransform ballRT, RectTransform fxLayer,
                     Image flashImage, RectTransform uiLayer)
    {
        cfg             = config;
        this.canvas     = canvas;
        this.ballRT     = ballRT;
        this.fxLayer    = fxLayer;
        this.flashImage = flashImage;
        this.uiLayer    = uiLayer;

        orbitRadius        = cfg.arenaRadius - cfg.ballRadius - cfg.arenaStrokeWidth * 0.5f - 2f;
        ballOrbitR         = orbitRadius;
        currentSpawnDelay  = cfg.obstacleSpawnDelay;

        BuildPool();
        BuildTrail();
        BuildUI();
        PlaceBall();
    }

    // ── Update ────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (state == State.Over) return;
        ReadInput();
        if (state != State.Playing) return;

        survivalTime += Time.deltaTime;
        TickScore();
        MoveBall();
        UpdateTrail();
        TickObstacles();
        UpdateShake();
        CheckCollisions();
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    private void ReadInput()
    {
        bool tap = false;
        for (int i = 0; i < Input.touchCount; i++)
            if (Input.GetTouch(i).phase == TouchPhase.Began) { tap = true; break; }
        if (Input.GetMouseButtonDown(0)) tap = true;

        if (tap && state == State.Playing && !isJumping)
            StartCoroutine(Jump());
    }

    // ── Mouvement balle ───────────────────────────────────────────────────────

    private float Speed => Mathf.Lerp(cfg.ballSpeedStart, cfg.ballSpeedMax,
                                      Mathf.Clamp01(survivalTime / cfg.speedRampDuration));

    private void MoveBall()
    {
        if (!isJumping) ballAngle += Speed * Time.deltaTime;
        PlaceBall();
    }

    private void PlaceBall()
    {
        float rad = ballAngle * Mathf.Deg2Rad;
        ballRT.anchoredPosition = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * ballOrbitR
                                + shakeOffset;
    }

    // ── Saut ──────────────────────────────────────────────────────────────────

    private IEnumerator Jump()
    {
        isJumping = true;
        float startR  = ballOrbitR;
        float innerR  = cfg.arenaRadius * (1f - cfg.jumpDepthRatio);
        float returnR = orbitRadius;

        SpawnRipple(ballRT.anchoredPosition - shakeOffset, 28f, 0.28f);
        StartCoroutine(Squash(0.65f, 1.35f, 0.07f));

        float e = 0f;
        while (e < cfg.jumpDurationIn)
        {
            e         += Time.deltaTime;
            ballOrbitR = Mathf.Lerp(startR, innerR, Mathf.SmoothStep(0f, 1f, e / cfg.jumpDurationIn));
            ballAngle += Speed * Time.deltaTime;
            PlaceBall();
            yield return null;
        }
        ballOrbitR = innerR;

        StartCoroutine(Squash(1.35f, 0.65f, 0.06f));

        e = 0f;
        while (e < cfg.jumpDurationOut)
        {
            e         += Time.deltaTime;
            ballOrbitR = Mathf.Lerp(innerR, returnR, Mathf.SmoothStep(0f, 1f, e / cfg.jumpDurationOut));
            ballAngle += Speed * Time.deltaTime;
            PlaceBall();
            yield return null;
        }
        ballOrbitR = returnR;

        SpawnRipple(ballRT.anchoredPosition - shakeOffset, 18f, 0.22f);
        isJumping = false;
    }

    // ── Squash & Stretch ──────────────────────────────────────────────────────

    private IEnumerator Squash(float sx, float sy, float dur)
    {
        float base2 = cfg.ballRadius * 2f;
        float e     = 0f;
        while (e < dur)
        {
            e += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, e / dur);
            ballRT.sizeDelta = new Vector2(base2 * Mathf.Lerp(sx, 1f, t),
                                           base2 * Mathf.Lerp(sy, 1f, t));
            yield return null;
        }
        ballRT.sizeDelta = Vector2.one * base2;
    }

    // ── Score ─────────────────────────────────────────────────────────────────

    private void TickScore()
    {
        score = Mathf.FloorToInt(survivalTime);
        if (score == lastScore) return;
        lastScore      = score;
        scoreText.text = score.ToString();
        StartCoroutine(PulseScore());
    }

    private IEnumerator PulseScore()
    {
        float e = 0f, dur = 0.20f;
        while (e < dur)
        {
            e += Time.deltaTime;
            float t = e / dur;
            float s = t < 0.5f ? Mathf.Lerp(1f, 1.4f, t * 2f) : Mathf.Lerp(1.4f, 1f, (t - 0.5f) * 2f);
            scoreRT.localScale = Vector3.one * s;
            yield return null;
        }
        scoreRT.localScale = Vector3.one;
    }

    // ── Obstacles ─────────────────────────────────────────────────────────────

    private void TickObstacles()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer < currentSpawnDelay) return;
        spawnTimer = 0f;
        SpawnObstacle();
        float prog        = Mathf.Clamp01(survivalTime / cfg.speedRampDuration);
        currentSpawnDelay = Mathf.Lerp(cfg.obstacleSpawnDelay, cfg.obstacleMinDelay, prog);
    }

    private void SpawnObstacle()
    {
        // Spawn devant la balle avec un offset aléatoire
        float angle = ballAngle + 155f + Random.Range(-35f, 35f);

        var go  = new GameObject("Obstacle");
        go.transform.SetParent(canvas.transform, false);

        var img           = go.AddComponent<Image>();
        img.sprite        = SpriteGenerator.CreateWhiteSquare();
        img.color         = cfg.arenaColor;
        img.raycastTarget = false;

        var rt  = img.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        // Pivot en haut → le sommet du rect = bord extérieur du ring
        rt.pivot     = new Vector2(0.5f, 1f);

        PositionObstacle(rt, angle);
        obstacles.Add(new ObstacleData { angle = angle, rt = rt, dodged = false });
    }

    /// <summary>
    /// Place un rectangle-obstacle planté dans la paroi, pointant vers le centre.
    /// Le pivot est au bord extérieur (en haut du rect en espace local).
    /// </summary>
    private void PositionObstacle(RectTransform rt, float angleDeg)
    {
        float rad    = angleDeg * Mathf.Deg2Rad;
        // Bord intérieur du ring = là où le pivot vient se poser
        float edgeR  = cfg.arenaRadius - cfg.arenaStrokeWidth * 0.5f;
        rt.anchoredPosition = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * edgeR;
        rt.sizeDelta        = new Vector2(cfg.obstacleWidth, cfg.obstacleDepth);
        // L'obstacle pointe vers le centre : rotation = angleDeg - 90° en espace UI
        rt.localRotation    = Quaternion.Euler(0f, 0f, angleDeg - 90f);
    }

    // ── Collisions ────────────────────────────────────────────────────────────

    private void CheckCollisions()
    {
        for (int i = obstacles.Count - 1; i >= 0; i--)
        {
            var   obs  = obstacles[i];
            float diff = Mathf.Abs(Mathf.DeltaAngle(ballAngle, obs.angle));

            // Perfect dodge : balle dans la fenêtre angulaire en plein saut
            if (!obs.dodged && isJumping && diff < cfg.dodgeWindowDeg)
            {
                var od = obstacles[i]; od.dodged = true; obstacles[i] = od;
                StartCoroutine(DodgeFX(ballRT.anchoredPosition - shakeOffset));
                continue;
            }

            // Collision : près du mur ET dans l'angle de l'obstacle
            bool nearWall = ballOrbitR > orbitRadius - cfg.obstacleDepth * 0.55f;
            bool inAngle  = diff < cfg.obstacleWidth * 1.6f;

            if (!nearWall || !inAngle || isJumping) continue;

            // Warning flash quand on approche
            if (diff < cfg.obstacleWidth * 2.8f)
                StartCoroutine(NearFlash(0.22f));

            // Hit
            if (diff < cfg.obstacleWidth * 0.85f)
            {
                StartCoroutine(HitSequence());
                return;
            }
        }
    }

    // ── Hit ───────────────────────────────────────────────────────────────────

    private IEnumerator HitSequence()
    {
        state = State.Dead;
        StartCoroutine(DoShake(cfg.shakeMagnitude, cfg.shakeDuration));
        StartCoroutine(FlashFull(0.75f, 0.3f));
        Time.timeScale = cfg.slowMoScale;
        yield return new WaitForSecondsRealtime(cfg.slowMoDuration);
        Time.timeScale = 1f;
        state = State.Over;
        ShowGameOver();
    }

    // ── Feedbacks ─────────────────────────────────────────────────────────────

    private IEnumerator DodgeFX(Vector2 pos)
    {
        SpawnRipple(pos, 45f, 0.40f);
        Burst(pos, 7, 68f);
        yield return null;
    }

    private IEnumerator NearFlash(float a)
    {
        flashImage.color = new Color(1f, 1f, 1f, a);
        float e = 0f, dur = 0.10f;
        while (e < dur)
        {
            e += Time.deltaTime;
            flashImage.color = new Color(1f, 1f, 1f, Mathf.Lerp(a, 0f, e / dur));
            yield return null;
        }
        flashImage.color = Color.clear;
    }

    private IEnumerator FlashFull(float a, float dur)
    {
        flashImage.color = new Color(1f, 1f, 1f, a);
        float e = 0f;
        while (e < dur)
        {
            e += Time.unscaledDeltaTime;
            flashImage.color = new Color(1f, 1f, 1f, Mathf.Lerp(a, 0f, e / dur));
            yield return null;
        }
        flashImage.color = Color.clear;
    }

    private IEnumerator DoShake(float mag, float dur)
    {
        shakeMag   = mag;
        shakeTimer = dur;
        yield return null;
    }

    private void UpdateShake()
    {
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float s     = shakeMag * Mathf.Clamp01(shakeTimer / cfg.shakeDuration);
            shakeOffset = new Vector2(Random.Range(-s, s), Random.Range(-s, s));
        }
        else { shakeOffset = Vector2.zero; shakeMag = 0f; }
    }

    // ── Trail ─────────────────────────────────────────────────────────────────

    private void BuildTrail()
    {
        for (int i = 0; i < cfg.trailLength; i++)
        {
            var go  = new GameObject($"Trail_{i}");
            go.transform.SetParent(fxLayer, false);
            var img = go.AddComponent<Image>();
            img.sprite        = SpriteGenerator.Circle();
            img.color         = Color.clear;
            img.raycastTarget = false;
            var rt            = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            trail.Add(new TrailDot { rt = rt, img = img });
        }
    }

    private void UpdateTrail()
    {
        trailTimer += Time.deltaTime;
        if (trailTimer < TrailInterval) return;
        trailTimer = 0f;

        Vector2 head = ballRT.anchoredPosition - shakeOffset;
        for (int i = cfg.trailLength - 1; i > 0; i--)
        {
            var d = trail[i]; d.pos = trail[i - 1].pos; trail[i] = d;
        }
        var h = trail[0]; h.pos = head; trail[0] = h;

        float d2 = cfg.ballRadius * 2f;
        for (int i = 0; i < cfg.trailLength; i++)
        {
            float t = (float)i / cfg.trailLength;
            trail[i].rt.anchoredPosition = trail[i].pos;
            trail[i].rt.sizeDelta        = Vector2.one * d2 * Mathf.Lerp(0.92f, 0.12f, t);
            trail[i].img.color           = new Color(1f, 1f, 1f,
                                               Mathf.Lerp(cfg.trailMaxAlpha, 0f, t));
        }
    }

    // ── Pool / Particules / Ripple ────────────────────────────────────────────

    private void BuildPool()
    {
        for (int i = 0; i < PoolSize; i++)
        {
            var go  = new GameObject($"P_{i}");
            go.transform.SetParent(fxLayer, false);
            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            img.color         = Color.clear;
            var rt            = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            go.SetActive(false);
            pool.Enqueue(img);
        }
    }

    private void Burst(Vector2 origin, int count, float speed)
    {
        for (int i = 0; i < count && pool.Count > 0; i++)
            StartCoroutine(AnimParticle(pool.Dequeue(), origin, speed));
    }

    private IEnumerator AnimParticle(Image img, Vector2 origin, float speed)
    {
        img.gameObject.SetActive(true);
        img.sprite = SpriteGenerator.Circle();
        float ang  = Random.Range(0f, Mathf.PI * 2f);
        var   dir  = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
        float life = Random.Range(0.22f, 0.42f);
        float sz   = Random.Range(4f, 10f);
        img.rectTransform.sizeDelta = Vector2.one * sz;

        float e = 0f;
        while (e < life)
        {
            e += Time.deltaTime;
            img.rectTransform.anchoredPosition = origin + dir * speed * e;
            img.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0f, e / life));
            yield return null;
        }
        img.color = Color.clear;
        img.gameObject.SetActive(false);
        pool.Enqueue(img);
    }

    private void SpawnRipple(Vector2 pos, float maxR, float dur)
    {
        if (pool.Count == 0) return;
        StartCoroutine(AnimRipple(pool.Dequeue(), pos, maxR, dur));
    }

    private IEnumerator AnimRipple(Image img, Vector2 pos, float maxR, float dur)
    {
        img.gameObject.SetActive(true);
        img.sprite = CircleArenaSetup.CreateRingSprite(128, 3f, maxR);
        img.rectTransform.anchoredPosition = pos;

        float e = 0f;
        while (e < dur)
        {
            e += Time.deltaTime;
            float t = e / dur;
            img.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(0f, maxR * 2f, t);
            img.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.85f, 0f, t * t));
            yield return null;
        }
        img.color = Color.clear;
        img.gameObject.SetActive(false);
        pool.Enqueue(img);
    }

    // ── UI ────────────────────────────────────────────────────────────────────

    private void BuildUI()
    {
        var go    = new GameObject("ScoreText");
        go.transform.SetParent(uiLayer, false);
        scoreText = go.AddComponent<TextMeshProUGUI>();
        scoreText.text            = "0";
        scoreText.fontSize        = 80f;
        scoreText.fontStyle       = FontStyles.Bold;
        scoreText.color           = Color.white;
        scoreText.alignment       = TextAlignmentOptions.Right;
        scoreText.raycastTarget   = false;

        scoreRT                   = scoreText.rectTransform;
        scoreRT.anchorMin         = new Vector2(1f, 1f);
        scoreRT.anchorMax         = new Vector2(1f, 1f);
        scoreRT.pivot             = new Vector2(1f, 1f);
        scoreRT.sizeDelta         = new Vector2(220f, 110f);
        scoreRT.anchoredPosition  = new Vector2(-44f, -55f);
    }

    private void ShowGameOver()
    {
        var panel = new GameObject("GameOverPanel");
        panel.transform.SetParent(uiLayer, false);

        var bg    = panel.AddComponent<Image>();
        bg.sprite = SpriteGenerator.CreateWhiteSquare();
        bg.color  = new Color(0f, 0f, 0f, 0f);
        Stretch(bg.rectTransform);

        AddLabel(panel.transform, "FIN",            100f, FontStyles.Bold, new Vector2(0f,  120f), new Vector2(700f, 130f));
        AddLabel(panel.transform, score.ToString(), 160f, FontStyles.Bold, new Vector2(0f,  -10f), new Vector2(700f, 200f));
        AddRetryBtn(panel.transform);

        StartCoroutine(FadePanel(bg));
    }

    private IEnumerator FadePanel(Image bg)
    {
        Color target = new Color(0f, 0f, 0f, 0.86f);
        float e = 0f, dur = 0.35f;
        while (e < dur)
        {
            e += Time.deltaTime;
            bg.color = Color.Lerp(Color.clear, target, Mathf.SmoothStep(0f, 1f, e / dur));
            yield return null;
        }
        bg.color = target;
    }

    private void AddLabel(Transform parent, string text, float size, FontStyles style,
                          Vector2 pos, Vector2 delta)
    {
        var go  = new GameObject("Lbl");
        go.transform.SetParent(parent, false);
        var t   = go.AddComponent<TextMeshProUGUI>();
        t.text          = text;
        t.fontSize      = size;
        t.fontStyle     = style;
        t.color         = Color.white;
        t.alignment     = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = delta;
    }

    private void AddRetryBtn(Transform parent)
    {
        var go  = new GameObject("RetryBtn");
        go.transform.SetParent(parent, false);
        var bg  = go.AddComponent<Image>();
        bg.sprite = SpriteGenerator.CreateWhiteSquare();
        bg.color  = Color.white;
        var rt    = bg.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(340f, 95f);
        rt.anchoredPosition = new Vector2(0f, -185f);

        var lGO  = new GameObject("Label");
        lGO.transform.SetParent(go.transform, false);
        var lbl  = lGO.AddComponent<TextMeshProUGUI>();
        lbl.text      = "REJOUER";
        lbl.fontSize  = 44f;
        lbl.fontStyle = FontStyles.Bold;
        lbl.color     = Color.black;
        lbl.alignment = TextAlignmentOptions.Center;
        var lrt = lbl.rectTransform;
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.onClick.AddListener(() =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        });
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}

#if false // Legacy duplicate class body — kept for reference, not compiled

    private Canvas        canvas;
    private float         arenaRadius;
    private RectTransform ballRT;
    private RectTransform fxLayer;
    private Image         flashImage;
    private RectTransform uiLayer;
    private float         ballRadius;

    // ── État de jeu ───────────────────────────────────────────────────────────

    private enum GameState { Playing, Dead, GameOver }

    private GameState state         = GameState.Playing;
    private float     ballAngle     = 90f;         // degrés, 0 = droite
    private float     ballOrbitR;                  // rayon courant (peut varier lors du saut)
    private bool      isJumping     = false;
    private float     survivalTime  = 0f;
    private int       score         = 0;
    private int       lastDisplayedScore = -1;

    // ── Obstacles ─────────────────────────────────────────────────────────────

    private readonly List<ArenaObstacleData> obstacles = new();
    private float nextSpawnDelay;
    private float spawnTimer;

    // ── UI ────────────────────────────────────────────────────────────────────

    private TextMeshProUGUI scoreText;
    private RectTransform   scoreRT;
    private GameObject      gameOverPanel;
    private TextMeshProUGUI gameOverScoreText;

    // ── Trail ─────────────────────────────────────────────────────────────────

    private readonly List<TrailDot> trailDots = new();
    private const int TrailLength = 10;
    private float trailTimer;
    private const float TrailInterval = 0.016f;

    // ── Particules ────────────────────────────────────────────────────────────

    private readonly Queue<Image> particlePool = new();
    private const int ParticlePoolSize = 40;

    // ── Shake ─────────────────────────────────────────────────────────────────

    private float shakeTimer;
    private float shakeMagnitude;
    private Vector2 shakeOffset;

    // ── Input ─────────────────────────────────────────────────────────────────

    private bool inputDown;

    // ── Initialisation ────────────────────────────────────────────────────────

    /// <summary>Appelé par CircleArenaSetup pour injecter toutes les dépendances.</summary>
    public void Init(Canvas canvas, float arenaRadius, RectTransform ballRT,
                     RectTransform fxLayer, Image flashImage,
                     RectTransform uiLayer, float ballRadius)
    {
        this.canvas      = canvas;
        this.arenaRadius = arenaRadius;
        this.ballRT      = ballRT;
        this.fxLayer     = fxLayer;
        this.flashImage  = flashImage;
        this.uiLayer     = uiLayer;
        this.ballRadius  = ballRadius;

        ballOrbitR    = arenaRadius - ballRadius - 4f;
        nextSpawnDelay = ObstacleSpawnDelay;
        spawnTimer     = 0f;

        BuildParticlePool();
        BuildTrail();
        BuildUI();

        PlaceBall();
    }

    // ── Update ────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (state == GameState.GameOver) return;

        HandleInput();

        if (state == GameState.Playing)
        {
            survivalTime += Time.deltaTime;
            UpdateScore();
            UpdateBallPosition();
            UpdateTrail();
            UpdateObstacles();
            UpdateShake();
            CheckCollisions();
        }
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    private void HandleInput()
    {
        bool pressed = false;

        // Touch
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            pressed = true;

        // Souris (éditeur)
        if (Input.GetMouseButtonDown(0))
            pressed = true;

        if (pressed && state == GameState.Playing && !isJumping)
        {
            StartCoroutine(DoJump());
        }
    }

    // ── Mouvement de la balle ─────────────────────────────────────────────────

    private float CurrentSpeed =>
        Mathf.Lerp(BallBaseSpeed, BallMaxSpeed, Mathf.Clamp01(survivalTime / SpeedRampDuration));

    private void UpdateBallPosition()
    {
        if (!isJumping)
            ballAngle += CurrentSpeed * Time.deltaTime;

        PlaceBall();
    }

    private void PlaceBall()
    {
        float rad = ballAngle * Mathf.Deg2Rad;
        Vector2 pos = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * ballOrbitR;
        ballRT.anchoredPosition = pos + shakeOffset;
    }

    // ── Saut ──────────────────────────────────────────────────────────────────

    private IEnumerator DoJump()
    {
        isJumping = true;
        float targetR   = arenaRadius * (1f - JumpInnerDistance);
        float startR    = ballOrbitR;
        float returnR   = arenaRadius - ballRadius - 4f;

        // Squash au départ
        StartCoroutine(SquashBall(0.7f, 1.3f, 0.08f));

        // Ripple au point de départ
        SpawnRipple(ballRT.anchoredPosition - shakeOffset, 30f, 0.35f);

        // Aller vers le centre
        float e = 0f;
        while (e < JumpDuration)
        {
            e += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(e / JumpDuration));
            ballOrbitR = Mathf.Lerp(startR, targetR, t);
            ballAngle += CurrentSpeed * Time.deltaTime;
            PlaceBall();
            yield return null;
        }

        ballOrbitR = targetR;

        // Stretch au retour
        StartCoroutine(SquashBall(1.3f, 0.7f, 0.06f));

        // Retour sur l'orbite
        e = 0f;
        while (e < JumpReturnDuration)
        {
            e += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(e / JumpReturnDuration));
            ballOrbitR = Mathf.Lerp(targetR, returnR, t);
            ballAngle += CurrentSpeed * Time.deltaTime;
            PlaceBall();
            yield return null;
        }

        ballOrbitR = returnR;

        // Ripple au retour sur le bord
        SpawnRipple(ballRT.anchoredPosition - shakeOffset, 20f, 0.25f);

        isJumping = false;
    }

    // ── Squash & Stretch ──────────────────────────────────────────────────────

    private IEnumerator SquashBall(float scaleX, float scaleY, float duration)
    {
        float baseSize = ballRadius * 2f;
        float e = 0f;
        while (e < duration)
        {
            e += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(e / duration));
            float sx = Mathf.Lerp(scaleX, 1f, t);
            float sy = Mathf.Lerp(scaleY, 1f, t);
            ballRT.sizeDelta = new Vector2(baseSize * sx, baseSize * sy);
            yield return null;
        }
        ballRT.sizeDelta = Vector2.one * baseSize;
    }

    // ── Score ─────────────────────────────────────────────────────────────────

    private void UpdateScore()
    {
        int newScore = Mathf.FloorToInt(survivalTime);
        if (newScore != lastDisplayedScore)
        {
            lastDisplayedScore = newScore;
            score = newScore;
            scoreText.text = score.ToString();
            StartCoroutine(PulseScore());
        }
    }

    private IEnumerator PulseScore()
    {
        float e = 0f, dur = 0.18f;
        Vector3 baseScale = Vector3.one;
        Vector3 peakScale = Vector3.one * 1.35f;
        while (e < dur)
        {
            e += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(e / dur));
            float s = t < 0.5f ? Mathf.Lerp(1f, 1.35f, t * 2f) : Mathf.Lerp(1.35f, 1f, (t - 0.5f) * 2f);
            scoreRT.localScale = Vector3.one * s;
            yield return null;
        }
        scoreRT.localScale = baseScale;
    }

    // ── Obstacles ─────────────────────────────────────────────────────────────

    private void UpdateObstacles()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= nextSpawnDelay)
        {
            spawnTimer = 0f;
            SpawnObstacle();
            // Délai décroît avec le temps → difficulté croissante
            float progress = Mathf.Clamp01(survivalTime / SpeedRampDuration);
            nextSpawnDelay = Mathf.Lerp(ObstacleSpawnDelay, ObstacleMinDelay, progress);
        }
    }

    private void SpawnObstacle()
    {
        // Angle opposé à la balle + aléatoire dans l'arc avant
        float angle = ballAngle + 160f + Random.Range(-40f, 40f);

        var data = new ArenaObstacleData
        {
            angle     = angle,
            halfArc   = ObstacleWidth * 0.5f,
            depth     = ObstacleDepth,
            rt        = BuildObstacleRect(angle),
            dodged    = false,
        };

        obstacles.Add(data);
    }

    private RectTransform BuildObstacleRect(float angle)
    {
        var go  = new GameObject("Obstacle");
        go.transform.SetParent(canvas.transform, false);

        var img = go.AddComponent<Image>();
        img.sprite        = SpriteGenerator.CreateWhiteSquare();
        img.color         = Color.white;
        img.raycastTarget = false;

        var rt = img.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);

        PositionObstacle(rt, angle);
        return rt;
    }

    private void PositionObstacle(RectTransform rt, float angleDeg)
    {
        float rad       = angleDeg * Mathf.Deg2Rad;
        float midR      = arenaRadius - ObstacleDepth * 0.5f - 3f;
        Vector2 center  = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * midR;

        // Largeur en unités canvas (arc → longueur de corde approximative)
        float arcWidth  = 2f * arenaRadius * Mathf.Sin(ObstacleWidth * 0.5f * Mathf.Deg2Rad);

        rt.anchoredPosition = center;
        rt.sizeDelta        = new Vector2(arcWidth, ObstacleDepth);
        rt.localRotation    = Quaternion.Euler(0f, 0f, angleDeg + 90f);
    }

    // ── Collisions ────────────────────────────────────────────────────────────

    private void CheckCollisions()
    {
        Vector2 ballPos = ballRT.anchoredPosition - shakeOffset;
        float ballOrbit = ballOrbitR;

        for (int i = obstacles.Count - 1; i >= 0; i--)
        {
            var obs = obstacles[i];

            // Angle relatif balle ↔ obstacle
            float diff = Mathf.DeltaAngle(ballAngle, obs.angle);
            float absD = Mathf.Abs(diff);

            // Perfect dodge check : balle passe dans la fenêtre en sautant
            if (!obs.dodged && isJumping && absD < DodgeWindowDeg)
            {
                obs.dodged = true;
                obstacles[i] = obs;
                StartCoroutine(DodgeFeedback(ballPos));
                continue;
            }

            // Collision : balle sur l'orbite ET dans l'arc de l'obstacle
            bool inArc    = absD < obs.halfArc + 8f;
            bool inDepth  = ballOrbit > arenaRadius - obs.depth - ballRadius - 8f;

            if (inArc && inDepth && !isJumping)
            {
                // Flash blanc proche (warning)
                if (absD < obs.halfArc + 22f)
                    StartCoroutine(NearMissFlash(0.18f));

                if (absD < obs.halfArc + 2f)
                {
                    StartCoroutine(OnHitObstacle());
                    return;
                }
            }
        }
    }

    // ── Hit ───────────────────────────────────────────────────────────────────

    private IEnumerator OnHitObstacle()
    {
        state = GameState.Dead;

        // Screen shake
        StartCoroutine(Shake(14f, 0.35f));

        // Flash blanc
        StartCoroutine(FlashScreen(0.6f, 0.25f));

        // Slow motion
        Time.timeScale = SlowMoScale;
        yield return new WaitForSecondsRealtime(SlowMoDuration);
        Time.timeScale = 1f;

        state = GameState.GameOver;
        ShowGameOver();
    }

    // ── Feedbacks ─────────────────────────────────────────────────────────────

    private IEnumerator DodgeFeedback(Vector2 pos)
    {
        SpawnRipple(pos, 40f, 0.45f);
        SpawnParticleBurst(pos, 6, 60f, Color.white);
        yield return null;
    }

    private IEnumerator NearMissFlash(float alpha)
    {
        flashImage.color = new Color(1f, 1f, 1f, alpha);
        float e = 0f, dur = 0.12f;
        while (e < dur)
        {
            e += Time.deltaTime;
            flashImage.color = new Color(1f, 1f, 1f, Mathf.Lerp(alpha, 0f, e / dur));
            yield return null;
        }
        flashImage.color = Color.clear;
    }

    private IEnumerator FlashScreen(float alpha, float duration)
    {
        flashImage.color = new Color(1f, 1f, 1f, alpha);
        float e = 0f;
        while (e < duration)
        {
            e += Time.unscaledDeltaTime;
            flashImage.color = new Color(1f, 1f, 1f, Mathf.Lerp(alpha, 0f, e / duration));
            yield return null;
        }
        flashImage.color = Color.clear;
    }

    private IEnumerator Shake(float magnitude, float duration)
    {
        shakeMagnitude = magnitude;
        shakeTimer     = duration;
        yield return null;
    }

    private void UpdateShake()
    {
        if (shakeTimer > 0f)
        {
            shakeTimer    -= Time.deltaTime;
            float strength = shakeMagnitude * (shakeTimer / 0.35f);
            shakeOffset    = new Vector2(
                Random.Range(-strength, strength),
                Random.Range(-strength, strength)
            );
        }
        else
        {
            shakeOffset    = Vector2.zero;
            shakeMagnitude = 0f;
        }
    }

    // ── Trail ─────────────────────────────────────────────────────────────────

    private void BuildTrail()
    {
        for (int i = 0; i < TrailLength; i++)
        {
            var go  = new GameObject($"Trail_{i}");
            go.transform.SetParent(fxLayer, false);
            var img = go.AddComponent<Image>();
            img.sprite        = SpriteGenerator.Circle();
            img.color         = Color.clear;
            img.raycastTarget = false;

            var rt            = img.rectTransform;
            rt.anchorMin      = new Vector2(0.5f, 0.5f);
            rt.anchorMax      = new Vector2(0.5f, 0.5f);
            rt.pivot          = new Vector2(0.5f, 0.5f);
            rt.sizeDelta      = Vector2.one * (ballRadius * 2f);

            trailDots.Add(new TrailDot { img = img, rt = rt });
        }
    }

    private void UpdateTrail()
    {
        trailTimer += Time.deltaTime;
        if (trailTimer < TrailInterval) return;
        trailTimer = 0f;

        Vector2 ballPos = ballRT.anchoredPosition - shakeOffset;

        // Décale les points du trail
        for (int i = TrailLength - 1; i > 0; i--)
        {
            var dot = trailDots[i];
            dot.pos = trailDots[i - 1].pos;
            trailDots[i] = dot;
        }
        var head = trailDots[0];
        head.pos = ballPos;
        trailDots[0] = head;

        // Met à jour l'affichage
        for (int i = 0; i < TrailLength; i++)
        {
            float alpha = Mathf.Lerp(0.38f, 0f, (float)i / TrailLength);
            float scale = Mathf.Lerp(1f, 0.3f, (float)i / TrailLength);
            trailDots[i].rt.anchoredPosition = trailDots[i].pos;
            trailDots[i].rt.sizeDelta        = Vector2.one * ballRadius * 2f * scale;
            trailDots[i].img.color           = new Color(1f, 1f, 1f, alpha);
        }
    }

    // ── Particules ────────────────────────────────────────────────────────────

    private void BuildParticlePool()
    {
        for (int i = 0; i < ParticlePoolSize; i++)
        {
            var go  = new GameObject($"Particle_{i}");
            go.transform.SetParent(fxLayer, false);
            var img = go.AddComponent<Image>();
            img.sprite        = SpriteGenerator.Circle();
            img.color         = Color.clear;
            img.raycastTarget = false;

            var rt            = img.rectTransform;
            rt.anchorMin      = new Vector2(0.5f, 0.5f);
            rt.anchorMax      = new Vector2(0.5f, 0.5f);
            rt.pivot          = new Vector2(0.5f, 0.5f);
            rt.sizeDelta      = Vector2.one * 8f;

            go.SetActive(false);
            particlePool.Enqueue(img);
        }
    }

    private void SpawnParticleBurst(Vector2 origin, int count, float speed, Color color)
    {
        for (int i = 0; i < count; i++)
        {
            if (particlePool.Count == 0) break;
            var img = particlePool.Dequeue();
            StartCoroutine(AnimateParticle(img, origin, speed, color));
        }
    }

    private IEnumerator AnimateParticle(Image img, Vector2 origin, float speed, Color color)
    {
        img.gameObject.SetActive(true);
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        var dir     = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        float life  = Random.Range(0.25f, 0.45f);
        float size  = Random.Range(4f, 9f);
        img.rectTransform.sizeDelta = Vector2.one * size;

        float e = 0f;
        while (e < life)
        {
            e += Time.deltaTime;
            float t = e / life;
            img.rectTransform.anchoredPosition = origin + dir * speed * e;
            img.color = new Color(color.r, color.g, color.b, Mathf.Lerp(1f, 0f, t * t));
            yield return null;
        }

        img.color = Color.clear;
        img.gameObject.SetActive(false);
        particlePool.Enqueue(img);
    }

    // ── Ripple ────────────────────────────────────────────────────────────────

    private void SpawnRipple(Vector2 pos, float maxRadius, float duration)
    {
        if (particlePool.Count == 0) return;
        var img = particlePool.Dequeue();
        StartCoroutine(AnimateRipple(img, pos, maxRadius, duration));
    }

    private IEnumerator AnimateRipple(Image img, Vector2 pos, float maxR, float duration)
    {
        img.gameObject.SetActive(true);
        img.rectTransform.anchoredPosition = pos;
        img.sprite = CircleArenaSetup.CreateRingSprite(128, 0.08f);

        float e = 0f;
        while (e < duration)
        {
            e += Time.deltaTime;
            float t = Mathf.Clamp01(e / duration);
            float r = Mathf.Lerp(0f, maxR, t);
            float a = Mathf.Lerp(0.9f, 0f, t * t);
            img.rectTransform.sizeDelta = Vector2.one * r * 2f;
            img.color = new Color(1f, 1f, 1f, a);
            yield return null;
        }

        img.color = Color.clear;
        img.gameObject.SetActive(false);
        particlePool.Enqueue(img);
    }

    // ── UI ────────────────────────────────────────────────────────────────────

    private void BuildUI()
    {
        // Score — coin supérieur droit
        var scoreGO     = new GameObject("ScoreText");
        scoreGO.transform.SetParent(uiLayer, false);
        scoreText       = scoreGO.AddComponent<TextMeshProUGUI>();
        scoreText.text  = "0";
        scoreText.fontSize        = 72f;
        scoreText.fontStyle       = FontStyles.Bold;
        scoreText.color           = Color.white;
        scoreText.alignment       = TextAlignmentOptions.Right;
        scoreText.raycastTarget   = false;

        scoreRT         = scoreText.rectTransform;
        scoreRT.anchorMin        = new Vector2(1f, 1f);
        scoreRT.anchorMax        = new Vector2(1f, 1f);
        scoreRT.pivot            = new Vector2(1f, 1f);
        scoreRT.sizeDelta        = new Vector2(200f, 100f);
        scoreRT.anchoredPosition = new Vector2(-40f, -50f);
    }

    private void ShowGameOver()
    {
        gameOverPanel = new GameObject("GameOverPanel");
        gameOverPanel.transform.SetParent(uiLayer, false);

        var bg        = gameOverPanel.AddComponent<Image>();
        bg.color      = new Color(0f, 0f, 0f, 0.82f);
        bg.sprite     = SpriteGenerator.CreateWhiteSquare();
        var bgRT      = bg.rectTransform;
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

        // "GAME OVER"
        BuildLabel(gameOverPanel.transform, "GAME OVER", 88f, FontStyles.Bold,
                   new Vector2(0f, 80f), new Vector2(800f, 120f));

        // Score final
        BuildLabel(gameOverPanel.transform, score.ToString(), 140f, FontStyles.Bold,
                   new Vector2(0f, -30f), new Vector2(600f, 180f));

        // Bouton Rejouer
        BuildRetryButton(gameOverPanel.transform);

        StartCoroutine(FadeInPanel(bg));
    }

    private IEnumerator FadeInPanel(Image bg)
    {
        float e = 0f, dur = 0.4f;
        while (e < dur)
        {
            e += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, e / dur);
            bg.color = new Color(0f, 0f, 0f, 0.82f * t);
            yield return null;
        }
    }

    private void BuildLabel(Transform parent, string text, float size, FontStyles style,
                            Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var go  = new GameObject("Label");
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text         = text;
        tmp.fontSize     = size;
        tmp.fontStyle    = style;
        tmp.color        = Color.white;
        tmp.alignment    = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        var rt = tmp.rectTransform;
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = sizeDelta;
    }

    private void BuildRetryButton(Transform parent)
    {
        var go  = new GameObject("RetryButton");
        go.transform.SetParent(parent, false);

        // Fond blanc du bouton
        var bg        = go.AddComponent<Image>();
        bg.color      = Color.white;
        bg.sprite     = SpriteGenerator.CreateWhiteSquare();

        var rt        = bg.rectTransform;
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(320f, 90f);
        rt.anchoredPosition = new Vector2(0f, -160f);

        // Texte du bouton
        var textGO  = new GameObject("BtnLabel");
        textGO.transform.SetParent(go.transform, false);
        var tmp     = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = "REJOUER";
        tmp.fontSize  = 42f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = Color.black;
        tmp.alignment = TextAlignmentOptions.Center;

        var trt = tmp.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        // Bouton cliquable
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;

        var colors          = btn.colors;
        colors.normalColor  = Color.white;
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        btn.colors          = colors;

        btn.onClick.AddListener(RestartGame);
    }

    private void RestartGame()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    // ── Structures internes ───────────────────────────────────────────────────

    private struct TrailDot
    {
        public Image         img;
        public RectTransform rt;
        public Vector2       pos;
    }

    private struct ArenaObstacleData
    {
        public float         angle;
        public float         halfArc;
        public float         depth;
        public RectTransform rt;
        public bool          dodged;
    }
}
#endif
