using UnityEngine;

/// <summary>
/// Données persistantes des améliorations achetées par le joueur durant la session TiltBall.
/// Portées par TBGameManager et appliquées à chaque niveau suivant l'achat.
/// </summary>
public class TBUpgradeData
{
    // ── Compteurs d'améliorations ─────────────────────────────────────────────

    /// <summary>Nombre d'alliés achetés (max 3).</summary>
    public int AllyCount    { get; private set; }

    /// <summary>Vrai si l'arme a été achetée.</summary>
    public bool HasWeapon   { get; private set; }

    /// <summary>Nombre de barrières achetées (max 4).</summary>
    public int BarrierCount { get; private set; }

    // ── Coût en score ─────────────────────────────────────────────────────────

    public const int CostAlly    = 80;
    public const int CostWeapon  = 120;
    public const int CostBarrier = 60;

    // ── Limites ───────────────────────────────────────────────────────────────

    public const int MaxAllies   = 3;
    public const int MaxBarriers = 4;

    // ── Achat ─────────────────────────────────────────────────────────────────

    /// <summary>Tente d'acheter un allié. Retourne vrai si l'achat réussit.</summary>
    public bool BuyAlly(ref int score)
    {
        if (AllyCount >= MaxAllies) return false;
        if (score < CostAlly)       return false;
        score      -= CostAlly;
        AllyCount++;
        return true;
    }

    /// <summary>Tente d'acheter l'arme. Retourne vrai si l'achat réussit.</summary>
    public bool BuyWeapon(ref int score)
    {
        if (HasWeapon)          return false;
        if (score < CostWeapon) return false;
        score     -= CostWeapon;
        HasWeapon  = true;
        return true;
    }

    /// <summary>Tente d'acheter une barrière. Retourne vrai si l'achat réussit.</summary>
    public bool BuyBarrier(ref int score)
    {
        if (BarrierCount >= MaxBarriers) return false;
        if (score < CostBarrier)         return false;
        score        -= CostBarrier;
        BarrierCount++;
        return true;
    }

    /// <summary>Remet toutes les améliorations à zéro (nouvelle partie).</summary>
    public void Reset()
    {
        AllyCount    = 0;
        HasWeapon    = false;
        BarrierCount = 0;
    }
}
