using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Widget de score du menu principal.
///
/// Le widget haut-gauche affiche le score total persistant et est entièrement cliquable.
/// Un clic ouvre un panneau plein écran avec 3 slides swipables :
///   - Game &amp; Watch
///   - Bubble Shooter
///   - Ball &amp; Goal
///
/// Chaque slide contient :
///   - Le nom du jeu + accent de couleur
///   - Le meilleur score en grand
///   - L'historique complet de toutes les parties (scrollable), du plus récent au plus ancien
///
/// Navigation : boutons ← → + swipe tactile (drag horizontal).
/// </summary>
public class MenuScorePanel : MonoBehaviour
{
    // ── Données des jeux ──────────────────────────────────────────────────────

    private static readonly string[]   GameNames  = { "GAME & WATCH", "BUBBLE SHOOTER", "BALL & GOAL" };
    private static readonly GameType[] GameTypes  = { GameType.GameAndWatch, GameType.BubbleShooter, GameType.BallAndGoal };
    private static readonly Color[]    GameColors =
    {
        new Color(0.35f, 0.75f, 1.00f, 1f),   // bleu ciel  — Game & Watch
        new Color(0.40f, 1.00f, 0.55f, 1f),   // vert       — Bubble Shooter
        new Color(1.00f, 0.60f, 0.20f, 1f),   // orange     — Ball & Goal
    };

    // ── Palette générale ──────────────────────────────────────────────────────

    private static readonly Color ColWidgetBg    = new Color(0.04f, 0.04f, 0.08f, 0.85f);
    private static readonly Color ColWidgetLbl   = new Color(1f, 1f, 1f, 0.45f);
    private static readonly Color ColWidgetVal   = Color.white;
    private static readonly Color ColPanelBg     = new Color(0.04f, 0.04f, 0.10f, 0.98f);
    private static readonly Color ColPanelTitle  = new Color(1f, 1f, 1f, 0.35f);
    private static readonly Color ColBestLbl     = new Color(1f, 1f, 1f, 0.38f);
    private static readonly Color ColHistLbl     = new Color(1f, 1f, 1f, 0.30f);
    private static readonly Color ColEntryBg     = new Color(1f, 1f, 1f, 0.04f);
    private static readonly Color ColEntryBest   = new Color(1.00f, 0.82f, 0.18f, 1f);
    private static readonly Color ColEntryNormal = new Color(1f, 1f, 1f, 0.75f);
    private static readonly Color ColNoScore     = new Color(1f, 1f, 1f, 0.25f);
    private static readonly Color ColNavBtn      = new Color(1f, 1f, 1f, 0.08f);
    private static readonly Color ColNavBtnHov   = new Color(1f, 1f, 1f, 0.18f);
    private static readonly Color ColNavTxt      = new Color(1f, 1f, 1f, 0.80f);
    private static readonly Color ColDot         = new Color(1f, 1f, 1f, 0.25f);
    private static readonly Color ColDotActive   = Color.white;
    private static readonly Color ColCloseBtn    = new Color(1f, 1f, 1f, 0.07f);
    private static readonly Color ColCloseTxt    = new Color(1f, 1f, 1f, 0.55f);
    private static readonly Color ColSep         = new Color(1f, 1f, 1f, 0.08f);

    // ── Layout ────────────────────────────────────────────────────────────────

    private const float WidgetW   = 320f;
    private const float WidgetH   = 140f;
    private const float MarginX   = 32f;
    private const float MarginY   = 48f;
    private const float SlideAnim = 0.38f;   // durée de la transition slide
    private const float EntryH    = 72f;
    private const float NavBtnW   = 110f;
    private const float NavBtnH   = 80f;

    // ── Feedback ─────────────────────────────────────────────────────────────

    private const float RecordFlashDuration  = 0.55f;
    private const float WidgetPulseDuration  = 0.35f;
    private const float WidgetPulseScale     = 1.18f;

    private static readonly Color ColRecordFlash = new Color(1.00f, 0.82f, 0.18f, 0.22f);

    // ── Références runtime ────────────────────────────────────────────────────

