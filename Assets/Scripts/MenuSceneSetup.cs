using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Construit procéduralement le menu principal sur un Canvas Screen Space Overlay.
///
/// Layout :
///   - Fond noir
///   - HUD haut-gauche (Score cliquable) + haut-droite (Horloge) via <see cref="MenuMainHud"/>
///   - Porte blanche centrée (plane assignable via Inspector) via <see cref="DoorManager"/>
///   - Bouton GAME bas-droite → panneau slide via <see cref="MenuGameSelectPanel"/>
/// </summary>
public class MenuSceneSetup : MonoBehaviour
{
    // ── Init ──────────────────────────────────────────────────────────────────

    private void Awake()
    {
        EnsureEventSystem();
        EnsureSceneTransition();
    }

    private void Start()
    {
        var canvasRT = BuildCanvas();

        BuildBackground(canvasRT);
        BuildHud(canvasRT);
        BuildDoor(canvasRT);
        BuildGamesButton(canvasRT);
    }

    // ── Canvas ────────────────────────────────────────────────────────────────

    private static RectTransform BuildCanvas()
    {
        var go  = new GameObject("MenuCanvas");
        var c   = go.AddComponent<Canvas>();
        c.renderMode   = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 0;

        var scaler                 = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight  = 1f;

        go.AddComponent<GraphicRaycaster>();
        return c.GetComponent<RectTransform>();
    }

    // ── Fond ──────────────────────────────────────────────────────────────────

    private static void BuildBackground(RectTransform parent)
    {
        var go            = new GameObject("Background");
        go.transform.SetParent(parent, false);
        var img           = go.AddComponent<Image>();
        img.sprite        = SpriteGenerator.CreateWhiteSquare();
        img.color         = new Color(0.05f, 0.04f, 0.07f, 1f);
        img.raycastTarget = false;
        StretchFull(img.rectTransform);
    }

    // ── HUD (Score + Horloge) ─────────────────────────────────────────────────

    private static void BuildHud(RectTransform canvasRT)
    {
        var go = new GameObject("MenuMainHud");
        go.transform.SetParent(canvasRT, false);
        var rt = go.AddComponent<RectTransform>();
        StretchFull(rt);
        go.AddComponent<MenuMainHud>().Init(rt);
    }

    // ── Porte (centre écran) ──────────────────────────────────────────────────

    private static void BuildDoor(RectTransform canvasRT)
    {
        var go = new GameObject("MenuDoorRoot");
        go.transform.SetParent(canvasRT, false);
        var rt = go.AddComponent<RectTransform>();
        StretchFull(rt);
        go.AddComponent<DoorManager>().InitUI(rt);
    }

    // ── Bouton GAME + panneau de sélection ────────────────────────────────────

    private static void BuildGamesButton(RectTransform canvasRT)
    {
        var panel = MenuGameSelectPanel.Create(canvasRT);

        var btnGO = new GameObject("GameButton");
        btnGO.transform.SetParent(canvasRT, false);

        var img           = btnGO.AddComponent<Image>();
        img.sprite        = SpriteGenerator.CreateWhiteSquare();
        img.color         = new Color(1f, 1f, 1f, 0.08f);

        var rt            = img.rectTransform;
        rt.anchorMin      = new Vector2(1f, 0f);
        rt.anchorMax      = new Vector2(1f, 0f);
        rt.pivot          = new Vector2(1f, 0f);
        rt.sizeDelta      = new Vector2(200f, 76f);
        rt.anchoredPosition = new Vector2(-28f, 180f);

        var lgo       = new GameObject("Label");
        lgo.transform.SetParent(rt, false);
        var tmp       = lgo.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text      = "GAME";
        tmp.fontSize  = 32f;
        tmp.fontStyle = TMPro.FontStyles.Bold;
        tmp.color     = new Color(1f, 1f, 1f, 0.70f);
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        var lrt       = tmp.rectTransform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;

        var btn           = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(panel.Show);
    }

    // ── Utilitaires ───────────────────────────────────────────────────────────

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    private static void EnsureSceneTransition()
    {
        if (SceneTransition.Instance != null) return;
        new GameObject("SceneTransition").AddComponent<SceneTransition>();
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}

