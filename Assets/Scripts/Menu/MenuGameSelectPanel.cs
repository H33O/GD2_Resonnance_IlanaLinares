using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Panneau de sélection de jeu affiché quand le joueur appuie sur "JOUER".
/// Contient 4 boutons dont un verrouillé (cadenas).
/// Se glisse par-dessus le menu principal via un CanvasGroup.
/// </summary>
public class MenuGameSelectPanel : MonoBehaviour
{
    // ── Palette ───────────────────────────────────────────────────────────────

    private static readonly Color ColBg        = new Color(0.05f, 0.05f, 0.05f, 0.96f);
    private static readonly Color ColBtnAccent  = Color.white;
    private static readonly Color ColBtnAccentTxt = new Color(0.05f, 0.05f, 0.05f, 1f);
    private static readonly Color ColBtnSecond  = new Color(0.12f, 0.12f, 0.12f, 1f);
    private static readonly Color ColBtnText    = Color.white;
    private static readonly Color ColBtnOutline = new Color(1f, 1f, 1f, 0.22f);
    private static readonly Color ColLocked     = new Color(0.35f, 0.35f, 0.35f, 1f);
    private static readonly Color ColLockedTxt  = new Color(1f, 1f, 1f, 0.30f);
    private static readonly Color ColSeparator  = new Color(1f, 1f, 1f, 0.18f);
    private static readonly Color ColBackBtn    = new Color(1f, 1f, 1f, 0.08f);
    private static readonly Color ColBackTxt    = new Color(1f, 1f, 1f, 0.55f);

    // ── Références ────────────────────────────────────────────────────────────

    private CanvasGroup group;

    // ── Création statique ─────────────────────────────────────────────────────

    /// <summary>
    /// Crée et retourne le panneau de sélection, caché par défaut (alpha 0).
    /// Appelle Show() pour l'afficher.
    /// </summary>
    public static MenuGameSelectPanel Create(Transform canvasParent)
    {
        var go    = new GameObject("GameSelectPanel");
        go.transform.SetParent(canvasParent, false);

        var rt         = go.AddComponent<RectTransform>();
        rt.anchorMin   = Vector2.zero;
        rt.anchorMax   = Vector2.one;
        rt.offsetMin   = rt.offsetMax = Vector2.zero;

        var panel      = go.AddComponent<MenuGameSelectPanel>();
        panel.group    = go.AddComponent<CanvasGroup>();
        panel.group.alpha          = 0f;
        panel.group.blocksRaycasts = false;
        panel.group.interactable   = false;

        panel.Build(rt);
        return panel;
    }

    // ── Construction ──────────────────────────────────────────────────────────