    private TextMeshProUGUI totalScoreLabel;
    private RectTransform   widgetRT;   // référence au widget haut-gauche pour le pulse

    // Panel plein écran
    private RectTransform   panelRT;
    private CanvasGroup     panelGroup;

    // Slider (viewport + conteneur des 3 slides)
    private RectTransform   sliderContainer;   // contient les 3 slides côte à côte
    private float           slideWidth;        // largeur d'un slide = largeur du viewport

    // Slides et leurs listes de scores
    private RectTransform[] slideRTs;
    private RectTransform[] scoreListRTs;      // le RectTransform "List" scrollable de chaque slide
    private Image[]         dotImages;

    // Indicateurs dot
    private int             currentSlide = 0;

    // Swipe drag
    private bool   isDragging;
    private float  dragStartX;
    private float  containerStartX;

    // Suivi des records par jeu pour détecter un nouveau meilleur score
    private readonly int[] _previousBest = { -1, -1, -1 };

    // ── Initialisation ────────────────────────────────────────────────────────

    /// <summary>Appelé par <see cref="MenuMainHud.Init"/>.</summary>
    public void Init(RectTransform canvasRT)
    {
        ScoreManager.EnsureExists();
        BuildWidget(canvasRT);
        BuildPanel(canvasRT);

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreAdded += OnScoreAdded;
    }

