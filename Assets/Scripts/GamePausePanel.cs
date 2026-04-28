using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panneau de pause générique, utilisable par tous les mini-jeux du projet.
///
/// Usage :
///   GamePausePanel.Create(canvasRT, onResume: null, returnToMenu: GoToMenu);
///
/// Le panneau est construit de façon procédurale avec l'esthétique du projet
/// (Michroma, carré noir semi-opaque, palette TiltBall).
///
/// Boutons :
///   • REPRENDRE — ferme le panneau, rétablit Time.timeScale = 1, appelle <see cref="onResume"/>.
///   • MENU      — rétablit Time.timeScale = 1, appelle <see cref="onMenu"/>.
///
/// Le bouton II (pause) est créé séparément via <see cref="CreatePauseButton"/>.
/// </summary>
public class GamePausePanel : MonoBehaviour
{
    // ── Palette ───────────────────────────────────────────────────────────────

    private static readonly Color ColOverlay  = new Color(0f,  0f,  0f,  0.72f);
    private static readonly Color ColPanelBg  = new Color(0f,  0f,  0f,  0.88f);
    private static readonly Color ColBtnBg    = new Color(1f,  1f,  1f,  0.10f);
    private static readonly Color ColSep      = new Color(1f,  1f,  1f,  0.15f);
    private static readonly Color ColReturnTxt = new Color(0.18f, 0.82f, 0.22f, 1f);
    private static readonly Color ColMenuTxt  = new Color(1f,  1f,  1f,  0.80f);
    private static readonly Color ColMenuBg   = new Color(0f,  0f,  0f,  0.60f);

    // ── Singleton par scène ───────────────────────────────────────────────────

    private static GamePausePanel _instance;

    // ── Callbacks ─────────────────────────────────────────────────────────────

    private Action _onResume;
    private Action _onMenu;

    // ── État ──────────────────────────────────────────────────────────────────

    private bool _visible;

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>
    /// Crée le panneau de pause et l'attache au <paramref name="canvasRT"/> donné.
    /// </summary>
    /// <param name="canvasRT">RectTransform racine du Canvas.</param>
    /// <param name="onResume">Callback optionnel appelé quand le joueur reprend.</param>
    /// <param name="onMenu">Callback qui navigue vers le menu (LoadScene, GoToMenu, etc.).</param>
    /// <returns>L'instance créée.</returns>
    public static GamePausePanel Create(RectTransform canvasRT, Action onResume, Action onMenu)
    {
        var go    = new GameObject("GamePausePanel");
        go.transform.SetParent(canvasRT, false);

        var panel      = go.AddComponent<GamePausePanel>();
        panel._onResume = onResume;
        panel._onMenu   = onMenu;
        panel.Build();
        panel.SetVisible(false);

        _instance = panel;
        return panel;
    }

    /// <summary>
    /// Crée le bouton II (pause) en bas-gauche du canvas.
    /// Doit être créé AVANT le panneau pour que l'ordre de sibling soit correct
    /// (panneau au-dessus du bouton).
    /// </summary>
    public static void CreatePauseButton(RectTransform canvasRT)
    {
        var go  = new GameObject("MenuButton");
        go.transform.SetParent(canvasRT, false);

        var img        = go.AddComponent<Image>();
        img.sprite     = SpriteGenerator.CreateWhiteSquare();
        img.color      = ColMenuBg;

        var rt              = img.rectTransform;
        rt.anchorMin        = new Vector2(0f, 0f);
        rt.anchorMax        = new Vector2(0f, 0f);
        rt.pivot            = new Vector2(0f, 0f);
        rt.sizeDelta        = new Vector2(190f, 100f);
        rt.anchoredPosition = new Vector2(50f, 30f);

        var btn           = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => _instance?.ToggleInternal());

