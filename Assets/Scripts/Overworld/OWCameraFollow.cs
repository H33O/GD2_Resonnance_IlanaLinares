using UnityEngine;

/// <summary>
/// Caméra qui suit le joueur en vue top-down (axes X et Y).
/// Smooth sur les deux axes. Offset optionnel.
/// </summary>
public class OWCameraFollow : MonoBehaviour
{
    [Header("Cible")]
    [SerializeField] private Transform target;

    [Header("Suivi")]
    [SerializeField] private float smoothTime = 0.18f;
    [SerializeField] private float yOffset    = 0f;
    [SerializeField] private float minY       = -9999f;

    // Conservés pour compatibilité sérialisée — non utilisés en top-down
    [SerializeField] private float fixedX     = 0f;

    private float velocityX;
    private float velocityY;

    private void LateUpdate()
    {
        if (target == null)
        {
            // Auto-recherche du joueur si la target n'est pas assignée
            var p = GameObject.FindWithTag("Player");
            if (p != null) target = p.transform;
            return;
        }

        float targetX   = target.position.x;
        float targetY   = Mathf.Max(target.position.y + yOffset, minY);

        float smoothedX = Mathf.SmoothDamp(transform.position.x, targetX, ref velocityX, smoothTime);
        float smoothedY = Mathf.SmoothDamp(transform.position.y, targetY, ref velocityY, smoothTime);

        transform.position = new Vector3(smoothedX, smoothedY, transform.position.z);
    }

    /// <summary>Assigne la cible de la caméra au runtime.</summary>
    public void SetTarget(Transform newTarget) => target = newTarget;
}
