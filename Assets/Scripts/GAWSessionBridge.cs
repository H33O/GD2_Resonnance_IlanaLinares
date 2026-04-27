using UnityEngine;

/// <summary>
/// Bridge minimal pour la scène GameAndWatch.
/// Délègue entièrement la fin de partie à <see cref="GameEndScreen"/>.
///
/// Garantit que les singletons persistants (<see cref="ScoreManager"/>,
/// <see cref="QuestManager"/>, <see cref="PlayerLevelManager"/>) existent
/// avant que la partie ne commence, afin que <see cref="ScoreManager.OnScoreAdded"/>
/// soit bien reçu par <see cref="QuestManager"/> même si le jeu démarre
/// directement sur cette scène (sans passer par le menu).
/// </summary>
public class GAWSessionBridge : MonoBehaviour
{
    private void Start()
    {
        // S'assurer que les singletons sont présents avant toute partie
        ScoreManager.EnsureExists();
        PlayerLevelManager.EnsureExists();
        QuestManager.EnsureExists();

        EnsureOWGameManager();
        EnsureGameEndScreen();
    }

    private static void EnsureOWGameManager()
    {
        if (OWGameManager.Instance != null) return;
        new GameObject("OWGameManager").AddComponent<OWGameManager>();
    }

    private static void EnsureGameEndScreen()
    {
        if (FindFirstObjectByType<GameEndScreen>() != null) return;

        var go     = new GameObject("GameEndScreen");
        var screen = go.AddComponent<GameEndScreen>();

        // Forcer le type GameAndWatch via réflexion
        var field = typeof(GameEndScreen)
            .GetField("gameType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(screen, GameType.GameAndWatch);
    }
}
