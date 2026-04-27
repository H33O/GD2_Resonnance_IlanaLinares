using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD du jeu TiltBall.
///
/// Composition :
///   ┌─────────────────────────────────────────┐  ← Widget HUD centré en haut
///   │  NV 3/12  │  00:42  │  ████░░ 150 pts  │
///   │           ◆ CLÉ REQUISE  (niveaux impairs)│
///   └─────────────────────────────────────────┘
///
///   • Typographie  : JimNightshade via TBUIStyle
///   • Fond widget  : sprite jaugenormal (Sliced) — gives the textured gauge feel
///   • Barre score  : jaugenormal en mode Filled, teinte dorée
///   • Bouton MENU  : jaugenormal Sliced, discret coin bas-gauche
/// </summary>
public class TBHud : MonoBehaviour
{
    // ── Palette ───────────────────────────────────────────────────────────────

    private static readonly Color ColWidgetBg     = new Color(0.06f, 0.05f, 0.12f, 0.90f);
    private static readonly Color ColTimer        = Color.white;
    private static readonly Color ColLevel        = new Color(1f, 1f, 1f, 0.65f);
    private static readonly Color ColKeyMissing   = new Color(1f, 1f, 1f, 0.35f);
    private static readonly Color ColKeyCollected = new Color(1f, 0.85f, 0.10f, 1f);
    private static readonly Color ColSep          = new Color(1f, 1f, 1f, 0.18f);
    private static readonly Color ColMenuBg       = new Color(0.05f, 0.04f, 0.10f, 0.85f);
    private static readonly Color ColScoreBar     = new Color(1f, 0.78f, 0.08f, 0.55f);
    private static readonly Color ColScoreBarBg   = new Color(0f, 0f, 0f, 0.35f);
    private static readonly Color ColScoreTxt     = new Color(1f, 0.85f, 0.10f, 1f);

    // ── État ──────────────────────────────────────────────────────────────────

    private bool            requireKey;
    private RectTransform   container;
    private TextMeshProUGUI keyLabel;
    private TextMeshProUGUI timerLabel;
    private TextMeshProUGUI scoreLabel;
    private Image           scoreBarFill;

    // Score max estimé pour la barre (ajusté dynamiquement)
    private int scoreMax = 500;

    // ── Init ──────────────────────────────────────────────────────────────────

    /// <summary>Initialise et construit le HUD.</summary>
    public void Init(bool needKey, RectTransform rt, int levelIndex = 0)
    {
        requireKey = needKey;
        container  = rt;
        Build(levelIndex);

        if (requireKey && TBGameManager.Instance != null)
            TBGameManager.Instance.OnKeyCollected.AddListener(OnKeyCollected);
    }

    private void OnDestroy()
    {
        if (TBGameManager.Instance != null)
            TBGameManager.Instance.OnKeyCollected.RemoveListener(OnKeyCollected);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (TBGameManager.Instance == null) return;

        // Timer
        if (timerLabel != null)
        {
            float t = TBGameManager.Instance.ElapsedTime;
            int   m = Mathf.FloorToInt(t / 60f);
            int   s = Mathf.FloorToInt(t % 60f);
            timerLabel.text = $"{m:00}:{s:00}";
        }

        // Score texte
        int score = TBGameManager.Instance.Score;
        if (scoreLabel != null)
            scoreLabel.text = $"{score} pts";

        // Barre de score (fill proportionnel au max connu)
        if (scoreBarFill != null)
        {
            if (score > scoreMax) scoreMax = score + 100;
            scoreBarFill.fillAmount = Mathf.Clamp01((float)score / scoreMax);
        }
    }

    // ── Construction ──────────────────────────────────────────────────────────

