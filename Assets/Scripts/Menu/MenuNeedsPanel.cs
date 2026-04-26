using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panneau "Tableau des Quêtes" du menu.
///
/// Contenu :
///   - Trois jauges cliquables : Eau / Nourriture / Sommeil
///     Le coût est affiché directement à droite de chaque jauge.
///     Un clic sur la jauge déclenche l'achat via ShopManager.
///   - Indicateur de Jour + progression du cycle
///   - Wallet de pièces en haut du panneau
///   - Feedback visuel (flash rouge si fonds insuffisants)
///
/// Référence de résolution : 1080 × 1920 (portrait 9:16).
/// </summary>
public class MenuNeedsPanel : MonoBehaviour
{
    // ── Timings ───────────────────────────────────────────────────────────────

    private const float SlideDuration  = 0.40f;
    private const float CanvasRefWidth = 1080f;

    // ── Layout ────────────────────────────────────────────────────────────────

    private const float GaugeH         = 36f;
    private const float GaugeTrackH    = 14f;

    // ── Palette ───────────────────────────────────────────────────────────────

    private static readonly Color ColBg            = new Color(0.06f, 0.05f, 0.08f, 0.98f);
    private static readonly Color ColTitle         = new Color(1f, 1f, 1f, 0.40f);
    private static readonly Color ColSep           = new Color(1f, 1f, 1f, 0.14f);
    private static readonly Color ColLabel         = new Color(1f, 1f, 1f, 0.55f);
    private static readonly Color ColTrackBg       = new Color(1f, 1f, 1f, 0.12f);
    private static readonly Color ColWater         = new Color(0.25f, 0.65f, 1.00f, 1f);
    private static readonly Color ColFood          = new Color(0.30f, 0.85f, 0.45f, 1f);
    private static readonly Color ColSleep         = new Color(0.65f, 0.40f, 1.00f, 1f);
    private static readonly Color ColDayBg         = new Color(1f, 1f, 1f, 0.06f);
    private static readonly Color ColDayText       = new Color(1f, 1f, 1f, 0.80f);
    private static readonly Color ColCycleTrack    = new Color(1f, 1f, 1f, 0.10f);
    private static readonly Color ColCycleFill     = new Color(1.00f, 0.82f, 0.18f, 0.80f);
    private static readonly Color ColShopBg        = new Color(1f, 1f, 1f, 0.06f);
    private static readonly Color ColBtnBg         = new Color(1f, 1f, 1f, 0.10f);
    private static readonly Color ColBtnTxt        = new Color(1f, 1f, 1f, 0.85f);
    private static readonly Color ColCostTxt       = new Color(1.00f, 0.82f, 0.18f, 1f);
    private static readonly Color ColBackBtn       = new Color(1f, 1f, 1f, 0.06f);
    private static readonly Color ColBackTxt       = new Color(1f, 1f, 1f, 0.45f);
    private static readonly Color ColCoinBg        = new Color(0.04f, 0.04f, 0.08f, 0.85f);
    private static readonly Color ColCoin          = new Color(1.00f, 0.82f, 0.18f, 1f);
    private static readonly Color ColFlashFail     = new Color(0.90f, 0.15f, 0.10f, 0.35f);

    // ── Références runtime ────────────────────────────────────────────────────

    private RectTransform   panelRT;
    private CanvasGroup     group;
    private bool            isAnimating;

    // Jauges
    private RectTransform   waterFill;
    private RectTransform   foodFill;
    private RectTransform   sleepFill;
    private TextMeshProUGUI waterPct;
    private TextMeshProUGUI foodPct;
    private TextMeshProUGUI sleepPct;

    // Blocs cliquables (pour le pulse de danger)
    private RectTransform   waterBlock;
    private RectTransform   foodBlock;
    private RectTransform   sleepBlock;

    // Cycle / Jour
    private TextMeshProUGUI dayLabel;
    private RectTransform   cycleFill;

    // Pièces
    private TextMeshProUGUI coinLabel;
    private Image           coinBarBg;
    private RectTransform   coinBarRT;   // pour le pulse scale

    // Labels de coût inline (à côté des jauges)
    private TextMeshProUGUI waterCostLabel;
    private TextMeshProUGUI foodCostLabel;
    private TextMeshProUGUI sleepCostLabel;

