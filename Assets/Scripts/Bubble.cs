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
    /// <param name="usesColoredSprite">Si vrai, le sprite est déjà coloré — on n'applique pas de teinte.</param>
    public void Init(BubbleColor color, int row, int col, bool usesColoredSprite = false)
    {
        ColorType = color;
        Row = row;
        Col = col;
        if (!usesColoredSprite)
            sr.color = color.ToUnityColor();
    }

    /// <summary>Met à jour les indices de grille après une descente.</summary>
    public void UpdateGridPosition(int row, int col)
    {
        Row = row;
        Col = col;
    }

    /// <summary>Anime le déplacement fluide vers une position cible (utilisé lors de la descente).</summary>
    public void MoveTo(Vector3 targetPos, float duration) => StartCoroutine(MoveRoutine(targetPos, duration));

    private IEnumerator MoveRoutine(Vector3 targetPos, float duration)
    {
        Vector3 startPos = transform.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
        transform.position = targetPos;
    }

    /// <summary>Anime la disparition de la bulle avec un effet glitch (aberration chromatique + jitter).</summary>
    public void Pop() => StartCoroutine(GlitchPopRoutine());

    private IEnumerator GlitchPopRoutine()
    {
        GetComponent<CircleCollider2D>().enabled = false;

        Vector3 basePos      = transform.position;
        float   initialScale = transform.localScale.x;

        // Copies chromatiques : rouge décalée à gauche, cyan décalée à droite
        GameObject redCopy  = CreateGlitchCopy(new Color(1f,    0.1f, 0.1f, 0.8f));
        GameObject cyanCopy = CreateGlitchCopy(new Color(0.1f,  1f,   0.95f, 0.8f));
        SpriteRenderer redSR  = redCopy.GetComponent<SpriteRenderer>();
        SpriteRenderer cyanSR = cyanCopy.GetComponent<SpriteRenderer>();

        const float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Jitter de position aléatoire
            Vector3 jitter = new Vector3(
                Random.Range(-0.08f, 0.08f),
                Random.Range(-0.04f, 0.04f), 0f);
            transform.position = basePos + jitter;

            // Décalage chromatique horizontal qui diminue avec le temps
            float chroma = Mathf.Lerp(0.12f, 0f, t);
            redCopy.transform.position  = basePos + jitter + Vector3.left  * chroma;
            cyanCopy.transform.position = basePos + jitter + Vector3.right * chroma;

            // Échelle : scintillement + réduction progressive (easeIn)
            float scaleMult = Mathf.Lerp(1.15f, 0f, t * t) * (1f + Random.Range(-0.15f, 0.15f));
            Vector3 glitchScale         = Vector3.one * initialScale * Mathf.Max(0f, scaleMult);
            transform.localScale        = glitchScale;
            redCopy.transform.localScale  = glitchScale;
            cyanCopy.transform.localScale = glitchScale;

            // Fondu des copies chromatiques
            float alpha  = Mathf.Lerp(0.85f, 0f, t);
            redSR.color  = new Color(1f,   0.1f, 0.1f,  alpha);
            cyanSR.color = new Color(0.1f, 1f,   0.95f, alpha);

            yield return null;
        }

        Destroy(redCopy);
        Destroy(cyanCopy);
        Destroy(gameObject);
    }

    private GameObject CreateGlitchCopy(Color color)
    {
        var go    = new GameObject("GlitchCopy");
        go.transform.position   = transform.position;
        go.transform.localScale = transform.localScale;
        var copySR        = go.AddComponent<SpriteRenderer>();
        copySR.sprite       = sr.sprite;
        copySR.color        = color;
        copySR.sortingOrder = sr.sortingOrder + 1;
        return go;
    }

    /// <summary>Fait tomber la bulle hors écran quand elle est décrochée du plafond.</summary>
    public void Fall() => StartCoroutine(FallRoutine());

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
        BubbleGrid.Instance?.OnBubbleFallComplete();
        Destroy(gameObject);
    }
}
