using System;
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
    private bool notified;

    /// <summary>Appelé une seule fois quand le projectile se pose ou est détruit.</summary>
    public Action OnLanded;

    /// <summary>Initialise le projectile et le lance dans la direction donnée.</summary>
    public void Init(BubbleColor c, Vector2 direction, float bubbleDiameter)
    {
        color = c;
        dir   = direction.normalized;

        var sr = GetComponent<SpriteRenderer>();
        Sprite colorSprite = BubbleGrid.Instance?.GetSprite(c);
        if (colorSprite != null) { sr.sprite = colorSprite; }
        else                     { sr.sprite = SpriteGenerator.Circle(); sr.color = c.ToUnityColor(); }
        sr.sortingOrder = 4;

        transform.localScale = Vector3.one * bubbleDiameter;
        detectionRadius = bubbleDiameter * 0.45f;

        Camera cam = Camera.main;
        halfW = cam.orthographicSize * cam.aspect - bubbleDiameter * 0.5f;
        topY  = cam.orthographicSize - bubbleDiameter * 0.5f;
    }

    private void Update()
    {
        transform.position += (Vector3)dir * Speed * Time.deltaTime;

        if (transform.position.x < -halfW) dir.x =  Mathf.Abs(dir.x);
        if (transform.position.x >  halfW) dir.x = -Mathf.Abs(dir.x);

        if (BubbleGoal.Instance != null && BubbleGoal.Instance.Contains(transform.position))
        {
            BubbleGameManager.Instance?.TriggerVictory();
            NotifyAndDestroy();
            return;
        }

        if (transform.position.y >= topY) { Land(); return; }

        Collider2D hit = Physics2D.OverlapCircle(transform.position, detectionRadius);
        if (hit != null)
        {
            if (hit.TryGetComponent<BonusBubble>(out var bonus))
            {
                BubbleGameManager.Instance?.AwardBonusShots(bonus.BonusAmount);
                BubbleGrid.Instance?.RemoveBonusBubble(hit.GetComponent<Bubble>());
                NotifyAndDestroy();
                return;
            }
            if (hit.TryGetComponent<Bubble>(out _)) { Land(); }
        }
    }

    private void Land()
    {
        BubbleGrid.Instance.PlaceProjectile(color, transform.position);
        NotifyAndDestroy();
    }

    /// <summary>Notifie OnLanded une seule fois puis détruit le GameObject.</summary>
    private void NotifyAndDestroy()
    {
        if (!notified) { notified = true; OnLanded?.Invoke(); }
        Destroy(gameObject);
    }

    // Sécurité : si le GO est détruit de l'extérieur (fin de partie, etc.)
    private void OnDestroy()
    {
        if (!notified) { notified = true; OnLanded?.Invoke(); }
    }
}

