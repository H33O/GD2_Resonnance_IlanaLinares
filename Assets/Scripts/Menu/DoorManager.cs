using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Gère le déverrouillage de la porte centrale et l'overlay "intérieur porte".
///
/// Condition de déverrouillage : chacun des 3 mini-jeux a été joué au moins une fois
/// (i.e. au moins un score enregistré dans <see cref="ScoreManager"/> pour chaque <see cref="GameType"/>).
///
/// Raccourci dev : touche <b>D</b> → force le déverrouillage immédiat.
///
/// Quand la porte est déverrouillée, un clic l'ouvre et affiche l'overlay "intérieur porte" :
///   - image de fond  (<see cref="interiorSprite"/>)
///   - bouton centré  (<see cref="buttonSprite"/>) qui charge la scène <see cref="TargetScene"/>
/// </summary>
public class DoorManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static DoorManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Sprites")]
    [Tooltip("Image affichée en fond de l'overlay intérieur porte (fond_interieur porte.png).")]
    [SerializeField] public Sprite interiorSprite;

    [Tooltip("Sprite du bouton UI central de l'overlay (bouton UI.png).")]
    [SerializeField] public Sprite buttonSprite;

    [Header("Scène")]
    [Tooltip("Nom de la scène à charger depuis l'overlay intérieur porte.")]
    [SerializeField] public string TargetScene = "CircleArena";

    [Tooltip("Libellé affiché sur le bouton de l'overlay.")]
    [SerializeField] public string ButtonLabel = "JOUER";

    // ── Constantes UI ─────────────────────────────────────────────────────────

    private const float OverlayFadeT  = 0.30f;
    private const float BtnW          = 480f;
    private const float BtnH          = 160f;

    private static readonly Color ColOverlayDim  = new Color(0f, 0f, 0f, 0.78f);
    private static readonly Color ColBtnLabel    = Color.white;
    private static readonly Color ColLockHint    = new Color(1f, 0.85f, 0.3f, 0.90f);

    // ── État ──────────────────────────────────────────────────────────────────

    private bool        _unlocked;
    private CanvasGroup _overlayGroup;
    private bool        _overlayOpen;
    private bool        _isAnimating;

    // ── Propriété publique ────────────────────────────────────────────────────

    /// <summary>Vrai si la porte est déverrouillée.</summary>
    public bool IsUnlocked => _unlocked;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        // Raccourci dev : D = déverrouiller immédiatement
        if (Keyboard.current != null && Keyboard.current.dKey.wasPressedThisFrame)
            ForceUnlock();
    }

    // ── Initialisation ────────────────────────────────────────────────────────

    /// <summary>
    /// À appeler dans <see cref="MenuMainSetup"/> après la construction du canvas.
    /// Construit l'overlay dans le <paramref name="canvasRT"/> et évalue l'état initial.
    /// </summary>
    public void Init(RectTransform canvasRT)
    {
        BuildOverlay(canvasRT);
        EvaluateUnlock();
    }

    // ── Logique de déverrouillage ─────────────────────────────────────────────

    /// <summary>Vérifie si les 3 jeux ont été joués et met à jour l'état du verrou.</summary>
    public void EvaluateUnlock()
    {
        if (_unlocked) return;

        var sm = ScoreManager.Instance;
        if (sm == null) return;

        bool allPlayed =
            sm.GetAllScores(GameType.GameAndWatch).Count  > 0 &&
            sm.GetAllScores(GameType.BubbleShooter).Count > 0 &&
            sm.GetAllScores(GameType.BallAndGoal).Count   > 0;

        if (allPlayed) ForceUnlock();
    }

    /// <summary>Déverrouille la porte immédiatement (appel manuel ou touche D).</summary>
    public void ForceUnlock()
    {
        if (_unlocked) return;
        _unlocked = true;
        Debug.Log("[DoorManager] Porte déverrouillée !");
    }

    // ── API publique : ouvrir / fermer l'overlay ──────────────────────────────

    /// <summary>
    /// Appelé par <see cref="MenuDoor"/> lors d'un clic sur la porte.
    /// Si verrouillée : ne fait rien (MenuDoor gère l'affichage du cadenas).
    /// Si déverrouillée : toggle l'overlay intérieur porte.
    /// </summary>
    public void OnDoorClicked()
    {
        if (!_unlocked) return;
        if (_overlayOpen) CloseOverlay();
        else OpenOverlay();
    }

    // ── Construction de l'overlay ─────────────────────────────────────────────

    private void BuildOverlay(RectTransform canvasRT)
    {
        // Root plein-écran
        var root       = new GameObject("DoorInteriorOverlay");
        root.transform.SetParent(canvasRT, false);

        var rootRT     = root.AddComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = rootRT.offsetMax = Vector2.zero;

        _overlayGroup              = root.AddComponent<CanvasGroup>();
        _overlayGroup.alpha        = 0f;
        _overlayGroup.blocksRaycasts = false;
        _overlayGroup.interactable   = false;

        // Fond sombre (dim)
        var dimGO    = new GameObject("Dim");
        dimGO.transform.SetParent(rootRT, false);
        var dimImg   = dimGO.AddComponent<Image>();
        dimImg.color = ColOverlayDim;
        var dimRT    = dimImg.rectTransform;
        dimRT.anchorMin = Vector2.zero;
        dimRT.anchorMax = Vector2.one;
        dimRT.offsetMin = dimRT.offsetMax = Vector2.zero;
        dimImg.raycastTarget = true; // bloque les clics derrière

        // Image fond intérieur porte (centrée, plein-écran)
        var bgGO   = new GameObject("InteriorBg");
        bgGO.transform.SetParent(rootRT, false);
        var bgImg  = bgGO.AddComponent<Image>();
        bgImg.raycastTarget = false;
        if (interiorSprite != null)
        {
            bgImg.sprite         = interiorSprite;
            bgImg.preserveAspect = true;
        }
        else
        {
            bgImg.color = new Color(0.12f, 0.08f, 0.18f, 1f);
        }
        var bgRT         = bgImg.rectTransform;
        bgRT.anchorMin   = Vector2.zero;
        bgRT.anchorMax   = Vector2.one;
        bgRT.offsetMin   = bgRT.offsetMax = Vector2.zero;

        // Bouton centré
        BuildInteriorButton(rootRT);

        // Bouton fermer (coin haut-droite)
        BuildCloseButton(rootRT);
    }

    private void BuildInteriorButton(RectTransform root)
    {
        var btnGO  = new GameObject("InteriorPlayButton");
        btnGO.transform.SetParent(root, false);

        var img    = btnGO.AddComponent<Image>();
        if (buttonSprite != null)
        {
            img.sprite         = buttonSprite;
            img.type           = Image.Type.Sliced;
            img.color          = Color.white;
        }
        else
        {
            img.color = new Color(0.85f, 0.78f, 1f, 1f);
        }

        var rt             = img.rectTransform;
        rt.anchorMin       = new Vector2(0.5f, 0.5f);
        rt.anchorMax       = new Vector2(0.5f, 0.5f);
        rt.pivot           = new Vector2(0.5f, 0.5f);
        rt.sizeDelta       = new Vector2(BtnW, BtnH);
        rt.anchoredPosition = new Vector2(0f, -200f);

        // Label
        var lblGO   = new GameObject("Label");
        lblGO.transform.SetParent(rt, false);
        var tmp     = lblGO.AddComponent<TextMeshProUGUI>();
        tmp.text        = ButtonLabel;
        tmp.fontSize    = 52f;
        tmp.fontStyle   = FontStyles.Bold;
        tmp.color       = ColBtnLabel;
        tmp.alignment   = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        var lrt         = tmp.rectTransform;
        lrt.anchorMin   = Vector2.zero;
        lrt.anchorMax   = Vector2.one;
        lrt.offsetMin   = lrt.offsetMax = Vector2.zero;

        var btn           = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(LoadTargetScene);
    }

    private void BuildCloseButton(RectTransform root)
    {
        var btnGO   = new GameObject("CloseButton");
        btnGO.transform.SetParent(root, false);

        var img     = btnGO.AddComponent<Image>();
        img.color   = new Color(1f, 1f, 1f, 0.08f);

        var rt      = img.rectTransform;
        rt.anchorMin      = new Vector2(1f, 1f);
        rt.anchorMax      = new Vector2(1f, 1f);
        rt.pivot          = new Vector2(1f, 1f);
        rt.sizeDelta      = new Vector2(100f, 100f);
        rt.anchoredPosition = new Vector2(-30f, -50f);

        var lblGO   = new GameObject("X");
        lblGO.transform.SetParent(rt, false);
        var tmp     = lblGO.AddComponent<TextMeshProUGUI>();
        tmp.text        = "✕";
        tmp.fontSize    = 48f;
        tmp.color       = new Color(1f, 1f, 1f, 0.6f);
        tmp.alignment   = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        var lrt         = tmp.rectTransform;
        lrt.anchorMin   = Vector2.zero;
        lrt.anchorMax   = Vector2.one;
        lrt.offsetMin   = lrt.offsetMax = Vector2.zero;

        var btn           = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(CloseOverlay);
    }

    // ── Animation overlay ──────────────────────────────────────────────────────

    private void OpenOverlay()
    {
        if (_isAnimating) return;
        _overlayOpen                   = true;
        _overlayGroup.blocksRaycasts   = true;
        _overlayGroup.interactable     = true;
        StartCoroutine(FadeOverlay(0f, 1f));
    }

    private void CloseOverlay()
    {
        if (_isAnimating) return;
        _overlayOpen                   = false;
        _overlayGroup.blocksRaycasts   = false;
        _overlayGroup.interactable     = false;
        StartCoroutine(FadeOverlay(1f, 0f));
    }

    private IEnumerator FadeOverlay(float from, float to)
    {
        _isAnimating = true;
        float elapsed = 0f;
        while (elapsed < OverlayFadeT)
        {
            elapsed            += Time.deltaTime;
            _overlayGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / OverlayFadeT));
            yield return null;
        }
        _overlayGroup.alpha = to;
        _isAnimating        = false;
    }

    // ── Chargement de scène ────────────────────────────────────────────────────

    private void LoadTargetScene()
    {
        if (string.IsNullOrEmpty(TargetScene)) return;

        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadScene(TargetScene, ButtonLabel);
        else
            UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(TargetScene);
    }
}
