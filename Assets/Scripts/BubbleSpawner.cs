using UnityEngine;

/// <summary>
/// Génère une grille hexagonale de bulles colorées en haut de l'écran au démarrage.
/// </summary>
public class BubbleSpawner : MonoBehaviour
{
    [Header("Grille")]
    [SerializeField] private int rows = 4;
    [SerializeField] private int columns = 9;
    [SerializeField] private float bubbleSpacing = 1.0f;
    [SerializeField] private float topMargin = 0.4f;
    [SerializeField] private float bubbleScale = 0.55f;

    [Header("Couleurs")]
    [SerializeField] private Color[] bubbleColors =
    {
        new Color(0.92f, 0.23f, 0.23f, 1f),
        new Color(0.20f, 0.44f, 0.92f, 1f),
        new Color(0.20f, 0.80f, 0.32f, 1f),
        new Color(1.00f, 0.86f, 0.10f, 1f),
        new Color(0.72f, 0.20f, 0.90f, 1f)
    };

    private void Start()
    {
        SpawnGrid();
    }

    private void SpawnGrid()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        float screenTop = cam.orthographicSize - topMargin;
        float rowHeight = bubbleSpacing * Mathf.Sqrt(3f) * 0.5f;
        float startX = -(columns - 1) * bubbleSpacing * 0.5f;

        for (int row = 0; row < rows; row++)
        {
            float rowOffset = (row % 2 != 0) ? bubbleSpacing * 0.5f : 0f;
            float y = screenTop - row * rowHeight;

            for (int col = 0; col < columns; col++)
            {
                float x = startX + col * bubbleSpacing + rowOffset;
                CreateBubble(new Vector3(x, y, 0f));
            }
        }
    }

    private void CreateBubble(Vector3 position)
    {
        Color color = (bubbleColors != null && bubbleColors.Length > 0)
            ? bubbleColors[Random.Range(0, bubbleColors.Length)]
            : Color.white;

        GameObject go = new GameObject("Bubble");
        go.transform.SetParent(transform);
        go.transform.position = position;
        go.transform.localScale = Vector3.one * bubbleScale;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteGenerator.Circle();
        sr.color = color;
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 0;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        go.AddComponent<Bubble>();
    }
}
