using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD du jeu TiltBall : widget centré en haut de l'écran regroupant
/// le numéro de niveau, le timer, le score et l'indicateur de clé (niveaux impairs).
/// Le bouton menu reste en bas à gauche, discret.
/// </summary>
public class TBHud : MonoBehaviour
{
    // ── Couleurs ──────────────────────────────────────────────────────────────

    private static readonly Color ColWidgetBg     = new Color(0.04f, 0.04f, 0.08f, 0.82f);
    private static readonly Color ColTimer        = Color.white;
    private static readonly Color ColLevel        = new Color(1f, 1f, 1f, 0.55f);
    private static readonly Color ColKeyMissing   = new Color(1f, 1f, 1f, 0.30f);
    private static readonly Color ColKeyCollected = new Color(1f, 0.85f, 0.10f, 1f);
    private static readonly Color ColSep          = new Color(1f, 1f, 1f, 0.15f);
    private static readonly Color ColMenuBg       = new Color(0f, 0f, 0f, 0.50f);

    // ── État ──────────────────────────────────────────────────────────────────

    private bool            requireKey;
    private RectTransform   container;
    private TextMeshProUGUI keyLabel;
    private TextMeshProUGUI timerLabel;
    private TextMeshProUGUI scoreLabel;

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

        if (timerLabel != null)
        {
            float t = TBGameManager.Instance.ElapsedTime;
            int   m = Mathf.FloorToInt(t / 60f);
            int   s = Mathf.FloorToInt(t % 60f);
            timerLabel.text = $"{m:00}:{s:00}";
        }

