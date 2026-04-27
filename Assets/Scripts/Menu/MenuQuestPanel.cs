using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Journal des quêtes — slide depuis la droite (EaseOutQuart, 0.4 s).
///
/// Affiche la vague active avec progression animée.
/// Le journal se rafraîchit en temps réel via <see cref="QuestManager.OnProgressChanged"/>.
/// Pas de bouton Retour : le joueur rappuie sur QUÊTES pour fermer.
/// </summary>
public class MenuQuestPanel : MonoBehaviour
{
    // ── Timings ───────────────────────────────────────────────────────────────

    private const float SlideDuration = 0.40f;
    private const float CanvasRefW    = 1080f;
    private const float FillAnimDur   = 0.65f;

    // ── Palette ───────────────────────────────────────────────────────────────

    private static readonly Color ColBg            = new Color(0.04f, 0.03f, 0.06f, 0.97f);
    private static readonly Color ColWhite         = Color.white;
    private static readonly Color ColWhiteDim      = new Color(1f, 1f, 1f, 0.75f);
    private static readonly Color ColSep           = new Color(1f, 1f, 1f, 0.15f);

    // Carte quête simple → bordure orange
    private static readonly Color ColBorderSimple  = new Color(0.95f, 0.42f, 0.04f, 1.00f);  // orange vif
    // Carte quête complexe → bordure bleue/violette
    private static readonly Color ColBorderComplex = new Color(0.35f, 0.65f, 1.00f, 1.00f);  // bleu clair
    // Carte terminée → bordure verte
    private static readonly Color ColBorderDone    = new Color(0.20f, 0.90f, 0.35f, 1.00f);

    // Fond de la carte (semi-transparent sombre, laisse le sprite transparaître)
    private static readonly Color ColCardBgSimple  = new Color(0.25f, 0.10f, 0.02f, 0.82f);
    private static readonly Color ColCardBgComplex = new Color(0.05f, 0.08f, 0.22f, 0.82f);
    private static readonly Color ColCardBgDone    = new Color(0.04f, 0.20f, 0.06f, 0.82f);

    private static readonly Color ColTitleSimple   = Color.white;
    private static readonly Color ColTitleComplex  = new Color(0.75f, 0.90f, 1.00f, 1.00f);
    private static readonly Color ColTitleDone     = new Color(0.30f, 0.95f, 0.45f, 1.00f);

    private static readonly Color ColDesc          = new Color(1f, 1f, 1f, 0.78f);
    private static readonly Color ColDescDone      = new Color(0.30f, 0.95f, 0.45f, 0.80f);

    private static readonly Color ColGold          = new Color(1.00f, 0.82f, 0.18f, 1.00f);
    private static readonly Color ColBlue          = new Color(0.40f, 0.80f, 1.00f, 1.00f);
    private static readonly Color ColDone          = new Color(0.30f, 0.95f, 0.45f, 1.00f);

    private static readonly Color ColTrack         = new Color(1f, 1f, 1f, 0.14f);
    private static readonly Color ColFillSimple    = new Color(0.95f, 0.55f, 0.05f, 1.00f);
    private static readonly Color ColFillComplex   = new Color(0.40f, 0.70f, 1.00f, 1.00f);
    private static readonly Color ColFillDone      = new Color(0.20f, 0.90f, 0.35f, 1.00f);

    private static readonly Color ColBadgeBg       = new Color(0.22f, 0.38f, 0.80f, 0.30f);
    private const  float          BorderPx          = 5f;   // épaisseur de la bordure simulée

    // ── État ──────────────────────────────────────────────────────────────────

    private RectTransform   _panelRT;
    private CanvasGroup     _group;
    private bool            _isAnimating;
    private bool            _isVisible;
    private Transform       _listParent;
    private TextMeshProUGUI _waveLabel;
    private TextMeshProUGUI _minScoreLabel;
    private Image           _returnBtnImg;    // mis à jour dans Start() avec le sprite

    // Quête parentière — références pour mise à jour sans rebuild complet
    private Image           _parentFillImg;
    private TextMeshProUGUI _parentProgLabel;

