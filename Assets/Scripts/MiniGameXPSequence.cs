using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Séquence de fin de mini-jeu (sauf Parry Game) :
///   1. Affiche l'écran "VICTOIRE" avec score et XP gagnée
///   2. Fait voler des petites boules bleues vers une mini-barre XP animée
///   3. Fait pulser la barre à chaque boule reçue
///   4. Indique la progression de niveau (1 → 4 max)
///   5. Écrit dans <see cref="GameEndData"/> et retourne au Menu
///
/// Usage : <see cref="Show(int, int, GameType, Action)"/> pour lancer la séquence.
/// </summary>
public class MiniGameXPSequence : MonoBehaviour
{
    // ── Timings ───────────────────────────────────────────────────────────────

    private const float FadeInDur      = 0.30f;
    private const float CountUpDur     = 0.80f;
    private const float PreBallWait    = 0.55f;
    private const float BallFlyDur     = 0.65f;
    private const float BallInterval   = 0.10f;
    private const float PostBallWait   = 1.20f;
    private const float FadeOutDur     = 0.35f;
    private const float FillAnimDur    = 0.45f;
    private const float PulseDur       = 0.20f;
    private const float PulseScale     = 1.10f;

    // ── Palette ───────────────────────────────────────────────────────────────

    private static readonly Color ColOverlay  = new Color(0.04f, 0.03f, 0.10f, 0.92f);
    private static readonly Color ColPanel    = new Color(0.07f, 0.06f, 0.14f, 0.98f);
    private static readonly Color ColAccent   = new Color(0.25f, 0.60f, 1.00f, 0.90f);
    private static readonly Color ColTitle    = new Color(0.30f, 0.80f, 1.00f, 1.00f);
    private static readonly Color ColSub      = new Color(1.00f, 1.00f, 1.00f, 0.45f);
    private static readonly Color ColScore    = new Color(1.00f, 0.82f, 0.18f, 1.00f);
    private static readonly Color ColXP       = new Color(0.40f, 0.78f, 1.00f, 1.00f);
    private static readonly Color ColBarBg    = new Color(0.10f, 0.10f, 0.24f, 1.00f);
    private static readonly Color ColBarFill  = new Color(0.25f, 0.60f, 1.00f, 1.00f);
    private static readonly Color ColBarMaxed = new Color(1.00f, 0.88f, 0.10f, 1.00f);
    private static readonly Color ColBall     = new Color(0.40f, 0.75f, 1.00f, 1.00f);
    private static readonly Color ColLvlLbl   = new Color(1.00f, 1.00f, 1.00f, 0.50f);
    private static readonly Color ColLvlVal   = new Color(0.40f, 0.78f, 1.00f, 1.00f);
    private static readonly Color ColLvlMax   = new Color(1.00f, 0.88f, 0.10f, 1.00f);
    private static readonly Color ColHint     = new Color(1.00f, 1.00f, 1.00f, 0.50f);

    private const int MaxLevel = PlayerLevelManager.MaxLevel;

    // ── Références runtime ────────────────────────────────────────────────────

    private RectTransform    _canvasRT;
    private CanvasGroup      _rootGroup;
    private TextMeshProUGUI  _scoreVal;
    private TextMeshProUGUI  _xpVal;
    private TextMeshProUGUI  _lvlVal;
    private TextMeshProUGUI  _xpBarLbl;
    private Image            _barFill;
    private RectTransform    _barRoot;
    private RectTransform    _ballArrival;   // point d'arrivée des boules

    // ── API statique ──────────────────────────────────────────────────────────

