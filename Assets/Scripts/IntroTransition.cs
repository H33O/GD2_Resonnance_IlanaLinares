using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Séquence d'intro cinématique jouée avant le menu principal.
/// Phases : fond blanc, balle noire monte du bas → traverse le ring noir →
/// entre dedans → inversion (fond noir, balle/ring blancs) → formes sortent →
/// réaspirées → balle grossit et remplit l'écran → menu révélé.
/// Un bouton SKIP permet de court-circuiter l'animation à tout moment.
/// </summary>
public class IntroTransition : MonoBehaviour
{
    // ── Durées des phases ─────────────────────────────────────────────────────

    private const float PhaseBallEnterDuration   = 1.8f;
    private const float PhaseRingPulseDuration   = 0.4f;
    private const float PhaseInvertDuration      = 0.6f;
    private const float PhaseShapesOutDuration   = 1.6f;
    private const float PhaseHoldDuration        = 0.6f;
    private const float PhaseShapesInDuration    = 1.2f;
    private const float PhaseFadeBlackDuration   = 0.7f;

    // ── Paramètres visuels ────────────────────────────────────────────────────

    private const float RingRadiusStart     = 60f;
    private const float RingRadiusEnd       = 180f;
    private const float RingRadiusYRatio    = 0.22f;
    private const float RingPositionY       = -680f;
    private const float RingThickness       = 8f;
    private const float BallStartY          = -900f;
    private const float BallSize            = 44f;
    private const int   ShapeCount          = 18;
    private const float ShapeSpreadRadius   = 780f;
    private const float ShapeMinSize        = 70f;
    private const float ShapeMaxSize        = 180f;
    private const float ShapeVerticalBias   = 1.6f;
    private const float PhaseBallRumbleDuration = 1.1f;

    // Taille cible de la balle pour remplir l'écran (en unités canvas 1080×1920)
    private const float BallFillDuration    = 0.55f;
    private const float BallFillSize        = 2600f;   // dépasse les deux dimensions

    // ── Références ────────────────────────────────────────────────────────────

    private Canvas          introCanvas;
    private RectTransform   canvasRT;
    private Image           background;
    private RectTransform   ringRT;
    private Image           ringImage;
    private RectTransform   ballRT;
    private Image           ballImage;
    private Image           blackOverlay;

    private readonly List<RectTransform> shapeRTs             = new();
    private readonly List<Image>         shapeImages          = new();
    private readonly List<Vector2>       shapeTargetPositions = new();
    private readonly List<float>         shapeAngularSpeeds   = new();

    private System.Action onComplete;

    // ── Contrôle du skip ──────────────────────────────────────────────────────

    private bool          skipRequested;
    private Coroutine     mainRoutine;

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>
    /// Lance la séquence d'intro dans le canvas donné.
    /// <paramref name="onComplete"/> est invoqué quand l'animation est terminée.
    /// </summary>
    public void Play(Canvas canvas, System.Action onComplete)
    {
        this.introCanvas = canvas;
        this.onComplete  = onComplete;
        canvasRT = canvas.GetComponent<RectTransform>();

        BuildScene();
        BuildSkipButton();
        mainRoutine = StartCoroutine(IntroRoutine());
    }

    // ── Bouton Skip ───────────────────────────────────────────────────────────

