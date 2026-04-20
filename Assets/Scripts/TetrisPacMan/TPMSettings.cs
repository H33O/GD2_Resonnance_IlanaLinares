using UnityEngine;

/// <summary>
/// Tous les paramètres de design du jeu Tetris×Pac-Man.
/// Créer via Assets ▸ Create ▸ TetrisPacMan ▸ Settings.
/// </summary>
[CreateAssetMenu(fileName = "TPMSettings", menuName = "TetrisPacMan/Settings")]
public class TPMSettings : ScriptableObject
{
    // ── Grille ────────────────────────────────────────────────────────────────

    [Header("Grid")]
    [Tooltip("Nombre de colonnes de la grille.")]
    public int gridWidth = 12;

    [Tooltip("Nombre de lignes de la grille.")]
    public int gridHeight = 18;

    [Tooltip("Taille d'une cellule en unités monde.")]
    public float cellSize = 0.95f;

    // ── Joueur ────────────────────────────────────────────────────────────────

    [Header("Player")]
    [Tooltip("Vitesse de glissement entre deux cellules (unités/s).")]
    public float playerSpeed = 8f;

    [Tooltip("Nombre de coups de bloc disponibles au départ.")]
    public int startingMoves = 30;

    // ── Blocs posés ───────────────────────────────────────────────────────────

    [Header("Blocks")]
    [Tooltip("Durée de l'animation de pose/destruction (secondes).")]
    public float blockAnimDuration = 0.14f;

    [Tooltip("Points de score par bloc détruit.")]
    public int pointsPerBlock = 10;

    [Tooltip("Coups récupérés lors de la destruction d'un bloc.")]
    public int movesRestoredOnDestroy = 1;

    // ── Tetris ────────────────────────────────────────────────────────────────

    [Header("Tetris")]
    [Tooltip("Intervalle de chute automatique (secondes).")]
    public float tetrisFallInterval = 1.4f;

    [Tooltip("Intervalle soft drop (flèche bas maintenu).")]
    public float tetrisSoftDropInterval = 0.07f;

    [Tooltip("Coups bonus gagnés par ligne effacée.")]
    public int movesPerLineClear = 2;

    [Tooltip("Points par ligne effacée.")]
    public int scorePerLineClear = 100;

    // ── Ennemi ────────────────────────────────────────────────────────────────

    [Header("Enemy")]
    [Tooltip("Délai entre chaque pas de l'ennemi (secondes).")]
    public float monsterStepDelay = 0.85f;

    [Tooltip("Délai après sa libération avant le premier pas.")]
    public float monsterReleaseDelay = 1.5f;

    // ── Rétrocompatibilité (alias utilisés par l'ancien code) ─────────────────

    [HideInInspector] public float chainScoreMultiplier = 1.5f;
    [HideInInspector] public int   movesGoal            = 999;
    [HideInInspector] public int   movesRestoredSingle       => movesRestoredOnDestroy;
    [HideInInspector] public int   movesRestoredChainBonus   = 1;
    [HideInInspector] public int   pointsSingle               => pointsPerBlock;
    [HideInInspector] public float monsterStartDelay          => monsterReleaseDelay;
}
