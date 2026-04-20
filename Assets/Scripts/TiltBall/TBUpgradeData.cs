using UnityEngine;

/// <summary>
/// Données persistantes des améliorations achetées par le joueur durant la session TiltBall.
/// Portées par TBGameManager et appliquées à chaque niveau suivant l'achat.
///
/// Coûts élevés pour rendre les achats rares et significatifs :
///   Allié    : 200 pts par unité (max 3)
///   Arme     : 350 pts (unique)
///   Barrière : 150 pts par unité (max 4)
///
/// Déblocage progressif par niveau :
///   Allié    → disponible dès le niveau 2
///   Barrière → disponible dès le niveau 4
///   Arme     → disponible dès le niveau 6
/// </summary>
public class TBUpgradeData
{
    // ── Compteurs ─────────────────────────────────────────────────────────────

    /// <summary>Nombre d'alliés achetés (max 3).</summary>
    public int AllyCount    { get; private set; }

    /// <summary>Vrai si l'arme a été achetée.</summary>
    public bool HasWeapon   { get; private set; }

    /// <summary>Nombre de barrières achetées (max 4).</summary>
    public int BarrierCount { get; private set; }

    // ── Coûts ─────────────────────────────────────────────────────────────────

    public const int CostAlly    = 200;
    public const int CostWeapon  = 350;
    public const int CostBarrier = 150;

    // ── Niveaux de déblocage ──────────────────────────────────────────────────

    public const int UnlockLevelAlly    = 2;
    public const int UnlockLevelBarrier = 4;
    public const int UnlockLevelWeapon  = 6;

    // ── Limites ───────────────────────────────────────────────────────────────

    public const int MaxAllies   = 3;
    public const int MaxBarriers = 4;

    // ── Achat ─────────────────────────────────────────────────────────────────

    /// <summary>Tente d'acheter un allié. Retourne vrai si l'achat réussit.</summary>
    public bool BuyAlly(ref int score)
    {
        if (AllyCount >= MaxAllies) return false;
        if (score < CostAlly)       return false;
        score     -= CostAlly;
        AllyCount++;
        return true;
    }

    /// <summary>Tente d'acheter l'arme. Retourne vrai si l'achat réussit.</summary>
    public bool BuyWeapon(ref int score)
    {
        if (HasWeapon)          return false;
        if (score < CostWeapon) return false;
        score    -= CostWeapon;
        HasWeapon = true;
        return true;
    }

    /// <summary>Tente d'acheter une barrière. Retourne vrai si l'achat réussit.</summary>
    public bool BuyBarrier(ref int score)
    {
        if (BarrierCount >= MaxBarriers) return false;
        if (score < CostBarrier)         return false;
        score       -= CostBarrier;
        BarrierCount++;
        return true;
    }

    /// <summary>
    /// Vérifie si une amélioration est débloquée pour le niveau courant.
    /// </summary>
    public static bool IsAllyUnlocked(int levelIndex)    => levelIndex >= UnlockLevelAlly;
    public static bool IsBarrierUnlocked(int levelIndex) => levelIndex >= UnlockLevelBarrier;
    public static bool IsWeaponUnlocked(int levelIndex)  => levelIndex >= UnlockLevelWeapon;

    /// <summary>Remet toutes les améliorations à zéro (nouvelle partie).</summary>
    public void Reset()
    {
        AllyCount    = 0;
        HasWeapon    = false;
        BarrierCount = 0;
    }
}