    // Feedback panel bg
    private Image panelBgImage;

    // ── Seuil de danger pour le pulse ────────────────────────────────────────

    private const float DangerThreshold  = 25f;   // % en dessous duquel le bloc pulse
    private const float DangerPulseDur   = 0.60f;
    private const float DangerPulseScale = 1.04f;
    private const float CoinPulseScale   = 1.10f;
    private const float CoinPulseDur     = 0.30f;

    private static readonly Color ColDangerPulse = new Color(1f, 0.25f, 0.15f, 0.18f);

    // État des pulses de danger (pour ne pas en lancer plusieurs en parallèle)
    private bool _waterPulsing;
    private bool _foodPulsing;
    private bool _sleepPulsing;

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>Crée le panneau hors écran (à droite) et retourne l'instance.</summary>
    public static MenuNeedsPanel Create(Transform canvasParent,
        Sprite spriteWater = null, Sprite spriteFood = null, Sprite spriteSleep = null)
    {
        var go = new GameObject("NeedsPanel");
        go.transform.SetParent(canvasParent, false);

        var rt       = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var cg            = go.AddComponent<CanvasGroup>();
        cg.alpha          = 1f;
        cg.blocksRaycasts = false;
        cg.interactable   = false;

        var panel        = go.AddComponent<MenuNeedsPanel>();
        panel.panelRT    = rt;
        panel.group      = cg;
        panel.SpriteWater = spriteWater;
        panel.SpriteFood  = spriteFood;
        panel.SpriteSleep = spriteSleep;
        rt.anchoredPosition = new Vector2(CanvasRefWidth, 0f);

        panel.Build(rt);
        return panel;
    }

    // ── Build ─────────────────────────────────────────────────────────────────

    private void Build(RectTransform root)
    {
        // Fond plein écran
        var bgImg = MakeImage("PanelBg", root, ColBg, stretch: true);
        bgImg.raycastTarget = true;
        panelBgImage = bgImg;

        // Titre
        MakeLabel("NeedsTitle", root, "TABLEAU DES QUÊTES",
            anchorMin: new Vector2(0f, 0.89f), anchorMax: new Vector2(1f, 0.95f),
            size: 46f, color: ColTitle, bold: true);

        MakeSep("Sep1", root, 0.888f);

        // Indicateur jour + cycle (affiché en premier, au-dessus du wallet)
        BuildDayWidget(root);

        // Wallet Pièces (placé directement sous le widget jour)
        BuildCoinBar(root);

        // Jauges cliquables avec coûts inline
        BuildNeedsSection(root);

        // Bouton Retour
        MakeBackButton(root);
    }

    // ── Coin Bar ──────────────────────────────────────────────────────────────

