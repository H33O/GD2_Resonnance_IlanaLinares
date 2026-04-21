using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Widget de score affiché en permanence dans le menu.
///
/// Layout haut-gauche :
///   ScorePanel  → label "SCORE TOTAL" + valeur + bouton [DÉTAILS]
///   DetailsModal → sous-panel plein écran avec 3 onglets :
///     - Game &amp; Watch
///     - Bubble Shooter
///     - Ball &amp; Goal
///   Chaque onglet liste tous les runs enregistrés depuis le début de la partie.
///
/// Appelé par <see cref="MenuMainHud.Init"/> à la place du widget score simple.
/// </summary>
public class MenuScorePanel : MonoBehaviour
{
    // ── Palette ───────────────────────────────────────────────────────────────

    private static readonly Color ColWidgetBg   = new Color(0.04f, 0.04f, 0.08f, 0.85f);
    private static readonly Color ColLabel      = new Color(1f, 1f, 1f, 0.45f);
    private static readonly Color ColValue      = Color.white;
    private static readonly Color ColDetailsBg  = new Color(0.04f, 0.04f, 0.10f, 0.96f);
    private static readonly Color ColTabActive  = new Color(0.20f, 0.55f, 1.00f, 1f);
    private static readonly Color ColTabIdle    = new Color(0.12f, 0.12f, 0.20f, 1f);
    private static readonly Color ColTabText    = Color.white;
    private static readonly Color ColScrollBg   = new Color(0.06f, 0.06f, 0.12f, 1f);
    private static readonly Color ColRunEntry   = new Color(1f, 1f, 1f, 0.80f);
    private static readonly Color ColNoScore    = new Color(1f, 1f, 1f, 0.30f);
    private static readonly Color ColCloseBtn   = new Color(0.20f, 0.20f, 0.30f, 1f);

    // ── Layout ────────────────────────────────────────────────────────────────

    private const float PanelW     = 320f;
    private const float PanelH     = 140f;
    private const float MarginX    = 32f;
    private const float MarginY    = 48f;
    private const float ModalW     = 860f;
    private const float ModalH     = 1400f;
    private const float TabH       = 88f;
    private const float EntryH     = 60f;

    // ── Références ────────────────────────────────────────────────────────────

    private TextMeshProUGUI totalScoreLabel;
    private GameObject      detailsModal;
    private RectTransform   modalRT;

    // Onglets
    private readonly string[] tabNames  = { "Game & Watch", "Bubble Shooter", "Ball & Goal" };
    private readonly GameType[] tabTypes = { GameType.GameAndWatch, GameType.BubbleShooter, GameType.BallAndGoal };

    private Image[]         tabBackgrounds;
    private TextMeshProUGUI[] tabLabels;
    private GameObject[]    tabContents;
    private int             activeTab = 0;

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>Construit le widget dans le canvas fourni.</summary>
    public void Init(RectTransform canvasRT)
    {
        BuildScoreWidget(canvasRT);
        BuildDetailsModal(canvasRT);

        // Écouter les nouveaux scores pour rafraîchir le total en temps réel
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreAdded += OnScoreAdded;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnDestroy()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreAdded -= OnScoreAdded;
    }

    private void OnScoreAdded(GameType type, int score)
    {
        RefreshTotal();
        if (detailsModal != null && detailsModal.activeSelf)
            RefreshTab(activeTab);
    }

    // ── Widget principal (haut-gauche, entièrement cliquable) ─────────────────

