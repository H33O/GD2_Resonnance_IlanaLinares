using UnityEngine;

/// <summary>
/// ScriptableObject contenant tous les paramètres éditables du mini-jeu arène.
/// Modifiable depuis l'Inspector sans toucher au code.
/// Chemin : Assets/ScriptableObjects/CircleArenaConfig.asset
/// </summary>
[CreateAssetMenu(fileName = "CircleArenaConfig", menuName = "CircleArena/Config")]
public class CircleArenaConfig : ScriptableObject
{
    [Header("Arène")]
    [Tooltip("Rayon de l'arène en pixels canvas (résolution de référence 1080x1920)")]
    public float arenaRadius          = 420f;
    [Tooltip("Épaisseur du contour du cercle en pixels")]
    public float arenaStrokeWidth     = 22f;
    [Tooltip("Couleur du fond")]
    public Color backgroundColor      = Color.black;
    [Tooltip("Couleur de l'anneau et des obstacles")]
    public Color arenaColor           = Color.white;

    [Header("Balle")]
    [Tooltip("Rayon de la balle en pixels")]
    public float ballRadius           = 18f;
    [Tooltip("Longueur du trail (nombre de points)")]
    [Range(4, 24)]
    public int trailLength            = 12;
    [Tooltip("Alpha maximal du trail (premier point)")]
    [Range(0f, 1f)]
    public float trailMaxAlpha        = 0.40f;

    [Header("Gameplay — Vitesse")]
    [Tooltip("Vitesse de rotation de la balle au départ (degrés/seconde)")]
    public float ballSpeedStart       = 100f;
    [Tooltip("Vitesse maximale en fin de partie")]
    public float ballSpeedMax         = 290f;
    [Tooltip("Durée (secondes) pour atteindre la vitesse maximale")]
    public float speedRampDuration    = 70f;

    [Header("Gameplay — Saut")]
    [Tooltip("Distance de saut : fraction du rayon vers le centre (0=pas de saut, 1=centre)")]
    [Range(0.1f, 0.9f)]
    public float jumpDepthRatio       = 0.50f;
    [Tooltip("Durée de l'aller (balle vers centre) en secondes")]
    public float jumpDurationIn       = 0.22f;
    [Tooltip("Durée du retour (centre vers bord) en secondes")]
    public float jumpDurationOut      = 0.30f;

    [Header("Obstacles")]
    [Tooltip("Délai initial entre chaque obstacle (secondes)")]
    public float obstacleSpawnDelay   = 2.0f;
    [Tooltip("Délai minimum entre obstacles en fin de partie")]
    public float obstacleMinDelay     = 0.55f;
    [Tooltip("Largeur de l'obstacle (pixels canvas)")]
    public float obstacleWidth        = 16f;
    [Tooltip("Profondeur de l'obstacle vers le centre (pixels canvas)")]
    public float obstacleDepth        = 55f;
    [Tooltip("Fenêtre angulaire du 'perfect dodge' (degrés)")]
    public float dodgeWindowDeg       = 14f;

    [Header("Feedbacks")]
    [Tooltip("Durée du slow-motion avant le game over (secondes réelles)")]
    public float slowMoDuration       = 0.30f;
    [Tooltip("Facteur de temps pendant le slow-mo (0.1 = 10% de la vitesse normale)")]
    [Range(0.05f, 0.5f)]
    public float slowMoScale          = 0.12f;
    [Tooltip("Magnitude du screen shake au game over")]
    public float shakeMagnitude       = 18f;
    [Tooltip("Durée du screen shake")]
    public float shakeDuration        = 0.35f;
}
