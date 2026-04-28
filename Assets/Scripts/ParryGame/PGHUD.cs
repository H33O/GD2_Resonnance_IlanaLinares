using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD du Parry Game.
/// - Top: score + combo
/// - Bottom: 2 onglets — DÉFENSE / SOINS (l'onglet Armes est supprimé)
/// - Hearts: HP
/// - Textes flottants : PARRY!, AÏIE!, COMBO!, BLOQUÉ!, +1 VIE!
/// - Flash écran + camera shake
/// - Police JimNightshade sur tous les textes
/// </summary>
public class PGHUD : MonoBehaviour
{
    // ── Constants ─────────────────────────────────────────────────────────────

    private const float CanvasRefW    = 1080f;
    private const float TabBarH       = 220f;
    private const float TabBarPadding = 24f;
    private const float HeartSize     = 72f;   // taille du carré de vie
    private const float HeartSpacing  = 14f;
    private const float PopPeakScale  = 1.35f;
    private const float PopDuration   = 0.18f;

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Settings")]
    public PGSettings settings;

    // ── Internal refs ─────────────────────────────────────────────────────────

    private TextMeshProUGUI   scoreLabel;
    private TextMeshProUGUI   comboLabel;
    private Image[]           heartImages;
    private int               maxHp;
    private Coroutine         scorePopCoroutine;
    private Coroutine         comboPopCoroutine;

    // Tabs : [0]=Défense  [1]=Soins
    private Image[]           tabBgImages;
    private Image[]           cooldownFill;
    private TextMeshProUGUI[] cooldownLabel;
    private bool[]            tabReady;

    // Overlays plein écran
    private Image _healFlashOverlay;
    private Image _shieldBlockOverlay;
    private Image _hitFlashOverlay;

    // Camera shake
    private Coroutine _shakeCoroutine;
    private Vector3   _camOrigin;

    // Root pour textes flottants
    private RectTransform _feedbackRoot;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        PGGameManager.OnScoreChanged   += HandleScore;
        PGGameManager.OnComboChanged   += HandleCombo;
        PGGameManager.OnHpChanged      += HandleHp;
        PGGameManager.OnHpRestored     += HandleHpRestored;
        PGGameManager.OnGameOver       += HandleGameOver;
        PGGameManager.OnShieldBlocked  += HandleShieldBlocked;
        PGGameManager.OnParryFail      += HandleHit;
        PGGameManager.OnParrySuccess   += HandleParrySuccess;

