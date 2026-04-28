using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Transition entre scènes : fondu noir rapide au départ, glitch visuel à l'arrivée.
/// Persiste entre les scènes via DontDestroyOnLoad.
/// </summary>
public class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance { get; private set; }

    [Header("Timings")]
    [SerializeField] private float fadeInDuration  = 0.25f;   // menu → noir
    [SerializeField] private float fadeOutDuration = 0.20f;   // noir → scène (après glitch)

    [Header("Glitch à l'arrivée")]
    [SerializeField] private int   glitchCount     = 7;       // nombre de frames de glitch
    [SerializeField] private float glitchFrameTime = 0.035f;  // durée de chaque frame
    [SerializeField] private float glitchMaxOffset = 40f;     // décalage max en pixels

    private Canvas          overlayCanvas;
    private Image           overlayImage;
    private CanvasGroup     canvasGroup;
    private RectTransform   overlayRect;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildOverlay();
        canvasGroup.alpha = 0f;
        overlayCanvas.gameObject.SetActive(false);
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>Lance la transition : fondu noir → chargement → glitch → révélation.</summary>
    public void LoadScene(string sceneName, string gameTitle)
    {
        StartCoroutine(TransitionRoutine(sceneName));
    }

    // ── Overlay ───────────────────────────────────────────────────────────────

    private void BuildOverlay()
    {
        var canvasGO = new GameObject("TransitionOverlay");
        canvasGO.transform.SetParent(transform, false);
        DontDestroyOnLoad(canvasGO);

        overlayCanvas              = canvasGO.AddComponent<Canvas>();
        overlayCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 999;
        canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();

        canvasGroup                    = canvasGO.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts     = false;
        canvasGroup.interactable       = false;

        // Fond noir plein écran
        var bgGO = new GameObject("Overlay");
        bgGO.transform.SetParent(canvasGO.transform, false);
        overlayImage       = bgGO.AddComponent<Image>();
        overlayImage.color = Color.black;
        overlayRect        = overlayImage.rectTransform;
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = overlayRect.offsetMax = Vector2.zero;
    }

    // ── Coroutines ────────────────────────────────────────────────────────────

    private IEnumerator TransitionRoutine(string sceneName)
    {
        // ── 1. Fondu vers le noir ─────────────────────────────────────────────
        overlayCanvas.gameObject.SetActive(true);
        canvasGroup.blocksRaycasts = true;
        ResetOverlay();

        yield return StartCoroutine(Fade(0f, 1f, fadeInDuration));

        // ── 2. Chargement de la scène ─────────────────────────────────────────
        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);

        if (load == null)
        {
            Debug.LogError($"[SceneTransition] Scène introuvable : '{sceneName}'. Vérifie les Build Settings.");
            yield return StartCoroutine(Fade(1f, 0f, fadeOutDuration));
            overlayCanvas.gameObject.SetActive(false);
            canvasGroup.blocksRaycasts = false;
            yield break;
        }

        load.allowSceneActivation = false;
        while (load.progress < 0.9f) yield return null;
        load.allowSceneActivation = true;

        // Attendre que la scène ait eu le temps d'exécuter Awake + Start
        // (MenuMainSetup construit tout le fond et le canvas dans Start)
        yield return null;  // frame 1 : Awake
        yield return null;  // frame 2 : Start
        yield return null;  // frame 3 : premier Update — tout est visible

        // ── 3. Glitch à l'arrivée ─────────────────────────────────────────────
        yield return StartCoroutine(GlitchRoutine());

        // ── 4. Fondu vers la scène ────────────────────────────────────────────
        ResetOverlay();
        yield return StartCoroutine(Fade(1f, 0f, fadeOutDuration));

        overlayCanvas.gameObject.SetActive(false);
        canvasGroup.blocksRaycasts = false;
    }

    /// <summary>Simule un glitch en faisant clignoter et décaler l'overlay rapidement.</summary>
    private IEnumerator GlitchRoutine()
    {
        for (int i = 0; i < glitchCount; i++)
        {
            // Opacité aléatoire : effet de clignotement
            canvasGroup.alpha = Random.Range(0.0f, 1.0f);

            // Décalage horizontal aléatoire : simulation de désynchro vidéo
            float offsetX = Random.Range(-glitchMaxOffset, glitchMaxOffset);
            float offsetY = Random.Range(-glitchMaxOffset * 0.3f, glitchMaxOffset * 0.3f);
            overlayRect.anchoredPosition = new Vector2(offsetX, offsetY);

            // Couleur légèrement teintée sur certaines frames (RVB split)
            overlayImage.color = Random.value > 0.5f
                ? new Color(0f, 0f, 0f, 1f)
                : new Color(Random.Range(0f, 0.08f), 0f, Random.Range(0f, 0.08f), 1f);

            yield return new WaitForSecondsRealtime(glitchFrameTime);
        }

        // Remettre à plein noir propre avant le fade out
        ResetOverlay();
        canvasGroup.alpha = 1f;
    }

    private void ResetOverlay()
    {
        overlayRect.anchoredPosition = Vector2.zero;
        overlayImage.color           = Color.black;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed           += Time.unscaledDeltaTime;
            canvasGroup.alpha  = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}

