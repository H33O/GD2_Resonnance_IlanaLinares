using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
public class Bubble : MonoBehaviour
{
    public BubbleColor ColorType { get; private set; }
    public int Row { get; private set; }
    public int Col { get; private set; }

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr.sprite == null)
            sr.sprite = SpriteGenerator.Circle();
    }

    /// <summary>Initialise la bulle avec une couleur et sa position dans la grille.</summary>
    public void Init(BubbleColor color, int row, int col)
    {
        ColorType = color;
        Row = row;
        Col = col;
        sr.color = color.ToUnityColor();
    }

    /// <summary>Anime la disparition de la bulle (réduction d'échelle).</summary>
    public void Pop() => StartCoroutine(PopRoutine());

    /// <summary>Fait tomber la bulle hors écran quand elle est décrochée du plafond.</summary>
    public void Fall() => StartCoroutine(FallRoutine());

    private IEnumerator PopRoutine()
    {
        float t = 0f;
        Vector3 startScale = transform.localScale;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t / 0.2f);
            yield return null;
        }
        Destroy(gameObject);
    }

    private IEnumerator FallRoutine()
    {
        GetComponent<CircleCollider2D>().enabled = false;
        float speed = 3f;
        while (transform.position.y > -12f)
        {
            speed += 20f * Time.deltaTime;
            transform.position += Vector3.down * speed * Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }
}