    private void BuildCoinBar(RectTransform root)
    {
        var barGO = new GameObject("CoinBar");
        barGO.transform.SetParent(root, false);

        coinBarBg = barGO.AddComponent<Image>();
        coinBarBg.sprite = SpriteGenerator.CreateWhiteSquare();
        coinBarBg.color  = ColCoinBg;
        coinBarBg.raycastTarget = false;

        var rt = coinBarBg.rectTransform;
        // Ancré juste en dessous du DayWidget (qui termine à anchorY ~0.830)
        rt.anchorMin = new Vector2(0.08f, 0.790f);
        rt.anchorMax = new Vector2(0.92f, 0.825f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        coinBarRT = rt;

        // Icône pièce
        var iconGO  = new GameObject("CoinIcon");
        iconGO.transform.SetParent(barGO.transform, false);
        var iconImg = iconGO.AddComponent<Image>();
        iconImg.sprite = SpriteGenerator.CreateWhiteSquare();
        iconImg.color  = ColCoin;
        iconImg.raycastTarget = false;
        var iconRT = iconImg.rectTransform;
        iconRT.anchorMin = new Vector2(0f, 0.5f);
        iconRT.anchorMax = new Vector2(0f, 0.5f);
        iconRT.pivot     = new Vector2(0f, 0.5f);
        iconRT.sizeDelta = new Vector2(34f, 34f);
        iconRT.anchoredPosition = new Vector2(16f, 0f);

        // Label valeur
        var valGO  = new GameObject("CoinValue");
        valGO.transform.SetParent(barGO.transform, false);
        var valTmp = valGO.AddComponent<TextMeshProUGUI>();
        valTmp.text      = "0";
        valTmp.fontSize  = 34f;
        valTmp.fontStyle = FontStyles.Bold;
        valTmp.color     = ColCoin;
        valTmp.alignment = TextAlignmentOptions.MidlineLeft;
        valTmp.raycastTarget = false;
        MenuAssets.ApplyFont(valTmp);
        var valRT = valTmp.rectTransform;
        valRT.anchorMin = new Vector2(0.18f, 0f);
        valRT.anchorMax = Vector2.one;
        valRT.offsetMin = new Vector2(0f, 4f);
        valRT.offsetMax = new Vector2(-8f, 0f);
        coinLabel = valTmp;

        // Label "PIÈCES"
        var lblGO = new GameObject("PiecesLabel");
        lblGO.transform.SetParent(barGO.transform, false);
        var lblTmp = lblGO.AddComponent<TextMeshProUGUI>();
        lblTmp.text      = "PIÈCES";
        lblTmp.fontSize  = 16f;
        lblTmp.fontStyle = FontStyles.Bold;
        lblTmp.color     = ColLabel;
        lblTmp.alignment = TextAlignmentOptions.MidlineRight;
        lblTmp.raycastTarget = false;
        MenuAssets.ApplyFont(lblTmp);
        var lblRT = lblTmp.rectTransform;
        lblRT.anchorMin = new Vector2(0.55f, 0f);
        lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = new Vector2(0f, 4f);
        lblRT.offsetMax = new Vector2(-10f, 0f);
    }

    // ── Jour + Cycle ──────────────────────────────────────────────────────────

    private void BuildDayWidget(RectTransform root)
    {
        var bgGO = new GameObject("DayWidget");
        bgGO.transform.SetParent(root, false);

        var bgImg = bgGO.AddComponent<Image>();
        bgImg.sprite = SpriteGenerator.CreateWhiteSquare();
        bgImg.color  = ColDayBg;
        bgImg.raycastTarget = false;

        var rt = bgImg.rectTransform;
        rt.anchorMin = new Vector2(0.08f, 0.83f);
        rt.anchorMax = new Vector2(0.92f, 0.868f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        // Label Jour
        var lblGO = new GameObject("DayLabel");
        lblGO.transform.SetParent(bgGO.transform, false);
        var lblTmp = lblGO.AddComponent<TextMeshProUGUI>();
        lblTmp.text      = "JOUR 1";
        lblTmp.fontSize  = 28f;
        lblTmp.fontStyle = FontStyles.Bold;
        lblTmp.color     = ColDayText;
        lblTmp.alignment = TextAlignmentOptions.MidlineLeft;
        lblTmp.raycastTarget = false;
        MenuAssets.ApplyFont(lblTmp);
        var lrt = lblTmp.rectTransform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = new Vector2(0.5f, 1f);
        lrt.offsetMin = new Vector2(16f, 0f);
        lrt.offsetMax = Vector2.zero;
        dayLabel = lblTmp;

        // Barre de progression du cycle (à droite du label)
        var trackGO  = new GameObject("CycleTrack");
        trackGO.transform.SetParent(bgGO.transform, false);
        var trackImg = trackGO.AddComponent<Image>();
        trackImg.sprite = SpriteGenerator.CreateWhiteSquare();
        trackImg.color  = ColCycleTrack;
        trackImg.raycastTarget = false;
        var trackRT = trackImg.rectTransform;
        trackRT.anchorMin = new Vector2(0.5f, 0.2f);
        trackRT.anchorMax = new Vector2(0.98f, 0.8f);
        trackRT.offsetMin = trackRT.offsetMax = Vector2.zero;

        var fillGO  = new GameObject("CycleFill");
        fillGO.transform.SetParent(trackGO.transform, false);
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.sprite = SpriteGenerator.CreateWhiteSquare();
        fillImg.color  = ColCycleFill;
        fillImg.raycastTarget = false;
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 0f;
        var fillRT  = fillImg.rectTransform;
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
        cycleFill = fillRT;

        // Stocker ref sur l'Image pour l'animer
        var _fillImg = fillImg;
        StartCoroutine(AnimateCycleFill(_fillImg));
    }

    // ── Sprites de jauges (assignés depuis MenuSceneSetup / MenuNeedsPanel.Create) ──

    public Sprite SpriteWater { get; set; }
    public Sprite SpriteFood  { get; set; }
    public Sprite SpriteSleep { get; set; }

    // ── Section Jauges ────────────────────────────────────────────────────────

    private void BuildNeedsSection(RectTransform root)
    {
        // Les jauges occupent désormais toute la zone centrale (boutique supprimée)
        float topAnchor = 0.72f;
        float spacing   = 0.14f;

        int waterCost = ShopManager.Instance != null ? ShopManager.Instance.WaterCost : 30;
        int foodCost  = ShopManager.Instance != null ? ShopManager.Instance.FoodCost  : 40;
        int sleepCost = ShopManager.Instance != null ? ShopManager.Instance.SleepCost : 50;

        BuildGaugeRow(root, "Eau",        "EAU",        ColWater, topAnchor,
            waterCost, () => ShopManager.Instance?.BuyWater(), SpriteWater,
            out waterFill, out waterPct, out waterCostLabel, out waterBlock);

        BuildGaugeRow(root, "Nourriture", "NOURRITURE", ColFood,  topAnchor - spacing,
            foodCost, () => ShopManager.Instance?.BuyFood(), SpriteFood,
            out foodFill, out foodPct, out foodCostLabel, out foodBlock);

        BuildGaugeRow(root, "Sommeil",    "SOMMEIL",    ColSleep, topAnchor - spacing * 2f,
            sleepCost, () => ShopManager.Instance?.BuySleep(), SpriteSleep,
            out sleepFill, out sleepPct, out sleepCostLabel, out sleepBlock);
    }

    /// <summary>
    /// Construit une ligne de jauge cliquable.
    /// La zone de clic couvre tout le bloc (label + track + coût).</summary>
    private void BuildGaugeRow(RectTransform root,
        string id, string labelText, Color fillColor,
        float anchorYCenter,
        int cost, System.Action onBuy,
        Sprite gaugeSprite,
        out RectTransform fillOut,
        out TextMeshProUGUI pctOut,
        out TextMeshProUGUI costLabelOut,
        out RectTransform blockOut)
    {
        const float blockHalf  = 0.058f;   // demi-hauteur du bloc cliquable
        const float trackHalf  = 0.018f;   // demi-hauteur de la piste
        const float labelTop   = 0.032f;   // espace label au-dessus de la piste

        float blockYMin = anchorYCenter - blockHalf;
        float blockYMax = anchorYCenter + blockHalf;

        // ── Fond cliquable du bloc ────────────────────────────────────────────
        var blockGO  = new GameObject($"GaugeBlock_{id}");
        blockGO.transform.SetParent(root, false);

        var blockImg = blockGO.AddComponent<Image>();
        blockImg.sprite = SpriteGenerator.CreateWhiteSquare();
        blockImg.color  = ColShopBg;

        var blockRT  = blockImg.rectTransform;
        blockRT.anchorMin = new Vector2(0.04f, blockYMin);
        blockRT.anchorMax = new Vector2(0.96f, blockYMax);
        blockRT.offsetMin = new Vector2(0f,  4f);
        blockRT.offsetMax = new Vector2(0f, -4f);
        blockOut = blockRT;

        // Barre de couleur gauche
        var sideGO  = new GameObject("ColorBar");
        sideGO.transform.SetParent(blockGO.transform, false);
        var sideImg = sideGO.AddComponent<Image>();
        sideImg.sprite = SpriteGenerator.CreateWhiteSquare();
        sideImg.color  = fillColor;
        sideImg.raycastTarget = false;
        var sideRT  = sideImg.rectTransform;
        sideRT.anchorMin = Vector2.zero;
        sideRT.anchorMax = new Vector2(0.025f, 1f);
        sideRT.offsetMin = sideRT.offsetMax = Vector2.zero;

        // ── Label nom (haut gauche) ───────────────────────────────────────────
        var lblGO  = new GameObject($"Label_{id}");
        lblGO.transform.SetParent(blockGO.transform, false);
        var lblTmp = lblGO.AddComponent<TextMeshProUGUI>();
        lblTmp.text      = labelText;
        lblTmp.fontSize  = 28f;
        lblTmp.fontStyle = FontStyles.Bold;
        lblTmp.color     = fillColor;
        lblTmp.alignment = TextAlignmentOptions.MidlineLeft;
        lblTmp.raycastTarget = false;
        MenuAssets.ApplyFont(lblTmp);
        var lblRT = lblTmp.rectTransform;
        lblRT.anchorMin = new Vector2(0.05f, 0.55f);
        lblRT.anchorMax = new Vector2(0.55f, 1f);
        lblRT.offsetMin = new Vector2(8f, 0f);
        lblRT.offsetMax = Vector2.zero;

        // ── Pourcentage (haut droite) ─────────────────────────────────────────
        var pctGO  = new GameObject($"Pct_{id}");
        pctGO.transform.SetParent(blockGO.transform, false);
        var pctTmp = pctGO.AddComponent<TextMeshProUGUI>();
        pctTmp.text      = "100%";
        pctTmp.fontSize  = 24f;
        pctTmp.fontStyle = FontStyles.Bold;
        pctTmp.color     = new Color(fillColor.r, fillColor.g, fillColor.b, 0.75f);
        pctTmp.alignment = TextAlignmentOptions.MidlineRight;
        pctTmp.raycastTarget = false;
        MenuAssets.ApplyFont(pctTmp);
        var pctRT = pctTmp.rectTransform;
        pctRT.anchorMin = new Vector2(0.45f, 0.55f);
        pctRT.anchorMax = new Vector2(0.95f, 1f);
        pctRT.offsetMin = Vector2.zero;
        pctRT.offsetMax = new Vector2(-4f, 0f);
        pctOut = pctTmp;

        // ── Track (bas) ───────────────────────────────────────────────────────
        var trackGO  = new GameObject($"Track_{id}");
        trackGO.transform.SetParent(blockGO.transform, false);
        var trackImg = trackGO.AddComponent<Image>();
        trackImg.sprite = SpriteGenerator.CreateWhiteSquare();
        trackImg.color  = ColTrackBg;
        trackImg.raycastTarget = false;
        var trackRT = trackImg.rectTransform;
        trackRT.anchorMin = new Vector2(0.05f, 0.08f);
        trackRT.anchorMax = new Vector2(0.95f, 0.50f);
        trackRT.offsetMin = trackRT.offsetMax = Vector2.zero;

        // Fill
        var fillGO  = new GameObject($"Fill_{id}");
        fillGO.transform.SetParent(trackGO.transform, false);
        var fillImg = fillGO.AddComponent<Image>();
        if (gaugeSprite != null)
        {
            fillImg.sprite       = gaugeSprite;
            fillImg.color        = Color.white;
            fillImg.type         = Image.Type.Filled;
            fillImg.fillMethod   = Image.FillMethod.Horizontal;
            fillImg.fillOrigin   = (int)Image.OriginHorizontal.Left;
            fillImg.preserveAspect = false;
        }
        else
        {
            fillImg.sprite     = SpriteGenerator.CreateWhiteSquare();
            fillImg.color      = fillColor;
            fillImg.type       = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
        }
        fillImg.raycastTarget = false;
        fillImg.fillAmount    = 1f;
        var fillRT  = fillImg.rectTransform;
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
        fillOut = fillRT;

        // ── Label coût (superposé sur la track, côté droit) ───────────────────
        var costGO  = new GameObject($"Cost_{id}");
        costGO.transform.SetParent(blockGO.transform, false);
        var costTmp = costGO.AddComponent<TextMeshProUGUI>();
        costTmp.text      = $"{cost} 🪙";
        costTmp.fontSize  = 22f;
        costTmp.fontStyle = FontStyles.Bold;
        costTmp.color     = ColCostTxt;
        costTmp.alignment = TextAlignmentOptions.MidlineRight;
        costTmp.raycastTarget = false;
        var costRT = costTmp.rectTransform;
        costRT.anchorMin = new Vector2(0.55f, 0.08f);
        costRT.anchorMax = new Vector2(0.95f, 0.50f);
        costRT.offsetMin = Vector2.zero;
        costRT.offsetMax = new Vector2(-4f, 0f);
        costLabelOut = costTmp;

        // ── Bouton couvrant tout le bloc ──────────────────────────────────────
        var btn = blockGO.AddComponent<Button>();
        btn.targetGraphic = blockImg;
        var colors = btn.colors;
        colors.normalColor      = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 1.3f);
        colors.pressedColor     = new Color(0.75f, 0.75f, 0.75f, 1f);
        colors.fadeDuration     = 0.08f;
        btn.colors = colors;
        btn.onClick.AddListener(() => onBuy?.Invoke());
    }

    // ── Bouton Retour ─────────────────────────────────────────────────────────

    private void MakeBackButton(RectTransform root)
    {
        var go  = new GameObject("BackButton");
        go.transform.SetParent(root, false);

        var img    = go.AddComponent<Image>();
        img.sprite = SpriteGenerator.CreateWhiteSquare();
        img.color  = ColBackBtn;
        MenuAssets.ApplyButtonSprite(img);
        var rt     = img.rectTransform;
        rt.anchorMin      = new Vector2(0.5f, 0f);
        rt.anchorMax      = new Vector2(0.5f, 0f);
        rt.pivot          = new Vector2(0.5f, 0f);
        rt.sizeDelta      = new Vector2(340f, 80f);
        rt.anchoredPosition = new Vector2(0f, 72f);

        var lgo  = new GameObject("Label");
        lgo.transform.SetParent(rt, false);
        var ltmp = lgo.AddComponent<TextMeshProUGUI>();
        ltmp.text      = "← RETOUR";
        ltmp.fontSize  = 38f;
        ltmp.fontStyle = FontStyles.Bold;
        ltmp.color     = ColBackTxt;
        ltmp.alignment = TextAlignmentOptions.Center;
        ltmp.raycastTarget = false;
        MenuAssets.ApplyFont(ltmp);
        var lrt  = ltmp.rectTransform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;

        var btn           = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(Hide);
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        if (NeedsManager.Instance != null)
        {
            NeedsManager.Instance.OnNeedsChanged += RefreshGauges;
            NeedsManager.Instance.OnDayAdvanced  += RefreshDay;
        }
        if (ShopManager.Instance != null)
            ShopManager.Instance.OnCoinsChanged  += RefreshCoins;

        if (ShopManager.Instance != null)
            ShopManager.Instance.OnPurchaseResult += OnPurchaseResult;

        RefreshAll();
    }

    private void OnDisable()
    {
        if (NeedsManager.Instance != null)
        {
            NeedsManager.Instance.OnNeedsChanged -= RefreshGauges;
            NeedsManager.Instance.OnDayAdvanced  -= RefreshDay;
        }
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.OnCoinsChanged  -= RefreshCoins;
            ShopManager.Instance.OnPurchaseResult -= OnPurchaseResult;
        }
    }

