using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Contrôle le déplacement horizontal fluide du joueur dans le Minijeu-Bulles.
/// Supporte clavier (éditeur) et touch drag (mobile).
/// </summary>
public class BubblesPlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float horizontalBounds = 8f;

    /// <summary>Vitesse horizontale courante du joueur en unités par seconde.</summary>
    public float HorizontalVelocity { get; private set; }

    private float previousX;
    private Vector2 touchStartPosition;
    private bool isTouching;

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

    /// <summary>Retourne une valeur entre -1 et 1 selon le clavier ou le glissement tactile.</summary>
    private float GetHorizontalInput()
    {
        // Clavier — utilisé en éditeur
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))  return -1f;
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) return  1f;

        // Touch — glissement horizontal
        var touchscreen = Touchscreen.current;
        if (touchscreen == null) return 0f;

        foreach (var touch in touchscreen.touches)
        {
            var phase = touch.phase.ReadValue();

            if (phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                touchStartPosition = touch.position.ReadValue();
                isTouching = true;
                continue;
            }

            if (!isTouching) continue;

            if (phase == UnityEngine.InputSystem.TouchPhase.Moved)
            {
                float deltaX = touch.delta.ReadValue().x;
                if (Mathf.Abs(deltaX) > 0.5f)
                    return Mathf.Sign(deltaX);
            }

            if (phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                phase == UnityEngine.InputSystem.TouchPhase.Canceled)
            {
                isTouching = false;
            }
        }

        return 0f;
    }
}