    /// <summary>
    /// Lance la séquence in-game.
    /// <paramref name="onComplete"/> est appelé quand la séquence se termine (pour retourner au menu).
    /// </summary>
    public static void Show(int score, int xpEarned, GameType gameType, Action onComplete)
    {
        var canvasGO = new GameObject("XPSequenceCanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler                 = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight  = 1f;

        canvasGO.AddComponent<GraphicRaycaster>();

        var rootGroup                = canvasGO.AddComponent<CanvasGroup>();
        rootGroup.alpha              = 0f;
        rootGroup.blocksRaycasts     = false;

        var seq           = canvasGO.AddComponent<MiniGameXPSequence>();
        seq._canvasRT     = canvas.GetComponent<RectTransform>();
        seq._rootGroup    = rootGroup;
        seq.StartCoroutine(seq.RunSequence(score, xpEarned, gameType, onComplete, canvasGO));
    }

    // ── Séquence principale ───────────────────────────────────────────────────

    private IEnumerator RunSequence(int score, int xp, GameType gameType,
                                    Action onComplete, GameObject selfGO)
    {
        // ── Enregistrement du score (sans créditer l'XP ici — fait au menu) ──
        ScoreManager.EnsureExists();
        ScoreManager.Instance.AddScoreOnly(gameType, score);
        GameEndData.Set(score, xp, gameType);

        BuildUI(score, xp);

        // ── Fade in ───────────────────────────────────────────────────────────
        yield return StartCoroutine(FadeGroup(_rootGroup, 0f, 1f, FadeInDur));
        _rootGroup.blocksRaycasts = true;

        // ── Count-up score ────────────────────────────────────────────────────
        yield return StartCoroutine(CountUp(_scoreVal, 0, score, CountUpDur, isCoin: false));
        yield return new WaitForSeconds(0.25f);

        // ── Count-up XP ───────────────────────────────────────────────────────
        yield return StartCoroutine(CountUp(_xpVal, 0, xp, CountUpDur * 0.75f, isCoin: true));
        yield return new WaitForSeconds(PreBallWait);

        // ── Boules bleues → barre XP ──────────────────────────────────────────
        int ballCount = Mathf.Clamp(xp / 5, 1, 12);
        yield return StartCoroutine(SpawnBalls(ballCount, xp));

        yield return new WaitForSeconds(PostBallWait);

        // ── Fade out → Menu ───────────────────────────────────────────────────
        yield return StartCoroutine(FadeGroup(_rootGroup, 1f, 0f, FadeOutDur));

        Destroy(selfGO);
        onComplete?.Invoke();
    }

    // ── Construction UI ───────────────────────────────────────────────────────

    private void BuildUI(int score, int xp)
    {
        var root = _canvasRT;

        // Fond sombre
        MakeImg("Overlay", root, Vector2.zero, Vector2.one, ColOverlay);

        // Panneau central
        var panel = MakePanel("Panel", root, new Vector2(0.07f, 0.24f), new Vector2(0.93f, 0.78f), ColPanel);

        // Accent bleu gauche
        var accent    = new GameObject("Accent");
        accent.transform.SetParent(panel, false);
        var accImg    = accent.AddComponent<Image>();
        accImg.sprite = SpriteGenerator.CreateWhiteSquare();
        accImg.color  = ColAccent;
        accImg.raycastTarget = false;
        var accRT     = accImg.rectTransform;
        accRT.anchorMin = new Vector2(0f, 0f);
        accRT.anchorMax = new Vector2(0.008f, 1f);
        accRT.offsetMin = accRT.offsetMax = Vector2.zero;

        // Titre VICTOIRE
        MakeTmp("Title", panel, "VICTOIRE !", 68f, ColTitle, FontStyles.Bold,
            new Vector2(0.04f, 0.83f), new Vector2(0.96f, 0.97f));

        MakeTmp("Sub", panel, "Niveau terminé", 26f, ColSub, FontStyles.Normal,
            new Vector2(0.04f, 0.76f), new Vector2(0.96f, 0.84f));

        // Score
        MakeTmp("ScoreLbl", panel, "SCORE", 22f, ColSub, FontStyles.Normal,
            new Vector2(0.04f, 0.63f), new Vector2(0.50f, 0.74f));
        _scoreVal = MakeTmp("ScoreVal", panel, "0", 62f, ColScore, FontStyles.Bold,
            new Vector2(0.04f, 0.46f), new Vector2(0.50f, 0.66f));

        // XP
        MakeTmp("XPLbl", panel, "XP GAGNÉE", 22f, ColSub, FontStyles.Normal,
            new Vector2(0.54f, 0.63f), new Vector2(0.96f, 0.74f));
        _xpVal = MakeTmp("XPVal", panel, "+0 ⭐", 54f, ColXP, FontStyles.Bold,
            new Vector2(0.54f, 0.46f), new Vector2(0.96f, 0.66f));

        // Séparateur vertical
        var vsep    = new GameObject("VSep");
        vsep.transform.SetParent(panel, false);
        var vsepImg = vsep.AddComponent<Image>();
        vsepImg.sprite = SpriteGenerator.CreateWhiteSquare();
        vsepImg.color  = new Color(1f, 1f, 1f, 0.07f);
        vsepImg.raycastTarget = false;
        var vsepRT    = vsepImg.rectTransform;
        vsepRT.anchorMin = new Vector2(0.499f, 0.48f);
        vsepRT.anchorMax = new Vector2(0.501f, 0.72f);
        vsepRT.offsetMin = vsepRT.offsetMax = Vector2.zero;

        // ── Zone barre XP ─────────────────────────────────────────────────────
        BuildXPBarZone(panel);

        // Hint
        MakeTmp("Hint", panel, "Retour au menu…", 22f, ColHint, FontStyles.Normal,
            new Vector2(0.04f, 0.01f), new Vector2(0.96f, 0.10f));
    }

    private void BuildXPBarZone(RectTransform parent)
    {
        var plm = PlayerLevelManager.EnsureExists();
        int level = Mathf.Clamp(plm.Level, 1, MaxLevel);
        bool maxed = level >= MaxLevel;

        // ── Niveau ────────────────────────────────────────────────────────────
        MakeTmp("LvlLbl", parent, "NIVEAU", 20f, ColLvlLbl, FontStyles.Normal,
            new Vector2(0.04f, 0.33f), new Vector2(0.18f, 0.43f));

        _lvlVal = MakeTmp("LvlVal", parent, level.ToString(), 40f,
            maxed ? ColLvlMax : ColLvlVal, FontStyles.Bold,
            new Vector2(0.04f, 0.21f), new Vector2(0.18f, 0.35f));

        // ── Barre ─────────────────────────────────────────────────────────────
        var barZone = new GameObject("BarZone");
        barZone.transform.SetParent(parent, false);
        _barRoot         = barZone.AddComponent<RectTransform>();
        _barRoot.anchorMin = new Vector2(0.20f, 0.24f);
        _barRoot.anchorMax = new Vector2(0.96f, 0.40f);
        _barRoot.offsetMin = _barRoot.offsetMax = Vector2.zero;

        // Fond barre
        var bgGO    = new GameObject("BarBg");
        bgGO.transform.SetParent(_barRoot, false);
        var bgImg   = bgGO.AddComponent<Image>();
        bgImg.sprite = SpriteGenerator.CreateWhiteSquare();
        bgImg.color  = ColBarBg;
        bgImg.raycastTarget = false;
        var bgRT    = bgImg.rectTransform;
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

        // Fill
        var fillGO  = new GameObject("BarFill");
        fillGO.transform.SetParent(_barRoot, false);
        _barFill    = fillGO.AddComponent<Image>();
        _barFill.sprite  = SpriteGenerator.CreateWhiteSquare();
        _barFill.color   = maxed ? ColBarMaxed : ColBarFill;
        _barFill.type    = Image.Type.Filled;
        _barFill.fillMethod = Image.FillMethod.Horizontal;
        _barFill.fillAmount = maxed ? 1f : plm.XPRatio;
        _barFill.raycastTarget = false;
        var fillRT  = _barFill.rectTransform;
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;

        // Texte XP bar simplifié : compteur XX / 100
        _xpBarLbl = MakeTmp("XPBarLbl", _barRoot,
            maxed ? "MAX" : $"{plm.CurrentXP} / {PlayerLevelManager.XPPerLevel}",
            18f, ColLvlLbl, FontStyles.Normal,
            Vector2.zero, Vector2.one);
        _xpBarLbl.alignment = TMPro.TextAlignmentOptions.Center;

        // Point d'arrivée des boules (centre de la barre)
        var targetGO    = new GameObject("BallArrival");
        targetGO.transform.SetParent(_canvasRT, false);
        _ballArrival    = targetGO.AddComponent<RectTransform>();
        _ballArrival.anchorMin = new Vector2(0.5f, 0.5f);
        _ballArrival.anchorMax = new Vector2(0.5f, 0.5f);
        _ballArrival.pivot     = new Vector2(0.5f, 0.5f);
        _ballArrival.sizeDelta = Vector2.zero;
        // On laisse Update positionner _ballArrival d'après _barRoot en world-space
    }

    // ── Boules ────────────────────────────────────────────────────────────────

    private IEnumerator SpawnBalls(int count, int totalXp)
    {
        // Pré-calcul de la position d'arrivée en coordonnées canvas
        // (centre de la barre, convertit world→canvas)
        Vector3 barWorldCenter = _barRoot.TransformPoint(_barRoot.rect.center);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRT,
            RectTransformUtility.WorldToScreenPoint(null, barWorldCenter),
            null,
            out Vector2 arrivalAP);

        if (_ballArrival != null) _ballArrival.anchoredPosition = arrivalAP;

        // Calcul XP à ajouter pour la progression visuelle
        var plm   = PlayerLevelManager.EnsureExists();
        float xpPerBall = (float)totalXp / count;
        float accXP = 0f;

        for (int i = 0; i < count; i++)
        {
            StartCoroutine(FlyBall(arrivalAP, i));
            yield return new WaitForSeconds(BallInterval);

            accXP += xpPerBall;
        }

        // Attendre la fin du dernier vol puis animer le fill final
        yield return new WaitForSeconds(BallFlyDur);

        // Refresh visuel du niveau après que l'XP aura été réellement créditée au menu
        // (ici on montre juste la progression simulée)
        RefreshBarVisual(plm, totalXp);
    }

