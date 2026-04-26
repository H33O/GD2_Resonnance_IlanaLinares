using UnityEngine;

/// <summary>
/// Bridge minimal pour la scène GameAndWatch.
/// Délègue entièrement la fin de partie à <see cref="GameEndScreen"/>.
///
/// Ce composant garantit que OWGameManager existe et que GameEndScreen
/// est présent dans la scène. Si GameEndScreen est déjà attaché à un
/// autre GameObject dans la scène, ce composant est superflu.
/// </summary>
public class GAWSessionBridge : MonoBehaviour
{
    private void Start()
    {
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

        // Forcer le type GameAndWatch via réflexion pour éviter de rendre le champ public
        var field = typeof(GameEndScreen)
            .GetField("gameType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(screen, GameType.GameAndWatch);
    }
}
