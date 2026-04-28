using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Cinématique d'intro en noir/blanc/gris :
///   Phase 1 (3.0 s) — Balle qui rebondit sur un sol avec simulation d'eau (ondulations circulaires).
///   Phase 2 (0.8 s) — Fondu au noir progressif.
///   Phase 3          — Chargement de la scène Menu.
/// </summary>
public class IntroLeaf : MonoBehaviour
{
    // ── Constantes de timing ──────────────────────────────────────────────────

    private const float BallDuration  = 3.0f;
    private const float FadeDuration  = 0.8f;
    private const string TargetScene  = "Menu";

    // ── Mise en page balle ────────────────────────────────────────────────────

    private const float BallSize       = 140f;
    private const float FloorY         = -680f;
    private const float BounceHeight   = 900f;
    private const float BounceGravity  = 2.6f;

    // ── Sol ───────────────────────────────────────────────────────────────────

    private const float FloorThickness = 3f;
    private const float FloorWidth     = 1080f;

    // ── Eau / ondulations ─────────────────────────────────────────────────────

    private const float RippleSpawnInterval = 0.38f;
    private const int   MaxRipples          = 6;
    private const float RippleLifetime      = 1.2f;
    private const float RippleMaxSize       = 480f;
    private const float RippleInitSize      = 40f;

    // ── Palette noir/blanc/gris ───────────────────────────────────────────────

    private static readonly Color ColorBg       = Color.black;
    private static readonly Color ColorBall     = Color.white;
    private static readonly Color ColorFloor    = new Color(0.85f, 0.85f, 0.85f, 1f);
    private static readonly Color ColorRipple   = new Color(0.75f, 0.75f, 0.75f, 0.6f);
    private static readonly Color ColorShadow   = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    private static readonly Color ColorWaterFill = new Color(0.08f, 0.08f, 0.08f, 0.85f);

    // ── Références internes ───────────────────────────────────────────────────

    private Canvas          _canvas;
    private Image           _fadeOverlay;
    private RectTransform   _ballRT;
    private RectTransform   _shadowRT;
    private Image           _shadowImg;
    private RectTransform   _waterRT;

    private readonly List<RippleData> _ripples = new List<RippleData>();

    private struct RippleData
    {
        public Image  Image;
        public float  Age;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        BuildCanvas();
        StartCoroutine(PlaySequence());
    }

    // ── Construction de la scène ──────────────────────────────────────────────

    private void BuildCanvas()
    {
        var cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = ColorBg;
        }

        var canvasGO               = new GameObject("IntroCanvas");
        _canvas                    = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode         = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder       = 0;

        var scaler                 = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight  = 1f;

        canvasGO.AddComponent<GraphicRaycaster>();

        var canvasRT = _canvas.GetComponent<RectTransform>();

