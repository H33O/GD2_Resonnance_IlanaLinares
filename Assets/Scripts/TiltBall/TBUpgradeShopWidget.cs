using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Widget de boutique d'améliorations affiché entre deux niveaux TiltBall.
/// Propose 3 améliorations achetables avec le score accumulé :
///   - Allié    : un allié suit le joueur et détruit les ennemis au contact
///   - Arme     : tire automatiquement sur les ennemis proches
///   - Barrière : place des obstacles qui bloquent les ennemis
///
/// Appelle <paramref name="onContinue"/> quand le joueur choisit de continuer.
/// </summary>
public class TBUpgradeShopWidget : MonoBehaviour
{
    // ── Palette ───────────────────────────────────────────────────────────────

    private static readonly Color ColOverlay    = new Color(0f,    0f,    0f,    0.72f);
    private static readonly Color ColPanel      = new Color(0.05f, 0.05f, 0.10f, 0.97f);
    private static readonly Color ColTitle      = new Color(1f,    0.85f, 0.10f, 1f);
    private static readonly Color ColScore      = Color.white;
    private static readonly Color ColBtnBuy     = new Color(0.10f, 0.65f, 0.35f, 1f);
    private static readonly Color ColBtnBuyTxt  = Color.white;
    private static readonly Color ColBtnOwned   = new Color(0.20f, 0.20f, 0.30f, 1f);
    private static readonly Color ColBtnOwnedTxt= new Color(1f,    1f,    1f,    0.40f);
    private static readonly Color ColBtnCont    = new Color(0.15f, 0.15f, 0.28f, 1f);
    private static readonly Color ColBtnContTxt = Color.white;
    private static readonly Color ColDesc       = new Color(1f,    1f,    1f,    0.65f);
    private static readonly Color ColSep        = new Color(1f,    1f,    1f,    0.12f);

    // ── Références ────────────────────────────────────────────────────────────

    private TBUpgradeData upgrades;
    private Action        onContinue;
    private GameObject    selfRoot;
    private TextMeshProUGUI scoreLabel;

    // ── Référence aux labels de coût (mis à jour après chaque achat) ─────────

    private TextMeshProUGUI allyStatusLabel;
    private TextMeshProUGUI barrierStatusLabel;
    private TextMeshProUGUI weaponStatusLabel;

    // ── API statique ──────────────────────────────────────────────────────────

    /// <summary>
    /// Crée et affiche le widget de boutique.
    /// </summary>
    /// <param name="upgrades">Référence aux données d'améliorations à modifier.</param>
    /// <param name="currentScore">Score courant (passé par référence via le GameManager).</param>
    /// <param name="onContinue">Callback déclenché quand le joueur continue.</param>
    public static TBUpgradeShopWidget Show(TBUpgradeData upgrades, Action onContinue)
    {
        var canvasGO = new GameObject("UpgradeShop");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;

        var scaler                 = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight  = 1f;

        canvasGO.AddComponent<GraphicRaycaster>();

        var widget          = canvasGO.AddComponent<TBUpgradeShopWidget>();
        widget.upgrades     = upgrades;
        widget.onContinue   = onContinue;
        widget.selfRoot     = canvasGO;

        widget.Build(canvas.GetComponent<RectTransform>());
        return widget;
    }

    // ── Construction ──────────────────────────────────────────────────────────

    private void Build(RectTransform root)
    {
        int levelIndex = TBGameManager.Instance?.LevelIndex ?? 0;

        // Fond semi-transparent
        MakeBg("Overlay", root, Vector2.zero, Vector2.one, ColOverlay);

        // Panneau principal centré
        var panel = MakePanel("Panel", root,
            new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.95f), ColPanel);

        // Titre
        MakeLabel("Title", panel, "AMÉLIORATIONS", 56f, ColTitle, FontStyles.Bold,
            new Vector2(0f, 0.90f), new Vector2(1f, 0.99f));

        // Score actuel
        scoreLabel = MakeTmp("ScoreLabel", panel,
            ScoreText(), 36f, ColScore, FontStyles.Normal,
            new Vector2(0f, 0.83f), new Vector2(1f, 0.92f));
        scoreLabel.alignment = TextAlignmentOptions.Center;

        // Séparateur
        MakeSep("Sep0", panel, new Vector2(0.05f, 0.81f), new Vector2(0.95f, 0.82f));

