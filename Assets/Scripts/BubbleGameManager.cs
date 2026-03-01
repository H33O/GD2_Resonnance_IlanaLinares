using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Gère l'état du jeu (score, coups, victoire/défaite) et crée l'UI automatiquement.
/// </summary>
public class BubbleGameManager : MonoBehaviour
{
    public static BubbleGameManager Instance { get; private set; }

    [Header("Règles")]
    [SerializeField] private int maxShots = 30;
    [SerializeField] private int targetScore = 500;

    public int Score { get; private set; }
    public int ShotsLeft { get; private set; }
    public bool IsGameActive { get; private set; }

    // UI
    private TextMeshProUGUI scoreText;
    private TextMeshProUGUI shotsText;
    private TextMeshProUGUI statusText;

    private void Awake()
    {
        Instance = this;
        ShotsLeft = maxShots;
        IsGameActive = true;
        CreateUI();
    }

    // ── Score & coups ─────────────────────────────────────────────────────────

    /// <summary>Ajoute des points au score.</summary>
    public void AddScore(int amount)
    {
        if (!IsGameActive) return;
        Score += amount;
        RefreshUI();
        if (Score >= targetScore) EndGame(true);
    }

    /// <summary>Consomme un coup. Retourne false si plus de coups ou jeu terminé.</summary>
    public bool TryShoot()
    {
        if (!IsGameActive || ShotsLeft <= 0) return false;
        ShotsLeft--;
        RefreshUI();
        return true;
    }

    /// <summary>Vérifie les conditions de victoire/défaite après chaque placement.</summary>
    public void CheckEnd()
    {
        if (!IsGameActive) return;
        if (BubbleGrid.Instance.IsEmpty()) EndGame(true);
        else if (ShotsLeft <= 0 && Score < targetScore) EndGame(false);
    }

    // ── Fin de partie ─────────────────────────────────────────────────────────

    private void EndGame(bool win)
    {
        IsGameActive = false;
        statusText.text = win ? "Victoire !" : "Défaite";
        statusText.gameObject.SetActive(true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ── UI créée automatiquement ──────────────────────────────────────────────

    private void CreateUI()
    {
        var canvasGO = new GameObject("BubbleCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        canvasGO.AddComponent<GraphicRaycaster>();

        Transform ct = canvas.transform;

        // — Score (haut gauche)
        scoreText = MakeText(ct, "Score : 0", new Vector2(20, -20), new Vector2(300, 55), TextAlignmentOptions.TopLeft);
        scoreText.fontSize = 30;

        // — Objectif (haut gauche, sous le score)
        var obj = MakeText(ct, $"Objectif : {targetScore}", new Vector2(20, -80), new Vector2(300, 45), TextAlignmentOptions.TopLeft);
        obj.fontSize = 22;
        obj.color = new Color(1f, 0.9f, 0.4f);

        // — Coups restants (bas, centré, bien visible)
        shotsText = MakeShotsPanel(ct);

        // — Message victoire / défaite (centre écran)
        statusText = MakeText(ct, "", Vector2.zero, new Vector2(500, 120), TextAlignmentOptions.Center, center: true);
        statusText.fontSize = 55;
        statusText.gameObject.SetActive(false);
    }

    private void RefreshUI()
    {
        scoreText.text = $"Score : {Score}";
        shotsText.text = $"{ShotsLeft}";
    }

    // Panneau coups restants : encadré centré en bas de l'écran
    private TextMeshProUGUI MakeShotsPanel(Transform parent)
    {
        // Fond semi-transparent
        var bg = new GameObject("ShotsPanel");
        bg.transform.SetParent(parent, false);
        var bgRT = bg.AddComponent<RectTransform>();
        bgRT.anchorMin = bgRT.anchorMax = new Vector2(0.5f, 0f); // bas centré
        bgRT.sizeDelta = new Vector2(260, 90);
        bgRT.anchoredPosition = new Vector2(0f, 50f);
        var bgImg = bg.AddComponent<UnityEngine.UI.Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.55f);

        // Label "COUPS"
        var label = new GameObject("Label");
        label.transform.SetParent(bg.transform, false);
        var labelRT = label.AddComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0f, 0.5f);
        labelRT.anchorMax = new Vector2(1f, 1f);
        labelRT.offsetMin = labelRT.offsetMax = Vector2.zero;
        var labelTMP = label.AddComponent<TextMeshProUGUI>();
        labelTMP.text = "COUPS RESTANTS";
        labelTMP.fontSize = 14;
        labelTMP.color = new Color(1f, 1f, 1f, 0.7f);
        labelTMP.alignment = TextAlignmentOptions.Center;

        // Nombre de coups (grand, en bas du panneau)
        var count = new GameObject("Count");
        count.transform.SetParent(bg.transform, false);
        var countRT = count.AddComponent<RectTransform>();
        countRT.anchorMin = new Vector2(0f, 0f);
        countRT.anchorMax = new Vector2(1f, 0.58f);
        countRT.offsetMin = countRT.offsetMax = Vector2.zero;
        var countTMP = count.AddComponent<TextMeshProUGUI>();
        countTMP.text = $"{maxShots}";
        countTMP.fontSize = 42;
        countTMP.fontStyle = FontStyles.Bold;
        countTMP.color = Color.white;
        countTMP.alignment = TextAlignmentOptions.Center;

        return countTMP;
    }

    private TextMeshProUGUI MakeText(Transform parent, string text, Vector2 pos, Vector2 size,
                                     TextAlignmentOptions align, bool center = false)
    {
        var go = new GameObject("TMP");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();

        if (center)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        }
        else
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f); // coin haut-gauche
        }

        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 28;
        tmp.color = Color.white;
        tmp.alignment = align;
        return tmp;
    }
}