        // ── Fond noir ─────────────────────────────────────────────────────────
        var bgGO  = new GameObject("IntroBg");
        bgGO.transform.SetParent(canvasRT, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color       = ColorBg;
        bgImg.sprite      = SpriteGenerator.CreateWhiteSquare();
        bgImg.raycastTarget = false;
        StretchFull(bgImg.rectTransform);

        // ── Zone d'eau (fond de sol) ──────────────────────────────────────────
        var waterGO  = new GameObject("Water");
        waterGO.transform.SetParent(canvasRT, false);
        var waterImg = waterGO.AddComponent<Image>();
        waterImg.sprite      = SpriteGenerator.CreateWhiteSquare();
        waterImg.color       = ColorWaterFill;
        waterImg.raycastTarget = false;
        _waterRT             = waterImg.rectTransform;
        _waterRT.anchorMin   = new Vector2(0f, 0f);
        _waterRT.anchorMax   = new Vector2(1f, 0f);
        _waterRT.pivot       = new Vector2(0.5f, 1f);
        _waterRT.sizeDelta   = new Vector2(0f, 220f);
        _waterRT.anchoredPosition = new Vector2(0f, FloorY);

        // ── Sol (ligne horizontale) ───────────────────────────────────────────
        var floorGO  = new GameObject("Floor");
        floorGO.transform.SetParent(canvasRT, false);
        var floorImg = floorGO.AddComponent<Image>();
        floorImg.sprite      = SpriteGenerator.CreateWhiteSquare();
        floorImg.color       = ColorFloor;
        floorImg.raycastTarget = false;
        var floorRT          = floorImg.rectTransform;
        floorRT.anchorMin    = new Vector2(0.5f, 0f);
        floorRT.anchorMax    = new Vector2(0.5f, 0f);
        floorRT.pivot        = new Vector2(0.5f, 1f);
        floorRT.sizeDelta    = new Vector2(FloorWidth, FloorThickness);
        floorRT.anchoredPosition = new Vector2(0f, FloorY);

        // ── Ondulations pool ──────────────────────────────────────────────────
        for (int i = 0; i < MaxRipples; i++)
        {
            var rGO  = new GameObject($"Ripple_{i}");
            rGO.transform.SetParent(canvasRT, false);
            var rImg = rGO.AddComponent<Image>();
            rImg.sprite      = SpriteGenerator.CreateRing(128, 0.08f);
            rImg.color       = new Color(ColorRipple.r, ColorRipple.g, ColorRipple.b, 0f);
            rImg.raycastTarget = false;
            var rRT          = rImg.rectTransform;
            rRT.anchorMin    = new Vector2(0.5f, 0f);
            rRT.anchorMax    = new Vector2(0.5f, 0f);
            rRT.pivot        = new Vector2(0.5f, 0.5f);
            rRT.sizeDelta    = Vector2.one * RippleInitSize;
            rRT.anchoredPosition = new Vector2(0f, FloorY);
            rImg.gameObject.SetActive(false);
            _ripples.Add(new RippleData { Image = rImg, Age = RippleLifetime + 1f });
        }

        // ── Ombre de la balle ─────────────────────────────────────────────────
        var shadowGO  = new GameObject("BallShadow");
        shadowGO.transform.SetParent(canvasRT, false);
        _shadowImg    = shadowGO.AddComponent<Image>();
        _shadowImg.sprite      = SpriteGenerator.CreateCircle(128);
        _shadowImg.color       = ColorShadow;
        _shadowImg.raycastTarget = false;
        _shadowRT    = _shadowImg.rectTransform;
        _shadowRT.anchorMin    = new Vector2(0.5f, 0f);
        _shadowRT.anchorMax    = new Vector2(0.5f, 0f);
        _shadowRT.pivot        = new Vector2(0.5f, 0.5f);
        _shadowRT.sizeDelta    = new Vector2(BallSize * 1.4f, BallSize * 0.35f);
        _shadowRT.anchoredPosition = new Vector2(0f, FloorY - BallSize * 0.05f);

        // ── Balle ─────────────────────────────────────────────────────────────
        var ballGO  = new GameObject("Ball");
        ballGO.transform.SetParent(canvasRT, false);
        var ballImg = ballGO.AddComponent<Image>();
        ballImg.sprite      = SpriteGenerator.CreateCircle(128);
        ballImg.color       = ColorBall;
        ballImg.raycastTarget = false;
        _ballRT             = ballImg.rectTransform;
        _ballRT.anchorMin   = new Vector2(0.5f, 0f);
        _ballRT.anchorMax   = new Vector2(0.5f, 0f);
        _ballRT.pivot       = new Vector2(0.5f, 0.5f);
        _ballRT.sizeDelta   = new Vector2(BallSize, BallSize);
        _ballRT.anchoredPosition = new Vector2(0f, FloorY + BounceHeight + BallSize * 0.5f);

        // ── Overlay de fondu (au-dessus de tout) ─────────────────────────────
        var fadeGO  = new GameObject("FadeOverlay");
        fadeGO.transform.SetParent(canvasRT, false);
        _fadeOverlay        = fadeGO.AddComponent<Image>();
        _fadeOverlay.sprite = SpriteGenerator.CreateWhiteSquare();
        _fadeOverlay.color  = new Color(0f, 0f, 0f, 0f);
        _fadeOverlay.raycastTarget = false;
        StretchFull(_fadeOverlay.rectTransform);
    }

    // ── Séquence principale ───────────────────────────────────────────────────

