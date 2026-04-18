using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Affiche le score total accumulé (Game &amp; Watch) dans l'Overworld,
/// en haut à gauche de l'écran.
/// Se construit procéduralement et s'abonne à <see cref="OWGameManager.OnTotalScoreChanged"/>.
/// Attacher à un GameObject dans la scène Overworld, ou instancié par <see cref="OWSceneSetup"/>.
/// </summary>
public class OWScoreDisplay : MonoBehaviour
{
    // ── Constantes visuelles ──────────────────────────────────────────────────

    private static readonly Color PanelBg      = new Color(0f, 0f, 0f, 0.65f);
    private static readonly Color LabelColor   = new Color(1f, 0.85f, 0.10f, 1f);
    private static readonly Color TitleColor   = new Color(0.75f, 0.75f, 0.75f, 1f);

    private const float PulseDuration = 0.50f;

    // ── Références UI ─────────────────────────────────────────────────────────

    private TextMeshProUGUI scoreValueLabel;
    private Image           panelImage;
    private CanvasGroup     canvasGroup;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        BuildUI();
        RefreshScore(OWGameManager.Instance != null ? OWGameManager.Instance.TotalScore : 0);

        if (OWGameManager.Instance != null)
            OWGameManager.Instance.OnTotalScoreChanged.AddListener(OnScoreChanged);
    }

    private void OnDestroy()
    {
        if (OWGameManager.Instance != null)
            OWGameManager.Instance.OnTotalScoreChanged.RemoveListener(OnScoreChanged);
    }

    // ── Callback ──────────────────────────────────────────────────────────────

    private void OnScoreChanged(int newTotal)
    {
        RefreshScore(newTotal);
        StopAllCoroutines();
        StartCoroutine(PulseEffect());
    }

    // ── Affichage ─────────────────────────────────────────────────────────────

    private void RefreshScore(int total)
    {
        if (scoreValueLabel != null)
            scoreValueLabel.text = total.ToString("D6");
    }

    // ── Animation de pulse à chaque mise à jour ───────────────────────────────

    private IEnumerator PulseEffect()
    {
        float elapsed = 0f;
        while (elapsed < PulseDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / PulseDuration;
            float scale = 1f + 0.18f * Mathf.Sin(t * Mathf.PI);
            transform.localScale = Vector3.one * scale;
            yield return null;
        }
        transform.localScale = Vector3.one;
    }

    // ── Construction UI ───────────────────────────────────────────────────────

    private void BuildUI()
    {
        // Canvas Screen Space Overlay
        var canvasGO      = new GameObject("OWScoreCanvas");
        canvasGO.transform.SetParent(transform, false);

        var canvas        = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        var scaler        = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        canvasGroup       = canvasGO.AddComponent<CanvasGroup>();
        canvasGroup.interactable   = false;
        canvasGroup.blocksRaycasts = false;

        // Panel fond haut-gauche
        var panelGO       = new GameObject("ScorePanel");
        panelGO.transform.SetParent(canvasGO.transform, false);

        var panelRT       = panelGO.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0f, 1f);
        panelRT.anchorMax = new Vector2(0f, 1f);
        panelRT.pivot     = new Vector2(0f, 1f);
        panelRT.anchoredPosition = new Vector2(12f, -12f);
        panelRT.sizeDelta = new Vector2(220f, 70f);

        panelImage        = panelGO.AddComponent<Image>();
        panelImage.color  = PanelBg;

        // Titre "G&W"
        var titleGO       = new GameObject("Title");
        titleGO.transform.SetParent(panelGO.transform, false);
        var titleTMP      = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text     = "GAME & WATCH";
        titleTMP.fontSize = 14f;
        titleTMP.color    = TitleColor;
        titleTMP.alignment = TextAlignmentOptions.TopLeft;
        titleTMP.fontStyle = FontStyles.Bold;
        var titleRT       = titleTMP.rectTransform;
        titleRT.anchorMin = new Vector2(0f, 0.5f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.offsetMin = new Vector2(10f, 0f);
        titleRT.offsetMax = new Vector2(-8f, -4f);

        // Score value
        var valueGO       = new GameObject("ScoreValue");
        valueGO.transform.SetParent(panelGO.transform, false);
        scoreValueLabel   = valueGO.AddComponent<TextMeshProUGUI>();
        scoreValueLabel.text    = "000000";
        scoreValueLabel.fontSize = 28f;
        scoreValueLabel.color   = LabelColor;
        scoreValueLabel.alignment = TextAlignmentOptions.BottomLeft;
        scoreValueLabel.fontStyle = FontStyles.Bold;
        var valueRT       = scoreValueLabel.rectTransform;
        valueRT.anchorMin = new Vector2(0f, 0f);
        valueRT.anchorMax = new Vector2(1f, 0.55f);
        valueRT.offsetMin = new Vector2(10f, 4f);
        valueRT.offsetMax = new Vector2(-8f, 0f);

        // Icône étoile (décoration)
        var starGO        = new GameObject("StarIcon");
        starGO.transform.SetParent(panelGO.transform, false);
        var starTMP       = starGO.AddComponent<TextMeshProUGUI>();
        starTMP.text      = "★";
        starTMP.fontSize  = 18f;
        starTMP.color     = LabelColor;
        starTMP.alignment = TextAlignmentOptions.MidlineRight;
        var starRT        = starTMP.rectTransform;
        starRT.anchorMin  = new Vector2(0f, 0f);
        starRT.anchorMax  = new Vector2(1f, 1f);
        starRT.offsetMin  = new Vector2(0f, 4f);
        starRT.offsetMax  = new Vector2(-8f, -4f);
    }
}