        var labelGO      = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        var tmp          = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text         = "II";
        tmp.fontSize     = 36f;
        tmp.color        = new Color(1f, 1f, 1f, 0.85f);
        tmp.alignment    = TextAlignmentOptions.Center;
        tmp.fontStyle    = FontStyles.Bold;
        tmp.raycastTarget = false;
        ApplyFont(tmp);
        var textRT       = tmp.rectTransform;
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = textRT.offsetMax = Vector2.zero;
    }

    /// <summary>Ouvre ou ferme le panneau.</summary>
    public static void Toggle() => _instance?.ToggleInternal();

    // ── Construction ──────────────────────────────────────────────────────────

    private void Build()
    {
        // ── Fond semi-opaque couvrant tout l'écran ────────────────────────────
        var overlayImg           = gameObject.AddComponent<Image>();
        overlayImg.color         = ColOverlay;
        overlayImg.raycastTarget = true;

        var rootRT          = overlayImg.rectTransform;
        rootRT.anchorMin    = Vector2.zero;
        rootRT.anchorMax    = Vector2.one;
        rootRT.offsetMin    = rootRT.offsetMax = Vector2.zero;

        // ── Panneau centré ────────────────────────────────────────────────────
        var panelGO      = new GameObject("Panel");
        panelGO.transform.SetParent(transform, false);

        var panelImg         = panelGO.AddComponent<Image>();
        panelImg.sprite      = SpriteGenerator.CreateWhiteSquare();
        panelImg.color       = ColPanelBg;
        panelImg.raycastTarget = true;

        var panelRT          = panelImg.rectTransform;
        panelRT.anchorMin    = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax    = new Vector2(0.5f, 0.5f);
        panelRT.pivot        = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta    = new Vector2(480f, 360f);
        panelRT.anchoredPosition = Vector2.zero;

        // ── Titre ─────────────────────────────────────────────────────────────
        var titleGO    = new GameObject("Title");
        titleGO.transform.SetParent(panelGO.transform, false);
        var titleTmp   = titleGO.AddComponent<TextMeshProUGUI>();
        titleTmp.text      = "PAUSE";
        titleTmp.fontSize  = 52f;
        titleTmp.color     = Color.white;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.raycastTarget = false;
        ApplyFont(titleTmp);
        var titleRT        = titleTmp.rectTransform;
        titleRT.anchorMin  = new Vector2(0f, 0.65f);
        titleRT.anchorMax  = new Vector2(1f, 1f);
        titleRT.offsetMin  = titleRT.offsetMax = Vector2.zero;

        // ── Séparateur ────────────────────────────────────────────────────────
        MakeSepH(panelRT, 0.62f);

        // ── Bouton REPRENDRE ──────────────────────────────────────────────────
        MakePanelButton(panelRT, "BtnReturn", "▶  REPRENDRE", ColReturnTxt,
            new Vector2(0.08f, 0.33f), new Vector2(0.92f, 0.60f), OnClickReturn);

        // ── Bouton MENU ───────────────────────────────────────────────────────
        MakePanelButton(panelRT, "BtnMenu", "← MENU", ColMenuTxt,
            new Vector2(0.08f, 0.05f), new Vector2(0.92f, 0.30f), OnClickMenu);
    }

    // ── Visibilité ────────────────────────────────────────────────────────────

    private void ToggleInternal() => SetVisible(!_visible);

    /// <summary>Affiche ou masque le panneau et ajuste Time.timeScale.</summary>
    public void SetVisible(bool show)
    {
        _visible           = show;
        gameObject.SetActive(show);
        Time.timeScale     = show ? 0f : 1f;
    }

    // ── Callbacks ─────────────────────────────────────────────────────────────

    private void OnClickReturn()
    {
        SetVisible(false);
        _onResume?.Invoke();
    }

    private void OnClickMenu()
    {
        Time.timeScale = 1f;
        _onMenu?.Invoke();
    }

    private void OnDestroy()
    {
        if (_visible) Time.timeScale = 1f;
        if (_instance == this) _instance = null;
    }

    // ── Helpers UI ────────────────────────────────────────────────────────────

    private static void MakeSepH(RectTransform parent, float anchorY)
    {
        var go  = new GameObject("SepH");
        go.transform.SetParent(parent, false);
        var img       = go.AddComponent<Image>();
        img.sprite    = SpriteGenerator.CreateWhiteSquare();
        img.color     = ColSep;
        img.raycastTarget = false;
        var rt        = img.rectTransform;
        rt.anchorMin  = new Vector2(0.05f, anchorY);
        rt.anchorMax  = new Vector2(0.95f, anchorY);
        rt.sizeDelta  = new Vector2(0f, 2f);
    }

    private static void MakePanelButton(RectTransform parent, string goName,
        string label, Color textColor,
        Vector2 anchorMin, Vector2 anchorMax,
        UnityEngine.Events.UnityAction onClick)
    {
        var btnGO         = new GameObject(goName);
        btnGO.transform.SetParent(parent, false);

        var img           = btnGO.AddComponent<Image>();
        img.sprite        = SpriteGenerator.CreateWhiteSquare();
        img.color         = ColBtnBg;

        var rt            = img.rectTransform;
        rt.anchorMin      = anchorMin;
        rt.anchorMax      = anchorMax;
        rt.offsetMin      = rt.offsetMax = Vector2.zero;

        var btn           = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;

        var colors                  = btn.colors;
        colors.normalColor          = Color.white;
        colors.highlightedColor     = new Color(1f, 1f, 1f, 2f);
        colors.pressedColor         = new Color(0.7f, 0.7f, 0.7f, 1f);
        colors.selectedColor        = Color.white;
        btn.colors                  = colors;

        btn.onClick.AddListener(onClick);

        var labelGO      = new GameObject("Label");
        labelGO.transform.SetParent(btnGO.transform, false);
        var tmp          = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text         = label;
        tmp.fontSize     = 36f;
        tmp.color        = textColor;
        tmp.alignment    = TextAlignmentOptions.Center;
        tmp.fontStyle    = FontStyles.Bold;
        tmp.raycastTarget = false;
        ApplyFont(tmp);
        var textRT       = tmp.rectTransform;
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = textRT.offsetMax = Vector2.zero;
    }

    // ── Police ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Applique Michroma. Tente MenuAssets en priorité, sinon Resources.
    /// </summary>
    private static void ApplyFont(TextMeshProUGUI tmp)
    {
        if (tmp == null) return;

        // Priorité 1 : MenuAssets (initialisé si on vient du menu)
        if (MenuAssets.Font != null)
        {
            tmp.font = MenuAssets.Font;
            return;
        }

        // Priorité 2 : Resources
        var f = Resources.Load<TMP_FontAsset>("Michroma-Regular SDF");
        if (f != null) { tmp.font = f; return; }

#if UNITY_EDITOR
        // Priorité 3 : AssetDatabase (Play Mode direct depuis l'éditeur)
        f = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/font/Michroma-Regular SDF.asset");
        if (f == null)
            f = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/font/Michroma/Michroma-Regular SDF.asset");
        if (f != null) tmp.font = f;
#endif
    }
}