    private void OnDestroy()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreAdded -= OnScoreAdded;
    }

    private void OnScoreAdded(GameType type, int score)
    {
        int idx = System.Array.IndexOf(GameTypes, type);

        // Détecter si c'est un nouveau record
        bool isNewRecord = false;
        if (idx >= 0 && ScoreManager.Instance != null)
        {
            var scores = ScoreManager.Instance.GetAllScores(type);
            int best   = 0;
            foreach (int s in scores) if (s > best) best = s;

            if (score >= best && score > _previousBest[idx])
            {
                isNewRecord         = true;
                _previousBest[idx]  = score;
            }
        }

        RefreshTotal();

        // Pulse du widget score pour indiquer qu'un score est arrivé
        StopCoroutine(nameof(PulseWidget));
        StartCoroutine(PulseWidget());

        // Si le panel est ouvert, rafraîchir + flash record
        if (panelRT != null && panelRT.gameObject.activeSelf && idx >= 0)
        {
            PopulateSlide(idx);
            if (isNewRecord)
                StartCoroutine(FlashHistorySlide(idx));
        }
    }

    // ── Widget haut-gauche ────────────────────────────────────────────────────

    private void BuildWidget(RectTransform parent)
    {
        var go  = new GameObject("ScorePanel");
        go.transform.SetParent(parent, false);

        var img          = go.AddComponent<Image>();
        img.sprite       = SpriteGenerator.CreateWhiteSquare();
        img.color        = ColWidgetBg;
        MenuAssets.ApplyButtonSprite(img);

        var rt           = img.rectTransform;
        rt.anchorMin     = new Vector2(0f, 1f);
        rt.anchorMax     = new Vector2(0f, 1f);
        rt.pivot         = new Vector2(0f, 1f);
        rt.sizeDelta     = new Vector2(WidgetW, WidgetH);
        rt.anchoredPosition = new Vector2(MarginX, -MarginY);
        widgetRT         = rt;   // stocker pour le pulse

        // Label "SCORES" centré (pas de valeur numérique visible ici)
        Lbl("ScoreLabel", rt, "SCORES",
            28f, FontStyles.Bold, ColWidgetVal,
            new Vector2(0f, 0.30f), Vector2.one,
            new Vector2(14f, 0f), new Vector2(-14f, 0f),
            TextAlignmentOptions.Center);

        // Sous-label indicatif
        Lbl("ScoreHint", rt, "Voir l'historique →",
            18f, FontStyles.Normal, ColWidgetLbl,
            Vector2.zero, new Vector2(1f, 0.42f),
            new Vector2(14f, 4f), new Vector2(-14f, 0f),
            TextAlignmentOptions.Center);

        var btn        = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors     = btn.colors;
        colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
        colors.pressedColor     = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.fadeDuration     = 0.08f;
        btn.colors = colors;
        btn.onClick.AddListener(OpenPanel);
    }

    // ── Panel plein écran ─────────────────────────────────────────────────────

    private void BuildPanel(RectTransform canvasRT)
    {
        var go = new GameObject("ScoreDetailsPanel");
        go.transform.SetParent(canvasRT, false);

        var bg       = go.AddComponent<Image>();
        bg.sprite    = SpriteGenerator.CreateWhiteSquare();
        bg.color     = ColPanelBg;
        bg.raycastTarget = true;

        panelRT          = bg.rectTransform;
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = panelRT.offsetMax = Vector2.zero;

        panelGroup            = go.AddComponent<CanvasGroup>();
        panelGroup.alpha      = 0f;
        panelGroup.blocksRaycasts = false;

        // ── Titre du panel ────────────────────────────────────────────────────
        Lbl("PanelTitle", panelRT, "HISTORIQUE DES SCORES",
            38f, FontStyles.Bold, ColPanelTitle,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -110f), new Vector2(0f, -20f),
            TextAlignmentOptions.Center);

        Sep("TitleSep", panelRT,
            new Vector2(0.04f, 1f), new Vector2(0.96f, 1f),
            new Vector2(0f, -116f), new Vector2(0f, -112f));

        // ── Viewport (zone d'affichage des slides) ────────────────────────────
        var viewportGO  = new GameObject("Viewport");
        viewportGO.transform.SetParent(panelRT, false);
        var viewportImg = viewportGO.AddComponent<Image>();
        viewportImg.color = Color.clear;
        var mask        = viewportGO.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        var viewportRT  = viewportImg.rectTransform;
        viewportRT.anchorMin = new Vector2(0f, 0f);
        viewportRT.anchorMax = new Vector2(1f, 1f);
        viewportRT.offsetMin = new Vector2(0f,  160f);   // espace bas (nav + dots + close)
        viewportRT.offsetMax = new Vector2(0f, -130f);   // espace haut (titre)

        // ── Conteneur des 3 slides (3× largeur) ──────────────────────────────
        var containerGO = new GameObject("SliderContainer");
        containerGO.transform.SetParent(viewportGO.transform, false);
        sliderContainer = containerGO.AddComponent<RectTransform>();

        // Plein haut du viewport, largeur = 3× viewport (rempli après layout)
        sliderContainer.anchorMin       = new Vector2(0f, 0f);
        sliderContainer.anchorMax       = new Vector2(1f, 1f);
        sliderContainer.offsetMin       = sliderContainer.offsetMax = Vector2.zero;

        // Drag handler sur le viewport
        var dragHandler = viewportGO.AddComponent<SliderDragHandler>();
        dragHandler.Init(this);

        // ── Créer les 3 slides ────────────────────────────────────────────────
        slideRTs      = new RectTransform[3];
        scoreListRTs  = new RectTransform[3];

        for (int i = 0; i < 3; i++)
            BuildSlide(i, viewportRT);

        // ── Navigation ← → ───────────────────────────────────────────────────
        BuildNavButtons(panelRT);

        // ── Dots indicateurs ──────────────────────────────────────────────────
        BuildDots(panelRT);

        // ── Bouton Fermer ─────────────────────────────────────────────────────
        BuildCloseButton(panelRT);

        go.SetActive(false);
    }

    // ── Construction d'un slide ───────────────────────────────────────────────

    private void BuildSlide(int idx, RectTransform viewportRT)
    {
        var go = new GameObject($"Slide_{GameNames[idx]}");
        go.transform.SetParent(sliderContainer, false);

        var rt          = go.AddComponent<RectTransform>();
        rt.anchorMin    = new Vector2(0f, 0f);
        rt.anchorMax    = new Vector2(0f, 1f);
        rt.pivot        = new Vector2(0f, 0.5f);
        rt.sizeDelta    = new Vector2(0f, 0f);
        slideRTs[idx]   = rt;

        // ── Fond du slide ─────────────────────────────────────────────────────
        var bgGO  = new GameObject("SlideBg");
        bgGO.transform.SetParent(rt, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.sprite = SpriteGenerator.CreateWhiteSquare();
        // Fond sombre avec légère teinte de la couleur du jeu
        bgImg.color  = new Color(
            0.05f + GameColors[idx].r * 0.04f,
            0.05f + GameColors[idx].g * 0.04f,
            0.08f + GameColors[idx].b * 0.04f,
            1f);
        bgImg.raycastTarget = false;
        var bgRT  = bgImg.rectTransform;
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

        // ── Accent couleur gauche ─────────────────────────────────────────────
        var accent    = new GameObject("Accent");
        accent.transform.SetParent(rt, false);
        var accentImg = accent.AddComponent<Image>();
        accentImg.sprite       = SpriteGenerator.CreateWhiteSquare();
        accentImg.color        = GameColors[idx];
        accentImg.raycastTarget = false;
        var accentRT  = accentImg.rectTransform;
        accentRT.anchorMin = new Vector2(0f, 0f);
        accentRT.anchorMax = new Vector2(0f, 1f);
        accentRT.offsetMin = new Vector2(0f, 0f);
        accentRT.offsetMax = new Vector2(8f, 0f);

        // ── Nom du jeu ────────────────────────────────────────────────────────
        Lbl($"GameName_{idx}", rt, GameNames[idx],
            54f, FontStyles.Bold, GameColors[idx],
            new Vector2(0.04f, 0.88f), new Vector2(0.96f, 1.0f),
            default, default, TextAlignmentOptions.MidlineLeft);

        // ── Section MEILLEUR SCORE ────────────────────────────────────────────
        Sep($"Sep1_{idx}", rt,
            new Vector2(0.04f, 0.87f), new Vector2(0.96f, 0.87f),
            new Vector2(0f, -1f), Vector2.zero);

        Lbl($"BestLbl_{idx}", rt, "MEILLEUR SCORE",
            26f, FontStyles.Bold, ColBestLbl,
            new Vector2(0.04f, 0.76f), new Vector2(0.96f, 0.86f),
            default, default, TextAlignmentOptions.MidlineLeft);

        var bestVal = Lbl($"BestVal_{idx}", rt, "—",
            88f, FontStyles.Bold, GameColors[idx],
            new Vector2(0.04f, 0.58f), new Vector2(0.96f, 0.78f),
            default, default, TextAlignmentOptions.MidlineLeft);
        // On stocke la ref dans le tag pour la retrouver facilement
        bestVal.gameObject.name = $"BestScore_{idx}";

        // ── Séparateur ────────────────────────────────────────────────────────
        Sep($"Sep2_{idx}", rt,
            new Vector2(0.04f, 0.57f), new Vector2(0.96f, 0.57f),
            new Vector2(0f, -1f), Vector2.zero);

        Lbl($"HistLbl_{idx}", rt, "TOUTES LES PARTIES",
            24f, FontStyles.Bold, ColHistLbl,
            new Vector2(0.04f, 0.50f), new Vector2(0.96f, 0.57f),
            default, default, TextAlignmentOptions.MidlineLeft);

        // ── Liste scrollable ──────────────────────────────────────────────────
        var listViewGO  = new GameObject("ListView");
        listViewGO.transform.SetParent(rt, false);
        var listViewImg = listViewGO.AddComponent<Image>();
        listViewImg.color = Color.clear;
        listViewImg.raycastTarget = false;
        var listViewMask = listViewGO.AddComponent<Mask>();
        listViewMask.showMaskGraphic = false;
        var listViewRT  = listViewImg.rectTransform;
        listViewRT.anchorMin = new Vector2(0.04f, 0.00f);
        listViewRT.anchorMax = new Vector2(0.96f, 0.49f);
        listViewRT.offsetMin = listViewRT.offsetMax = Vector2.zero;

        var scrollRect  = listViewGO.AddComponent<ScrollRect>();
        scrollRect.horizontal        = false;
        scrollRect.vertical          = true;
        scrollRect.scrollSensitivity = 40f;
        scrollRect.movementType      = ScrollRect.MovementType.Clamped;

        var listGO = new GameObject("List");
        listGO.transform.SetParent(listViewGO.transform, false);
        var listRT = listGO.AddComponent<RectTransform>();
        listRT.anchorMin = new Vector2(0f, 1f);
        listRT.anchorMax = new Vector2(1f, 1f);
        listRT.pivot     = new Vector2(0.5f, 1f);
        listRT.offsetMin = listRT.offsetMax = Vector2.zero;

        var vlg = listGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing              = 6f;
        vlg.padding              = new RectOffset(0, 0, 8, 8);
        vlg.childAlignment       = TextAnchor.UpperCenter;
        vlg.childControlWidth    = true;
        vlg.childControlHeight   = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        var csf = listGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content  = listRT;
        scrollRect.viewport = listViewRT;

        scoreListRTs[idx] = listRT;
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    private void BuildNavButtons(RectTransform parent)
    {
        // ← Précédent
        var prevRT = MakeNavButton("BtnPrev", parent,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(NavBtnW, NavBtnH), new Vector2(40f, 110f), "←");
        prevRT.GetComponent<Button>().onClick.AddListener(() => GoToSlide(currentSlide - 1));

        // → Suivant
        var nextRT = MakeNavButton("BtnNext", parent,
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(NavBtnW, NavBtnH), new Vector2(-40f, 110f), "→");
        nextRT.GetComponent<Button>().onClick.AddListener(() => GoToSlide(currentSlide + 1));
    }

    private RectTransform MakeNavButton(string name, RectTransform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 size, Vector2 anchoredPos, string label)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.sprite = SpriteGenerator.CreateWhiteSquare();
        img.color  = ColNavBtn;

        var rt  = img.rectTransform;
        rt.anchorMin       = anchorMin;
        rt.anchorMax       = anchorMax;
        rt.pivot           = pivot;
        rt.sizeDelta       = size;
        rt.anchoredPosition = anchoredPos;

        var lgo = new GameObject("Label");
        lgo.transform.SetParent(rt, false);
        var tmp = lgo.AddComponent<TextMeshProUGUI>();
        tmp.text          = label;
        tmp.fontSize      = 44f;
        tmp.fontStyle     = FontStyles.Bold;
        tmp.color         = ColNavTxt;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        var lrt = tmp.rectTransform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors        = btn.colors;
        colors.normalColor      = Color.white;
        colors.highlightedColor = new Color(1.8f, 1.8f, 1.8f, 1f);
        colors.pressedColor     = new Color(0.6f, 0.6f, 0.6f, 1f);
        colors.fadeDuration     = 0.07f;
        btn.colors = colors;

        return rt;
    }

    // ── Dots ──────────────────────────────────────────────────────────────────

    private void BuildDots(RectTransform parent)
    {
        dotImages = new Image[3];
        float dotSize  = 14f;
        float dotGap   = 22f;
        float totalW   = 3 * dotSize + 2 * (dotGap - dotSize);

        for (int i = 0; i < 3; i++)
        {
            var go  = new GameObject($"Dot_{i}");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = SpriteGenerator.CreateWhiteSquare();
            img.color  = (i == 0) ? ColDotActive : ColDot;
            img.raycastTarget = false;
            dotImages[i] = img;

            var rt  = img.rectTransform;
            rt.anchorMin        = new Vector2(0.5f, 0f);
            rt.anchorMax        = new Vector2(0.5f, 0f);
            rt.pivot            = new Vector2(0.5f, 0f);
            rt.sizeDelta        = new Vector2(dotSize, dotSize);
            float offsetX       = (i - 1) * dotGap;
            rt.anchoredPosition = new Vector2(offsetX, 168f);
        }
    }

    // ── Bouton fermer ─────────────────────────────────────────────────────────

    private void BuildCloseButton(RectTransform parent)
    {
        var go  = new GameObject("BtnClose");
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.sprite = SpriteGenerator.CreateWhiteSquare();
        img.color  = ColCloseBtn;

        var rt  = img.rectTransform;
        rt.anchorMin        = new Vector2(0.5f, 0f);
        rt.anchorMax        = new Vector2(0.5f, 0f);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.sizeDelta        = new Vector2(560f, 72f);
        rt.anchoredPosition = new Vector2(0f, 44f);

        var lgo = new GameObject("Label");
        lgo.transform.SetParent(rt, false);
        var tmp = lgo.AddComponent<TextMeshProUGUI>();
        tmp.text          = "FERMER";
        tmp.fontSize      = 30f;
        tmp.fontStyle     = FontStyles.Bold;
        tmp.color         = ColCloseTxt;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        var lrt = tmp.rectTransform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors        = btn.colors;
        colors.highlightedColor = new Color(1.5f, 1.5f, 1.5f, 1f);
        colors.fadeDuration     = 0.07f;
        btn.colors = colors;
        btn.onClick.AddListener(ClosePanel);
    }

    // ── Navigation slides ─────────────────────────────────────────────────────

    public void GoToSlide(int idx)
    {
        if (idx < 0 || idx >= 3 || idx == currentSlide) return;
        currentSlide = idx;
        UpdateDots();
        PopulateSlide(idx);
        StartCoroutine(AnimateSlider(idx));
    }

    /// <summary>Appelé par le drag handler quand un swipe est complété.</summary>
    public void OnSwipeEnd(float deltaX)
    {
        if (Mathf.Abs(deltaX) < 60f) return;
        GoToSlide(deltaX < 0 ? currentSlide + 1 : currentSlide - 1);
    }

    private IEnumerator AnimateSlider(int targetIdx)
    {
        if (sliderContainer == null) yield break;

        float startX   = sliderContainer.anchoredPosition.x;
        float targetX  = -targetIdx * slideWidth;
        float elapsed  = 0f;

        while (elapsed < SlideAnim)
        {
            elapsed += Time.deltaTime;
            float e  = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / SlideAnim), 3f); // EaseOutCubic
            sliderContainer.anchoredPosition = new Vector2(Mathf.Lerp(startX, targetX, e), 0f);
            yield return null;
        }
        sliderContainer.anchoredPosition = new Vector2(targetX, 0f);
    }

    private void UpdateDots()
    {
        if (dotImages == null) return;
        for (int i = 0; i < dotImages.Length; i++)
            dotImages[i].color = (i == currentSlide) ? ColDotActive : ColDot;
    }

    // ── Positionnement des slides (après layout) ──────────────────────────────

    /// <summary>
    /// Appelé à la fin du premier frame pour que le viewport ait ses dimensions réelles.
    /// </summary>
    private IEnumerator SetupSlideLayout()
    {
        // Attendre un frame que le layout soit calculé
        yield return null;
        yield return null;

        var viewportRT = sliderContainer.parent as RectTransform;
        if (viewportRT == null) yield break;

        slideWidth = viewportRT.rect.width;
        if (slideWidth <= 0f) slideWidth = 1080f;   // fallback sécurisé

        // Conteneur = 3× largeur viewport
        sliderContainer.sizeDelta = new Vector2(slideWidth * 3f, 0f);
        sliderContainer.anchorMin = new Vector2(0f, 0f);
        sliderContainer.anchorMax = new Vector2(0f, 1f);
        sliderContainer.pivot     = new Vector2(0f, 0.5f);
        sliderContainer.anchoredPosition = Vector2.zero;

        // Positionner chaque slide
        for (int i = 0; i < slideRTs.Length; i++)
        {
            var rt        = slideRTs[i];
            rt.sizeDelta  = new Vector2(slideWidth, 0f);
            rt.anchoredPosition = new Vector2(i * slideWidth, 0f);
        }

        // Peupler le premier slide visible
        PopulateSlide(0);
    }

    // ── Population des scores ─────────────────────────────────────────────────

    private void PopulateSlide(int idx)
    {
        if (ScoreManager.Instance == null) return;
        if (scoreListRTs == null || idx >= scoreListRTs.Length) return;

        IReadOnlyList<int> scores = ScoreManager.Instance.GetAllScores(GameTypes[idx]);

        // Meilleur score
        var bestLabel = FindLabelInSlide(idx, $"BestScore_{idx}");
        if (bestLabel != null)
        {
            if (scores.Count == 0)
            {
                bestLabel.text  = "—";
                bestLabel.color = ColNoScore;
            }
            else
            {
                int best       = 0;
                foreach (int s in scores) if (s > best) best = s;
                bestLabel.text  = best.ToString("N0");
                bestLabel.color = GameColors[idx];
            }
        }

        // Historique
        var listRT = scoreListRTs[idx];
        if (listRT == null) return;

        // Vider
        for (int c = listRT.childCount - 1; c >= 0; c--)
            Destroy(listRT.GetChild(c).gameObject);

        if (scores.Count == 0)
        {
            AddHistoryRow(listRT, "Aucune partie jouée", ColNoScore, isRecord: false, isBold: false);
            return;
        }

        // Trouver le meilleur pour le mettre en évidence
        int bestScore = 0;
        foreach (int s in scores) if (s > bestScore) bestScore = s;

        // Du plus récent au plus ancien
        for (int i = scores.Count - 1; i >= 0; i--)
        {
            int  run    = scores.Count - i;
            bool isRec  = (scores[i] == bestScore);
            string txt  = $"Partie #{run}   {scores[i]:N0} pts{(isRec ? "  ★" : "")}";
            AddHistoryRow(listRT, txt,
                isRec ? ColEntryBest : ColEntryNormal,
                isRecord: isRec, isBold: isRec);
        }
    }

    private TextMeshProUGUI FindLabelInSlide(int idx, string goName)
    {
        if (slideRTs == null || idx >= slideRTs.Length) return null;
        var slide = slideRTs[idx];
        if (slide == null) return null;
        var found = slide.Find(goName);
        return found != null ? found.GetComponent<TextMeshProUGUI>() : null;
    }

    private void AddHistoryRow(RectTransform parent, string text, Color col,
        bool isRecord, bool isBold)
    {
        var go  = new GameObject("Entry");
        go.transform.SetParent(parent, false);

        var bg  = go.AddComponent<Image>();
        bg.sprite = SpriteGenerator.CreateWhiteSquare();
        bg.color  = isRecord
            ? new Color(1f, 0.82f, 0.18f, 0.08f)
            : ColEntryBg;
        bg.raycastTarget = false;

        var rt  = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, EntryH);

        var lgo = new GameObject("Text");
        lgo.transform.SetParent(go.transform, false);
        var tmp = lgo.AddComponent<TextMeshProUGUI>();
        tmp.text          = text;
        tmp.fontSize      = 28f;
        tmp.fontStyle     = isBold ? FontStyles.Bold : FontStyles.Normal;
        tmp.color         = col;
        tmp.alignment     = TextAlignmentOptions.MidlineLeft;
        tmp.raycastTarget = false;
        MenuAssets.ApplyFont(tmp);
        var lrt = tmp.rectTransform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(20f, 0f);
        lrt.offsetMax = new Vector2(-20f, 0f);
    }

    // ── Ouverture / fermeture ─────────────────────────────────────────────────

    private void OpenPanel()
    {
        if (panelRT == null) return;
        panelRT.gameObject.SetActive(true);
        panelGroup.blocksRaycasts = true;
        currentSlide = 0;
        UpdateDots();
        StartCoroutine(FadeGroup(panelGroup, 0f, 1f, 0.22f));
        StartCoroutine(SetupSlideLayout());
    }

    private void ClosePanel()
    {
        StartCoroutine(ClosePanelRoutine());
    }

    private IEnumerator ClosePanelRoutine()
    {
        panelGroup.blocksRaycasts = false;
        yield return StartCoroutine(FadeGroup(panelGroup, 1f, 0f, 0.18f));
        panelRT.gameObject.SetActive(false);
    }

    // ── Rafraîchissement total ────────────────────────────────────────────────

    private void RefreshTotal()
    {
        if (totalScoreLabel == null) return;
        int total = ScoreManager.Instance != null ? ScoreManager.Instance.GetTotalScore() : 0;
        totalScoreLabel.text = total.ToString("N0");
    }

    // ── Feedbacks visuels ─────────────────────────────────────────────────────

    /// <summary>Pulse du widget score (scale) quand un nouveau score est enregistré.</summary>
    private IEnumerator PulseWidget()
    {
        if (widgetRT == null) yield break;
        float elapsed = 0f;
        while (elapsed < WidgetPulseDuration)
        {
            elapsed += Time.deltaTime;
            float t  = elapsed / WidgetPulseDuration;
            float s  = t < 0.5f
                ? Mathf.Lerp(1f, WidgetPulseScale, t * 2f)
                : Mathf.Lerp(WidgetPulseScale, 1f, (t - 0.5f) * 2f);
            widgetRT.localScale = Vector3.one * s;
            yield return null;
        }
        widgetRT.localScale = Vector3.one;
    }

    /// <summary>
    /// Flash doré sur le fond du slide d'historique pour signaler un nouveau record.
    /// Répète 3 fois pour attirer l'attention.
    /// </summary>
    private IEnumerator FlashHistorySlide(int idx)
    {
        if (slideRTs == null || idx >= slideRTs.Length) yield break;
        var slideRT = slideRTs[idx];
        if (slideRT == null) yield break;

        // Trouver le SlideBg
        var bgT = slideRT.Find("SlideBg");
        if (bgT == null) yield break;
        var bgImg = bgT.GetComponent<Image>();
        if (bgImg == null) yield break;

        Color baseColor = bgImg.color;
        int   pulses    = 3;

        for (int p = 0; p < pulses; p++)
        {
            float elapsed = 0f;
            while (elapsed < RecordFlashDuration)
            {
                elapsed += Time.deltaTime;
                float t  = elapsed / RecordFlashDuration;
                float e  = t < 0.4f
                    ? Mathf.Lerp(0f, 1f, t / 0.4f)
                    : Mathf.Lerp(1f, 0f, (t - 0.4f) / 0.6f);
                if (bgImg != null) bgImg.color = Color.Lerp(baseColor, ColRecordFlash, e);
                yield return null;
            }
        }
        if (bgImg != null) bgImg.color = baseColor;
    }

    // ── Helpers UI ────────────────────────────────────────────────────────────

    private static TextMeshProUGUI Lbl(string name, RectTransform parent, string text,
        float size, FontStyles style, Color col,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin = default, Vector2 offsetMax = default,
        TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text          = text;
        tmp.fontSize      = size;
        tmp.fontStyle     = style;
        tmp.color         = col;
        tmp.alignment     = align;
        tmp.raycastTarget = false;
        MenuAssets.ApplyFont(tmp);
        var rt = tmp.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        return tmp;
    }

    private static void Sep(string name, RectTransform parent,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = SpriteGenerator.CreateWhiteSquare();
        img.color  = ColSep;
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    private static IEnumerator FadeGroup(CanvasGroup g, float from, float to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t      += Time.deltaTime;
            g.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / dur));
            yield return null;
        }
        g.alpha = to;
    }
}

// ── Drag handler (swipe entre les slides) ─────────────────────────────────────

/// <summary>
/// Détecte le drag horizontal sur le viewport et notifie <see cref="MenuScorePanel"/>.
/// Séparé pour respecter la responsabilité unique.
/// </summary>
public class SliderDragHandler : MonoBehaviour,
    UnityEngine.EventSystems.IBeginDragHandler,
    UnityEngine.EventSystems.IDragHandler,
    UnityEngine.EventSystems.IEndDragHandler
{
    private MenuScorePanel panel;
    private float          startX;
    private float          lastDeltaX;

    public void Init(MenuScorePanel owner) => panel = owner;

    public void OnBeginDrag(UnityEngine.EventSystems.PointerEventData e)
    {
        startX     = e.position.x;
        lastDeltaX = 0f;
    }

    public void OnDrag(UnityEngine.EventSystems.PointerEventData e)
    {
        lastDeltaX = e.position.x - startX;
    }

    public void OnEndDrag(UnityEngine.EventSystems.PointerEventData e)
    {
        panel?.OnSwipeEnd(lastDeltaX);
    }
}
