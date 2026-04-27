using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panneau "Quêtes" du menu.
///
/// Affiche la vague de quêtes active avec leur progression et leur récompense.
/// La liste est reconstruite à chaque ouverture pour refléter la vague courante.
/// Slide depuis la droite (EaseOutQuart, 0.4 s).
/// Les barres de progression s'animent à l'ouverture (EaseOutQuart, 0.65 s).
///
/// Référence de résolution : 1080 × 1920 (portrait 9:16).
/// </summary>
public class MenuNeedsPanel : MonoBehaviour
{
    // ── Timings ───────────────────────────────────────────────────────────────

    private const float SlideDuration  = 0.40f;
    private const float CanvasRefWidth = 1080f;
    private const float FillAnimDur    = 0.65f;

    // ── Palette ───────────────────────────────────────────────────────────────

    private static readonly Color ColBg          = new Color(0.06f, 0.05f, 0.08f, 0.98f);
    private static readonly Color ColTitle        = new Color(1f, 1f, 1f, 1.00f);   // blanc pur
    private static readonly Color ColSep          = new Color(1f, 1f, 1f, 0.20f);
    private static readonly Color ColBackBtn      = new Color(1f, 1f, 1f, 0.08f);
    private static readonly Color ColBackTxt      = new Color(1f, 1f, 1f, 1.00f);   // blanc pur
    private static readonly Color ColRowBg        = new Color(1f, 1f, 1f, 0.06f);
    private static readonly Color ColRowDone      = new Color(0.15f, 0.55f, 0.20f, 0.22f);
    private static readonly Color ColAccentSimple = new Color(1.00f, 0.82f, 0.18f, 1.00f);
    private static readonly Color ColAccentXP     = new Color(0.40f, 0.80f, 1.00f, 1.00f);
    private static readonly Color ColAccentDone   = new Color(0.30f, 0.95f, 0.45f, 1.00f);
    private static readonly Color ColTrackBg      = new Color(1f,    1f,    1f,    0.12f);
    private static readonly Color ColFillSimple   = new Color(1.00f, 0.82f, 0.18f, 0.90f);
    private static readonly Color ColFillXP       = new Color(0.40f, 0.80f, 1.00f, 0.90f);
    private static readonly Color ColFillDone     = new Color(0.30f, 0.95f, 0.45f, 0.90f);
    private static readonly Color ColText         = new Color(1f, 1f, 1f, 1.00f);   // blanc pur
    private static readonly Color ColTextXP       = new Color(0.40f, 0.80f, 1.00f, 1.00f);
    private static readonly Color ColTextSub      = new Color(1f, 1f, 1f, 1.00f);   // blanc pur (était 0.45)
    private static readonly Color ColReward       = new Color(1.00f, 0.82f, 0.18f, 1.00f);
    private static readonly Color ColXPReward     = new Color(0.40f, 0.80f, 1.00f, 1.00f);
    private static readonly Color ColDoneLabel    = new Color(0.30f, 0.95f, 0.45f, 1.00f);
    private static readonly Color ColWaveInfo     = new Color(1f, 1f, 1f, 1.00f);   // blanc pur
    private static readonly Color ColMinScore     = new Color(1f, 1f, 1f, 0.75f);   // quasi-blanc

    // ── État ──────────────────────────────────────────────────────────────────

    private RectTransform panelRT;
    private CanvasGroup   group;
    private bool          isAnimating;

    // Zone scrollable des lignes de quêtes
    private Transform     questListParent;
    private TextMeshProUGUI waveInfoLabel;
    private TextMeshProUGUI minScoreLabel;

    // Références pour animer les fills
    private readonly List<(Image fill, float targetRatio, bool isDone)> _fillRefs
        = new List<(Image, float, bool)>();

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

        var panel         = go.AddComponent<MenuNeedsPanel>();
        panel.panelRT     = rt;
        panel.group       = cg;
        rt.anchoredPosition = new Vector2(CanvasRefWidth, 0f);

