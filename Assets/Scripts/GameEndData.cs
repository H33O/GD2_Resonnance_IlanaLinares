/// <summary>
/// Conteneur statique transférant les données de fin de partie vers la scène Menu.
/// Les données sont écrites par <see cref="GameEndScreen"/> avant la transition,
/// puis lues par <see cref="MenuCoinReceiver"/> au démarrage de la scène Menu.
///
/// Inclut désormais le <see cref="GameType"/> pour permettre à
/// <see cref="QuestManager"/> de valider correctement la session côté Menu.
/// </summary>
public static class GameEndData
{
    /// <summary>Score final de la partie.</summary>
    public static int      FinalScore  { get; private set; }

    /// <summary>Pièces gagnées pendant la partie.</summary>
    public static int      CoinsEarned { get; private set; }

    /// <summary>Type de mini-jeu qui vient de se terminer.</summary>
    public static GameType GameType    { get; private set; }

    /// <summary>Vrai si des données en attente doivent être traitées par le Menu.</summary>
    public static bool     HasPending  { get; private set; }

    /// <summary>Enregistre les données de fin de partie avant la transition vers le Menu.</summary>
    public static void Set(int finalScore, int coinsEarned, GameType gameType = global::GameType.GameAndWatch)
    {
        FinalScore  = finalScore;
        CoinsEarned = coinsEarned;
        GameType    = gameType;
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
