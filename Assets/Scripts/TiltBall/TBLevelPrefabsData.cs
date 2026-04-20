using UnityEngine;

/// <summary>
/// ScriptableObject regroupant tous les sprites utilisés par TBSceneSetup.
/// Assignez-le sur le composant TBSceneSetup dans l'éditeur.
///
/// Chaque champ est optionnel : si null, TBSceneSetup utilise un sprite
/// généré procéduralement (cercle ou carré blanc).
/// Les prefabs Hole et Obstacle permettent de personnaliser la structure complète
/// (sprite, collider, composants) sans toucher au code.
/// </summary>
[CreateAssetMenu(fileName = "TBLevelPrefabsData", menuName = "TiltBall/Level Prefabs Data")]
public class TBLevelPrefabsData : ScriptableObject
{
    [Header("Joueur")]
    [Tooltip("Prefab du joueur. Si assigné, il est instancié à la place du joueur procédural. " +
             "Le prefab doit contenir un SpriteRenderer, un CircleCollider2D, un Rigidbody2D et un TBPlayerController.")]
    public GameObject playerPrefab;

    [Tooltip("Sprite du joueur. Utilisé uniquement si playerPrefab est null.")]
    public Sprite playerSprite;

    [Tooltip("Couleur teintée sur le sprite du joueur.")]
    public Color playerColor = Color.white;

    [Header("Ennemi")]
    [Tooltip("Sprite des ennemis. Null → cercle rouge généré.")]
    public Sprite enemySprite;

    [Tooltip("Couleur teintée sur le sprite ennemi.")]
    public Color enemyColor = new Color(0.90f, 0.14f, 0.14f, 1f);

    [Header("Goal (trou)")]
    [Tooltip("Prefab du trou/goal. Si assigné, il est instancié en remplacement du goal procédural. " +
             "Le prefab doit contenir un SpriteRenderer, un CircleCollider2D (isTrigger) et un TBHole.")]
    public GameObject holePrefab;

    [Tooltip("Sprite du trou/goal ouvert. Utilisé uniquement si holePrefab est null.")]
    public Sprite holeSprite;

    [Tooltip("Couleur du goal quand ouvert.")]
    public Color holeOpenColor = new Color(0.04f, 0.04f, 0.06f, 1f);

    [Tooltip("Couleur du goal quand verrouillé (clé requise).")]
    public Color holeLockedColor = new Color(0.45f, 0.22f, 0.02f, 1f);

    [Header("Clé")]
    [Tooltip("Sprite de la clé à ramasser. Null → losange jaune généré.")]
    public Sprite keySprite;

    [Tooltip("Couleur teintée sur le sprite de la clé.")]
    public Color keyColor = new Color(1f, 0.85f, 0f, 1f);

    [Header("Obstacles & Murs")]
    [Tooltip("Prefab des obstacles intérieurs. Si assigné, il est instancié pour chaque obstacle. " +
             "Le prefab doit contenir un SpriteRenderer et un BoxCollider2D.")]
    public GameObject obstaclePrefab;

    [Tooltip("Sprite des obstacles intérieurs. Utilisé uniquement si obstaclePrefab est null.")]
    public Sprite obstacleSprite;

    [Tooltip("Couleur des obstacles.")]
    public Color obstacleColor = new Color(0.20f, 0.20f, 0.30f, 1f);

    [Tooltip("Couleur des murs de bordure.")]
    public Color wallColor = new Color(0.88f, 0.88f, 0.92f, 1f);

    [Header("Fond")]
    [Tooltip("Couleur de fond de la caméra.")]
    public Color backgroundColor = new Color(0.06f, 0.06f, 0.10f, 1f);
}
