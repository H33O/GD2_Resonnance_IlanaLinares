using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Widget XP du menu principal.
///
/// Affiche :
///   • Le niveau courant  (NV. X / 4)
///   • Un compteur animé  XX / 100
///   • Une jauge bleue qui se remplit de gauche à droite
///
/// Quand le niveau monte, un flash doré + scale-bounce + glow pulsent.
/// Quand le niveau 4 est atteint, la jauge passe en doré et
/// <see cref="MenuDoor.StartYellowPulse"/> est appelé.
///
/// Usage : <see cref="Create(RectTransform)"/> depuis <see cref="MenuMainSetup"/>.
/// Point d'arrivée des tokens : <see cref="GetWorldCenter"/>.
/// </summary>
public class MenuXPBar : MonoBehaviour
{
    // ── Palette ───────────────────────────────────────────────────────────────

    private static readonly Color ColBg           = new Color(0.08f, 0.08f, 0.14f, 0.95f);
    private static readonly Color ColBarBg        = new Color(0.12f, 0.12f, 0.22f, 1.00f);
    private static readonly Color ColBarFill      = new Color(0.25f, 0.60f, 1.00f, 1.00f);
    private static readonly Color ColBarMax       = new Color(1.00f, 0.85f, 0.10f, 1.00f);
    private static readonly Color ColLvlLbl       = new Color(1.00f, 1.00f, 1.00f, 0.45f);
    private static readonly Color ColLvlMax       = new Color(1.00f, 0.85f, 0.10f, 1.00f);
    private static readonly Color ColCounter      = new Color(1.00f, 1.00f, 1.00f, 0.92f);
    private static readonly Color ColLevelUpFlash = new Color(1.00f, 0.90f, 0.20f, 0.70f);
    private static readonly Color ColGlow         = new Color(0.25f, 0.60f, 1.00f, 0.35f);

    // ── Layout ────────────────────────────────────────────────────────────────

    private const float WidgetW = 520f;
    private const float WidgetH = 80f;
    private const float BarH    = 18f;
    /// <summary>Positionné sous le <see cref="MenuLevelWidget"/> (PosY = -248 = -148 widget - 100 h).</summary>
    private const float PosY    = -268f;

    // ── Animation ─────────────────────────────────────────────────────────────

    private const float FillDur       = 0.55f;
    private const float PulseDur      = 0.22f;
    private const float PulseS        = 1.12f;
    private const float LevelUpBounce = 0.38f;
    private const float LevelUpFlash  = 0.50f;
    private const float GlowDur       = 1.20f;

    // ── Références ────────────────────────────────────────────────────────────

    private RectTransform   _root;
    private Image           _fill;
    private Image           _glowOverlay;
    private TextMeshProUGUI _counterLbl;
    private TextMeshProUGUI _levelLbl;
    private RectTransform   _canvasRT;

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>Crée le widget et l'attache au canvas.</summary>
    public static MenuXPBar Create(RectTransform canvasRT)
    {
        var go        = new GameObject("MenuXPBar");
        go.transform.SetParent(canvasRT, false);
        var comp      = go.AddComponent<MenuXPBar>();
        comp._canvasRT = canvasRT;
        return comp;
    }

    /// <summary>Retourne le centre monde du widget (cible des tokens volants).</summary>
    public Vector3 GetWorldCenter()
        => _root != null ? _root.TransformPoint(_root.rect.center) : Vector3.zero;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        BuildUI();

