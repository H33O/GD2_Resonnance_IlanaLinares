using UnityEngine;

/// <summary>
/// Micro-point blanc qui rebondit dans les limites visibles de la caméra orthographique.
/// Utilisé comme décoration de fond dans la scène GameAndWatch — effet luciole.
/// Chaque instance gère son propre déplacement ; aucune ombre pour rester discret.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class GAWFirefly : MonoBehaviour
{
    // ── Constantes ────────────────────────────────────────────────────────────

    private const float RadiusMin     = 0.03f;
    private const float RadiusMax     = 0.07f;
    private const float SpeedMin      = 1.2f;
    private const float SpeedMax      = 3.0f;
    private const float AlphaMin      = 0.25f;
    private const float AlphaMax      = 0.70f;
    private const int   SortingOrder  = -8;

    // ── État ──────────────────────────────────────────────────────────────────

    private float   _radius;
    private Vector2 _velocity;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        _radius = Random.Range(RadiusMin, RadiusMax);

        var sr          = GetComponent<SpriteRenderer>();
        sr.sprite       = SpriteGenerator.CreateCircle(32);
        sr.sortingOrder = SortingOrder;
        sr.color        = new Color(1f, 1f, 1f, Random.Range(AlphaMin, AlphaMax));

        transform.localScale = Vector3.one * (_radius * 2f);
        transform.position   = RandomPositionInBounds(GetCameraBounds());

        RandomizeVelocity();
    }

    private void Update()
    {
        var bounds = GetCameraBounds();
        Move(bounds);
    }

    // ── Mouvement ─────────────────────────────────────────────────────────────

    private void Move(Rect bounds)
    {
        var pos = (Vector2)transform.position + _velocity * Time.deltaTime;

        if (pos.x - _radius < bounds.xMin)
        {
            pos.x       = bounds.xMin + _radius;
            _velocity.x = Mathf.Abs(_velocity.x);
        }
        else if (pos.x + _radius > bounds.xMax)
        {
            pos.x       = bounds.xMax - _radius;
            _velocity.x = -Mathf.Abs(_velocity.x);
        }

        if (pos.y - _radius < bounds.yMin)
        {
            pos.y       = bounds.yMin + _radius;
            _velocity.y = Mathf.Abs(_velocity.y);
        }
        else if (pos.y + _radius > bounds.yMax)
        {
            pos.y       = bounds.yMax - _radius;
            _velocity.y = -Mathf.Abs(_velocity.y);
        }

        transform.position = new Vector3(pos.x, pos.y, 0f);
    }

    // ── Utilitaires ───────────────────────────────────────────────────────────

    private void RandomizeVelocity()
    {
        float angle = Random.Range(15f, 75f) * Mathf.Deg2Rad;
        int   sx    = Random.value > 0.5f ? 1 : -1;
        int   sy    = Random.value > 0.5f ? 1 : -1;
        float speed = Random.Range(SpeedMin, SpeedMax);
        _velocity   = new Vector2(Mathf.Cos(angle) * sx, Mathf.Sin(angle) * sy) * speed;
    }

    private static Vector2 RandomPositionInBounds(Rect bounds)
    {
        return new Vector2(
            Random.Range(bounds.xMin + RadiusMax, bounds.xMax - RadiusMax),
            Random.Range(bounds.yMin + RadiusMax, bounds.yMax - RadiusMax));
    }

    private static Rect GetCameraBounds()
    {
        var cam = Camera.main;
        if (cam == null) return new Rect(-9f, -16f, 18f, 32f);
        float h = cam.orthographicSize;
        float w = h * cam.aspect;
        return new Rect(-w, -h, w * 2f, h * 2f);
    }
}