    private void Update()
    {
        // Mise à jour progressive de la barre de cycle
        if (NeedsManager.Instance != null && cycleFill != null)
        {
            var fillImg = cycleFill.GetComponent<Image>();
            if (fillImg != null)
                fillImg.fillAmount = NeedsManager.Instance.CycleProgress;
        }
    }

    // ── Refresh ───────────────────────────────────────────────────────────────

    private void RefreshAll()
    {
        if (NeedsManager.Instance != null)
            RefreshGauges(NeedsManager.Instance.Water, NeedsManager.Instance.Food, NeedsManager.Instance.Sleep);

        if (NeedsManager.Instance != null)
            RefreshDay(NeedsManager.Instance.Day);

        if (ShopManager.Instance != null)
            RefreshCoins(ShopManager.Instance.Coins);

        RefreshCosts();
    }

    private void RefreshGauges(float water, float food, float sleep)
    {
        SetGauge(waterFill, waterPct, water);
        SetGauge(foodFill,  foodPct,  food);
        SetGauge(sleepFill, sleepPct, sleep);

        // Pulse de danger si une jauge est en dessous du seuil
        TriggerDangerPulse(waterBlock, water, ref _waterPulsing);
        TriggerDangerPulse(foodBlock,  food,  ref _foodPulsing);
        TriggerDangerPulse(sleepBlock, sleep, ref _sleepPulsing);
    }