    private void BuildScoreWidget(RectTransform parent)
    {
        // Conteneur — c'est lui qui est le bouton
        var widgetGO  = new GameObject("ScorePanel");
        widgetGO.transform.SetParent(parent, false);

        var img          = widgetGO.AddComponent<Image>();
        img.sprite       = SpriteGenerator.CreateWhiteSquare();
        img.color        = ColWidgetBg;

        var rt           = img.rectTransform;
        rt.anchorMin     = new Vector2(0f, 1f);
        rt.anchorMax     = new Vector2(0f, 1f);
        rt.pivot         = new Vector2(0f, 1f);
        rt.sizeDelta     = new Vector2(PanelW, PanelH);
        rt.anchoredPosition = new Vector2(MarginX, -MarginY);

        // Libellé "SCORE TOTAL"
        MakeText("ScoreLabel", rt, "SCORE TOTAL",
            fontSize: 20f, style: FontStyles.Bold, color: ColLabel,
            anchorMin: new Vector2(0f, 0.55f), anchorMax: Vector2.one,
            offsetMin: new Vector2(14f, 0f),   offsetMax: new Vector2(-14f, 0f));

        // Valeur numérique — occupe tout le bas du widget
        var valueTMP = MakeText("ScoreValue", rt, "0",
            fontSize: 40f, style: FontStyles.Bold, color: ColValue,
            anchorMin: Vector2.zero,             anchorMax: new Vector2(1f, 0.62f),
            offsetMin: new Vector2(14f, 4f),     offsetMax: new Vector2(-14f, 0f),
            alignment: TextAlignmentOptions.BottomLeft);
        totalScoreLabel = valueTMP.GetComponent<TextMeshProUGUI>();

        // Le widget entier est le bouton
        var btn        = widgetGO.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(OpenDetails);

        RefreshTotal();
    }

    // ── Modal de détails ──────────────────────────────────────────────────────

    private void BuildDetailsModal(RectTransform parent)
    {
        // Fond assombrissant plein écran
        detailsModal = new GameObject("ScoreDetailsModal");
        detailsModal.transform.SetParent(parent, false);

        var overlay    = detailsModal.AddComponent<Image>();
        overlay.sprite = SpriteGenerator.CreateWhiteSquare();
        overlay.color  = new Color(0f, 0f, 0f, 0.60f);

        var overlayRT  = overlay.rectTransform;
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.offsetMin = overlayRT.offsetMax = Vector2.zero;

        // Capturer les clics sur le fond pour fermer
        var overlayBtn = detailsModal.AddComponent<Button>();
        overlayBtn.targetGraphic = overlay;
        overlayBtn.transition    = Selectable.Transition.None;
        overlayBtn.onClick.AddListener(CloseDetails);

        // Panneau central
        var panelGO    = new GameObject("DetailsPanel");
        panelGO.transform.SetParent(detailsModal.transform, false);

        var panelImg   = panelGO.AddComponent<Image>();
        panelImg.sprite = SpriteGenerator.CreateWhiteSquare();
        panelImg.color  = ColDetailsBg;
        panelImg.raycastTarget = true;  // bloque le clic de fermeture du fond

        modalRT              = panelImg.rectTransform;
        modalRT.anchorMin    = new Vector2(0.5f, 0.5f);
        modalRT.anchorMax    = new Vector2(0.5f, 0.5f);
        modalRT.pivot        = new Vector2(0.5f, 0.5f);
        modalRT.sizeDelta    = new Vector2(ModalW, ModalH);
        modalRT.anchoredPosition = Vector2.zero;

        // Stopper la propagation du clic vers le fond
        var panelBtn = panelGO.AddComponent<Button>();
        panelBtn.targetGraphic = panelImg;
        panelBtn.transition    = Selectable.Transition.None;
        // pas de listener → absorbe le clic sans fermer

        BuildModalHeader(modalRT);
        BuildModalTabs(modalRT);
        BuildModalContent(modalRT);
        BuildCloseButton(modalRT);

        detailsModal.SetActive(false);
    }

    private void BuildModalHeader(RectTransform parent)
    {
        MakeText("ModalTitle", parent, "SCORES — HISTORIQUE",
            fontSize: 44f, style: FontStyles.Bold, color: ColValue,
            anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(1f, 1f),
            offsetMin: new Vector2(30f, -110f), offsetMax: new Vector2(-30f, -20f),
            alignment: TextAlignmentOptions.Center);

        // Séparateur
        var sepGO  = new GameObject("Separator");
        sepGO.transform.SetParent(parent, false);
        var sepImg = sepGO.AddComponent<Image>();
        sepImg.sprite = SpriteGenerator.CreateWhiteSquare();
        sepImg.color  = new Color(1f, 1f, 1f, 0.12f);
        sepImg.raycastTarget = false;
        var sepRT    = sepImg.rectTransform;
        sepRT.anchorMin = new Vector2(0f, 1f);
        sepRT.anchorMax = new Vector2(1f, 1f);
        sepRT.offsetMin = new Vector2(20f, -122f);
        sepRT.offsetMax = new Vector2(-20f, -120f);
    }

