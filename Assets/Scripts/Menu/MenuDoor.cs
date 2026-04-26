using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Widget "Porte" du menu principal.
///
/// - Affiche le sprite de porte centré bas-écran.
/// - Verrouillée : sprite cadenas centré + tooltip au clic.
/// - Déverrouillée : clic → <see cref="DoorManager.OnDoorClicked"/> (overlay intérieur).
/// </summary>
public class MenuDoor : MonoBehaviour
{
    // ── Mise en page ──────────────────────────────────────────────────────────

    private const float DoorW       = 620f;
    private const float DoorH       = 900f;
    private const float DoorOffsetY = 120f;

    // Cadenas — positionné au centre vertical haut de la porte
    private const float LockW = 200f;
    private const float LockH = 200f;

    // Tooltip
    private const float TooltipW    = 780f;
    private const float TooltipH    = 200f;
    private const float TooltipDur  = 2.8f;   // durée d'affichage en secondes
    private const float TooltipFade = 0.30f;  // durée du fondu

    // ── Références runtime ────────────────────────────────────────────────────

    private Image           _lockImage;
    private RectTransform   _tooltipRT;
    private CanvasGroup     _tooltipGroup;
    private bool            _tooltipShowing;

    // ── Sprites (assignés depuis MenuSceneSetup) ──────────────────────────────

    public Sprite DoorSprite { get; set; }

    // ── Initialisation ────────────────────────────────────────────────────────

    /// <summary>Construit la porte dans le <paramref name="canvasRT"/> fourni.</summary>
    public void Init(RectTransform canvasRT)
    {
        var doorRT = BuildDoorImage(canvasRT);
        BuildLockImage(doorRT);
        BuildTooltip(canvasRT);
        RefreshLockVisual();
    }

    // ── Image de porte ────────────────────────────────────────────────────────

    private RectTransform BuildDoorImage(RectTransform parent)
    {
        var go = new GameObject("Door");
        go.transform.SetParent(parent, false);

        var img          = go.AddComponent<Image>();
        img.sprite       = DoorSprite;
        img.color        = Color.white;
        img.preserveAspect = true;
        img.raycastTarget = true;

        if (DoorSprite == null)
            img.color = new Color(1f, 1f, 1f, 0f);

        var rt              = img.rectTransform;
        rt.anchorMin        = new Vector2(0.5f, 0f);
        rt.anchorMax        = new Vector2(0.5f, 0f);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.sizeDelta        = new Vector2(DoorW, DoorH);
        rt.anchoredPosition = new Vector2(0f, DoorOffsetY);

        var btn                  = go.AddComponent<Button>();
        btn.targetGraphic        = img;
        var colors               = btn.colors;
        colors.highlightedColor  = new Color(1f, 1f, 1f, 0.85f);
        colors.pressedColor      = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.fadeDuration      = 0.08f;
        btn.colors               = colors;
        btn.onClick.AddListener(OnClick);

        return rt;
    }

    // ── Sprite cadenas ────────────────────────────────────────────────────────

    private void BuildLockImage(RectTransform doorRT)
    {
        var go  = new GameObject("LockImage");
        go.transform.SetParent(doorRT, false);

        _lockImage               = go.AddComponent<Image>();
        _lockImage.sprite        = MenuAssets.LockSprite;
        _lockImage.color         = Color.white;
        _lockImage.preserveAspect = true;
        _lockImage.raycastTarget = false;

        // Si pas de sprite, fallback invisible
        if (MenuAssets.LockSprite == null)
            _lockImage.color = new Color(1f, 1f, 1f, 0f);

        var rt        = _lockImage.rectTransform;
        rt.anchorMin  = new Vector2(0.5f, 0.5f);
        rt.anchorMax  = new Vector2(0.5f, 0.5f);
        rt.pivot      = new Vector2(0.5f, 0.5f);
        rt.sizeDelta  = new Vector2(LockW, LockH);
        rt.anchoredPosition = Vector2.zero;
    }

    // ── Tooltip "Finissez les 3 mini jeux…" ──────────────────────────────────

    private void BuildTooltip(RectTransform canvasRT)
    {
        var go  = new GameObject("DoorTooltip");
        go.transform.SetParent(canvasRT, false);

        var rt          = go.AddComponent<RectTransform>();
        rt.anchorMin    = new Vector2(0.5f, 0f);
        rt.anchorMax    = new Vector2(0.5f, 0f);
        rt.pivot        = new Vector2(0.5f, 0f);
        rt.sizeDelta    = new Vector2(TooltipW, TooltipH);
        rt.anchoredPosition = new Vector2(0f, DoorOffsetY + DoorH + 24f);

        _tooltipGroup               = go.AddComponent<CanvasGroup>();
        _tooltipGroup.alpha         = 0f;
        _tooltipGroup.blocksRaycasts = false;
        _tooltipGroup.interactable  = false;

        // Fond
        var bg          = go.AddComponent<Image>();
        bg.sprite       = SpriteGenerator.CreateWhiteSquare();
        bg.color        = new Color(0.06f, 0.04f, 0.10f, 0.92f);
        bg.raycastTarget = false;

        // Texte
        var txtGO  = new GameObject("TooltipText");
        txtGO.transform.SetParent(go.transform, false);
        var tmp    = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text   = "Finissez les 3 mini jeux\npour voir ce qu'il y a derrière la porte";
        tmp.fontSize    = 32f;
        tmp.fontStyle   = FontStyles.Bold;
        tmp.color       = Color.white;
        tmp.alignment   = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        MenuAssets.ApplyFont(tmp);

        var trt         = tmp.rectTransform;
        trt.anchorMin   = Vector2.zero;
        trt.anchorMax   = Vector2.one;
        trt.offsetMin   = new Vector2(24f, 16f);
        trt.offsetMax   = new Vector2(-24f, -16f);

        _tooltipRT = rt;
    }

    // ── Rafraîchissement visuel ───────────────────────────────────────────────

    /// <summary>Affiche/masque le cadenas selon l'état courant de <see cref="DoorManager"/>.</summary>
    public void RefreshLockVisual()
    {
        bool locked = DoorManager.Instance == null || !DoorManager.Instance.IsUnlocked;
        if (_lockImage != null) _lockImage.gameObject.SetActive(locked);
    }

    // ── Clic ──────────────────────────────────────────────────────────────────

    private void OnClick()
    {
        if (DoorManager.Instance == null) return;

        DoorManager.Instance.EvaluateUnlock();
        RefreshLockVisual();

        if (!DoorManager.Instance.IsUnlocked)
        {
            // Afficher le tooltip "Finissez les 3 mini jeux…"
            StopAllCoroutines();
            StartCoroutine(ShowTooltip());
            return;
        }

        DoorManager.Instance.OnDoorClicked();
    }

    // ── Tooltip coroutine ─────────────────────────────────────────────────────

    private IEnumerator ShowTooltip()
    {
        if (_tooltipGroup == null) yield break;
        _tooltipShowing = true;

        // Fade in
        yield return StartCoroutine(FadeTooltip(0f, 1f, TooltipFade));

        // Maintien
        yield return new WaitForSeconds(TooltipDur);

        // Fade out
        yield return StartCoroutine(FadeTooltip(1f, 0f, TooltipFade));

        _tooltipShowing = false;
    }

    private IEnumerator FadeTooltip(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (_tooltipGroup != null)
                _tooltipGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        if (_tooltipGroup != null)
            _tooltipGroup.alpha = to;
    }
}
