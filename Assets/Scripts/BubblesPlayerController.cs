using UnityEngine;

/// <summary>
/// Contrôle le déplacement horizontal fluide du joueur dans le Minijeu-Bulles.
/// </summary>
public class BubblesPlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float horizontalBounds = 8f;

    /// <summary>Vitesse horizontale courante du joueur en unités par seconde.</summary>
    public float HorizontalVelocity { get; private set; }

    private float previousX;

    private void Start()
    {
        previousX = transform.position.x;
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsGameActive)
            return;

        float input = GetHorizontalInput();

        Vector3 newPosition = transform.position + Vector3.right * input * moveSpeed * Time.deltaTime;
        newPosition.x = Mathf.Clamp(newPosition.x, -horizontalBounds, horizontalBounds);
        transform.position = newPosition;

        HorizontalVelocity = (transform.position.x - previousX) / Time.deltaTime;
        previousX = transform.position.x;
    }

    private float GetHorizontalInput()
    {
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
            return -1f;
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            return 1f;
        return 0f;
    }
}
