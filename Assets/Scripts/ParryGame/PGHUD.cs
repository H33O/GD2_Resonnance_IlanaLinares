using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Builds and drives the Parry Game HUD:
/// - Top: score and combo
/// - Bottom: three action tabs — Défense / Armes / Soins (fully functional)
/// - Hearts: HP display
/// - Strong screen-flash feedback: green heal, cyan shield, red hit
/// - Camera shake on hit and on abilities
/// - Double-strike burst and weapon-grey feedback
/// </summary>
public class PGHUD : MonoBehaviour
{
    // ── Constants ─────────────────────────────────────────────────────────────

    private const float CanvasRefW    = 1080f;
    private const float CanvasRefH    = 1920f;

    // Bottom bar
    private const float TabBarH       = 220f;
    private const float TabBarPadding = 24f;

    // Hearts
    private const float HeartSize     = 80f;
    private const float HeartSpacing  = 20f;

    // Score pop
    private const float PopPeakScale  = 1.35f;
    private const float PopDuration   = 0.18f;

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Settings")]
    public PGSettings settings;

    // ── Internal references ───────────────────────────────────────────────────

    private TextMeshProUGUI scoreLabel;
    private TextMeshProUGUI comboLabel;
    private Image[]         heartImages;
    private int             maxHp;

    private Coroutine scorePopCoroutine;
    private Coroutine comboPopCoroutine;

    // Per-tab state
    private Image[]         tabBgImages;     // [0]=Defense [1]=Weapon [2]=Heal
    private Image[]         cooldownFill;    // fill bar under each tab
    private TextMeshProUGUI[] cooldownLabel; // "45s" countdown text
    private bool[]          tabReady;

    // Feedback overlays
    private Image _healFlashOverlay;
    private Image _shieldBlockOverlay;
    private Image _hitFlashOverlay;

    // Camera shake
    private Coroutine _shakeCoroutine;
    private Vector3   _camOrigin;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        PGGameManager.OnScoreChanged  += HandleScore;
        PGGameManager.OnComboChanged  += HandleCombo;
        PGGameManager.OnHpChanged     += HandleHp;
        PGGameManager.OnHpRestored    += HandleHpRestored;
        PGGameManager.OnGameOver      += HandleGameOver;
        PGGameManager.OnShieldBlocked += HandleShieldBlocked;
        PGGameManager.OnParryFail     += HandleHit;

