using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Composant optionnel conservé pour compatibilité.
/// Toute la génération de niveau est désormais gérée par TBSceneSetup.
/// </summary>
public class TBLevelBootstrap : MonoBehaviour
{
    /// <summary>
    /// Toujours null dans le nouveau système procédural.
    /// Conservé pour que TBPlayerController.ReadDirection() compile sans erreur.
    /// </summary>
    public static TBSettings Settings => null;
}
