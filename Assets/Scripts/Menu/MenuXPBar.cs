using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Barre d'XP persistante affichée dans le menu principal.
///
/// Layout (portrait 1080×1920) :
///   - Fond sombre horizontal centré juste sous le HUD haut
///   - Indicateur de niveau (1 → 4 max) à gauche
///   - Barre de progression bleue à remplissage animé
///   - Petites boules bleues qui "rentrent" dans la barre lors d'un gain d'XP
///
/// Quand le niveau 4 est atteint, notifie <see cref="MenuDoor"/> pour le pulse jaune.
/// </summary>
public class MenuXPBar : MonoBehaviour
{
    // ── Layout ────────────────────────────────────────────────────────────────

    private const float BarW           = 940f;
    private const float BarH           = 52f;
    private const float BallSize       = 28f;
    private const float PosY           = -170f;   // anchoredPosition Y depuis le haut du canvas

    // ── Timings ───────────────────────────────────────────────────────────────

    private const float FillAnimDur    = 0.55f;   // durée du fill de la barre
    private const float PulseDur       = 0.22f;   // durée du pulse de la barre
    private const float PulseScale     = 1.08f;
    private const float BallFlyDur     = 0.70f;

    // ── Palette ───────────────────────────────────────────────────────────────

    private static readonly Color ColBg       = new Color(0.05f, 0.05f, 0.12f, 0.90f);
    private static readonly Color ColBarBg    = new Color(0.10f, 0.10f, 0.22f, 1.00f);
    private static readonly Color ColBarFill  = new Color(0.25f, 0.60f, 1.00f, 1.00f);  // bleu XP
    private static readonly Color ColBall     = new Color(0.40f, 0.75f, 1.00f, 1.00f);
    private static readonly Color ColLvlLbl   = new Color(1.00f, 1.00f, 1.00f, 0.55f);
    private static readonly Color ColLvlVal   = new Color(0.40f, 0.78f, 1.00f, 1.00f);
    private static readonly Color ColXPLbl    = new Color(1.00f, 1.00f, 1.00f, 0.38f);
    private static readonly Color ColMaxed    = new Color(1.00f, 0.88f, 0.10f, 1.00f);  // jaune max

    private const int MaxLevel = 4;

    // ── État runtime ──────────────────────────────────────────────────────────

    private RectTransform _canvasRT;
    private RectTransform _fillRT;
    private Image         _fillImg;
    private RectTransform _barRoot;
    private TextMeshProUGUI _lvlVal;
    private TextMeshProUGUI _xpLbl;
    private Coroutine     _fillCoroutine;

    // Cible du vol des boules (centre de la barre)
    private RectTransform _ballTarget;

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>Crée et attache la barre XP dans le <paramref name="canvasRT"/> fourni.</summary>
    public static MenuXPBar Create(RectTransform canvasRT)
    {
        var go  = new GameObject("MenuXPBar");
        go.transform.SetParent(canvasRT, false);

        var rt           = go.AddComponent<RectTransform>();
        rt.anchorMin     = new Vector2(0.5f, 1f);
        rt.anchorMax     = new Vector2(0.5f, 1f);
        rt.pivot         = new Vector2(0.5f, 1f);
        rt.sizeDelta     = new Vector2(BarW, 110f);
        rt.anchoredPosition = new Vector2(0f, PosY);

        var comp         = go.AddComponent<MenuXPBar>();
        comp._canvasRT   = canvasRT;
        return comp;
    }

    // ── Init ──────────────────────────────────────────────────────────────────

    private void Start()
    {
        BuildUI();
        RefreshInstant();

        // Abonnement aux changements d'XP/niveau
        if (PlayerLevelManager.Instance != null)
        {
            PlayerLevelManager.Instance.OnProgressChanged += OnProgressChanged;
            PlayerLevelManager.Instance.OnLevelUp         += OnLevelUp;
        }
    }

