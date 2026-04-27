using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Remplace l'ancien panneau d'historique de scores.
///
/// Widget haut-gauche : niveau du joueur + barre XP.
/// Un clic ouvre un panneau plein écran avec :
///   – Niveau actuel, XP dans le palier, vague de quêtes active
///   – Bouton Fermer
///
/// L'historique des scores est supprimé conformément aux specs.
/// </summary>
public class MenuScorePanel : MonoBehaviour
{
    // ── Palette ───────────────────────────────────────────────────────────────

    private static readonly Color ColWidgetBg    = new Color(0.04f, 0.04f, 0.08f, 0.85f);
    private static readonly Color ColWidgetLbl   = new Color(1f, 1f, 1f, 1.00f);   // blanc pur
    private static readonly Color ColXPFill      = new Color(0.40f, 0.80f, 1.00f, 0.90f);
    private static readonly Color ColXPTrack     = new Color(1f,    1f,    1f,    0.15f);
    private static readonly Color ColLevelVal    = new Color(1f, 1f, 1f, 1.00f);
    private static readonly Color ColLevelUp     = new Color(0.40f, 0.80f, 1.00f, 1.00f);
    private static readonly Color ColPanelBg     = new Color(0.04f, 0.04f, 0.10f, 0.98f);
    private static readonly Color ColPanelTitle  = new Color(1f, 1f, 1f, 1.00f);   // blanc pur
    private static readonly Color ColCloseBtn    = new Color(1f,    1f,    1f,    0.07f);
    private static readonly Color ColCloseTxt    = new Color(1f, 1f, 1f, 1.00f);   // blanc pur
    private static readonly Color ColWaveLbl     = new Color(1f, 1f, 1f, 1.00f);   // blanc pur
    private static readonly Color ColWaveVal     = new Color(1.00f, 0.82f, 0.18f, 1.00f);
    private static readonly Color ColXPLbl       = new Color(1f, 1f, 1f, 1.00f);   // blanc pur
    private static readonly Color ColXPVal       = new Color(0.40f, 0.80f, 1.00f, 1.00f);
    private static readonly Color ColQuestDone   = new Color(0.30f, 0.95f, 0.45f, 1.00f);
    private static readonly Color ColQuestPend   = new Color(1f, 1f, 1f, 1.00f);   // blanc pur
    private static readonly Color ColQuestComplex= new Color(1.00f, 0.82f, 0.18f, 1.00f);
    private static readonly Color ColSep         = new Color(1f,    1f,    1f,    0.12f);
    private static readonly Color ColMinScoreLbl = new Color(1f, 1f, 1f, 1.00f);   // blanc pur

    // ── Layout ────────────────────────────────────────────────────────────────

    private const float WidgetW   = 380f;   // plus large
    private const float WidgetH   = 160f;   // plus haut
    private const float MarginX   = 32f;
    private const float MarginY   = 48f;
    private const float PulseDur  = 0.35f;
    private const float PulseScl  = 1.12f;
    private const float XPAnimDur = 0.65f;

    // ── Références runtime ────────────────────────────────────────────────────

    private TextMeshProUGUI _levelLabel;
    private TextMeshProUGUI _xpLabel;
    private Image           _xpFill;
    private RectTransform   _widgetRT;

    private RectTransform   _panelRT;
    private CanvasGroup     _panelGroup;
    private TextMeshProUGUI _panelLevelLabel;
    private TextMeshProUGUI _panelXPLabel;
    private Image           _panelXPFill;
    private Transform       _questListParent;
    private TextMeshProUGUI _waveLabel;
    private TextMeshProUGUI _minScoreLabel;

    private float _xpFillFrom;
    private bool  _panelBuilt;

    // ── Initialisation ────────────────────────────────────────────────────────