    private void Build(RectTransform root)
    {
        // Fond opaque plein panneau
        var bg         = new GameObject("PanelBg");
        bg.transform.SetParent(root, false);
        var bgImg      = bg.AddComponent<Image>();
        bgImg.sprite   = SpriteGenerator.CreateWhiteSquare();
        bgImg.color    = ColBg;
        bgImg.raycastTarget = false;
        Stretch(bgImg.rectTransform);

        // Titre de section
        var title = new GameObject("SelectTitle");
        title.transform.SetParent(root, false);
        var tmp              = title.AddComponent<TextMeshProUGUI>();
        tmp.text             = "SÉLECTIONNER UN JEU";
        tmp.fontSize         = 44f;
        tmp.fontStyle        = FontStyles.Bold;
        tmp.color            = new Color(1f, 1f, 1f, 0.45f);
        tmp.alignment        = TextAlignmentOptions.Center;
        tmp.raycastTarget    = false;
        var titleRT          = tmp.rectTransform;
        titleRT.anchorMin    = new Vector2(0f, 0.82f);
        titleRT.anchorMax    = new Vector2(1f, 0.92f);
        titleRT.offsetMin    = titleRT.offsetMax = Vector2.zero;

        // Séparateur sous le titre
        var sep       = new GameObject("Sep");
        sep.transform.SetParent(root, false);
        var sepImg    = sep.AddComponent<Image>();
        sepImg.sprite = SpriteGenerator.CreateWhiteSquare();
        sepImg.color  = ColSeparator;
        sepImg.raycastTarget = false;
        var sepRT     = sepImg.rectTransform;
        sepRT.anchorMin = new Vector2(0.08f, 0.81f);
        sepRT.anchorMax = new Vector2(0.92f, 0.81f);
        sepRT.sizeDelta = new Vector2(0f, 2f);

        // Zone des 4 boutons
        var zone       = new GameObject("BtnZone");
        zone.transform.SetParent(root, false);
        var zoneRT     = zone.AddComponent<RectTransform>();
        zoneRT.anchorMin = new Vector2(0.5f, 0.18f);
        zoneRT.anchorMax = new Vector2(0.5f, 0.80f);
        zoneRT.sizeDelta = new Vector2(640f, 0f);
        zoneRT.offsetMin = Vector2.zero;
        zoneRT.offsetMax = Vector2.zero;

        float slotH   = 1f / 4f;                    // chaque bouton occupe 25 %
        float gap      = 0.015f;                     // écart entre boutons

        // Bouton 1 — accentué (premier, en haut)
        MakeBtn(zoneRT, "Btn_Game1", "TILT BALL",
                new Vector2(0f, slotH * 3 + gap), new Vector2(1f, slotH * 4),
                ColBtnAccent, ColBtnAccentTxt, 54f, isAccent: true,
                onClick: OnGame1);

        // Bouton 2
        MakeBtn(zoneRT, "Btn_Game2", "BULLES",
                new Vector2(0f, slotH * 2 + gap), new Vector2(1f, slotH * 3 - gap),
                ColBtnSecond, ColBtnText, 54f,
                onClick: OnGame2);

        // Bouton 3
        MakeBtn(zoneRT, "Btn_Game3", "ARÈNE",
                new Vector2(0f, slotH * 1 + gap), new Vector2(1f, slotH * 2 - gap),
                ColBtnSecond, ColBtnText, 54f,
                onClick: OnGame3);

        // Bouton 4 — verrouillé (cadenas)
        MakeBtn(zoneRT, "Btn_Game4", "🔒  BIENTÔT",
                new Vector2(0f, 0f), new Vector2(1f, slotH * 1 - gap),
                ColLocked, ColLockedTxt, 48f,
                locked: true);

        // Bouton retour — coin bas-centre
        MakeBackButton(root);
    }

    // ── Helpers de construction ───────────────────────────────────────────────

