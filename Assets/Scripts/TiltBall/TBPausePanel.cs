using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panneau de pause du TiltBall.
///
/// Construit de manière procédurale dans le Canvas HUD.
/// S'affiche lorsque le joueur appuie sur le bouton menu en bas-gauche.
///
/// Boutons :
///   • REPRENDRE — ferme le panneau et rétablit Time.timeScale = 1
///   • MENU      — retourne au menu principal via TBGameManager.GoToMenu()
/// </summary>
public class TBPausePanel : MonoBehaviour
{
    // ── Palette ───────────────────────────────────────────────────────────────

    private static readonly Color ColOverlay   = new Color(0f,  0f,  0f,  0.72f);
    private static readonly Color ColPanelBg   = new Color(0f,  0f,  0f,  0.88f);
    private static readonly Color ColBtnBg     = new Color(1f,  1f,  1f,  0.10f);
    private static readonly Color ColBtnHover  = new Color(1f,  1f,  1f,  0.20f);
    private static readonly Color ColSep       = new Color(1f,  1f,  1f,  0.15f);
    private static readonly Color ColTitleTxt  = Color.white;
    private static readonly Color ColReturnTxt = new Color(0.18f, 0.82f, 0.22f, 1f);   // vert player
    private static readonly Color ColMenuTxt   = new Color(1f,   1f,   1f,  0.80f);

    // ── État ──────────────────────────────────────────────────────────────────

    private static TBPausePanel _instance;

    private GameObject _root;
    private bool       _visible;

    // ── Création statique ─────────────────────────────────────────────────────

    /// <summary>
    /// Crée le panneau de pause et l'attache au Canvas passé en paramètre.
    /// Le panneau est caché par défaut.
    /// </summary>
    public static TBPausePanel Create(RectTransform canvasRT)
    {
        var go = new GameObject("PausePanel");
        go.transform.SetParent(canvasRT, false);

        var panel     = go.AddComponent<TBPausePanel>();
        panel.Build(canvasRT);
        panel.SetVisible(false);

        _instance = panel;
        return panel;
    }

    /// <summary>Ouvre ou ferme le panneau depuis n'importe où.</summary>
    public static void Toggle() => _instance?.ToggleInternal();

    // ── Construction ──────────────────────────────────────────────────────────

