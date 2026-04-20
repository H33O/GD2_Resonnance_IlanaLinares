using UnityEngine;

/// <summary>
/// ScriptableObject de configuration d'un niveau TiltBall.
/// Créer via Assets > Create > TiltBall > Level Settings.
/// </summary>
[CreateAssetMenu(fileName = "TBLevelSettings", menuName = "TiltBall/Level Settings")]
public class TBSettings : ScriptableObject
{
    [Header("Grille")]
    [Tooltip("Distance d'un déplacement (une case).")]
    public float stepSize = 1.0f;

    [Tooltip("Intervalle en secondes entre deux déplacements (joueur et ennemis).")]
    public float moveInterval = 0.35f;

    [Tooltip("Rayon de détection de collision pour le déplacement.")]
    public float checkRadius = 0.45f;

    [Header("Limites écran")]
    [Tooltip("Limite horizontale absolue du centre du joueur / ennemi.")]
    public float maxX = 4.5f;

    [Tooltip("Limite verticale absolue du centre du joueur / ennemi.")]
    public float maxY = 8.7f;

    [Header("Ennemis")]
    [Tooltip("Portée de détection du joueur par l'ennemi.")]
    public float enemyDetectionRange = 10f;

    [Header("Input accéléromètre")]
    [Tooltip("Seuil minimal d'inclinaison pour déclencher un déplacement.")]
    public float inputThreshold = 0.28f;

    [Header("Niveau")]
    [Tooltip("Niveau 2 : la clé doit être ramassée avant d'entrer dans le trou.")]
    public bool requireKey = false;

    [Tooltip("Points accordés par seconde restante à la fin du niveau.")]
    public int pointsPerSecond = 10;

    [Tooltip("Durée maximale du niveau en secondes (0 = illimité).")]
    public float levelDuration = 0f;
}
