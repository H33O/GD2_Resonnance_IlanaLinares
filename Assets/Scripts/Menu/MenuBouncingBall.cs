using UnityEngine;

/// <summary>
/// Balle blanche qui rebondit dans les limites visibles de la caméra orthographique.
/// Utilisée comme décoration de fond dans la scène Menu.
/// </summary>
public class MenuBouncingBall : MonoBehaviour
{
    // ── Constantes ────────────────────────────────────────────────────────────

    private const float BallRadius     = 0.35f;
    private const float BallSpeed      = 3.8f;
    private const float SortingOrder   = -9;

    private static readonly Color BallColor  = new Color(0.15f, 0.15f, 0.15f, 1f);
    private static readonly Color ShadowColor = new Color(0.6f, 0.6f, 0.6f, 0.35f);

    // ── État ──────────────────────────────────────────────────────────────────

    private Vector2 _velocity;
    private SpriteRenderer _ballRenderer;
    private Transform _shadowTransform;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        BuildBall();
        BuildShadow();
        RandomizeVelocity();
    }

    private void Update()
    {
        var bounds = GetCameraBounds();
        MoveBall(bounds);
        UpdateShadow(bounds);
    }

    // ── Construction ──────────────────────────────────────────────────────────

    private void BuildBall()
    {
        _ballRenderer               = gameObject.AddComponent<SpriteRenderer>();
        _ballRenderer.sprite        = SpriteGenerator.CreateCircle(128);
        _ballRenderer.color         = BallColor;
        _ballRenderer.sortingOrder  = (int)SortingOrder;

        transform.localScale = Vector3.one * (BallRadius * 2f);
        transform.position   = Vector3.zero;
    }

    private void BuildShadow()
    {
        var shadowGO              = new GameObject("BallShadow");
        shadowGO.transform.SetParent(transform.parent);
        var sr                    = shadowGO.AddComponent<SpriteRenderer>();
        sr.sprite                 = SpriteGenerator.CreateCircle(128);
        sr.color                  = ShadowColor;
        sr.sortingOrder           = (int)SortingOrder - 1;
        shadowGO.transform.localScale = new Vector3(BallRadius * 2.4f, BallRadius * 0.6f, 1f);
        _shadowTransform = shadowGO.transform;
    }

    private void RandomizeVelocity()
    {
        float angle = Random.Range(20f, 70f) * Mathf.Deg2Rad;
        int signX   = Random.value > 0.5f ? 1 : -1;
        int signY   = Random.value > 0.5f ? 1 : -1;
        _velocity   = new Vector2(Mathf.Cos(angle) * signX, Mathf.Sin(angle) * signY) * BallSpeed;
    }

    // ── Mouvement ─────────────────────────────────────────────────────────────

    private void MoveBall(Rect bounds)
    {
        var pos = (Vector2)transform.position + _velocity * Time.deltaTime;

        if (pos.x - BallRadius < bounds.xMin)
        {
            pos.x         = bounds.xMin + BallRadius;
            _velocity.x   = Mathf.Abs(_velocity.x);
        }
        else if (pos.x + BallRadius > bounds.xMax)
        {
            pos.x         = bounds.xMax - BallRadius;
            _velocity.x   = -Mathf.Abs(_velocity.x);
        }

        if (pos.y - BallRadius < bounds.yMin)
        {
            pos.y         = bounds.yMin + BallRadius;
            _velocity.y   = Mathf.Abs(_velocity.y);
        }
        else if (pos.y + BallRadius > bounds.yMax)
        {
            pos.y         = bounds.yMax - BallRadius;
            _velocity.y   = -Mathf.Abs(_velocity.y);
        }

        transform.position = new Vector3(pos.x, pos.y, 0f);
    }

    private void UpdateShadow(Rect bounds)
    {
        if (_shadowTransform == null) return;

        float floorY   = bounds.yMin + BallRadius * 0.5f;
        float dist     = transform.position.y - floorY;
        float t        = Mathf.Clamp01(1f - dist / (bounds.height * 0.5f));
        float scaleX   = Mathf.Lerp(BallRadius * 1.2f, BallRadius * 2.4f, t);
        float scaleY   = Mathf.Lerp(BallRadius * 0.2f, BallRadius * 0.6f, t);

        _shadowTransform.localScale = new Vector3(scaleX, scaleY, 1f);
        _shadowTransform.position   = new Vector3(transform.position.x, floorY, 0f);

        var color              = _shadowTransform.GetComponent<SpriteRenderer>().color;
        color.a                = Mathf.Lerp(0.05f, 0.35f, t);
        _shadowTransform.GetComponent<SpriteRenderer>().color = color;
    }

    // ── Utilitaire ────────────────────────────────────────────────────────────

    private static Rect GetCameraBounds()
    {
        var cam = Camera.main;
        if (cam == null) return new Rect(-9f, -16f, 18f, 32f);

        float h = cam.orthographicSize;
        float w = h * cam.aspect;
        return new Rect(-w, -h, w * 2f, h * 2f);
    }

    private void OnDestroy()
    {
        if (_shadowTransform != null)
            Destroy(_shadowTransform.gameObject);
    }
}
