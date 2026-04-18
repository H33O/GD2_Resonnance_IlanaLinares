using UnityEngine;

/// <summary>
/// Tous les paramètres de design du jeu Tetris×Pac-Man.
/// Créer via  Assets ▸ Create ▸ TetrisPacMan ▸ Settings.
/// </summary>
[CreateAssetMenu(fileName = "TPMSettings", menuName = "TetrisPacMan/Settings")]
public class TPMSettings : ScriptableObject
{
    // ── Grille ────────────────────────────────────────────────────────────────

    [Header("Grid")]
    [Tooltip("Nombre de colonnes de la grille.")]
    public int gridWidth  = 9;

    [Tooltip("Nombre de lignes de la grille.")]
    public int gridHeight = 15;

    [Tooltip("Taille d'une cellule en unités monde. Calibré pour 1080×1920 portrait.")]
    public float cellSize = 1.18f;

    // ── Joueur ────────────────────────────────────────────────────────────────

    [Header("Player")]
    [Tooltip("Vitesse de déplacement du joueur (cellules/seconde).")]
    public float playerSpeed = 6f;

    [Tooltip("Nombre initial de coups disponibles.")]
    public int startingMoves = 20;

    // ── Blocs ─────────────────────────────────────────────────────────────────

    [Header("Blocks")]
    [Tooltip("Coups redonnés lors de la destruction d'un bloc seul.")]
    public int movesRestoredSingle = 1;

    [Tooltip("Coups bonus pour chaque bloc supplémentaire détruit en chaîne.")]
    public int movesRestoredChainBonus = 1;

    [Tooltip("Points de score pour un bloc seul détruit.")]
    public int pointsSingle = 10;

    [Tooltip("Multiplicateur de score par bloc supplémentaire dans la chaîne.")]
    public float chainScoreMultiplier = 1.5f;

    [Tooltip("Durée de l'animation de pose/destruction (secondes).")]
    public float blockAnimDuration = 0.18f;

    // ── Monstre ───────────────────────────────────────────────────────────────

    [Header("Monster")]
    [Tooltip("Délai entre chaque pas du monstre (secondes). Plus élevé = plus lent.")]
    public float monsterStepDelay = 0.7f;

    [Tooltip("Délai initial avant que le monstre commence à bouger.")]
    public float monsterStartDelay = 2.0f;

    // ── Sortie ────────────────────────────────────────────────────────────────

    [Header("Exit")]
    [Tooltip("Pulsation de l'indicateur de sortie.")]
    public float exitPulseSpeed = 2.5f;
}