        // Trois cartes d'amélioration — centrées, sans bouton à droite
        bool allyLocked    = !TBUpgradeData.IsAllyUnlocked(levelIndex);
        bool barrierLocked = !TBUpgradeData.IsBarrierUnlocked(levelIndex);
        bool weaponLocked  = !TBUpgradeData.IsWeaponUnlocked(levelIndex);

        string allyDesc    = allyLocked
            ? $"Débloqué au niveau {TBUpgradeData.UnlockLevelAlly + 1}"
            : "Suit le joueur · détruit les ennemis au contact";
        string barrierDesc = barrierLocked
            ? $"Débloqué au niveau {TBUpgradeData.UnlockLevelBarrier + 1}"
            : "Murs défensifs qui bloquent les ennemis";
        string weaponDesc  = weaponLocked
            ? $"Débloqué au niveau {TBUpgradeData.UnlockLevelWeapon + 1}"
            : "Tir automatique vers l'ennemi le plus proche";

        BuildUpgradeCard(panel, "ALLIÉ", allyDesc,
            $"{TBUpgradeData.CostAlly} pts  ({upgrades.AllyCount}/{TBUpgradeData.MaxAllies})",
            new Vector2(0.03f, 0.56f), new Vector2(0.97f, 0.80f),
            out allyStatusLabel, OnBuyAlly, allyLocked);

        MakeSep("Sep1", panel, new Vector2(0.05f, 0.54f), new Vector2(0.95f, 0.55f));

        BuildUpgradeCard(panel, "BARRIÈRE", barrierDesc,
            $"{TBUpgradeData.CostBarrier} pts  ({upgrades.BarrierCount}/{TBUpgradeData.MaxBarriers})",
            new Vector2(0.03f, 0.30f), new Vector2(0.97f, 0.53f),
            out barrierStatusLabel, OnBuyBarrier, barrierLocked);

        MakeSep("Sep2", panel, new Vector2(0.05f, 0.28f), new Vector2(0.95f, 0.29f));

        BuildUpgradeCard(panel, "ARME", weaponDesc,
            $"{TBUpgradeData.CostWeapon} pts",
            new Vector2(0.03f, 0.06f), new Vector2(0.97f, 0.27f),
            out weaponStatusLabel, OnBuyWeapon, weaponLocked);

        // Un seul bouton Continuer — sous le panneau
        var btnContGO = new GameObject("BtnContinue");
        btnContGO.transform.SetParent(root, false);
        var btnContRT       = btnContGO.AddComponent<RectTransform>();
        btnContRT.anchorMin = new Vector2(0.08f, 0.02f);
        btnContRT.anchorMax = new Vector2(0.92f, 0.11f);
        btnContRT.offsetMin = btnContRT.offsetMax = Vector2.zero;

        var btnContImg   = btnContGO.AddComponent<Image>();
        btnContImg.color = ColBtnCont;
        TBUIStyle.ApplyJauge(btnContImg, ColBtnCont);

        var btnContBtn   = btnContGO.AddComponent<Button>();
        btnContBtn.targetGraphic = btnContImg;
        btnContBtn.onClick.AddListener(OnContinue);

        var contLbl = MakeTmp("Label", btnContRT, "CONTINUER →", 52f, ColBtnContTxt, FontStyles.Bold,
            Vector2.zero, Vector2.one);
        _ = contLbl; // used via MakeTmp which applies font