    private readonly List<(Image fill, float target)> _fills = new List<(Image, float)>();

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>Crée le panel dans le canvas donné et le retourne.</summary>
    public static MenuQuestPanel Create(Transform canvasParent)
    {
        var go = new GameObject("QuestPanel");
        go.transform.SetParent(canvasParent, false);

        var rt       = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var cg            = go.AddComponent<CanvasGroup>();
        cg.alpha          = 1f;
        cg.blocksRaycasts = false;
        cg.interactable   = false;

        var p      = go.AddComponent<MenuQuestPanel>();
        p._panelRT = rt;
        p._group   = cg;
        rt.anchoredPosition = new Vector2(CanvasRefW, 0f);

        p.Build(rt);
        return p;
    }

    // ── Construction ──────────────────────────────────────────────────────────

    private void Build(RectTransform root)
    {
        // ── Fond ──────────────────────────────────────────────────────────────
        Img("Bg", root, ColBg, stretch: true).raycastTarget = true;

        // ── Titre ─────────────────────────────────────────────────────────────
        Lbl("Titre", root, "QUÊTES",
            new Vector2(0f, 0.920f), new Vector2(1f, 0.975f),
            52f, ColWhite, FontStyles.Bold);

        // ── Sous-titre vague ───────────────────────────────────────────────────
        _waveLabel = Lbl("Vague", root, "",
            new Vector2(0f, 0.883f), new Vector2(1f, 0.922f),
            21f, ColWhiteDim, FontStyles.Normal);

        // ── Score minimum ──────────────────────────────────────────────────────
        _minScoreLabel = Lbl("MinScore", root, "",
            new Vector2(0f, 0.852f), new Vector2(1f, 0.885f),
            18f, ColWhiteDim, FontStyles.Normal);

        // ── Séparateur principal ───────────────────────────────────────────────
        var sep = Img("Sep", root, ColSep).rectTransform;
        sep.anchorMin = new Vector2(0.04f, 0.849f);
        sep.anchorMax = new Vector2(0.96f, 0.851f);
        sep.sizeDelta = Vector2.zero;

        // ── Quête parentière ───────────────────────────────────────────────────
        BuildParentQuestSection(root);

        // ── ScrollView des cartes de quêtes — repositionnée sous la section parentière
        BuildScrollView(root,
            new Vector2(0.03f, 0.115f),
            new Vector2(0.97f, 0.715f));    // ancMax.y abaissé de 0.848 → 0.715

        // ── Bouton RETOUR (bas de la slide, style photo) ──────────────────────
        BuildReturnButton(root);
    }

    // ── Quête parentière ──────────────────────────────────────────────────────

    /// <summary>
    /// Construit la section quête parentière entre le séparateur et la scroll view.
    /// Zone : anchorY [0.716 .. 0.848]
    /// </summary>
    private void BuildParentQuestSection(RectTransform root)
    {
        var def   = QuestManager.ParentQuestDefinition;
        var prog  = QuestManager.Instance?.ParentQuestProgress ?? new QuestProgress { Id = QuestManager.ParentQuestId };
        bool done = prog.Completed;

        // Étiquette "QUÊTE PARENTIÈRE"
        Lbl("ParentLabel", root, "— QUÊTE PARENTIÈRE —",
            new Vector2(0.04f, 0.830f), new Vector2(0.96f, 0.852f),
            14f, new Color(1f, 0.75f, 0.20f, 0.85f), FontStyles.Bold);

        // Carte parentière (zone [0.716 .. 0.828])
        var cardSection = new GameObject("ParentCard");
        cardSection.transform.SetParent(root, false);
        var cardRT       = cardSection.AddComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0.03f, 0.718f);
        cardRT.anchorMax = new Vector2(0.97f, 0.828f);
        cardRT.offsetMin = cardRT.offsetMax = Vector2.zero;

        // Bordure
        Color borderCol = done
            ? ColBorderDone
            : new Color(1.00f, 0.80f, 0.15f, 1f);   // or/jaune — couleur unique parentière

        var borderImg = cardSection.AddComponent<Image>();
        borderImg.sprite = SpriteGenerator.CreateWhiteSquare();
        borderImg.color  = borderCol;
        borderImg.raycastTarget = false;

