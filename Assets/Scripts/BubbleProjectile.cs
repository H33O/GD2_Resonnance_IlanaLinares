using System;
using UnityEngine;

/// <summary>
/// Projectile lancé par le Shooter.
///
/// Comportement :
///   - Se déplace en ligne droite à vitesse constante.
///   - Rebondit sur les murs gauche et droit.
///   - Atterrit sur le plafond ou au contact d'une bulle.
///   - Même couleur qu'une bulle touchée → BFS pop + score.
///   - Couleur différente → se colle à la grille à côté.
///
/// Robustesse collision :
///   - <c>minTravelSqr</c> calculé dynamiquement depuis la rangée la plus basse de la
///     grille : la détection n'est activée qu'une fois le projectile passé cette rangée.
///   - <c>OverlapCircleAll</c> filtré sur le composant <see cref="Bubble"/> uniquement :
///     aucun faux positif avec les colliders UI, BubbleGoal, etc.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class BubbleProjectile : MonoBehaviour
{
    private const float Speed = 14f;

    // ── État ──────────────────────────────────────────────────────────────────

    private BubbleColor color;
    private Vector2     dir;
    private float       halfW, topY, detectionRadius;
    private bool        notified;
    private float       minTravelSqr;   // distance² à parcourir avant d'activer la détection
    private Vector3     spawnPos;

    /// <summary>Appelé une seule fois quand le projectile atterrit ou est détruit.</summary>
    public Action OnLanded;

    // ── Initialisation ────────────────────────────────────────────────────────

    /// <summary>Initialise le projectile et le lance dans la direction donnée.</summary>
    public void Init(BubbleColor c, Vector2 direction, float bubbleDiameter)
    {
        color    = c;
        dir      = direction.normalized;
        spawnPos = transform.position;

        // Applique le sprite / couleur
        var sr = GetComponent<SpriteRenderer>();
        Sprite colorSprite = BubbleGrid.Instance?.GetSprite(c);
        if (colorSprite != null) { sr.sprite = colorSprite; }
        else                     { sr.sprite = SpriteGenerator.Circle(); sr.color = c.ToUnityColor(); }
        sr.sortingOrder = 4;

        transform.localScale = Vector3.one * bubbleDiameter;
        detectionRadius      = bubbleDiameter * 0.48f;

        Camera cam = Camera.main;
        halfW = cam.orthographicSize * cam.aspect - bubbleDiameter * 0.5f;
        topY  = cam.orthographicSize - bubbleDiameter * 0.5f;

        // seuil de sécurité : le projectile ne peut détecter une bulle qu'après avoir
        // franchi la zone vide entre le canon et la première rangée vide de la grille.
        // On prend 80 % de cette distance pour laisser une petite marge d'angle.
        float spawnedBottomY = BubbleGrid.Instance != null
            ? BubbleGrid.Instance.SpawnedBottomY
            : 0f;
        float safeDistance = Mathf.Max(0.3f,
            (spawnedBottomY - transform.position.y) * 0.80f);
        minTravelSqr = safeDistance * safeDistance;
    }

    // ── Mise à jour ───────────────────────────────────────────────────────────

    private void Update()
    {
        // Déplacement
        transform.position += (Vector3)(dir * Speed * Time.deltaTime);

        // Rebonds murs gauche / droit
        if (transform.position.x < -halfW) dir.x =  Mathf.Abs(dir.x);
        if (transform.position.x >  halfW) dir.x = -Mathf.Abs(dir.x);

        // Zone victoire
        if (BubbleGoal.Instance != null && BubbleGoal.Instance.Contains(transform.position))
        {
            BubbleGameManager.Instance?.TriggerVictory();
            NotifyAndDestroy();
            return;
        }

        // Plafond → atterrissage immédiat (toujours actif)
        if (transform.position.y >= topY) { Land(); return; }

        // Détection collision : inactive tant que la distance de sécurité n'est pas franchie
        float traveledSqr = (transform.position - spawnPos).sqrMagnitude;
        if (traveledSqr < minTravelSqr) return;

        // Récupère TOUS les colliders à portée et filtre uniquement les composants Bubble
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius);
        foreach (Collider2D hit in hits)
        {
            // Bulle bonus : récompense uniquement si la couleur du projectile correspond
            if (hit.TryGetComponent<BonusBubble>(out var bonus))
            {
                if (bonus.ColorType == color)
                {
                    // Bonne couleur → récompense et destruction
                    BubbleGameManager.Instance?.AwardBonusShots(bonus.BonusAmount);
                    BubbleGrid.Instance?.RemoveBonusBubble(hit.GetComponent<Bubble>());
                    NotifyAndDestroy();
                }
                else
                {
                    // Mauvaise couleur → se colle normalement comme sur une bulle ordinaire
                    Land();
                }
                return;
            }

            // Bulle normale : atterrissage (colle ou explose selon la couleur — géré par BubbleGrid)
            if (hit.TryGetComponent<Bubble>(out _))
            {
                Land();
                return;
            }
        }
    }

    // ── Atterrissage ──────────────────────────────────────────────────────────

    private void Land()
    {
        BubbleGrid.Instance?.PlaceProjectile(color, transform.position);
        NotifyAndDestroy();
    }

    private void NotifyAndDestroy()
    {
        if (!notified) { notified = true; OnLanded?.Invoke(); }
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (!notified) { notified = true; OnLanded?.Invoke(); }
    }
}