    private void BuildSkipButton()
    {
        var go = new GameObject("SkipButton");
        go.transform.SetParent(introCanvas.transform, false);

        var rt          = go.AddComponent<RectTransform>();
        rt.anchorMin    = new Vector2(1f, 0f);
        rt.anchorMax    = new Vector2(1f, 0f);
        rt.pivot        = new Vector2(1f, 0f);
        rt.sizeDelta    = new Vector2(180f, 72f);
        rt.anchoredPosition = new Vector2(-40f, 60f);

        var img         = go.AddComponent<Image>();
        img.sprite      = SpriteGenerator.CreateWhiteSquare();
        img.color       = new Color(1f, 1f, 1f, 0.08f);

        // Contour discret
        var outGO       = new GameObject("Outline");
        outGO.transform.SetParent(rt, false);
        var outRT       = outGO.AddComponent<RectTransform>();
        outRT.anchorMin = Vector2.zero;
        outRT.anchorMax = Vector2.one;
        outRT.offsetMin = new Vector2(-1.5f, -1.5f);
        outRT.offsetMax = new Vector2( 1.5f,  1.5f);
        var outImg      = outGO.AddComponent<Image>();
        outImg.sprite   = SpriteGenerator.CreateWhiteSquare();
        outImg.color    = new Color(1f, 1f, 1f, 0.20f);
        outImg.raycastTarget = false;
        outGO.transform.SetAsFirstSibling();

        // Label "SKIP"
        var labelGO     = new GameObject("Label");
        labelGO.transform.SetParent(rt, false);
        var labelRT     = labelGO.AddComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = labelRT.offsetMax = Vector2.zero;
        var tmp         = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text        = "SKIP";
        tmp.fontSize    = 28f;
        tmp.fontStyle   = FontStyles.Bold;
        tmp.color       = new Color(1f, 1f, 1f, 0.70f);
        tmp.alignment   = TextAlignmentOptions.Center;
        tmp.characterSpacing = 4f;
        tmp.raycastTarget = false;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var cb  = new Button.ButtonClickedEvent();
        cb.AddListener(OnSkip);
        btn.onClick = cb;
    }

    private void OnSkip()
    {
        if (skipRequested) return;
        skipRequested = true;

        if (mainRoutine != null) StopCoroutine(mainRoutine);

        // Nettoie les formes si elles existent déjà
        foreach (var sr in shapeRTs)
            if (sr != null) Destroy(sr.gameObject);
        shapeRTs.Clear();
        shapeImages.Clear();
        shapeTargetPositions.Clear();
        shapeAngularSpeeds.Clear();

        StartCoroutine(SkipToEnd());
    }

    private IEnumerator SkipToEnd()
    {
        // Met le fond noir et la balle blanche immédiatement
        background.color = Color.black;
        ringImage.gameObject.SetActive(false);
        ballImage.color  = Color.white;
        ballRT.anchoredPosition = new Vector2(0f, RingPositionY);
        ballRT.sizeDelta = Vector2.one * BallSize;

        yield return StartCoroutine(PhaseBallFillScreen());

        Destroy(introCanvas.gameObject);
        onComplete?.Invoke();
        Destroy(this);
    }

    // ── Construction de la scène d'intro ──────────────────────────────────────

    private void BuildScene()
    {
        var bgGO    = new GameObject("IntroBg");
        bgGO.transform.SetParent(introCanvas.transform, false);
        background  = bgGO.AddComponent<Image>();
        background.sprite        = SpriteGenerator.CreateWhiteSquare();
        background.color         = Color.white;
        background.raycastTarget = false;
        Stretch(background.rectTransform);

        var ringGO  = new GameObject("IntroRing");
        ringGO.transform.SetParent(introCanvas.transform, false);
        ringImage   = ringGO.AddComponent<Image>();
        ringImage.sprite        = SpriteGenerator.CreateRing(128, RingThickness / RingRadiusEnd);
        ringImage.color         = Color.black;
        ringImage.raycastTarget = false;
        ringRT      = ringImage.rectTransform;
        ringRT.anchorMin        = new Vector2(0.5f, 0.5f);
        ringRT.anchorMax        = new Vector2(0.5f, 0.5f);
        ringRT.pivot            = new Vector2(0.5f, 0.5f);
        ringRT.sizeDelta        = new Vector2(RingRadiusStart * 2f, RingRadiusStart * 2f * RingRadiusYRatio);
        ringRT.anchoredPosition = new Vector2(0f, RingPositionY);

        var ballGO  = new GameObject("IntroBall");
        ballGO.transform.SetParent(introCanvas.transform, false);
        ballImage   = ballGO.AddComponent<Image>();
        ballImage.sprite        = SpriteGenerator.Circle();
        ballImage.color         = Color.black;
        ballImage.raycastTarget = false;
        ballRT      = ballImage.rectTransform;
        ballRT.anchorMin        = new Vector2(0.5f, 0.5f);
        ballRT.anchorMax        = new Vector2(0.5f, 0.5f);
        ballRT.pivot            = new Vector2(0.5f, 0.5f);
        ballRT.sizeDelta        = Vector2.one * BallSize;
        ballRT.anchoredPosition = new Vector2(0f, BallStartY);

        // Overlay noir léger — sert uniquement de bloqueur de raycasts pendant l'intro
        var overlayGO   = new GameObject("IntroBlackOverlay");
        overlayGO.transform.SetParent(introCanvas.transform, false);
        blackOverlay    = overlayGO.AddComponent<Image>();
        blackOverlay.sprite         = SpriteGenerator.CreateWhiteSquare();
        blackOverlay.color          = new Color(0f, 0f, 0f, 0f);
        blackOverlay.raycastTarget  = true;
        Stretch(blackOverlay.rectTransform);
    }