        PGAbilitySystem.OnAbilityUsed     += HandleAbilityUsed;
        PGAbilitySystem.OnAbilityReady    += HandleAbilityReady;
        PGAbilitySystem.OnCooldownProgress += HandleCooldownProgress;
    }

    private void OnDisable()
    {
        PGGameManager.OnScoreChanged  -= HandleScore;
        PGGameManager.OnComboChanged  -= HandleCombo;
        PGGameManager.OnHpChanged     -= HandleHp;
        PGGameManager.OnHpRestored    -= HandleHpRestored;
        PGGameManager.OnGameOver      -= HandleGameOver;
        PGGameManager.OnShieldBlocked -= HandleShieldBlocked;
        PGGameManager.OnParryFail     -= HandleHit;

        PGAbilitySystem.OnAbilityUsed     -= HandleAbilityUsed;
        PGAbilitySystem.OnAbilityReady    -= HandleAbilityReady;
        PGAbilitySystem.OnCooldownProgress -= HandleCooldownProgress;
    }

    // ── Initialisation ────────────────────────────────────────────────────────

    /// <summary>Builds the full HUD inside the provided canvas RectTransform.</summary>
    public void Init(RectTransform canvasRT)
    {
        maxHp = settings != null ? settings.maxHp : 3;

        if (Camera.main != null) _camOrigin = Camera.main.transform.position;

        BuildTopBar(canvasRT);
        BuildHearts(canvasRT);
        BuildBottomTabBar(canvasRT);
        BuildHealFlashOverlay(canvasRT);
        BuildShieldBlockOverlay(canvasRT);
        BuildHitFlashOverlay(canvasRT);
    }

    // ── Top bar (score + combo) ───────────────────────────────────────────────

    private void BuildTopBar(RectTransform canvasRT)
    {
        var root = MakeRT("TopBar", canvasRT);
        root.anchorMin       = new Vector2(0f, 1f);
        root.anchorMax       = new Vector2(1f, 1f);
        root.pivot           = new Vector2(0.5f, 1f);
        root.sizeDelta       = new Vector2(0f, 160f);
        root.anchoredPosition = Vector2.zero;

        var bg    = root.gameObject.AddComponent<Image>();
        bg.color  = new Color(0f, 0f, 0f, 0.55f);
        bg.raycastTarget = false;

        var scoreGO = new GameObject("Score");
        scoreGO.transform.SetParent(root, false);
        scoreLabel = scoreGO.AddComponent<TextMeshProUGUI>();
        scoreLabel.text      = "0";
        scoreLabel.fontSize  = 72f;
        scoreLabel.fontStyle = FontStyles.Bold;
        scoreLabel.color     = Color.white;
        scoreLabel.alignment = TextAlignmentOptions.Center;
        scoreLabel.raycastTarget = false;
        MenuAssets.ApplyFont(scoreLabel);
        var sRT        = scoreLabel.rectTransform;
        sRT.anchorMin  = new Vector2(0.2f, 0f);
        sRT.anchorMax  = new Vector2(0.8f, 1f);
        sRT.offsetMin  = sRT.offsetMax = Vector2.zero;

        var comboGO = new GameObject("Combo");
        comboGO.transform.SetParent(root, false);
        comboLabel = comboGO.AddComponent<TextMeshProUGUI>();
        comboLabel.text      = "";
        comboLabel.fontSize  = 40f;
        comboLabel.fontStyle = FontStyles.Bold;
        comboLabel.color     = new Color(1f, 0.85f, 0.2f, 1f);
        comboLabel.alignment = TextAlignmentOptions.Center;
        comboLabel.raycastTarget = false;
        MenuAssets.ApplyFont(comboLabel);
        comboGO.SetActive(false);
        var cRT        = comboLabel.rectTransform;
        cRT.anchorMin  = new Vector2(0.6f, 0f);
        cRT.anchorMax  = new Vector2(1f, 1f);
        cRT.offsetMin  = cRT.offsetMax = Vector2.zero;
    }

    // ── Hearts ────────────────────────────────────────────────────────────────

    private void BuildHearts(RectTransform canvasRT)
    {
        var root = MakeRT("Hearts", canvasRT);
        root.anchorMin        = new Vector2(0f, 1f);
        root.anchorMax        = new Vector2(0f, 1f);
        root.pivot            = new Vector2(0f, 1f);
        root.sizeDelta        = new Vector2((HeartSize + HeartSpacing) * maxHp, HeartSize);
        root.anchoredPosition = new Vector2(32f, -170f);

        heartImages = new Image[maxHp];
        for (int i = 0; i < maxHp; i++)
        {
            var hGO = new GameObject($"Heart_{i}");
            hGO.transform.SetParent(root, false);
            var img = hGO.AddComponent<Image>();
            img.color         = new Color(0.95f, 0.25f, 0.25f, 1f);
            img.raycastTarget = false;
            img.sprite        = SpriteGenerator.CreateCircle(64);
            var rt            = img.rectTransform;
            rt.anchorMin      = new Vector2(0f, 0f);
            rt.anchorMax      = new Vector2(0f, 0f);
            rt.pivot          = new Vector2(0f, 0f);
            rt.sizeDelta      = new Vector2(HeartSize, HeartSize);
            rt.anchoredPosition = new Vector2(i * (HeartSize + HeartSpacing), 0f);
            heartImages[i]    = img;
        }
    }

    // ── Bottom tab bar (Défense / Armes / Soins) ──────────────────────────────

    private void BuildBottomTabBar(RectTransform canvasRT)
    {
        tabBgImages    = new Image[3];
        cooldownFill   = new Image[3];
        cooldownLabel  = new TextMeshProUGUI[3];
        tabReady       = new bool[] { true, true, true };

        var root = MakeRT("BottomTabBar", canvasRT);
        root.anchorMin        = new Vector2(0f, 0f);
        root.anchorMax        = new Vector2(1f, 0f);
        root.pivot            = new Vector2(0.5f, 0f);
        root.sizeDelta        = new Vector2(0f, TabBarH);
        root.anchoredPosition = Vector2.zero;

        var bg = root.gameObject.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.08f, 0.92f);
        bg.raycastTarget = false;

        string[] labels = { "DÉFENSE", "ARMES", "SOINS" };
        string[] icons  = { "🛡", "⚔", "♥" };
        Color[]  colors =
        {
            settings != null ? settings.colorDefense : new Color(0.30f, 0.55f, 1f, 1f),
            settings != null ? settings.colorWeapons : new Color(1f, 0.38f, 0.28f, 1f),
            settings != null ? settings.colorHeals   : new Color(0.25f, 0.85f, 0.45f, 1f),
        };

        float tabW = (CanvasRefW - TabBarPadding * 4f) / 3f;
        float tabH = TabBarH - TabBarPadding * 2f;

        for (int i = 0; i < labels.Length; i++)
        {
            int captured = i; // capture for lambda
            BuildTab(root, labels[i], icons[i], colors[i], i, tabW, tabH,
                     () => OnTabPressed(captured));
        }
    }

    private void BuildTab(RectTransform parent, string label, string icon,
                          Color accentColor, int index, float tabW, float tabH,
                          UnityEngine.Events.UnityAction onClick)
    {
        var tabGO = new GameObject($"Tab_{label}");
        tabGO.transform.SetParent(parent, false);

        // Background
        var img   = tabGO.AddComponent<Image>();
        img.sprite = SpriteGenerator.CreateWhiteSquare();
        img.color  = new Color(accentColor.r * 0.18f, accentColor.g * 0.18f, accentColor.b * 0.18f, 1f);
        tabBgImages[index] = img;

        var rt          = img.rectTransform;
        rt.anchorMin    = new Vector2(0f, 0f);
        rt.anchorMax    = new Vector2(0f, 0f);
        rt.pivot        = new Vector2(0f, 0f);
        rt.sizeDelta    = new Vector2(tabW, tabH);
        rt.anchoredPosition = new Vector2(TabBarPadding + index * (tabW + TabBarPadding), TabBarPadding);

        // Accent top border
        var borderGO  = new GameObject("Border");
        borderGO.transform.SetParent(rt, false);
        var bImg      = borderGO.AddComponent<Image>();
        bImg.color    = accentColor;
        bImg.raycastTarget = false;
        var bRT       = bImg.rectTransform;
        bRT.anchorMin = new Vector2(0f, 1f);
        bRT.anchorMax = new Vector2(1f, 1f);
        bRT.pivot     = new Vector2(0.5f, 1f);
        bRT.sizeDelta = new Vector2(0f, 6f);
        bRT.anchoredPosition = Vector2.zero;

        // Icon (emoji)
        var iGO  = new GameObject("Icon");
        iGO.transform.SetParent(rt, false);
        var iTmp = iGO.AddComponent<TextMeshProUGUI>();
        iTmp.text      = icon;
        iTmp.fontSize  = 52f;
        iTmp.alignment = TextAlignmentOptions.Center;
        iTmp.raycastTarget = false;
        var iRT        = iTmp.rectTransform;
        iRT.anchorMin  = new Vector2(0f, 0.40f);
        iRT.anchorMax  = new Vector2(1f, 1f);
        iRT.offsetMin  = iRT.offsetMax = Vector2.zero;

        // Label
        var lblGO = new GameObject("Label");
        lblGO.transform.SetParent(rt, false);
        var tmp   = lblGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 26f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = new Color(accentColor.r, accentColor.g, accentColor.b, 1f);
        tmp.alignment = TextAlignmentOptions.Bottom | TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        MenuAssets.ApplyFont(tmp);
        var lRT       = tmp.rectTransform;
        lRT.anchorMin = new Vector2(0f, 0f);
        lRT.anchorMax = new Vector2(1f, 0.42f);
        lRT.offsetMin = new Vector2(4f, 4f);
        lRT.offsetMax = new Vector2(-4f, 0f);

        // Cooldown fill bar (grows left→right as cooldown progresses)
        var fillBgGO = new GameObject("CooldownBg");
        fillBgGO.transform.SetParent(rt, false);
        var fillBgImg = fillBgGO.AddComponent<Image>();
        fillBgImg.sprite = SpriteGenerator.CreateWhiteSquare();
        fillBgImg.color  = new Color(0f, 0f, 0f, 0.45f);
        fillBgImg.raycastTarget = false;
        var fillBgRT    = fillBgImg.rectTransform;
        fillBgRT.anchorMin = new Vector2(0f, 0f);
        fillBgRT.anchorMax = new Vector2(1f, 0f);
        fillBgRT.pivot     = new Vector2(0f, 0f);
        fillBgRT.sizeDelta = new Vector2(0f, 8f);
        fillBgRT.anchoredPosition = Vector2.zero;

        var fillGO = new GameObject("CooldownFill");
        fillGO.transform.SetParent(rt, false);
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.sprite = SpriteGenerator.CreateWhiteSquare();
        fillImg.color  = accentColor;
        fillImg.type   = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 1f; // starts full (ready)
        fillImg.raycastTarget = false;
        var fillRT     = fillImg.rectTransform;
        fillRT.anchorMin = new Vector2(0f, 0f);
        fillRT.anchorMax = new Vector2(1f, 0f);
        fillRT.pivot     = new Vector2(0f, 0f);
        fillRT.sizeDelta = new Vector2(0f, 8f);
        fillRT.anchoredPosition = Vector2.zero;
        cooldownFill[index] = fillImg;

        // Cooldown text (hidden while ready)
        var cdGO = new GameObject("CooldownTxt");
        cdGO.transform.SetParent(rt, false);
        var cdTmp = cdGO.AddComponent<TextMeshProUGUI>();
        cdTmp.text      = "";
        cdTmp.fontSize  = 30f;
        cdTmp.fontStyle = FontStyles.Bold;
        cdTmp.color     = new Color(1f, 1f, 1f, 0.70f);
        cdTmp.alignment = TextAlignmentOptions.Center;
        cdTmp.raycastTarget = false;
        MenuAssets.ApplyFont(cdTmp);
        cdGO.SetActive(false);
        var cdRT        = cdTmp.rectTransform;
        cdRT.anchorMin  = Vector2.zero;
        cdRT.anchorMax  = Vector2.one;
        cdRT.offsetMin  = cdRT.offsetMax = Vector2.zero;
        cooldownLabel[index] = cdTmp;

        // Clickable button
        var btn  = tabGO.AddComponent<Button>();
        btn.targetGraphic = img;
        var co   = btn.colors;
        co.normalColor      = Color.white;
        co.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
        co.pressedColor     = new Color(accentColor.r, accentColor.g, accentColor.b, 0.6f);
        co.disabledColor    = new Color(0.35f, 0.35f, 0.35f, 0.6f);
        co.fadeDuration     = 0.08f;
        btn.colors          = co;
        btn.onClick.AddListener(onClick);
    }

    // ── Feedback overlays ─────────────────────────────────────────────────────

    /// <summary>Full-screen green flash shown on heal.</summary>
    private void BuildHealFlashOverlay(RectTransform canvasRT)
    {
        var go  = new GameObject("HealFlash");
        go.transform.SetParent(canvasRT, false);
        _healFlashOverlay = go.AddComponent<Image>();
        _healFlashOverlay.color = new Color(0.20f, 1f, 0.40f, 0f);
        _healFlashOverlay.raycastTarget = false;
        var rt  = _healFlashOverlay.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.SetActive(false);
    }

    /// <summary>Full-screen cyan flash shown on shield block.</summary>
    private void BuildShieldBlockOverlay(RectTransform canvasRT)
    {
        var go  = new GameObject("ShieldBlockFlash");
        go.transform.SetParent(canvasRT, false);
        _shieldBlockOverlay = go.AddComponent<Image>();
        _shieldBlockOverlay.color = new Color(0.40f, 0.85f, 1f, 0f);
        _shieldBlockOverlay.raycastTarget = false;
        var rt  = _shieldBlockOverlay.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.SetActive(false);
    }

    /// <summary>Full-screen red flash shown on player hit.</summary>
    private void BuildHitFlashOverlay(RectTransform canvasRT)
    {
        var go  = new GameObject("HitFlash");
        go.transform.SetParent(canvasRT, false);
        _hitFlashOverlay = go.AddComponent<Image>();
        _hitFlashOverlay.color = new Color(1f, 0.05f, 0.05f, 0f);
        _hitFlashOverlay.raycastTarget = false;
        var rt  = _hitFlashOverlay.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.SetActive(false);
    }

    // ── Tab press dispatch ────────────────────────────────────────────────────

    private void OnTabPressed(int index)
    {
        if (!tabReady[index]) return;
        var ability = PGAbilitySystem.Instance;
        if (ability == null) return;

        switch (index)
        {
            case 0: ability.UseShield(); break;
            case 1: ability.UseWeapon(); break;
            case 2: ability.UseHeal();   break;
        }

        // Haptic pop on the tab icon
        StartCoroutine(TabPressEffect(index));
    }

    // ── Ability event handlers ────────────────────────────────────────────────

    private void HandleAbilityUsed(PGAbilitySystem.AbilityType type)
    {
        int idx = AbilityIndex(type);
        tabReady[idx] = false;
        SetTabGreyed(idx, true);
        if (cooldownLabel[idx] != null) cooldownLabel[idx].gameObject.SetActive(true);
        if (cooldownFill[idx]  != null) cooldownFill[idx].fillAmount = 0f;
    }

    private void HandleAbilityReady(PGAbilitySystem.AbilityType type)
    {
        int idx = AbilityIndex(type);
        tabReady[idx] = true;
        SetTabGreyed(idx, false);
        if (cooldownLabel[idx] != null) { cooldownLabel[idx].text = ""; cooldownLabel[idx].gameObject.SetActive(false); }
        if (cooldownFill[idx]  != null) cooldownFill[idx].fillAmount = 1f;
        StartCoroutine(TabReadyFlash(idx));
    }

    private void HandleCooldownProgress(PGAbilitySystem.AbilityType type, float progress)
    {
        int idx = AbilityIndex(type);
        if (cooldownFill[idx]  != null) cooldownFill[idx].fillAmount  = progress;
        if (cooldownLabel[idx] != null)
        {
            float total = type switch
            {
                PGAbilitySystem.AbilityType.Heal   => settings != null ? settings.healCooldown   : 45f,
                PGAbilitySystem.AbilityType.Weapon => settings != null ? settings.weaponCooldown : 20f,
                _                                  => settings != null ? settings.shieldCooldown :  8f,
            };
            float remaining = Mathf.Ceil(total * (1f - progress));
            cooldownLabel[idx].text = $"{remaining:0}s";
        }
    }

    // ── Game event handlers ───────────────────────────────────────────────────

    private void HandleScore(int score)
    {
        if (scoreLabel == null) return;
        scoreLabel.text = score.ToString();
        if (scorePopCoroutine != null) StopCoroutine(scorePopCoroutine);
        scorePopCoroutine = StartCoroutine(PopRoutine(scoreLabel.transform, PopPeakScale, PopDuration));
    }

    private void HandleCombo(int combo)
    {
        if (comboLabel == null) return;
        if (combo <= 1) { comboLabel.gameObject.SetActive(false); return; }
        comboLabel.gameObject.SetActive(true);
        comboLabel.text = $"x{combo} combo";
        if (comboPopCoroutine != null) StopCoroutine(comboPopCoroutine);
        comboPopCoroutine = StartCoroutine(PopRoutine(comboLabel.transform, 1.25f, PopDuration));
    }

    private void HandleHp(int hp)
    {
        if (heartImages == null) return;
        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] == null) continue;
            bool alive = i < hp;
            heartImages[i].color = alive
                ? new Color(0.95f, 0.25f, 0.25f, 1f)
                : new Color(0.3f, 0.3f, 0.3f, 0.4f);
        }
    }

    private void HandleHpRestored(int hp)
    {
        HandleHp(hp);
        // Strong green flash + big heart pop + camera shake
        StartCoroutine(ScreenFlash(_healFlashOverlay, new Color(0.10f, 1f, 0.35f, 0.72f), 0.70f));
        StartCoroutine(CameraShake(0.18f, 0.12f));

        if (heartImages != null)
            for (int i = 0; i < hp && i < heartImages.Length; i++)
                StartCoroutine(PopRoutine(heartImages[i].transform, 1.8f, 0.30f));
    }

    private void HandleShieldBlocked()
    {
        // Strong cyan flash + camera shake
        StartCoroutine(ScreenFlash(_shieldBlockOverlay, new Color(0.40f, 0.85f, 1f, 0.80f), 0.45f));
        StartCoroutine(CameraShake(0.20f, 0.14f));
    }

    private void HandleHit()
    {
        // Red vignette flash + strong camera shake
        StartCoroutine(ScreenFlash(_hitFlashOverlay, new Color(1f, 0.05f, 0.05f, 0.70f), 0.50f));
        StartCoroutine(CameraShake(0.32f, 0.22f));

        // Shake active hearts
        if (heartImages != null)
            for (int i = 0; i < heartImages.Length; i++)
                if (heartImages[i] != null && heartImages[i].color.a > 0.5f)
                    StartCoroutine(PopRoutine(heartImages[i].transform, 1.4f, 0.20f));
    }

    private void HandleGameOver()
    {
        if (comboLabel != null) comboLabel.gameObject.SetActive(false);
    }

    // ── Visual helpers ────────────────────────────────────────────────────────

    private void SetTabGreyed(int idx, bool greyed)
    {
        if (tabBgImages[idx] == null) return;
        var btn = tabBgImages[idx].GetComponent<Button>();
        if (btn != null) btn.interactable = !greyed;

        // Dim the background
        var c = tabBgImages[idx].color;
        tabBgImages[idx].color = greyed
            ? new Color(c.r * 0.3f, c.g * 0.3f, c.b * 0.3f, 1f)
            : new Color(c.r / 0.3f, c.g / 0.3f, c.b / 0.3f, 1f);
    }

    /// <summary>Brief bright flash when an ability comes off cooldown.</summary>
    private IEnumerator TabReadyFlash(int idx)
    {
        if (tabBgImages[idx] == null) yield break;
        var original = tabBgImages[idx].color;
        tabBgImages[idx].color = Color.white;
        yield return new WaitForSeconds(0.08f);
        tabBgImages[idx].color = original;
        yield return new WaitForSeconds(0.06f);
        tabBgImages[idx].color = Color.white;
        yield return new WaitForSeconds(0.06f);
        tabBgImages[idx].color = original;
    }

    /// <summary>Scale-pop on the tab icon when pressed.</summary>
    private IEnumerator TabPressEffect(int idx)
    {
        if (tabBgImages[idx] == null) yield break;
        yield return PopRoutine(tabBgImages[idx].transform, 1.12f, 0.14f);
    }

    /// <summary>Full-screen colored flash (fade in → fade out).</summary>
    private IEnumerator ScreenFlash(Image overlay, Color peakColor, float duration)
    {
        if (overlay == null) yield break;
        overlay.gameObject.SetActive(true);
        float half = duration * 0.5f;

        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            var c   = peakColor;
            c.a     = Mathf.Lerp(0f, peakColor.a, t / half);
            overlay.color = c;
            yield return null;
        }
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            var c   = peakColor;
            c.a     = Mathf.Lerp(peakColor.a, 0f, t / half);
            overlay.color = c;
            yield return null;
        }

        overlay.color = new Color(peakColor.r, peakColor.g, peakColor.b, 0f);
        overlay.gameObject.SetActive(false);
    }

    // ── Camera shake ──────────────────────────────────────────────────────────

    /// <summary>Shakes the main camera by <paramref name="magnitude"/> world units for <paramref name="duration"/> seconds.</summary>
    private IEnumerator CameraShake(float duration, float magnitude)
    {
        var cam = Camera.main;
        if (cam == null) yield break;

        if (_shakeCoroutine != null)
        {
            StopCoroutine(_shakeCoroutine);
            cam.transform.position = _camOrigin;
        }

        _shakeCoroutine = StartCoroutine(ShakeRoutine(cam, duration, magnitude));
        yield return _shakeCoroutine;
        _shakeCoroutine = null;
    }

    private IEnumerator ShakeRoutine(Camera cam, float duration, float magnitude)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float decay = 1f - elapsed / duration;
            float ox    = Random.Range(-1f, 1f) * magnitude * decay;
            float oy    = Random.Range(-1f, 1f) * magnitude * decay;
            cam.transform.position = _camOrigin + new Vector3(ox, oy, 0f);
            yield return null;
        }
        cam.transform.position = _camOrigin;
    }

    // ── Pop animation ─────────────────────────────────────────────────────────

    private static IEnumerator PopRoutine(Transform t, float peakScale, float duration)
    {
        float half = duration * 0.5f;
        float e = 0f;
        while (e < half)
        {
            e += Time.deltaTime;
            t.localScale = Vector3.one * Mathf.Lerp(1f, peakScale, e / half);
            yield return null;
        }
        e = 0f;
        while (e < half)
        {
            e += Time.deltaTime;
            t.localScale = Vector3.one * Mathf.Lerp(peakScale, 1f, e / half);
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    // ── Utilities ─────────────────────────────────────────────────────────────

    private static int AbilityIndex(PGAbilitySystem.AbilityType type) => type switch
    {
        PGAbilitySystem.AbilityType.Shield => 0,
        PGAbilitySystem.AbilityType.Weapon => 1,
        PGAbilitySystem.AbilityType.Heal   => 2,
        _                                  => 0,
    };

    private static RectTransform MakeRT(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }
}
