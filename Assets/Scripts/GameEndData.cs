/// <summary>
/// Conteneur statique transférant les données de fin de partie vers la scène Menu.
/// Les données sont écrites par le bridge qui déclenche le retour au menu,
/// puis lues par <see cref="MenuCoinReceiver"/> au démarrage de la scène Menu.
/// </summary>
public static class GameEndData
{
    /// <summary>Score final affiché dans la card de fin de partie.</summary>
    public static int FinalScore   { get; private set; }

    /// <summary>Pièces gagnées pendant la partie (converties depuis le score).</summary>
    public static int CoinsEarned  { get; private set; }

    /// <summary>Vrai si des données en attente doivent être traitées par le Menu.</summary>
    public static bool HasPending  { get; private set; }

    /// <summary>Enregistre les données de fin de partie avant la transition vers le Menu.</summary>
    public static void Set(int finalScore, int coinsEarned)
    {
        FinalScore  = finalScore;
        CoinsEarned = coinsEarned;
        HasPending  = true;
    }

    /// <summary>Consomme les données en attente (à appeler depuis <see cref="MenuCoinReceiver"/>).</summary>
    public static void Consume()
    {
        FinalScore  = 0;
        CoinsEarned = 0;
        HasPending  = false;
    }
}
