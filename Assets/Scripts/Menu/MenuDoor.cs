using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Widget "Porte" du menu principal.
///
/// Layout bas-centre du Canvas :
///   - Rectangle bordeaux/gris sombre cliquable (bouton porte)
///   - Au clic : 3 slots de jeu apparaissent en fade-in (Game and Watch, Bulles, TiltBall)
///   - Un second clic referme les slots
///
/// Référence de résolution : 1080 × 1920.
/// </summary>
public class MenuDoor : MonoBehaviour
{
    // ── Mise en page ──────────────────────────────────────────────────────────

    private const float DoorW       = 420f;
    private const float DoorH       = 640f;
    private const float DoorOffsetY = 60f;

    private const float SlotW       = 320f;
    private const float SlotH       = 140f;
    private const float SlotGap     = 22f;
    private const float SlotFadeT   = 0.28f;

    // ── Palette ───────────────────────────────────────────────────────────────

    private static readonly Color ColDoorBg     = new Color(0.14f, 0.10f, 0.16f, 1f);
    private static readonly Color ColDoorBorder = new Color(0.60f, 0.55f, 0.70f, 0.55f);
    private static readonly Color ColSlotBg     = new Color(0.18f, 0.18f, 0.28f, 1f);
    private static readonly Color ColSlotBorder = new Color(0.70f, 0.70f, 0.90f, 0.45f);
    private static readonly Color ColSlotLabel  = Color.white;
    private static readonly Color ColSlotSub    = new Color(1f, 1f, 1f, 0.45f);
    private static readonly Color ColDoorHint   = new Color(1f, 1f, 1f, 0.30f);

    // ── Données des 3 jeux ────────────────────────────────────────────────────

    private static readonly string[] SlotTitles    = { "GAME AND WATCH", "BUBBLE SHOOTER", "TILT BALL" };
    private static readonly string[] SlotSubtitles = { "Classic", "Casual", "Skill" };

    // ── État ──────────────────────────────────────────────────────────────────

    private CanvasGroup slotsGroup;
    private bool        slotsVisible;
    private bool        isAnimating;

    // ── Initialisation ────────────────────────────────────────────────────────

    /// <summary>Construit la porte dans le <paramref name="canvasRT"/> fourni.</summary>
    public void Init(RectTransform canvasRT)
    {
        var doorRT = BuildDoorBackground(canvasRT);
        BuildHintLabel(doorRT);
        BuildSlots(doorRT);
    }

    // ── Fond de la porte ──────────────────────────────────────────────────────

    private RectTransform BuildDoorBackground(RectTransform parent)
    {
        var go = new GameObject("Door");
        go.transform.SetParent(parent, false);

        var img           = go.AddComponent<Image>();
        img.sprite        = SpriteGenerator.CreateWhiteSquare();
        img.color         = ColDoorBg;

        var rt            = img.rectTransform;
        rt.anchorMin      = new Vector2(0.5f, 0f);
        rt.anchorMax      = new Vector2(0.5f, 0f);
        rt.pivot          = new Vector2(0.5f, 0f);
        rt.sizeDelta      = new Vector2(DoorW, DoorH);
        rt.anchoredPosition = new Vector2(0f, DoorOffsetY);

        BuildOutline(rt, ColDoorBorder, 3f);

        var btn           = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(ToggleSlots);

        return rt;
    }

    // ── Label "OUVRIR" bas de porte ───────────────────────────────────────────

