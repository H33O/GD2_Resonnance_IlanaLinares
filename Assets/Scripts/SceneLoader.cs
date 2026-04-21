using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gestionnaire de chargement de scènes asynchrone avec écran de chargement minimaliste.
///
/// Persiste entre les scènes via <c>DontDestroyOnLoad</c>.
/// Utilisé en fallback quand <see cref="SceneTransition"/> n'est pas présent.
///
/// Affiche :
///   - Un fondu noir (0.25s)
///   - Une barre de progression + pourcentage
///   - Un fondu de sortie (0.20s) après activation de la scène
/// </summary>
public class SceneLoader : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static SceneLoader Instance { get; private set; }

    // ── Timings ───────────────────────────────────────────────────────────────

    private const float FadeInDuration  = 0.25f;
    private const float FadeOutDuration = 0.20f;
    private const float MinLoadTime     = 0.35f;  // charge minimum pour éviter le flash

    // ── Palette ───────────────────────────────────────────────────────────────

    private static readonly Color ColOverlay  = Color.black;
    private static readonly Color ColBarBg    = new Color(0.15f, 0.15f, 0.15f, 1f);
    private static readonly Color ColBarFill  = Color.white;
    private static readonly Color ColText     = new Color(1f, 1f, 1f, 0.55f);

    // ── Références UI ─────────────────────────────────────────────────────────

    private Canvas        overlayCanvas;
    private CanvasGroup   canvasGroup;
    private Image         fillBar;
    private TextMeshProUGUI progressLabel;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildOverlay();
        SetVisible(false);
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>Lance le chargement asynchrone de <paramref name="sceneName"/> avec fade + barre de progression.</summary>
    public void LoadAsync(string sceneName)
    {
        StartCoroutine(LoadRoutine(sceneName));
    }

    // ── Construction de l'overlay ─────────────────────────────────────────────

    private void BuildOverlay()
    {
        var canvasGO               = new GameObject("SceneLoaderOverlay");
        canvasGO.transform.SetParent(transform, false);
        DontDestroyOnLoad(canvasGO);

        overlayCanvas              = canvasGO.AddComponent<Canvas>();
        overlayCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 998;   // sous SceneTransition (999)
        canvasGO.AddComponent<CanvasScaler>();

        canvasGroup                = canvasGO.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable   = false;

        // Fond noir
        var bgGO      = new GameObject("BG");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgImg     = bgGO.AddComponent<Image>();
        bgImg.color   = ColOverlay;
        var bgRT      = bgImg.rectTransform;
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

        // Barre de fond
        var barBgGO   = new GameObject("BarBg");
        barBgGO.transform.SetParent(canvasGO.transform, false);
        var barBgImg  = barBgGO.AddComponent<Image>();
        barBgImg.color = ColBarBg;
        var barBgRT   = barBgImg.rectTransform;
        barBgRT.anchorMin       = new Vector2(0.1f, 0.5f);
        barBgRT.anchorMax       = new Vector2(0.9f, 0.5f);
        barBgRT.pivot           = new Vector2(0.5f, 0.5f);
        barBgRT.sizeDelta       = new Vector2(0f, 6f);
        barBgRT.anchoredPosition = new Vector2(0f, -60f);

        // Barre de remplissage (fill)
        var barFillGO  = new GameObject("BarFill");
        barFillGO.transform.SetParent(barBgGO.transform, false);
        var barFillImg = barFillGO.AddComponent<Image>();
        barFillImg.color = ColBarFill;
        barFillImg.type  = Image.Type.Filled;
        barFillImg.fillMethod = Image.FillMethod.Horizontal;
        barFillImg.fillAmount = 0f;
        fillBar = barFillImg;
        var barFillRT   = barFillImg.rectTransform;
        barFillRT.anchorMin = Vector2.zero;
        barFillRT.anchorMax = Vector2.one;
        barFillRT.offsetMin = barFillRT.offsetMax = Vector2.zero;

        // Label pourcentage
        var labelGO    = new GameObject("ProgressLabel");
        labelGO.transform.SetParent(canvasGO.transform, false);
        var labelTmp   = labelGO.AddComponent<TextMeshProUGUI>();
        labelTmp.text  = "0%";
        labelTmp.fontSize = 32f;
        labelTmp.fontStyle = FontStyles.Bold;
        labelTmp.color = ColText;
        labelTmp.alignment = TextAlignmentOptions.Center;
        labelTmp.raycastTarget = false;
        progressLabel  = labelTmp;
        var labelRT    = labelTmp.rectTransform;
        labelRT.anchorMin       = new Vector2(0f, 0.5f);
        labelRT.anchorMax       = new Vector2(1f, 0.5f);
        labelRT.pivot           = new Vector2(0.5f, 0.5f);
        labelRT.sizeDelta       = new Vector2(0f, 60f);
        labelRT.anchoredPosition = new Vector2(0f, -100f);
    }

    // ── Coroutine de chargement ───────────────────────────────────────────────

    private IEnumerator LoadRoutine(string sceneName)
    {
        // Fade in
        SetVisible(true);
        SetProgress(0f);
        yield return StartCoroutine(Fade(0f, 1f, FadeInDuration));

        // Chargement asynchrone
        float elapsed = 0f;
        var op        = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f || elapsed < MinLoadTime)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(op.progress / 0.9f);
            SetProgress(Mathf.Min(progress, elapsed / MinLoadTime));
            yield return null;
        }

        SetProgress(1f);
        yield return null;

        op.allowSceneActivation = true;
        yield return null;  // laisse Unity initialiser la nouvelle scène

        // Fade out
        yield return StartCoroutine(Fade(1f, 0f, FadeOutDuration));
        SetVisible(false);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetVisible(bool visible)
    {
        overlayCanvas.gameObject.SetActive(visible);
        canvasGroup.blocksRaycasts = visible;
    }

    private void SetProgress(float t)
    {
        t = Mathf.Clamp01(t);
        if (fillBar      != null) fillBar.fillAmount   = t;
        if (progressLabel != null) progressLabel.text   = $"{Mathf.RoundToInt(t * 100f)}%";
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed           += Time.deltaTime;
            canvasGroup.alpha  = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}
