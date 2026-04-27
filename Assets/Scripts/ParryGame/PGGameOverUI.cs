using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays the Game Over overlay for the Parry Game.
/// Shows final score and two buttons: Rejouer / Menu.
/// </summary>
public class PGGameOverUI : MonoBehaviour
{
    // ── Constants ─────────────────────────────────────────────────────────────

    private const float FadeDuration = 0.4f;
    private const float BtnW         = 420f;
    private const float BtnH         = 130f;

    // ── Internal ──────────────────────────────────────────────────────────────

    private CanvasGroup     overlayGroup;
    private TextMeshProUGUI finalScoreLabel;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnEnable()  => PGGameManager.OnGameOver += HandleGameOver;
    private void OnDisable() => PGGameManager.OnGameOver -= HandleGameOver;

    // ── Initialisation ────────────────────────────────────────────────────────

    /// <summary>Builds the overlay inside the provided canvas RectTransform.</summary>
    public void Init(RectTransform canvasRT)
    {
        BuildOverlay(canvasRT);
    }

    // ── Build ─────────────────────────────────────────────────────────────────

    private void BuildOverlay(RectTransform canvasRT)
    {
        var root = new GameObject("GameOverOverlay");
        root.transform.SetParent(canvasRT, false);

        var rt          = root.AddComponent<RectTransform>();
        rt.anchorMin    = Vector2.zero;
        rt.anchorMax    = Vector2.one;
        rt.offsetMin    = rt.offsetMax = Vector2.zero;

        overlayGroup              = root.AddComponent<CanvasGroup>();
        overlayGroup.alpha        = 0f;
        overlayGroup.blocksRaycasts = false;
        overlayGroup.interactable   = false;

        // Dim background
        var dimGO    = new GameObject("Dim");
        dimGO.transform.SetParent(rt, false);
        var dimImg   = dimGO.AddComponent<Image>();
        dimImg.color = new Color(0f, 0f, 0f, 0.84f);
        var dimRT    = dimImg.rectTransform;
        dimRT.anchorMin = Vector2.zero;
        dimRT.anchorMax = Vector2.one;
        dimRT.offsetMin = dimRT.offsetMax = Vector2.zero;

        // "GAME OVER" title
        var titleGO  = new GameObject("Title");
        titleGO.transform.SetParent(rt, false);
        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text      = "GAME OVER";
        titleTMP.fontSize  = 96f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color     = new Color(0.95f, 0.25f, 0.15f, 1f);
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.raycastTarget = false;
        MenuAssets.ApplyFont(titleTMP);
        var titleRT         = titleTMP.rectTransform;
        titleRT.anchorMin   = new Vector2(0f, 0.6f);
        titleRT.anchorMax   = new Vector2(1f, 0.75f);
        titleRT.offsetMin   = titleRT.offsetMax = Vector2.zero;

        // Score display
        var scoreGO  = new GameObject("FinalScore");
        scoreGO.transform.SetParent(rt, false);
        finalScoreLabel = scoreGO.AddComponent<TextMeshProUGUI>();
        finalScoreLabel.text      = "Score: 0";
        finalScoreLabel.fontSize  = 56f;
        finalScoreLabel.color     = Color.white;
        finalScoreLabel.alignment = TextAlignmentOptions.Center;
        finalScoreLabel.raycastTarget = false;
        MenuAssets.ApplyFont(finalScoreLabel);
        var sRT           = finalScoreLabel.rectTransform;
        sRT.anchorMin     = new Vector2(0f, 0.50f);
        sRT.anchorMax     = new Vector2(1f, 0.62f);
        sRT.offsetMin     = sRT.offsetMax = Vector2.zero;

        // Buttons
        BuildButton(rt, "REJOUER",   new Vector2(0f, -60f),  new Color(0.30f, 0.55f, 1f, 1f),
                    () => PGGameManager.Instance?.Restart());
        BuildButton(rt, "MENU",      new Vector2(0f, -220f), new Color(0.5f, 0.5f, 0.5f, 1f),
                    () => PGGameManager.Instance?.ReturnToMenu());
    }

    private void BuildButton(RectTransform parent, string label, Vector2 anchoredPos,
                              Color color, UnityEngine.Events.UnityAction onClick)
    {
        var go  = new GameObject($"Btn_{label}");
        go.transform.SetParent(parent, false);

        var img   = go.AddComponent<Image>();
        img.sprite = SpriteGenerator.CreateWhiteSquare();
        img.color  = color;

        var rt          = img.rectTransform;
        rt.anchorMin    = new Vector2(0.5f, 0.5f);
        rt.anchorMax    = new Vector2(0.5f, 0.5f);
        rt.pivot        = new Vector2(0.5f, 0.5f);
        rt.sizeDelta    = new Vector2(BtnW, BtnH);
        rt.anchoredPosition = anchoredPos;

        var lGO  = new GameObject("Label");
        lGO.transform.SetParent(rt, false);
        var tmp  = lGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 48f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        MenuAssets.ApplyFont(tmp);
        var lRT       = tmp.rectTransform;
        lRT.anchorMin = Vector2.zero;
        lRT.anchorMax = Vector2.one;
        lRT.offsetMin = lRT.offsetMax = Vector2.zero;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);
    }

    // ── Game over handler ─────────────────────────────────────────────────────

    private void HandleGameOver()
    {
        if (finalScoreLabel != null && PGGameManager.Instance != null)
            finalScoreLabel.text = $"Score : {PGGameManager.Instance.Score}";

        overlayGroup.blocksRaycasts = true;
        overlayGroup.interactable   = true;
        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        while (elapsed < FadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            overlayGroup.alpha = Mathf.Clamp01(elapsed / FadeDuration);
            yield return null;
        }
        overlayGroup.alpha = 1f;
    }
}
