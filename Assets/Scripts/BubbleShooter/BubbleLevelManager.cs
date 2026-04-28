using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Orchestre la progression entre les niveaux du Bubble Shooter.
///
/// Place ce composant sur un GameObject dans la scène Minijeu-Bulles.
/// Assigne les trois <see cref="BubbleLevelData"/> dans l'Inspector.
///
/// Flux : Niveau 1 → grille vidée → transition → Niveau 2 → … → Niveau 3 → victoire finale.
/// </summary>
public class BubbleLevelManager : MonoBehaviour
{
    public static BubbleLevelManager Instance { get; private set; }

    // ── Niveaux ───────────────────────────────────────────────────────────────

    [Header("Niveaux (assignes dans l'ordre)")]
    [SerializeField] private BubbleLevelData level1;
    [SerializeField] private BubbleLevelData level2;
    [SerializeField] private BubbleLevelData level3;

    // ── UI de transition ──────────────────────────────────────────────────────

    [Header("UI Canvas (auto-détecté si null)")]
    [SerializeField] private Transform canvasTransform;

    // ── État ──────────────────────────────────────────────────────────────────

    private int currentLevelIndex;   // 0, 1, 2
    private BubbleLevelData[] levels;

    private const float TransitionDuration = 1.8f;
    private static readonly Color ColOverlay = new Color(0f, 0f, 0f, 0f);

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        Instance = this;
        levels   = new[] { level1, level2, level3 };
    }

    private void Start()
    {
        if (canvasTransform == null)
        {
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null) canvasTransform = canvas.transform;
        }

        ApplyLevel(0);
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>
    /// Appelé par <see cref="BubbleGrid"/> quand la grille est complètement vidée.
    /// Passe au niveau suivant ou déclenche la victoire finale.
    /// </summary>
    public void OnGridCleared()
    {
        int next = currentLevelIndex + 1;
        if (next >= levels.Length)
        {
            BubbleGameManager.Instance?.TriggerVictory();
            return;
        }

        StartCoroutine(TransitionToLevel(next));
    }

    /// <summary>Retourne les données du niveau courant.</summary>
    public BubbleLevelData Current => levels[currentLevelIndex];

    /// <summary>Numéro de niveau courant (1-based, pour l'affichage).</summary>
    public int CurrentNumber => currentLevelIndex + 1;

    // ── Application d'un niveau ───────────────────────────────────────────────

    private void ApplyLevel(int index)
    {
        currentLevelIndex = index;
        var data = levels[index];
        if (data == null) return;

        BubbleGrid.Instance?.ApplyLevelData(data);
        BubbleGameManager.Instance?.ApplyLevelData(data);
        BubbleSceneSetup.ApplyBackground(data);
    }

    // ── Transition entre niveaux ──────────────────────────────────────────────

    private IEnumerator TransitionToLevel(int nextIndex)
    {
        // Gèle le jeu pendant la transition
        if (BubbleGameManager.Instance != null)
            BubbleGameManager.Instance.SetGameActive(false);

        // Overlay de fondu vers le noir
        var overlay = CreateFullscreenOverlay();
        var overlayImg = overlay.GetComponent<Image>();

        // Fondu entrant (→ noir)
        yield return StartCoroutine(FadeOverlay(overlayImg, 0f, 1f, 0.4f));

        // Applique le nouveau niveau
        ApplyLevel(nextIndex);

        // Label de transition (ex : "NIVEAU 2")
        string label = levels[nextIndex]?.levelName ?? $"NIVEAU {nextIndex + 1}";
        var titleGO = SpawnTransitionLabel(overlay.transform, label);

        yield return new WaitForSeconds(1.0f);

        // Fade out du label
        var titleTMP = titleGO.GetComponent<TextMeshProUGUI>();
        yield return StartCoroutine(FadeText(titleTMP, 1f, 0f, 0.35f));
        Destroy(titleGO);

        // Fondu sortant (→ transparent)
        yield return StartCoroutine(FadeOverlay(overlayImg, 1f, 0f, 0.4f));
        Destroy(overlay);

        // Reprend le jeu
        if (BubbleGameManager.Instance != null)
            BubbleGameManager.Instance.SetGameActive(true);
    }

    // ── Helpers UI ────────────────────────────────────────────────────────────

    private GameObject CreateFullscreenOverlay()
    {
        var go = new GameObject("LevelTransitionOverlay");
        go.transform.SetParent(canvasTransform, false);

        var rt       = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var img    = go.AddComponent<Image>();
        img.sprite = SpriteGenerator.CreateWhiteSquare();
        img.color  = new Color(0f, 0f, 0f, 0f);
        img.raycastTarget = true;   // bloque les inputs pendant la transition

        return go;
    }

    private GameObject SpawnTransitionLabel(Transform parent, string text)
    {
        var go = new GameObject("TransitionLabel");
        go.transform.SetParent(parent, false);

        var rt  = go.AddComponent<RectTransform>();
        rt.anchorMin        = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(900f, 200f);
        rt.anchoredPosition = Vector2.zero;

        var tmp   = go.AddComponent<TextMeshProUGUI>();
        tmp.text  = text;
        tmp.fontSize    = 96f;
        tmp.fontStyle   = FontStyles.Bold;
        tmp.color       = Color.white;
        tmp.alignment   = TextAlignmentOptions.Center;
        tmp.characterSpacing = 8f;

        // Michroma
        var font = LoadMichroma();
        if (font != null) tmp.font = font;

        return go;
    }

    private static TMP_FontAsset LoadMichroma()
    {
        var f = Resources.Load<TMP_FontAsset>("Michroma-Regular SDF");
        if (f != null) return f;
#if UNITY_EDITOR
        f = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/font/Michroma-Regular SDF.asset");
        if (f != null) return f;
        f = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/sprites/Michroma/Michroma-Regular SDF.asset");
#endif
        return f;
    }

    private static IEnumerator FadeOverlay(Image img, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            img.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, t));
            yield return null;
        }
        img.color = new Color(0f, 0f, 0f, to);
    }

    private static IEnumerator FadeText(TextMeshProUGUI tmp, float from, float to, float duration)
    {
        if (tmp == null) yield break;
        float elapsed = 0f;
        Color c = tmp.color;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            tmp.color = new Color(c.r, c.g, c.b, Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }
        tmp.color = new Color(c.r, c.g, c.b, to);
    }
}
