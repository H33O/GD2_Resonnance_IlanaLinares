using UnityEngine;

public class DebugColliders : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider != null)
        {
            Gizmos.color = collider.isTrigger ? Color.green : Color.red;
            Vector3 center = transform.position + (Vector3)collider.offset;
            Vector3 size = collider.size;
            Gizmos.DrawWireCube(center, size);
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.1f);
        }
    }
}
