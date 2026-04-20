using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Widget de résultat affiché à la fin d'un niveau TiltBall.
/// Affiche le temps exact et le score accumulé.
/// Se crée et se détruit lui-même dynamiquement.
/// </summary>
public class TBWinWidget : MonoBehaviour
{
    // ── Couleurs ──────────────────────────────────────────────────────────────

    private static readonly Color ColPanel      = new Color(0.04f, 0.04f, 0.08f, 0.95f);
    private static readonly Color ColTitle      = new Color(1f, 0.85f, 0.10f, 1f);
    private static readonly Color ColText       = Color.white;
    private static readonly Color ColBtnBg      = new Color(0.15f, 0.15f, 0.25f, 1f);
    private static readonly Color ColBtnText    = Color.white;

    // ── API statique ──────────────────────────────────────────────────────────

    /// <summary>
    /// Crée et affiche le widget de résultat.
    /// </summary>
    /// <param name="elapsedTime">Temps écoulé en secondes.</param>
    /// <param name="score">Score total.</param>
    /// <param name="onContinue">Callback déclenché quand le joueur appuie sur "Continuer".</param>
    public static void Show(float elapsedTime, int score, Action onContinue)
    {
        // Canvas racine
        var canvasGO = new GameObject("WinWidget");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        var scaler                 = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight  = 1f;

        canvasGO.AddComponent<GraphicRaycaster>();

        var widget = canvasGO.AddComponent<TBWinWidget>();
        widget.Build(canvas.GetComponent<RectTransform>(), elapsedTime, score, onContinue, canvasGO);
    }

    // ── Construction ──────────────────────────────────────────────────────────

    private void Build(RectTransform root, float elapsedTime, int score, Action onContinue, GameObject selfRoot)
    {
        // Fond semi-transparent plein écran
        var overlay = MakePanel("Overlay", root, Vector2.zero, Vector2.one);
        overlay.color = new Color(0f, 0f, 0f, 0.65f);

        // Panneau central
        var panelRT = MakePanelRT("Panel", root,
            new Vector2(0.1f, 0.30f), new Vector2(0.9f, 0.72f));
        panelRT.GetComponent<Image>().color = ColPanel;

        // Titre
        MakeLabel("Title", panelRT,
            "NIVEAU TERMINÉ", 64f, ColTitle, FontStyles.Bold,
            new Vector2(0f, 0.72f), new Vector2(1f, 0.96f));

        // Temps
        int   minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int   seconds = Mathf.FloorToInt(elapsedTime % 60f);
        float millis  = (elapsedTime % 1f) * 100f;
        string timeStr = $"Temps   {minutes:00}:{seconds:00}.{(int)millis:00}";

        MakeLabel("Time", panelRT,
            timeStr, 42f, ColText, FontStyles.Normal,
            new Vector2(0.05f, 0.48f), new Vector2(0.95f, 0.68f));

        // Score
        MakeLabel("Score", panelRT,
            $"Score   {score}", 42f, ColText, FontStyles.Normal,
            new Vector2(0.05f, 0.26f), new Vector2(0.95f, 0.46f));

        // Bouton Continuer
        MakeButton("BtnContinue", panelRT,
            "CONTINUER",
            new Vector2(0.15f, 0.04f), new Vector2(0.85f, 0.20f),
            () =>
            {
                Destroy(selfRoot);
                onContinue?.Invoke();
            });
    }

    // ── Helpers UI ────────────────────────────────────────────────────────────

    private static Image MakePanel(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);

        var img      = go.AddComponent<Image>();
        img.color    = Color.black;

        var rt        = img.rectTransform;
        rt.anchorMin  = anchorMin;
        rt.anchorMax  = anchorMax;
        rt.offsetMin  = rt.offsetMax = Vector2.zero;

        return img;
    }

    private static RectTransform MakePanelRT(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);

        var img      = go.AddComponent<Image>();
        img.color    = ColPanel;

        var rt        = img.rectTransform;
        rt.anchorMin  = anchorMin;
        rt.anchorMax  = anchorMax;
        rt.offsetMin  = rt.offsetMax = Vector2.zero;

        return rt;
    }

    private static void MakeLabel(string name, RectTransform parent,
        string text, float fontSize, Color color, FontStyles style,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);

        var tmp        = go.AddComponent<TextMeshProUGUI>();
        tmp.text       = text;
        tmp.fontSize   = fontSize;
        tmp.color      = color;
        tmp.fontStyle  = style;
        tmp.alignment  = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;

        var rt         = tmp.rectTransform;
        rt.anchorMin   = anchorMin;
        rt.anchorMax   = anchorMax;
        rt.offsetMin   = rt.offsetMax = Vector2.zero;
    }

    private static void MakeButton(string name, RectTransform parent,
        string label, Vector2 anchorMin, Vector2 anchorMax, Action onClick)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);

        var img   = go.AddComponent<Image>();
        img.color = ColBtnBg;

        var rt        = img.rectTransform;
        rt.anchorMin  = anchorMin;
        rt.anchorMax  = anchorMax;
        rt.offsetMin  = rt.offsetMax = Vector2.zero;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => onClick?.Invoke());

        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);

        var tmp        = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text       = label;
        tmp.fontSize   = 44f;
        tmp.color      = ColBtnText;
        tmp.fontStyle  = FontStyles.Bold;
        tmp.alignment  = TextAlignmentOptions.Center;

        var textRT     = tmp.rectTransform;
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = textRT.offsetMax = Vector2.zero;
    }
}