    /// <summary>Appelé par <see cref="MenuMainHud.Init"/>.</summary>
    public void Init(RectTransform canvasRT)
    {
        PlayerLevelManager.EnsureExists();
        BuildWidget(canvasRT);
        BuildPanel(canvasRT);
        RefreshAll(animate: false);

        if (PlayerLevelManager.Instance != null)
        {
            PlayerLevelManager.Instance.OnLevelUp        += OnLevelUp;
            PlayerLevelManager.Instance.OnProgressChanged += OnXPChanged;
        }

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnProgressChanged += RefreshQuestList;
            QuestManager.Instance.OnWaveStarted     += OnWaveStarted;
        }
    }

    private void OnXPChanged()   => RefreshAll(animate: true);
    private void OnWaveStarted(int _) => RefreshAll(animate: false);

    private void OnDestroy()
    {
        if (PlayerLevelManager.Instance != null)
        {
            PlayerLevelManager.Instance.OnLevelUp        -= OnLevelUp;
            PlayerLevelManager.Instance.OnProgressChanged -= OnXPChanged;
        }
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnProgressChanged -= RefreshQuestList;
            QuestManager.Instance.OnWaveStarted     -= OnWaveStarted;
        }
    }

    // ── Widget haut-gauche ────────────────────────────────────────────────────

    private void BuildWidget(RectTransform parent)
    {
        var go       = new GameObject("LevelWidget");
        go.transform.SetParent(parent, false);

        // Simple Image, pas un bouton
        var img      = go.AddComponent<Image>();
        img.sprite   = SpriteGenerator.CreateWhiteSquare();
        img.color    = ColWidgetBg;
        img.raycastTarget = false;

        var rt           = img.rectTransform;
        rt.anchorMin     = new Vector2(0f, 1f);
        rt.anchorMax     = new Vector2(0f, 1f);
        rt.pivot         = new Vector2(0f, 1f);
        rt.sizeDelta     = new Vector2(WidgetW, WidgetH);
        rt.anchoredPosition = new Vector2(MarginX, -MarginY);
        _widgetRT        = rt;

        // Label "NIVEAU"
        MakeLabel("LvlLbl", rt, "NIVEAU",
            new Vector2(0.06f, 0.62f), new Vector2(0.55f, 1f),
            22f, ColWidgetLbl, FontStyles.Bold, TextAlignmentOptions.BottomLeft);

        // Valeur niveau (grand)
        var lvlGO    = new GameObject("LvlVal");
        lvlGO.transform.SetParent(rt, false);
        var lvlTmp   = lvlGO.AddComponent<TextMeshProUGUI>();
        lvlTmp.text      = "1";
        lvlTmp.fontSize  = 64f;
        lvlTmp.fontStyle = FontStyles.Bold;
        lvlTmp.color     = ColLevelVal;
        lvlTmp.alignment = TextAlignmentOptions.MidlineLeft;
        lvlTmp.raycastTarget = false;
        MenuAssets.ApplyFont(lvlTmp);
        var lvlRT    = lvlTmp.rectTransform;
        lvlRT.anchorMin = new Vector2(0.06f, 0.08f);
        lvlRT.anchorMax = new Vector2(0.52f, 0.66f);
        lvlRT.offsetMin = lvlRT.offsetMax = Vector2.zero;
        _levelLabel  = lvlTmp;

        // Piste XP — plus haute (30 % de hauteur)
        var trackGO  = new GameObject("XPTrack");
        trackGO.transform.SetParent(rt, false);
        var trackImg = trackGO.AddComponent<Image>();
        trackImg.sprite = SpriteGenerator.CreateWhiteSquare();
        trackImg.color  = ColXPTrack;
        trackImg.raycastTarget = false;
        var trackRT  = trackImg.rectTransform;
        trackRT.anchorMin = new Vector2(0.06f, 0.06f);
        trackRT.anchorMax = new Vector2(0.94f, 0.30f);   // barre plus épaisse
        trackRT.offsetMin = trackRT.offsetMax = Vector2.zero;

        // Remplissage XP
        var fillGO   = new GameObject("XPFill");
        fillGO.transform.SetParent(trackGO.transform, false);
        var fillImg  = fillGO.AddComponent<Image>();
        fillImg.sprite = SpriteGenerator.CreateWhiteSquare();
        fillImg.color  = ColXPFill;
        fillImg.type   = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 0f;
        fillImg.raycastTarget = false;
        var fillRT   = fillImg.rectTransform;
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
        _xpFill = fillImg;

        // Label XP (ex: "40 / 100 XP")
        var xpLblGO  = new GameObject("XPLabel");
        xpLblGO.transform.SetParent(rt, false);
        var xpTmp    = xpLblGO.AddComponent<TextMeshProUGUI>();
        xpTmp.text        = "0 / 100 XP";
        xpTmp.fontSize    = 19f;
        xpTmp.color       = ColWidgetLbl;
        xpTmp.alignment   = TextAlignmentOptions.MidlineLeft;
        xpTmp.raycastTarget = false;
        MenuAssets.ApplyFont(xpTmp);
        var xpRT     = xpTmp.rectTransform;
        xpRT.anchorMin = new Vector2(0.06f, 0.30f);
        xpRT.anchorMax = new Vector2(0.94f, 0.62f);
        xpRT.offsetMin = xpRT.offsetMax = Vector2.zero;
        _xpLabel = xpTmp;
        // Pas de Button — widget informatif uniquement
    }

    // ── Panel plein écran ─────────────────────────────────────────────────────

    private void BuildPanel(RectTransform canvasRT)
    {
        var go = new GameObject("ProgressPanel");
        go.transform.SetParent(canvasRT, false);

        var bg        = go.AddComponent<Image>();
        bg.sprite     = SpriteGenerator.CreateWhiteSquare();
        bg.color      = ColPanelBg;
        bg.raycastTarget = true;

        _panelRT          = bg.rectTransform;
        _panelRT.anchorMin = Vector2.zero;
        _panelRT.anchorMax = Vector2.one;
        _panelRT.offsetMin = _panelRT.offsetMax = Vector2.zero;

        _panelGroup                = go.AddComponent<CanvasGroup>();
        _panelGroup.alpha          = 0f;
        _panelGroup.blocksRaycasts = false;

        // Titre
        MakeLabel("PanelTitle", _panelRT, "PROGRESSION",
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            42f, ColPanelTitle, FontStyles.Bold, TextAlignmentOptions.Center,
            new Vector2(0f, -100f), new Vector2(0f, -20f));

        Sep("TitleSep", _panelRT,
            new Vector2(0.04f, 1f), new Vector2(0.96f, 1f),
            new Vector2(0f, -116f), new Vector2(0f, -112f));

        // ── Section Niveau ────────────────────────────────────────────────────

        MakeLabel("NivLbl", _panelRT, "NIVEAU",
            new Vector2(0.06f, 0.84f), new Vector2(0.50f, 0.90f),
            24f, ColXPLbl, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);

        var nivGO  = new GameObject("NivVal");
        nivGO.transform.SetParent(_panelRT, false);
        var nivTmp = nivGO.AddComponent<TextMeshProUGUI>();
        nivTmp.fontSize  = 96f;
        nivTmp.fontStyle = FontStyles.Bold;
        nivTmp.color     = ColLevelVal;
        nivTmp.alignment = TextAlignmentOptions.MidlineLeft;
        nivTmp.raycastTarget = false;
        MenuAssets.ApplyFont(nivTmp);
        var nivRT  = nivTmp.rectTransform;
        nivRT.anchorMin = new Vector2(0.06f, 0.70f);
        nivRT.anchorMax = new Vector2(0.50f, 0.86f);
        nivRT.offsetMin = nivRT.offsetMax = Vector2.zero;
        _panelLevelLabel = nivTmp;

        // Section XP (droite)
        MakeLabel("XPLbl", _panelRT, "EXPÉRIENCE",
            new Vector2(0.52f, 0.84f), new Vector2(0.94f, 0.90f),
            24f, ColXPLbl, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);

        var xpValGO  = new GameObject("XPVal");
        xpValGO.transform.SetParent(_panelRT, false);
        var xpValTmp = xpValGO.AddComponent<TextMeshProUGUI>();
        xpValTmp.fontSize  = 36f;
        xpValTmp.fontStyle = FontStyles.Bold;
        xpValTmp.color     = ColXPVal;
        xpValTmp.alignment = TextAlignmentOptions.MidlineLeft;
        xpValTmp.raycastTarget = false;
        MenuAssets.ApplyFont(xpValTmp);
        var xpValRT  = xpValTmp.rectTransform;
        xpValRT.anchorMin = new Vector2(0.52f, 0.76f);
        xpValRT.anchorMax = new Vector2(0.94f, 0.86f);
        xpValRT.offsetMin = xpValRT.offsetMax = Vector2.zero;
        _panelXPLabel = xpValTmp;

        // Barre XP panneau
        var pTrackGO  = new GameObject("PanelXPTrack");
        pTrackGO.transform.SetParent(_panelRT, false);
        var pTrackImg = pTrackGO.AddComponent<Image>();
        pTrackImg.sprite = SpriteGenerator.CreateWhiteSquare();
        pTrackImg.color  = ColXPTrack;
        pTrackImg.raycastTarget = false;
        var pTrackRT  = pTrackImg.rectTransform;
        pTrackRT.anchorMin = new Vector2(0.06f, 0.696f);
        pTrackRT.anchorMax = new Vector2(0.94f, 0.720f);
        pTrackRT.offsetMin = pTrackRT.offsetMax = Vector2.zero;

        var pFillGO   = new GameObject("PanelXPFill");
        pFillGO.transform.SetParent(pTrackGO.transform, false);
        var pFillImg  = pFillGO.AddComponent<Image>();
        pFillImg.sprite = SpriteGenerator.CreateWhiteSquare();
        pFillImg.color  = ColXPFill;
        pFillImg.type   = Image.Type.Filled;
        pFillImg.fillMethod = Image.FillMethod.Horizontal;
        pFillImg.fillAmount = 0f;
        pFillImg.raycastTarget = false;
        var pFillRT   = pFillImg.rectTransform;
        pFillRT.anchorMin = Vector2.zero;
        pFillRT.anchorMax = Vector2.one;
        pFillRT.offsetMin = pFillRT.offsetMax = Vector2.zero;
        _panelXPFill = pFillImg;

        // ── Section Quêtes ────────────────────────────────────────────────────

        Sep("QuestSep", _panelRT,
            new Vector2(0.04f, 0.695f), new Vector2(0.96f, 0.695f),
            Vector2.zero, Vector2.zero);

        MakeLabel("VagueLbl", _panelRT, "",
            new Vector2(0.06f, 0.65f), new Vector2(0.70f, 0.693f),
            22f, ColWaveLbl, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);

        // On stocke la ref via GetChild après que le label est créé
        _waveLabel = _panelRT.Find("VagueLbl")?.GetComponent<TextMeshProUGUI>();

        // Score minimum
        MakeLabel("MinScoreLbl", _panelRT, "",
            new Vector2(0.06f, 0.618f), new Vector2(0.94f, 0.648f),
            19f, ColMinScoreLbl, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
        _minScoreLabel = _panelRT.Find("MinScoreLbl")?.GetComponent<TextMeshProUGUI>();

        // Scrollview quêtes
        BuildQuestScrollView(_panelRT);

        // Fermer
        BuildCloseButton(_panelRT);

        go.SetActive(false);
        _panelBuilt = true;
    }

    private void BuildQuestScrollView(RectTransform parent)
    {
        var viewGO   = new GameObject("QuestView");
        viewGO.transform.SetParent(parent, false);
        var viewImg  = viewGO.AddComponent<Image>();
        viewImg.color = Color.clear;
        viewImg.raycastTarget = false;
        var viewMask = viewGO.AddComponent<Mask>();
        viewMask.showMaskGraphic = false;
        var viewRT   = viewImg.rectTransform;
        viewRT.anchorMin = new Vector2(0.04f, 0.17f);
        viewRT.anchorMax = new Vector2(0.96f, 0.618f);
        viewRT.offsetMin = viewRT.offsetMax = Vector2.zero;

        var scrollRect                = viewGO.AddComponent<ScrollRect>();
        scrollRect.horizontal         = false;
        scrollRect.vertical           = true;
        scrollRect.scrollSensitivity  = 40f;
        scrollRect.movementType       = ScrollRect.MovementType.Clamped;

        var listGO = new GameObject("QuestList");
        listGO.transform.SetParent(viewGO.transform, false);
        var listRT = listGO.AddComponent<RectTransform>();
        listRT.anchorMin = new Vector2(0f, 1f);
        listRT.anchorMax = new Vector2(1f, 1f);
        listRT.pivot     = new Vector2(0.5f, 1f);
        listRT.offsetMin = listRT.offsetMax = Vector2.zero;

        var vlg = listGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing              = 8f;
        vlg.padding              = new RectOffset(0, 0, 4, 8);
        vlg.childAlignment       = TextAnchor.UpperCenter;
        vlg.childControlWidth    = true;
        vlg.childControlHeight   = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        var csf = listGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content  = listRT;
        scrollRect.viewport = viewRT;

        _questListParent = listGO.transform;

        // Forcer un rebuild immédiat pour que le ContentSizeFitter soit actif
        LayoutRebuilder.ForceRebuildLayoutImmediate(listRT);
    }

    private void BuildCloseButton(RectTransform parent)
    {
        var go   = new GameObject("CloseBtn");
        go.transform.SetParent(parent, false);

        var img    = go.AddComponent<Image>();
        img.sprite = SpriteGenerator.CreateWhiteSquare();
        img.color  = ColCloseBtn;
        MenuAssets.ApplyButtonSprite(img);

        var rt     = img.rectTransform;
        rt.anchorMin      = new Vector2(0.5f, 0f);
        rt.anchorMax      = new Vector2(0.5f, 0f);
        rt.pivot          = new Vector2(0.5f, 0f);
        rt.sizeDelta      = new Vector2(360f, 88f);
        rt.anchoredPosition = new Vector2(0f, 72f);

        MakeLabel("CloseLbl", rt, "← FERMER",
            Vector2.zero, Vector2.one,
            36f, ColCloseTxt, FontStyles.Bold, TextAlignmentOptions.Center);

        var btn           = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(ClosePanel);
    }

    // ── Open / Close panel ────────────────────────────────────────────────────

    private void OpenPanel()
    {
        if (_panelRT == null) return;
        RefreshAll(animate: false);
        _panelRT.gameObject.SetActive(true);
        _panelGroup.blocksRaycasts = true;
        StopCoroutine(nameof(FadePanel));
        StartCoroutine(FadePanel(0f, 1f, 0.22f));
    }

    private void ClosePanel()
    {
        _panelGroup.blocksRaycasts = false;
        StopCoroutine(nameof(FadePanel));
        StartCoroutine(FadePanel(1f, 0f, 0.18f, () => _panelRT.gameObject.SetActive(false)));
    }

    private IEnumerator FadePanel(float from, float to, float dur, System.Action onDone = null)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            _panelGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / dur));
            yield return null;
        }
        _panelGroup.alpha = to;
        onDone?.Invoke();
    }

    // ── Refresh ───────────────────────────────────────────────────────────────

    private void RefreshAll(bool animate)
    {
        var lm = PlayerLevelManager.Instance;
        if (lm == null) return;

        // Widget
        if (_levelLabel != null)
            _levelLabel.text = lm.Level.ToString();

        float targetRatio = lm.XPRatio;
        if (_xpFill != null)
        {
            if (animate)
                StartCoroutine(AnimateFill(_xpFill, _xpFillFrom, targetRatio, XPAnimDur));
            else
                _xpFill.fillAmount = targetRatio;
        }
        _xpFillFrom = targetRatio;

        if (_xpLabel != null)
            _xpLabel.text = $"{lm.CurrentXP} / {lm.XPToNextLevel} XP";

        // Panel (si visible)
        if (_panelRT != null && _panelRT.gameObject.activeSelf)
        {
            if (_panelLevelLabel != null) _panelLevelLabel.text = lm.Level.ToString();
            if (_panelXPLabel    != null) _panelXPLabel.text    = $"{lm.CurrentXP} / {lm.XPToNextLevel} XP";
            if (_panelXPFill     != null) _panelXPFill.fillAmount = lm.XPRatio;
            if (_waveLabel       != null && QuestManager.Instance != null)
                _waveLabel.text = $"Vague {QuestManager.Instance.WaveIndex + 1} — {QuestManager.Instance.CompletedCount()}/{QuestManager.Instance.ActiveWave.Count} quêtes terminées";
            if (_minScoreLabel   != null && QuestManager.Instance != null)
                _minScoreLabel.text = $"Score minimum requis : {QuestManager.Instance.GetMinScore()} pts";

            RefreshQuestList();
        }
    }

    private void RefreshQuestList()
    {
        if (_questListParent == null || QuestManager.Instance == null) return;

        // Vider la liste
        for (int i = _questListParent.childCount - 1; i >= 0; i--)
            Destroy(_questListParent.GetChild(i).gameObject);

        // Reconstruire
        foreach (var def in QuestManager.Instance.ActiveWave)
        {
            var prog    = QuestManager.Instance.GetProgress(def.Id);
            bool done   = prog.Completed;
            int display = Mathf.Min(prog.Count, def.RequiredCount);

            var rowGO  = new GameObject($"QRow_{def.Id}");
            rowGO.transform.SetParent(_questListParent, false);

            var rowImg = rowGO.AddComponent<Image>();
            rowImg.sprite = SpriteGenerator.CreateWhiteSquare();
            rowImg.color  = done
                ? new Color(0.15f, 0.55f, 0.20f, 0.18f)
                : new Color(1f,    1f,    1f,    0.05f);
            rowImg.raycastTarget = false;

            var rowLE = rowGO.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 88f;
            rowLE.flexibleWidth   = 1f;

            var rowRT = rowImg.rectTransform;

            // Accent gauche
            var acGO  = new GameObject("Acc");
            acGO.transform.SetParent(rowGO.transform, false);
            var acImg = acGO.AddComponent<Image>();
            acImg.sprite = SpriteGenerator.CreateWhiteSquare();
            acImg.color  = done ? ColQuestDone : (def.IsComplex ? ColQuestComplex : ColXPFill);
            acImg.raycastTarget = false;
            var acRT   = acImg.rectTransform;
            acRT.anchorMin = Vector2.zero;
            acRT.anchorMax = new Vector2(0.012f, 1f);
            acRT.offsetMin = acRT.offsetMax = Vector2.zero;

            // Titre
            var tGO  = new GameObject("T");
            tGO.transform.SetParent(rowGO.transform, false);
            var tTmp = tGO.AddComponent<TextMeshProUGUI>();
            tTmp.text      = done ? $"✓  {def.Title}" : def.Title;
            tTmp.fontSize  = 24f;
            tTmp.fontStyle = FontStyles.Bold;
            tTmp.color     = done ? ColQuestDone : (def.IsComplex ? ColQuestComplex : ColQuestPend);
            tTmp.alignment = TextAlignmentOptions.MidlineLeft;
            tTmp.raycastTarget = false;
            MenuAssets.ApplyFont(tTmp);
            var tRT  = tTmp.rectTransform;
            tRT.anchorMin = new Vector2(0.04f, 0.52f);
            tRT.anchorMax = new Vector2(0.78f, 1f);
            tRT.offsetMin = tRT.offsetMax = Vector2.zero;

            // Récompense
            var rGO  = new GameObject("R");
            rGO.transform.SetParent(rowGO.transform, false);
            var rTmp = rGO.AddComponent<TextMeshProUGUI>();
            rTmp.text     = done ? "✓" : $"+{def.RewardCoins}";
            rTmp.fontSize = 26f;
            rTmp.fontStyle = FontStyles.Bold;
            rTmp.color    = done ? ColQuestDone : ColWaveVal;
            rTmp.alignment = TextAlignmentOptions.MidlineRight;
            rTmp.raycastTarget = false;
            MenuAssets.ApplyFont(rTmp);
            var rRT   = rTmp.rectTransform;
            rRT.anchorMin = new Vector2(0.78f, 0.50f);
            rRT.anchorMax = new Vector2(0.97f, 1f);
            rRT.offsetMin = rRT.offsetMax = Vector2.zero;

            // XP (si complexe)
            if (def.IsComplex && !done)
            {
                var xpRowGO  = new GameObject("XPR");
                xpRowGO.transform.SetParent(rowGO.transform, false);
                var xpRowTmp = xpRowGO.AddComponent<TextMeshProUGUI>();
                xpRowTmp.text     = $"+{def.RewardXP} XP";
                xpRowTmp.fontSize = 18f;
                xpRowTmp.color    = ColXPVal;
                xpRowTmp.alignment = TextAlignmentOptions.MidlineRight;
                xpRowTmp.raycastTarget = false;
                MenuAssets.ApplyFont(xpRowTmp);
                var xpRowRT   = xpRowTmp.rectTransform;
                xpRowRT.anchorMin = new Vector2(0.78f, 0f);
                xpRowRT.anchorMax = new Vector2(0.97f, 0.52f);
                xpRowRT.offsetMin = xpRowRT.offsetMax = Vector2.zero;
            }

            // Progression
            var pGO  = new GameObject("P");
            pGO.transform.SetParent(rowGO.transform, false);
            var pTmp = pGO.AddComponent<TextMeshProUGUI>();
            pTmp.text     = done ? "Terminée" : $"{display} / {def.RequiredCount}";
            pTmp.fontSize = 19f;
            pTmp.color    = done ? ColQuestDone : ColXPLbl;
            pTmp.alignment = TextAlignmentOptions.MidlineLeft;
            pTmp.raycastTarget = false;
            MenuAssets.ApplyFont(pTmp);
            var pRT   = pTmp.rectTransform;
            pRT.anchorMin = new Vector2(0.04f, 0f);
            pRT.anchorMax = new Vector2(0.60f, 0.50f);
            pRT.offsetMin = pRT.offsetMax = Vector2.zero;
        }
    }

    // ── Level-up feedback ─────────────────────────────────────────────────────

    private void OnLevelUp(int newLevel)
    {
        StopCoroutine(nameof(LevelUpPulse));
        StartCoroutine(LevelUpPulse(newLevel));
    }

    private IEnumerator LevelUpPulse(int newLevel)
    {
        if (_levelLabel != null)
        {
            _levelLabel.color = ColLevelUp;
            _levelLabel.text  = newLevel.ToString();
        }

        // Pulse le widget
        if (_widgetRT != null)
        {
            float t = 0f;
            while (t < PulseDur)
            {
                t += Time.deltaTime;
                float n = Mathf.Clamp01(t / PulseDur);
                float s = n < 0.5f
                    ? Mathf.Lerp(1f, PulseScl, n * 2f)
                    : Mathf.Lerp(PulseScl, 1f, (n - 0.5f) * 2f);
                _widgetRT.localScale = Vector3.one * s;
                yield return null;
            }
            _widgetRT.localScale = Vector3.one;
        }

        // Restaurer la couleur
        yield return new WaitForSeconds(0.35f);
        if (_levelLabel != null)
            _levelLabel.color = ColLevelVal;
    }

    // ── Animation barre XP ───────────────────────────────────────────────────

    private IEnumerator AnimateFill(Image fill, float from, float to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / dur), 4f);
            fill.fillAmount = Mathf.Lerp(from, to, e);
            yield return null;
        }
        fill.fillAmount = to;
    }

    // ── Helpers UI ────────────────────────────────────────────────────────────

    private static TextMeshProUGUI MakeLabel(string name, RectTransform parent, string text,
        Vector2 anchorMin, Vector2 anchorMax,
        float size, Color color, FontStyles style, TextAlignmentOptions align,
        Vector2 offsetMin = default, Vector2 offsetMax = default)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.fontStyle = style;
        tmp.color     = color;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        MenuAssets.ApplyFont(tmp);
        var rt    = tmp.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        return tmp;
    }

    private static void Sep(string name, RectTransform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = SpriteGenerator.CreateWhiteSquare();
        img.color  = ColSep;
        img.raycastTarget = false;
        var rt    = img.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }
}
