using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Widget "Porte" du menu principal.
///
/// - Affiche le sprite de porte centré bas-écran.
/// - Verrouillée : sprite cadenas + badge "Atteindre le niveau 4".
/// - Déverrouillée : cadenas disparaît, porte pulse dorée, clic → <see cref="DoorManager"/>.
/// </summary>
public class MenuDoor : MonoBehaviour
{
    // ── Mise en page ──────────────────────────────────────────────────────────

    private const float DoorW       = 620f;
    private const float DoorH       = 900f;
    private const float DoorOffsetY = 120f;

    private const float LockW = 200f;
    private const float LockH = 200f;

    private const float TooltipW    = 780f;
    private const float TooltipH    = 200f;
    private const float TooltipDur  = 2.8f;
    private const float TooltipFade = 0.30f;

    // ── Pulse dorée ───────────────────────────────────────────────────────────

    private const float PulsePeriod  = 1.6f;
    private const float PulseMaxGlow = 0.55f;

    // ── Références runtime ────────────────────────────────────────────────────

    private Image           _doorImage;
    private Image           _lockImage;
    private RectTransform   _tooltipRT;
    private CanvasGroup     _tooltipGroup;
    private bool            _tooltipShowing;
    private bool            _pulsingGold;
    private Coroutine       _pulseCoroutine;

    // ── Sprites ───────────────────────────────────────────────────────────────

    public Sprite DoorSprite { get; set; }

    // ── Init ──────────────────────────────────────────────────────────────────

    /// <summary>Construit la porte dans le <paramref name="canvasRT"/> fourni.</summary>
    public void Init(RectTransform canvasRT)
    {
        var doorRT = BuildDoorImage(canvasRT);
        BuildLockImage(doorRT);
        BuildTooltip(canvasRT);
        RefreshLockVisual();
    }

    // ── Image de porte ────────────────────────────────────────────────────────

    private RectTransform BuildDoorImage(RectTransform parent)
    {
        var go = new GameObject("Door");
        go.transform.SetParent(parent, false);

        _doorImage              = go.AddComponent<Image>();
        _doorImage.sprite       = DoorSprite;
        _doorImage.color        = Color.white;
        _doorImage.preserveAspect = true;
        _doorImage.raycastTarget  = true;

        if (DoorSprite == null)
            _doorImage.color = new Color(1f, 1f, 1f, 0f);

        var rt              = _doorImage.rectTransform;
        rt.anchorMin        = new Vector2(0.5f, 0f);
        rt.anchorMax        = new Vector2(0.5f, 0f);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.sizeDelta        = new Vector2(DoorW, DoorH);
        rt.anchoredPosition = new Vector2(0f, DoorOffsetY);

        var btn                 = go.AddComponent<Button>();
        btn.targetGraphic       = _doorImage;
        var colors              = btn.colors;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.85f);
        colors.pressedColor     = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.fadeDuration     = 0.08f;
        btn.colors              = colors;
        btn.onClick.AddListener(OnClick);

