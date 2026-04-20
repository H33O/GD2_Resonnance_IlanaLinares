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

    // ── Labels des boutons d'achat (mis à jour après chaque achat) ────────────

    private Button          btnAlly;
    private TextMeshProUGUI btnAllyLabel;
    private Button          btnWeapon;
    private TextMeshProUGUI btnWeaponLabel;
    private Button          btnBarrier;
    private TextMeshProUGUI btnBarrierLabel;

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
        // Fond semi-transparent
        MakeBg("Overlay", root, Vector2.zero, Vector2.one, ColOverlay);

        // Panneau principal centré
        var panel = MakePanel("Panel", root,
            new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.88f), ColPanel);

        // Titre
        MakeLabel("Title", panel, "AMÉLIORATIONS", 58f, ColTitle, FontStyles.Bold,
            new Vector2(0f, 0.86f), new Vector2(1f, 0.99f));

        // Score actuel
        scoreLabel = MakeTmp("ScoreLabel", panel,
            ScoreText(), 38f, ColScore, FontStyles.Normal,
            new Vector2(0f, 0.78f), new Vector2(1f, 0.88f));
        scoreLabel.alignment = TextAlignmentOptions.Center;

        // Séparateur
        MakeSep("Sep1", panel, new Vector2(0.06f, 0.76f), new Vector2(0.94f, 0.77f));

        // Trois cartes d'amélioration (distribuées verticalement)
        BuildUpgradeCard(panel,
            "ALLIÉ",
            "Suit le joueur · détruit les ennemis",
            $"{TBUpgradeData.CostAlly} pts  ({upgrades.AllyCount}/{TBUpgradeData.MaxAllies})",
            new Vector2(0.05f, 0.52f), new Vector2(0.95f, 0.74f),
            out btnAlly, out btnAllyLabel,
            OnBuyAlly);

        MakeSep("Sep2", panel, new Vector2(0.06f, 0.50f), new Vector2(0.94f, 0.51f));

        BuildUpgradeCard(panel,
            "ARME",
            "Tire sur l'ennemi le plus proche",
            $"{TBUpgradeData.CostWeapon} pts",
            new Vector2(0.05f, 0.27f), new Vector2(0.95f, 0.49f),
            out btnWeapon, out btnWeaponLabel,
            OnBuyWeapon);

        MakeSep("Sep3", panel, new Vector2(0.06f, 0.25f), new Vector2(0.94f, 0.26f));

        BuildUpgradeCard(panel,
            "BARRIÈRE",
            "Place des murs défensifs dans le niveau",
            $"{TBUpgradeData.CostBarrier} pts  ({upgrades.BarrierCount}/{TBUpgradeData.MaxBarriers})",
            new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.24f),
            out btnBarrier, out btnBarrierLabel,
            OnBuyBarrier);

        // Bouton Continuer
        MakeButton("BtnContinue", panel, "CONTINUER →",
            new Vector2(0.10f, 0.01f), new Vector2(0.90f, 0.12f),
            ColBtnCont, ColBtnContTxt, 48f, OnContinue);

        // Repositionne le bouton Continuer sous le panneau
        var btnContGO = new GameObject("BtnContinueBottom");
        btnContGO.transform.SetParent(root, false);
        var btnContRT     = btnContGO.AddComponent<RectTransform>();
        btnContRT.anchorMin = new Vector2(0.10f, 0.05f);
        btnContRT.anchorMax = new Vector2(0.90f, 0.14f);
        btnContRT.offsetMin = btnContRT.offsetMax = Vector2.zero;

        var btnContImg   = btnContGO.AddComponent<Image>();
        btnContImg.color = ColBtnCont;
        var btnContBtn   = btnContGO.AddComponent<Button>();
        btnContBtn.targetGraphic = btnContImg;
        btnContBtn.onClick.AddListener(OnContinue);

        MakeTmp("Label", btnContRT, "CONTINUER →", 52f, ColBtnContTxt, FontStyles.Bold,
            Vector2.zero, Vector2.one);

        RefreshButtons();
        StartCoroutine(FadeIn());
    }

    // ── Carte d'amélioration ──────────────────────────────────────────────────

    private void BuildUpgradeCard(RectTransform parent,
        string title, string description, string costLabel,
        Vector2 anchorMin, Vector2 anchorMax,
        out Button outBtn, out TextMeshProUGUI outBtnLabel,
        Action onClick)
    {
        var zone = MakeZone($"Card_{title}", parent, anchorMin, anchorMax);

        // Nom de l'amélioration
        MakeLabel($"Title_{title}", zone, title, 44f, Color.white, FontStyles.Bold,
            new Vector2(0f, 0.60f), new Vector2(0.65f, 1f));

        // Description courte
        MakeTmp($"Desc_{title}", zone, description, 28f, ColDesc, FontStyles.Normal,
            new Vector2(0f, 0.20f), new Vector2(0.65f, 0.62f)).alignment = TextAlignmentOptions.TopLeft;

        // Bouton Acheter (côté droit)
        var btn = MakeButton($"Btn_{title}", zone, costLabel,
            new Vector2(0.67f, 0.10f), new Vector2(1.00f, 0.90f),
            ColBtnBuy, ColBtnBuyTxt, 28f, onClick);

        outBtn      = zone.GetComponentInChildren<Button>();
        outBtnLabel = btn;
    }

    // ── Achats ────────────────────────────────────────────────────────────────

    private void OnBuyAlly()
    {
        int score = TBGameManager.Instance.Score;
        if (!upgrades.BuyAlly(ref score)) return;
        TBGameManager.Instance.SetScore(score);
        RefreshButtons();
    }

    private void OnBuyWeapon()
    {
        int score = TBGameManager.Instance.Score;
        if (!upgrades.BuyWeapon(ref score)) return;
        TBGameManager.Instance.SetScore(score);
        RefreshButtons();
    }

    private void OnBuyBarrier()
    {
        int score = TBGameManager.Instance.Score;
        if (!upgrades.BuyBarrier(ref score)) return;
        TBGameManager.Instance.SetScore(score);
        RefreshButtons();
    }

    private void OnContinue()
    {
        Destroy(selfRoot);
        onContinue?.Invoke();
    }

    // ── Mise à jour des boutons ───────────────────────────────────────────────

    private void RefreshButtons()
    {
        if (scoreLabel != null) scoreLabel.text = ScoreText();

        int score = TBGameManager.Instance?.Score ?? 0;

        RefreshBtn(btnAlly,    btnAllyLabel,
            upgrades.AllyCount >= TBUpgradeData.MaxAllies,
            score < TBUpgradeData.CostAlly,
            $"{TBUpgradeData.CostAlly} pts\n({upgrades.AllyCount}/{TBUpgradeData.MaxAllies})");

        RefreshBtn(btnWeapon,  btnWeaponLabel,
            upgrades.HasWeapon,
            score < TBUpgradeData.CostWeapon,
            upgrades.HasWeapon ? "ACQUIS" : $"{TBUpgradeData.CostWeapon} pts");

        RefreshBtn(btnBarrier, btnBarrierLabel,
            upgrades.BarrierCount >= TBUpgradeData.MaxBarriers,
            score < TBUpgradeData.CostBarrier,
            $"{TBUpgradeData.CostBarrier} pts\n({upgrades.BarrierCount}/{TBUpgradeData.MaxBarriers})");
    }

    private static void RefreshBtn(Button btn, TextMeshProUGUI label, bool owned, bool tooExpensive, string text)
    {
        if (btn == null) return;

        bool disabled = owned || tooExpensive;
        btn.interactable = !disabled;

        var img = btn.GetComponent<Image>();
        if (img != null) img.color = disabled ? ColBtnOwned : ColBtnBuy;

        if (label != null)
        {
            label.text  = text;
            label.color = disabled ? ColBtnOwnedTxt : ColBtnBuyTxt;
        }
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
        img.sprite = SpriteGenerator.CreateWhiteSquare();
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
        img.sprite = SpriteGenerator.CreateWhiteSquare();
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
        img.color  = ColSep;
        img.sprite = SpriteGenerator.CreateWhiteSquare();
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
        var rt          = tmp.rectTransform;
        rt.anchorMin    = aMin; rt.anchorMax = aMax;
        rt.offsetMin    = rt.offsetMax = Vector2.zero;
        return tmp;
    }

    /// <summary>Crée un bouton et retourne le TextMeshPro du label.</summary>
    private static TextMeshProUGUI MakeButton(string name, RectTransform parent, string label,
        Vector2 aMin, Vector2 aMax,
        Color bgColor, Color txtColor, float fontSize, Action onClick)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img   = go.AddComponent<Image>();
        img.color = bgColor;
        img.sprite = SpriteGenerator.CreateWhiteSquare();
        var rt    = img.rectTransform;
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => onClick?.Invoke());

        var lgo = new GameObject("Label");
        lgo.transform.SetParent(go.transform, false);
        var tmp        = lgo.AddComponent<TextMeshProUGUI>();
        tmp.text       = label;
        tmp.fontSize   = fontSize;
        tmp.color      = txtColor;
        tmp.fontStyle  = FontStyles.Bold;
        tmp.alignment  = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;
        var lrt        = tmp.rectTransform;
        lrt.anchorMin  = Vector2.zero;
        lrt.anchorMax  = Vector2.one;
        lrt.offsetMin  = new Vector2(8f, 4f);
        lrt.offsetMax  = new Vector2(-8f, -4f);
        return tmp;
    }
}