    /// <summary>Déclenche le pulse rouge sur un bloc si la valeur passe sous le seuil et qu'aucun pulse n'est déjà en cours.</summary>
    private void TriggerDangerPulse(RectTransform block, float value, ref bool isPulsing)
    {
        if (block == null || isPulsing) return;
        if (value > DangerThreshold)   return;

        isPulsing = true;   // marquer immédiatement avant le StartCoroutine
        if (block == waterBlock)
            StartCoroutine(DangerPulse(block, b => _waterPulsing = b));
        else if (block == foodBlock)
            StartCoroutine(DangerPulse(block, b => _foodPulsing  = b));
        else if (block == sleepBlock)
            StartCoroutine(DangerPulse(block, b => _sleepPulsing = b));
    }

    private static void SetGauge(RectTransform fill, TextMeshProUGUI pct, float value)
    {
        if (fill != null)
        {
            var img = fill.GetComponent<Image>();
            if (img != null) img.fillAmount = value / 100f;
        }
        if (pct != null)
            pct.text = $"{Mathf.RoundToInt(value)}%";
    }

    private void RefreshDay(int day)
    {
        if (dayLabel != null)
            dayLabel.text = $"JOUR {day}";
    }

    private void RefreshCoins(int coins)
    {
        if (coinLabel != null)
            coinLabel.text = coins.ToString("N0");

        StopCoroutine(nameof(PulseCoinBar));
        StartCoroutine(PulseCoinBar());
    }