    private void BuildModalTabs(RectTransform parent)
    {
        int count        = tabNames.Length;
        tabBackgrounds   = new Image[count];
        tabLabels        = new TextMeshProUGUI[count];

        float tabW       = (ModalW - 40f) / count;
        float tabY       = -140f;     // depuis le haut du modal

        for (int i = 0; i < count; i++)
        {
            int idx = i;

            var tabGO  = new GameObject($"Tab_{tabNames[i]}");
            tabGO.transform.SetParent(parent, false);

            var tabImg = tabGO.AddComponent<Image>();
            tabImg.sprite = SpriteGenerator.CreateWhiteSquare();
            tabImg.color  = (i == 0) ? ColTabActive : ColTabIdle;
            tabBackgrounds[i] = tabImg;

            var tabRT  = tabImg.rectTransform;
            tabRT.anchorMin = new Vector2(0f, 1f);
            tabRT.anchorMax = new Vector2(0f, 1f);
            tabRT.pivot     = new Vector2(0f, 1f);
            tabRT.sizeDelta = new Vector2(tabW - 4f, TabH);
            tabRT.anchoredPosition = new Vector2(20f + i * (tabW), tabY);

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(tabGO.transform, false);
            var tmp     = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text    = tabNames[i];
            tmp.fontSize = 22f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color   = ColTabText;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            var labelRT = tmp.rectTransform;
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = labelRT.offsetMax = Vector2.zero;
            tabLabels[i] = tmp;

            var btn     = tabGO.AddComponent<Button>();
            btn.targetGraphic = tabImg;
            btn.onClick.AddListener(() => SelectTab(idx));
        }
    }