        PGAbilitySystem.OnAbilityUsed      += HandleAbilityUsed;
        PGAbilitySystem.OnAbilityReady     += HandleAbilityReady;
        PGAbilitySystem.OnCooldownProgress += HandleCooldownProgress;
    }

    private void OnDisable()
    {
        PGGameManager.OnScoreChanged   -= HandleScore;
        PGGameManager.OnComboChanged   -= HandleCombo;
        PGGameManager.OnHpChanged      -= HandleHp;
        PGGameManager.OnHpRestored     -= HandleHpRestored;
        PGGameManager.OnGameOver       -= HandleGameOver;
        PGGameManager.OnShieldBlocked  -= HandleShieldBlocked;
        PGGameManager.OnParryFail      -= HandleHit;
        PGGameManager.OnParrySuccess   -= HandleParrySuccess;

        PGAbilitySystem.OnAbilityUsed      -= HandleAbilityUsed;
        PGAbilitySystem.OnAbilityReady     -= HandleAbilityReady;
        PGAbilitySystem.OnCooldownProgress -= HandleCooldownProgress;
    }

    // ── Init ──────────────────────────────────────────────────────────────────

    public void Init(RectTransform canvasRT)
    {
        maxHp = settings != null ? settings.maxHp : 3;
        if (Camera.main != null) _camOrigin = Camera.main.transform.position;

        // Root textes flottants (au-dessus du jeu, sous les overlays)
        var feedbackGO = new GameObject("FeedbackRoot");
        feedbackGO.transform.SetParent(canvasRT, false);
        _feedbackRoot            = feedbackGO.AddComponent<RectTransform>();
        _feedbackRoot.anchorMin  = Vector2.zero;
        _feedbackRoot.anchorMax  = Vector2.one;
        _feedbackRoot.offsetMin  = _feedbackRoot.offsetMax = Vector2.zero;

        BuildTopBar(canvasRT);
        BuildHearts(canvasRT);
        BuildBottomTabBar(canvasRT);
        BuildHealFlashOverlay(canvasRT);
        BuildShieldBlockOverlay(canvasRT);
        BuildHitFlashOverlay(canvasRT);
        BuildMenuButton(canvasRT);
    }

    // ── Bouton pause + panneau (bas-gauche) ───────────────────────────────────

    private static void BuildMenuButton(RectTransform canvasRT)
    {
        // Ordre de sibling : bouton d'abord, panneau ensuite (panneau au-dessus).
        GamePausePanel.CreatePauseButton(canvasRT);
        GamePausePanel.Create(canvasRT,
            onResume: null,
            onMenu:   () => PGGameManager.Instance?.ReturnToMenu());
    }

    // ── Top bar ───────────────────────────────────────────────────────────────

    private void BuildTopBar(RectTransform canvasRT)
    {
        var root = MakeRT("TopBar", canvasRT);
        root.anchorMin        = new Vector2(0f, 1f);
        root.anchorMax        = new Vector2(1f, 1f);
        root.pivot            = new Vector2(0.5f, 1f);
        root.sizeDelta        = new Vector2(0f, 160f);
        root.anchoredPosition = Vector2.zero;

        var bg           = root.gameObject.AddComponent<Image>();
        bg.color         = new Color(0f, 0f, 0f, 0.55f);
        bg.raycastTarget = false;

        // Score
        var scoreGO              = new GameObject("Score");
        scoreGO.transform.SetParent(root, false);
        scoreLabel               = scoreGO.AddComponent<TextMeshProUGUI>();
        scoreLabel.text          = "0";
        scoreLabel.fontSize      = 72f;
        scoreLabel.fontStyle     = FontStyles.Bold;
        scoreLabel.color         = Color.white;
        scoreLabel.alignment     = TextAlignmentOptions.Center;
        scoreLabel.raycastTarget = false;
        MenuAssets.ApplyFont(scoreLabel);
        var sRT       = scoreLabel.rectTransform;
        sRT.anchorMin = new Vector2(0.2f, 0f);
        sRT.anchorMax = new Vector2(0.8f, 1f);
        sRT.offsetMin = sRT.offsetMax = Vector2.zero;

        // Combo
        var comboGO              = new GameObject("Combo");
        comboGO.transform.SetParent(root, false);
        comboLabel               = comboGO.AddComponent<TextMeshProUGUI>();
        comboLabel.text          = "";
        comboLabel.fontSize      = 40f;
        comboLabel.fontStyle     = FontStyles.Bold;
        comboLabel.color         = new Color(1f, 0.85f, 0.2f, 1f);
        comboLabel.alignment     = TextAlignmentOptions.Center;
        comboLabel.raycastTarget = false;
        MenuAssets.ApplyFont(comboLabel);
        comboGO.SetActive(false);
        var cRT       = comboLabel.rectTransform;
        cRT.anchorMin = new Vector2(0.6f, 0f);
        cRT.anchorMax = new Vector2(1f, 1f);
        cRT.offsetMin = cRT.offsetMax = Vector2.zero;
    }

    // ── Hearts (carrés) ───────────────────────────────────────────────────────

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
            var hGO           = new GameObject($"Heart_{i}");
            hGO.transform.SetParent(root, false);
            var img           = hGO.AddComponent<Image>();
            img.color         = new Color(0.95f, 0.25f, 0.25f, 1f);
            img.raycastTarget = false;
            // Carré blanc : sprite carré au lieu du cercle
            img.sprite        = SpriteGenerator.CreateWhiteSquare();
            var rt            = img.rectTransform;
            rt.anchorMin      = new Vector2(0f, 0f);
            rt.anchorMax      = new Vector2(0f, 0f);
            rt.pivot          = new Vector2(0f, 0f);
            rt.sizeDelta      = new Vector2(HeartSize, HeartSize);
            rt.anchoredPosition = new Vector2(i * (HeartSize + HeartSpacing), 0f);
            heartImages[i]    = img;
        }
    }

    // ── Bottom tab bar : DÉFENSE / SOINS (2 onglets) ─────────────────────────

    private void BuildBottomTabBar(RectTransform canvasRT)
    {
        tabBgImages   = new Image[2];
        cooldownFill  = new Image[2];
        cooldownLabel = new TextMeshProUGUI[2];
        tabReady      = new bool[] { true, true };

        var root = MakeRT("BottomTabBar", canvasRT);
        root.anchorMin        = new Vector2(0f, 0f);
        root.anchorMax        = new Vector2(1f, 0f);
        root.pivot            = new Vector2(0.5f, 0f);
        root.sizeDelta        = new Vector2(0f, TabBarH);
        root.anchoredPosition = Vector2.zero;

        var bg           = root.gameObject.AddComponent<Image>();
        bg.color         = new Color(0.05f, 0.05f, 0.08f, 0.92f);
        bg.raycastTarget = false;

        string[] labels = { "DÉFENSE", "SOINS" };
        string[] icons  = { "🛡", "♥" };
        Color[]  colors =
        {
            settings != null ? settings.colorDefense : new Color(0.30f, 0.55f, 1f,   1f),
            settings != null ? settings.colorHeals   : new Color(0.25f, 0.85f, 0.45f, 1f),
        };

        for (int i = 0; i < labels.Length; i++)
        {
            int captured = i;
            BuildTab(root, labels[i], icons[i], colors[i], i,
                     () => OnTabPressed(captured));
        }
    }

    private void BuildTab(RectTransform parent, string label, string icon,
                          Color accentColor, int index,
                          UnityEngine.Events.UnityAction onClick)
    {
        var tabGO  = new GameObject($"Tab_{label}");
        tabGO.transform.SetParent(parent, false);

        var img    = tabGO.AddComponent<Image>();
        img.sprite = SpriteGenerator.CreateWhiteSquare();
        img.color  = new Color(accentColor.r * 0.18f, accentColor.g * 0.18f, accentColor.b * 0.18f, 1f);
        tabBgImages[index] = img;

        // Chaque onglet occupe exactement la moitié de la largeur du parent,
        // avec un padding interne sur les bords et entre les deux onglets.
        const float totalTabs = 2f;
        float anchorXMin = index / totalTabs;
        float anchorXMax = (index + 1f) / totalTabs;

        var rt        = img.rectTransform;
        rt.anchorMin  = new Vector2(anchorXMin, 0f);
        rt.anchorMax  = new Vector2(anchorXMax, 1f);
        rt.pivot      = new Vector2(0.5f, 0.5f);
        rt.offsetMin  = new Vector2(index == 0 ? TabBarPadding : TabBarPadding * 0.5f, TabBarPadding);
        rt.offsetMax  = new Vector2(index == 0 ? -TabBarPadding * 0.5f : -TabBarPadding, -TabBarPadding);

        // Bord accent haut
        var bGO       = new GameObject("Border");
        bGO.transform.SetParent(rt, false);
        var bImg      = bGO.AddComponent<Image>();
        bImg.color    = accentColor;
        bImg.raycastTarget = false;
        var bRT       = bImg.rectTransform;
        bRT.anchorMin = new Vector2(0f, 1f);
        bRT.anchorMax = new Vector2(1f, 1f);
        bRT.pivot     = new Vector2(0.5f, 1f);
        bRT.sizeDelta = new Vector2(0f, 6f);
        bRT.anchoredPosition = Vector2.zero;

        // ── Constantes de layout internes à l'onglet ──────────────────────────
        const float CooldownBarH = 20f;  // hauteur de la barre de cooldown
        const float IconSize     = 0.30f; // fraction de hauteur pour l'icône

        // Icône — moitié supérieure
        var iGO            = new GameObject("Icon");
        iGO.transform.SetParent(rt, false);
        var iTmp           = iGO.AddComponent<TextMeshProUGUI>();
        iTmp.text          = icon;
        iTmp.fontSize      = 48f;
        iTmp.alignment     = TextAlignmentOptions.Bottom | TextAlignmentOptions.Center;
        iTmp.raycastTarget = false;
        MenuAssets.ApplyFont(iTmp);
        var iRT       = iTmp.rectTransform;
        iRT.anchorMin = new Vector2(0f, 0.5f);
        iRT.anchorMax = new Vector2(1f, 1f);
        iRT.offsetMin = new Vector2(4f, 0f);
        iRT.offsetMax = new Vector2(-4f, -4f);

        // Label — centré verticalement dans la moitié basse (au-dessus de la barre)
        var lblGO          = new GameObject("Label");
        lblGO.transform.SetParent(rt, false);
        var lTmp           = lblGO.AddComponent<TextMeshProUGUI>();
        lTmp.text          = label;
        lTmp.fontSize      = 28f;
        lTmp.fontStyle     = FontStyles.Bold;
        lTmp.color         = accentColor;
        lTmp.alignment     = TextAlignmentOptions.Center;
        lTmp.raycastTarget = false;
        MenuAssets.ApplyFont(lTmp);
        var lRT       = lTmp.rectTransform;
        lRT.anchorMin = new Vector2(0f, 0f);
        lRT.anchorMax = new Vector2(1f, 0.5f);
        lRT.offsetMin = new Vector2(4f, CooldownBarH + 4f);
        lRT.offsetMax = new Vector2(-4f, 0f);

        // ── Cooldown fond ─────────────────────────────────────────────────────
        var fbGO          = new GameObject("CooldownBg");
        fbGO.transform.SetParent(rt, false);
        var fbImg         = fbGO.AddComponent<Image>();
        fbImg.sprite      = SpriteGenerator.CreateWhiteSquare();
        fbImg.color       = new Color(0f, 0f, 0f, 0.55f);
        fbImg.raycastTarget = false;
        var fbRT          = fbImg.rectTransform;
        fbRT.anchorMin    = new Vector2(0f, 0f);
        fbRT.anchorMax    = new Vector2(1f, 0f);
        fbRT.pivot        = new Vector2(0f, 0f);
        fbRT.sizeDelta    = new Vector2(0f, CooldownBarH);
        fbRT.anchoredPosition = Vector2.zero;

        // ── Cooldown fill ─────────────────────────────────────────────────────
        var fGO           = new GameObject("CooldownFill");
        fGO.transform.SetParent(rt, false);
        var fImg          = fGO.AddComponent<Image>();
        fImg.sprite       = SpriteGenerator.CreateWhiteSquare();
        fImg.color        = accentColor;
        fImg.type         = Image.Type.Filled;
        fImg.fillMethod   = Image.FillMethod.Horizontal;
        fImg.fillAmount   = 1f;
        fImg.raycastTarget = false;
        var fRT           = fImg.rectTransform;
        fRT.anchorMin     = new Vector2(0f, 0f);
        fRT.anchorMax     = new Vector2(1f, 0f);
        fRT.pivot         = new Vector2(0f, 0f);
        fRT.sizeDelta     = new Vector2(0f, CooldownBarH);
        fRT.anchoredPosition = Vector2.zero;
        cooldownFill[index] = fImg;

        // ── Cooldown texte — centré sur la barre ─────────────────────────────
        var cdGO          = new GameObject("CooldownTxt");
        cdGO.transform.SetParent(rt, false);
        var cdTmp         = cdGO.AddComponent<TextMeshProUGUI>();
        cdTmp.text        = "";
        cdTmp.fontSize    = 22f;
        cdTmp.fontStyle   = FontStyles.Bold;
        cdTmp.color       = Color.white;
        cdTmp.alignment   = TextAlignmentOptions.Center;
        cdTmp.raycastTarget = false;
        MenuAssets.ApplyFont(cdTmp);
        cdGO.SetActive(false);
        var cdRT          = cdTmp.rectTransform;
        cdRT.anchorMin    = new Vector2(0f, 0f);
        cdRT.anchorMax    = new Vector2(1f, 0f);
        cdRT.pivot        = new Vector2(0f, 0f);
        cdRT.sizeDelta    = new Vector2(0f, CooldownBarH);
        cdRT.anchoredPosition = Vector2.zero;
        cooldownLabel[index] = cdTmp;

        // Bouton
        var btn             = tabGO.AddComponent<Button>();
        btn.targetGraphic   = img;
        var co              = btn.colors;
        co.normalColor      = Color.white;
        co.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
        co.pressedColor     = new Color(accentColor.r, accentColor.g, accentColor.b, 0.6f);
        co.disabledColor    = new Color(0.35f, 0.35f, 0.35f, 0.6f);
        co.fadeDuration     = 0.08f;
        btn.colors          = co;
        btn.onClick.AddListener(onClick);
    }

    // ── Overlays plein écran ──────────────────────────────────────────────────

    private void BuildHealFlashOverlay(RectTransform r)     => _healFlashOverlay    = BuildOverlay(r, "HealFlash",       new Color(0.10f, 1f, 0.35f, 0f));
    private void BuildShieldBlockOverlay(RectTransform r)   => _shieldBlockOverlay  = BuildOverlay(r, "ShieldBlockFlash",new Color(0.40f, 0.85f, 1f, 0f));
    private void BuildHitFlashOverlay(RectTransform r)      => _hitFlashOverlay     = BuildOverlay(r, "HitFlash",        new Color(1f, 0.05f, 0.05f, 0f));

    private static Image BuildOverlay(RectTransform canvasRT, string name, Color color)
    {
        var go            = new GameObject(name);
        go.transform.SetParent(canvasRT, false);
        var img           = go.AddComponent<Image>();
        img.color         = color;
        img.raycastTarget = false;
        var rt            = img.rectTransform;
        rt.anchorMin      = Vector2.zero;
        rt.anchorMax      = Vector2.one;
        rt.offsetMin      = rt.offsetMax = Vector2.zero;
        go.SetActive(false);
        return img;
    }

    // ── Tab dispatch ──────────────────────────────────────────────────────────

    private void OnTabPressed(int index)
    {
        if (!tabReady[index]) return;
        var ability = PGAbilitySystem.Instance;
        if (ability == null) return;

        switch (index)
        {
            case 0: ability.UseShield(); break; // Défense
            case 1: ability.UseHeal();   break; // Soins
        }

        StartCoroutine(TabPressEffect(index));
    }

    // ── Ability event handlers ────────────────────────────────────────────────

    private void HandleAbilityUsed(PGAbilitySystem.AbilityType type)
    {
        int idx = AbilityIndex(type);
        if (idx < 0) return;
        tabReady[idx] = false;
        SetTabGreyed(idx, true);
        if (cooldownLabel[idx] != null) cooldownLabel[idx].gameObject.SetActive(true);
        if (cooldownFill[idx]  != null) cooldownFill[idx].fillAmount = 0f;
    }

    private void HandleAbilityReady(PGAbilitySystem.AbilityType type)
    {
        int idx = AbilityIndex(type);
        if (idx < 0) return;
        tabReady[idx] = true;
        SetTabGreyed(idx, false);
        if (cooldownLabel[idx] != null)
        {
            cooldownLabel[idx].text = "";
            cooldownLabel[idx].gameObject.SetActive(false);
        }
        if (cooldownFill[idx] != null) cooldownFill[idx].fillAmount = 1f;
        StartCoroutine(TabReadyFlash(idx));
    }

    private void HandleCooldownProgress(PGAbilitySystem.AbilityType type, float progress)
    {
        int idx = AbilityIndex(type);
        if (idx < 0) return;
        if (cooldownFill[idx]  != null) cooldownFill[idx].fillAmount = progress;
        if (cooldownLabel[idx] != null)
        {
            float total = type switch
            {
                PGAbilitySystem.AbilityType.Heal => settings != null ? settings.healCooldown   : 45f,
                _                                => settings != null ? settings.shieldCooldown :  8f,
            };
            cooldownLabel[idx].text = $"{Mathf.Ceil(total * (1f - progress)):0}s";
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

    private void HandleParrySuccess()
    {
        SpawnFloatingText("PARRY!", new Color(1f, 0.92f, 0.20f, 1f), 72f,
                          new Vector2(0f, 100f), Vector2.up * 200f, 0.75f);
    }

    private void HandleCombo(int combo)
    {
        if (comboLabel == null) return;
        if (combo <= 1) { comboLabel.gameObject.SetActive(false); return; }

        comboLabel.gameObject.SetActive(true);
        comboLabel.text = $"x{combo} COMBO!";
        if (comboPopCoroutine != null) StopCoroutine(comboPopCoroutine);

        float peak = 1.25f + Mathf.Min(combo - 2, 8) * 0.08f;
        comboPopCoroutine = StartCoroutine(PopRoutine(comboLabel.transform, peak, PopDuration));

        Color col = combo >= 10
            ? new Color(1f, 0.25f, 0.10f, 1f)
            : combo >= 5
                ? new Color(1f, 0.55f, 0.10f, 1f)
                : new Color(1f, 0.85f, 0.20f, 1f);

        SpawnFloatingText($"x{combo} COMBO!", col, 68f + Mathf.Min(combo, 15) * 2.5f,
                          new Vector2(0f, 200f), Vector2.up * 230f, 1.0f);
    }

    private void HandleHp(int hp)
    {
        if (heartImages == null) return;
        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] == null) continue;
            heartImages[i].color = i < hp
                ? new Color(0.95f, 0.25f, 0.25f, 1f)
                : new Color(0.30f, 0.30f, 0.30f, 0.40f);
        }
    }

    private void HandleHpRestored(int hp)
    {
        HandleHp(hp);
        StartCoroutine(ScreenFlash(_healFlashOverlay, new Color(0.10f, 1f, 0.35f, 0.72f), 0.70f));
        StartCoroutine(CameraShake(0.18f, 0.12f));
        SpawnFloatingText("+1 VIE!", new Color(0.20f, 1f, 0.40f, 1f), 80f,
                          new Vector2(0f, 300f), Vector2.up * 180f, 0.80f);

        if (heartImages != null)
            for (int i = 0; i < hp && i < heartImages.Length; i++)
                StartCoroutine(PopRoutine(heartImages[i].transform, 1.8f, 0.30f));
    }

    private void HandleShieldBlocked()
    {
        StartCoroutine(ScreenFlash(_shieldBlockOverlay, new Color(0.40f, 0.85f, 1f, 0.80f), 0.45f));
        StartCoroutine(CameraShake(0.20f, 0.14f));
        SpawnFloatingText("BLOQUÉ!", new Color(0.40f, 0.85f, 1f, 1f), 70f,
                          new Vector2(0f, 100f), Vector2.up * 190f, 0.65f);
    }

    private void HandleHit()
    {
        StartCoroutine(ScreenFlash(_hitFlashOverlay, new Color(1f, 0.05f, 0.05f, 0.70f), 0.50f));
        StartCoroutine(CameraShake(0.32f, 0.22f));
        SpawnFloatingText("AÏIE!", new Color(1f, 0.10f, 0.10f, 1f), 100f,
                          new Vector2(0f, 0f), Vector2.up * 160f, 0.70f);

        if (heartImages != null)
            for (int i = 0; i < heartImages.Length; i++)
                if (heartImages[i] != null && heartImages[i].color.a > 0.5f)
                    StartCoroutine(PopRoutine(heartImages[i].transform, 1.4f, 0.20f));
    }

    private void HandleGameOver()
    {
        if (comboLabel != null) comboLabel.gameObject.SetActive(false);
    }

    // ── Textes flottants ──────────────────────────────────────────────────────

    /// <summary>
    /// Spawne un texte animé avec police JimNightshade :
    /// pop d'apparition → dérive en pixels → fondu quadratique.
    /// </summary>
    private void SpawnFloatingText(string text, Color color, float fontSize,
                                   Vector2 anchorOffset, Vector2 drift, float duration)
    {
        if (_feedbackRoot == null) return;
        StartCoroutine(FloatingTextRoutine(text, color, fontSize, anchorOffset, drift, duration));
    }

    private IEnumerator FloatingTextRoutine(string text, Color color, float fontSize,
                                            Vector2 anchorOffset, Vector2 drift, float duration)
    {
        var go             = new GameObject("FloatTxt");
        go.transform.SetParent(_feedbackRoot, false);

        var tmp            = go.AddComponent<TextMeshProUGUI>();
        tmp.text           = text;
        tmp.fontSize       = fontSize;
        tmp.fontStyle      = FontStyles.Bold;
        tmp.color          = color;
        tmp.alignment      = TextAlignmentOptions.Center;
        tmp.raycastTarget  = false;
        MenuAssets.ApplyFont(tmp);

        var rt             = tmp.rectTransform;
        rt.anchorMin       = new Vector2(0.5f, 0.5f);
        rt.anchorMax       = new Vector2(0.5f, 0.5f);
        rt.pivot           = new Vector2(0.5f, 0.5f);
        rt.sizeDelta       = new Vector2(900f, 220f);
        rt.anchoredPosition = anchorOffset;

        // Pop d'apparition
        float e = 0f;
        while (e < 0.10f)
        {
            e += Time.deltaTime;
            go.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.15f, e / 0.10f);
            yield return null;
        }
        go.transform.localScale = Vector3.one;

        // Dérive + fondu quadratique
        Vector2 startPos = anchorOffset;
        e = 0f;
        while (e < duration)
        {
            e += Time.deltaTime;
            float r           = e / duration;
            rt.anchoredPosition = startPos + drift * r;
            var c             = color;
            c.a               = Mathf.Lerp(1f, 0f, r * r);
            tmp.color         = c;
            yield return null;
        }

        Destroy(go);
    }

    // ── Tab visual helpers ────────────────────────────────────────────────────

    private void SetTabGreyed(int idx, bool greyed)
    {
        if (tabBgImages[idx] == null) return;
        var btn = tabBgImages[idx].GetComponent<Button>();
        if (btn != null) btn.interactable = !greyed;

        var c = tabBgImages[idx].color;
        tabBgImages[idx].color = greyed
            ? new Color(c.r * 0.3f, c.g * 0.3f, c.b * 0.3f, 1f)
            : new Color(Mathf.Min(c.r / 0.3f, 1f), Mathf.Min(c.g / 0.3f, 1f), Mathf.Min(c.b / 0.3f, 1f), 1f);
    }

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

    private IEnumerator TabPressEffect(int idx)
    {
        if (tabBgImages[idx] == null) yield break;
        yield return PopRoutine(tabBgImages[idx].transform, 1.12f, 0.14f);
    }

    // ── Screen flash + camera shake ───────────────────────────────────────────

    private IEnumerator ScreenFlash(Image overlay, Color peakColor, float duration)
    {
        if (overlay == null) yield break;
        overlay.gameObject.SetActive(true);
        float half = duration * 0.5f;

        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            var c = peakColor; c.a = Mathf.Lerp(0f, peakColor.a, t / half);
            overlay.color = c;
            yield return null;
        }
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            var c = peakColor; c.a = Mathf.Lerp(peakColor.a, 0f, t / half);
            overlay.color = c;
            yield return null;
        }
        overlay.color = new Color(peakColor.r, peakColor.g, peakColor.b, 0f);
        overlay.gameObject.SetActive(false);
    }

    private IEnumerator CameraShake(float duration, float magnitude)
    {
        var cam = Camera.main;
        if (cam == null) yield break;
        if (_shakeCoroutine != null) { StopCoroutine(_shakeCoroutine); cam.transform.position = _camOrigin; }
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
            cam.transform.position = _camOrigin + new Vector3(
                Random.Range(-1f, 1f) * magnitude * decay,
                Random.Range(-1f, 1f) * magnitude * decay, 0f);
            yield return null;
        }
        cam.transform.position = _camOrigin;
    }

    // ── Pop animation ─────────────────────────────────────────────────────────

    private static IEnumerator PopRoutine(Transform t, float peakScale, float duration)
    {
        float half = duration * 0.5f, e = 0f;
        while (e < half) { e += Time.deltaTime; t.localScale = Vector3.one * Mathf.Lerp(1f, peakScale, e / half); yield return null; }
        e = 0f;
        while (e < half) { e += Time.deltaTime; t.localScale = Vector3.one * Mathf.Lerp(peakScale, 1f, e / half); yield return null; }
        t.localScale = Vector3.one;
    }

    // ── Utilities ─────────────────────────────────────────────────────────────

    /// <summary>Index du tab pour un type d'ability. Retourne -1 si le type n'a plus de tab.</summary>
    private static int AbilityIndex(PGAbilitySystem.AbilityType type) => type switch
    {
        PGAbilitySystem.AbilityType.Shield => 0,
        PGAbilitySystem.AbilityType.Heal   => 1,
        _                                  => -1,
    };

    private static RectTransform MakeRT(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }
}
