using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Effet de glitch sur le titre du menu en world space (TextMeshPro).
/// Jitter permanent et rafales de décalages avec déplacement des caractères.
/// </summary>
[RequireComponent(typeof(TextMeshPro))]
public class TitleGlitchWorld : MonoBehaviour
{
    [Header("Jitter permanent")]
    [SerializeField] private float jitterAmplitude     = 0.012f;
    [SerializeField] [Range(0f, 1f)] private float jitterChance = 0.06f;

    [Header("Rafale de glitch")]
    [SerializeField] private float glitchIntervalMin   = 1.8f;
    [SerializeField] private float glitchIntervalMax   = 4.5f;
    [SerializeField] private float glitchBurstDuration = 0.14f;
    [SerializeField] private int   glitchBurstCount    = 5;
    [SerializeField] private float glitchShiftX        = 0.10f;
    [SerializeField] private float glitchShiftY        = 0.02f;
    [SerializeField] private Color glitchTint          = new Color(0.75f, 0.75f, 0.75f, 1f);

    [Header("Déplacement des caractères")]
    [SerializeField] [Range(0f, 1f)] private float charGlitchChance = 0.45f;
    [SerializeField] private float charOffsetX = 0.05f;
    [SerializeField] private float charOffsetY = 0.025f;

    private TextMeshPro tmp;
    private Vector3     basePosition;
    private bool        isGlitching;

    private void Awake()  => tmp = GetComponent<TextMeshPro>();

    private void Start()
    {
        basePosition = transform.localPosition;
        StartCoroutine(GlitchLoop());
    }

    private void Update()
    {
        if (isGlitching) return;

        if (Random.value < jitterChance)
        {
            transform.localPosition = basePosition + new Vector3(
                Random.Range(-jitterAmplitude, jitterAmplitude),
                Random.Range(-jitterAmplitude * 0.25f, jitterAmplitude * 0.25f),
                0f
            );
        }
        else
        {
            transform.localPosition = basePosition;
        }
    }

    private IEnumerator GlitchLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(glitchIntervalMin, glitchIntervalMax));
            yield return StartCoroutine(DoGlitch());
        }
    }

    /// <summary>Rafale de micro-décalages avec déplacement des vertices TMP.</summary>
    private IEnumerator DoGlitch()
    {
        isGlitching = true;
        Color originalColor = tmp.color;
        float stepDuration  = glitchBurstDuration / glitchBurstCount;

        for (int i = 0; i < glitchBurstCount; i++)
        {
            transform.localPosition = basePosition + new Vector3(
                Random.Range(-glitchShiftX, glitchShiftX),
                Random.Range(-glitchShiftY, glitchShiftY),
                0f
            );
            tmp.color = (i % 2 == 0) ? glitchTint : originalColor;
            GlitchCharacterVertices();
            yield return new WaitForSeconds(stepDuration);
            RestoreCharacterVertices();
        }

        transform.localPosition = basePosition;
        tmp.color   = originalColor;
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

            int       matIdx  = charInfo.materialReferenceIndex;
            int       vtxIdx  = charInfo.vertexIndex;
            Vector3[] verts   = textInfo.meshInfo[matIdx].vertices;
            Vector3   offset  = new Vector3(
                Random.Range(-charOffsetX, charOffsetX),
                Random.Range(-charOffsetY, charOffsetY),
                0f
            );
            for (int v = 0; v < 4; v++) verts[vtxIdx + v] += offset;
        }
        tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }

    private void RestoreCharacterVertices() => tmp.ForceMeshUpdate();
}