    private void Build(int levelIndex)
    {
        float widgetH = requireKey ? 160f : 108f;

        // ── Fond du widget (sprite jauge Sliced) ──────────────────────────────
        var widgetGO  = new GameObject("HudWidget");
        widgetGO.transform.SetParent(container, false);

        var widgetImg = widgetGO.AddComponent<Image>();
        widgetImg.color = ColWidgetBg;
        widgetImg.raycastTarget = false;
        TBUIStyle.ApplyJauge(widgetImg, ColWidgetBg);

        var widgetRT          = widgetImg.rectTransform;
        widgetRT.anchorMin    = new Vector2(0.5f, 1f);
        widgetRT.anchorMax    = new Vector2(0.5f, 1f);
        widgetRT.pivot        = new Vector2(0.5f, 1f);
        widgetRT.sizeDelta    = new Vector2(780f, widgetH);
        widgetRT.anchoredPosition = new Vector2(0f, -20f);

        float rowYMin = requireKey ? 0.42f : 0f;

        // ── Colonne gauche : NV ───────────────────────────────────────────────
        var levelTmp = MakeTmp("LevelLabel", widgetRT,
            $"NV {levelIndex + 1}/{TBGameManager.TotalLevels}",
            28f, ColLevel, FontStyles.Bold,
            new Vector2(0f, rowYMin), new Vector2(0.28f, 1f));
        levelTmp.alignment = TextAlignmentOptions.Center;
        MakeSepV(widgetRT, 0.30f, requireKey ? 0.46f : 0.14f, 0.86f);

        // ── Colonne centrale : Timer ───────────────────────────────────────────
        timerLabel = MakeTmp("TimerLabel", widgetRT,
            "00:00", 40f, ColTimer, FontStyles.Bold,
            new Vector2(0.32f, rowYMin), new Vector2(0.60f, 1f));
        timerLabel.alignment = TextAlignmentOptions.Center;

        MakeSepV(widgetRT, 0.62f, requireKey ? 0.46f : 0.14f, 0.86f);

        // ── Colonne droite : Score (label + barre jauge) ──────────────────────
        BuildScoreColumn(widgetRT, rowYMin);

        // ── Ligne 2 : indicateur de clé ───────────────────────────────────────
        if (requireKey)
            BuildKeyIndicator(widgetRT);

        // ── Swipe input plein écran ───────────────────────────────────────────
        TBSwipeInput.Create(container);

        // ── Bouton MENU (bas-gauche) ──────────────────────────────────────────
        BuildMenuButton();
    }

    private void BuildScoreColumn(RectTransform parent, float rowYMin)
    {
        // Score texte
        scoreLabel = MakeTmp("ScoreLabel", parent,
            "0 pts", 26f, ColScoreTxt, FontStyles.Bold,
            new Vector2(0.64f, rowYMin + 0.32f), new Vector2(1f, 1f));
        scoreLabel.alignment = TextAlignmentOptions.Center;

        // Fond de la barre
        var barBgGO  = new GameObject("ScoreBarBg");
        barBgGO.transform.SetParent(parent, false);
        var barBgImg = barBgGO.AddComponent<Image>();
        barBgImg.raycastTarget = false;
        TBUIStyle.ApplyJauge(barBgImg, ColScoreBarBg);
        var barBgRT  = barBgImg.rectTransform;
        barBgRT.anchorMin = new Vector2(0.65f, rowYMin);
        barBgRT.anchorMax = new Vector2(0.98f, rowYMin + 0.30f);
        barBgRT.offsetMin = barBgRT.offsetMax = Vector2.zero;

        // Barre de remplissage jauge dorée
        var barFillGO  = new GameObject("ScoreBarFill");
        barFillGO.transform.SetParent(parent, false);
        var barFillImg = barFillGO.AddComponent<Image>();
        barFillImg.raycastTarget = false;
        TBUIStyle.ApplyJaugeFill(barFillImg, ColScoreBar, 0f);
        var barFillRT  = barFillImg.rectTransform;
        barFillRT.anchorMin = new Vector2(0.65f, rowYMin);
        barFillRT.anchorMax = new Vector2(0.98f, rowYMin + 0.30f);
        barFillRT.offsetMin = barFillRT.offsetMax = Vector2.zero;
        scoreBarFill = barFillImg;
    }

