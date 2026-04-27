using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Journal des quêtes — slide depuis la droite (EaseOutQuart, 0.4 s).
///
/// Affiche la vague de quêtes active avec progression animée.
/// Pas de bouton Retour ici : le joueur rappuie sur le bouton QUÊTES pour fermer.
/// </summary>
public class MenuQuestPanel : MonoBehaviour
{
    // ── Timings ───────────────────────────────────────────────────────────────

    private const float SlideDuration = 0.40f;
    private const float CanvasRefW    = 1080f;
    private const float FillAnimDur   = 0.65f;

    // ── Palette — tout en blanc ───────────────────────────────────────────────

    private static readonly Color ColBg           = new Color(0.06f, 0.05f, 0.08f, 0.98f);
    private static readonly Color ColWhite        = Color.white;
    private static readonly Color ColWhiteDim     = Color.white;
    private static readonly Color ColSep          = new Color(1f, 1f, 1f, 0.18f);
    private static readonly Color ColRowBg        = new Color(1f, 1f, 1f, 0.06f);
    private static readonly Color ColRowDone      = new Color(0.15f, 0.55f, 0.20f, 0.22f);
    private static readonly Color ColAccentSimple = new Color(1.00f, 0.82f, 0.18f, 1.00f);
    private static readonly Color ColAccentXP     = new Color(0.40f, 0.80f, 1.00f, 1.00f);
    private static readonly Color ColAccentDone   = new Color(0.30f, 0.95f, 0.45f, 1.00f);
    private static readonly Color ColTrack        = new Color(1f, 1f, 1f, 0.12f);
    private static readonly Color ColFillSimple   = new Color(1.00f, 0.82f, 0.18f, 1.00f);
    private static readonly Color ColFillXP       = new Color(0.40f, 0.80f, 1.00f, 1.00f);
    private static readonly Color ColFillDone     = new Color(0.30f, 0.95f, 0.45f, 1.00f);
    private static readonly Color ColDone         = new Color(0.30f, 0.95f, 0.45f, 1.00f);
    private static readonly Color ColGold         = new Color(1.00f, 0.82f, 0.18f, 1.00f);
    private static readonly Color ColBlue         = new Color(0.40f, 0.80f, 1.00f, 1.00f);

    // ── État ──────────────────────────────────────────────────────────────────

    private RectTransform _panelRT;
    private CanvasGroup   _group;
    private bool          _isAnimating;
    private Transform     _listParent;
    private TextMeshProUGUI _waveLabel;
    private TextMeshProUGUI _minScoreLabel;
    private readonly List<(Image fill, float target)> _fills = new();

    // ── Factory ───────────────────────────────────────────────────────────────

    public static MenuQuestPanel Create(Transform canvasParent)
    {
        var go = new GameObject("QuestPanel");
        go.transform.SetParent(canvasParent, false);

        var rt       = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var cg            = go.AddComponent<CanvasGroup>();
        cg.alpha          = 1f;
        cg.blocksRaycasts = false;
        cg.interactable   = false;

        var p      = go.AddComponent<MenuQuestPanel>();
        p._panelRT = rt;
        p._group   = cg;
        rt.anchoredPosition = new Vector2(CanvasRefW, 0f);

        p.Build(rt);
        return p;
    }

    // ── Construction ──────────────────────────────────────────────────────────

    private void Build(RectTransform root)
    {
        // Fond
        Img("Bg", root, ColBg, stretch: true).raycastTarget = true;

        // Titre
        Lbl("Titre", root, "JOURNAL DES QUÊTES",
            new Vector2(0f, 0.91f), new Vector2(1f, 0.97f),
            48f, ColWhite, FontStyles.Bold);

        // Vague
        _waveLabel = Lbl("Vague", root, "",
            new Vector2(0f, 0.873f), new Vector2(1f, 0.912f),
            22f, ColWhiteDim, FontStyles.Normal);

        // Score minimum
        _minScoreLabel = Lbl("MinScore", root, "",
            new Vector2(0f, 0.840f), new Vector2(1f, 0.874f),
            19f, ColWhiteDim, FontStyles.Normal);

        // Séparateur
        var sep = Img("Sep", root, ColSep).rectTransform;
        sep.anchorMin = new Vector2(0.04f, 0.837f);
        sep.anchorMax = new Vector2(0.96f, 0.839f);
        sep.sizeDelta = Vector2.zero;

        // ScrollView de la liste
        BuildScrollView(root,
            new Vector2(0.03f, 0.10f),
            new Vector2(0.97f, 0.835f));
    }

