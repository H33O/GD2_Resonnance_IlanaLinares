using UnityEngine;

/// <summary>
/// Bridge minimal pour la scène GameAndWatch.
/// Délègue entièrement la fin de partie à <see cref="GameEndScreen"/>.
///
/// Garantit que les singletons persistants (<see cref="ScoreManager"/>,
/// <see cref="QuestManager"/>) existent avant que la partie ne commence.
/// </summary>
public class GAWSessionBridge : MonoBehaviour
{
    private void Start()
    {
        ScoreManager.EnsureExists();
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

        var field = typeof(GameEndScreen)
            .GetField("gameType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(screen, GameType.GameAndWatch);
    }
}
