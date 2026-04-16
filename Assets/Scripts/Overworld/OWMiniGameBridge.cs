using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Pont entre un mini-jeu et l'overworld.
/// Ajoute un bouton "Retour" flottant en HUD dans chaque mini-jeu.
/// OWGameManager persiste entre les scènes (DontDestroyOnLoad).
/// </summary>
public class OWMiniGameBridge : MonoBehaviour
{
    [Header("HUD Retour")]
    [SerializeField] private bool showReturnButton    = true;
    [SerializeField] private bool countAsCompletion   = true;

    [Header("Position du bouton (Viewport)")]
    [SerializeField] private Vector2 buttonAnchorMin  = new Vector2(0f, 0.9f);
    [SerializeField] private Vector2 buttonAnchorMax  = new Vector2(0.3f, 1f);

    private void Start()
    {
        EnsureOWGameManager();

        if (showReturnButton)
            BuildReturnButton();
    }

    // ── Bouton retour ─────────────────────────────────────────────────────────

    private void BuildReturnButton()
    {
        // Canvas Screen Space Overlay
        var canvasGO       = new GameObject("OWReturnCanvas");
        var canvas         = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        var scaler         = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight  = 1f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // EventSystem si absent
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Panel bouton
        var btnGO          = new GameObject("ReturnButton");
        btnGO.transform.SetParent(canvasGO.transform, false);

        var img            = btnGO.AddComponent<Image>();
        img.color          = new Color(0f, 0f, 0f, 0.65f);

        var btn            = btnGO.AddComponent<Button>();
        var colors         = btn.colors;
        colors.highlightedColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        colors.pressedColor     = new Color(0.4f, 0.4f, 0.4f, 1f);
        btn.colors         = colors;
        btn.onClick.AddListener(ReturnToOverworld);

        var rect           = btnGO.GetComponent<RectTransform>();
        rect.anchorMin     = buttonAnchorMin;
        rect.anchorMax     = buttonAnchorMax;
        rect.offsetMin     = new Vector2(8f,  4f);
        rect.offsetMax     = new Vector2(-8f, -4f);

        // Texte du bouton
        var textGO         = new GameObject("Label");
        textGO.transform.SetParent(btnGO.transform, false);

        var tmp            = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text           = "← Overworld";
        tmp.fontSize       = 24f;
        tmp.color          = Color.white;
        tmp.alignment      = TextAlignmentOptions.Center;
        tmp.fontStyle      = FontStyles.Bold;

        var textRect       = tmp.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;
    }

    // ── Retour ────────────────────────────────────────────────────────────────

    /// <summary>Déclenche le retour vers l'overworld depuis ce mini-jeu.</summary>
    public void ReturnToOverworld()
    {
        if (OWGameManager.Instance != null)
            OWGameManager.Instance.ReturnToOverworld(countAsCompletion);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(OWGameManager.SceneOverworld);
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private void EnsureOWGameManager()
    {
        if (OWGameManager.Instance != null) return;
        // Si on arrive directement dans le mini-jeu sans passer par l'overworld,
        // on crée un OWGameManager minimal.
        var go = new GameObject("OWGameManager");
        go.AddComponent<OWGameManager>();
        Debug.LogWarning("[OWMiniGameBridge] OWGameManager créé à la volée (démarrage direct depuis mini-jeu).");
    }
}