    private void BuildScrollView(RectTransform root, Vector2 ancMin, Vector2 ancMax)
    {
        var viewGO   = new GameObject("ScrollView");
        viewGO.transform.SetParent(root, false);

        var mask     = viewGO.AddComponent<Image>();
        mask.color   = Color.clear;
        mask.raycastTarget = false;
        viewGO.AddComponent<Mask>().showMaskGraphic = false;

        var viewRT   = mask.rectTransform;
        viewRT.anchorMin = ancMin;
        viewRT.anchorMax = ancMax;
        viewRT.offsetMin = viewRT.offsetMax = Vector2.zero;

        var sr              = viewGO.AddComponent<ScrollRect>();
        sr.horizontal       = false;
        sr.vertical         = true;
        sr.scrollSensitivity = 45f;
        sr.movementType     = ScrollRect.MovementType.Clamped;

        var listGO = new GameObject("List");
        listGO.transform.SetParent(viewGO.transform, false);

        var listRT = listGO.AddComponent<RectTransform>();
        listRT.anchorMin = new Vector2(0f, 1f);
        listRT.anchorMax = new Vector2(1f, 1f);
        listRT.pivot     = new Vector2(0.5f, 1f);
        listRT.offsetMin = listRT.offsetMax = Vector2.zero;

        var vlg = listGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 10f;
        vlg.padding = new RectOffset(0, 0, 6, 12);
        vlg.childControlWidth    = true;
        vlg.childControlHeight   = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        var csf = listGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sr.content  = listRT;
        sr.viewport = viewRT;

        _listParent = listGO.transform;
    }

    // ── Reconstruction des lignes ─────────────────────────────────────────────

    private void RebuildList()
    {
        // Vider
        for (int i = _listParent.childCount - 1; i >= 0; i--)
            Destroy(_listParent.GetChild(i).gameObject);
        _fills.Clear();

        if (QuestManager.Instance == null) return;

        // Header
        int done  = QuestManager.Instance.CompletedCount();
        int total = QuestManager.Instance.ActiveWave.Count;
        if (_waveLabel    != null)
            _waveLabel.text    = $"Vague {QuestManager.Instance.WaveIndex + 1}   ·   {done} / {total} complétées";
        if (_minScoreLabel != null)
            _minScoreLabel.text = $"Score minimum requis : {QuestManager.Instance.GetMinScore()} pts";

        // Lignes
        foreach (var def in QuestManager.Instance.ActiveWave)
        {
            var prog  = QuestManager.Instance.GetProgress(def.Id);
            bool isDone = prog.Completed;
            float ratio = isDone ? 1f
                : (def.RequiredCount > 0 ? Mathf.Clamp01((float)prog.Count / def.RequiredCount) : 0f);

            var fill = BuildRow(def, prog, isDone);
            _fills.Add((fill, ratio));
        }
    }

