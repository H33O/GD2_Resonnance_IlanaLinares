using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simule un essaim de lucioles dans le menu via des éléments UI animés sur le Canvas
/// (Screen Space Overlay — les ParticleSystem world-space seraient masqués par le canvas).
///
/// Chaque luciole :
///   - Cercle lumineux jaune-vert, taille 6–18 px
///   - Dérive lentement sur une trajectoire sinusoïdale (Perlin noise)
///   - Pulse son opacité de façon indépendante
///   - Varie légèrement sa teinte (vert chaud → jaune doux)
///   - Clignotement court et rare pour imiter l'organe bioluminescent
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class MenuFireflies : MonoBehaviour
{
    // ── Paramètres pool ───────────────────────────────────────────────────────

    private const int   FireflyCount   = 16;
    private const float MinSize        = 6f;
    private const float MaxSize        = 18f;

    // ── Vitesse de dérive ─────────────────────────────────────────────────────

    private const float DriftSpeed     = 0.09f;   // amplitude de déplacement Perlin / frame
    private const float DriftFreqBase  = 0.28f;   // fréquence Perlin de base
    private const float DriftRadius    = 210f;    // rayon max de dérive autour de l'origine

    // ── Opacité ───────────────────────────────────────────────────────────────

    private const float AlphaMin       = 0.05f;
    private const float AlphaMax       = 0.82f;
    private const float PulseSpeedMin  = 0.6f;
    private const float PulseSpeedMax  = 2.1f;

    // ── Palette jaune-vert bioluminescent ─────────────────────────────────────

    private static readonly Color ColA = new Color(0.95f, 0.98f, 0.35f, 1f);  // jaune vif
    private static readonly Color ColB = new Color(0.60f, 0.98f, 0.30f, 1f);  // vert chaud
    private static readonly Color ColC = new Color(0.98f, 0.88f, 0.20f, 1f);  // ambre doux

    // ── Clignotement ──────────────────────────────────────────────────────────

    private const float BlinkChance    = 0.0025f; // probabilité par frame de déclencher un clignotement
    private const float BlinkDuration  = 0.12f;

    // ── Canvas rect (injecté) ────────────────────────────────────────────────

    private RectTransform canvasRT;

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Crée et démarre les lucioles dans le canvas fourni.</summary>
    public void Init(RectTransform canvas)
    {
        canvasRT = canvas;
        for (int i = 0; i < FireflyCount; i++)
            StartCoroutine(RunFirefly(i));
    }

    // ── Coroutine par luciole ─────────────────────────────────────────────────

    private IEnumerator RunFirefly(int index)
    {
        // ── Création du GameObject ────────────────────────────────────────────
        var go = new GameObject($"Firefly_{index}");
        go.transform.SetParent(canvasRT, false);

        // Halo extérieur (grand cercle très transparent)
        var haloGO  = new GameObject("Halo");
        haloGO.transform.SetParent(go.transform, false);
        var haloImg = haloGO.AddComponent<Image>();
        haloImg.sprite       = SpriteGenerator.CreateCircle(64);
        haloImg.raycastTarget = false;
        var haloRT  = haloImg.rectTransform;
        haloRT.anchorMin = haloRT.anchorMax = new Vector2(0.5f, 0.5f);
        haloRT.pivot     = new Vector2(0.5f, 0.5f);

        // Noyau lumineux (petit cercle net)
        var coreImg = go.AddComponent<Image>();
        coreImg.sprite       = SpriteGenerator.CreateCircle(64);
        coreImg.raycastTarget = false;
        var rt = coreImg.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);

        // ── Paramètres aléatoires par luciole ─────────────────────────────────
        float size       = Random.Range(MinSize, MaxSize);
        float haloMult   = Random.Range(2.2f, 3.8f);
        float pulseSpeed = Random.Range(PulseSpeedMin, PulseSpeedMax);
        float pulsePhase = Random.Range(0f, Mathf.PI * 2f);
        float noiseOffX  = Random.Range(0f, 100f);
        float noiseOffY  = Random.Range(0f, 100f);
        float driftFreq  = DriftFreqBase * Random.Range(0.7f, 1.4f);
        float colorT     = Random.Range(0f, 1f);
        Color baseColor  = SelectColor(colorT);

        rt.sizeDelta     = Vector2.one * size;
        haloRT.sizeDelta = Vector2.one * (size * haloMult);

        // Position d'origine aléatoire sur tout l'écran
        float w = canvasRT.rect.width;
        float h = canvasRT.rect.height;
        Vector2 origin = new Vector2(
            Random.Range(-w * 0.5f + 60f, w * 0.5f - 60f),
            Random.Range(-h * 0.5f + 60f, h * 0.5f - 60f));

        // Délai de démarrage pour désynchroniser les lucioles
        yield return new WaitForSeconds(Random.Range(0f, 3.5f));

        // ── Boucle d'animation ────────────────────────────────────────────────
        var state = new FireflyState();

        while (go != null)
        {
            float t = Time.time;

            // -- Position (Perlin noise 2D) -----------------------------------
            float nx  = Mathf.PerlinNoise(noiseOffX + t * driftFreq,        0f) - 0.5f;
            float ny  = Mathf.PerlinNoise(0f,         noiseOffY + t * driftFreq) - 0.5f;
            rt.anchoredPosition = origin + new Vector2(nx, ny) * DriftRadius * 2f;

            // -- Opacité pulsante sinusoïdale ---------------------------------
            float rawAlpha = Mathf.Sin(t * pulseSpeed + pulsePhase) * 0.5f + 0.5f;
            float alpha    = Mathf.Lerp(AlphaMin, AlphaMax, rawAlpha);

            // -- Clignotement rare --------------------------------------------
            if (!state.Blinking && Random.value < BlinkChance)
                StartCoroutine(Blink(coreImg, haloImg, state));

            if (!state.Blinking)
            {
                coreImg.color  = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                haloImg.color  = new Color(baseColor.r, baseColor.g, baseColor.b, alpha * 0.18f);
            }

            yield return null;
        }
    }

    // ── Clignotement bref ─────────────────────────────────────────────────────

    private IEnumerator Blink(Image core, Image halo, FireflyState state)
    {
        state.Blinking = true;

        // Flash blanc rapide
        core.color = new Color(1f, 1f, 0.9f, 1f);
        halo.color = new Color(1f, 1f, 0.8f, 0.55f);
        yield return new WaitForSeconds(BlinkDuration * 0.3f);

        // Fade out du flash
        float elapsed = 0f;
        while (elapsed < BlinkDuration)
        {
            elapsed += Time.deltaTime;
            float n = 1f - elapsed / BlinkDuration;
            core.color = new Color(1f, 1f, 0.9f, n);
            halo.color = new Color(1f, 1f, 0.8f, n * 0.4f);
            yield return null;
        }

        state.Blinking = false;
    }

    /// <summary>Boîte mutable passée par référence aux coroutines imbriquées.</summary>
    private class FireflyState { public bool Blinking; }

    // ── Utilitaire palette ────────────────────────────────────────────────────

    private static Color SelectColor(float t)
    {
        if (t < 0.5f) return Color.Lerp(ColA, ColB, t * 2f);
        return Color.Lerp(ColB, ColC, (t - 0.5f) * 2f);
    }
}