    private void OnDestroy()
    {
        if (PlayerLevelManager.Instance != null)
        {
            PlayerLevelManager.Instance.OnProgressChanged -= OnProgressChanged;
            PlayerLevelManager.Instance.OnLevelUp         -= OnLevelUp;
        }
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>
    /// Lance l'animation des boules bleues entrant dans la barre + remplit l'XP.
    /// Appelé par <see cref="MenuXPReceiver"/> après un gain d'XP.
    /// </summary>
    public void AnimateXPGain(int xpAmount)
    {
        int count = Mathf.Clamp(xpAmount / 5, 1, 12);
        StartCoroutine(SpawnBallSequence(count));
    }

    // ── Construction UI ───────────────────────────────────────────────────────

    private void BuildUI()
    {
        var root = GetComponent<RectTransform>();

        // ── Fond ──────────────────────────────────────────────────────────────
        var bgGO = new GameObject("XPBarBg");
        bgGO.transform.SetParent(root, false);
        var bgImg          = bgGO.AddComponent<Image>();
        bgImg.sprite       = SpriteGenerator.CreateWhiteSquare();
        bgImg.color        = ColBg;
        bgImg.raycastTarget = false;
        var bgRT           = bgImg.rectTransform;
        bgRT.anchorMin     = Vector2.zero;
        bgRT.anchorMax     = Vector2.one;
        bgRT.offsetMin     = bgRT.offsetMax = Vector2.zero;

        // ── Indicateur de niveau (gauche) ─────────────────────────────────────
        var lvlGO = new GameObject("LvlBlock");
        lvlGO.transform.SetParent(root, false);
        var lvlRT      = lvlGO.AddComponent<RectTransform>();
        lvlRT.anchorMin = new Vector2(0f, 0f);
        lvlRT.anchorMax = new Vector2(0.12f, 1f);
        lvlRT.offsetMin = lvlRT.offsetMax = Vector2.zero;

        var lblGO = new GameObject("LvlLbl");
        lblGO.transform.SetParent(lvlRT, false);
        var lbl           = lblGO.AddComponent<TextMeshProUGUI>();
        lbl.text          = "NV.";
        lbl.fontSize      = 20f;
        lbl.color         = ColLvlLbl;
        lbl.alignment     = TextAlignmentOptions.Bottom;
        lbl.raycastTarget = false;
        MenuAssets.ApplyFont(lbl);
        var lblRT = lbl.rectTransform;
        lblRT.anchorMin = new Vector2(0f, 0.50f);
        lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;

        var valGO = new GameObject("LvlVal");
        valGO.transform.SetParent(lvlRT, false);
        _lvlVal           = valGO.AddComponent<TextMeshProUGUI>();
        _lvlVal.text      = "1";
        _lvlVal.fontSize  = 40f;
        _lvlVal.fontStyle = FontStyles.Bold;
        _lvlVal.color     = ColLvlVal;
        _lvlVal.alignment = TextAlignmentOptions.Top;
        _lvlVal.raycastTarget = false;
        MenuAssets.ApplyFont(_lvlVal);
        var valRT = _lvlVal.rectTransform;
        valRT.anchorMin = Vector2.zero;
        valRT.anchorMax = new Vector2(1f, 0.58f);
        valRT.offsetMin = valRT.offsetMax = Vector2.zero;

        // ── Zone barre (droite du niveau) ──────────────────────────────────────
        var barZone = new GameObject("BarZone");
        barZone.transform.SetParent(root, false);
        _barRoot          = barZone.AddComponent<RectTransform>();
        _barRoot.anchorMin = new Vector2(0.14f, 0.18f);
        _barRoot.anchorMax = new Vector2(1.00f, 0.72f);
        _barRoot.offsetMin = _barRoot.offsetMax = Vector2.zero;

        // Fond barre
        var barBgGO       = new GameObject("BarBg");
        barBgGO.transform.SetParent(_barRoot, false);
        var barBgImg      = barBgGO.AddComponent<Image>();
        barBgImg.sprite   = SpriteGenerator.CreateCircle(32);
        barBgImg.color    = ColBarBg;
        barBgImg.raycastTarget = false;
        var barBgRT       = barBgImg.rectTransform;
        barBgRT.anchorMin = Vector2.zero;
        barBgRT.anchorMax = Vector2.one;
        barBgRT.offsetMin = barBgRT.offsetMax = Vector2.zero;

        // Fill barre
        var fillGO        = new GameObject("BarFill");
        fillGO.transform.SetParent(_barRoot, false);
        _fillImg          = fillGO.AddComponent<Image>();
        _fillImg.sprite   = SpriteGenerator.CreateWhiteSquare();
        _fillImg.color    = ColBarFill;
        _fillImg.type     = Image.Type.Filled;
        _fillImg.fillMethod = Image.FillMethod.Horizontal;
        _fillImg.raycastTarget = false;
        _fillRT           = _fillImg.rectTransform;
        _fillRT.anchorMin = Vector2.zero;
        _fillRT.anchorMax = Vector2.one;
        _fillRT.offsetMin = _fillRT.offsetMax = Vector2.zero;

        // Label XP courant
        var xpGO          = new GameObject("XPLbl");
        xpGO.transform.SetParent(_barRoot, false);
        _xpLbl            = xpGO.AddComponent<TextMeshProUGUI>();
        _xpLbl.fontSize   = 20f;
        _xpLbl.color      = ColXPLbl;
        _xpLbl.alignment  = TextAlignmentOptions.Center;
        _xpLbl.raycastTarget = false;
        MenuAssets.ApplyFont(_xpLbl);
        var xpRT = _xpLbl.rectTransform;
        xpRT.anchorMin = Vector2.zero;
        xpRT.anchorMax = Vector2.one;
        xpRT.offsetMin = xpRT.offsetMax = Vector2.zero;

        // Point d'arrivée des boules (centre de la barre)
        var targetGO      = new GameObject("BallTarget");
        targetGO.transform.SetParent(_canvasRT, false);
        _ballTarget       = targetGO.AddComponent<RectTransform>();
        _ballTarget.anchorMin = new Vector2(0.5f, 1f);
        _ballTarget.anchorMax = new Vector2(0.5f, 1f);
        _ballTarget.pivot     = new Vector2(0.5f, 0.5f);
        _ballTarget.sizeDelta = Vector2.zero;
        _ballTarget.anchoredPosition = new Vector2(0f, PosY - 55f);
    }

    // ── Logique ───────────────────────────────────────────────────────────────

    private void RefreshInstant()
    {
        var plm = PlayerLevelManager.Instance;
        if (plm == null) return;

        int level = Mathf.Clamp(plm.Level, 1, MaxLevel);
        bool maxed = level >= MaxLevel;

        _lvlVal.text  = level.ToString();
        _lvlVal.color = maxed ? ColMaxed : ColLvlVal;

        if (_fillImg != null)
        {
            _fillImg.fillAmount = maxed ? 1f : plm.XPRatio;
            _fillImg.color      = maxed ? ColMaxed : ColBarFill;
        }

        if (_xpLbl != null)
        {
            _xpLbl.text = maxed
                ? "MAX"
                : $"{plm.CurrentXP} / {plm.XPToNextLevel} XP";
        }
    }

    private void OnProgressChanged()
    {
        if (_fillCoroutine != null) StopCoroutine(_fillCoroutine);
        _fillCoroutine = StartCoroutine(AnimateFill());
    }

    private void OnLevelUp(int newLevel)
    {
        int clamped = Mathf.Clamp(newLevel, 1, MaxLevel);
        _lvlVal.text = clamped.ToString();

        if (newLevel >= MaxLevel)
        {
            _lvlVal.color  = ColMaxed;
            _fillImg.color = ColMaxed;
            _xpLbl.text    = "MAX";

            // Pulse jaune sur la porte
            var door = FindFirstObjectByType<MenuDoor>();
            door?.StartYellowPulse();
        }

        StartCoroutine(PulseBar());
    }

    // ── Animation fill ────────────────────────────────────────────────────────

    private IEnumerator AnimateFill()
    {
        var plm = PlayerLevelManager.Instance;
        if (plm == null || _fillImg == null) yield break;

        int   level  = Mathf.Clamp(plm.Level, 1, MaxLevel);
        bool  maxed  = level >= MaxLevel;
        float target = maxed ? 1f : plm.XPRatio;
        float start  = _fillImg.fillAmount;
        float t      = 0f;

        while (t < FillAnimDur)
        {
            t += Time.deltaTime;
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / FillAnimDur), 3f);
            _fillImg.fillAmount = Mathf.Lerp(start, target, e);

            if (_xpLbl != null && !maxed)
            {
                int cur = Mathf.RoundToInt(Mathf.Lerp(0, plm.CurrentXP, e));
                _xpLbl.text = $"{cur} / {plm.XPToNextLevel} XP";
            }

            yield return null;
        }