    private static void BuildHintLabel(RectTransform doorRT)
    {
        var go      = new GameObject("HintLabel");
        go.transform.SetParent(doorRT, false);

        var tmp     = go.AddComponent<TextMeshProUGUI>();
        tmp.text    = "▲  OUVRIR";
        tmp.fontSize = 22f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color   = ColDoorHint;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        var rt      = tmp.rectTransform;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0.13f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // ── 3 slots de jeu (centrés dans la porte, cachés au départ) ─────────────

    private void BuildSlots(RectTransform doorRT)
    {
        var container = new GameObject("SlotsContainer");
        container.transform.SetParent(doorRT, false);

        var containerRT       = container.AddComponent<RectTransform>();
        containerRT.anchorMin = Vector2.zero;
        containerRT.anchorMax = Vector2.one;
        containerRT.offsetMin = containerRT.offsetMax = Vector2.zero;

        slotsGroup                = container.AddComponent<CanvasGroup>();
        slotsGroup.alpha          = 0f;
        slotsGroup.blocksRaycasts = false;
        slotsGroup.interactable   = false;

        float totalH = SlotH * 3f + SlotGap * 2f;
        float startY = totalH * 0.5f + SlotGap * 0.5f;

        for (int i = 0; i < 3; i++)
        {
            float cy = startY - i * (SlotH + SlotGap);
            BuildSlot(containerRT, i, cy);
        }
    }

    private static void BuildSlot(RectTransform parent, int index, float centerY)
    {
        var go = new GameObject($"Slot_{index}");
        go.transform.SetParent(parent, false);

        var img           = go.AddComponent<Image>();
        img.sprite        = SpriteGenerator.CreateWhiteSquare();
        img.color         = ColSlotBg;
        img.raycastTarget = false;

        var rt            = img.rectTransform;
        rt.anchorMin      = new Vector2(0.5f, 0.5f);
        rt.anchorMax      = new Vector2(0.5f, 0.5f);
        rt.pivot          = new Vector2(0.5f, 0.5f);
        rt.sizeDelta      = new Vector2(SlotW, SlotH);
        rt.anchoredPosition = new Vector2(0f, centerY);

        BuildOutline(rt, ColSlotBorder, 2f);

        // Titre du jeu
        var titleGO        = new GameObject("Title");
        titleGO.transform.SetParent(rt, false);
        var titleTmp       = titleGO.AddComponent<TextMeshProUGUI>();
        titleTmp.text      = SlotTitles[index];
        titleTmp.fontSize  = 30f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color     = ColSlotLabel;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.raycastTarget = false;
        var titleRT        = titleTmp.rectTransform;
        titleRT.anchorMin  = new Vector2(0f, 0.5f);
        titleRT.anchorMax  = Vector2.one;
        titleRT.offsetMin  = new Vector2(12f, 0f);
        titleRT.offsetMax  = new Vector2(-12f, 0f);

        // Sous-titre genre
        var subGO        = new GameObject("Subtitle");
        subGO.transform.SetParent(rt, false);
        var subTmp       = subGO.AddComponent<TextMeshProUGUI>();
        subTmp.text      = SlotSubtitles[index];
        subTmp.fontSize  = 22f;
        subTmp.fontStyle = FontStyles.Normal;
        subTmp.color     = ColSlotSub;
        subTmp.alignment = TextAlignmentOptions.Center;
        subTmp.raycastTarget = false;
        var subRT        = subTmp.rectTransform;
        subRT.anchorMin  = Vector2.zero;
        subRT.anchorMax  = new Vector2(1f, 0.5f);
        subRT.offsetMin  = new Vector2(12f, 0f);
        subRT.offsetMax  = new Vector2(-12f, 0f);
    }

    // ── Toggle slots ──────────────────────────────────────────────────────────

    private void ToggleSlots()
    {
        if (isAnimating) return;
        slotsVisible = !slotsVisible;
        StartCoroutine(FadeSlots(slotsVisible));
    }

    private IEnumerator FadeSlots(bool show)
    {
        isAnimating = true;
        float from  = show ? 0f : 1f;
        float to    = show ? 1f : 0f;

        slotsGroup.blocksRaycasts = show;
        slotsGroup.interactable   = show;

        float elapsed = 0f;
        while (elapsed < SlotFadeT)
        {
            elapsed          += Time.deltaTime;
            slotsGroup.alpha  = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / SlotFadeT));
            yield return null;
        }
        slotsGroup.alpha = to;
        isAnimating      = false;
    }

    // ── Helper bordure ────────────────────────────────────────────────────────

    private static void BuildOutline(RectTransform parent, Color color, float thickness)
    {
        var go = new GameObject("Outline");
        go.transform.SetParent(parent, false);
        go.transform.SetAsFirstSibling();

        var img           = go.AddComponent<Image>();
        img.sprite        = SpriteGenerator.CreateWhiteSquare();
        img.color         = color;
        img.raycastTarget = false;

        var rt       = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(-thickness, -thickness);
        rt.offsetMax = new Vector2( thickness,  thickness);
    }
}