    private void RefreshCosts()
    {
        if (ShopManager.Instance == null) return;
        if (waterCostLabel != null) waterCostLabel.text = $"{ShopManager.Instance.WaterCost} 🪙";
        if (foodCostLabel  != null) foodCostLabel.text  = $"{ShopManager.Instance.FoodCost} 🪙";
        if (sleepCostLabel != null) sleepCostLabel.text = $"{ShopManager.Instance.SleepCost} 🪙";
    }

    private void OnPurchaseResult(bool success, string item, int remaining)
    {
        if (!success)
            StartCoroutine(FlashFail());
    }

    // ── Coroutines feedback ───────────────────────────────────────────────────

    /// <summary>
    /// Pulse rouge continu sur un bloc de jauge tant qu'il est en danger.
    /// S'arrête automatiquement quand la valeur repasse au-dessus du seuil.
    /// </summary>
    private IEnumerator DangerPulse(RectTransform block, System.Action<bool> setPulsing)
    {
        setPulsing(true);
        var blockImg = block.GetComponent<Image>();
        if (blockImg == null) { setPulsing(false); yield break; }

        Color baseColor = blockImg.color;

        // Boucle de pulses tant que la valeur est en danger
        while (true)
        {
            // Vérifier si on est encore en danger (on relit depuis NeedsManager)
            float val = GetBlockValue(block);
            if (val > DangerThreshold || val <= 0f) break;

            // Un aller-retour
            float elapsed = 0f;
            while (elapsed < DangerPulseDur)
            {
                elapsed += Time.deltaTime;
                float t  = elapsed / DangerPulseDur;
                float e  = t < 0.4f
                    ? Mathf.Lerp(0f, 1f, t / 0.4f)
                    : Mathf.Lerp(1f, 0f, (t - 0.4f) / 0.6f);

                // Pulse couleur + légère mise à l'échelle
                if (blockImg != null)
                    blockImg.color = Color.Lerp(baseColor, ColDangerPulse, e);
                float s = Mathf.Lerp(1f, DangerPulseScale, Mathf.Sin(t * Mathf.PI));
                if (block != null)
                    block.localScale = Vector3.one * s;

                yield return null;
            }

            if (blockImg != null) blockImg.color = baseColor;
            if (block    != null) block.localScale = Vector3.one;

            // Petite pause avant le prochain pulse
            yield return new WaitForSeconds(0.8f);
        }

        if (blockImg != null) blockImg.color = baseColor;
        if (block    != null) block.localScale = Vector3.one;
        setPulsing(false);
    }

