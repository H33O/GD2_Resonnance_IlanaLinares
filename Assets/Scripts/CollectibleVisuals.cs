using UnityEngine;

public class CollectibleVisuals : MonoBehaviour
{
    [SerializeField] private Color humanColor = new Color(0.9f, 0.3f, 0.3f);

    private void Awake()
    {
        CreateSprite();
    }

    private void CreateSprite()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
        }

        if (sr.sprite == null)
        {
            Texture2D texture = new Texture2D(16, 16);
            Color[] pixels = new Color[16 * 16];
            
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = humanColor;
            }
            
            texture.SetPixels(pixels);
            texture.filterMode = FilterMode.Point;
            texture.Apply();
            
            sr.sprite = Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 32);
            sr.sortingOrder = 5;
        }
    }
}
