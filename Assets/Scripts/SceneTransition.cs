using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gère le fondu noir entre le menu et une scène de jeu.
/// Affiche le titre du jeu pendant le fondu puis charge la scène.
/// Persiste entre les scènes via DontDestroyOnLoad.
/// </summary>
public class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance { get; private set; }

    [Header("Timings")]
    [SerializeField] private float fadeInDuration  = 0.5f;
    [SerializeField] private float titleHoldDuration = 0.8f;
    [SerializeField] private float fadeOutDuration = 0.6f;

    [Header("Visuel")]
    [SerializeField] private Color overlayColor = Color.black;
    [SerializeField] private float titleFontSize = 72f;

    private Canvas          overlayCanvas;
    private Image           overlayImage;
    private TextMeshProUGUI titleText;
    private CanvasGroup     canvasGroup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildOverlay();
        canvasGroup.alpha = 0f;
        overlayCanvas.gameObject.SetActive(false);
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>Lance le fondu noir, affiche le titre puis charge la scène.</summary>
    public void LoadScene(string sceneName, string gameTitle)
    {
        StartCoroutine(TransitionRoutine(sceneName, gameTitle));
    }

    // ── Construction de l'overlay ─────────────────────────────────────────────

    private void BuildOverlay()
    {
        var canvasGO = new GameObject("TransitionOverlay");
        canvasGO.transform.SetParent(transform, false);
        DontDestroyOnLoad(canvasGO);

        overlayCanvas = canvasGO.AddComponent<Canvas>();
        overlayCanvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 999;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        ((CanvasScaler)canvasGO.GetComponent<CanvasScaler>()).referenceResolution = new Vector2(1080, 1920);

        canvasGroup       = canvasGO.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;

        // Fond noir plein écran
        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        overlayImage = bgGO.AddComponent<Image>();
        overlayImage.color = overlayColor;
        var bgRT = overlayImage.rectTransform;
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

        // Titre centré
        var titleGO = new GameObject("GameTitle");
        titleGO.transform.SetParent(canvasGO.transform, false);
        titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.fontSize  = titleFontSize;
        titleText.color     = Color.white;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        var titleRT = titleText.rectTransform;
        titleRT.anchorMin        = new Vector2(0f, 0.5f);
        titleRT.anchorMax        = new Vector2(1f, 0.5f);
        titleRT.pivot            = new Vector2(0.5f, 0.5f);
        titleRT.sizeDelta        = new Vector2(0f, 200f);
        titleRT.anchoredPosition = Vector2.zero;
    }

    // ── Coroutine de transition ───────────────────────────────────────────────

    private IEnumerator TransitionRoutine(string sceneName, string gameTitle)
    {
        // Pas de fondu — chargement direct
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        while (asyncLoad.progress < 0.9f)
            yield return null;

        asyncLoad.allowSceneActivation = true;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }

    private IEnumerator FadeTitle(float from, float to, float duration)
    {
        float elapsed = 0f;
        Color c = titleText.color;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, elapsed / duration);
            titleText.color = c;
            yield return null;
        }
        c.a = to;
        titleText.color = c;
    }
}