    private IEnumerator FlyBall(Vector2 arrivalAP, int index)
    {
        var go  = new GameObject($"XPBall_{index}");
        go.transform.SetParent(_canvasRT, false);

        var img = go.AddComponent<Image>();
        img.sprite = SpriteGenerator.CreateCircle(32);
        img.color  = ColBall;
        img.raycastTarget = false;

        var rt       = img.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(26f, 26f);

        // Départ : dispersé en bas de l'écran
        Vector2 startAP = new Vector2(
            UnityEngine.Random.Range(-380f, 380f),
            UnityEngine.Random.Range(-680f, -260f));

        // Bézier
        Vector2 ctrl = Vector2.Lerp(startAP, arrivalAP, 0.45f)
                     + new Vector2(UnityEngine.Random.Range(-180f, 180f), UnityEngine.Random.Range(120f, 320f));

        rt.anchoredPosition = startAP;

        float t = 0f;
        while (t < BallFlyDur)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / BallFlyDur);
            float e = 1f - Mathf.Pow(1f - n, 3f);

            rt.anchoredPosition =
                  Mathf.Pow(1f - e, 2f) * startAP
                + 2f * (1f - e) * e     * ctrl
                + e * e                 * arrivalAP;

            float alpha = n > 0.78f ? Mathf.InverseLerp(1f, 0.78f, n) : 1f;
            img.color   = new Color(ColBall.r, ColBall.g, ColBall.b, alpha);

