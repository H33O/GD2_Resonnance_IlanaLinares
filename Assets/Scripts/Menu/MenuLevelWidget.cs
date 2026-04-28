using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Widget de niveau affiché en permanence dans la scène Menu.
///
/// Affiche :
///   • "NIV  X" — label + grand chiffre
///   • "X / 100 XP" — compteur XP sous le chiffre
///   • Une jauge bleue en bas du widget
///
/// Quand le joueur monte de niveau :
///   - La jauge se remet à zéro puis le compteur et le chiffre se mettent à jour
///   - Un pulse visuel anime le widget
///   - Si niveau 4 atteint : couleurs dorées + MenuDoor.StartYellowPulse()
///
/// Usage : <see cref="Create(RectTransform)"/> depuis <see cref="MenuMainSetup"/>.
/// Point d'arrivée des tokens XP : <see cref="GetWorldCenter"/>.
/// </summary>
public class MenuLevelWidget : MonoBehaviour
{
    // ── Palette ───────────────────────────────────────────────────────────────

    private static readonly Color ColBg       = new Color(0.06f, 0.06f, 0.12f, 0.92f);
    private static readonly Color ColBorder   = new Color(1.00f, 1.00f, 1.00f, 0.10f);
    private static readonly Color ColLabel    = new Color(1.00f, 1.00f, 1.00f, 0.38f);
    private static readonly Color ColLevel    = new Color(0.40f, 0.75f, 1.00f, 1.00f);
    private static readonly Color ColLevelMax = new Color(1.00f, 0.85f, 0.10f, 1.00f);
    private static readonly Color ColBarBg    = new Color(0.10f, 0.10f, 0.22f, 1.00f);
    private static readonly Color ColBarFill  = new Color(0.25f, 0.60f, 1.00f, 1.00f);
    private static readonly Color ColBarMax   = new Color(1.00f, 0.85f, 0.10f, 1.00f);
    private static readonly Color ColXPCount  = new Color(1.00f, 1.00f, 1.00f, 0.65f);

    // ── Layout ────────────────────────────────────────────────────────────────

    private const float WidgetW = 320f;
    private const float WidgetH = 148f;   // plus haut pour accueillir jauge + compteur
    private const float BarH    = 12f;
    private const float PosY    = -148f;  // depuis le haut du canvas

    // ── Animation ─────────────────────────────────────────────────────────────

    private const float PulseDur = 0.28f;
    private const float PulseS   = 1.18f;
    private const float FillDur  = 0.55f;

    // ── Références ────────────────────────────────────────────────────────────

    private RectTransform   _root;
    private TextMeshProUGUI _levelNum;
    private TextMeshProUGUI _levelSub;
    private TextMeshProUGUI _xpCounter;
    private Image           _barFill;

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>Crée le widget et l'attache au canvas.</summary>
    public static MenuLevelWidget Create(RectTransform canvasRT)
    {
        var go = new GameObject("MenuLevelWidget");
        go.transform.SetParent(canvasRT, false);
        return go.AddComponent<MenuLevelWidget>();
    }

    /// <summary>Retourne le centre monde du widget (cible des tokens volants).</summary>
    public Vector3 GetWorldCenter()
        => _root != null ? _root.TransformPoint(_root.rect.center) : Vector3.zero;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        BuildUI();