    // ── Séquence principale ───────────────────────────────────────────────────

    private IEnumerator IntroRoutine()
    {
        yield return StartCoroutine(PhaseBallEnter());
        yield return StartCoroutine(PhaseRingPulse());
        yield return StartCoroutine(PhaseInvert());

        SpawnShapes();
        yield return StartCoroutine(PhaseShapesExpand());
        yield return new WaitForSeconds(PhaseHoldDuration);
        yield return StartCoroutine(PhaseShapesCollapse());
        yield return StartCoroutine(PhaseBallRumble());

        // Remplacement du fondu noir : la balle blanche grandit et remplit l'écran
        yield return StartCoroutine(PhaseBallFillScreen());

        Destroy(introCanvas.gameObject);
        onComplete?.Invoke();
        Destroy(this);
    }

    // ── Phase 1 : La balle noire monte du bas et traverse le ring noir ────────

    private IEnumerator PhaseBallEnter()
    {
        float elapsed    = 0f;
        Vector2 startPos = new Vector2(0f, BallStartY);
        Vector2 endPos   = new Vector2(0f, RingPositionY);

        float ringStart  = RingRadiusStart;
        float ringPeek   = RingRadiusEnd * 0.55f;

        while (elapsed < PhaseBallEnterDuration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.Clamp01(elapsed / PhaseBallEnterDuration);
            float s  = Mathf.SmoothStep(0f, 1f, t);

            ballRT.anchoredPosition = Vector2.Lerp(startPos, endPos, s);

            float ringT = Mathf.Sin(t * Mathf.PI);
            float ringR = Mathf.Lerp(ringStart, ringPeek, ringT);
            ringRT.sizeDelta = new Vector2(ringR * 2f, ringR * 2f * RingRadiusYRatio);

            if (t > 0.65f)
            {
                float enterT = (t - 0.65f) / 0.35f;
                float ballS  = Mathf.Lerp(1f, 0.4f, Mathf.SmoothStep(0f, 1f, enterT));
                ballRT.sizeDelta = Vector2.one * BallSize * ballS;
            }

            yield return null;
        }

        ballRT.anchoredPosition = endPos;
        ballRT.sizeDelta        = Vector2.one * BallSize;
    }

    // ── Phase 2 : Pulse du ring ───────────────────────────────────────────────

    private IEnumerator PhaseRingPulse()
    {
        float elapsed   = 0f;
        float ringStart = ringRT.sizeDelta.x * 0.5f;
        float ringPeak  = ringStart * 1.35f;

        while (elapsed < PhaseRingPulseDuration)
        {
            elapsed += Time.deltaTime;
            float t     = Mathf.Clamp01(elapsed / PhaseRingPulseDuration);
            float pulse = Mathf.Sin(t * Mathf.PI);
            float r     = Mathf.Lerp(ringStart, ringPeak, pulse);
            ringRT.sizeDelta = new Vector2(r * 2f, r * 2f * RingRadiusYRatio);
            yield return null;
        }

        ringRT.sizeDelta = new Vector2(RingRadiusEnd * 2f, RingRadiusEnd * 2f * RingRadiusYRatio);
    }

    // ── Phase 3 : Inversion ────────────────────────────────────────────────────