        // Fond intérieur
        var bgGO  = new GameObject("Bg");
        bgGO.transform.SetParent(cardRT, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.sprite = SpriteGenerator.CreateWhiteSquare();
        bgImg.color  = done
            ? ColCardBgDone
            : new Color(0.12f, 0.10f, 0.02f, 0.88f);
        bgImg.raycastTarget = false;
        var bgInnerRT = bgImg.rectTransform;
        bgInnerRT.anchorMin = Vector2.zero;
        bgInnerRT.anchorMax = Vector2.one;
        bgInnerRT.offsetMin = new Vector2(BorderPx,  BorderPx);
        bgInnerRT.offsetMax = new Vector2(-BorderPx, -BorderPx);

        Transform textParent = bgGO.transform;

        // Titre
        string titleText = done ? $"✓  {def.Title}" : def.Title;
        Color  titleCol  = done ? ColTitleDone : new Color(1f, 0.90f, 0.40f, 1f);
        AddTMP("Title", textParent, titleText,
            22f, FontStyles.Bold, titleCol,
            new Vector2(0.03f, 0.56f), new Vector2(0.80f, 0.97f));

        // Récompense pièces
        if (!done)
        {
            AddTMP("Coins", textParent, $"+{def.RewardCoins} 🪙",
                18f, FontStyles.Bold, ColGold,
                new Vector2(0.80f, 0.56f), new Vector2(0.97f, 0.97f),
                TextAlignmentOptions.MidlineRight);
        }

        // Description courte
        AddTMP("Desc", textParent, def.Description,
            14f, FontStyles.Normal, done ? ColDescDone : ColDesc,
            new Vector2(0.03f, 0.30f), new Vector2(0.97f, 0.58f));

        // Piste de jauge
        var trackGO  = new GameObject("Track");
        trackGO.transform.SetParent(textParent, false);
        var trackImg = trackGO.AddComponent<Image>();
        trackImg.sprite = SpriteGenerator.CreateWhiteSquare();
        trackImg.color  = ColTrack;
        trackImg.raycastTarget = false;
        var trackRT  = trackImg.rectTransform;
        trackRT.anchorMin = new Vector2(0.03f, 0.06f);
        trackRT.anchorMax = new Vector2(0.72f, 0.26f);
        trackRT.offsetMin = trackRT.offsetMax = Vector2.zero;

        // Remplissage de jauge
        var fillGO  = new GameObject("Fill");
        fillGO.transform.SetParent(trackGO.transform, false);
        _parentFillImg = fillGO.AddComponent<Image>();
        _parentFillImg.sprite     = SpriteGenerator.CreateWhiteSquare();
        _parentFillImg.color      = done ? ColFillDone : new Color(1f, 0.80f, 0.15f, 1f);
        _parentFillImg.type       = Image.Type.Filled;
        _parentFillImg.fillMethod = Image.FillMethod.Horizontal;
        _parentFillImg.fillAmount = prog.GetRatio(def);
        _parentFillImg.raycastTarget = false;
        var fillRT = _parentFillImg.rectTransform;
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;

        // Label progression "0/3"
        int display = Mathf.Min(prog.Count, def.RequiredCount);
        string progText = done ? "TERMINÉE ✓" : $"{display} / {def.RequiredCount}";
        _parentProgLabel = AddTMP("Prog", textParent, progText,
            15f, done ? FontStyles.Bold : FontStyles.Normal,
            done ? ColDone : ColWhiteDim,
            new Vector2(0.73f, 0.04f), new Vector2(0.97f, 0.28f),
            TextAlignmentOptions.MidlineRight);
    }

