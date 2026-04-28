using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Widget de fin de partie : DEFEAT + score + bloc XP inline + retour menu auto.
///
/// Le bloc XP affiche le niveau par jeu directement dans la carte defeat,
/// sans passer par le menu. L'XP est créditée et animée immédiatement.
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
    private const float CardH = 980f;  // agrandi pour le bloc XP

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
    private static readonly Color ColXPBg        = new Color(0.06f, 0.14f, 0.06f, 0.95f);
    private static readonly Color ColXPTitle     = new Color(1.00f, 1.00f, 1.00f, 0.50f);
    private static readonly Color ColXPValue     = new Color(0.40f, 1.00f, 0.55f, 1.00f);
    private static readonly Color ColXPLevelUp   = new Color(1.00f, 0.90f, 0.10f, 1.00f);
    private static readonly Color ColBarBg       = new Color(0.05f, 0.05f, 0.05f, 0.60f);
    private static readonly Color ColBarFill     = new Color(0.25f, 0.85f, 0.40f, 1.00f);

    // ── Runtime ───────────────────────────────────────────────────────────────

    private Canvas          rootCanvas;
    private CanvasGroup     rootGroup;
    private TextMeshProUGUI scoreValLabel;
    private TextMeshProUGUI hintLabel;
    private TextMeshProUGUI xpLevelLabel;
    private TextMeshProUGUI xpCounterLabel;
    private Image           xpBarFill;
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

        // XP par jeu — paliers fixes pour GAW
        int xp;
        if (gameType == GameType.GameAndWatch)
            xp = score >= 50 ? 50 : 25;
        else
            xp = GameEndData.ComputeXP(score);

        // On écrit aussi dans GameEndData pour rétrocompat (MenuXPReceiver ignoré désormais)
        GameEndData.SetWithXP(score, xp, gameType);

        StartCoroutine(RunScreen(score, xp));
    }

    // ── Séquence principale ───────────────────────────────────────────────────

    private IEnumerator RunScreen(int score, int xpToGrant)
    {
        rootCanvas.gameObject.SetActive(true);
        yield return StartCoroutine(FadeCanvasGroup(rootGroup, 0f, 1f, 0.30f));
        rootGroup.blocksRaycasts = true;

        yield return StartCoroutine(CountUp(scoreValLabel, 0, score, 1.10f));

        // ── Animation XP inline ───────────────────────────────────────────────
        yield return StartCoroutine(AnimateXPGain(gameType, xpToGrant));

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

    /// <summary>
    /// Crédite l'XP dans <see cref="GameLevelManager"/> et anime la barre + compteur inline.
    /// </summary>
    private IEnumerator AnimateXPGain(GameType gt, int xpToGrant)
    {
        GameLevelManager.EnsureExists();
        var glm = GameLevelManager.Instance;

        int levelBefore = glm.GetLevel(gt);
        int xpBefore    = glm.GetCurrentXP(gt);

        // Créditer l'XP
        int levelsGained = glm.AddXP(gt, xpToGrant);

        int levelAfter = glm.GetLevel(gt);
        int xpAfter    = glm.GetCurrentXP(gt);

        // Mise à jour immédiate du label niveau
        if (xpLevelLabel != null)
            xpLevelLabel.text = $"NIV {levelAfter}";

        // Gestion montée de niveau : on anime la barre qui se remplit d'abord
        float fillTarget = xpAfter / (float)GameLevelManager.XPPerLevel;

        // Si on a monté de niveau, animer jusqu'à 1 puis revenir à 0
        if (levelsGained > 0)
        {
            // Remplir jusqu'à la fin
            yield return StartCoroutine(AnimateBar(xpBefore / (float)GameLevelManager.XPPerLevel, 1f, 0.45f));

            // Flash LEVEL UP
            if (xpLevelLabel != null)
            {
                xpLevelLabel.text  = "LEVEL UP !";
                xpLevelLabel.color = ColXPLevelUp;
            }
            yield return new WaitForSecondsRealtime(0.50f);

            // Reset barre puis remplir jusqu'au xp résiduel
            if (xpBarFill != null)
            {
                var rt = xpBarFill.rectTransform;
                rt.anchorMax = new Vector2(0f, rt.anchorMax.y);
            }

            yield return StartCoroutine(AnimateBar(0f, fillTarget, 0.35f));

            if (xpLevelLabel != null)
            {
                xpLevelLabel.text  = $"NIV {levelAfter}";
                xpLevelLabel.color = ColXPValue;
            }
        }
        else
        {
            yield return StartCoroutine(AnimateBar(
                xpBefore / (float)GameLevelManager.XPPerLevel, fillTarget, 0.55f));
        }

        // Compte à rebours XP
        if (xpCounterLabel != null)
            xpCounterLabel.text = $"{xpAfter} / {GameLevelManager.XPPerLevel} XP";
    }

    private IEnumerator AnimateBar(float from, float to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / dur), 3f);
            float v = Mathf.Lerp(from, to, e);

            if (xpBarFill != null)
            {
                var rt = xpBarFill.rectTransform;
                rt.anchorMax = new Vector2(Mathf.Clamp01(v), rt.anchorMax.y);
            }

            if (xpCounterLabel != null)
            {
                int displayed = Mathf.RoundToInt(v * GameLevelManager.XPPerLevel);
                xpCounterLabel.text = $"{displayed} / {GameLevelManager.XPPerLevel} XP";
            }

            yield return null;
        }
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
            new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.97f),
            96f, ColDefeat, FontStyles.Bold | FontStyles.UpperCase);

        // Sous-titre
        Label(card, "Sub", "PARTIE TERMINÉE",
            new Vector2(0.05f, 0.79f), new Vector2(0.95f, 0.86f),
            26f, ColSub, FontStyles.Normal);

        // Divider rouge
        HLine(card, 0.77f, ColDivider);

        // ── Section SCORE ─────────────────────────────────────────────────────
        Label(card, "ScoreLbl", "SCORE",
            new Vector2(0.08f, 0.64f), new Vector2(0.95f, 0.74f),
            28f, ColScoreLbl, FontStyles.Bold);
        scoreValLabel = Label(card, "ScoreVal", "0",
            new Vector2(0.08f, 0.50f), new Vector2(0.95f, 0.66f),
            86f, ColScoreVal, FontStyles.Bold);

        // Divider bas score
        HLine(card, 0.48f, ColDivider);

        // ── Bloc XP inline ────────────────────────────────────────────────────
        BuildXPBlock(card);

        // Divider bas XP
        HLine(card, 0.22f, ColDivider);

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
        hintRT.anchorMin = new Vector2(0.05f, 0.13f);
        hintRT.anchorMax = new Vector2(0.95f, 0.22f);
        hintRT.offsetMin = hintRT.offsetMax = Vector2.zero;

        // ── Bouton MENU (retour) ──────────────────────────────────────────────
        MakeReturnButton(card);
    }

    /// <summary>Construit le bloc XP (niveau + barre + compteur) dans la card defeat.</summary>
    private void BuildXPBlock(RectTransform card)
    {
        // Fond vert sombre
        var bg    = new GameObject("XPBg");
        bg.transform.SetParent(card, false);
        var bgImg = bg.AddComponent<Image>();
        bgImg.sprite = SpriteGenerator.CreateWhiteSquare();
        bgImg.color  = ColXPBg;
        bgImg.raycastTarget = false;
        var bgRT = bgImg.rectTransform;
        bgRT.anchorMin = new Vector2(0.04f, 0.23f);
        bgRT.anchorMax = new Vector2(0.96f, 0.47f);
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

        // Titre "EXPÉRIENCE"
        var titleGO  = new GameObject("XPTitle");
        titleGO.transform.SetParent(card, false);
        var titleTmp        = titleGO.AddComponent<TextMeshProUGUI>();
        titleTmp.text       = "EXPÉRIENCE";
        titleTmp.fontSize   = 22f;
        titleTmp.fontStyle  = FontStyles.Bold;
        titleTmp.color      = ColXPTitle;
        titleTmp.alignment  = TextAlignmentOptions.Left;
        titleTmp.raycastTarget = false;
        var titleRT = titleTmp.rectTransform;
        titleRT.anchorMin = new Vector2(0.08f, 0.41f);
        titleRT.anchorMax = new Vector2(0.60f, 0.47f);
        titleRT.offsetMin = titleRT.offsetMax = Vector2.zero;

        // Niveau courant (mis à jour dynamiquement)
        GameLevelManager.EnsureExists();
        int currentLevel = GameLevelManager.Instance.GetLevel(gameType);

        var lvlGO  = new GameObject("XPLevel");
        lvlGO.transform.SetParent(card, false);
        xpLevelLabel          = lvlGO.AddComponent<TextMeshProUGUI>();
        xpLevelLabel.text     = $"NIV {currentLevel}";
        xpLevelLabel.fontSize = 48f;
        xpLevelLabel.fontStyle = FontStyles.Bold;
        xpLevelLabel.color    = ColXPValue;
        xpLevelLabel.alignment = TextAlignmentOptions.Left;
        xpLevelLabel.raycastTarget = false;
        var lvlRT = xpLevelLabel.rectTransform;
        lvlRT.anchorMin = new Vector2(0.08f, 0.28f);
        lvlRT.anchorMax = new Vector2(0.55f, 0.43f);
        lvlRT.offsetMin = lvlRT.offsetMax = Vector2.zero;

        // Compteur XP (mis à jour dynamiquement)
        int currentXP = GameLevelManager.Instance.GetCurrentXP(gameType);

        var ctrGO  = new GameObject("XPCounter");
        ctrGO.transform.SetParent(card, false);
        xpCounterLabel          = ctrGO.AddComponent<TextMeshProUGUI>();
        xpCounterLabel.text     = $"{currentXP} / {GameLevelManager.XPPerLevel} XP";
        xpCounterLabel.fontSize = 28f;
        xpCounterLabel.fontStyle = FontStyles.Normal;
        xpCounterLabel.color    = ColXPTitle;
        xpCounterLabel.alignment = TextAlignmentOptions.Right;
        xpCounterLabel.raycastTarget = false;
        var ctrRT = xpCounterLabel.rectTransform;
        ctrRT.anchorMin = new Vector2(0.50f, 0.28f);
        ctrRT.anchorMax = new Vector2(0.93f, 0.43f);
        ctrRT.offsetMin = ctrRT.offsetMax = Vector2.zero;

        // Fond barre XP
        var barBgGO  = new GameObject("XPBarBg");
        barBgGO.transform.SetParent(card, false);
        var barBgImg        = barBgGO.AddComponent<Image>();
        barBgImg.sprite     = SpriteGenerator.CreateWhiteSquare();
        barBgImg.color      = ColBarBg;
        barBgImg.raycastTarget = false;
        var barBgRT = barBgImg.rectTransform;
        barBgRT.anchorMin = new Vector2(0.06f, 0.23f);
        barBgRT.anchorMax = new Vector2(0.94f, 0.28f);
        barBgRT.offsetMin = barBgRT.offsetMax = Vector2.zero;

        // Remplissage barre XP (anchorMax.x animé dynamiquement)
        var barFillGO  = new GameObject("XPBarFill");
        barFillGO.transform.SetParent(card, false);
        xpBarFill           = barFillGO.AddComponent<Image>();
        xpBarFill.sprite    = SpriteGenerator.CreateWhiteSquare();
        xpBarFill.color     = ColBarFill;
        xpBarFill.raycastTarget = false;
        var barFillRT = xpBarFill.rectTransform;
        float initialRatio = GameLevelManager.Instance.GetCurrentXP(gameType) / (float)GameLevelManager.XPPerLevel;
        barFillRT.anchorMin = new Vector2(0.06f, 0.23f);
        barFillRT.anchorMax = new Vector2(0.06f + (0.94f - 0.06f) * initialRatio, 0.28f);
        barFillRT.offsetMin = barFillRT.offsetMax = Vector2.zero;
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