        if (scoreLabel != null)
            scoreLabel.text = $"{TBGameManager.Instance.Score} pts";
    }

    // ── Construction ──────────────────────────────────────────────────────────

    private void Build(int levelIndex)
    {
        // ── Widget centré en haut ─────────────────────────────────────────────
        //
        //   ┌────────────────────────────────────┐
        //   │  NV 3/8   │  00:42  │   150 pts    │
        //   │           ◆ CLÉ REQUISE (conditionnel)│
        //   └────────────────────────────────────┘

        var widgetGO  = new GameObject("HudWidget");
        widgetGO.transform.SetParent(container, false);

        var widgetImg = widgetGO.AddComponent<Image>();
        widgetImg.sprite      = SpriteGenerator.CreateWhiteSquare();
        widgetImg.color       = ColWidgetBg;
        widgetImg.raycastTarget = false;

        var widgetRT          = widgetImg.rectTransform;
        widgetRT.anchorMin    = new Vector2(0.5f, 1f);
        widgetRT.anchorMax    = new Vector2(0.5f, 1f);
        widgetRT.pivot        = new Vector2(0.5f, 1f);
        widgetRT.sizeDelta    = new Vector2(760f, requireKey ? 140f : 96f);
        widgetRT.anchoredPosition = new Vector2(0f, -24f);

        // Colonne gauche : Niveau
        var levelGO    = new GameObject("LevelLabel");
        levelGO.transform.SetParent(widgetRT, false);
        var levelTmp   = levelGO.AddComponent<TextMeshProUGUI>();
        levelTmp.text  = $"NV {levelIndex + 1}/{TBGameManager.TotalLevels}";
        levelTmp.fontSize   = 30f;
        levelTmp.color      = ColLevel;
        levelTmp.fontStyle  = FontStyles.Bold;
        levelTmp.alignment  = TextAlignmentOptions.Center;
        levelTmp.raycastTarget = false;
        var levelRT         = levelTmp.rectTransform;
        levelRT.anchorMin   = new Vector2(0f,     requireKey ? 0.44f : 0f);
        levelRT.anchorMax   = new Vector2(0.32f,  1f);
        levelRT.offsetMin   = new Vector2(8f,  0f);
        levelRT.offsetMax   = new Vector2(0f, -6f);

        // Séparateur gauche
        MakeSepV(widgetRT, 0.34f, requireKey ? 0.44f : 0.12f, 0.88f);

        // Colonne centrale : Timer
        var timerGO      = new GameObject("TimerLabel");
        timerGO.transform.SetParent(widgetRT, false);
        timerLabel       = timerGO.AddComponent<TextMeshProUGUI>();
        timerLabel.text  = "00:00";
        timerLabel.fontSize   = 38f;
        timerLabel.color      = ColTimer;
        timerLabel.fontStyle  = FontStyles.Bold;
        timerLabel.alignment  = TextAlignmentOptions.Center;
        timerLabel.raycastTarget = false;
        var timerRT           = timerLabel.rectTransform;
        timerRT.anchorMin     = new Vector2(0.36f, requireKey ? 0.44f : 0f);
        timerRT.anchorMax     = new Vector2(0.64f, 1f);
        timerRT.offsetMin     = timerRT.offsetMax = Vector2.zero;

        // Séparateur droit
        MakeSepV(widgetRT, 0.66f, requireKey ? 0.44f : 0.12f, 0.88f);

        // Colonne droite : Score
        var scoreGO     = new GameObject("ScoreLabel");
        scoreGO.transform.SetParent(widgetRT, false);
        scoreLabel      = scoreGO.AddComponent<TextMeshProUGUI>();
        scoreLabel.text = "0 pts";
        scoreLabel.fontSize   = 28f;
        scoreLabel.color      = new Color(1f, 0.85f, 0.10f, 0.90f);
        scoreLabel.fontStyle  = FontStyles.Bold;
        scoreLabel.alignment  = TextAlignmentOptions.Center;
        scoreLabel.raycastTarget = false;
        var scoreRT           = scoreLabel.rectTransform;
        scoreRT.anchorMin     = new Vector2(0.68f, requireKey ? 0.44f : 0f);
        scoreRT.anchorMax     = new Vector2(1f,    1f);
        scoreRT.offsetMin     = new Vector2(0f,  0f);
        scoreRT.offsetMax     = new Vector2(-8f, -6f);

        // Ligne 2 optionnelle : indicateur de clé (centré sur toute la largeur)
        if (requireKey)
            BuildKeyIndicator(widgetRT);

        // Swipe input plein écran (doit être ajouté en premier sibling pour être sous le HUD)
        TBSwipeInput.Create(container);

        // Bouton retour menu (coin bas-gauche)
        BuildMenuButton();
    }

    private void BuildKeyIndicator(RectTransform widgetRT)
    {
        var go  = new GameObject("KeyIndicator");
        go.transform.SetParent(widgetRT, false);

        keyLabel           = go.AddComponent<TextMeshProUGUI>();
        keyLabel.text      = "◆  CLÉ REQUISE";
        keyLabel.fontSize  = 24f;
        keyLabel.color     = ColKeyMissing;
        keyLabel.alignment = TextAlignmentOptions.Center;
        keyLabel.fontStyle = FontStyles.Bold;
        keyLabel.raycastTarget = false;

        var rt       = keyLabel.rectTransform;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0.42f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private void MakeSepV(RectTransform parent, float anchorX, float anchorYMin, float anchorYMax)
    {
        var go  = new GameObject("SepV");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite      = SpriteGenerator.CreateWhiteSquare();
        img.color       = ColSep;
        img.raycastTarget = false;
        var rt          = img.rectTransform;
        rt.anchorMin    = new Vector2(anchorX, anchorYMin);
        rt.anchorMax    = new Vector2(anchorX, anchorYMax);
        rt.sizeDelta    = new Vector2(2f, 0f);
    }

    private void BuildMenuButton()
    {
        var go  = new GameObject("MenuButton");
        go.transform.SetParent(container, false);

        var img   = go.AddComponent<Image>();
        img.sprite = SpriteGenerator.CreateWhiteSquare();
        img.color = ColMenuBg;

        var rt         = img.rectTransform;
        rt.anchorMin   = new Vector2(0f, 0f);
        rt.anchorMax   = new Vector2(0f, 0f);
        rt.pivot       = new Vector2(0f, 0f);
        rt.sizeDelta   = new Vector2(200f, 68f);
        rt.anchoredPosition = new Vector2(20f, 20f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => TBGameManager.Instance?.GoToMenu());

        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);

        var tmp        = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text       = "← MENU";
        tmp.fontSize   = 26f;
        tmp.color      = Color.white;
        tmp.alignment  = TextAlignmentOptions.Center;
        tmp.fontStyle  = FontStyles.Bold;
        tmp.raycastTarget = false;

        var textRT       = tmp.rectTransform;
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = textRT.offsetMax = Vector2.zero;
    }

    // ── Événements ────────────────────────────────────────────────────────────

    private void OnKeyCollected()
    {
        if (keyLabel == null) return;
        keyLabel.color = ColKeyCollected;
        keyLabel.text  = "◆  CLÉ COLLECTÉE";
    }
}