        return rt;
    }

    // ── Cadenas ───────────────────────────────────────────────────────────────

    private void BuildLockImage(RectTransform doorRT)
    {
        var go  = new GameObject("LockImage");
        go.transform.SetParent(doorRT, false);

        _lockImage                = go.AddComponent<Image>();
        _lockImage.sprite         = MenuAssets.LockSprite;
        _lockImage.color          = Color.white;
        _lockImage.preserveAspect = true;
        _lockImage.raycastTarget  = false;

        if (MenuAssets.LockSprite == null)
            _lockImage.color = new Color(1f, 1f, 1f, 0f);

        var rt        = _lockImage.rectTransform;
        rt.anchorMin  = new Vector2(0.5f, 0.62f);
        rt.anchorMax  = new Vector2(0.5f, 0.62f);
        rt.pivot      = new Vector2(0.5f, 0.5f);
        rt.sizeDelta  = new Vector2(LockW, LockH);
        rt.anchoredPosition = Vector2.zero;
    }

    // ── Tooltip ───────────────────────────────────────────────────────────────

    private void BuildTooltip(RectTransform canvasRT)
    {
        var go  = new GameObject("DoorTooltip");
        go.transform.SetParent(canvasRT, false);

        var rt          = go.AddComponent<RectTransform>();
        rt.anchorMin    = new Vector2(0.5f, 0f);
        rt.anchorMax    = new Vector2(0.5f, 0f);
        rt.pivot        = new Vector2(0.5f, 0f);
        rt.sizeDelta    = new Vector2(TooltipW, TooltipH);
        rt.anchoredPosition = new Vector2(0f, DoorOffsetY + DoorH + 24f);

        _tooltipGroup               = go.AddComponent<CanvasGroup>();
        _tooltipGroup.alpha         = 0f;
        _tooltipGroup.blocksRaycasts = false;
        _tooltipGroup.interactable  = false;

        var bg          = go.AddComponent<Image>();
        bg.sprite       = SpriteGenerator.CreateWhiteSquare();
        bg.color        = new Color(0.06f, 0.04f, 0.10f, 0.92f);
        bg.raycastTarget = false;

        var txtGO  = new GameObject("TooltipText");
        txtGO.transform.SetParent(go.transform, false);
        var tmp    = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text   = $"Atteins le niveau {DoorManager.UnlockLevel} pour ouvrir la porte";
        tmp.fontSize    = 32f;
        tmp.fontStyle   = FontStyles.Bold;
        tmp.color       = Color.white;
        tmp.alignment   = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        MenuAssets.ApplyFont(tmp);

        var trt       = tmp.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(24f, 16f);
        trt.offsetMax = new Vector2(-24f, -16f);

        _tooltipRT = rt;
    }

    // ── Rafraîchissement visuel ───────────────────────────────────────────────

    /// <summary>
    /// Affiche/masque le cadenas.
    /// Déclenche la pulse dorée si la porte vient d'être déverrouillée.
    /// </summary>
    public void RefreshLockVisual()
    {
        bool locked = DoorManager.Instance == null || !DoorManager.Instance.IsUnlocked;

        if (_lockImage != null) _lockImage.gameObject.SetActive(locked);

        if (!locked && !_pulsingGold)
            StartGoldPulse();
    }

    // ── Pulse dorée ───────────────────────────────────────────────────────────

    /// <summary>Démarre la pulsation dorée infinie sur le sprite de la porte.</summary>
    public void StartGoldPulse()
    {
        if (_pulsingGold) return;
        _pulsingGold = true;
        if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
        _pulseCoroutine = StartCoroutine(GoldPulseLoop());
    }

    private IEnumerator GoldPulseLoop()
    {
        float t = 0f;
        while (_pulsingGold)
        {
            t += Time.deltaTime;
            float phase = Mathf.PingPong(t / PulsePeriod, 1f);
            float glow  = Mathf.SmoothStep(0f, PulseMaxGlow, phase);

            if (_doorImage != null)
                _doorImage.color = new Color(1f, 1f - glow * 0.35f, 1f - glow, 1f);

            yield return null;
        }

        if (_doorImage != null)
            _doorImage.color = Color.white;
    }

    // ── Clic ──────────────────────────────────────────────────────────────────

    private void OnClick()
    {
        if (DoorManager.Instance == null) return;

        DoorManager.Instance.EvaluateUnlock();
        RefreshLockVisual();

        if (!DoorManager.Instance.IsUnlocked)
        {
            StopAllCoroutines();
            StartCoroutine(ShowTooltip());
            return;
        }

        DoorManager.Instance.OnDoorClicked();
    }

    // ── Tooltip coroutine ─────────────────────────────────────────────────────

    private IEnumerator ShowTooltip()
    {
        if (_tooltipGroup == null) yield break;
        _tooltipShowing = true;

        yield return StartCoroutine(FadeTooltip(0f, 1f, TooltipFade));
        yield return new WaitForSeconds(TooltipDur);
        yield return StartCoroutine(FadeTooltip(1f, 0f, TooltipFade));

        _tooltipShowing = false;
    }

    private IEnumerator FadeTooltip(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (_tooltipGroup != null)
                _tooltipGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        if (_tooltipGroup != null)
            _tooltipGroup.alpha = to;
    }
}
