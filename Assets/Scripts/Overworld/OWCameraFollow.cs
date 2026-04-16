using UnityEngine;

/// <summary>
/// Caméra qui suit le joueur verticalement dans l'overworld.
/// Ne suit pas l'axe X (caméra centrée horizontalement).
/// Contrainte : ne descend pas en dessous d'un Y minimum.
/// </summary>
public class OWCameraFollow : MonoBehaviour
{
    [Header("Cible")]
    [SerializeField] private Transform target;

    [Header("Suivi")]
    [SerializeField] private float smoothTime   = 0.2f;
    [SerializeField] private float yOffset      = 2f;
    [SerializeField] private float minY         = 0f;

    [Header("Limites horizontales")]
    [SerializeField] private float fixedX       = 0f;

    private float velocityY;

    private void LateUpdate()
    {
        if (target == null) return;

        float targetY   = Mathf.Max(target.position.y + yOffset, minY);
        float smoothedY = Mathf.SmoothDamp(transform.position.y, targetY, ref velocityY, smoothTime);

        transform.position = new Vector3(fixedX, smoothedY, transform.position.z);
    }

    /// <summary>Assigne la cible de la caméra au runtime.</summary>
    public void SetTarget(Transform newTarget) => target = newTarget;
}
