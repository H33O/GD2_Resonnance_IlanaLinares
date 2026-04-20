using UnityEngine;

/// <summary>
/// Anime un RectTransform en bobbing vertical (haut ↔ bas) avec une sinusoïde.
/// Attach ce composant sur le GameObject "CharacterSlot" créé par MenuSceneSetup.
///
/// L'animation est désactivée par défaut (enabled = false dans l'Inspector).
/// Active-la quand tu es prêt à l'animer.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class MenuCharacterSlot : MonoBehaviour
{
    // ── Paramètres ────────────────────────────────────────────────────────────

    [Header("Bobbing")]
    [Tooltip("Amplitude verticale du déplacement en pixels UI.")]
    public float amplitude = 18f;

    [Tooltip("Durée d'un cycle complet (aller-retour) en secondes.")]
    public float period = 2.4f;

    [Tooltip("Phase de départ en secondes (décale le début du cycle).")]
    public float phaseOffset = 0f;

    [Header("Squash & Stretch")]
    [Tooltip("Applique un léger squash/stretch sur l'axe Y au point le plus bas/haut.")]
    public bool squashStretch = true;

    [Tooltip("Intensité du squash (0 = aucun effet, 0.06 = subtil).")]
    public float squashIntensity = 0.06f;

    // ── État interne ──────────────────────────────────────────────────────────

    private RectTransform rt;
    private Vector2       basePosition;
    private Vector3       baseScale;

    // ── Init ──────────────────────────────────────────────────────────────────

    private void Awake()
    {
        rt           = GetComponent<RectTransform>();
        basePosition = rt.anchoredPosition;
        baseScale    = rt.localScale;
    }

    private void OnEnable()
    {
        // Repart proprement depuis la position de base à chaque activation
        rt.anchoredPosition = basePosition;
        rt.localScale       = baseScale;
    }

    private void OnDisable()
    {
        // Remet en place lors de la désactivation
        rt.anchoredPosition = basePosition;
        rt.localScale       = baseScale;
    }

    // ── Animation ─────────────────────────────────────────────────────────────

    private void Update()
    {
        if (period <= 0f) return;

        float t       = (Time.time + phaseOffset) / period;
        float sine    = Mathf.Sin(t * Mathf.PI * 2f);   // [-1, 1]
        float offsetY = sine * amplitude;

        rt.anchoredPosition = basePosition + new Vector2(0f, offsetY);

        if (squashStretch)
        {
            // Légèrement aplati en bas, allongé en haut
            float scaleY = baseScale.y * (1f - sine * squashIntensity);
            float scaleX = baseScale.x * (1f + sine * squashIntensity * 0.5f);
            rt.localScale = new Vector3(scaleX, scaleY, baseScale.z);
        }
    }
}
