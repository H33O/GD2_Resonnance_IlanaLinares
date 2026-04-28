/// <summary>
/// Conteneur statique transférant les données de fin de partie vers la scène Menu.
/// Écrit par <see cref="GameEndScreen"/> / managers de jeu avant la transition de scène.
/// Consommé par <see cref="MenuXPReceiver"/> au démarrage du Menu.
/// </summary>
public static class GameEndData
{
    /// <summary>Score final de la partie.</summary>
    public static int      FinalScore { get; private set; }

    /// <summary>XP gagnée pendant la partie (calculée depuis le score).</summary>
    public static int      XPEarned   { get; private set; }

    /// <summary>Type de mini-jeu terminé.</summary>
    public static GameType GameType   { get; private set; }

    /// <summary>Vrai si des données en attente doivent être traitées par le Menu.</summary>
    public static bool     HasPending { get; private set; }

    /// <summary>
    /// Enregistre les données de fin de partie.
    /// L'XP est calculée automatiquement : 1 XP par tranche de 5 points, minimum 5 XP.
    /// </summary>
    public static void Set(int finalScore, GameType gameType = global::GameType.GameAndWatch)
    {
        FinalScore = finalScore;
        GameType   = gameType;
        XPEarned   = ComputeXP(finalScore);
        HasPending = true;
    }

    /// <summary>Permet de spécifier explicitement l'XP (ex. victoire avec bonus).</summary>
    public static void SetWithXP(int finalScore, int xp, GameType gameType = global::GameType.GameAndWatch)
    {
        FinalScore = finalScore;
        GameType   = gameType;
        XPEarned   = xp;
        HasPending = true;
    }

    /// <summary>Consomme les données en attente (appelé par <see cref="MenuXPReceiver"/>).</summary>
    public static void Consume()
    {
        FinalScore = 0;
        XPEarned   = 0;
        HasPending = false;
    }

    /// <summary>
    /// Formule de conversion : 1 XP par tranche de 5 points, minimum 5 XP garanti.
    /// </summary>
    public static int ComputeXP(int score)
        => score <= 0 ? 5 : System.Math.Max(5, score / 5);
}
