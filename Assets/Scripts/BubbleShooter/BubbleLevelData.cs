using UnityEngine;

/// <summary>
/// Données d'un niveau du Bubble Shooter.
/// Crée une instance via le menu Assets → Create → Bubble Shooter → Level Data.
/// </summary>
[CreateAssetMenu(menuName = "Bubble Shooter/Level Data", fileName = "BubbleLevelData")]
public class BubbleLevelData : ScriptableObject
{
    [Header("Identification")]
    [Tooltip("Nom affiché à l'écran au début du niveau (ex : 'NIVEAU 1').")]
    public string levelName = "NIVEAU 1";

    [Header("Règles")]
    [Tooltip("Nombre de tirs disponibles pour ce niveau.")]
    public int maxShots = 30;

    [Tooltip("Nombre de lignes remplies au démarrage.")]
    [Range(1, 12)]
    public int startRows = 5;

    [Tooltip("Nombre de couleurs de bulles actives (2 à 5).")]
    [Range(2, 5)]
    public int colorCount = 4;

    [Tooltip("Nombre de tirs entre chaque descente de grille (0 = jamais).")]
    public int shotsPerDescend = 8;

    [Tooltip("Durée de l'animation de descente en secondes.")]
    public float descendDuration = 1.2f;

    [Tooltip("Probabilité (0-1) qu'une bulle soit une bulle bonus.")]
    [Range(0f, 0.5f)]
    public float bonusBubbleChance = 0.08f;

    [Header("Fond")]
    [Tooltip("Sprite de fond affiché derrière la grille. Laisse vide pour utiliser la couleur.")]
    public Sprite backgroundSprite;

    [Tooltip("Couleur unie ou teinte du sprite de fond.")]
    public Color backgroundColor = new Color(0.05f, 0.05f, 0.05f, 1f);

    [Tooltip("Mode de remplissage du sprite de fond.")]
    public BubbleSceneSetup.BackgroundFit backgroundFit = BubbleSceneSetup.BackgroundFit.Fill;

    [Header("Sprites des bulles")]
    [Tooltip("Sprites indexés par couleur : 0=Rouge, 1=Bleu, 2=Vert, 3=Jaune, 4=Violet.\n"
           + "Laisse un slot vide pour utiliser la couleur unie par défaut.")]
    public Sprite[] bubbleSprites = new Sprite[5];
}
