using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Anime un RectTransform UI en parallaxe.
/// - Sur device  : utilise le gyroscope (Input.gyro).
/// - En éditeur  : suit la souris normalisée dans la fenêtre.
///
/// Le sprite doit être légèrement plus grand que l'écran (oversize) pour
/// que le décalage ne révèle pas les bords. Un oversize de 1.15 est suffisant
/// pour une amplitude de déplacement standard.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class MenuParallaxBackground : MonoBehaviour
{
    // ── Paramètres publics ────────────────────────────────────────────────────

    /// <summary>Amplitude max du déplacement en pixels UI (référence 1080×1920).</summary>
    [Tooltip("Amplitude max du déplacement en pixels UI (espace de référence 1080×1920).")]
    public float amplitude = 40f;

    /// <summary>Vitesse de lissage (lerp). Plus la valeur est basse, plus le suivi est mou.</summary>
    [Tooltip("Vitesse de lissage. Valeur entre 1 et 15 recommandée.")]
    public float smoothSpeed = 6f;

    /// <summary>Inverse le sens horizontal.</summary>
    [Tooltip("Inverse l'axe horizontal.")]
    public bool invertX = false;

    /// <summary>Inverse le sens vertical.</summary>
    [Tooltip("Inverse l'axe vertical.")]
    public bool invertY = false;

    // ── État interne ──────────────────────────────────────────────────────────

    private RectTransform rt;
    private Vector2       currentOffset;
    private bool          gyroAvailable;

    // Gyro — filtre passe-bas pour lisser les tremblements
    private Quaternion gyroReference  = Quaternion.identity;
    private const float GyroLowPass   = 0.05f;
    private Vector2    gyroFiltered;

    // ── Init ──────────────────────────────────────────────────────────────────

    private void Awake()
    {
        rt = GetComponent<RectTransform>();

#if !UNITY_EDITOR
        gyroAvailable = SystemInfo.supportsGyroscope;
        if (gyroAvailable)
        {
            Input.gyro.enabled = true;
            gyroReference      = Input.gyro.attitude;
        }
#endif
    }

    // ── Update ────────────────────────────────────────────────────────────────

    private void Update()
    {
        Vector2 target = gyroAvailable ? ReadGyro() : ReadMouse();

        if (invertX) target.x = -target.x;
        if (invertY) target.y = -target.y;

        currentOffset = Vector2.Lerp(currentOffset, target * amplitude, smoothSpeed * Time.deltaTime);
        rt.anchoredPosition = currentOffset;
    }

    // ── Lecture des entrées ───────────────────────────────────────────────────

    /// <summary>
    /// Retourne un vecteur [-1, 1] à partir de la position normalisée de la souris.
    /// </summary>
    private static Vector2 ReadMouse()
    {
        float x = Mathf.Clamp01(Input.mousePosition.x / Screen.width)  * 2f - 1f;
        float y = Mathf.Clamp01(Input.mousePosition.y / Screen.height) * 2f - 1f;
        return new Vector2(x, y);
    }

    /// <summary>
    /// Retourne un vecteur [-1, 1] à partir de l'inclinaison du gyroscope,
    /// avec filtre passe-bas pour éviter les tremblements.
    /// </summary>
    private Vector2 ReadGyro()
    {
        // Tilt relatif à l'orientation de référence (capture au démarrage)
        Quaternion attitude = Input.gyro.attitude;
        Quaternion delta    = Quaternion.Inverse(gyroReference) * attitude;

        // Extrait les angles d'euler et les normalise vers [-1, 1]
        Vector3 euler = delta.eulerAngles;
        float rawX    = NormalizeAngle(euler.y) / 30f;   // lacet  → horizontal
        float rawY    = NormalizeAngle(euler.x) / 30f;   // tangage → vertical

        // Filtre passe-bas exponentiel
        gyroFiltered = Vector2.Lerp(gyroFiltered, new Vector2(rawX, rawY), GyroLowPass);
        return new Vector2(
            Mathf.Clamp(gyroFiltered.x, -1f, 1f),
            Mathf.Clamp(gyroFiltered.y, -1f, 1f));
    }

    /// <summary>Ramène un angle Unity (0-360) dans [-180, 180].</summary>
    private static float NormalizeAngle(float angle)
        => angle > 180f ? angle - 360f : angle;
}