    private void BuildKeyIndicator(RectTransform widgetRT)
    {
        // Fond teinté pour la ligne de clé
        var bgGO  = new GameObject("KeyBg");
        bgGO.transform.SetParent(widgetRT, false);
        var bgImg = bgGO.AddComponent<Image>();
        TBUIStyle.ApplyJauge(bgImg, new Color(0.5f, 0.4f, 0.05f, 0.30f));
        bgImg.raycastTarget = false;
        var bgRT  = bgImg.rectTransform;
        bgRT.anchorMin = new Vector2(0f, 0f);
        bgRT.anchorMax = new Vector2(1f, 0.40f);
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

        var go = new GameObject("KeyIndicator");
        go.transform.SetParent(widgetRT, false);
        keyLabel           = go.AddComponent<TextMeshProUGUI>();
        keyLabel.text      = "◆  CLÉ REQUISE";
        keyLabel.fontSize  = 24f;
        keyLabel.color     = ColKeyMissing;
        keyLabel.alignment = TextAlignmentOptions.Center;
        keyLabel.fontStyle = FontStyles.Bold;
        keyLabel.raycastTarget = false;
        TBUIStyle.ApplyFont(keyLabel);
        var rt       = keyLabel.rectTransform;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0.40f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private void BuildMenuButton()
    {
        var go  = new GameObject("MenuButton");
        go.transform.SetParent(container, false);

        var img = go.AddComponent<Image>();
        img.color = ColMenuBg;
        TBUIStyle.ApplyJauge(img, ColMenuBg);

        var rt         = img.rectTransform;
        rt.anchorMin   = new Vector2(0f, 0f);
        rt.anchorMax   = new Vector2(0f, 0f);
        rt.pivot       = new Vector2(0f, 0f);
        rt.sizeDelta   = new Vector2(210f, 72f);
        rt.anchoredPosition = new Vector2(20f, 20f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => TBGameManager.Instance?.GoToMenu());

        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        var tmp        = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text       = "← MENU";
        tmp.fontSize   = 28f;
        tmp.color      = new Color(1f, 1f, 1f, 0.80f);
        tmp.alignment  = TextAlignmentOptions.Center;
        tmp.fontStyle  = FontStyles.Bold;
        tmp.raycastTarget = false;
        TBUIStyle.ApplyFont(tmp);
        var textRT       = tmp.rectTransform;
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = textRT.offsetMax = Vector2.zero;
    }

    // ── Séparateur vertical ───────────────────────────────────────────────────

    private void MakeSepV(RectTransform parent, float anchorX, float anchorYMin, float anchorYMax)
    {
        var go  = new GameObject("SepV");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = ColSep;
        img.raycastTarget = false;
        var rt  = img.rectTransform;
        rt.anchorMin = new Vector2(anchorX, anchorYMin);
        rt.anchorMax = new Vector2(anchorX, anchorYMax);
        rt.sizeDelta = new Vector2(2f, 0f);
    }

    // ── Helper TMP ────────────────────────────────────────────────────────────

    private static TextMeshProUGUI MakeTmp(string name, RectTransform parent,
        string text, float fontSize, Color color, FontStyles style,
        Vector2 aMin, Vector2 aMax)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp        = go.AddComponent<TextMeshProUGUI>();
        tmp.text       = text;
        tmp.fontSize   = fontSize;
        tmp.color      = color;
        tmp.fontStyle  = style;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = false;
        TBUIStyle.ApplyFont(tmp);
        var rt         = tmp.rectTransform;
        rt.anchorMin   = aMin;
        rt.anchorMax   = aMax;
        rt.offsetMin   = rt.offsetMax = Vector2.zero;
        return tmp;
    }

    // ── Événements ────────────────────────────────────────────────────────────

    private void OnKeyCollected()
    {
        if (keyLabel == null) return;
        keyLabel.color = ColKeyCollected;
        keyLabel.text  = "◆  CLÉ COLLECTÉE";
    }
}
