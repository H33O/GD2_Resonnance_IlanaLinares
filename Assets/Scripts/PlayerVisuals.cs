using UnityEngine;

public class PlayerVisuals : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private Sprite playerSprite;
    [SerializeField] private Color chariotColor = new Color(1f, 1f, 1f);
    [SerializeField] private Vector2 chariotSize = new Vector2(1.2f, 0.6f);

    private void Start()
    {
        CreateChariotSprite();
    }

    private void CreateChariotSprite()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = gameObject.AddComponent<SpriteRenderer>();

        if (playerSprite != null)
        {
            sr.sprite = playerSprite;
        }
        else
        {
            Texture2D texture = new Texture2D(64, 32);
            Color[] pixels = new Color[64 * 32];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = chariotColor;
            texture.SetPixels(pixels);
            texture.filterMode = FilterMode.Point;
            texture.Apply();
            sr.sprite = Sprite.Create(texture, new Rect(0, 0, 64, 32), new Vector2(0.5f, 0.5f), 32);
        }

        sr.sortingOrder = 10;

        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
            col.size = chariotSize;

        transform.localScale = Vector3.one;
    }
}