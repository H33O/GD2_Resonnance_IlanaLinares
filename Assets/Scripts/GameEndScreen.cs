using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Widget de fin de partie : DEFEAT + score + retour menu auto.
///
/// Usage : attacher ce composant à un GameObject vide dans chaque scène de mini-jeu.
/// Il s'abonne à <see cref="GameManager.OnGameOver"/> et gère la transition vers le Menu.
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
    private static readonly Color ColCardEdge    = new Color(0.80f, 0.10f, 0.10f, 0.90f);
    private static readonly Color ColDefeat      = new Color(0.95f, 0.15f, 0.15f, 1.00f);
    private static readonly Color ColSub         = new Color(1.00f, 1.00f, 1.00f, 1.00f);
    private static readonly Color ColScoreLbl    = new Color(1.00f, 1.00f, 1.00f, 1.00f);
    private static readonly Color ColScoreVal    = new Color(1.00f, 0.82f, 0.18f, 1.00f);
    private static readonly Color ColHint        = new Color(1.00f, 1.00f, 1.00f, 0.70f);
    private static readonly Color ColDivider     = new Color(0.80f, 0.10f, 0.10f, 0.50f);

    // ── Runtime ───────────────────────────────────────────────────────────────

    private Canvas          rootCanvas;
    private CanvasGroup     rootGroup;
    private TextMeshProUGUI scoreValLabel;
    private TextMeshProUGUI hintLabel;
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

    // ── Debug ─────────────────────────────────────────────────────────────────

    private void Update()
    {
    }

    // ── Point d'entrée public ─────────────────────────────────────────────────

    /// <summary>Déclenche le widget manuellement si nécessaire.</summary>
    public void Trigger()
    {
        if (fired) return;
        fired = true;

        int score = GameManager.Instance != null ? GameManager.Instance.CurrentScore : 0;

        ScoreManager.EnsureExists();
        ScoreManager.Instance.AddScoreOnly(gameType, score);

        GameEndData.Set(score, gameType);

        StartCoroutine(RunScreen(score));
    }

    // ── Séquence principale ───────────────────────────────────────────────────

    private IEnumerator RunScreen(int score)
    {
        rootCanvas.gameObject.SetActive(true);
        yield return StartCoroutine(FadeCanvasGroup(rootGroup, 0f, 1f, 0.30f));
        rootGroup.blocksRaycasts = true;

        yield return StartCoroutine(CountUp(scoreValLabel, 0, score, 1.10f));

        float elapsed = 0f;
        while (elapsed < autoReturnSec)
        {
            elapsed += Time.unscaledDeltaTime;
            int rem = Mathf.CeilToInt(autoReturnSec - elapsed);
            if (hintLabel != null)
                hintLabel.text = $"Retour au menu dans {rem}s — appuie sur Game pour rejouer";
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
            new Vector2(0.08f, 0.60f), new Vector2(0.95f, 0.72f),
            28f, ColScoreLbl, FontStyles.Bold);
        scoreValLabel = Label(card, "ScoreVal", "0",
            new Vector2(0.08f, 0.44f), new Vector2(0.95f, 0.63f),
            86f, ColScoreVal, FontStyles.Bold);

        // Divider bas
        HLine(card, 0.42f, ColDivider);

        // ── Hint countdown ────────────────────────────────────────────────────
        var hintGO = new GameObject("Hint");
        hintGO.transform.SetParent(card, false);
        hintLabel              = hintGO.AddComponent<TextMeshProUGUI>();
        hintLabel.text         = $"Retour au menu dans {Mathf.CeilToInt(autoReturnSec)}s — appuie sur Game pour rejouer";
        hintLabel.fontSize     = 24f;
        hintLabel.color        = ColHint;
        hintLabel.alignment    = TextAlignmentOptions.Center;
        hintLabel.raycastTarget = false;
        hintLabel.enableWordWrapping = true;
        var hintRT = hintLabel.rectTransform;
        hintRT.anchorMin = new Vector2(0.05f, 0.20f);
        hintRT.anchorMax = new Vector2(0.95f, 0.40f);
        hintRT.offsetMin = hintRT.offsetMax = Vector2.zero;

        // ── Bouton MENU (retour) ──────────────────────────────────────────────
        MakeReturnButton(card);
    }

    private void MakeReturnButton(RectTransform parent)
    {
        var go = new GameObject("ReturnButton");
        go.transform.SetParent(parent, false);

        var img    = go.AddComponent<Image>();
        img.sprite = SpriteGenerator.CreateWhiteSquare();
        img.color  = new Color(0.80f, 0.10f, 0.10f, 0.90f);

        var rt     = img.rectTransform;
        rt.anchorMin      = new Vector2(0.10f, 0.04f);
        rt.anchorMax      = new Vector2(0.90f, 0.18f);
        rt.offsetMin      = rt.offsetMax = Vector2.zero;

        var lgo  = new GameObject("Label");
        lgo.transform.SetParent(rt, false);
        var ltmp = lgo.AddComponent<TextMeshProUGUI>();
        ltmp.text          = "← RETOUR AU MENU";
        ltmp.fontSize      = 30f;
        ltmp.fontStyle     = FontStyles.Bold;
        ltmp.color         = Color.white;
        ltmp.alignment     = TextAlignmentOptions.Center;
        ltmp.raycastTarget = false;
        var lrt  = ltmp.rectTransform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;

        var btn           = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(GoToMenu);
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

    private static IEnumerator CountUp(TextMeshProUGUI lbl, int from, int to, float dur)
    {
        if (lbl == null) yield break;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / dur), 4f);
            int   v = Mathf.RoundToInt(Mathf.Lerp(from, to, e));
            lbl.text = v.ToString("N0");
            yield return null;
        }
        lbl.text = to.ToString("N0");
    }
}
