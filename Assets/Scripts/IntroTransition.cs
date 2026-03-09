using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Séquence d'intro cinématique jouée avant le menu principal.
/// Phases : fond blanc, balle noire monte du bas → traverse le ring noir →
/// entre dedans → inversion (fond noir, balle/ring blancs) → formes sortent →
/// réaspirées → fondu noir → menu révélé.
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
    private const float RingRadiusYRatio    = 0.22f;   // ellipse aplatie (trou vu de biais)
    private const float RingPositionY       = -680f;   // ring en bas de l'écran
    private const float RingThickness       = 8f;
    private const float BallStartY          = -900f;   // balle part du bas
    private const float BallSize            = 44f;
    private const int   ShapeCount          = 18;
    private const float ShapeSpreadRadius   = 780f;    // dispersion plus large
    private const float ShapeMinSize        = 70f;     // formes plus grosses
    private const float ShapeMaxSize        = 180f;    // formes plus grosses
    private const float ShapeVerticalBias   = 1.6f;    // pousse les formes plus haut
    private const float PhaseBallRumbleDuration = 1.1f;

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

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>
    /// Lance la séquence d'intro dans le canvas donné.
    /// <paramref name="onComplete"/> est invoqué quand le fondu noir est terminé.
    /// </summary>
    public void Play(Canvas canvas, System.Action onComplete)
    {
        this.introCanvas = canvas;
        this.onComplete  = onComplete;
        canvasRT = canvas.GetComponent<RectTransform>();

        BuildScene();
        StartCoroutine(IntroRoutine());
    }

    // ── Construction de la scène d'intro ──────────────────────────────────────

    private void BuildScene()
    {
        // Fond blanc (avant inversion)
        var bgGO    = new GameObject("IntroBg");
        bgGO.transform.SetParent(introCanvas.transform, false);
        background  = bgGO.AddComponent<Image>();
        background.sprite        = SpriteGenerator.CreateWhiteSquare();
        background.color         = Color.white;
        background.raycastTarget = false;
        Stretch(background.rectTransform);

        // Ring noir sur fond blanc
        var ringGO  = new GameObject("IntroRing");
        ringGO.transform.SetParent(introCanvas.transform, false);
        ringImage   = ringGO.AddComponent<Image>();
        ringImage.sprite        = CreateRingSprite(256, RingThickness / RingRadiusEnd);
        ringImage.color         = Color.black;
        ringImage.raycastTarget = false;
        ringRT      = ringImage.rectTransform;
        ringRT.anchorMin        = new Vector2(0.5f, 0.5f);
        ringRT.anchorMax        = new Vector2(0.5f, 0.5f);
        ringRT.pivot            = new Vector2(0.5f, 0.5f);
        ringRT.sizeDelta        = new Vector2(RingRadiusStart * 2f, RingRadiusStart * 2f * RingRadiusYRatio);
        ringRT.anchoredPosition = new Vector2(0f, RingPositionY);

        // Balle noire (visible sur fond blanc) qui monte du bas
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

        // Overlay noir pour le fondu final (bloque les raycasts pendant l'intro)
        var overlayGO   = new GameObject("IntroBlackOverlay");
        overlayGO.transform.SetParent(introCanvas.transform, false);
        blackOverlay    = overlayGO.AddComponent<Image>();
        blackOverlay.sprite         = SpriteGenerator.CreateWhiteSquare();
        blackOverlay.color          = new Color(0f, 0f, 0f, 0f);
        blackOverlay.raycastTarget  = true;  // bloque les clics pendant l'intro
        Stretch(blackOverlay.rectTransform);
    }

    // ── Séquence principale ───────────────────────────────────────────────────

    private IEnumerator IntroRoutine()
    {
        // ── Phase 1 : Balle noire monte du bas et traverse le ring noir ───────
        yield return StartCoroutine(PhaseBallEnter());

        // ── Phase 2 : Pulse du ring quand la balle entre ──────────────────────
        yield return StartCoroutine(PhaseRingPulse());

        // ── Phase 3 : Inversion (fond blanc → fond noir, balle noire → blanche)
        yield return StartCoroutine(PhaseInvert());

        // ── Phase 4 : Formes sortent du cercle ───────────────────────────────
        SpawnShapes();
        yield return StartCoroutine(PhaseShapesExpand());

        // ── Phase 5 : Pause ───────────────────────────────────────────────────
        yield return new WaitForSeconds(PhaseHoldDuration);

        // ── Phase 6 : Formes aspirées vers le centre ──────────────────────────
        yield return StartCoroutine(PhaseShapesCollapse());

        // ── Phase 6b : Balle tremble et grandit (feedback avant le noir) ──────
        yield return StartCoroutine(PhaseBallRumble());

        // ── Phase 7 : Fondu vers le noir ──────────────────────────────────────
        yield return StartCoroutine(PhaseFadeToBlack());

        // ── Nettoyage et callback ─────────────────────────────────────────────
        // On détruit uniquement le canvas d'intro, pas le MenuSetup
        Destroy(introCanvas.gameObject);
        onComplete?.Invoke();
        Destroy(this); // retire le composant IntroTransition du MenuSetup
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

            // Balle monte du bas vers le ring
            ballRT.anchoredPosition = Vector2.Lerp(startPos, endPos, s);

            // Ring s'élargit légèrement à mi-chemin (effet de profondeur/perspective)
            float ringT = Mathf.Sin(t * Mathf.PI);
            float ringR = Mathf.Lerp(ringStart, ringPeek, ringT);
            ringRT.sizeDelta = new Vector2(ringR * 2f, ringR * 2f * RingRadiusYRatio);

            // À ~70% du chemin la balle rétrécit pour simuler le passage à travers
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

    // ── Phase 3 : Inversion (fond blanc → noir, balle noire → blanche) ────────

    private IEnumerator PhaseInvert()
    {
        float elapsed = 0f;

        while (elapsed < PhaseInvertDuration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / PhaseInvertDuration));

            // Le fond passe de blanc à noir
            background.color = Color.Lerp(Color.white, Color.black, t);

            // Le ring passe de noir à blanc (pour rester visible sur fond noir)
            ringImage.color  = Color.Lerp(Color.black, Color.white, t);

            // La balle passe de noire à blanche
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
        int[]    sides   = { 3, 4, 5, 6, 4, 6, 3, 4 }; // triangles, carrés, pentagones, hexagones
        float[]  sizes   = new float[ShapeCount];

        for (int i = 0; i < ShapeCount; i++)
        {
            int shapeSides   = sides[i % sides.Length];
            float shapeSize  = Random.Range(ShapeMinSize, ShapeMaxSize);
            sizes[i]         = shapeSize;

            // Angle de dispersion
            float angle      = (i / (float)ShapeCount) * 360f + Random.Range(-15f, 15f);
            float dist       = Random.Range(ShapeSpreadRadius * 0.5f, ShapeSpreadRadius);
            float rad        = angle * Mathf.Deg2Rad;
            // Formes poussées haut : composante Y amplifiée + décalage vertical du ring
            float spreadX    = Mathf.Cos(rad) * dist;
            float spreadY    = (Mathf.Abs(Mathf.Sin(rad)) * ShapeVerticalBias + 0.15f) * dist;
            Vector2 target   = new Vector2(spreadX, RingPositionY + spreadY);
            shapeTargetPositions.Add(target);
            shapeAngularSpeeds.Add(Random.Range(-90f, 90f));

            var shapeGO     = new GameObject($"IntroShape_{i}");
            shapeGO.transform.SetParent(introCanvas.transform, false);

            var img         = shapeGO.AddComponent<Image>();
            img.sprite      = CreatePolygonSprite(shapeSides, 128);
            img.color       = new Color(0.85f, 0.85f, 0.85f, 0f); // formes claires sur fond noir
            img.raycastTarget = false;

            var rt          = img.rectTransform;
            rt.anchorMin    = new Vector2(0.5f, 0.5f);
            rt.anchorMax    = new Vector2(0.5f, 0.5f);
            rt.pivot        = new Vector2(0.5f, 0.5f);
            rt.sizeDelta    = Vector2.one * shapeSize;
            rt.anchoredPosition = new Vector2(0f, RingPositionY); // part du centre du ring

            shapeRTs.Add(rt);
            shapeImages.Add(img);
        }

        // S'assure que le ring et la balle restent par-dessus les formes
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
            float t  = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / PhaseShapesOutDuration));

            for (int i = 0; i < shapeRTs.Count; i++)
            {
                // Décalage de départ par index pour un effet de cascade smooth
                float delay     = (i / (float)ShapeCount) * 0.35f;
                float localT    = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elapsed - delay * PhaseShapesOutDuration) / PhaseShapesOutDuration));

                shapeRTs[i].anchoredPosition = Vector2.Lerp(new Vector2(0f, RingPositionY), shapeTargetPositions[i], localT);
                shapeRTs[i].localRotation    = Quaternion.Euler(0f, 0f, shapeAngularSpeeds[i] * elapsed);

                // Fade in progressif (formes blanches sur fond noir)
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

        // Captures des positions actuelles
        var startPositions = new List<Vector2>();
        for (int i = 0; i < shapeRTs.Count; i++)
            startPositions.Add(shapeRTs[i].anchoredPosition);

        while (elapsed < PhaseShapesInDuration)
        {
            elapsed += Time.deltaTime;

            for (int i = 0; i < shapeRTs.Count; i++)
            {
                // Décalage inversé : les formes les plus loin partent en premier
                float delay  = (1f - i / (float)ShapeCount) * 0.2f;
                float localT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elapsed - delay * PhaseShapesInDuration) / PhaseShapesInDuration));

                shapeRTs[i].anchoredPosition = Vector2.Lerp(startPositions[i], new Vector2(0f, RingPositionY), localT);
                shapeRTs[i].localRotation    = Quaternion.Euler(0f, 0f, shapeAngularSpeeds[i] * (PhaseShapesOutDuration + PhaseHoldDuration + elapsed));

                // Taille diminue aussi (effet d'aspiration)
                float scaleT = Mathf.Lerp(1f, 0f, Mathf.SmoothStep(0f, 1f, localT));
                shapeRTs[i].localScale = Vector3.one * scaleT;

                // Fade out
                var c = shapeImages[i].color;
                c.a   = Mathf.Lerp(0.88f, 0f, Mathf.SmoothStep(0f, 1f, localT));
                shapeImages[i].color = c;
            }

            yield return null;
        }
    }

    // ── Phase 6b : Balle tremble et grossit (feedback pré-fondu) ─────────────

    private IEnumerator PhaseBallRumble()
    {
        float elapsed      = 0f;
        Vector2 basePos    = ballRT.anchoredPosition;
        float   baseSize   = BallSize;
        float   maxSize    = BallSize * 3.8f;   // grossit jusqu'à ~4x

        // Paramètres du tremblement : fréquence augmente, amplitude aussi puis diminue
        const float freqBase  = 18f;
        const float freqMax   = 42f;
        const float ampBase   = 6f;
        const float ampMax    = 22f;

        while (elapsed < PhaseBallRumbleDuration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.Clamp01(elapsed / PhaseBallRumbleDuration);

            // Croissance smooth de la taille — accélère vers la fin
            float growT  = Mathf.SmoothStep(0f, 1f, t);
            float size   = Mathf.Lerp(baseSize, maxSize, growT * growT);
            ballRT.sizeDelta = Vector2.one * size;

            // Tremblement : fréquence et amplitude montent avec t
            float freq   = Mathf.Lerp(freqBase, freqMax, t);
            float amp    = Mathf.Lerp(ampBase, ampMax, Mathf.SmoothStep(0f, 1f, t));

            // Deux axes indépendants avec phases décalées → mouvement organique
            float offsetX = Mathf.Sin(elapsed * freq)               * amp;
            float offsetY = Mathf.Sin(elapsed * freq * 1.37f + 1f)  * amp * 0.6f;

            ballRT.anchoredPosition = basePos + new Vector2(offsetX, offsetY);

            yield return null;
        }

        // Fin : balle centrée, taille maximale (le fondu va la couvrir)
        ballRT.anchoredPosition = basePos;
        ballRT.sizeDelta        = Vector2.one * maxSize;
    }

    // ── Phase 7 : Fondu vers le noir ──────────────────────────────────────────

    private IEnumerator PhaseFadeToBlack()
    {
        float elapsed = 0f;

        while (elapsed < PhaseFadeBlackDuration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / PhaseFadeBlackDuration));
            blackOverlay.color = new Color(0f, 0f, 0f, t);
            yield return null;
        }

        blackOverlay.color = Color.black;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    /// <summary>Crée un sprite anneau (cercle creux) de la taille donnée.</summary>
    private static Sprite CreateRingSprite(int size, float thicknessRatio)
    {
        var tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var pixels = new Color[size * size];
        float center  = size * 0.5f;
        float outerR  = center - 1f;
        float innerR  = outerR * (1f - thicknessRatio * 8f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist  = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center));
                float outer = Mathf.Clamp01(1f - (dist - outerR));
                float inner = Mathf.Clamp01(1f - (innerR - dist));
                pixels[y * size + x] = new Color(1f, 1f, 1f, outer * inner);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    /// <summary>Crée un sprite polygone convexe à N côtés.</summary>
    private static Sprite CreatePolygonSprite(int sides, int size)
    {
        var tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var pixels = new Color[size * size];
        float center = size * 0.5f;
        float radius = center - 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                var p     = new Vector2(x + 0.5f - center, y + 0.5f - center);
                float alpha = PointInPolygon(p, sides, radius) ? 1f : 0f;

                // Léger antialiasing
                if (alpha == 0f)
                {
                    float minDist = DistToPolygonEdge(p, sides, radius);
                    alpha = Mathf.Clamp01(1f - minDist);
                }

                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static bool PointInPolygon(Vector2 p, int sides, float radius)
    {
        float angleStep = 2f * Mathf.PI / sides;
        float offset    = -Mathf.PI * 0.5f; // pointe vers le haut

        for (int i = 0; i < sides; i++)
        {
            float a0 = offset + i * angleStep;
            float a1 = offset + (i + 1) * angleStep;
            var   v0 = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * radius;
            var   v1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * radius;
            // Signe du produit croisé
            if ((v1.x - v0.x) * (p.y - v0.y) - (v1.y - v0.y) * (p.x - v0.x) < 0f)
                return false;
        }
        return true;
    }

    private static float DistToPolygonEdge(Vector2 p, int sides, float radius)
    {
        float angleStep = 2f * Mathf.PI / sides;
        float offset    = -Mathf.PI * 0.5f;
        float minDist   = float.MaxValue;

        for (int i = 0; i < sides; i++)
        {
            float a0 = offset + i * angleStep;
            float a1 = offset + (i + 1) * angleStep;
            var   v0 = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * radius;
            var   v1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * radius;

            float d  = DistPointSegment(p, v0, v1);
            if (d < minDist) minDist = d;
        }
        return minDist;
    }

    private static float DistPointSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float   t  = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + t * ab);
    }
}
