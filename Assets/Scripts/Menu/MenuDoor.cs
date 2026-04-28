using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Widget "Porte" du menu principal.
///
/// - Affiche un rectangle blanc fluorescent centré bas-écran avec un halo mystique rayonnant.
/// - Verrouillée : sprite cadenas + badge "Une fois chacun des jeux monté au niveau 4, la porte se déverrouillera".
/// - Déverrouillée : cadenas disparaît, porte pulse blanc intense, clic → <see cref="DoorManager"/> → ParryGame.
/// </summary>
public class MenuDoor : MonoBehaviour
{
    // ── Mise en page ──────────────────────────────────────────────────────────

    private const float DoorW       = 620f;
    private const float DoorH       = 900f;
    private const float DoorOffsetY = 380f;  // remonté pour exposer les halos lumineux

    private const float LockW = 200f;
    private const float LockH = 200f;

    private const float TooltipW    = 780f;
    private const float TooltipH    = 200f;
    private const float TooltipDur  = 2.8f;
    private const float TooltipFade = 0.30f;

    // ── Pulse mystique ────────────────────────────────────────────────────────

    private const float PulsePeriod  = 2.2f;
    private const float PulseMinAlpha = 0.88f;
    private const float PulseMaxAlpha = 1.0f;

    // ── Halo ──────────────────────────────────────────────────────────────────

    private const int HaloLayerCount = 4;

    // ── Références runtime ────────────────────────────────────────────────────

    private Image           _doorImage;
    private Image           _lockImage;
    private RectTransform   _tooltipRT;
    private CanvasGroup     _tooltipGroup;
    private bool            _tooltipShowing;
    private bool            _pulsingGlow;
    private Coroutine       _pulseCoroutine;
    private Image[]         _haloImages;

    // ── Sprites ───────────────────────────────────────────────────────────────

    public Sprite DoorSprite { get; set; }

    // ── Init ──────────────────────────────────────────────────────────────────

    /// <summary>Construit la porte dans le <paramref name="canvasRT"/> fourni.</summary>
    public void Init(RectTransform canvasRT)
    {
        BuildHaloRays(canvasRT);
        var doorRT = BuildDoorImage(canvasRT);
        BuildGlowLayers(canvasRT, doorRT);
        BuildLockImage(doorRT);
        BuildTooltip(canvasRT);
        RefreshLockVisual();
    }

    // ── Rayons de halo (fond) ─────────────────────────────────────────────────

    private void BuildHaloRays(RectTransform parent)
    {
        // Couches de halo soft empilées pour simuler les rayons mystiques
        _haloImages = new Image[HaloLayerCount];

        float[] scales  = { 1.9f, 1.5f, 1.2f, 1.05f };
        float[] alphas  = { 0.07f, 0.10f, 0.13f, 0.18f };

        for (int i = 0; i < HaloLayerCount; i++)
        {
            var go  = new GameObject($"DoorHalo_{i}");
            go.transform.SetParent(parent, false);

            var img  = go.AddComponent<Image>();
            img.sprite      = CreateRadialGlowSprite();
            img.color       = new Color(1f, 1f, 1f, alphas[i]);
            img.raycastTarget = false;

            float w = DoorW * scales[i];
            float h = DoorH * scales[i] * 1.4f;

            var rt              = img.rectTransform;
            rt.anchorMin        = new Vector2(0.5f, 0f);
            rt.anchorMax        = new Vector2(0.5f, 0f);
            rt.pivot            = new Vector2(0.5f, 0f);
            rt.sizeDelta        = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(0f, DoorOffsetY - h * 0.15f);

            _haloImages[i] = img;
        }
    }

    // ── Couches de lueur autour de la porte ───────────────────────────────────

    private static void BuildGlowLayers(RectTransform parent, RectTransform doorRT)
    {
        float[] expansions = { 80f, 40f, 18f };
        float[] alphas     = { 0.12f, 0.22f, 0.35f };

        foreach (var (exp, alpha) in ZipArrays(expansions, alphas))
        {
            var go  = new GameObject("DoorGlow");
            go.transform.SetParent(parent, false);

            var img  = go.AddComponent<Image>();
            img.sprite       = SpriteGenerator.CreateWhiteSquare();
            img.color        = new Color(1f, 1f, 1f, alpha);
            img.raycastTarget = false;

            var rt              = img.rectTransform;
            rt.anchorMin        = new Vector2(0.5f, 0f);
            rt.anchorMax        = new Vector2(0.5f, 0f);
            rt.pivot            = new Vector2(0.5f, 0f);
            rt.sizeDelta        = new Vector2(DoorW + exp * 2f, DoorH + exp);
            rt.anchoredPosition = new Vector2(0f, DoorOffsetY - exp * 0.5f);
        }
    }

    // ── Image de porte ────────────────────────────────────────────────────────

