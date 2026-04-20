using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD du jeu TiltBall : numéro de niveau, timer en temps réel,
/// indicateur de clé (niveaux impairs), bouton menu.
/// </summary>
public class TBHud : MonoBehaviour
{
    // ── Couleurs ──────────────────────────────────────────────────────────────

    private static readonly Color ColTimer        = new Color(1f, 1f, 1f, 0.90f);
    private static readonly Color ColLevel        = new Color(1f, 1f, 1f, 0.60f);
    private static readonly Color ColKeyMissing   = new Color(1f, 1f, 1f, 0.28f);
    private static readonly Color ColKeyCollected = new Color(1f, 0.85f, 0.10f, 1f);

    // ── État ──────────────────────────────────────────────────────────────────

    private bool            requireKey;
    private RectTransform   container;
    private TextMeshProUGUI keyLabel;
    private TextMeshProUGUI timerLabel;

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

    // ── Update timer ──────────────────────────────────────────────────────────

    private void Update()
    {
        if (timerLabel == null || TBGameManager.Instance == null) return;

        float t = TBGameManager.Instance.ElapsedTime;
        int   m = Mathf.FloorToInt(t / 60f);
        int   s = Mathf.FloorToInt(t % 60f);
        timerLabel.text = $"{m:00}:{s:00}";
    }

    // ── Construction ──────────────────────────────────────────────────────────

    private void Build(int levelIndex)
    {
        // Numéro de niveau (coin haut-gauche)
        MakeLabel("LevelLabel", $"NIVEAU {levelIndex + 1}/{TBGameManager.TotalLevels}",
                  34f, ColLevel,
                  new Vector2(0.03f, 0.935f), new Vector2(0.55f, 0.975f),
                  TextAlignmentOptions.Left);

        // Timer (coin haut-droit)
        var timerGO = new GameObject("TimerLabel");
        timerGO.transform.SetParent(container, false);
        timerLabel           = timerGO.AddComponent<TextMeshProUGUI>();
        timerLabel.text      = "00:00";
        timerLabel.fontSize  = 40f;
        timerLabel.color     = ColTimer;
        timerLabel.alignment = TextAlignmentOptions.Right;
        timerLabel.fontStyle = FontStyles.Bold;

        var timerRT       = timerLabel.rectTransform;
        timerRT.anchorMin = new Vector2(0.60f, 0.935f);
        timerRT.anchorMax = new Vector2(0.97f, 0.975f);
        timerRT.offsetMin = timerRT.offsetMax = Vector2.zero;

        // Indicateur de clé (niveaux impairs uniquement)
        if (requireKey)
            BuildKeyIndicator();

        // Joystick virtuel (bas de l'écran)
        TBJoystick.Create(container);

        // Bouton retour menu (coin haut-gauche, au-dessus du joystick)
        BuildMenuButton();
    }

    private void BuildKeyIndicator()
    {
        var go  = new GameObject("KeyIndicator");
        go.transform.SetParent(container, false);

        keyLabel           = go.AddComponent<TextMeshProUGUI>();
        keyLabel.text      = "◆ CLÉ";
        keyLabel.fontSize  = 36f;
        keyLabel.color     = ColKeyMissing;
        keyLabel.alignment = TextAlignmentOptions.Center;
        keyLabel.fontStyle = FontStyles.Bold;

        var rt        = keyLabel.rectTransform;
        rt.anchorMin  = new Vector2(0.30f, 0.895f);
        rt.anchorMax  = new Vector2(0.70f, 0.932f);
        rt.offsetMin  = rt.offsetMax = Vector2.zero;
    }

    private void BuildMenuButton()
    {
        var go  = new GameObject("MenuButton");
        go.transform.SetParent(container, false);

        var img   = go.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.55f);

        var rt        = img.rectTransform;
        rt.anchorMin  = new Vector2(0f, 0f);
        rt.anchorMax  = new Vector2(0.30f, 0.045f);
        rt.offsetMin  = new Vector2(16f, 16f);
        rt.offsetMax  = new Vector2(-8f, -8f);

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

        var textRT       = tmp.rectTransform;
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = textRT.offsetMax = Vector2.zero;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void MakeLabel(string name, string text, float fontSize, Color color,
                           Vector2 anchorMin, Vector2 anchorMax,
                           TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(container, false);

        var tmp        = go.AddComponent<TextMeshProUGUI>();
        tmp.text       = text;
        tmp.fontSize   = fontSize;
        tmp.color      = color;
        tmp.alignment  = alignment;

        var rt         = tmp.rectTransform;
        rt.anchorMin   = anchorMin;
        rt.anchorMax   = anchorMax;
        rt.offsetMin   = rt.offsetMax = Vector2.zero;
    }

    // ── Événements ────────────────────────────────────────────────────────────

    private void OnKeyCollected()
    {
        if (keyLabel) keyLabel.color = ColKeyCollected;
    }
}