            float s = Mathf.Sin(n * Mathf.PI) * 0.5f + 0.75f;
            rt.localScale = Vector3.one * s;

            yield return null;
        }

        Destroy(go);

        // Pulse de la barre à chaque boule reçue
        StartCoroutine(PulseBar());
    }

    private void RefreshBarVisual(PlayerLevelManager plm, int xpAdded)
    {
        // Simulation de la progression XP future (sera réellement créditée au menu)
        int   level     = Mathf.Clamp(plm.Level, 1, PlayerLevelManager.MaxLevel);
        bool  maxed     = level >= PlayerLevelManager.MaxLevel;
        int   simXP     = plm.CurrentXP + xpAdded;
        float simRatio  = maxed ? 1f : Mathf.Clamp01((float)simXP / PlayerLevelManager.XPPerLevel);

        if (simRatio >= 1f && !maxed)
        {
            level = Mathf.Min(level + 1, PlayerLevelManager.MaxLevel);
            maxed = level >= PlayerLevelManager.MaxLevel;
            simRatio = maxed ? 1f : 0f;
        }

        _lvlVal.text  = level.ToString();
        _lvlVal.color = maxed ? ColLvlMax : ColLvlVal;

        if (_xpBarLbl != null)
        {
            int simXPDisplay = maxed ? PlayerLevelManager.XPPerLevel : Mathf.Clamp(simXP, 0, PlayerLevelManager.XPPerLevel - 1);
            _xpBarLbl.text = maxed ? "MAX" : $"{simXPDisplay} / {PlayerLevelManager.XPPerLevel}";
        }

        StartCoroutine(AnimateFillTo(simRatio, maxed));
    }

    private IEnumerator AnimateFillTo(float targetFill, bool maxed)
    {
        if (_barFill == null) yield break;

        float start = _barFill.fillAmount;
        float t     = 0f;

        while (t < FillAnimDur)
        {
            t += Time.deltaTime;
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / FillAnimDur), 3f);
            _barFill.fillAmount = Mathf.Lerp(start, targetFill, e);
            yield return null;
        }

        _barFill.fillAmount = targetFill;
        _barFill.color      = maxed ? ColBarMaxed : ColBarFill;

        if (_xpBarLbl != null && !maxed)
            _xpBarLbl.text = "MAX - PORTE DÉVERROUILLÉE !";
    }

    private IEnumerator PulseBar()
    {
        if (_barRoot == null) yield break;
        Vector3 baseScale = _barRoot.localScale;

        float t = 0f;
        while (t < PulseDur)
        {
            t += Time.deltaTime;
            float e = Mathf.Sin(Mathf.Clamp01(t / PulseDur) * Mathf.PI);
            _barRoot.localScale = Vector3.Lerp(baseScale, baseScale * PulseScale, e);
            yield return null;
        }

        _barRoot.localScale = baseScale;
    }

    // ── Helpers UI ────────────────────────────────────────────────────────────

    private static RectTransform MakePanel(string name, RectTransform parent,
        Vector2 aMin, Vector2 aMax, Color col)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = SpriteGenerator.CreateWhiteSquare();
        img.color  = col;
        img.raycastTarget = false;
        var rt   = img.rectTransform;
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    private static void MakeImg(string name, RectTransform parent,
        Vector2 aMin, Vector2 aMax, Color col)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = SpriteGenerator.CreateWhiteSquare();
        img.color  = col;
        img.raycastTarget = false;
        var rt   = img.rectTransform;
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static TextMeshProUGUI MakeTmp(string name, RectTransform parent,
        string text, float fontSize, Color col, FontStyles style,
        Vector2 aMin, Vector2 aMax)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text             = text;
        tmp.fontSize         = fontSize;
        tmp.color            = col;
        tmp.fontStyle        = style;
        tmp.alignment        = TextAlignmentOptions.Center;
        tmp.enableAutoSizing = false;
        tmp.raycastTarget    = false;
        MenuAssets.ApplyFont(tmp);
        var rt = tmp.rectTransform;
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return tmp;
    }

    // ── Coroutines utilitaires ────────────────────────────────────────────────

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

    private static IEnumerator CountUp(TextMeshProUGUI lbl, int from, int to, float dur, bool isCoin)
    {
        if (lbl == null) yield break;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / dur), 4f);
            int   v = Mathf.RoundToInt(Mathf.Lerp(from, to, e));
            lbl.text = isCoin ? $"+{v} ⭐" : v.ToString("N0");
            yield return null;
        }
        lbl.text = isCoin ? $"+{to} ⭐" : to.ToString("N0");
    }
}