    private void Build(RectTransform canvasRT)
    {
        _root = gameObject;

        // ── Fond semi-opaque couvrant tout l'écran ────────────────────────────
        var overlayImg        = _root.AddComponent<Image>();
        overlayImg.color      = ColOverlay;
        overlayImg.raycastTarget = true;   // bloque les touches au HUD sous-jacent

        var rootRT            = overlayImg.rectTransform;
        rootRT.anchorMin      = Vector2.zero;
        rootRT.anchorMax      = Vector2.one;
        rootRT.offsetMin      = rootRT.offsetMax = Vector2.zero;

        // ── Panneau centré ────────────────────────────────────────────────────
        var panelGO           = new GameObject("Panel");
        panelGO.transform.SetParent(_root.transform, false);

        var panelImg          = panelGO.AddComponent<Image>();
        panelImg.sprite       = SpriteGenerator.CreateWhiteSquare();
        panelImg.color        = ColPanelBg;
        panelImg.raycastTarget = true;

        var panelRT           = panelImg.rectTransform;
        panelRT.anchorMin     = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax     = new Vector2(0.5f, 0.5f);
        panelRT.pivot         = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta     = new Vector2(480f, 360f);
        panelRT.anchoredPosition = Vector2.zero;

        // ── Titre ─────────────────────────────────────────────────────────────
        var titleGO  = new GameObject("Title");
        titleGO.transform.SetParent(panelGO.transform, false);
        var titleTmp = titleGO.AddComponent<TextMeshProUGUI>();
        titleTmp.text       = "PAUSE";
        titleTmp.fontSize   = 52f;
        titleTmp.color      = ColTitleTxt;
        titleTmp.alignment  = TextAlignmentOptions.Center;
        titleTmp.fontStyle  = FontStyles.Bold;
        titleTmp.raycastTarget = false;
        TBUIStyle.ApplyFont(titleTmp);
        var titleRT         = titleTmp.rectTransform;
        titleRT.anchorMin   = new Vector2(0f, 0.65f);
        titleRT.anchorMax   = new Vector2(1f, 1f);
        titleRT.offsetMin   = titleRT.offsetMax = Vector2.zero;

        // ── Séparateur ────────────────────────────────────────────────────────
        MakeSepH(panelRT, 0.62f);

        // ── Bouton REPRENDRE ──────────────────────────────────────────────────
        MakePanelButton(panelRT,
            "BtnReturn",
            "▶  REPRENDRE",
            ColReturnTxt,
            new Vector2(0.08f, 0.33f),
            new Vector2(0.92f, 0.60f),
            OnClickReturn);

        // ── Bouton MENU ───────────────────────────────────────────────────────
        MakePanelButton(panelRT,
            "BtnMenu",
            "← MENU",
            ColMenuTxt,
            new Vector2(0.08f, 0.05f),
            new Vector2(0.92f, 0.30f),
            OnClickMenu);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void MakeSepH(RectTransform parent, float anchorY)
    {
        var go  = new GameObject("SepH");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite        = SpriteGenerator.CreateWhiteSquare();
        img.color         = ColSep;
        img.raycastTarget = false;
        var rt            = img.rectTransform;
        rt.anchorMin      = new Vector2(0.05f, anchorY);
        rt.anchorMax      = new Vector2(0.95f, anchorY);
        rt.sizeDelta      = new Vector2(0f, 2f);
    }

    private static void MakePanelButton(RectTransform parent, string goName,
        string label, Color textColor,
        Vector2 anchorMin, Vector2 anchorMax,
        UnityEngine.Events.UnityAction onClick)
    {
        var btnGO           = new GameObject(goName);
        btnGO.transform.SetParent(parent, false);

        var img             = btnGO.AddComponent<Image>();
        img.sprite          = SpriteGenerator.CreateWhiteSquare();
        img.color           = ColBtnBg;

        var rt              = img.rectTransform;
        rt.anchorMin        = anchorMin;
        rt.anchorMax        = anchorMax;
        rt.offsetMin        = rt.offsetMax = Vector2.zero;

        var btn             = btnGO.AddComponent<Button>();
        btn.targetGraphic   = img;

        // Couleur de survol légèrement plus claire
        var colors          = btn.colors;
        colors.normalColor      = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 2f);   // multiplie l'alpha de l'image
        colors.pressedColor     = new Color(0.7f, 0.7f, 0.7f, 1f);
        colors.selectedColor    = Color.white;
        btn.colors          = colors;

        btn.onClick.AddListener(onClick);

        var labelGO         = new GameObject("Label");
        labelGO.transform.SetParent(btnGO.transform, false);
        var tmp             = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text            = label;
        tmp.fontSize        = 36f;
        tmp.color           = textColor;
        tmp.alignment       = TextAlignmentOptions.Center;
        tmp.fontStyle       = FontStyles.Bold;
        tmp.raycastTarget   = false;
        TBUIStyle.ApplyFont(tmp);
        var textRT          = tmp.rectTransform;
        textRT.anchorMin    = Vector2.zero;
        textRT.anchorMax    = Vector2.one;
        textRT.offsetMin    = textRT.offsetMax = Vector2.zero;
    }

    // ── Visibilité / Pause ────────────────────────────────────────────────────

    private void ToggleInternal()
    {
        SetVisible(!_visible);
    }

    /// <summary>Affiche ou masque le panneau et ajuste Time.timeScale.</summary>
    public void SetVisible(bool show)
    {
        _visible = show;
        _root.SetActive(show);
        Time.timeScale = show ? 0f : 1f;
    }

    // ── Callbacks boutons ─────────────────────────────────────────────────────

    private void OnClickReturn()
    {
        SetVisible(false);
        // Réinitialise le joystick : évite une direction bloquée si le joueur
        // a levé le doigt pendant la pause (le OnPointerUp n'a pas été reçu
        // car le panneau bloquait les raycasts).
        TBJoystick.Instance?.ResetInput();
    }

    private void OnClickMenu()
    {
        // Remettre timeScale à 1 avant la transition de scène
        Time.timeScale = 1f;
        TBGameManager.Instance?.GoToMenu();
    }

    // ── Cleanup ───────────────────────────────────────────────────────────────

    private void OnDestroy()
    {
        // S'assurer que timeScale est réinitialisé si le panel est détruit en cours de jeu
        if (_visible) Time.timeScale = 1f;
        if (_instance == this) _instance = null;
    }
}