        PlayerLevelManager.EnsureExists();
        PlayerLevelManager.Instance.OnLevelUp         += OnLevelUp;
        PlayerLevelManager.Instance.OnProgressChanged += RefreshInstant;
    }

    private void OnDestroy()
    {
        if (PlayerLevelManager.Instance == null) return;
        PlayerLevelManager.Instance.OnLevelUp         -= OnLevelUp;
        PlayerLevelManager.Instance.OnProgressChanged -= RefreshInstant;
    }

    private void Start() => RefreshInstant();

    // ── Construction ─────────────────────────────────────────────────────────

    private void BuildUI()
    {
        // Racine
        _root                  = gameObject.AddComponent<RectTransform>();
        _root.anchorMin        = new Vector2(0.5f, 1f);
        _root.anchorMax        = new Vector2(0.5f, 1f);
        _root.pivot            = new Vector2(0.5f, 1f);
        _root.sizeDelta        = new Vector2(WidgetW, WidgetH);
        _root.anchoredPosition = new Vector2(0f, PosY);

        // Fond
        var bgGO  = new GameObject("Bg");
        bgGO.transform.SetParent(_root, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.sprite = SpriteGenerator.CreateWhiteSquare();
        bgImg.color  = ColBg;
        bgImg.raycastTarget = false;
        var bgRT = bgImg.rectTransform;
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

        // Bordure fine (en premier dans la hiérarchie → derrière tout)
        var frameGO  = new GameObject("Border");
        frameGO.transform.SetParent(_root, false);
        var frameImg = frameGO.AddComponent<Image>();
        frameImg.sprite = SpriteGenerator.CreateWhiteSquare();
        frameImg.color  = ColBorder;
        frameImg.raycastTarget = false;
        var frameRT = frameImg.rectTransform;
        frameRT.anchorMin = Vector2.zero;
        frameRT.anchorMax = Vector2.one;
        frameRT.offsetMin = new Vector2(-1f, -1f);
        frameRT.offsetMax = new Vector2( 1f,  1f);
        frameGO.transform.SetAsFirstSibling();

        // Label "NIV" en petit — haut gauche
        var lblGO = new GameObject("NiveauLbl");
        lblGO.transform.SetParent(_root, false);
        var lbl   = lblGO.AddComponent<TextMeshProUGUI>();
        lbl.text          = "NIV";
        lbl.fontSize      = 18f;
        lbl.fontStyle     = FontStyles.Bold;
        lbl.color         = ColLabel;
        lbl.alignment     = TextAlignmentOptions.TopLeft;
        lbl.raycastTarget = false;
        MenuAssets.ApplyFont(lbl);
        var lblRT = lbl.rectTransform;
        lblRT.anchorMin = new Vector2(0f, 0.55f);
        lblRT.anchorMax = new Vector2(0.4f, 1.00f);
        lblRT.offsetMin = new Vector2(14f, 0f);
        lblRT.offsetMax = Vector2.zero;

        // Grand chiffre niveau — haut gauche sous "NIV"
        var numGO = new GameObject("LevelNum");
        numGO.transform.SetParent(_root, false);
        _levelNum           = numGO.AddComponent<TextMeshProUGUI>();
        _levelNum.text      = "1";
        _levelNum.fontSize  = 52f;
        _levelNum.fontStyle = FontStyles.Bold;
        _levelNum.color     = ColLevel;
        _levelNum.alignment = TextAlignmentOptions.TopLeft;
        _levelNum.raycastTarget = false;
        MenuAssets.ApplyFont(_levelNum);
        var numRT = _levelNum.rectTransform;
        numRT.anchorMin = new Vector2(0f, 0.28f);
        numRT.anchorMax = new Vector2(0.5f, 0.70f);
        numRT.offsetMin = new Vector2(14f, 0f);
        numRT.offsetMax = Vector2.zero;

        // "/ 4" à droite du chiffre
        var subGO = new GameObject("LevelSub");
        subGO.transform.SetParent(_root, false);
        _levelSub           = subGO.AddComponent<TextMeshProUGUI>();
        _levelSub.text      = $"/ {PlayerLevelManager.MaxLevel}";
        _levelSub.fontSize  = 22f;
        _levelSub.fontStyle = FontStyles.Normal;
        _levelSub.color     = ColLabel;
        _levelSub.alignment = TextAlignmentOptions.BottomLeft;
        _levelSub.raycastTarget = false;
        MenuAssets.ApplyFont(_levelSub);
        var subRT = _levelSub.rectTransform;
        subRT.anchorMin = new Vector2(0.38f, 0.28f);
        subRT.anchorMax = new Vector2(1.00f, 0.62f);
        subRT.offsetMin = new Vector2(0f, 0f);
        subRT.offsetMax = new Vector2(-10f, 0f);

        // Compteur "0 / 100 XP"
        var cntGO = new GameObject("XPCounter");
        cntGO.transform.SetParent(_root, false);
        _xpCounter           = cntGO.AddComponent<TextMeshProUGUI>();
        _xpCounter.text      = $"0 / {PlayerLevelManager.XPPerLevel} XP";
        _xpCounter.fontSize  = 16f;
        _xpCounter.fontStyle = FontStyles.Normal;
        _xpCounter.color     = ColXPCount;
        _xpCounter.alignment = TextAlignmentOptions.BottomLeft;
        _xpCounter.raycastTarget = false;
        MenuAssets.ApplyFont(_xpCounter);
        var cntRT = _xpCounter.rectTransform;
        cntRT.anchorMin = new Vector2(0f, 0.18f);
        cntRT.anchorMax = new Vector2(1f, 0.36f);
        cntRT.offsetMin = new Vector2(14f, 0f);
        cntRT.offsetMax = new Vector2(-14f, 0f);

        // Fond barre XP
        var barBgGO  = new GameObject("BarBg");
        barBgGO.transform.SetParent(_root, false);
        var barBgImg = barBgGO.AddComponent<Image>();
        barBgImg.sprite = SpriteGenerator.CreateWhiteSquare();
        barBgImg.color  = ColBarBg;
        barBgImg.raycastTarget = false;
        var barBgRT = barBgImg.rectTransform;
        barBgRT.anchorMin = new Vector2(0f, 0f);
        barBgRT.anchorMax = new Vector2(1f, 0f);
        barBgRT.pivot     = new Vector2(0f, 0f);
        barBgRT.offsetMin = new Vector2(14f, 8f);
        barBgRT.offsetMax = new Vector2(-14f, 8f + BarH);

        // Fill barre XP
        var fillGO = new GameObject("BarFill");
        fillGO.transform.SetParent(_root, false);
        _barFill             = fillGO.AddComponent<Image>();
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
        fillRT.offsetMin = new Vector2(14f, 8f);
        fillRT.offsetMax = new Vector2(-14f, 8f + BarH);
    }

    // ── Refresh ───────────────────────────────────────────────────────────────

    private void RefreshInstant()
    {
        var  plm   = PlayerLevelManager.Instance;
        bool maxed = plm.IsMaxLevel;

        if (_levelNum != null)
        {
            _levelNum.text  = plm.Level.ToString();
            _levelNum.color = maxed ? ColLevelMax : ColLevel;
        }

        if (_levelSub != null)
            _levelSub.color = maxed ? ColLevelMax : ColLabel;

        if (_xpCounter != null)
            _xpCounter.text = maxed
                ? "MAX"
                : $"{plm.CurrentXP} / {PlayerLevelManager.XPPerLevel} XP";

        if (_barFill != null)
        {
            _barFill.fillAmount = maxed ? 1f : plm.XPRatio;
            _barFill.color      = maxed ? ColBarMax : ColBarFill;
        }
    }

    // ── OnLevelUp ─────────────────────────────────────────────────────────────

    private void OnLevelUp(int newLevel)
    {
        RefreshInstant();
        StartCoroutine(PulseWidget());

        if (newLevel >= PlayerLevelManager.MaxLevel)
        {
            var door = FindFirstObjectByType<MenuDoor>();
            door?.StartYellowPulse();
        }
    }

    // ── AnimateXPGain — appelé par MenuXPReceiver après transfert ─────────────

    /// <summary>Anime la jauge et le compteur vers la nouvelle valeur XP.</summary>
    public void AnimateXPGain(int xpAdded)
    {
        StartCoroutine(AnimateFill());
    }

    private IEnumerator AnimateFill()
    {
        var  plm     = PlayerLevelManager.Instance;
        bool maxed   = plm.IsMaxLevel;
        float target = maxed ? 1f : plm.XPRatio;
        float start  = _barFill != null ? _barFill.fillAmount : 0f;

        int xpStart  = Mathf.RoundToInt(start * PlayerLevelManager.XPPerLevel);
        int xpTarget = maxed ? PlayerLevelManager.XPPerLevel : plm.CurrentXP;

        float t = 0f;
        while (t < FillDur)
        {
            t += Time.deltaTime;
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / FillDur), 3f);

            if (_barFill != null)
                _barFill.fillAmount = Mathf.Lerp(start, target, e);

            if (_xpCounter != null && !maxed)
                _xpCounter.text = $"{Mathf.RoundToInt(Mathf.Lerp(xpStart, xpTarget, e))} / {PlayerLevelManager.XPPerLevel} XP";

            yield return null;
        }

        RefreshInstant();
    }

    // ── Pulse ─────────────────────────────────────────────────────────────────

    private IEnumerator PulseWidget()
    {
        if (_root == null) yield break;
        float t = 0f;
        while (t < PulseDur)
        {
            t += Time.deltaTime;
            float e = Mathf.Sin(Mathf.Clamp01(t / PulseDur) * Mathf.PI);
            _root.localScale = Vector3.Lerp(Vector3.one, Vector3.one * PulseS, e);
            yield return null;
        }
        _root.localScale = Vector3.one;
    }
}