    private void BuildReturnButton(RectTransform root)
    {
        var go = new GameObject("BtnRetour");
        go.transform.SetParent(root, false);

        var rt       = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.12f, 0.020f);
        rt.anchorMax = new Vector2(0.88f, 0.108f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        // Fond avec le sprite jaugenormal si disponible, sinon couleur unie
        var img    = go.AddComponent<Image>();
        if (MenuAssets.TextBadgeSprite != null)
        {
            img.sprite = MenuAssets.TextBadgeSprite;
            img.type   = Image.Type.Sliced;
            img.color  = Color.white;
        }
        else
        {
            img.sprite = SpriteGenerator.CreateWhiteSquare();
            img.color  = new Color(0.22f, 0.14f, 0.04f, 0.95f);
        }

        // Icône + texte
        var lgo  = new GameObject("Label");
        lgo.transform.SetParent(rt, false);
        var tmp  = lgo.AddComponent<TextMeshProUGUI>();
        tmp.text      = "⬛ RETOUR";
        tmp.fontSize  = 26f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = new Color(1.00f, 0.82f, 0.18f, 1f);   // or, comme la photo
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        MenuAssets.ApplyFont(tmp);
        var lrt  = tmp.rectTransform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;

        var btn           = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors        = btn.colors;
        colors.highlightedColor = new Color(1f, 0.90f, 0.50f, 1f);
        colors.pressedColor     = new Color(0.75f, 0.55f, 0.10f, 1f);
        btn.colors = colors;
        btn.onClick.AddListener(Hide);

        _returnBtnImg = img;   // référence pour mise à jour du sprite dans Start()
    }

    private void BuildScrollView(RectTransform root, Vector2 ancMin, Vector2 ancMax)
    {
        // ── ScrollRect parent ──────────────────────────────────────────────────
        // Le ScrollRect DOIT être le parent du viewport.
        // Hiérarchie correcte : [ScrollRoot+ScrollRect] → [Viewport+Mask+Image] → [List+VLG+CSF]

        var scrollGO = new GameObject("ScrollRoot");
        scrollGO.transform.SetParent(root, false);

        var scrollRT       = scrollGO.AddComponent<RectTransform>();
        scrollRT.anchorMin = ancMin;
        scrollRT.anchorMax = ancMax;
        scrollRT.offsetMin = scrollRT.offsetMax = Vector2.zero;

        var sr               = scrollGO.AddComponent<ScrollRect>();
        sr.horizontal        = false;
        sr.vertical          = true;
        sr.scrollSensitivity = 55f;
        sr.movementType      = ScrollRect.MovementType.Clamped;
        sr.inertia           = true;
        sr.decelerationRate  = 0.135f;

        // ── Viewport (enfant du ScrollRect) ───────────────────────────────────

        var viewGO = new GameObject("Viewport");
        viewGO.transform.SetParent(scrollGO.transform, false);

        // L'Image est requise par Mask
        var maskImg  = viewGO.AddComponent<Image>();
        maskImg.sprite = SpriteGenerator.CreateWhiteSquare();
        maskImg.color  = Color.clear;
        maskImg.raycastTarget = false;

        viewGO.AddComponent<Mask>().showMaskGraphic = false;

        var viewRT       = maskImg.rectTransform;
        viewRT.anchorMin = Vector2.zero;
        viewRT.anchorMax = Vector2.one;
        viewRT.offsetMin = viewRT.offsetMax = Vector2.zero;

        // ── Conteneur de la liste (enfant du Viewport) ────────────────────────

        var listGO = new GameObject("List");
        listGO.transform.SetParent(viewGO.transform, false);

        var listRT       = listGO.AddComponent<RectTransform>();
        listRT.anchorMin = new Vector2(0f, 1f);
        listRT.anchorMax = new Vector2(1f, 1f);
        listRT.pivot     = new Vector2(0.5f, 1f);
        listRT.sizeDelta = Vector2.zero;

        var vlg = listGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing              = 12f;
        vlg.padding              = new RectOffset(8, 8, 8, 16);
        vlg.childControlWidth    = true;
        vlg.childControlHeight   = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        var csf         = listGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ── Brancher le ScrollRect ────────────────────────────────────────────

        sr.viewport = viewRT;
        sr.content  = listRT;

        _listParent = listGO.transform;
    }

    // ── Reconstruction de la liste ────────────────────────────────────────────