    private void MakeBtn(RectTransform parent, string goName, string label,
                         Vector2 anchorMin, Vector2 anchorMax,
                         Color bgColor, Color txtColor, float fontSize,
                         bool isAccent = false, bool locked = false,
                         System.Action onClick = null)
    {
        var go      = new GameObject(goName);
        go.transform.SetParent(parent, false);

        var img         = go.AddComponent<Image>();
        img.sprite      = SpriteGenerator.CreateWhiteSquare();
        img.color       = bgColor;
        var rt          = img.rectTransform;
        rt.anchorMin    = anchorMin;
        rt.anchorMax    = anchorMax;
        rt.offsetMin    = rt.offsetMax = Vector2.zero;

        // Contour sur les boutons non-accent et non-verrouillés
        if (!isAccent)
        {
            var outGO       = new GameObject("Outline");
            outGO.transform.SetParent(rt, false);
            var outImg      = outGO.AddComponent<Image>();
            outImg.sprite   = SpriteGenerator.CreateWhiteSquare();
            outImg.color    = locked ? new Color(1f,1f,1f,0.08f) : ColBtnOutline;
            outImg.raycastTarget = false;
            var outRT       = outImg.rectTransform;
            outRT.anchorMin = Vector2.zero;
            outRT.anchorMax = Vector2.one;
            outRT.offsetMin = new Vector2(-1.5f, -1.5f);
            outRT.offsetMax = new Vector2( 1.5f,  1.5f);
            outGO.transform.SetAsFirstSibling();
        }

        // Label
        var lgo         = new GameObject("Label");
        lgo.transform.SetParent(rt, false);
        var ltmp        = lgo.AddComponent<TextMeshProUGUI>();
        ltmp.text       = label;
        ltmp.fontSize   = fontSize;
        ltmp.fontStyle  = FontStyles.Bold;
        ltmp.color      = txtColor;
        ltmp.alignment  = TextAlignmentOptions.Center;
        ltmp.raycastTarget = false;
        var lrt         = ltmp.rectTransform;
        lrt.anchorMin   = Vector2.zero;
        lrt.anchorMax   = Vector2.one;
        lrt.offsetMin   = lrt.offsetMax = Vector2.zero;

        // Bouton Unity — désactivé si verrouillé
        if (!locked && onClick != null)
        {
            var btn           = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());
        }
    }

    private void MakeBackButton(RectTransform root)
    {
        var go      = new GameObject("BackButton");
        go.transform.SetParent(root, false);

        var img         = go.AddComponent<Image>();
        img.sprite      = SpriteGenerator.CreateWhiteSquare();
        img.color       = ColBackBtn;
        var rt          = img.rectTransform;
        rt.anchorMin    = new Vector2(0.5f, 0.04f);
        rt.anchorMax    = new Vector2(0.5f, 0.14f);
        rt.sizeDelta    = new Vector2(380f, 0f);
        rt.offsetMin    = new Vector2(rt.offsetMin.x, 0f);
        rt.offsetMax    = new Vector2(rt.offsetMax.x, 0f);

        var lgo         = new GameObject("Label");
        lgo.transform.SetParent(rt, false);
        var ltmp        = lgo.AddComponent<TextMeshProUGUI>();
        ltmp.text       = "← RETOUR";
        ltmp.fontSize   = 42f;
        ltmp.fontStyle  = FontStyles.Bold;
        ltmp.color      = ColBackTxt;
        ltmp.alignment  = TextAlignmentOptions.Center;
        ltmp.raycastTarget = false;
        var lrt         = ltmp.rectTransform;
        lrt.anchorMin   = Vector2.zero;
        lrt.anchorMax   = Vector2.one;
        lrt.offsetMin   = lrt.offsetMax = Vector2.zero;

        var btn           = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(Hide);
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // ── Affichage ─────────────────────────────────────────────────────────────

    /// <summary>Affiche le panneau avec un fondu entrant.</summary>
    public void Show() => StartCoroutine(Fade(0f, 1f, true));

    /// <summary>Masque le panneau avec un fondu sortant.</summary>
    public void Hide() => StartCoroutine(Fade(1f, 0f, false));

    private System.Collections.IEnumerator Fade(float from, float to, bool show)
    {
        const float duration = 0.22f;
        if (show)
        {
            group.blocksRaycasts = true;
            group.interactable   = true;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed      += Time.deltaTime;
            group.alpha   = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        group.alpha = to;

        if (!show)
        {
            group.blocksRaycasts = false;
            group.interactable   = false;
        }
    }

    // ── Actions ───────────────────────────────────────────────────────────────

    private void OnGame1()
    {
        const string sceneName = "TiltBall";
        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadScene(sceneName, "TILT BALL");
        else
            SceneManager.LoadScene(sceneName);
    }

    private void OnGame2()
    {
        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadScene(OWGameManager.SceneMinijeu2, "BULLES");
        else
            SceneManager.LoadScene(OWGameManager.SceneMinijeu2);
    }

    private void OnGame3()
    {
        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadScene(OWGameManager.SceneMinijeu4, "ARÈNE");
        else
            SceneManager.LoadScene(OWGameManager.SceneMinijeu4);
    }
}