        RefreshButtons();
        StartCoroutine(FadeIn());
    }

    // ── Carte d'amélioration ──────────────────────────────────────────────────

    /// <summary>
    /// Carte centrée : titre + description + statut/coût sur toute la largeur.
    /// Le bouton d'achat est intégré dans la carte (pas de carré à droite).
    /// </summary>
    private void BuildUpgradeCard(RectTransform parent,
        string title, string description, string costLabel,
        Vector2 anchorMin, Vector2 anchorMax,
        out TextMeshProUGUI outStatusLabel,
        Action onClick, bool levelLocked = false)
    {
        var zone = MakeZone($"Card_{title}", parent, anchorMin, anchorMax);

        // Fond de carte discret
        var cardBg = zone.gameObject.AddComponent<Image>();
        cardBg.color = new Color(1f, 1f, 1f, 0.04f);
        TBUIStyle.ApplyJauge(cardBg, new Color(1f, 1f, 1f, 0.04f));
        cardBg.raycastTarget = false;

        // Nom de l'amélioration — centré en haut
        MakeLabel($"Title_{title}", zone, title, 40f,
            levelLocked ? ColBtnOwnedTxt : Color.white,
            FontStyles.Bold,
            new Vector2(0f, 0.62f), new Vector2(1f, 1f));

        // Description — centrée au milieu
        var descTmp = MakeTmp($"Desc_{title}", zone, description, 24f, ColDesc, FontStyles.Normal,
            new Vector2(0.04f, 0.30f), new Vector2(0.96f, 0.65f));
        descTmp.alignment = TextAlignmentOptions.Center;

        // Statut / coût — centré en bas, cliquable
        var statusGO  = new GameObject($"Status_{title}");
        statusGO.transform.SetParent(zone, false);
        var statusImg = statusGO.AddComponent<Image>();
        statusImg.color = levelLocked ? ColBtnOwned : ColBtnBuy;
        TBUIStyle.ApplyJauge(statusImg, levelLocked ? ColBtnOwned : ColBtnBuy);
        var statusRT      = statusImg.rectTransform;
        statusRT.anchorMin = new Vector2(0.15f, 0.04f);
        statusRT.anchorMax = new Vector2(0.85f, 0.30f);
        statusRT.offsetMin = statusRT.offsetMax = Vector2.zero;

        var statusLblGO  = new GameObject("Label");
        statusLblGO.transform.SetParent(statusGO.transform, false);
        var statusTmp     = statusLblGO.AddComponent<TextMeshProUGUI>();
        statusTmp.text    = levelLocked ? "🔒 " + costLabel : costLabel;
        statusTmp.fontSize = 22f;
        statusTmp.color   = levelLocked ? ColBtnOwnedTxt : ColBtnBuyTxt;
        statusTmp.fontStyle = FontStyles.Bold;
        statusTmp.alignment = TextAlignmentOptions.Center;
        statusTmp.enableWordWrapping = false;
        statusTmp.raycastTarget = false;
        TBUIStyle.ApplyFont(statusTmp);
        var statusLblRT   = statusTmp.rectTransform;
        statusLblRT.anchorMin = Vector2.zero;
        statusLblRT.anchorMax = Vector2.one;
        statusLblRT.offsetMin = statusLblRT.offsetMax = Vector2.zero;

        outStatusLabel = statusTmp;

        if (!levelLocked)
        {
            var btn = statusGO.AddComponent<Button>();
            btn.targetGraphic = statusImg;
            btn.onClick.AddListener(() => onClick?.Invoke());
        }
    }

    // ── Achats ────────────────────────────────────────────────────────────────

    private void OnBuyAlly()
    {
        int score = TBGameManager.Instance.Score;
        if (!upgrades.BuyAlly(ref score)) return;
        TBGameManager.Instance.SetScore(score);
        TBGameManager.Instance.RegisterUpgradePurchase();
        RefreshButtons();
    }

    private void OnBuyWeapon()
    {
        int score = TBGameManager.Instance.Score;
        if (!upgrades.BuyWeapon(ref score)) return;
        TBGameManager.Instance.SetScore(score);
        TBGameManager.Instance.RegisterUpgradePurchase();
        RefreshButtons();
    }

    private void OnBuyBarrier()
    {
        int score = TBGameManager.Instance.Score;
        if (!upgrades.BuyBarrier(ref score)) return;
        TBGameManager.Instance.SetScore(score);
        TBGameManager.Instance.RegisterUpgradePurchase();
        RefreshButtons();
    }

    private void OnContinue()
    {
        Destroy(selfRoot);
        onContinue?.Invoke();
    }

    // ── Mise à jour des statuts ───────────────────────────────────────────────

    private void RefreshButtons()
    {
        if (scoreLabel != null) scoreLabel.text = ScoreText();

        int  score      = TBGameManager.Instance?.Score ?? 0;
        int  levelIndex = TBGameManager.Instance?.LevelIndex ?? 0;

        bool allyLocked    = !TBUpgradeData.IsAllyUnlocked(levelIndex);
        bool barrierLocked = !TBUpgradeData.IsBarrierUnlocked(levelIndex);
        bool weaponLocked  = !TBUpgradeData.IsWeaponUnlocked(levelIndex);

        RefreshStatus(allyStatusLabel,
            allyLocked || upgrades.AllyCount >= TBUpgradeData.MaxAllies,
            !allyLocked && score < TBUpgradeData.CostAlly,
            allyLocked    ? "🔒 Bloqué"
            : upgrades.AllyCount >= TBUpgradeData.MaxAllies ? "MAX"
            : $"{TBUpgradeData.CostAlly} pts  ({upgrades.AllyCount}/{TBUpgradeData.MaxAllies})");

        RefreshStatus(barrierStatusLabel,
            barrierLocked || upgrades.BarrierCount >= TBUpgradeData.MaxBarriers,
            !barrierLocked && score < TBUpgradeData.CostBarrier,
            barrierLocked ? "🔒 Bloqué"
            : upgrades.BarrierCount >= TBUpgradeData.MaxBarriers ? "MAX"
            : $"{TBUpgradeData.CostBarrier} pts  ({upgrades.BarrierCount}/{TBUpgradeData.MaxBarriers})");

        RefreshStatus(weaponStatusLabel,
            weaponLocked || upgrades.HasWeapon,
            !weaponLocked && score < TBUpgradeData.CostWeapon,
            weaponLocked  ? "🔒 Bloqué"
            : upgrades.HasWeapon ? "ACQUIS"
            : $"{TBUpgradeData.CostWeapon} pts");
    }

    private static void RefreshStatus(TextMeshProUGUI label, bool owned, bool tooExpensive, string text)
    {
        if (label == null) return;
        bool disabled = owned || tooExpensive;

        var btn = label.transform.parent?.GetComponent<Button>();
        if (btn != null) btn.interactable = !disabled;

        var img = label.transform.parent?.GetComponent<Image>();
        if (img != null) img.color = disabled ? ColBtnOwned : ColBtnBuy;

        label.text  = text;
        label.color = disabled ? ColBtnOwnedTxt : ColBtnBuyTxt;
    }

    private static string ScoreText()
        => $"Score disponible : {TBGameManager.Instance?.Score ?? 0} pts";

    // ── Fade in ───────────────────────────────────────────────────────────────

    private IEnumerator FadeIn()
    {
        var cg    = selfRoot.AddComponent<CanvasGroup>();
        cg.alpha  = 0f;
        float t   = 0f;
        float dur = 0.22f;
        while (t < dur)
        {
            t        += Time.deltaTime;
            cg.alpha  = Mathf.Lerp(0f, 1f, t / dur);
            yield return null;
        }
        cg.alpha = 1f;
    }

    // ── Helpers UI ────────────────────────────────────────────────────────────

    private static void MakeBg(string name, RectTransform parent, Vector2 aMin, Vector2 aMax, Color col)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = col;
        TBUIStyle.ApplyJauge(img, col);
        var rt    = img.rectTransform;
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static RectTransform MakePanel(string name, RectTransform parent, Vector2 aMin, Vector2 aMax, Color col)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color  = col;
        TBUIStyle.ApplyJauge(img, col);
        var rt     = img.rectTransform;
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    private static RectTransform MakeZone(string name, RectTransform parent, Vector2 aMin, Vector2 aMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    private static void MakeSep(string name, RectTransform parent, Vector2 aMin, Vector2 aMax)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = ColSep;
        TBUIStyle.ApplyJauge(img, ColSep);
        var rt     = img.rectTransform;
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static void MakeLabel(string name, RectTransform parent, string text,
        float fontSize, Color color, FontStyles style, Vector2 aMin, Vector2 aMax)
    {
        MakeTmp(name, parent, text, fontSize, color, style, aMin, aMax);
    }

    private static TextMeshProUGUI MakeTmp(string name, RectTransform parent, string text,
        float fontSize, Color color, FontStyles style, Vector2 aMin, Vector2 aMax)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp         = go.AddComponent<TextMeshProUGUI>();
        tmp.text        = text;
        tmp.fontSize    = fontSize;
        tmp.color       = color;
        tmp.fontStyle   = style;
        tmp.alignment   = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;
        TBUIStyle.ApplyFont(tmp);
        var rt          = tmp.rectTransform;
        rt.anchorMin    = aMin; rt.anchorMax = aMax;
        rt.offsetMin    = rt.offsetMax = Vector2.zero;
        return tmp;
    }
}