using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Configures la scène Minijeu-Bulles : crée le fond en SpriteRenderer monde
/// derrière tous les objets du jeu, de la même façon que UIManager dans GameAndWatch.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class BubbleSceneSetup : MonoBehaviour
{
    [Header("Background")]
    [SerializeField] private Sprite backgroundSprite;

    private void Start()
    {
        // Background disabled — camera uses solid black.
    }

    /// <summary>
    /// Crée un SpriteRenderer dans l'espace monde centré sur la caméra,
    /// derrière tous les objets du jeu (sortingOrder -10).
    /// </summary>
    private void BuildBackground()
    {
        if (backgroundSprite == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        var go = new GameObject("Background");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = backgroundSprite;
        sr.sortingOrder = -10;

        Vector3 camPos = cam.transform.position;
        go.transform.position = new Vector3(camPos.x, camPos.y, 0f);

        float camHeight    = 2f * cam.orthographicSize;
        float camWidth     = camHeight * cam.aspect;
        float spriteHeight = backgroundSprite.bounds.size.y;
        float spriteWidth  = backgroundSprite.bounds.size.x;

        if (spriteHeight > 0f && spriteWidth > 0f)
        {
            go.transform.localScale = new Vector3(
                camWidth  / spriteWidth,
                camHeight / spriteHeight,
                1f
            );
        }
    }
}