        panel.Build(rt);
        return panel;
    }

    // ── Construction ──────────────────────────────────────────────────────────

    private void Build(RectTransform root)
    {
        // Fond plein écran
        MakeImage("PanelBg", root, ColBg, stretch: true).raycastTarget = true;

        // Titre
        MakeLabel("QuestTitle", root, "QUÊTES",
            new Vector2(0f, 0.91f), new Vector2(1f, 0.97f),
            52f, ColTitle, FontStyles.Bold);

        // Info vague
        var waveGO  = new GameObject("WaveInfo");
        waveGO.transform.SetParent(root, false);
        waveInfoLabel = waveGO.AddComponent<TextMeshProUGUI>();
        waveInfoLabel.fontSize  = 22f;
        waveInfoLabel.color     = ColWaveInfo;
        waveInfoLabel.alignment = TextAlignmentOptions.Center;
        waveInfoLabel.raycastTarget = false;
        MenuAssets.ApplyFont(waveInfoLabel);
        var waveRT  = waveInfoLabel.rectTransform;
        waveRT.anchorMin = new Vector2(0f, 0.878f);
        waveRT.anchorMax = new Vector2(1f, 0.912f);
        waveRT.offsetMin = waveRT.offsetMax = Vector2.zero;

        // Score minimum
        var msGO  = new GameObject("MinScoreInfo");
        msGO.transform.SetParent(root, false);
        minScoreLabel = msGO.AddComponent<TextMeshProUGUI>();
        minScoreLabel.fontSize  = 19f;
        minScoreLabel.color     = ColMinScore;
        minScoreLabel.alignment = TextAlignmentOptions.Center;
        minScoreLabel.raycastTarget = false;
        MenuAssets.ApplyFont(minScoreLabel);
        var msRT  = minScoreLabel.rectTransform;
        msRT.anchorMin = new Vector2(0f, 0.848f);
        msRT.anchorMax = new Vector2(1f, 0.878f);
        msRT.offsetMin = msRT.offsetMax = Vector2.zero;

        // Séparateur
        var sepRT = MakeImage("Sep", root, ColSep).rectTransform;
        sepRT.anchorMin = new Vector2(0.06f, 0.845f);
        sepRT.anchorMax = new Vector2(0.94f, 0.847f);
        sepRT.sizeDelta = Vector2.zero;

        // Scrollview des quêtes
        BuildScrollView(root);

        // Bouton Retour
        MakeBackButton(root);
    }

    private void BuildScrollView(RectTransform root)
    {
        var viewGO   = new GameObject("QuestView");
        viewGO.transform.SetParent(root, false);
        var viewImg  = viewGO.AddComponent<Image>();
        viewImg.color = Color.clear;
        viewImg.raycastTarget = false;
        var viewMask = viewGO.AddComponent<Mask>();
        viewMask.showMaskGraphic = false;
        var viewRT   = viewImg.rectTransform;
        viewRT.anchorMin = new Vector2(0.02f, 0.17f);
        viewRT.anchorMax = new Vector2(0.98f, 0.845f);
        viewRT.offsetMin = viewRT.offsetMax = Vector2.zero;

        var scrollRect              = viewGO.AddComponent<ScrollRect>();
        scrollRect.horizontal       = false;
        scrollRect.vertical         = true;
        scrollRect.scrollSensitivity = 40f;
        scrollRect.movementType     = ScrollRect.MovementType.Clamped;

        var listGO = new GameObject("QuestList");
        listGO.transform.SetParent(viewGO.transform, false);
        var listRT = listGO.AddComponent<RectTransform>();
        listRT.anchorMin = new Vector2(0f, 1f);
        listRT.anchorMax = new Vector2(1f, 1f);
        listRT.pivot     = new Vector2(0.5f, 1f);
        listRT.offsetMin = listRT.offsetMax = Vector2.zero;

        var vlg = listGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing              = 10f;
        vlg.padding              = new RectOffset(0, 0, 6, 10);
        vlg.childAlignment       = TextAnchor.UpperCenter;
        vlg.childControlWidth    = true;
        vlg.childControlHeight   = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        var csf = listGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content  = listRT;
        scrollRect.viewport = viewRT;

        questListParent = listGO.transform;
    }

    // ── Construction des lignes de quêtes ─────────────────────────────────────

    private void RebuildQuestRows()
    {
        if (questListParent == null || QuestManager.Instance == null) return;

        // Nettoyer l'ancienne liste
        for (int i = questListParent.childCount - 1; i >= 0; i--)
            Destroy(questListParent.GetChild(i).gameObject);
        _fillRefs.Clear();

        // Mettre à jour le header
        if (waveInfoLabel != null)
        {
            int done  = QuestManager.Instance.CompletedCount();
            int total = QuestManager.Instance.ActiveWave.Count;
            waveInfoLabel.text = $"Vague {QuestManager.Instance.WaveIndex + 1}  —  {done}/{total} complétées";
        }
        if (minScoreLabel != null)
            minScoreLabel.text = $"Score minimum requis : {QuestManager.Instance.GetMinScore()} pts";

        // Construire une ligne par quête active
        foreach (var def in QuestManager.Instance.ActiveWave)
        {
            var prog   = QuestManager.Instance.GetProgress(def.Id);
            bool done  = prog.Completed;
            float ratio = done ? 1f : (def.RequiredCount > 0
                ? Mathf.Clamp01((float)prog.Count / def.RequiredCount) : 0f);

            var fill = BuildQuestRow(questListParent, def, prog, done, ratio);
            _fillRefs.Add((fill, ratio, done));
        }
    }

    private Image BuildQuestRow(Transform parent, QuestDefinition def,
        QuestProgress prog, bool done, float ratio)
    {
        bool isXP = def.IsComplex;
        Color accentCol = done ? ColAccentDone : (isXP ? ColAccentXP : ColAccentSimple);
        Color fillCol   = done ? ColFillDone   : (isXP ? ColFillXP   : ColFillSimple);

        var rowGO  = new GameObject($"Row_{def.Id}");
        rowGO.transform.SetParent(parent, false);

        var rowImg = rowGO.AddComponent<Image>();
        rowImg.sprite = SpriteGenerator.CreateWhiteSquare();
        rowImg.color  = done ? ColRowDone : ColRowBg;
        rowImg.raycastTarget = false;

        var le = rowGO.AddComponent<LayoutElement>();
        le.preferredHeight = isXP ? 118f : 100f;
        le.flexibleWidth   = 1f;

        var rowRT = rowImg.rectTransform;

        // Accent gauche
        var acGO  = new GameObject("Acc");
        acGO.transform.SetParent(rowGO.transform, false);
        var acImg = acGO.AddComponent<Image>();
        acImg.sprite = SpriteGenerator.CreateWhiteSquare();
        acImg.color  = accentCol;
        acImg.raycastTarget = false;
        var acRT = acImg.rectTransform;
        acRT.anchorMin = Vector2.zero;
        acRT.anchorMax = new Vector2(0.014f, 1f);
        acRT.offsetMin = acRT.offsetMax = Vector2.zero;

        // Titre
        var tGO  = new GameObject("T");
        tGO.transform.SetParent(rowGO.transform, false);
        var tTmp = tGO.AddComponent<TextMeshProUGUI>();
        tTmp.text      = done ? $"✓  {def.Title}" : def.Title;
        tTmp.fontSize  = 24f;
        tTmp.fontStyle = FontStyles.Bold;
        tTmp.color     = done ? ColDoneLabel : (isXP ? ColTextXP : ColText);
        tTmp.alignment = TextAlignmentOptions.MidlineLeft;
        tTmp.raycastTarget = false;
        MenuAssets.ApplyFont(tTmp);
        var tRT = tTmp.rectTransform;
        tRT.anchorMin = new Vector2(0.04f, isXP ? 0.58f : 0.52f);
        tRT.anchorMax = new Vector2(0.76f, 1f);
        tRT.offsetMin = tRT.offsetMax = Vector2.zero;

        // Récompense pièces
        var rGO  = new GameObject("R");
        rGO.transform.SetParent(rowGO.transform, false);
        var rTmp = rGO.AddComponent<TextMeshProUGUI>();
        rTmp.text      = done ? "✓" : $"+{def.RewardCoins}";
        rTmp.fontSize  = 26f;
        rTmp.fontStyle = FontStyles.Bold;
        rTmp.color     = done ? ColDoneLabel : ColReward;
        rTmp.alignment = TextAlignmentOptions.MidlineRight;
        rTmp.raycastTarget = false;
        MenuAssets.ApplyFont(rTmp);
        var rRT = rTmp.rectTransform;
        rRT.anchorMin = new Vector2(0.76f, isXP ? 0.55f : 0.50f);
        rRT.anchorMax = new Vector2(0.97f, 1f);
        rRT.offsetMin = rRT.offsetMax = Vector2.zero;

        // Récompense XP (complexe seulement)
        if (isXP && !done)
        {
            var xpGO  = new GameObject("XP");
            xpGO.transform.SetParent(rowGO.transform, false);
            var xpTmp = xpGO.AddComponent<TextMeshProUGUI>();
            xpTmp.text      = $"+{def.RewardXP} XP";
            xpTmp.fontSize  = 19f;
            xpTmp.color     = ColXPReward;
            xpTmp.alignment = TextAlignmentOptions.MidlineRight;
            xpTmp.raycastTarget = false;
            MenuAssets.ApplyFont(xpTmp);
            var xpRT = xpTmp.rectTransform;
            xpRT.anchorMin = new Vector2(0.76f, 0.10f);
            xpRT.anchorMax = new Vector2(0.97f, 0.58f);
            xpRT.offsetMin = xpRT.offsetMax = Vector2.zero;
        }

        // Piste de progression
        var trackGO  = new GameObject("Track");
        trackGO.transform.SetParent(rowGO.transform, false);
        var trackImg = trackGO.AddComponent<Image>();
        trackImg.sprite = SpriteGenerator.CreateWhiteSquare();
        trackImg.color  = ColTrackBg;
        trackImg.raycastTarget = false;
        var trackRT  = trackImg.rectTransform;
        trackRT.anchorMin = new Vector2(0.04f, 0.08f);
        trackRT.anchorMax = new Vector2(0.72f, 0.28f);
        trackRT.offsetMin = new Vector2(4f, 0f);
        trackRT.offsetMax = Vector2.zero;

        // Remplissage (commence à 0 pour animation)
        var fillGO   = new GameObject("Fill");
        fillGO.transform.SetParent(trackGO.transform, false);
        var fillImg  = fillGO.AddComponent<Image>();
        fillImg.sprite = SpriteGenerator.CreateWhiteSquare();
        fillImg.color  = fillCol;
        fillImg.type   = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 0f;   // animation depuis 0
        fillImg.raycastTarget = false;
        var fillRT   = fillImg.rectTransform;
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;

        // Label de progression
        var pGO  = new GameObject("P");
        pGO.transform.SetParent(rowGO.transform, false);
        var pTmp = pGO.AddComponent<TextMeshProUGUI>();
        int display = Mathf.Min(prog.Count, def.RequiredCount);
        pTmp.text     = done ? "Terminée" : $"{display} / {def.RequiredCount}";
        pTmp.fontSize = 19f;
        pTmp.color    = done ? ColDoneLabel : ColTextSub;
        pTmp.alignment = TextAlignmentOptions.MidlineLeft;
        pTmp.raycastTarget = false;
        MenuAssets.ApplyFont(pTmp);
        var pRT = pTmp.rectTransform;
        pRT.anchorMin = new Vector2(0.04f, 0.04f);
        pRT.anchorMax = new Vector2(0.55f, 0.45f);
        pRT.offsetMin = new Vector2(4f, 0f);
        pRT.offsetMax = Vector2.zero;

        return fillImg;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnProgressChanged += OnProgressChanged;
            QuestManager.Instance.OnWaveStarted     += OnWaveStarted;
        }
    }

    private void OnDisable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnProgressChanged -= OnProgressChanged;
            QuestManager.Instance.OnWaveStarted     -= OnWaveStarted;
        }
    }

    private void OnProgressChanged()  { /* Les fills se mettront à jour au prochain Show */ }
    private void OnWaveStarted(int _)  { /* Reconstruction au prochain Show */ }

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
        var lrt  = ltmp.rectTransform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;

        var btn           = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(Hide);
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
        float size, Color color, FontStyles style)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.fontStyle = style;
        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        MenuAssets.ApplyFont(tmp);
        var rt    = tmp.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // ── Show / Hide ───────────────────────────────────────────────────────────

    /// <summary>Slide le panneau depuis la droite, reconstruit les lignes, anime les fills.</summary>
    public void Show()
    {
        if (isAnimating) return;
        isAnimating          = true;
        group.blocksRaycasts = true;
        group.interactable   = true;

        // Reconstruire la liste (depuis 0)
        RebuildQuestRows();

        StopAllCoroutines();
        panelRT.anchoredPosition = new Vector2(CanvasRefWidth, 0f);
        StartCoroutine(Slide(CanvasRefWidth, 0f, () =>
        {
            isAnimating = false;
            StartCoroutine(AnimateAllFills());
        }));
    }

    /// <summary>Slide le panneau hors écran vers la droite.</summary>
    public void Hide()
    {
        if (isAnimating) return;
        isAnimating          = true;
        group.blocksRaycasts = false;
        group.interactable   = false;

        StopAllCoroutines();
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

    // ── Animation des fills ───────────────────────────────────────────────────

    private IEnumerator AnimateAllFills()
    {
        // Décalage en cascade entre les lignes
        float delay = 0f;
        foreach (var (fill, target, done) in _fillRefs)
        {
            if (fill == null) continue;
            Color col = done ? ColFillDone
                : (fill.color == ColFillXP ? ColFillXP : ColFillSimple);
            StartCoroutine(AnimateFill(fill, 0f, target, col, FillAnimDur, delay));
            delay += 0.08f;
        }
        yield break;
    }

    private static IEnumerator AnimateFill(Image fill, float from, float to,
        Color targetColor, float dur, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        fill.color = targetColor;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.Clamp01(elapsed / dur);
            float e  = 1f - Mathf.Pow(1f - t, 4f);
            fill.fillAmount = Mathf.Lerp(from, to, e);
            yield return null;
        }
        fill.fillAmount = to;

        // Flash si 100%
        if (Mathf.Approximately(to, 1f))
            yield return FlashFill(fill, targetColor);
    }

    private static IEnumerator FlashFill(Image fill, Color baseColor)
    {
        Color bright = Color.Lerp(baseColor, Color.white, 0.55f);
        float t = 0f;
        while (t < 0.25f)
        {
            t += Time.deltaTime;
            fill.color = Color.Lerp(bright, baseColor, t / 0.25f);
            yield return null;
        }
        fill.color = baseColor;
    }
}