    private IEnumerator PhaseInvert()
    {
        float elapsed = 0f;

        while (elapsed < PhaseInvertDuration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / PhaseInvertDuration));

            background.color = Color.Lerp(Color.white, Color.black, t);
            ringImage.color  = Color.Lerp(Color.black, Color.white, t);
            ballImage.color  = Color.Lerp(Color.black, Color.white, t);

            yield return null;
        }

        background.color = Color.black;
        ringImage.color  = Color.white;
        ballImage.color  = Color.white;
    }

    // ── Phase 4 : Formes sortent du cercle ───────────────────────────────────

    private void SpawnShapes()
    {
        int[]    sides   = { 3, 4, 5, 6, 4, 6, 3, 4 };

        for (int i = 0; i < ShapeCount; i++)
        {
            int   shapeSides = sides[i % sides.Length];
            float shapeSize  = Random.Range(ShapeMinSize, ShapeMaxSize);

            float angle      = (i / (float)ShapeCount) * 360f + Random.Range(-15f, 15f);
            float dist       = Random.Range(ShapeSpreadRadius * 0.5f, ShapeSpreadRadius);
            float rad        = angle * Mathf.Deg2Rad;
            float spreadX    = Mathf.Cos(rad) * dist;
            float spreadY    = (Mathf.Abs(Mathf.Sin(rad)) * ShapeVerticalBias + 0.15f) * dist;
            Vector2 target   = new Vector2(spreadX, RingPositionY + spreadY);
            shapeTargetPositions.Add(target);
            shapeAngularSpeeds.Add(Random.Range(-90f, 90f));

            var shapeGO     = new GameObject($"IntroShape_{i}");
            shapeGO.transform.SetParent(introCanvas.transform, false);

            var img         = shapeGO.AddComponent<Image>();
            img.sprite      = SpriteGenerator.CreatePolygon(shapeSides, 64);
            img.color       = new Color(0.85f, 0.85f, 0.85f, 0f);
            img.raycastTarget = false;

            var rt          = img.rectTransform;
            rt.anchorMin    = new Vector2(0.5f, 0.5f);
            rt.anchorMax    = new Vector2(0.5f, 0.5f);
            rt.pivot        = new Vector2(0.5f, 0.5f);
            rt.sizeDelta    = Vector2.one * shapeSize;
            rt.anchoredPosition = new Vector2(0f, RingPositionY);

            shapeRTs.Add(rt);
            shapeImages.Add(img);
        }

        ringImage.transform.SetAsLastSibling();
        ballImage.transform.SetAsLastSibling();
        blackOverlay.transform.SetAsLastSibling();
    }

    private IEnumerator PhaseShapesExpand()
    {
        float elapsed = 0f;

        while (elapsed < PhaseShapesOutDuration)
        {
            elapsed += Time.deltaTime;

            for (int i = 0; i < shapeRTs.Count; i++)
            {
                float delay  = (i / (float)ShapeCount) * 0.35f;
                float localT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elapsed - delay * PhaseShapesOutDuration) / PhaseShapesOutDuration));

                shapeRTs[i].anchoredPosition = Vector2.Lerp(new Vector2(0f, RingPositionY), shapeTargetPositions[i], localT);
                shapeRTs[i].localRotation    = Quaternion.Euler(0f, 0f, shapeAngularSpeeds[i] * elapsed);

                var c = shapeImages[i].color;
                c.a   = Mathf.Lerp(0f, 0.88f, Mathf.Clamp01(localT * 1.5f));
                shapeImages[i].color = c;
            }

            yield return null;
        }
    }

    // ── Phase 6 : Formes aspirées vers le centre ──────────────────────────────

    private IEnumerator PhaseShapesCollapse()
    {
        float elapsed = 0f;

        var startPositions = new List<Vector2>();
        for (int i = 0; i < shapeRTs.Count; i++)
            startPositions.Add(shapeRTs[i].anchoredPosition);

        while (elapsed < PhaseShapesInDuration)
        {
            elapsed += Time.deltaTime;

            for (int i = 0; i < shapeRTs.Count; i++)
            {
                float delay  = (1f - i / (float)ShapeCount) * 0.2f;
                float localT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elapsed - delay * PhaseShapesInDuration) / PhaseShapesInDuration));

                shapeRTs[i].anchoredPosition = Vector2.Lerp(startPositions[i], new Vector2(0f, RingPositionY), localT);
                shapeRTs[i].localRotation    = Quaternion.Euler(0f, 0f, shapeAngularSpeeds[i] * (PhaseShapesOutDuration + PhaseHoldDuration + elapsed));

                float scaleT = Mathf.Lerp(1f, 0f, Mathf.SmoothStep(0f, 1f, localT));
                shapeRTs[i].localScale = Vector3.one * scaleT;

                var c = shapeImages[i].color;
                c.a   = Mathf.Lerp(0.88f, 0f, Mathf.SmoothStep(0f, 1f, localT));
                shapeImages[i].color = c;
            }

            yield return null;
        }
    }

    // ── Phase 6b : Balle tremble et grossit (feedback pré-remplissage) ────────

    private IEnumerator PhaseBallRumble()
    {
        float elapsed      = 0f;
        Vector2 basePos    = ballRT.anchoredPosition;
        float   baseSize   = BallSize;
        float   maxSize    = BallSize * 3.8f;

        const float freqBase  = 18f;
        const float freqMax   = 42f;
        const float ampBase   = 6f;
        const float ampMax    = 22f;

        while (elapsed < PhaseBallRumbleDuration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.Clamp01(elapsed / PhaseBallRumbleDuration);

            float growT  = Mathf.SmoothStep(0f, 1f, t);
            float size   = Mathf.Lerp(baseSize, maxSize, growT * growT);
            ballRT.sizeDelta = Vector2.one * size;

            float freq   = Mathf.Lerp(freqBase, freqMax, t);
            float amp    = Mathf.Lerp(ampBase, ampMax, Mathf.SmoothStep(0f, 1f, t));
            float offsetX = Mathf.Sin(elapsed * freq)              * amp;
            float offsetY = Mathf.Sin(elapsed * freq * 1.37f + 1f) * amp * 0.6f;

            ballRT.anchoredPosition = basePos + new Vector2(offsetX, offsetY);

            yield return null;
        }

        ballRT.anchoredPosition = basePos;
        ballRT.sizeDelta        = Vector2.one * maxSize;
    }

    // ── Phase 7 : La balle blanche grossit et remplit l'écran ────────────────

    /// <summary>
    /// La balle blanche s'étend depuis son centre jusqu'à couvrir tout l'écran,
    /// révélant le menu qui est déjà construit en dessous.
    /// Ensuite la balle est détruite (le menu reprend sa couleur de fond naturelle).
    /// </summary>
    private IEnumerator PhaseBallFillScreen()
    {
        // Centre la balle sur l'écran pour que l'expansion soit symétrique
        Vector2 fillCenter  = Vector2.zero;
        float   startSize   = ballRT.sizeDelta.x;

        ballRT.anchoredPosition = fillCenter;
        blackOverlay.raycastTarget = true; // bloque les clics pendant la transition

        float elapsed = 0f;
        while (elapsed < BallFillDuration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / BallFillDuration));
            float s  = Mathf.Lerp(startSize, BallFillSize, t);
            ballRT.sizeDelta = Vector2.one * s;
            yield return null;
        }

        ballRT.sizeDelta = Vector2.one * BallFillSize;

        // Petit temps de pause pour laisser le blanc s'installer
        yield return new WaitForSeconds(0.08f);

        // Fait disparaître la balle rapidement — le fond du menu (quasi-noir) prend le relais
        float fadeElapsed = 0f;
        const float fadeDur = 0.22f;
        while (fadeElapsed < fadeDur)
        {
            fadeElapsed += Time.deltaTime;
            float a = Mathf.Lerp(1f, 0f, Mathf.Clamp01(fadeElapsed / fadeDur));
            ballImage.color = new Color(1f, 1f, 1f, a);
            background.color = new Color(0f, 0f, 0f, a); // fond s'efface aussi
            yield return null;
        }

        ballImage.color  = Color.clear;
        background.color = Color.clear;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
