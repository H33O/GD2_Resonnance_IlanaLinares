using UnityEngine;

/// <summary>
/// Fait suivre la caméra orthographique le joueur verticalement (scroll vertical).
/// Activé à partir du niveau 0 sur tous les niveaux.
/// La caméra reste contrainte dans les limites verticales du monde.
/// </summary>
public class TBCameraFollow : MonoBehaviour
{
    // ── Configuration ─────────────────────────────────────────────────────────

    /// <summary>Niveau d'activation — 0 = tous les niveaux.</summary>
    public const int  ActivationLevel = 0;
    public const float SmoothSpeed   = 6.0f;  // unités/seconde (lerp)

    // ── État ──────────────────────────────────────────────────────────────────

    private Transform    playerTransform;
    private float        worldHalfH;   // limite verticale du monde
    private float        camHalfH;     // demi-hauteur orthographique de la caméra

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>Attache TBCameraFollow à la caméra principale pour tout niveau ≥ ActivationLevel.</summary>
    public static void AttachIfNeeded(int levelIndex, float extendedHalfH)
    {
        if (levelIndex < ActivationLevel) return;

        var cam = Camera.main;
        if (cam == null) return;

        // Retire l'ancien composant éventuel
        var old = cam.GetComponent<TBCameraFollow>();
        if (old != null) Destroy(old);

        var follow        = cam.gameObject.AddComponent<TBCameraFollow>();
        follow.worldHalfH = extendedHalfH;
        follow.camHalfH   = cam.orthographicSize;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        var player = FindFirstObjectByType<TBPlayerController>();
        if (player != null) playerTransform = player.transform;
    }

    private void LateUpdate()
    {
        if (playerTransform == null)
        {
            // Tentative de récupération tardive (respawn)
            var player = FindFirstObjectByType<TBPlayerController>();
            if (player != null) playerTransform = player.transform;
            return;
        }

        var cam = Camera.main;
        if (cam == null) return;

        float targetY = playerTransform.position.y;

        // Clamp pour ne pas déborder du monde
        float minY = -worldHalfH + camHalfH;
        float maxY =  worldHalfH - camHalfH;
        targetY    = Mathf.Clamp(targetY, minY, maxY);

        float smoothY = Mathf.Lerp(cam.transform.position.y, targetY, Time.deltaTime * SmoothSpeed);
        cam.transform.position = new Vector3(0f, smoothY, -10f);
    }
}
