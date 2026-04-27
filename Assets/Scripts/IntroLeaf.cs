using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Cinématique d'intro de 4 secondes :
///   Phase 1 (2.5 s) — Feuille qui tombe sur fond noir avec rotation douce.
///   Phase 2 (0.8 s) — Fondu au noir progressif.
///   Phase 3          — Chargement et reveal de la scène Menu.
///
/// Ce MonoBehaviour est placé dans la scène <c>Intro</c> et se détruit lui-même
/// une fois la transition terminée.
/// </summary>
public class IntroLeaf : MonoBehaviour
{
    // ── Constantes de timing ──────────────────────────────────────────────────

    /// <summary>Durée totale pendant laquelle la feuille tombe avant le fondu.</summary>
    private const float FallDuration    = 2.5f;

    /// <summary>Durée du fondu au noir avant de charger la scène Menu.</summary>
    private const float FadeDuration    = 0.8f;

    /// <summary>Nom de la scène à charger après la cinématique.</summary>
    private const string TargetScene    = "Menu";

    // ── Paramètres visuels ────────────────────────────────────────────────────

    /// <summary>Taille de la feuille en pixels canvas (référence 1080×1920).</summary>
    private const float LeafSize        = 320f;

    /// <summary>Position Y de départ de la feuille (hors écran vers le haut).</summary>
    private const float LeafStartY      = 1100f;

    /// <summary>Position Y d'arrivée de la feuille (bas de l'écran).</summary>
    private const float LeafEndY        = -800f;

    /// <summary>Amplitude du balancement horizontal (oscillation sinusoïdale).</summary>
    private const float SwayAmplitude   = 180f;

    /// <summary>Fréquence du balancement horizontal (cycles par seconde).</summary>
    private const float SwayFrequency   = 0.9f;

    /// <summary>Rotation initiale de la feuille en degrés.</summary>
    private const float RotationStart   = 15f;

    /// <summary>Rotation finale de la feuille en degrés (sens contraire = -1 tour ~).</summary>
    private const float RotationEnd     = -340f;

    // ── Références internes ───────────────────────────────────────────────────

    private Canvas          _canvas;
    private Image           _fadeOverlay;
    private RectTransform   _leafRT;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        BuildCanvas();
        StartCoroutine(PlaySequence());
    }

    // ── Construction de la scène ──────────────────────────────────────────────

    private void BuildCanvas()
    {
        // ── Fond noir caméra ──────────────────────────────────────────────────
        var cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
        }

        // ── Canvas Screen Space Overlay ───────────────────────────────────────
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

        // ── Fond noir UI (sécurité) ───────────────────────────────────────────
        var bgGO    = new GameObject("IntroBg");
        bgGO.transform.SetParent(canvasRT, false);
        var bgImg   = bgGO.AddComponent<Image>();
        bgImg.color = Color.black;
        bgImg.sprite = SpriteGenerator.CreateWhiteSquare();
        bgImg.raycastTarget = false;
        StretchFull(bgImg.rectTransform);

        // ── Feuille ───────────────────────────────────────────────────────────
        var leafGO  = new GameObject("Leaf");
        leafGO.transform.SetParent(canvasRT, false);
        var leafImg = leafGO.AddComponent<Image>();
        leafImg.raycastTarget = false;

        // Chargement du sprite anim_feuille via AssetDatabase en éditeur,
        // ou Resources en build
        leafImg.sprite = LoadLeafSprite();

        _leafRT = leafImg.rectTransform;
        _leafRT.anchorMin        = new Vector2(0.5f, 0.5f);
        _leafRT.anchorMax        = new Vector2(0.5f, 0.5f);
        _leafRT.pivot            = new Vector2(0.5f, 0.5f);
        _leafRT.sizeDelta        = new Vector2(LeafSize, LeafSize);
        _leafRT.anchoredPosition = new Vector2(0f, LeafStartY);
        _leafRT.localRotation   = Quaternion.Euler(0f, 0f, RotationStart);

        // ── Overlay de fondu (au-dessus de tout) ─────────────────────────────
        var fadeGO  = new GameObject("FadeOverlay");
        fadeGO.transform.SetParent(canvasRT, false);
        _fadeOverlay        = fadeGO.AddComponent<Image>();
        _fadeOverlay.sprite = SpriteGenerator.CreateWhiteSquare();
        _fadeOverlay.color  = new Color(0f, 0f, 0f, 0f);
        _fadeOverlay.raycastTarget = false;
        StretchFull(_fadeOverlay.rectTransform);
    }

    // ── Chargement sprite ─────────────────────────────────────────────────────

    private static Sprite LoadLeafSprite()
    {
#if UNITY_EDITOR
        var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/sprites/anim_feuille.png");
        if (tex != null)
        {
            var objs = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(
                "Assets/sprites/anim_feuille.png");
            foreach (var obj in objs)
                if (obj is Sprite sp) return sp;

            return Sprite.Create(tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), 100f);
        }
#endif
        return Resources.Load<Sprite>("anim_feuille") ?? SpriteGenerator.CreateWhiteSquare();
    }

    // ── Séquence principale ───────────────────────────────────────────────────

    private IEnumerator PlaySequence()
    {
        // ── Phase 1 : chute de la feuille ─────────────────────────────────────
        float elapsed = 0f;
        while (elapsed < FallDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / FallDuration);

            // Interpolation verticale EaseInQuad (accélération naturelle)
            float tEased = t * t;
            float posY = Mathf.LerpUnclamped(LeafStartY, LeafEndY, tEased);

            // Balancement horizontal sinusoïdal
            float posX = Mathf.Sin(elapsed * SwayFrequency * Mathf.PI * 2f) * SwayAmplitude;

            _leafRT.anchoredPosition = new Vector2(posX, posY);

            // Rotation continue
            float angle = Mathf.LerpUnclamped(RotationStart, RotationEnd, t);
            _leafRT.localRotation = Quaternion.Euler(0f, 0f, angle);

            yield return null;
        }

        // ── Phase 2 : fondu au noir ───────────────────────────────────────────
        elapsed = 0f;
        while (elapsed < FadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / FadeDuration);
            _fadeOverlay.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }
        _fadeOverlay.color = Color.black;

        // ── Phase 3 : chargement de la scène Menu ─────────────────────────────
        yield return SceneManager.LoadSceneAsync(TargetScene);
    }

    // ── Utilitaire ────────────────────────────────────────────────────────────

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
