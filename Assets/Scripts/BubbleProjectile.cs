using UnityEngine;

/// <summary>
/// Projectile lancé par le Shooter. Se déplace en ligne droite, rebondit sur les murs,
/// et atterrit quand il touche une bulle ou le plafond.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class BubbleProjectile : MonoBehaviour
{
    private const float Speed = 14f;

    private BubbleColor color;
    private Vector2 dir;
    private float halfW, topY, detectionRadius;

    /// <summary>Initialise le projectile et le lance dans la direction donnée.</summary>
    public void Init(BubbleColor c, Vector2 direction, float bubbleDiameter)
    {
        color = c;
        dir = direction.normalized;

        var sr = GetComponent<SpriteRenderer>();
        Sprite colorSprite = BubbleGrid.Instance?.GetSprite(c);
        if (colorSprite != null)
        {
            sr.sprite = colorSprite;
        }
        else
        {
            sr.sprite = SpriteGenerator.Circle();
            sr.color = c.ToUnityColor();
        }
        sr.sortingOrder = 4;

        transform.localScale = Vector3.one * bubbleDiameter;
        detectionRadius = bubbleDiameter * 0.45f;

        Camera cam = Camera.main;
        halfW = cam.orthographicSize * cam.aspect - bubbleDiameter * 0.5f;
        topY = cam.orthographicSize - bubbleDiameter * 0.5f;
    }

    private void Update()
    {
        transform.position += (Vector3)dir * Speed * Time.deltaTime;

        // Rebond sur les murs latéraux
        if (transform.position.x < -halfW) { dir.x = Mathf.Abs(dir.x); }
        if (transform.position.x > halfW)  { dir.x = -Mathf.Abs(dir.x); }

        // Goal : victoire quand le projectile atteint la barre du haut
        if (BubbleGoal.Instance != null && BubbleGoal.Instance.Contains(transform.position))
        {
            BubbleGameManager.Instance?.TriggerVictory();
            Destroy(gameObject);
            return;
        }

        // Plafond (fallback si le goal n'est pas initialisé)
        if (transform.position.y >= topY) { Land(); return; }

        // Collision avec une bulle de la grille
        Collider2D hit = Physics2D.OverlapCircle(transform.position, detectionRadius);
        if (hit != null)
        {
            // Bonus bubble : direct hit → coups bonus (sans condition de couleur)
            if (hit.TryGetComponent<BonusBubble>(out var bonus))
            {
                BubbleGameManager.Instance?.AwardBonusShots(bonus.BonusAmount);
                BubbleGrid.Instance?.RemoveBonusBubble(hit.GetComponent<Bubble>());
                Destroy(gameObject);
                return;
            }
            // Bulle normale
            if (hit.TryGetComponent<Bubble>(out _)) { Land(); }
        }
    }

    private void Land()
    {
        BubbleGrid.Instance.PlaceProjectile(color, transform.position);
        Destroy(gameObject);
    }
}
