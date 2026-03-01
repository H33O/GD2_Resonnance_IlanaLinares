using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Effet de grésillage écran déclenché sur demande : flash d'overlay + micro-jitter caméra.
/// À attacher sur la MainCamera. Appeler Trigger() pour déclencher une rafale.
/// </summary>
public class ScreenGlitch : MonoBehaviour
{
    public static ScreenGlitch Instance { get; private set; }

    [Header("Rafale")]
    [SerializeField] private int   burstCount        = 5;
    [SerializeField] private float burstStepDuration = 0.04f;

    [Header("Intensité")]
    [SerializeField] [Range(0f, 0.3f)] private float maxOverlayAlpha    = 0.07f;
    [SerializeField] private float cameraJitterAmount = 0.04f;
    [SerializeField] private Color glitchTint         = new Color(0.85f, 1f, 1f, 1f);

    private Camera      mainCamera;
    private Vector3     cameraBasePosition;
    private CanvasGroup glitchGroup;
    private Image       overlayImage;
    private bool        isRunning;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        mainCamera         = Camera.main;
        cameraBasePosition = mainCamera.transform.position;
        BuildOverlay();
    }

    /// <summary>Déclenche une rafale de grésillage.</summary>
    public void Trigger()
    {
        if (isRunning) return;
        StartCoroutine(DoGlitch());
    }

    private void BuildOverlay()
    {
        var canvasGO = new GameObject("GlitchOverlay");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        glitchGroup                = canvasGO.AddComponent<CanvasGroup>();
        glitchGroup.alpha          = 0f;
        glitchGroup.blocksRaycasts = false;
        glitchGroup.interactable   = false;

        var imgGO = new GameObject("Overlay");
        imgGO.transform.SetParent(canvasGO.transform, false);
        overlayImage               = imgGO.AddComponent<Image>();
        overlayImage.color         = Color.white;
        overlayImage.raycastTarget = false;
        var rt = overlayImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private IEnumerator DoGlitch()
    {
        isRunning = true;

        for (int i = 0; i < burstCount; i++)
        {
            overlayImage.color = (i % 2 == 0)
                ? new Color(1f, 1f, 1f, Random.Range(0f, maxOverlayAlpha))
                : new Color(glitchTint.r, glitchTint.g, glitchTint.b,
                            Random.Range(0f, maxOverlayAlpha * 0.5f));

            glitchGroup.alpha = 1f;

            mainCamera.transform.position = cameraBasePosition + new Vector3(
                Random.Range(-cameraJitterAmount, cameraJitterAmount),
                Random.Range(-cameraJitterAmount, cameraJitterAmount),
                0f
            );

            yield return new WaitForSeconds(burstStepDuration);
        }

        glitchGroup.alpha             = 0f;
        mainCamera.transform.position = cameraBasePosition;
        isRunning = false;
    }
}