    private Image BuildRow(QuestDefinition def, QuestProgress prog, bool isDone)
    {
        bool isXP = def.IsComplex;

        Color accentCol = isDone ? ColAccentDone : (isXP ? ColAccentXP  : ColAccentSimple);
        Color fillCol   = isDone ? ColFillDone   : (isXP ? ColFillXP    : ColFillSimple);
        Color titleCol  = isDone ? ColDone        : ColWhite;
        Color rewardCol = isDone ? ColDone        : ColGold;
        Color xpCol     = ColBlue;
        Color progCol   = isDone ? ColDone        : ColWhiteDim;

        // ── Fond de ligne ──────────────────────────────────────────────────────
        var rowGO  = new GameObject($"Row_{def.Id}");
        rowGO.transform.SetParent(_listParent, false);

        var rowImg = rowGO.AddComponent<Image>();
        rowImg.sprite = SpriteGenerator.CreateWhiteSquare();
        rowImg.color  = isDone ? ColRowDone : ColRowBg;
        rowImg.raycastTarget = false;

        var le = rowGO.AddComponent<LayoutElement>();
        le.preferredHeight = isXP ? 120f : 102f;
        le.flexibleWidth   = 1f;

        var rowRT = rowImg.rectTransform;

        // Accent gauche
        var ac = new GameObject("Acc");
        ac.transform.SetParent(rowGO.transform, false);
        var acI = ac.AddComponent<Image>();
        acI.sprite = SpriteGenerator.CreateWhiteSquare();
        acI.color  = accentCol;
        acI.raycastTarget = false;
        var acRT = acI.rectTransform;
        acRT.anchorMin = Vector2.zero;
        acRT.anchorMax = new Vector2(0.014f, 1f);
        acRT.offsetMin = acRT.offsetMax = Vector2.zero;

        // Titre
        var t = AddTMP("T", rowGO.transform,
            isDone ? $"✓  {def.Title}" : def.Title,
            24f, FontStyles.Bold, titleCol,
            new Vector2(0.04f, isXP ? 0.56f : 0.50f), new Vector2(0.74f, 1f));

        // Récompense pièces (droite haut)
        AddTMP("R", rowGO.transform,
            isDone ? "✓" : $"+{def.RewardCoins}",
            26f, FontStyles.Bold, isDone ? ColDone : ColGold,
            new Vector2(0.74f, isXP ? 0.55f : 0.50f), new Vector2(0.97f, 1f),
            TextAlignmentOptions.MidlineRight);

        // Récompense XP (droite bas — uniquement quête complexe non terminée)
        if (isXP && !isDone)
        {
            AddTMP("XP", rowGO.transform,
                $"+{def.RewardXP} XP",
                18f, FontStyles.Normal, xpCol,
                new Vector2(0.74f, 0.08f), new Vector2(0.97f, 0.55f),
                TextAlignmentOptions.MidlineRight);
        }

        // Piste de progression
        var trackGO = new GameObject("Track");
        trackGO.transform.SetParent(rowGO.transform, false);
        var trackI  = trackGO.AddComponent<Image>();
        trackI.sprite = SpriteGenerator.CreateWhiteSquare();
        trackI.color  = ColTrack;
        trackI.raycastTarget = false;
        var trackRT = trackI.rectTransform;
        trackRT.anchorMin = new Vector2(0.04f, 0.08f);
        trackRT.anchorMax = new Vector2(0.70f, 0.26f);
        trackRT.offsetMin = new Vector2(4f, 0f);
        trackRT.offsetMax = Vector2.zero;

        var fillGO  = new GameObject("Fill");
        fillGO.transform.SetParent(trackGO.transform, false);
        var fillI   = fillGO.AddComponent<Image>();
        fillI.sprite = SpriteGenerator.CreateWhiteSquare();
        fillI.color  = fillCol;
        fillI.type   = Image.Type.Filled;
        fillI.fillMethod = Image.FillMethod.Horizontal;
        fillI.fillAmount = 0f;   // animé au Show
        fillI.raycastTarget = false;
        var fillRT  = fillI.rectTransform;
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;

        // Label progression (bas gauche)
        int display = Mathf.Min(prog.Count, def.RequiredCount);
        AddTMP("P", rowGO.transform,
            isDone ? "Terminée" : $"{display} / {def.RequiredCount}",
            19f, FontStyles.Normal, progCol,
            new Vector2(0.04f, 0.04f), new Vector2(0.55f, 0.42f),
            TextAlignmentOptions.MidlineLeft);

        return fillI;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnProgressChanged += OnQuestChanged;
            QuestManager.Instance.OnWaveStarted     += OnWaveStarted;
        }
    }

    private void OnDisable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnProgressChanged -= OnQuestChanged;
            QuestManager.Instance.OnWaveStarted     -= OnWaveStarted;
        }
    }

    private void OnQuestChanged()  { /* rafraîchi au prochain Show */ }
    private void OnWaveStarted(int _) { /* rafraîchi au prochain Show */ }

    // ── Show / Hide ──────────────────────────────────────────────────────────

    public void Show()
    {
        if (_isAnimating) return;
        _isAnimating          = true;
        _group.blocksRaycasts = true;
        _group.interactable   = true;

        RebuildList();

        // Forcer le recalcul du layout pour que ContentSizeFitter applique les hauteurs
        if (_listParent != null)
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(
                _listParent.GetComponent<RectTransform>());

        StopAllCoroutines();
        _panelRT.anchoredPosition = new Vector2(CanvasRefW, 0f);
        StartCoroutine(Slide(CanvasRefW, 0f, () =>
        {
            _isAnimating = false;
            StartCoroutine(AnimateFills());
        }));
    }

    public void Hide()
    {
        if (_isAnimating) return;
        _isAnimating          = true;
        _group.blocksRaycasts = false;
        _group.interactable   = false;

        StopAllCoroutines();
        StartCoroutine(Slide(_panelRT.anchoredPosition.x, CanvasRefW, () => _isAnimating = false));
    }

    private IEnumerator Slide(float from, float to, System.Action done)
    {
        float e = 0f;
        while (e < SlideDuration)
        {
            e += Time.unscaledDeltaTime;
            float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(e / SlideDuration), 4f);
            _panelRT.anchoredPosition = new Vector2(Mathf.LerpUnclamped(from, to, t), 0f);
            yield return null;
        }
        _panelRT.anchoredPosition = new Vector2(to, 0f);
        done?.Invoke();
    }

    // ── Animation fills ───────────────────────────────────────────────────────

    private IEnumerator AnimateFills()
    {
        float delay = 0f;
        foreach (var (fill, target) in _fills)
        {
            if (fill != null)
                StartCoroutine(AnimateFill(fill, 0f, target, FillAnimDur, delay));
            delay += 0.09f;
        }
        yield break;
    }

    private static IEnumerator AnimateFill(Image img, float from, float to, float dur, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        float e = 0f;
        Color baseCol = img.color;
        while (e < dur)
        {
            e += Time.deltaTime;
            float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(e / dur), 4f);
            img.fillAmount = Mathf.Lerp(from, to, t);
            yield return null;
        }
        img.fillAmount = to;

        if (Mathf.Approximately(to, 1f))
        {
            // Flash blanc rapide
            float f = 0f;
            Color bright = Color.Lerp(baseCol, Color.white, 0.6f);
            while (f < 0.25f)
            {
                f += Time.deltaTime;
                img.color = Color.Lerp(bright, baseCol, f / 0.25f);
                yield return null;
            }
            img.color = baseCol;
        }
    }

    // ── Helpers UI ────────────────────────────────────────────────────────────

    private static Image Img(string name, RectTransform parent, Color col, bool stretch = false)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = SpriteGenerator.CreateWhiteSquare();
        img.color  = col;
        img.raycastTarget = false;
        if (stretch)
        {
            img.rectTransform.anchorMin = Vector2.zero;
            img.rectTransform.anchorMax = Vector2.one;
            img.rectTransform.offsetMin = img.rectTransform.offsetMax = Vector2.zero;
        }
        return img;
    }

    private static TextMeshProUGUI Lbl(string name, RectTransform parent, string text,
        Vector2 ancMin, Vector2 ancMax, float size, Color col, FontStyles style,
        TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.fontStyle = style;
        tmp.color     = col;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        MenuAssets.ApplyFont(tmp);
        var rt    = tmp.rectTransform;
        rt.anchorMin = ancMin;
        rt.anchorMax = ancMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return tmp;
    }

    private static TextMeshProUGUI AddTMP(string name, Transform parent,
        string text, float size, FontStyles style, Color col,
        Vector2 ancMin, Vector2 ancMax,
        TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.fontStyle = style;
        tmp.color     = col;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        MenuAssets.ApplyFont(tmp);
        var rt    = tmp.rectTransform;
        rt.anchorMin = ancMin;
        rt.anchorMax = ancMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return tmp;
    }
}