        _fillImg.fillAmount = target;
        RefreshInstant();
    }

    // ── Pulse barre ───────────────────────────────────────────────────────────

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

    // ── Boules bleues ─────────────────────────────────────────────────────────

    private IEnumerator SpawnBallSequence(int count)
    {
        float interval = Mathf.Clamp(BallFlyDur / count * 0.6f, 0.04f, 0.18f);

        for (int i = 0; i < count; i++)
        {
            StartCoroutine(FlyBall());
            yield return new WaitForSeconds(interval);
        }
    }

    private IEnumerator FlyBall()
    {
        var go  = new GameObject("XPBall");
        go.transform.SetParent(_canvasRT, false);

        var img = go.AddComponent<Image>();
        img.sprite = SpriteGenerator.CreateCircle(32);
        img.color  = ColBall;
        img.raycastTarget = false;

        var rt         = img.rectTransform;
        rt.anchorMin   = new Vector2(0.5f, 0.5f);
        rt.anchorMax   = new Vector2(0.5f, 0.5f);
        rt.pivot       = new Vector2(0.5f, 0.5f);
        rt.sizeDelta   = new Vector2(BallSize, BallSize);

        // Départ : position aléatoire dans la moitié basse du canvas
        Vector2 startAP = new Vector2(
            Random.Range(-400f, 400f),
            Random.Range(-600f, -200f));

        // Arrivée : centre de la barre XP
        Vector2 endAP = _ballTarget != null
            ? _ballTarget.anchoredPosition
            : new Vector2(0f, PosY - 55f);

        // Point de contrôle Bézier
        Vector2 ctrl = Vector2.Lerp(startAP, endAP, 0.45f)
                     + new Vector2(Random.Range(-200f, 200f), Random.Range(100f, 350f));

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
                + e * e                 * endAP;

            float alpha = n > 0.75f ? Mathf.InverseLerp(1f, 0.75f, n) : 1f;
            img.color   = new Color(ColBall.r, ColBall.g, ColBall.b, alpha);

            float s = Mathf.Sin(n * Mathf.PI) * 0.6f + 0.7f;
            rt.localScale = Vector3.one * s;

            yield return null;
        }

        Destroy(go);

        // Pulse de la barre à chaque boule qui arrive
        StartCoroutine(PulseBar());
    }
}