    private void BuildModalContent(RectTransform parent)
    {
        int count    = tabNames.Length;
        tabContents  = new GameObject[count];

        float contentTop    = -140f - TabH - 8f;  // sous les onglets
        float contentBottom = 80f;                 // au-dessus du bouton fermer

        for (int i = 0; i < count; i++)
        {
            // Zone scrollable pour chaque onglet
            var contentGO  = new GameObject($"Content_{tabNames[i]}");
            contentGO.transform.SetParent(parent, false);
            tabContents[i] = contentGO;

            var bgImg = contentGO.AddComponent<Image>();
            bgImg.sprite = SpriteGenerator.CreateWhiteSquare();
            bgImg.color  = ColScrollBg;
            bgImg.raycastTarget = false;

            var contentRT  = bgImg.rectTransform;
            contentRT.anchorMin = new Vector2(0f, 0f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.offsetMin = new Vector2(20f, contentBottom);
            contentRT.offsetMax = new Vector2(-20f, contentTop);

            // ScrollRect
            var scrollRect = contentGO.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical   = true;
            scrollRect.scrollSensitivity = 30f;

            // Viewport
            var viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(contentGO.transform, false);
            var viewportImg = viewportGO.AddComponent<Image>();
            viewportImg.color = Color.clear;
            viewportImg.raycastTarget = false;
            var mask = viewportGO.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            var viewportRT = viewportImg.rectTransform;
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.offsetMin = viewportRT.offsetMax = Vector2.zero;

            // Liste des scores (conteneur scrollable)
            var listGO  = new GameObject("List");
            listGO.transform.SetParent(viewportGO.transform, false);
            var listRT  = listGO.AddComponent<RectTransform>();
            listRT.anchorMin = new Vector2(0f, 1f);
            listRT.anchorMax = new Vector2(1f, 1f);
            listRT.pivot     = new Vector2(0.5f, 1f);
            listRT.offsetMin = listRT.offsetMax = Vector2.zero;

            var vlg = listGO.AddComponent<VerticalLayoutGroup>();
            vlg.spacing           = 4f;
            vlg.padding           = new RectOffset(16, 16, 10, 10);
            vlg.childAlignment    = TextAnchor.UpperLeft;
            vlg.childControlWidth  = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;

            var csf = listGO.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content  = listRT;
            scrollRect.viewport = viewportRT;

            contentGO.SetActive(i == 0);
        }
    }

    private void BuildCloseButton(RectTransform parent)
    {
        var btnGO  = new GameObject("CloseButton");
        btnGO.transform.SetParent(parent, false);

        var btnImg = btnGO.AddComponent<Image>();
        btnImg.sprite = SpriteGenerator.CreateWhiteSquare();
        btnImg.color  = ColCloseBtn;

        var btnRT  = btnImg.rectTransform;
        btnRT.anchorMin = new Vector2(0f, 0f);
        btnRT.anchorMax = new Vector2(1f, 0f);
        btnRT.pivot     = new Vector2(0.5f, 0f);
        btnRT.sizeDelta = new Vector2(0f, 72f);
        btnRT.offsetMin = new Vector2(20f, 10f);
        btnRT.offsetMax = new Vector2(-20f, 82f);

        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(btnGO.transform, false);
        var tmp     = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text    = "FERMER";
        tmp.fontSize = 32f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color   = ColTabText;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        var labelRT = tmp.rectTransform;
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = labelRT.offsetMax = Vector2.zero;

        var btn     = btnGO.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(CloseDetails);
    }

    // ── Logique des onglets ───────────────────────────────────────────────────

    private void SelectTab(int idx)
    {
        for (int i = 0; i < tabNames.Length; i++)
        {
            tabBackgrounds[i].color = (i == idx) ? ColTabActive : ColTabIdle;
            if (tabContents[i] != null) tabContents[i].SetActive(i == idx);
        }
        activeTab = idx;
        RefreshTab(idx);
    }

    private void RefreshTab(int idx)
    {
        if (tabContents == null || idx >= tabContents.Length) return;

        var contentGO = tabContents[idx];
        if (contentGO == null) return;

        var scrollRect = contentGO.GetComponent<ScrollRect>();
        if (scrollRect == null || scrollRect.content == null) return;

        var listRT = scrollRect.content;

        // Vider l'ancienne liste
        for (int c = listRT.childCount - 1; c >= 0; c--)
            Destroy(listRT.GetChild(c).gameObject);

        // Peupler avec les scores
        IReadOnlyList<int> scores = ScoreManager.Instance != null
            ? ScoreManager.Instance.GetAllScores(tabTypes[idx])
            : new List<int>();

        if (scores.Count == 0)
        {
            AddEntryRow(listRT, "Aucun score enregistré", ColNoScore, isBold: false);
        }
        else
        {
            // Du plus récent au plus ancien
            for (int i = scores.Count - 1; i >= 0; i--)
            {
                int runNumber = scores.Count - i;
                string label  = $"Run #{runNumber}   {scores[i]:N0} pts";
                AddEntryRow(listRT, label, ColRunEntry, isBold: false);
            }
        }
    }

    private void AddEntryRow(RectTransform parent, string text, Color color, bool isBold)
    {
        var go  = new GameObject("Entry");
        go.transform.SetParent(parent, false);

        var rt  = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, EntryH);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = 28f;
        tmp.fontStyle = isBold ? FontStyles.Bold : FontStyles.Normal;
        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.raycastTarget = false;
    }

    // ── Ouverture / fermeture ─────────────────────────────────────────────────

    private void OpenDetails()
    {
        if (ScoreManager.Instance == null) return;

        detailsModal.SetActive(true);
        SelectTab(0);
    }

    private void CloseDetails()
    {
        if (detailsModal != null)
            detailsModal.SetActive(false);
    }

    // ── Rafraîchissement ──────────────────────────────────────────────────────

    private void RefreshTotal()
    {
        if (totalScoreLabel == null) return;

        int total = ScoreManager.Instance != null ? ScoreManager.Instance.GetTotalScore() : 0;
        totalScoreLabel.text = total.ToString("N0");
    }

    // ── Helpers UI ────────────────────────────────────────────────────────────

    private static GameObject MakeText(string name, RectTransform parent,
        string text, float fontSize, FontStyles style, Color color,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin = default, Vector2 offsetMax = default,
        TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft)
    {
        var go         = new GameObject(name);
        go.transform.SetParent(parent, false);

        var tmp        = go.AddComponent<TextMeshProUGUI>();
        tmp.text       = text;
        tmp.fontSize   = fontSize;
        tmp.fontStyle  = style;
        tmp.color      = color;
        tmp.alignment  = alignment;
        tmp.raycastTarget = false;

        var rt         = tmp.rectTransform;
        rt.anchorMin   = anchorMin;
        rt.anchorMax   = anchorMax;
        rt.offsetMin   = offsetMin;
        rt.offsetMax   = offsetMax;

        return go;
    }
}
