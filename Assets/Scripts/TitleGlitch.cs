using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Effet de glitch sur le titre du menu : léger jitter permanent et
/// rafales de décalages rapides avec déplacement individuel des caractères.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
[RequireComponent(typeof(RectTransform))]
public class TitleGlitch : MonoBehaviour
{
    [Header("Jitter permanent")]
    [SerializeField] private float jitterAmplitude  = 2.5f;
    [SerializeField] [Range(0f, 1f)] private float jitterChance = 0.06f;

    [Header("Rafale de glitch")]
    [SerializeField] private float glitchIntervalMin = 1.8f;
    [SerializeField] private float glitchIntervalMax = 4.5f;
    [SerializeField] private float glitchBurstDuration = 0.14f;
    [SerializeField] private int   glitchBurstCount   = 5;
    [SerializeField] private float glitchShiftX        = 20f;
    [SerializeField] private float glitchShiftY        = 4f;
    [SerializeField] private Color glitchTint = new Color(0.65f, 0.95f, 1f, 1f);

    [Header("Déplacement des caractères")]
    [SerializeField] [Range(0f, 1f)] private float charGlitchChance = 0.45f;
    [SerializeField] private float charOffsetX = 10f;
    [SerializeField] private float charOffsetY = 5f;

    private RectTransform  rectTransform;
    private TextMeshProUGUI tmp;
    private Vector2 basePosition;
    private bool    isGlitching;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        tmp           = GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        basePosition = rectTransform.anchoredPosition;
        StartCoroutine(GlitchLoop());
    }

    private void Update()
    {
        if (isGlitching) return;

        // Léger jitter aléatoire à chaque frame
        if (Random.value < jitterChance)
        {
            rectTransform.anchoredPosition = basePosition + new Vector2(
                Random.Range(-jitterAmplitude, jitterAmplitude),
                Random.Range(-jitterAmplitude * 0.25f, jitterAmplitude * 0.25f)
            );
        }
        else
        {
            rectTransform.anchoredPosition = basePosition;
        }
    }

    private IEnumerator GlitchLoop()
    {
        while (true)
        {
            float wait = Random.Range(glitchIntervalMin, glitchIntervalMax);
            yield return new WaitForSeconds(wait);
            yield return StartCoroutine(DoGlitch());
        }
    }

    /// <summary>Exécute une rafale de micro-décalages avec glitch des caractères.</summary>
    private IEnumerator DoGlitch()
    {
        isGlitching = true;
        Color originalColor = tmp.color;
        float stepDuration  = glitchBurstDuration / glitchBurstCount;

        for (int i = 0; i < glitchBurstCount; i++)
        {
            rectTransform.anchoredPosition = basePosition + new Vector2(
                Random.Range(-glitchShiftX, glitchShiftX),
                Random.Range(-glitchShiftY, glitchShiftY)
            );

            tmp.color = (i % 2 == 0) ? glitchTint : originalColor;

            GlitchCharacterVertices();

            yield return new WaitForSeconds(stepDuration);

            RestoreCharacterVertices();
        }

        rectTransform.anchoredPosition = basePosition;
        tmp.color = originalColor;
        isGlitching = false;
    }

    /// <summary>Déplace aléatoirement certains caractères via les vertices TMP.</summary>
    private void GlitchCharacterVertices()
    {
        tmp.ForceMeshUpdate();
        TMP_TextInfo textInfo = tmp.textInfo;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;
            if (Random.value > charGlitchChance) continue;

            int       matIndex   = charInfo.materialReferenceIndex;
            int       vertIndex  = charInfo.vertexIndex;
            Vector3[] vertices   = textInfo.meshInfo[matIndex].vertices;
            Vector3   offset     = new Vector3(
                Random.Range(-charOffsetX, charOffsetX),
                Random.Range(-charOffsetY, charOffsetY),
                0f
            );

            for (int v = 0; v < 4; v++)
                vertices[vertIndex + v] += offset;
        }

        tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }

    /// <summary>Remet les vertices à leur position d'origine.</summary>
    private void RestoreCharacterVertices()
    {
        tmp.ForceMeshUpdate();
    }
}
