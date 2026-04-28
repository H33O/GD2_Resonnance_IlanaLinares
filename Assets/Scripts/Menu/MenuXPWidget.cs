using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Widget XP permanent affiché dans la scène Menu.
///
/// Affiche :
///   • "NIV X / 4"   — niveau courant
///   • "XX / 100 XP" — compteur XP
///   • Jauge bleue   — remplissage gauche → droite
///
/// Animations :
///   • <see cref="AnimateXPGain"/> : jauge + compteur animés en douceur
///   • Montée de niveau : flash blanc → chiffre change → bounce scale + "NIV UP !" éphémère
///   • Niveau max : couleurs dorées, pulse continu, <see cref="MenuDoor.StartYellowPulse"/>
///
/// Usage : <see cref="Create(RectTransform)"/> depuis <see cref="MenuMainSetup"/>.
/// Point cible des tokens XP : <see cref="GetBarWorldCenter"/>.
/// </summary>
public class MenuXPWidget : MonoBehaviour
{
    // ── Palette ───────────────────────────────────────────────────────────────

    private static readonly Color ColBg          = new Color(0.06f, 0.06f, 0.14f, 0.96f);
    private static readonly Color ColBorder      = new Color(0.25f, 0.60f, 1.00f, 0.30f);
    private static readonly Color ColLevelLbl    = new Color(1.00f, 1.00f, 1.00f, 0.42f);
    private static readonly Color ColLevelVal    = new Color(0.40f, 0.78f, 1.00f, 1.00f);
    private static readonly Color ColLevelMax    = new Color(1.00f, 0.85f, 0.10f, 1.00f);
    private static readonly Color ColXPLbl       = new Color(1.00f, 1.00f, 1.00f, 0.50f);
    private static readonly Color ColXPMax       = new Color(1.00f, 0.85f, 0.10f, 0.80f);
    private static readonly Color ColBarBg       = new Color(0.10f, 0.10f, 0.22f, 1.00f);
    private static readonly Color ColBarFill     = new Color(0.25f, 0.60f, 1.00f, 1.00f);
    private static readonly Color ColBarGlow     = new Color(0.40f, 0.75f, 1.00f, 0.45f);
    private static readonly Color ColBarMax      = new Color(1.00f, 0.85f, 0.10f, 1.00f);
    private static readonly Color ColLevelUpBg   = new Color(0.25f, 0.60f, 1.00f, 0.18f);
    private static readonly Color ColLevelUpText = new Color(0.55f, 0.90f, 1.00f, 1.00f);

    // ── Layout ────────────────────────────────────────────────────────────────

    private const float WidgetW  = 340f;
    private const float WidgetH  = 100f;
    private const float BarH     = 14f;
    private const float PosY     = -110f; // depuis le haut du canvas

    // ── Timings ───────────────────────────────────────────────────────────────

    private const float FillDur         = 0.70f;
    private const float BounceDur       = 0.42f;
    private const float FlashDur        = 0.30f;
    private const float LevelUpToastDur = 1.80f;
    private const float MaxPulsePeriod  = 1.60f;

    // ── Références ────────────────────────────────────────────────────────────

    private RectTransform   _root;
    private TextMeshProUGUI _levelVal;
    private TextMeshProUGUI _xpLabel;
    private Image           _barFill;
    private Image           _barGlow;
    private Image           _flashOverlay;
    private RectTransform   _toastRT;
    private TextMeshProUGUI _toastLabel;

    // ── État interne ──────────────────────────────────────────────────────────

    private float _displayedRatio = 0f;
    private bool  _maxPulseRunning;

    // ── Factory ───────────────────────────────────────────────────────────────

    public static MenuXPWidget Create(RectTransform canvasRT)
    {
        var go = new GameObject("MenuXPWidget");
        go.transform.SetParent(canvasRT, false);
        return go.AddComponent<MenuXPWidget>();
    }

    /// <summary>Retourne le centre monde de la barre XP (cible des tokens).</summary>
    public Vector3 GetBarWorldCenter()
        => _barFill != null
            ? _barFill.rectTransform.TransformPoint(_barFill.rectTransform.rect.center)
            : (_root != null ? _root.TransformPoint(_root.rect.center) : Vector3.zero);

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        BuildUI();