        PlayerLevelManager.EnsureExists();
        PlayerLevelManager.Instance.OnProgressChanged += RefreshInstant;
        PlayerLevelManager.Instance.OnLevelUp         += OnLevelUp;
    }

    private void OnDestroy()
    {
        if (PlayerLevelManager.Instance == null) return;
        PlayerLevelManager.Instance.OnProgressChanged -= RefreshInstant;
        PlayerLevelManager.Instance.OnLevelUp         -= OnLevelUp;
    }

    private void Start() => RefreshInstant();

    // ── Construction ─────────────────────────────────────────────────────────

    private void BuildUI()
    {
        // Racine
        _root            = gameObject.AddComponent<RectTransform>();
        _root.anchorMin  = new Vector2(0.5f, 1f);
        _root.anchorMax  = new Vector2(0.5f, 1f);
        _root.pivot      = new Vector2(0.5f, 1f);
        _root.sizeDelta  = new Vector2(WidgetW, WidgetH);
        _root.anchoredPosition = new Vector2(0f, PosY);

        // Fond
        var bgGO  = new GameObject("Bg");
        bgGO.transform.SetParent(_root, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.sprite = SpriteGenerator.CreateWhiteSquare();
        bgImg.color  = ColBg;
        bgImg.raycastTarget = false;
        var bgRT    = bgImg.rectTransform;
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

        // Label niveau — haut gauche
        var lvlGO    = new GameObject("LevelLbl");
        lvlGO.transform.SetParent(_root, false);
        _levelLbl    = lvlGO.AddComponent<TextMeshProUGUI>();
        _levelLbl.fontSize  = 20f;
        _levelLbl.fontStyle = FontStyles.Bold;
        _levelLbl.color     = ColLvlLbl;
        _levelLbl.alignment = TextAlignmentOptions.TopLeft;
        _levelLbl.raycastTarget = false;
        MenuAssets.ApplyFont(_levelLbl);
        var lvlRT    = _levelLbl.rectTransform;
        lvlRT.anchorMin = new Vector2(0f, 1f);
        lvlRT.anchorMax = new Vector2(0.5f, 1f);
        lvlRT.pivot     = new Vector2(0f, 1f);
        lvlRT.offsetMin = new Vector2(14f, -28f);
        lvlRT.offsetMax = Vector2.zero;

        // Compteur XX / 100 — haut droite
        var cntGO    = new GameObject("Counter");
        cntGO.transform.SetParent(_root, false);
        _counterLbl  = cntGO.AddComponent<TextMeshProUGUI>();
        _counterLbl.fontSize  = 20f;
        _counterLbl.fontStyle = FontStyles.Bold;
        _counterLbl.color     = ColCounter;
        _counterLbl.alignment = TextAlignmentOptions.TopRight;
        _counterLbl.raycastTarget = false;
        MenuAssets.ApplyFont(_counterLbl);
        var cntRT    = _counterLbl.rectTransform;
        cntRT.anchorMin = new Vector2(0.5f, 1f);
        cntRT.anchorMax = new Vector2(1f, 1f);
        cntRT.pivot     = new Vector2(1f, 1f);
        cntRT.offsetMin = new Vector2(0f, -28f);
        cntRT.offsetMax = new Vector2(-14f, 0f);

        // Fond barre
        var barBgGO  = new GameObject("BarBg");
        barBgGO.transform.SetParent(_root, false);
        var barBgImg = barBgGO.AddComponent<Image>();
        barBgImg.sprite = SpriteGenerator.CreateWhiteSquare();
        barBgImg.color  = ColBarBg;
        barBgImg.raycastTarget = false;
        var barBgRT  = barBgImg.rectTransform;
        barBgRT.anchorMin = new Vector2(0f, 0f);
        barBgRT.anchorMax = new Vector2(1f, 0f);
        barBgRT.pivot     = new Vector2(0f, 0f);
        barBgRT.offsetMin = new Vector2(14f, 10f);
        barBgRT.offsetMax = new Vector2(-14f, 10f + BarH);

        // Fill barre
        var fillGO  = new GameObject("Fill");
        fillGO.transform.SetParent(_root, false);
        _fill       = fillGO.AddComponent<Image>();
        _fill.sprite = SpriteGenerator.CreateWhiteSquare();
        _fill.color  = ColBarFill;
        _fill.type   = Image.Type.Filled;
        _fill.fillMethod = Image.FillMethod.Horizontal;
        _fill.fillAmount = 0f;
        _fill.raycastTarget = false;
        var fillRT  = _fill.rectTransform;
        fillRT.anchorMin = new Vector2(0f, 0f);
        fillRT.anchorMax = new Vector2(1f, 0f);
        fillRT.pivot     = new Vector2(0f, 0f);
        fillRT.offsetMin = new Vector2(14f, 10f);
        fillRT.offsetMax = new Vector2(-14f, 10f + BarH);

        // Glow overlay (flash level-up)
        var glowGO  = new GameObject("GlowOverlay");
        glowGO.transform.SetParent(_root, false);
        _glowOverlay = glowGO.AddComponent<Image>();
        _glowOverlay.sprite = SpriteGenerator.CreateWhiteSquare();
        _glowOverlay.color  = new Color(ColGlow.r, ColGlow.g, ColGlow.b, 0f);
        _glowOverlay.raycastTarget = false;
        var glowRT  = _glowOverlay.rectTransform;
        glowRT.anchorMin = Vector2.zero;
        glowRT.anchorMax = Vector2.one;
        glowRT.offsetMin = new Vector2(-6f, -6f);
        glowRT.offsetMax = new Vector2( 6f,  6f);
    }

    // ── Refresh ───────────────────────────────────────────────────────────────

    private void RefreshInstant()
    {
        var plm   = PlayerLevelManager.Instance;
        bool maxed = plm.IsMaxLevel;

        if (_levelLbl != null)
        {
            _levelLbl.text  = $"NV. {plm.Level} / {PlayerLevelManager.MaxLevel}";
            _levelLbl.color = maxed ? ColLvlMax : ColLvlLbl;
        }

        if (_counterLbl != null)
        {
            _counterLbl.text  = maxed ? "MAX" : $"{plm.CurrentXP} / {PlayerLevelManager.XPPerLevel}";
            _counterLbl.color = maxed ? ColLvlMax : ColCounter;
        }

        if (_fill != null)
        {
            _fill.fillAmount = maxed ? 1f : plm.XPRatio;
            _fill.color      = maxed ? ColBarMax : ColBarFill;
        }
    }

    // ── OnLevelUp ─────────────────────────────────────────────────────────────

    private void OnLevelUp(int newLevel)
    {
        StartCoroutine(LevelUpSequence(newLevel));
    }

    private IEnumerator LevelUpSequence(int newLevel)
    {
        // 1. Reset la jauge visuellement à 0 pour montrer un nouveau palier
        if (_fill != null) _fill.fillAmount = 0f;

        // 2. Flash doré sur le fond du widget
        yield return StartCoroutine(FlashGlow());

        // 3. Bounce scale du widget entier
        yield return StartCoroutine(BounceWidget());

        // 4. Mise à jour des labels (niveau, compteur)
        RefreshInstant();

        // 5. Si niveau max : porte jaune
        if (newLevel >= PlayerLevelManager.MaxLevel)
        {
            var door = FindFirstObjectByType<MenuDoor>();
            door?.StartYellowPulse();
        }
    }

    private IEnumerator FlashGlow()
    {
        if (_glowOverlay == null) yield break;

        // Flash in
        float t = 0f;
        while (t < LevelUpFlash * 0.4f)
        {
            t += Time.deltaTime;
            float e = Mathf.Clamp01(t / (LevelUpFlash * 0.4f));
            _glowOverlay.color = new Color(ColLevelUpFlash.r, ColLevelUpFlash.g, ColLevelUpFlash.b,
                                           ColLevelUpFlash.a * e);
            yield return null;
        }

        // Flash out
        t = 0f;
        while (t < LevelUpFlash * 0.6f)
        {
            t += Time.deltaTime;
            float e = 1f - Mathf.Clamp01(t / (LevelUpFlash * 0.6f));
            _glowOverlay.color = new Color(ColLevelUpFlash.r, ColLevelUpFlash.g, ColLevelUpFlash.b,
                                           ColLevelUpFlash.a * e);
            yield return null;
        }

        _glowOverlay.color = new Color(ColGlow.r, ColGlow.g, ColGlow.b, 0f);
    }

    private IEnumerator BounceWidget()
    {
        if (_root == null) yield break;

        // Scale up rapide
        float t = 0f;
        while (t < LevelUpBounce * 0.45f)
        {
            t += Time.deltaTime;
            float e = Mathf.Clamp01(t / (LevelUpBounce * 0.45f));
            _root.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.20f, e);
            yield return null;
        }

        // Scale down élastique
        t = 0f;
        while (t < LevelUpBounce * 0.55f)
        {
            t += Time.deltaTime;
            float e = Mathf.Clamp01(t / (LevelUpBounce * 0.55f));
            float overshoot = Mathf.Sin(e * Mathf.PI) * 0.08f;
            _root.localScale = Vector3.Lerp(Vector3.one * 1.20f, Vector3.one, e) + Vector3.one * overshoot;
            yield return null;
        }

        _root.localScale = Vector3.one;
    }

    // ── Gain XP animé ─────────────────────────────────────────────────────────

    /// <summary>
    /// Anime la jauge vers la nouvelle valeur après un gain d'XP.
    /// Appelé par <see cref="MenuXPReceiver"/> une fois les tokens en vol.
    /// </summary>
    public void AnimateXPGain(int xpAdded)
    {
        StartCoroutine(AnimateFill());
    }

    private IEnumerator AnimateFill()
    {
        var plm      = PlayerLevelManager.Instance;
        bool maxed   = plm.IsMaxLevel;
        float target = maxed ? 1f : plm.XPRatio;
        float start  = _fill != null ? _fill.fillAmount : 0f;

        int xpStart  = Mathf.RoundToInt(start * PlayerLevelManager.XPPerLevel);
        int xpTarget = maxed ? PlayerLevelManager.XPPerLevel : plm.CurrentXP;

        float t = 0f;
        while (t < FillDur)
        {
            t += Time.deltaTime;
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / FillDur), 3f);

            if (_fill != null)
                _fill.fillAmount = Mathf.Lerp(start, target, e);

            if (_counterLbl != null && !maxed)
                _counterLbl.text = $"{Mathf.RoundToInt(Mathf.Lerp(xpStart, xpTarget, e))} / {PlayerLevelManager.XPPerLevel}";

            yield return null;
        }

        RefreshInstant();
    }

    // ── Pulse widget (level-up) ────────────────────────────────────────────────

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
