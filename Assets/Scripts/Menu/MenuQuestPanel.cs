using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Journal des quêtes — slide depuis la droite (EaseOutQuart, 0.4 s).
///
/// Affiche la vague active avec progression animée.
/// Le journal se rafraîchit en temps réel via <see cref="QuestManager.OnProgressChanged"/>.
/// Pas de bouton Retour : le joueur rappuie sur QUÊTES pour fermer.
/// </summary>
public class MenuQuestPanel : MonoBehaviour
{
    // ── Timings ───────────────────────────────────────────────────────────────

    private const float SlideDuration = 0.40f;
    private const float CanvasRefW    = 1080f;
    private const float FillAnimDur   = 0.65f;

    // ── Palette ───────────────────────────────────────────────────────────────

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
    private static readonly Color ColLevelTag     = new Color(0.40f, 0.80f, 1.00f, 0.22f);

    // ── État ──────────────────────────────────────────────────────────────────

    private RectTransform   _panelRT;
    private CanvasGroup     _group;
    private bool            _isAnimating;
    private bool            _isVisible;
    private Transform       _listParent;
    private TextMeshProUGUI _waveLabel;
    private TextMeshProUGUI _minScoreLabel;

    private readonly List<(Image fill, float target)> _fills = new List<(Image, float)>();

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>Crée le panel dans le canvas donné et le retourne.</summary>
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
        // Viewport (conteneur masqué)
        var viewGO = new GameObject("Viewport");
        viewGO.transform.SetParent(root, false);

        var maskImg  = viewGO.AddComponent<Image>();
        maskImg.color = Color.clear;
        maskImg.raycastTarget = false;
        viewGO.AddComponent<Mask>().showMaskGraphic = false;

        var viewRT       = maskImg.rectTransform;
        viewRT.anchorMin = ancMin;
        viewRT.anchorMax = ancMax;
        viewRT.offsetMin = viewRT.offsetMax = Vector2.zero;

        // ScrollRect sur le viewport
        var sr               = viewGO.AddComponent<ScrollRect>();
        sr.horizontal        = false;
        sr.vertical          = true;
        sr.scrollSensitivity = 45f;
        sr.movementType      = ScrollRect.MovementType.Clamped;
        sr.viewport          = viewRT;

        // Conteneur du contenu (ancré en haut, grandit vers le bas)
        var listGO = new GameObject("List");
        listGO.transform.SetParent(viewGO.transform, false);

        var listRT       = listGO.AddComponent<RectTransform>();
        listRT.anchorMin = new Vector2(0f, 1f);
        listRT.anchorMax = new Vector2(1f, 1f);
        listRT.pivot     = new Vector2(0.5f, 1f);
        listRT.offsetMin = listRT.offsetMax = Vector2.zero;

        var vlg = listGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing              = 10f;
        vlg.padding              = new RectOffset(0, 0, 6, 12);
        vlg.childControlWidth    = true;
        vlg.childControlHeight   = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        var csf             = listGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit     = ContentSizeFitter.FitMode.PreferredSize;

        sr.content = listRT;