        PlayerLevelManager.EnsureExists();
        PlayerLevelManager.Instance.OnLevelUp         += HandleLevelUp;
        PlayerLevelManager.Instance.OnProgressChanged += RefreshInstant;
    }

    private void OnDestroy()
    {
        if (PlayerLevelManager.Instance == null) return;
        PlayerLevelManager.Instance.OnLevelUp         -= HandleLevelUp;
        PlayerLevelManager.Instance.OnProgressChanged -= RefreshInstant;
    }

    private void Start() => RefreshInstant();

    // ── Construction UI ───────────────────────────────────────────────────────

    private void BuildUI()
    {
        // Racine
        _root                  = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
        _root.anchorMin        = new Vector2(0.5f, 1f);
        _root.anchorMax        = new Vector2(0.5f, 1f);
        _root.pivot            = new Vector2(0.5f, 1f);
        _root.sizeDelta        = new Vector2(WidgetW, WidgetH);
        _root.anchoredPosition = new Vector2(0f, PosY);

        // Fond
        var bg = Make<Image>(_root, "Bg");
        bg.sprite = SpriteGenerator.CreateWhiteSquare();
        bg.color  = ColBg;
        bg.raycastTarget = false;
        Stretch(bg.rectTransform);

        // Bordure bleue subtile
        var border = Make<Image>(_root, "Border");
        border.sprite = SpriteGenerator.CreateWhiteSquare();
        border.color  = ColBorder;
        border.raycastTarget = false;
        var bRT = border.rectTransform;
        bRT.anchorMin = Vector2.zero; bRT.anchorMax = Vector2.one;
        bRT.offsetMin = new Vector2(-1f, -1f); bRT.offsetMax = new Vector2(1f, 1f);
        border.transform.SetAsFirstSibling();

        // Flash overlay (level-up blanc)
        _flashOverlay = Make<Image>(_root, "Flash");
        _flashOverlay.sprite = SpriteGenerator.CreateWhiteSquare();
        _flashOverlay.color  = new Color(1f, 1f, 1f, 0f);
        _flashOverlay.raycastTarget = false;
        Stretch(_flashOverlay.rectTransform);

        // Label "NIV" à gauche
        var nivLbl = Make<TextMeshProUGUI>(_root, "NivLbl");
        nivLbl.text      = "NIV";
        nivLbl.fontSize  = 14f;
        nivLbl.fontStyle = FontStyles.Bold;
        nivLbl.color     = ColLevelLbl;
        nivLbl.alignment = TextAlignmentOptions.MidlineLeft;
        nivLbl.raycastTarget = false;
        MenuAssets.ApplyFont(nivLbl);
        Place(nivLbl.rectTransform, 0f, 0.58f, 0.30f, 1f, 14f, 0f, -6f, 0f);

        // Chiffre niveau
        _levelVal           = Make<TextMeshProUGUI>(_root, "LevelVal");
        _levelVal.text      = "1";
        _levelVal.fontSize  = 38f;
        _levelVal.fontStyle = FontStyles.Bold;
        _levelVal.color     = ColLevelVal;
        _levelVal.alignment = TextAlignmentOptions.MidlineLeft;
        _levelVal.raycastTarget = false;
        MenuAssets.ApplyFont(_levelVal);
        Place(_levelVal.rectTransform, 0.18f, 0.52f, 0.55f, 1.00f, 0f, 0f, -6f, 0f);

        // "/ 4" à droite du chiffre
        var maxLbl = Make<TextMeshProUGUI>(_root, "MaxLbl");
        maxLbl.text      = $"/ {PlayerLevelManager.MaxLevel}";
        maxLbl.fontSize  = 16f;
        maxLbl.fontStyle = FontStyles.Normal;
        maxLbl.color     = ColLevelLbl;
        maxLbl.alignment = TextAlignmentOptions.BottomLeft;
        maxLbl.raycastTarget = false;
        MenuAssets.ApplyFont(maxLbl);
        Place(maxLbl.rectTransform, 0.44f, 0.50f, 0.80f, 0.90f, 0f, 0f, 0f, 0f);

        // Compteur XP (droite)
        _xpLabel           = Make<TextMeshProUGUI>(_root, "XPLabel");
        _xpLabel.text      = $"0 / {PlayerLevelManager.XPPerLevel} XP";
        _xpLabel.fontSize  = 14f;
        _xpLabel.fontStyle = FontStyles.Normal;
        _xpLabel.color     = ColXPLbl;
        _xpLabel.alignment = TextAlignmentOptions.MidlineRight;
        _xpLabel.raycastTarget = false;
        MenuAssets.ApplyFont(_xpLabel);
        Place(_xpLabel.rectTransform, 0f, 0.58f, 1f, 1f, 14f, 0f, -12f, 0f);

        // Fond barre
        var barBg = Make<Image>(_root, "BarBg");
        barBg.sprite = SpriteGenerator.CreateWhiteSquare();
        barBg.color  = ColBarBg;
        barBg.raycastTarget = false;
        var barBgRT = barBg.rectTransform;
        barBgRT.anchorMin = new Vector2(0f, 0f);
        barBgRT.anchorMax = new Vector2(1f, 0f);
        barBgRT.pivot     = new Vector2(0f, 0f);
        barBgRT.offsetMin = new Vector2(14f, 12f);
        barBgRT.offsetMax = new Vector2(-14f, 12f + BarH);

        // Glow derrière la barre
        _barGlow = Make<Image>(_root, "BarGlow");
        _barGlow.sprite = SpriteGenerator.CreateWhiteSquare();
        _barGlow.color  = new Color(ColBarGlow.r, ColBarGlow.g, ColBarGlow.b, 0f);
        _barGlow.raycastTarget = false;
        var glowRT = _barGlow.rectTransform;
        glowRT.anchorMin = new Vector2(0f, 0f);
        glowRT.anchorMax = new Vector2(1f, 0f);
        glowRT.pivot     = new Vector2(0f, 0f);
        glowRT.offsetMin = new Vector2(10f, 8f);
        glowRT.offsetMax = new Vector2(-10f, 8f + BarH + 6f);

        // Fill barre
        _barFill = Make<Image>(_root, "BarFill");
        _barFill.sprite      = SpriteGenerator.CreateWhiteSquare();
        _barFill.color       = ColBarFill;
        _barFill.type        = Image.Type.Filled;
        _barFill.fillMethod  = Image.FillMethod.Horizontal;
        _barFill.fillAmount  = 0f;
        _barFill.raycastTarget = false;
        var fillRT = _barFill.rectTransform;
        fillRT.anchorMin = new Vector2(0f, 0f);
        fillRT.anchorMax = new Vector2(1f, 0f);
        fillRT.pivot     = new Vector2(0f, 0f);
        fillRT.offsetMin = new Vector2(14f, 12f);
        fillRT.offsetMax = new Vector2(-14f, 12f + BarH);

        // Toast "NIVEAU X !" (caché par défaut)
        BuildToast();
    }

    private void BuildToast()
    {
        var toastGO = new GameObject("LevelUpToast");
        toastGO.transform.SetParent(_root, false);
        _toastRT = toastGO.AddComponent<RectTransform>();
        _toastRT.anchorMin        = new Vector2(0.5f, 1f);
        _toastRT.anchorMax        = new Vector2(0.5f, 1f);
        _toastRT.pivot            = new Vector2(0.5f, 0f);
        _toastRT.sizeDelta        = new Vector2(260f, 42f);
        _toastRT.anchoredPosition = new Vector2(0f, 6f);

        var bg = toastGO.AddComponent<Image>();
        bg.sprite = SpriteGenerator.CreateWhiteSquare();
        bg.color  = ColLevelUpBg;
        bg.raycastTarget = false;

        var group = toastGO.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        _toastLabel           = Make<TextMeshProUGUI>(_toastRT, "ToastLbl");
        _toastLabel.text      = "NIVEAU 2 !";
        _toastLabel.fontSize  = 22f;
        _toastLabel.fontStyle = FontStyles.Bold;
        _toastLabel.color     = ColLevelUpText;
        _toastLabel.alignment = TextAlignmentOptions.Center;
        _toastLabel.raycastTarget = false;
        MenuAssets.ApplyFont(_toastLabel);
        Stretch(_toastLabel.rectTransform);
    }

    // ── Refresh immédiat ──────────────────────────────────────────────────────

    private void RefreshInstant()
    {
        var   plm   = PlayerLevelManager.Instance;
        bool  maxed = plm.IsMaxLevel;

        if (_levelVal != null)
        {
            _levelVal.text  = plm.Level.ToString();
            _levelVal.color = maxed ? ColLevelMax : ColLevelVal;
        }

        if (_xpLabel != null)
            _xpLabel.text = maxed ? "MAX" : $"{plm.CurrentXP} / {PlayerLevelManager.XPPerLevel} XP";

        if (_barFill != null)
        {
            _barFill.fillAmount = plm.XPRatio;
            _barFill.color      = maxed ? ColBarMax : ColBarFill;
        }

        _displayedRatio = plm.XPRatio;
    }

    // ── Animation XP (appelée par MenuXPReceiver) ─────────────────────────────

    /// <summary>
    /// Anime la jauge et le compteur vers la nouvelle valeur XP.
    /// Appelé par <see cref="MenuXPReceiver"/> après avoir crédité l'XP.
    /// </summary>
    public void AnimateXPGain(int xpBefore, int xpAfter, int levelBefore)
    {
        StopCoroutine(nameof(XPFillRoutine));
        StartCoroutine(XPFillRoutine(xpBefore, xpAfter, levelBefore));
    }

    private IEnumerator XPFillRoutine(int xpBefore, int xpAfter, int levelBefore)
    {
        var plm = PlayerLevelManager.Instance;

        // Si on a monté de niveau, animer jusqu'à 100, puis reset et continuer
        if (plm.Level > levelBefore)
        {
            // Remplir jusqu'au bout
            yield return StartCoroutine(AnimateFillTo(xpBefore / (float)PlayerLevelManager.XPPerLevel,
                                                      1f, FillDur * 0.6f, levelBefore));

            // Flash + bounce level-up
            yield return StartCoroutine(LevelUpSequence(plm.Level));

            // Repartir de 0 vers la nouvelle valeur courante
            if (_barFill != null) _barFill.fillAmount = 0f;
            _displayedRatio = 0f;
            yield return StartCoroutine(AnimateFillTo(0f, plm.XPRatio, FillDur * 0.5f, plm.Level));
        }
        else
        {
            yield return StartCoroutine(AnimateFillTo(_displayedRatio, plm.XPRatio, FillDur, plm.Level));
        }

        RefreshInstant();

        // Si niveau max atteint, lancer le pulse doré en continu
        if (plm.IsMaxLevel && !_maxPulseRunning)
            StartCoroutine(MaxLevelPulse());
    }

    private IEnumerator AnimateFillTo(float from, float to, float dur, int level)
    {
        bool maxed = PlayerLevelManager.Instance.IsMaxLevel;
        int  xpFrom = Mathf.RoundToInt(from * PlayerLevelManager.XPPerLevel);
        int  xpTo   = Mathf.RoundToInt(to   * PlayerLevelManager.XPPerLevel);

        // Glow de la barre s'allume pendant l'animation
        StartCoroutine(GlowBar(dur));

        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / dur), 3f);

            float ratio = Mathf.Lerp(from, to, e);
            if (_barFill != null) _barFill.fillAmount = ratio;
            _displayedRatio = ratio;

            if (_xpLabel != null && !maxed)
                _xpLabel.text = $"{Mathf.RoundToInt(Mathf.Lerp(xpFrom, xpTo, e))} / {PlayerLevelManager.XPPerLevel} XP";

            yield return null;
        }
    }

    private IEnumerator GlowBar(float dur)
    {
        if (_barGlow == null) yield break;
        float half = dur * 0.5f;

        // Fade in
        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            _barGlow.color = new Color(ColBarGlow.r, ColBarGlow.g, ColBarGlow.b,
                                       Mathf.Clamp01(t / half) * ColBarGlow.a);
            yield return null;
        }
        // Fade out
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            _barGlow.color = new Color(ColBarGlow.r, ColBarGlow.g, ColBarGlow.b,
                                       (1f - Mathf.Clamp01(t / half)) * ColBarGlow.a);
            yield return null;
        }
        _barGlow.color = new Color(ColBarGlow.r, ColBarGlow.g, ColBarGlow.b, 0f);
    }

    // ── Level-up sequence ─────────────────────────────────────────────────────

    private void HandleLevelUp(int newLevel)
    {
        // La séquence est déclenchée depuis XPFillRoutine pour synchroniser
        // avec l'animation de la barre — on ne fait rien ici directement.
    }

    private IEnumerator LevelUpSequence(int newLevel)
    {
        // 1. Flash blanc rapide
        yield return StartCoroutine(FlashWidget(FlashDur));

        // 2. Mise à jour du chiffre de niveau
        if (_levelVal != null)
        {
            bool maxed = newLevel >= PlayerLevelManager.MaxLevel;
            _levelVal.text  = newLevel.ToString();
            _levelVal.color = maxed ? ColLevelMax : ColLevelVal;
        }

        // 3. Toast "NIVEAU X !"
        StartCoroutine(ShowToast(newLevel));

        // 4. Bounce scale du widget
        yield return StartCoroutine(BounceWidget());

        // 5. Si niveau max : déclencher la porte
        if (newLevel >= PlayerLevelManager.MaxLevel)
        {
            var door = FindFirstObjectByType<MenuDoor>();
            door?.StartYellowPulse();

            var doorMgr = FindFirstObjectByType<DoorManager>();
            doorMgr?.ForceUnlock();
        }
    }

    private IEnumerator FlashWidget(float dur)
    {
        if (_flashOverlay == null) yield break;

        float t = 0f;
        while (t < dur * 0.4f)
        {
            t += Time.deltaTime;
            _flashOverlay.color = new Color(1f, 1f, 1f, Mathf.Clamp01(t / (dur * 0.4f)) * 0.55f);
            yield return null;
        }
        t = 0f;
        while (t < dur * 0.6f)
        {
            t += Time.deltaTime;
            _flashOverlay.color = new Color(1f, 1f, 1f, (1f - Mathf.Clamp01(t / (dur * 0.6f))) * 0.55f);
            yield return null;
        }
        _flashOverlay.color = new Color(1f, 1f, 1f, 0f);
    }

    private IEnumerator BounceWidget()
    {
        if (_root == null) yield break;

        float t = 0f;
        while (t < BounceDur * 0.45f)
        {
            t += Time.deltaTime;
            float e = Mathf.Clamp01(t / (BounceDur * 0.45f));
            _root.localScale = Vector3.one * Mathf.Lerp(1f, 1.22f, e);
            yield return null;
        }
        t = 0f;
        while (t < BounceDur * 0.55f)
        {
            t += Time.deltaTime;
            float e = Mathf.Clamp01(t / (BounceDur * 0.55f));
            float overshoot = Mathf.Sin(e * Mathf.PI) * 0.06f;
            _root.localScale = Vector3.one * (Mathf.Lerp(1.22f, 1f, e) + overshoot);
            yield return null;
        }
        _root.localScale = Vector3.one;
    }

    private IEnumerator ShowToast(int newLevel)
    {
        if (_toastRT == null || _toastLabel == null) yield break;

        var group = _toastRT.GetComponent<CanvasGroup>();
        if (group == null) yield break;

        bool maxed = newLevel >= PlayerLevelManager.MaxLevel;
        _toastLabel.text  = maxed ? "MAX LEVEL !" : $"NIVEAU {newLevel} !";
        _toastLabel.color = maxed ? ColLevelMax   : ColLevelUpText;

        // Slide up + fade in
        float t = 0f;
        float fadeDur = 0.22f;
        while (t < fadeDur)
        {
            t += Time.deltaTime;
            float e = Mathf.Clamp01(t / fadeDur);
            group.alpha = e;
            _toastRT.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, 12f, e));
            yield return null;
        }

        yield return new WaitForSeconds(LevelUpToastDur);

        // Fade out
        t = 0f;
        while (t < fadeDur)
        {
            t += Time.deltaTime;
            group.alpha = 1f - Mathf.Clamp01(t / fadeDur);
            yield return null;
        }
        group.alpha = 0f;
    }

    private IEnumerator MaxLevelPulse()
    {
        _maxPulseRunning = true;
        while (true)
        {
            if (_barGlow == null) { yield return null; continue; }

            float t = 0f;
            while (t < MaxPulsePeriod)
            {
                t += Time.deltaTime;
                float e = Mathf.Sin(Mathf.Clamp01(t / MaxPulsePeriod) * Mathf.PI);
                _barGlow.color = new Color(ColBarMax.r, ColBarMax.g, ColBarMax.b, e * 0.40f);
                yield return null;
            }

            yield return new WaitForSeconds(0.30f);
        }
    }

    // ── Helpers UI ────────────────────────────────────────────────────────────

    private static T Make<T>(Transform parent, string name) where T : Component
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<T>();
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static void Place(RectTransform rt,
        float ax, float ay, float bx, float by,
        float ox, float oy, float ox2, float oy2)
    {
        rt.anchorMin = new Vector2(ax, ay);
        rt.anchorMax = new Vector2(bx, by);
        rt.offsetMin = new Vector2(ox,  oy);
        rt.offsetMax = new Vector2(ox2, oy2);
    }
}