    private RectTransform BuildDoorImage(RectTransform parent)
    {
        var go = new GameObject("Door");
        go.transform.SetParent(parent, false);

        _doorImage              = go.AddComponent<Image>();
        _doorImage.sprite       = SpriteGenerator.CreateWhiteSquare();
        _doorImage.color        = Color.white;
        _doorImage.raycastTarget  = true;

        var rt              = _doorImage.rectTransform;
        rt.anchorMin        = new Vector2(0.5f, 0f);
        rt.anchorMax        = new Vector2(0.5f, 0f);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.sizeDelta        = new Vector2(DoorW, DoorH);
        rt.anchoredPosition = new Vector2(0f, DoorOffsetY);

        var btn                 = go.AddComponent<Button>();
        btn.targetGraphic       = _doorImage;
        var colors              = btn.colors;
        colors.highlightedColor = new Color(0.9f, 0.9f, 1f, 1f);
        colors.pressedColor     = new Color(0.7f, 0.7f, 0.85f, 1f);
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
        _lockImage.color          = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        _lockImage.preserveAspect = true;
        _lockImage.raycastTarget  = false;

        if (MenuAssets.LockSprite == null)
            _lockImage.color = new Color(0f, 0f, 0f, 0f);

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
        tmp.text   = $"Une fois chacun des jeux monté au niveau {DoorManager.UnlockLevel}, la porte se déverrouillera";
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
    /// Déclenche la pulse lumineuse si la porte vient d'être déverrouillée.
    /// </summary>
    public void RefreshLockVisual()
    {
        bool locked = DoorManager.Instance == null || !DoorManager.Instance.IsUnlocked;

        if (_lockImage != null) _lockImage.gameObject.SetActive(locked);

        if (!locked && !_pulsingGlow)
            StartGlowPulse();
    }

    // ── Pulse lumineuse mystique ───────────────────────────────────────────────

    // ── Pulse jaune (niveau 4 atteint) ───────────────────────────────────────

    /// <summary>
    /// Remplace le pulse blanc mystique par un pulse jaune vif indiquant que
    /// le Parry Game est accessible. Appelé par <see cref="MenuXPBar"/> au niveau 4.
    /// </summary>
    public void StartYellowPulse()
    {
        // Stoppe le pulse blanc si actif
        if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
        _pulsingGlow = true;
        _pulseCoroutine = StartCoroutine(YellowPulseLoop());
    }

    private IEnumerator YellowPulseLoop()
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime;
            float phase = Mathf.PingPong(t / 1.4f, 1f);
            float alpha = Mathf.SmoothStep(0.80f, 1.0f, phase);

            Color yellow = new Color(1f, 0.85f + phase * 0.10f, 0.0f, alpha);
            if (_doorImage != null) _doorImage.color = yellow;

            // Halos teintés en jaune
            if (_haloImages != null)
            {
                for (int i = 0; i < _haloImages.Length; i++)
                {
                    float hp  = Mathf.PingPong((t + i * 0.25f) / 1.4f, 1f);
                    float ha  = 0.08f + hp * 0.14f;
                    if (_haloImages[i] != null)
                        _haloImages[i].color = new Color(1f, 0.88f, 0.0f, ha);
                }
            }

            yield return null;
        }
    }

    /// <summary>Démarre la pulsation blanche fluorescente infinie sur la porte.</summary>
    public void StartGlowPulse()
    {
        if (_pulsingGlow) return;
        _pulsingGlow = true;
        if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
        _pulseCoroutine = StartCoroutine(GlowPulseLoop());
    }

    private IEnumerator GlowPulseLoop()
    {
        float t = 0f;
        while (_pulsingGlow)
        {
            t += Time.deltaTime;
            float phase = Mathf.PingPong(t / PulsePeriod, 1f);
            float alpha = Mathf.SmoothStep(PulseMinAlpha, PulseMaxAlpha, phase);

            if (_doorImage != null)
                _doorImage.color = new Color(1f, 1f, 1f, alpha);

            // Halo qui pulse légèrement en décalage
            if (_haloImages != null)
            {
                for (int i = 0; i < _haloImages.Length; i++)
                {
                    float haloPhase = Mathf.PingPong((t + i * 0.3f) / PulsePeriod, 1f);
                    float haloBase  = 0.05f + i * 0.04f;
                    if (_haloImages[i] != null)
                    {
                        var c   = _haloImages[i].color;
                        c.a     = Mathf.SmoothStep(haloBase, haloBase + 0.10f, haloPhase);
                        _haloImages[i].color = c;
                    }
                }
            }

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

    // ── Génération sprite halo radial ─────────────────────────────────────────

    private static Sprite _radialGlowSprite;

    /// <summary>
    /// Génère un sprite de lueur radiale douce blanc → transparent.
    /// Simule des rayons de lumière émanant du bas.
    /// </summary>
    private static Sprite CreateRadialGlowSprite()
    {
        if (_radialGlowSprite != null) return _radialGlowSprite;

        const int size = 256;
        var tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var pixels = new Color[size * size];

        float cx = size * 0.5f;
        float cy = 0f;   // Centre en bas

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx   = x + 0.5f - cx;
            float dy   = y + 0.5f - cy;
            float dist = Mathf.Sqrt(dx * dx + dy * dy) / size;

            // Atténuation radiale
            float alpha = Mathf.Clamp01(1f - dist * 1.8f);
            alpha = alpha * alpha;  // falloff quadratique plus doux

            // Ajout de rayons angulaires simulés via bruit directionnel
            float angle = Mathf.Atan2(dy, Mathf.Abs(dx));
            float rays  = Mathf.Abs(Mathf.Sin(angle * 7f)) * 0.4f + 0.6f;
            alpha *= rays;

            pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
        }

        tex.SetPixels(pixels);
        tex.Apply(false, true);

        _radialGlowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0f), size);
        return _radialGlowSprite;
    }

    // ── Helper zip ────────────────────────────────────────────────────────────

    private static System.Collections.Generic.IEnumerable<(float, float)> ZipArrays(float[] a, float[] b)
    {
        int len = Mathf.Min(a.Length, b.Length);
        for (int i = 0; i < len; i++)
            yield return (a[i], b[i]);
    }
}