        _listParent = listGO.transform;
    }

    // ── Reconstruction de la liste ────────────────────────────────────────────

    /// <summary>
    /// Vide et reconstruit toutes les lignes de quêtes.
    /// Appelle <see cref="ForceLayoutRebuild"/> via coroutine pour que le
    /// <see cref="ContentSizeFitter"/> calcule les hauteurs correctement.
    /// </summary>
    private void RebuildList()
    {
        // Vider
        for (int i = _listParent.childCount - 1; i >= 0; i--)
            Destroy(_listParent.GetChild(i).gameObject);
        _fills.Clear();

        if (QuestManager.Instance == null)
        {
            if (_waveLabel    != null) _waveLabel.text    = "Système de quêtes indisponible";
            if (_minScoreLabel != null) _minScoreLabel.text = "";
            return;
        }

        // Mise à jour des labels d'en-tête
        int done  = QuestManager.Instance.CompletedCount();
        int total = QuestManager.Instance.ActiveWave.Count;
        if (_waveLabel    != null)
            _waveLabel.text    = $"Vague {QuestManager.Instance.WaveIndex + 1}   ·   {done} / {total} complétées";
        if (_minScoreLabel != null)
            _minScoreLabel.text = $"Score minimum requis : {QuestManager.Instance.GetMinScore()} pts";

        // Construire une ligne par quête
        foreach (var def in QuestManager.Instance.ActiveWave)
        {
            var prog  = QuestManager.Instance.GetProgress(def.Id);
            bool done1  = prog.Completed;
            float ratio = prog.GetRatio(def);

            var fill = BuildRow(def, prog, done1);
            _fills.Add((fill, ratio));
        }

        // Forcer le layout immédiatement (frame actuelle)
        if (_listParent != null)
        {
            var rt = _listParent.GetComponent<RectTransform>();
            if (rt != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }
    }

    private Image BuildRow(QuestDefinition def, QuestProgress prog, bool isDone)
    {
        bool isXP = def.IsComplex;

        Color accentCol = isDone ? ColAccentDone : (isXP ? ColAccentXP  : ColAccentSimple);
        Color fillCol   = isDone ? ColFillDone   : (isXP ? ColFillXP    : ColFillSimple);
        Color titleCol  = isDone ? ColDone        : ColWhite;
        Color progCol   = isDone ? ColDone        : ColWhiteDim;

        float rowH = isXP ? 130f : 110f;

        // ── Fond de ligne ──────────────────────────────────────────────────────
        var rowGO = new GameObject($"Row_{def.Id}");
        rowGO.transform.SetParent(_listParent, false);

        var rowImg = rowGO.AddComponent<Image>();
        rowImg.sprite = SpriteGenerator.CreateWhiteSquare();
        rowImg.color  = isDone ? ColRowDone : ColRowBg;
        rowImg.raycastTarget = false;

        var le = rowGO.AddComponent<LayoutElement>();
        le.preferredHeight = rowH;
        le.flexibleWidth   = 1f;

        var rowRT = rowImg.rectTransform;

        // Accent gauche (bande colorée verticale)
        var ac  = new GameObject("Acc");
        ac.transform.SetParent(rowGO.transform, false);
        var acI = ac.AddComponent<Image>();
        acI.sprite = SpriteGenerator.CreateWhiteSquare();
        acI.color  = accentCol;
        acI.raycastTarget = false;
        var acRT  = acI.rectTransform;
        acRT.anchorMin = Vector2.zero;
        acRT.anchorMax = new Vector2(0.014f, 1f);
        acRT.offsetMin = acRT.offsetMax = Vector2.zero;

        // Titre de la quête
        AddTMP("T", rowGO.transform,
            isDone ? $"✓  {def.Title}" : def.Title,
            24f, FontStyles.Bold, titleCol,
            new Vector2(0.04f, isXP ? 0.60f : 0.52f), new Vector2(0.74f, 1f));

        // Récompense pièces (haut-droite)
        string coinsText = isDone ? "✓" : $"+{def.RewardCoins}";
        AddTMP("R", rowGO.transform,
            coinsText,
            26f, FontStyles.Bold, isDone ? ColDone : ColGold,
            new Vector2(0.74f, isXP ? 0.58f : 0.50f), new Vector2(0.97f, 1f),
            TextAlignmentOptions.MidlineRight);

        // Badge "+N XP · NIVEAU" (quêtes complexes non terminées uniquement)
        if (isXP && !isDone)
        {
            // Fond du badge
            var badgeGO = new GameObject("XPBadge");
            badgeGO.transform.SetParent(rowGO.transform, false);
            var badgeImg = badgeGO.AddComponent<Image>();
            badgeImg.sprite = SpriteGenerator.CreateWhiteSquare();
            badgeImg.color  = ColLevelTag;
            badgeImg.raycastTarget = false;
            var badgeRT = badgeImg.rectTransform;
            badgeRT.anchorMin = new Vector2(0.04f, 0.56f);
            badgeRT.anchorMax = new Vector2(0.70f, 0.78f);
            badgeRT.offsetMin = badgeRT.offsetMax = Vector2.zero;

            AddTMP("XP", badgeGO.transform,
                $"+{def.RewardXP} XP  ·  NIVEAU +1",
                17f, FontStyles.Bold, ColBlue,
                Vector2.zero, Vector2.one,
                TextAlignmentOptions.Center);
        }

        // Piste de progression (fond gris)
        var trackGO = new GameObject("Track");
        trackGO.transform.SetParent(rowGO.transform, false);
        var trackI  = trackGO.AddComponent<Image>();
        trackI.sprite = SpriteGenerator.CreateWhiteSquare();
        trackI.color  = ColTrack;
        trackI.raycastTarget = false;
        var trackRT = trackI.rectTransform;
        trackRT.anchorMin = new Vector2(0.04f, 0.08f);
        trackRT.anchorMax = new Vector2(0.70f, 0.28f);
        trackRT.offsetMin = new Vector2(4f, 0f);
        trackRT.offsetMax = Vector2.zero;

        // Jauge de remplissage (Image.Filled Horizontal)
        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(trackGO.transform, false);
        var fillI  = fillGO.AddComponent<Image>();
        fillI.sprite     = SpriteGenerator.CreateWhiteSquare();
        fillI.color      = fillCol;
        fillI.type       = Image.Type.Filled;
        fillI.fillMethod = Image.FillMethod.Horizontal;
        fillI.fillAmount = 0f;   // animé dans AnimateFills()
        fillI.raycastTarget = false;
        var fillRT = fillI.rectTransform;
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;

        // Label progression (bas-gauche) : "X / N sessions"
        int display = Mathf.Min(prog.Count, def.RequiredCount);
        AddTMP("P", rowGO.transform,
            isDone ? "Terminée ✓" : $"{display} / {def.RequiredCount} sessions",
            18f, FontStyles.Normal, progCol,
            new Vector2(0.04f, 0.04f), new Vector2(0.60f, 0.40f),
            TextAlignmentOptions.MidlineLeft);

        return fillI;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnProgressChanged += OnQuestProgressChanged;
            QuestManager.Instance.OnWaveStarted     += OnWaveStarted;
        }
    }

    private void OnDisable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnProgressChanged -= OnQuestProgressChanged;
            QuestManager.Instance.OnWaveStarted     -= OnWaveStarted;
        }
    }

    /// <summary>
    /// Appelé quand la progression des quêtes change (score ajouté, complétion).
    /// Si le panel est visible, rafraîchit immédiatement la liste.
    /// </summary>
    private void OnQuestProgressChanged()
    {
        if (_isVisible)
            StartCoroutine(RebuildNextFrame());
    }

    private void OnWaveStarted(int _)
    {
        if (_isVisible)
            StartCoroutine(RebuildNextFrame());
    }

    /// <summary>
    /// Attend une frame avant de rebuilder pour laisser Unity mettre à jour
    /// les données du QuestManager (exécuté dans le même frame que l'event).
    /// </summary>
    private IEnumerator RebuildNextFrame()
    {
        yield return null;
        RebuildList();
        if (_fills.Count > 0)
            StartCoroutine(AnimateFills());
    }

    // ── Show / Hide ───────────────────────────────────────────────────────────

    public void Show()
    {
        if (_isAnimating) return;
        _isAnimating          = true;
        _isVisible            = true;
        _group.blocksRaycasts = true;
        _group.interactable   = true;

        RebuildList();

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
        _isVisible            = false;
        _group.blocksRaycasts = false;
        _group.interactable   = false;

        StopAllCoroutines();
        StartCoroutine(Slide(_panelRT.anchoredPosition.x, CanvasRefW, () =>
        {
            _isAnimating = false;
        }));
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

    // ── Animation des jauges ──────────────────────────────────────────────────

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
        if (img == null) yield break;

        float e        = 0f;
        Color baseCol  = img.color;

        while (e < dur)
        {
            e += Time.deltaTime;
            float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(e / dur), 4f);
            img.fillAmount = Mathf.Lerp(from, to, t);
            yield return null;
        }
        img.fillAmount = to;

        // Flash blanc si la jauge est pleine
        if (Mathf.Approximately(to, 1f))
        {
            float f      = 0f;
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
