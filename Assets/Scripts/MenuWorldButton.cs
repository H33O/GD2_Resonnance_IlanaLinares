using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Bouton du menu en world space 2D.
/// Affiche un label TMP, un rectangle de fond (SpriteRenderer) et un contour.
/// Détecte le tap / clic via Physics2D et réagit au rebond de la balle.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class MenuWorldButton : MonoBehaviour
{
    // ── Configuration ─────────────────────────────────────────────────────────

    [Header("Apparence")]
    [SerializeField] private Vector2 size          = new Vector2(3.2f, 0.65f);
    [SerializeField] private Color   bgColor       = new Color(0.08f, 0.08f, 0.08f, 0.92f);
    [SerializeField] private Color   outlineColor  = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] private Color   flashColor    = new Color(1f, 1f, 1f, 0.55f);
    [SerializeField] private string  label         = "PLAY";
    [SerializeField] private float   fontSize      = 1.0f;
    [SerializeField] private int     sortingBase   = 5;

    [Header("Bounce")]
    [SerializeField] private float bounceScaleUp   = 1.10f;
    [SerializeField] private float bounceScaleDown = 0.96f;
    [SerializeField] private float bounceDuration  = 0.20f;

    // ── Références internes ───────────────────────────────────────────────────

    private SpriteRenderer  bgRenderer;
    private SpriteRenderer  outlineRenderer;
    private BoxCollider2D   col;
    private Vector3         baseScale;
    private bool            isReacting;

    // Callback au clic
    public event System.Action OnClick;

    // ── Bounds world space (utilisé par MenuBall) ─────────────────────────────

    public Bounds WorldBounds => col.bounds;

    // ── Initialisation ────────────────────────────────────────────────────────

    private void Awake()
    {
        col          = GetComponent<BoxCollider2D>();
        col.size     = size;
        col.isTrigger = true;
        baseScale    = Vector3.one;
    }

    private void Start()
    {
        BuildVisuals();
    }

    private void BuildVisuals()
    {
        // Fond
        var bgGO = new GameObject("BtnBG");
        bgGO.transform.SetParent(transform, false);
        bgRenderer           = bgGO.AddComponent<SpriteRenderer>();
        bgRenderer.sprite    = SpriteGenerator.CreateWhiteSquare();
        bgRenderer.color     = bgColor;
        bgRenderer.sortingOrder = sortingBase;
        bgGO.transform.localScale = new Vector3(size.x, size.y, 1f);

        // Contour (légèrement plus grand)
        var outlineGO = new GameObject("BtnOutline");
        outlineGO.transform.SetParent(transform, false);
        outlineRenderer           = outlineGO.AddComponent<SpriteRenderer>();
        outlineRenderer.sprite    = SpriteGenerator.CreateWhiteSquare();
        outlineRenderer.color     = outlineColor;
        outlineRenderer.sortingOrder = sortingBase - 1;
        float border              = 0.04f;
        outlineGO.transform.localScale = new Vector3(size.x + border, size.y + border, 1f);

        // Texte
        var textGO = new GameObject("BtnLabel");
        textGO.transform.SetParent(transform, false);
        textGO.transform.localPosition = new Vector3(0f, 0f, -0.1f);
        var tmp             = textGO.AddComponent<TextMeshPro>();
        tmp.text            = label;
        tmp.fontSize        = fontSize;
        tmp.fontStyle       = FontStyles.Bold;
        tmp.color           = Color.white;
        tmp.alignment       = TextAlignmentOptions.Center;
        tmp.sortingOrder    = sortingBase + 1;

        var rt = tmp.rectTransform;
        rt.sizeDelta = size;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition3D = Vector3.zero;
    }

    // ── Détection du tap ──────────────────────────────────────────────────────

    private void Update()
    {
        DetectTap();
    }

    private void DetectTap()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (!Input.GetMouseButtonDown(0)) return;
        Vector3 wp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        wp.z = 0f;
        if (col.bounds.Contains(wp)) FireClick();
#else
        foreach (var touch in Input.touches)
        {
            if (touch.phase != TouchPhase.Began) continue;
            Vector3 wp = Camera.main.ScreenToWorldPoint(touch.position);
            wp.z = 0f;
            if (col.bounds.Contains(wp)) { FireClick(); break; }
        }
#endif
    }

    private void FireClick()
    {
        if (!isReacting) StartCoroutine(BounceRoutine());
        OnClick?.Invoke();
    }

    // ── Réaction au rebond de la balle ────────────────────────────────────────

    /// <summary>Déclenche l'animation visuelle lorsque la balle touche ce bouton.</summary>
    public void TriggerBounceReaction()
    {
        if (isReacting) return;
        StartCoroutine(BounceRoutine());
        StartCoroutine(FlashRoutine());
        StartCoroutine(OutlinePulseRoutine());
    }

    // ── Coroutines ────────────────────────────────────────────────────────────

    private IEnumerator BounceRoutine()
    {
        isReacting = true;
        float half = bounceDuration * 0.5f;

        yield return StartCoroutine(ScaleTo(bounceScaleUp,   half));
        yield return StartCoroutine(ScaleTo(bounceScaleDown, half * 0.5f));
        yield return StartCoroutine(ScaleTo(1f,              half * 0.5f));

        transform.localScale = baseScale;
        isReacting = false;
    }

    private IEnumerator ScaleTo(float target, float duration)
    {
        float elapsed = 0f;
        float start   = transform.localScale.x;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            float s  = Mathf.Lerp(start, target, t);
            transform.localScale = baseScale * s;
            yield return null;
        }
    }

    private IEnumerator FlashRoutine()
    {
        if (bgRenderer == null) yield break;

        Color original = bgRenderer.color;
        float elapsed  = 0f;
        float duration = 0.22f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t  = elapsed / duration;
            bgRenderer.color = Color.Lerp(flashColor, original, t);
            yield return null;
        }
        bgRenderer.color = original;
    }

    private IEnumerator OutlinePulseRoutine()
    {
        if (outlineRenderer == null) yield break;

        Color pulse    = new Color(1f, 1f, 1f, 0.9f);
        Color original = outlineColor;
        float elapsed  = 0f;
        float duration = 0.38f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.Clamp01(elapsed / duration);
            outlineRenderer.color = Color.Lerp(pulse, original, t);
            yield return null;
        }
        outlineRenderer.color = original;
    }
}