    /// <summary>Retourne la valeur (0-100) correspondant au bloc de jauge fourni.</summary>
    private float GetBlockValue(RectTransform block)
    {
        if (NeedsManager.Instance == null) return 100f;
        if (block == waterBlock) return NeedsManager.Instance.Water;
        if (block == foodBlock)  return NeedsManager.Instance.Food;
        if (block == sleepBlock) return NeedsManager.Instance.Sleep;
        return 100f;
    }

    private IEnumerator PulseCoinBar()
    {
        if (coinBarBg == null) yield break;

        // Flash couleur
        float duration = 0.35f;
        float elapsed  = 0f;
        Color baseColor  = ColCoinBg;
        Color flashColor = new Color(0.55f, 0.42f, 0.05f, 0.95f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float e = t < 0.3f
                ? Mathf.Lerp(0f, 1f, t / 0.3f)
                : Mathf.Lerp(1f, 0f, (t - 0.3f) / 0.7f);
            coinBarBg.color = Color.Lerp(baseColor, flashColor, e);

            // Scale simultané
            if (coinBarRT != null)
            {
                float s = Mathf.Lerp(1f, CoinPulseScale, Mathf.Sin(t * Mathf.PI));
                coinBarRT.localScale = Vector3.one * s;
            }

            yield return null;
        }

        coinBarBg.color = baseColor;
        if (coinBarRT != null)
            coinBarRT.localScale = Vector3.one;
    }

