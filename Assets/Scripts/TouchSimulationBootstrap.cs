using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;

/// <summary>
/// Active automatiquement la simulation touch par la souris au démarrage.
/// Permet de tester tous les inputs tactiles dans l'éditeur sans appareil physique.
/// Clic gauche = doigt posé / levé, déplacement souris bouton enfoncé = glissement.
/// </summary>
public static class TouchSimulationBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Enable()
    {
        TouchSimulation.Enable();
    }
}