    private void RebuildList()
    {
        // Détacher les enfants existants avant de les détruire.
        // Destroy() est asynchrone — on détache d'abord pour que LayoutGroup
        // ne les compte plus dans le même frame.
        for (int i = _listParent.childCount - 1; i >= 0; i--)
        {
            var child = _listParent.GetChild(i);
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }
        _fills.Clear();

        if (QuestManager.Instance == null)
        {
            if (_waveLabel     != null) _waveLabel.text     = "Système de quêtes indisponible";
            if (_minScoreLabel != null) _minScoreLabel.text = "";
            return;
        }

        // Rafraîchir la jauge de la quête parentière sans rebuild
        RefreshParentQuestCard();

        // En-têtes
        int done  = QuestManager.Instance.CompletedCount();
        int total = QuestManager.Instance.ActiveWave.Count;
        if (_waveLabel != null)
            _waveLabel.text = $"Vague {QuestManager.Instance.WaveIndex + 1}   ·   {done} / {total} complétées";
        if (_minScoreLabel != null)
            _minScoreLabel.text = $"Score minimum requis : {QuestManager.Instance.GetMinScore()} pts";

        if (total == 0)
        {
            // Aucune quête — afficher un message de remplacement
            AddTMP("Empty", _listParent,
                "Aucune quête disponible.\nJoue pour débloquer la prochaine vague !",
                22f, FontStyles.Normal, ColWhiteDim,
                Vector2.zero, Vector2.one, TextAlignmentOptions.Center);
            return;
        }

        // Lignes de quêtes
        foreach (var def in QuestManager.Instance.ActiveWave)
        {
            var prog  = QuestManager.Instance.GetProgress(def.Id);
            float ratio = prog.GetRatio(def);
            var fill  = BuildRow(def, prog, prog.Completed);
            _fills.Add((fill, ratio));
        }

        // Forcer le recalcul du layout
        var listRT = _listParent.GetComponent<RectTransform>();
        if (listRT != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(listRT);
    }

    /// <summary>
    /// Met à jour uniquement les éléments dynamiques de la carte parentière
    /// (jauge + label) sans reconstruire toute la section.
    /// </summary>
    private void RefreshParentQuestCard()
    {
        if (QuestManager.Instance == null) return;

        var def   = QuestManager.ParentQuestDefinition;
        var prog  = QuestManager.Instance.ParentQuestProgress;
        bool done = prog.Completed;

        if (_parentFillImg != null)
        {
            _parentFillImg.fillAmount = prog.GetRatio(def);
            _parentFillImg.color = done
                ? ColFillDone
                : new Color(1f, 0.80f, 0.15f, 1f);
        }

        if (_parentProgLabel != null)
        {
            int display = Mathf.Min(prog.Count, def.RequiredCount);
            _parentProgLabel.text      = done ? "TERMINÉE ✓" : $"{display} / {def.RequiredCount}";
            _parentProgLabel.fontStyle = done ? FontStyles.Bold : FontStyles.Normal;
            _parentProgLabel.color     = done ? ColDone : ColWhiteDim;
        }
    }

    private Image BuildRow(QuestDefinition def, QuestProgress prog, bool isDone)
    {
        bool isXP = def.IsComplex;

        Color borderCol  = isDone ? ColBorderDone  : (isXP ? ColBorderComplex : ColBorderSimple);
        Color cardBgCol  = isDone ? ColCardBgDone  : (isXP ? ColCardBgComplex : ColCardBgSimple);
        Color titleCol   = isDone ? ColTitleDone   : (isXP ? ColTitleComplex  : ColTitleSimple);
        Color descCol    = isDone ? ColDescDone    : ColDesc;
        Color fillCol    = isDone ? ColFillDone    : (isXP ? ColFillComplex   : ColFillSimple);
        Color rewardCol  = isDone ? ColDone        : ColGold;

        // Hauteur de la carte : plus haute pour les quêtes complexes
        float rowH = isXP ? 148f : 128f;

        // ── Couche 1 : Bordure (rectangle plein de la couleur de bordure) ─────
        var borderGO  = new GameObject($"Row_{def.Id}");
        borderGO.transform.SetParent(_listParent, false);

        var borderImg = borderGO.AddComponent<Image>();
        borderImg.sprite = SpriteGenerator.CreateWhiteSquare();
        borderImg.color  = borderCol;
        borderImg.raycastTarget = true;   // reçoit les événements scroll

        var le = borderGO.AddComponent<LayoutElement>();
        le.preferredHeight = rowH;
        le.flexibleWidth   = 1f;

        var borderRT = borderImg.rectTransform;

        // ── Couche 2 : Fond de la carte (inset de BorderPx) ──────────────────
        var cardGO   = new GameObject("Card");
        cardGO.transform.SetParent(borderGO.transform, false);

        // Fond avec le sprite jaugenormal si disponible
        var cardImg  = cardGO.AddComponent<Image>();
        bool hasBadgeSprite = MenuAssets.TextBadgeSprite != null;
        if (hasBadgeSprite)
        {
            cardImg.sprite = MenuAssets.TextBadgeSprite;
            cardImg.type   = Image.Type.Sliced;
            cardImg.color  = Color.white;   // laisse le sprite s'exprimer
        }
        else
        {
            cardImg.sprite = SpriteGenerator.CreateWhiteSquare();
            cardImg.color  = cardBgCol;
        }
        cardImg.raycastTarget = false;

        var cardRT   = cardImg.rectTransform;
        cardRT.anchorMin = Vector2.zero;
        cardRT.anchorMax = Vector2.one;
        // Inset = épaisseur de la bordure simulée
        cardRT.offsetMin = new Vector2( BorderPx,  BorderPx);
        cardRT.offsetMax = new Vector2(-BorderPx, -BorderPx);

        // Overlay de teinte (semi-transparent sombre) par dessus le sprite
        if (hasBadgeSprite)
        {
            var tintGO  = new GameObject("Tint");
            tintGO.transform.SetParent(cardGO.transform, false);
            var tintImg = tintGO.AddComponent<Image>();
            tintImg.sprite = SpriteGenerator.CreateWhiteSquare();
            tintImg.color  = cardBgCol;
            tintImg.raycastTarget = false;
            var tintRT = tintImg.rectTransform;
            tintRT.anchorMin = Vector2.zero;
            tintRT.anchorMax = Vector2.one;
            tintRT.offsetMin = tintRT.offsetMax = Vector2.zero;
        }

        // ── Zone de texte : zone intérieure de la carte ───────────────────────
        Transform textParent = cardGO.transform;

        // Titre de la quête (ligne 1)
        string titleText = isDone ? $"✓  {def.Title}" : def.Title;
        AddTMP("Title", textParent, titleText,
            24f, FontStyles.Bold, titleCol,
            new Vector2(0.04f, isXP ? 0.64f : 0.60f),
            new Vector2(isXP ? 0.72f : 0.78f, 0.97f));

        // Description / objectif (ligne 2) — wrap activé
        var descTmp = AddTMP("Desc", textParent, def.Description,
            16f, FontStyles.Normal, descCol,
            new Vector2(0.04f, isXP ? 0.38f : 0.30f),
            new Vector2(0.96f, isXP ? 0.65f : 0.62f));
        descTmp.enableWordWrapping = true;
        descTmp.overflowMode = TextOverflowModes.Ellipsis;

        // Récompense pièces (coin) — haut droite
        if (!isDone)
        {
            AddTMP("Coins", textParent,
                $"+{def.RewardCoins} 🪙",
                20f, FontStyles.Bold, rewardCol,
                new Vector2(isXP ? 0.73f : 0.79f, 0.65f),
                new Vector2(0.97f, 0.97f),
                TextAlignmentOptions.MidlineRight);
        }

        // Badge XP "NIVEAU +1" — quêtes complexes uniquement
        if (isXP && !isDone)
        {
            var badgeGO  = new GameObject("XPBadge");
            badgeGO.transform.SetParent(textParent, false);
            var badgeImg = badgeGO.AddComponent<Image>();
            badgeImg.sprite = SpriteGenerator.CreateWhiteSquare();
            badgeImg.color  = ColBadgeBg;
            badgeImg.raycastTarget = false;
            var badgeRT  = badgeImg.rectTransform;
            badgeRT.anchorMin = new Vector2(0.04f, 0.64f);
            badgeRT.anchorMax = new Vector2(0.72f, 0.82f);
            badgeRT.offsetMin = badgeRT.offsetMax = Vector2.zero;

            AddTMP("XP", badgeGO.transform,
                $"+{def.RewardXP} XP  ·  NIVEAU +1",
                14f, FontStyles.Bold, ColBlue,
                Vector2.zero, Vector2.one,
                TextAlignmentOptions.Center);
        }

        // ── Jauge de progression ──────────────────────────────────────────────

        // Piste (fond sombre)
        var trackGO  = new GameObject("Track");
        trackGO.transform.SetParent(textParent, false);
        var trackImg = trackGO.AddComponent<Image>();
        trackImg.sprite = SpriteGenerator.CreateWhiteSquare();
        trackImg.color  = ColTrack;
        trackImg.raycastTarget = false;
        var trackRT  = trackImg.rectTransform;
        trackRT.anchorMin = new Vector2(0.04f, 0.06f);
        trackRT.anchorMax = new Vector2(0.75f, 0.22f);
        trackRT.offsetMin = trackRT.offsetMax = Vector2.zero;

        // Remplissage
        var fillGO   = new GameObject("Fill");
        fillGO.transform.SetParent(trackGO.transform, false);
        var fillI    = fillGO.AddComponent<Image>();
        fillI.sprite     = SpriteGenerator.CreateWhiteSquare();
        fillI.color      = fillCol;
        fillI.type       = Image.Type.Filled;
        fillI.fillMethod = Image.FillMethod.Horizontal;
        fillI.fillAmount = 0f;
        fillI.raycastTarget = false;
        var fillRT   = fillI.rectTransform;
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;

        // Label progression (droite de la jauge)
        int display = Mathf.Min(prog.Count, def.RequiredCount);
        string progressText = isDone
            ? "TERMINÉE ✓"
            : $"{display} / {def.RequiredCount}";

        AddTMP("Prog", textParent, progressText,
            16f, isDone ? FontStyles.Bold : FontStyles.Normal,
            isDone ? ColDone : ColWhiteDim,
            new Vector2(0.76f, 0.04f),
            new Vector2(0.97f, 0.26f),
            TextAlignmentOptions.MidlineRight);

        return fillI;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        // S'abonner ici (Start) : à ce moment QuestManager.Instance est garanti
        // d'exister car MenuMainSetup.Start() l'a créé avant d'appeler BuildQuestsButton().
        SubscribeToEvents();

        // Appliquer le sprite jaugenormal sur le bouton RETOUR maintenant que
        // MenuAssets.Init() a été appelé (il est garanti d'avoir eu lieu avant Start)
        if (_returnBtnImg != null && MenuAssets.TextBadgeSprite != null)
        {
            _returnBtnImg.sprite = MenuAssets.TextBadgeSprite;
            _returnBtnImg.type   = Image.Type.Sliced;
            _returnBtnImg.color  = Color.white;
        }
    }

    private void OnEnable()
    {
        // Ré-abonnement si réactivation
        SubscribeToEvents();
    }

    private void OnDisable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnProgressChanged -= OnQuestProgressChanged;
            QuestManager.Instance.OnWaveStarted     -= OnWaveStarted;
        }
    }

    private void SubscribeToEvents()
    {
        if (QuestManager.Instance == null) return;
        // Éviter les doublons d'abonnement
        QuestManager.Instance.OnProgressChanged -= OnQuestProgressChanged;
        QuestManager.Instance.OnProgressChanged += OnQuestProgressChanged;
        QuestManager.Instance.OnWaveStarted     -= OnWaveStarted;
        QuestManager.Instance.OnWaveStarted     += OnWaveStarted;
    }

    /// <summary>
    /// Appelé quand la progression des quêtes change (score ajouté, complétion).
    /// Si le panel est visible, rafraîchit immédiatement la liste.
    /// </summary>
    private void OnQuestProgressChanged()
    {
        if (_isVisible)
            StartCoroutine(RebuildNextFrame());
    }

    private void OnWaveStarted(int _)
    {
        if (_isVisible)
            StartCoroutine(RebuildNextFrame());
    }

    /// <summary>
    /// Attend une frame avant de rebuilder pour laisser Unity mettre à jour
    /// les données du QuestManager (exécuté dans le même frame que l'event).
    /// </summary>
    private IEnumerator RebuildNextFrame()
    {
        yield return null;
        RebuildList();
        if (_fills.Count > 0)
            StartCoroutine(AnimateFills());
        AnimateParentFill();
    }

    /// <summary>Anime la jauge de la carte parentière depuis 0 jusqu'à sa valeur cible.</summary>
    private void AnimateParentFill()
    {
        if (_parentFillImg == null || QuestManager.Instance == null) return;
        var def   = QuestManager.ParentQuestDefinition;
        var prog  = QuestManager.Instance.ParentQuestProgress;
        float target = prog.GetRatio(def);
        StartCoroutine(AnimateFill(_parentFillImg, 0f, target, FillAnimDur, 0f));
    }

    // ── Show / Hide ───────────────────────────────────────────────────────────

    public void Show()
    {
        if (_isAnimating) return;
        _isAnimating          = true;
        _isVisible            = true;
        _group.blocksRaycasts = true;
        _group.interactable   = true;

        RebuildList();

        StopAllCoroutines();
        _panelRT.anchoredPosition = new Vector2(CanvasRefW, 0f);
        StartCoroutine(Slide(CanvasRefW, 0f, () =>
        {
            _isAnimating = false;
            StartCoroutine(AnimateFills());
            AnimateParentFill();
        }));
    }

    public void Hide()
    {
        if (_isAnimating) return;
        _isAnimating          = true;
        _isVisible            = false;
        _group.blocksRaycasts = false;
        _group.interactable   = false;

        StopAllCoroutines();
        StartCoroutine(Slide(_panelRT.anchoredPosition.x, CanvasRefW, () =>
        {
            _isAnimating = false;
        }));
    }

    private IEnumerator Slide(float from, float to, System.Action done)
    {
        float e = 0f;
        while (e < SlideDuration)
        {
            e += Time.unscaledDeltaTime;
            float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(e / SlideDuration), 4f);
            _panelRT.anchoredPosition = new Vector2(Mathf.LerpUnclamped(from, to, t), 0f);
            yield return null;
        }
        _panelRT.anchoredPosition = new Vector2(to, 0f);
        done?.Invoke();
    }

    // ── Animation des jauges ──────────────────────────────────────────────────

    private IEnumerator AnimateFills()
    {
        float delay = 0f;
        foreach (var (fill, target) in _fills)
        {
            if (fill != null)
                StartCoroutine(AnimateFill(fill, 0f, target, FillAnimDur, delay));
            delay += 0.09f;
        }
        yield break;
    }

    private static IEnumerator AnimateFill(Image img, float from, float to, float dur, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        if (img == null) yield break;

        float e        = 0f;
        Color baseCol  = img.color;

        while (e < dur)
        {
            e += Time.deltaTime;
            float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(e / dur), 4f);
            img.fillAmount = Mathf.Lerp(from, to, t);
            yield return null;
        }
        img.fillAmount = to;

        // Flash blanc si la jauge est pleine
        if (Mathf.Approximately(to, 1f))
        {
            float f      = 0f;
            Color bright = Color.Lerp(baseCol, Color.white, 0.6f);
            while (f < 0.25f)
            {
                f += Time.deltaTime;
                img.color = Color.Lerp(bright, baseCol, f / 0.25f);
                yield return null;
            }
            img.color = baseCol;
        }
    }

    // ── Helpers UI ────────────────────────────────────────────────────────────

    private static Image Img(string name, RectTransform parent, Color col, bool stretch = false)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = SpriteGenerator.CreateWhiteSquare();
        img.color  = col;
        img.raycastTarget = false;
        if (stretch)
        {
            img.rectTransform.anchorMin = Vector2.zero;
            img.rectTransform.anchorMax = Vector2.one;
            img.rectTransform.offsetMin = img.rectTransform.offsetMax = Vector2.zero;
        }
        return img;
    }

    private static TextMeshProUGUI Lbl(string name, RectTransform parent, string text,
        Vector2 ancMin, Vector2 ancMax, float size, Color col, FontStyles style,
        TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.fontStyle = style;
        tmp.color     = col;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        MenuAssets.ApplyFont(tmp);
        var rt    = tmp.rectTransform;
        rt.anchorMin = ancMin;
        rt.anchorMax = ancMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return tmp;
    }

    private static TextMeshProUGUI AddTMP(string name, Transform parent,
        string text, float size, FontStyles style, Color col,
        Vector2 ancMin, Vector2 ancMax,
        TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.fontStyle = style;
        tmp.color     = col;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        MenuAssets.ApplyFont(tmp);
        var rt    = tmp.rectTransform;
        rt.anchorMin = ancMin;
        rt.anchorMax = ancMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return tmp;
    }
}