    private IEnumerator FlashFail()
    {
        if (panelBgImage == null) yield break;

        float duration = 0.40f;
        float elapsed  = 0f;
        Color baseColor = ColBg;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float e = t < 0.25f
                ? Mathf.Lerp(0f, 1f, t / 0.25f)
                : Mathf.Lerp(1f, 0f, (t - 0.25f) / 0.75f);
            panelBgImage.color = Color.Lerp(baseColor, ColFlashFail, e);
            yield return null;
        }
        panelBgImage.color = baseColor;
    }

    private IEnumerator AnimateCycleFill(Image fillImg)
    {
        while (true)
        {
            if (NeedsManager.Instance != null && fillImg != null)
                fillImg.fillAmount = NeedsManager.Instance.CycleProgress;
            yield return null;
        }
    }

    // ── Show / Hide ───────────────────────────────────────────────────────────

    /// <summary>Slide le panneau depuis la droite, EaseOutQuart 0.4s.</summary>
    public void Show()
    {
        if (isAnimating) return;
        isAnimating          = true;
        group.blocksRaycasts = true;
        group.interactable   = true;

        RefreshCosts();
        StopCoroutine(nameof(Slide));
        panelRT.anchoredPosition = new Vector2(CanvasRefWidth, 0f);
        StartCoroutine(Slide(CanvasRefWidth, 0f, () => isAnimating = false));
    }

    /// <summary>Slide le panneau hors écran vers la droite, EaseOutQuart 0.4s.</summary>
    public void Hide()
    {
        if (isAnimating) return;
        isAnimating          = true;
        group.blocksRaycasts = false;
        group.interactable   = false;

        StopCoroutine(nameof(Slide));
        StartCoroutine(Slide(panelRT.anchoredPosition.x, CanvasRefWidth, () => isAnimating = false));
    }

    private IEnumerator Slide(float fromX, float toX, System.Action onComplete)
    {
        float elapsed = 0f;
        while (elapsed < SlideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t  = Mathf.Clamp01(elapsed / SlideDuration);
            float e  = 1f - Mathf.Pow(1f - t, 4f);
            panelRT.anchoredPosition = new Vector2(Mathf.LerpUnclamped(fromX, toX, e), 0f);
            yield return null;
        }
        panelRT.anchoredPosition = new Vector2(toX, 0f);
        onComplete?.Invoke();
    }

    // ── Helpers UI ────────────────────────────────────────────────────────────

    private static Image MakeImage(string name, RectTransform parent, Color color,
        bool stretch = false)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = SpriteGenerator.CreateWhiteSquare();
        img.color  = color;
        img.raycastTarget = false;
        if (stretch)
        {
            img.rectTransform.anchorMin = Vector2.zero;
            img.rectTransform.anchorMax = Vector2.one;
            img.rectTransform.offsetMin = img.rectTransform.offsetMax = Vector2.zero;
        }
        return img;
    }

    private static void MakeLabel(string name, RectTransform parent, string text,
        Vector2 anchorMin, Vector2 anchorMax,
        float size, Color color, bool bold = false)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        MenuAssets.ApplyFont(tmp);
        var rt    = tmp.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static void MakeSep(string name, RectTransform parent, float anchorY)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = SpriteGenerator.CreateWhiteSquare();
        img.color  = new Color(1f, 1f, 1f, 0.14f);
        img.raycastTarget = false;
        var rt    = img.rectTransform;
        rt.anchorMin = new Vector2(0.08f, anchorY);
        rt.anchorMax = new Vector2(0.92f, anchorY);
        rt.sizeDelta = new Vector2(0f, 2f);
    }
}
