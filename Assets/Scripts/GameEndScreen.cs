using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Widget de fin de partie : DEFEAT + score + pièces gagnées + bouton menu.
///
/// Usage : attacher ce composant à un GameObject vide dans chaque scène de mini-jeu.
/// Il s'abonne à <see cref="GameManager.OnGameOver"/> et gère la transition vers le Menu.
///
/// Le flow complet :
///   1. GameOver déclenché → fond sombre slide-in, "DEFEAT" en rouge
///   2. Score animé (count-up)
///   3. Pièces gagnées animées
///   4. Bouton "MENU" cliquable + countdown auto
///   5. Écriture dans <see cref="GameEndData"/> → transition via <see cref="SceneTransition"/>
///   6. Au menu, <see cref="MenuCoinReceiver"/> récupère les données et crédite les pièces
/// </summary>
public class GameEndScreen : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [SerializeField] private GameType gameType       = GameType.GameAndWatch;
    [SerializeField] private float    autoReturnSec  = 6f;

    // ── Layout ────────────────────────────────────────────────────────────────

    private const float CardW = 860f;
    private const float CardH = 760f;

    // ── Palette ───────────────────────────────────────────────────────────────

    private static readonly Color ColScrim       = new Color(0.04f, 0.02f, 0.08f, 0.92f);
    private static readonly Color ColCard        = new Color(0.09f, 0.08f, 0.15f, 1.00f);
    private static readonly Color ColCardEdge    = new Color(0.80f, 0.10f, 0.10f, 0.90f);  // rouge defeat
    private static readonly Color ColDefeat      = new Color(0.95f, 0.15f, 0.15f, 1.00f);
    private static readonly Color ColSub         = new Color(1.00f, 1.00f, 1.00f, 0.35f);
    private static readonly Color ColScoreLbl    = new Color(1.00f, 1.00f, 1.00f, 0.45f);
    private static readonly Color ColScoreVal    = new Color(1.00f, 0.82f, 0.18f, 1.00f);
    private static readonly Color ColCoinsLbl    = new Color(1.00f, 1.00f, 1.00f, 0.45f);
    private static readonly Color ColCoinsVal    = new Color(0.30f, 0.95f, 0.45f, 1.00f);
    private static readonly Color ColSep         = new Color(1.00f, 1.00f, 1.00f, 0.10f);
    private static readonly Color ColBtnMenu     = new Color(0.12f, 0.12f, 0.22f, 1.00f);
    private static readonly Color ColBtnMenuHov  = new Color(0.20f, 0.20f, 0.35f, 1.00f);
    private static readonly Color ColBtnMenuTxt  = new Color(1.00f, 1.00f, 1.00f, 0.90f);
    private static readonly Color ColHint        = new Color(0.55f, 0.55f, 0.55f, 1.00f);
    private static readonly Color ColDivider     = new Color(0.80f, 0.10f, 0.10f, 0.50f);

    // ── Runtime ───────────────────────────────────────────────────────────────

    private Canvas          rootCanvas;
    private CanvasGroup     rootGroup;
    private TextMeshProUGUI scoreValLabel;
    private TextMeshProUGUI coinsValLabel;
    private TextMeshProUGUI hintLabel;
    private Button          menuButton;
    private bool            fired;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        Build();

        if (GameManager.Instance != null)
            GameManager.Instance.OnGameOver.AddListener(Trigger);
        else
            Debug.LogWarning("[GameEndScreen] GameManager introuvable dans la scène.");
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameOver.RemoveListener(Trigger);
    }

    // ── Point d'entrée public ─────────────────────────────────────────────────

    /// <summary>Déclenche le widget manuellement si nécessaire.</summary>
    public void Trigger()
    {
        if (fired) return;
        fired = true;

        int score = GameManager.Instance != null ? GameManager.Instance.CurrentScore : 0;
        int coins = Mathf.Max(1, score / 10);

        // Sauvegarder le score (sans créditer les pièces — MenuCoinReceiver le fera)
        ScoreManager.EnsureExists();
        ScoreManager.Instance.AddScoreOnly(gameType, score);

        // Stocker pour le Menu
        GameEndData.Set(score, coins);

        StartCoroutine(RunScreen(score, coins));
    }

    // ── Séquence principale ───────────────────────────────────────────────────

    private IEnumerator RunScreen(int score, int coins)
    {
        // Activer et fade-in
        rootCanvas.gameObject.SetActive(true);
        yield return StartCoroutine(FadeCanvasGroup(rootGroup, 0f, 1f, 0.30f));
        rootGroup.blocksRaycasts = true;

        // 1. Count-up score
        yield return StartCoroutine(CountUp(scoreValLabel, 0, score, 1.10f, isCoin: false));

        // 2. Pause + count-up pièces
        yield return new WaitForSecondsRealtime(0.45f);
        yield return StartCoroutine(CountUp(coinsValLabel, 0, coins, 0.75f, isCoin: true));

        // 3. Countdown auto-retour + écoute bouton
        bool clicked = false;
        menuButton.onClick.AddListener(() => clicked = true);

        float elapsed = 0f;
        while (elapsed < autoReturnSec && !clicked)
        {
            elapsed += Time.unscaledDeltaTime;
            int rem = Mathf.CeilToInt(autoReturnSec - elapsed);
            if (hintLabel != null)
                hintLabel.text = $"Retour automatique dans {rem}s";
            yield return null;
        }

        if (hintLabel != null)
            hintLabel.text = "Chargement…";

        yield return new WaitForSecondsRealtime(0.25f);

        GoToMenu();
    }

    private void GoToMenu()
    {
        string scene = MenuMainSetup.SceneName;
        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadScene(scene, scene);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(scene);
    }

    // ── Construction UI ───────────────────────────────────────────────────────

    private void Build()
    {
        // Canvas plein écran, au-dessus de tout
        var cgo          = new GameObject("GameEndCanvas");
        rootCanvas       = cgo.AddComponent<Canvas>();
        rootCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        rootCanvas.sortingOrder = 900;
        var scaler       = cgo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution  = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight   = 1f;
        cgo.AddComponent<GraphicRaycaster>();

        rootGroup                = cgo.AddComponent<CanvasGroup>();
        rootGroup.alpha          = 0f;
        rootGroup.blocksRaycasts = false;
        cgo.SetActive(false);

        var root = cgo.GetComponent<RectTransform>();

        // ── Fond sombre ───────────────────────────────────────────────────────
        Stretch("Scrim", root, ColScrim, raycast: false);

        // ── Card centrale ─────────────────────────────────────────────────────
        var card   = CenteredRect("Card", root, new Vector2(0.5f, 0.52f), CardW, CardH);
        Bg(card, ColCard);

        // Bordure rouge gauche (accent defeat)
        var edge = new GameObject("EdgeAccent");
        edge.transform.SetParent(card, false);
        var edgeImg = edge.AddComponent<Image>();
        edgeImg.sprite = SpriteGenerator.CreateWhiteSquare();
        edgeImg.color  = ColCardEdge;
        edgeImg.raycastTarget = false;
        var edgeRT = edgeImg.rectTransform;
        edgeRT.anchorMin = new Vector2(0f, 0f);
        edgeRT.anchorMax = new Vector2(0.012f, 1f);
        edgeRT.offsetMin = edgeRT.offsetMax = Vector2.zero;

        // ── DEFEAT ───────────────────────────────────────────────────────────
        Label(card, "Defeat", "DEFEAT",
            new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.97f),
            96f, ColDefeat, FontStyles.Bold | FontStyles.UpperCase);

        // Sous-titre
        Label(card, "Sub", "PARTIE TERMINÉE",
            new Vector2(0.05f, 0.76f), new Vector2(0.95f, 0.84f),
            26f, ColSub, FontStyles.Normal);

        // Divider rouge
        HLine(card, 0.748f, ColDivider);

        // ── Section SCORE ─────────────────────────────────────────────────────
        Label(card, "ScoreLbl", "SCORE",
            new Vector2(0.08f, 0.60f), new Vector2(0.50f, 0.72f),
            28f, ColScoreLbl, FontStyles.Bold);
        scoreValLabel = Label(card, "ScoreVal", "0",
            new Vector2(0.08f, 0.44f), new Vector2(0.50f, 0.63f),
            86f, ColScoreVal, FontStyles.Bold);

        // ── Section PIÈCES ───────────────────────────────────────────────────
        Label(card, "CoinsLbl", "PIÈCES GAGNÉES",
            new Vector2(0.52f, 0.60f), new Vector2(0.95f, 0.72f),
            26f, ColCoinsLbl, FontStyles.Bold);
        coinsValLabel = Label(card, "CoinsVal", "0 🪙",
            new Vector2(0.52f, 0.44f), new Vector2(0.95f, 0.63f),
            72f, ColCoinsVal, FontStyles.Bold);

        // Séparateur vertical entre score et pièces
        VLine(card, 0.50f, ColSep);

        // Divider bas
        HLine(card, 0.42f, ColDivider);

        // ── Bouton MENU ───────────────────────────────────────────────────────
        var btnRT      = CenteredRect("BtnMenu", card, new Vector2(0.50f, 0.275f), 680f, 88f);
        var btnImg     = btnRT.gameObject.AddComponent<Image>();
        btnImg.sprite  = SpriteGenerator.CreateWhiteSquare();
        btnImg.color   = ColBtnMenu;
        menuButton     = btnRT.gameObject.AddComponent<Button>();
        menuButton.targetGraphic = btnImg;
        var colors     = menuButton.colors;
        colors.normalColor      = Color.white;
        colors.highlightedColor = new Color(1.4f, 1.4f, 1.4f, 1f);
        colors.pressedColor     = new Color(0.7f, 0.7f, 0.7f, 1f);
        colors.fadeDuration     = 0.08f;
        menuButton.colors = colors;

        // Bordure bouton
        var btnBorder = new GameObject("BtnBorder");
        btnBorder.transform.SetParent(btnRT, false);
        var bbi = btnBorder.AddComponent<Image>();
        bbi.sprite = SpriteGenerator.CreateWhiteSquare();
        bbi.color  = new Color(1f, 1f, 1f, 0.12f);
        bbi.raycastTarget = false;
        var bbiRT = bbi.rectTransform;
        bbiRT.anchorMin = Vector2.zero;
        bbiRT.anchorMax = Vector2.one;
        bbiRT.offsetMin = new Vector2(-2f, -2f);
        bbiRT.offsetMax = new Vector2( 2f,  2f);

        Label(btnRT, "BtnLabel", "RETOUR AU MENU",
            Vector2.zero, Vector2.one, 34f, ColBtnMenuTxt, FontStyles.Bold);

        // ── Hint countdown ────────────────────────────────────────────────────
        var hintGO = new GameObject("Hint");
        hintGO.transform.SetParent(card, false);
        hintLabel              = hintGO.AddComponent<TextMeshProUGUI>();
        hintLabel.text         = $"Retour automatique dans {Mathf.CeilToInt(autoReturnSec)}s";
        hintLabel.fontSize     = 22f;
        hintLabel.color        = ColHint;
        hintLabel.alignment    = TextAlignmentOptions.Center;
        hintLabel.raycastTarget = false;
        var hintRT = hintLabel.rectTransform;
        hintRT.anchorMin = new Vector2(0.05f, 0.08f);
        hintRT.anchorMax = new Vector2(0.95f, 0.18f);
        hintRT.offsetMin = hintRT.offsetMax = Vector2.zero;
    }

    // ── Helpers de construction ───────────────────────────────────────────────

    private static Image Stretch(string name, RectTransform parent, Color col, bool raycast = true)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = SpriteGenerator.CreateWhiteSquare();
        img.color  = col;
        img.raycastTarget = raycast;
        var rt   = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return img;
    }

    private static void Bg(RectTransform parent, Color col)
    {
        var go  = new GameObject("Bg");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = SpriteGenerator.CreateWhiteSquare();
        img.color  = col;
        img.raycastTarget = false;
        var rt   = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static RectTransform CenteredRect(string name, RectTransform parent,
        Vector2 anchor, float w, float h)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = Vector2.zero;
        return rt;
    }

    private static TextMeshProUGUI Label(RectTransform parent, string name, string text,
        Vector2 aMin, Vector2 aMax, float size, Color col, FontStyles style)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text             = text;
        tmp.fontSize         = size;
        tmp.fontStyle        = style;
        tmp.color            = col;
        tmp.alignment        = TextAlignmentOptions.Center;
        tmp.raycastTarget    = false;
        tmp.enableAutoSizing = false;
        var rt = tmp.rectTransform;
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return tmp;
    }

    private static void HLine(RectTransform parent, float anchorY, Color col)
    {
        var go  = new GameObject("HLine");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = SpriteGenerator.CreateWhiteSquare();
        img.color  = col;
        img.raycastTarget = false;
        var rt   = img.rectTransform;
        rt.anchorMin = new Vector2(0.04f, anchorY);
        rt.anchorMax = new Vector2(0.96f, anchorY + 0.004f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static void VLine(RectTransform parent, float anchorX, Color col)
    {
        var go  = new GameObject("VLine");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = SpriteGenerator.CreateWhiteSquare();
        img.color  = col;
        img.raycastTarget = false;
        var rt   = img.rectTransform;
        rt.anchorMin = new Vector2(anchorX - 0.003f, 0.44f);
        rt.anchorMax = new Vector2(anchorX + 0.003f, 0.72f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // ── Coroutines ────────────────────────────────────────────────────────────

    private static IEnumerator FadeCanvasGroup(CanvasGroup g, float from, float to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t      += Time.unscaledDeltaTime;
            g.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / dur));
            yield return null;
        }
        g.alpha = to;
    }

    private static IEnumerator CountUp(TextMeshProUGUI lbl, int from, int to, float dur, bool isCoin)
    {
        if (lbl == null) yield break;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / dur), 4f);  // EaseOutQuart
            int   v = Mathf.RoundToInt(Mathf.Lerp(from, to, e));
            lbl.text = isCoin ? $"+{v} 🪙" : v.ToString("N0");
            yield return null;
        }
        lbl.text = isCoin ? $"+{to} 🪙" : to.ToString("N0");
    }
}
