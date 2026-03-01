using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Effet de glitch sur un bouton UI : jitter permanent, rafales périodiques
/// et animation de rebond au moment de l'appui.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class ButtonGlitch : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Jitter permanent")]
    [SerializeField] private float jitterAmplitude = 1.2f;
    [SerializeField] [Range(0f, 1f)] private float jitterChance = 0.04f;

    [Header("Rafale de glitch")]
    [SerializeField] private float glitchIntervalMin   = 2.5f;
    [SerializeField] private float glitchIntervalMax   = 6f;
    [SerializeField] private float glitchBurstDuration = 0.12f;
    [SerializeField] private int   glitchBurstCount    = 4;
    [SerializeField] private float glitchShiftX        = 12f;
    [SerializeField] private float glitchShiftY        = 3f;

    [Header("Déplacement des caractères")]
    [SerializeField] [Range(0f, 1f)] private float charGlitchChance = 0.4f;
    [SerializeField] private float charOffsetX = 7f;
    [SerializeField] private float charOffsetY = 4f;

    [Header("Feedback pression")]
    [SerializeField] private float pressScaleDown  = 0.92f;
    [SerializeField] private float pressScaleDuration = 0.08f;
    [SerializeField] private float releaseScaleDuration = 0.12f;

    private RectTransform    rectTransform;
    private TextMeshProUGUI  tmp;
    private Vector2          basePosition;
    private Vector3          baseScale;
    private bool             isGlitching;
    private bool             isPressed;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        tmp           = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void Start()
    {
        basePosition = rectTransform.anchoredPosition;
        baseScale    = rectTransform.localScale;
        StartCoroutine(GlitchLoop());
    }

    private void Update()
    {
        if (isGlitching || isPressed) return;

        if (Random.value < jitterChance)
        {
            rectTransform.anchoredPosition = basePosition + new Vector2(
                Random.Range(-jitterAmplitude, jitterAmplitude),
                Random.Range(-jitterAmplitude * 0.2f, jitterAmplitude * 0.2f)
            );
        }
        else
        {
            rectTransform.anchoredPosition = basePosition;
        }
    }

    // ── Feedback pression ────────────────────────────────────────────────────

    /// <summary>Déclenche l'animation de rebond au toucher / clic.</summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        StopCoroutine("PressAnimation");
        StartCoroutine(PressAnimation());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
    }

    private IEnumerator PressAnimation()
    {
        // Scale down
        float elapsed = 0f;
        while (elapsed < pressScaleDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / pressScaleDuration;
            float s = Mathf.Lerp(1f, pressScaleDown, t);
            rectTransform.localScale = baseScale * s;
            yield return null;
        }

        // Scale back up
        elapsed = 0f;
        while (elapsed < releaseScaleDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / releaseScaleDuration;
            float s = Mathf.Lerp(pressScaleDown, 1f, t);
            rectTransform.localScale = baseScale * s;
            yield return null;
        }

        rectTransform.localScale    = baseScale;
        rectTransform.anchoredPosition = basePosition;
    }

    // ── Glitch ───────────────────────────────────────────────────────────────

    private IEnumerator GlitchLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(glitchIntervalMin, glitchIntervalMax));
            yield return StartCoroutine(DoGlitch());
        }
    }

    /// <summary>Rafale de micro-décalages avec glitch des caractères TMP.</summary>
    private IEnumerator DoGlitch()
    {
        isGlitching = true;
        float stepDuration = glitchBurstDuration / glitchBurstCount;

        for (int i = 0; i < glitchBurstCount; i++)
        {
            rectTransform.anchoredPosition = basePosition + new Vector2(
                Random.Range(-glitchShiftX, glitchShiftX),
                Random.Range(-glitchShiftY, glitchShiftY)
            );

            if (tmp != null) GlitchCharacterVertices();
            yield return new WaitForSeconds(stepDuration);
            if (tmp != null) RestoreCharacterVertices();
        }

        rectTransform.anchoredPosition = basePosition;
        isGlitching = false;
    }

    private void GlitchCharacterVertices()
    {
        tmp.ForceMeshUpdate();
        TMP_TextInfo textInfo = tmp.textInfo;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;
            if (Random.value > charGlitchChance) continue;

            int       matIndex  = charInfo.materialReferenceIndex;
            int       vertIndex = charInfo.vertexIndex;
            Vector3[] vertices  = textInfo.meshInfo[matIndex].vertices;
            Vector3   offset    = new Vector3(
                Random.Range(-charOffsetX, charOffsetX),
                Random.Range(-charOffsetY, charOffsetY),
                0f
            );
            for (int v = 0; v < 4; v++)
                vertices[vertIndex + v] += offset;
        }

        tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }

    private void RestoreCharacterVertices()
    {
        tmp.ForceMeshUpdate();
    }
}
