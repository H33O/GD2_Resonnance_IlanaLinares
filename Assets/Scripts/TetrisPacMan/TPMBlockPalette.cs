using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Palette de blocs colorés affichée en bas de l'écran.
/// Le joueur sélectionne une couleur, puis tape sur une cellule vide pour poser,
/// ou retape sur un bloc existant pour le retirer.
/// </summary>
public class TPMBlockPalette : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static TPMBlockPalette Instance { get; private set; }

    // ── Couleurs disponibles ───────────────────────────────────────────────────

    public static readonly Color[] BlockColors = new Color[]
    {
        new Color(0.20f, 0.55f, 1.00f, 1f),   // Bleu
        new Color(0.15f, 0.85f, 0.30f, 1f),   // Vert
        new Color(1.00f, 0.75f, 0.05f, 1f),   // Jaune
        new Color(1.00f, 0.35f, 0.15f, 1f),   // Rouge-orange
        new Color(0.75f, 0.25f, 1.00f, 1f),   // Violet
    };

    public static readonly string[] BlockNames = new string[]
    {
        "BLEU", "VERT", "OR", "FEU", "VIOLET"
    };

    // ── État ──────────────────────────────────────────────────────────────────

    private int selectedIndex = 0;

    private readonly List<GameObject> slotObjects  = new();
    private readonly List<Image>      slotBorders  = new();
    private readonly List<Image>      slotInners   = new();

    // ── Événements ────────────────────────────────────────────────────────────

    /// <summary>Couleur actuellement sélectionnée.</summary>
    public Color SelectedColor => BlockColors[selectedIndex];
    public int   SelectedIndex => selectedIndex;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── Construction UI ───────────────────────────────────────────────────────

    /// <summary>
    /// Construit la palette dans le canvas fourni.
    /// À appeler par TPMSceneSetup après la construction du canvas HUD.
    /// </summary>
    public void Build(Canvas parentCanvas)
    {
        // ── Fond de la barre de palette ───────────────────────────────────────
        var barGO = new GameObject("PaletteBar");
        barGO.transform.SetParent(parentCanvas.transform, false);
        var barRT        = barGO.AddComponent<RectTransform>();
        barRT.anchorMin  = new Vector2(0f, 0f);
        barRT.anchorMax  = new Vector2(1f, 0f);
        barRT.pivot      = new Vector2(0.5f, 0f);
        barRT.sizeDelta  = new Vector2(0f, 140f);
        barRT.anchoredPosition = Vector2.zero;
        var barImg       = barGO.AddComponent<Image>();
        barImg.color     = new Color(0.05f, 0.05f, 0.12f, 0.92f);

        // ── Titre "BLOCS" ─────────────────────────────────────────────────────
        var titleGO = new GameObject("PaletteTitle");
        titleGO.transform.SetParent(barGO.transform, false);
        var titleRT         = titleGO.AddComponent<RectTransform>();
        titleRT.anchorMin   = new Vector2(0f, 0.72f);
        titleRT.anchorMax   = new Vector2(1f, 1f);
        titleRT.offsetMin   = titleRT.offsetMax = Vector2.zero;
        var titleTMP        = titleGO.AddComponent<TMPro.TextMeshProUGUI>();
        titleTMP.text       = "BLOCS";
        titleTMP.fontSize   = 18f;
        titleTMP.color      = new Color(0.7f, 0.7f, 0.8f, 1f);
        titleTMP.alignment  = TMPro.TextAlignmentOptions.Center;
        titleTMP.fontStyle  = TMPro.FontStyles.Bold;

        // ── Layout horizontal des slots ────────────────────────────────────────
        var layoutGO = new GameObject("SlotsLayout");
        layoutGO.transform.SetParent(barGO.transform, false);
        var layoutRT        = layoutGO.AddComponent<RectTransform>();
        layoutRT.anchorMin  = new Vector2(0.02f, 0f);
        layoutRT.anchorMax  = new Vector2(0.98f, 0.72f);
        layoutRT.offsetMin  = layoutRT.offsetMax = Vector2.zero;
        var hlg = layoutGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing           = 8f;
        hlg.childAlignment    = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;
        hlg.padding = new RectOffset(4, 4, 4, 4);

        for (int i = 0; i < BlockColors.Length; i++)
            BuildSlot(layoutGO.transform, i);

        RefreshSelection();
    }

    private void BuildSlot(Transform parent, int index)
    {
        var slotGO = new GameObject($"Slot_{index}");
        slotGO.transform.SetParent(parent, false);
        slotObjects.Add(slotGO);

        // ── Border (bord blanc quand sélectionné) ─────────────────────────────
        var borderImg   = slotGO.AddComponent<Image>();
        borderImg.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        slotBorders.Add(borderImg);

        // ── Corps coloré ──────────────────────────────────────────────────────
        var innerGO   = new GameObject("Inner");
        innerGO.transform.SetParent(slotGO.transform, false);
        var innerRT         = innerGO.AddComponent<RectTransform>();
        innerRT.anchorMin   = new Vector2(0.08f, 0.08f);
        innerRT.anchorMax   = new Vector2(0.92f, 0.92f);
        innerRT.offsetMin   = innerRT.offsetMax = Vector2.zero;
        var innerImg        = innerGO.AddComponent<Image>();
        innerImg.color      = BlockColors[index];
        innerImg.sprite     = SpriteGenerator.CreateColoredSquare(BlockColors[index]);
        slotInners.Add(innerImg);

        // ── Label couleur ─────────────────────────────────────────────────────
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(slotGO.transform, false);
        var labelRT         = labelGO.AddComponent<RectTransform>();
        labelRT.anchorMin   = new Vector2(0f, 0f);
        labelRT.anchorMax   = new Vector2(1f, 0.30f);
        labelRT.offsetMin   = labelRT.offsetMax = Vector2.zero;
        var labelTMP        = labelGO.AddComponent<TMPro.TextMeshProUGUI>();
        labelTMP.text       = BlockNames[index];
        labelTMP.fontSize   = 10f;
        labelTMP.color      = Color.white;
        labelTMP.alignment  = TMPro.TextAlignmentOptions.Center;

        // ── Touche tactile ────────────────────────────────────────────────────
        var btn = slotGO.AddComponent<Button>();
        int capturedIndex = index;
        btn.onClick.AddListener(() => SelectBlock(capturedIndex));

        // Feedback tactile : scale on press
        var nav = btn.navigation;
        nav.mode = Navigation.Mode.None;
        btn.navigation = nav;
    }

    // ── Logique sélection ─────────────────────────────────────────────────────

    /// <summary>Sélectionne le bloc d'index donné.</summary>
    public void SelectBlock(int index)
    {
        selectedIndex = Mathf.Clamp(index, 0, BlockColors.Length - 1);
        RefreshSelection();
    }

    private void RefreshSelection()
    {
        for (int i = 0; i < slotBorders.Count; i++)
        {
            bool sel = (i == selectedIndex);
            slotBorders[i].color = sel
                ? new Color(1f, 1f, 1f, 1f)           // bord blanc = sélectionné
                : new Color(0.25f, 0.25f, 0.30f, 1f); // bord sombre = non-sélectionné

            // Scale légère du slot sélectionné
            slotObjects[i].transform.localScale = sel
                ? Vector3.one * 1.10f
                : Vector3.one;
        }
    }
}