    private IEnumerator PlaySequence()
    {
        float elapsed         = 0f;
        float rippleTimer     = 0f;
        float bounceVelocity  = 0f;
        float ballY           = FloorY + BounceHeight + BallSize * 0.5f;
        bool  onGround        = false;

        // Vitesse initiale vers le bas (chute libre)
        bounceVelocity = -Mathf.Sqrt(2f * BounceGravity * 1000f * BounceHeight / 1920f) * 1920f;

        while (elapsed < BallDuration)
        {
            elapsed    += Time.deltaTime;
            rippleTimer += Time.deltaTime;

            // ── Physique de rebond ─────────────────────────────────────────────
            bounceVelocity += BounceGravity * 1000f * Time.deltaTime;
            ballY          -= bounceVelocity * Time.deltaTime;

            float groundY = FloorY + BallSize * 0.5f;
            if (ballY <= groundY)
            {
                ballY          = groundY;
                bounceVelocity = -bounceVelocity * 0.72f;
                onGround       = true;
            }
            else
            {
                onGround = false;
            }

            // ── Position balle ────────────────────────────────────────────────
            _ballRT.anchoredPosition = new Vector2(0f, ballY);

            // ── Écrasement/étirement de la balle ─────────────────────────────
            float distFromGround = (ballY - groundY) / BounceHeight;
            float squash         = onGround
                ? 1f + Mathf.Abs(bounceVelocity) / 4000f
                : 1f + Mathf.Clamp(distFromGround * 0.12f, 0f, 0.15f);
            float squashY = onGround
                ? 1f - Mathf.Clamp(Mathf.Abs(bounceVelocity) / 6000f, 0f, 0.3f)
                : 1f;

            _ballRT.localScale = new Vector3(squash, squashY, 1f);

            // ── Ombre ────────────────────────────────────────────────────────
            float t        = Mathf.Clamp01(1f - (ballY - groundY) / BounceHeight);
            float shadowW  = Mathf.Lerp(BallSize * 0.5f, BallSize * 1.6f, t);
            float shadowH  = Mathf.Lerp(BallSize * 0.1f, BallSize * 0.4f, t);
            _shadowRT.sizeDelta = new Vector2(shadowW, shadowH);
            var shadowColor = _shadowImg.color;
            shadowColor.a = Mathf.Lerp(0.05f, 0.5f, t);
            _shadowImg.color = shadowColor;

            // ── Ondulations ──────────────────────────────────────────────────
            if (onGround || rippleTimer >= RippleSpawnInterval)
            {
                if (onGround || rippleTimer >= RippleSpawnInterval)
                {
                    SpawnRipple();
                    rippleTimer = 0f;
                }
            }
            UpdateRipples();

            yield return null;
        }

        // ── Phase 2 : fondu au noir ───────────────────────────────────────────
        elapsed = 0f;
        while (elapsed < FadeDuration)
        {
            elapsed += Time.deltaTime;
            _fadeOverlay.color = new Color(0f, 0f, 0f, Mathf.Clamp01(elapsed / FadeDuration));
            yield return null;
        }
        _fadeOverlay.color = Color.black;

        yield return SceneManager.LoadSceneAsync(TargetScene);
    }

    // ── Gestion des ondulations ───────────────────────────────────────────────

    private void SpawnRipple()
    {
        for (int i = 0; i < _ripples.Count; i++)
        {
            var r = _ripples[i];
            if (r.Age > RippleLifetime)
            {
                r.Age = 0f;
                r.Image.gameObject.SetActive(true);
                r.Image.color = new Color(ColorRipple.r, ColorRipple.g, ColorRipple.b, ColorRipple.a);
                r.Image.rectTransform.sizeDelta = Vector2.one * RippleInitSize;
                _ripples[i] = r;
                return;
            }
        }
    }

    private void UpdateRipples()
    {
        for (int i = 0; i < _ripples.Count; i++)
        {
            var r = _ripples[i];
            if (r.Age > RippleLifetime) continue;

            r.Age += Time.deltaTime;
            float progress = r.Age / RippleLifetime;
            float size     = Mathf.Lerp(RippleInitSize, RippleMaxSize, progress);
            float alpha    = ColorRipple.a * (1f - progress);

            r.Image.rectTransform.sizeDelta = new Vector2(size, size * 0.35f);
            var c = r.Image.color;
            c.a   = alpha;
            r.Image.color = c;

            if (r.Age >= RippleLifetime)
                r.Image.gameObject.SetActive(false);

            _ripples[i] = r;
        }
    }

    // ── Utilitaire ────────────────────────────────────────────────────────────

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
