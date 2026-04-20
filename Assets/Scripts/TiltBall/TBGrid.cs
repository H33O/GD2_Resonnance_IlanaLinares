/// <summary>
/// Valeurs de grille actives pour le niveau courant.
/// Initialisées par TBLevelBootstrap depuis TBSettings.
/// Fallback sur des valeurs raisonnables si non initialisé.
///
/// Monde 9:16 portrait, caméra orthographique size 9.6 :
///   Largeur  = 10.8 u  → x ∈ [-5.4, 5.4]
///   Hauteur  = 19.2 u  → y ∈ [-9.6,  9.6]
/// Murs de 0.35 u → zone jouable effective : x ∈ [-5.05, 5.05], y ∈ [-9.25, 9.25]
/// </summary>
public static class TBGrid
{
    /// <summary>Distance d'un déplacement (une case).</summary>
    public static float StepSize    = 1.0f;

    /// <summary>
    /// Rayon utilisé pour détecter les collisions lors d'un déplacement.
    /// Doit être inférieur à StepSize / 2 et supérieur au demi-épaisseur des murs.
    /// </summary>
    public static float CheckRadius = 0.45f;

    /// <summary>Intervalle en secondes entre deux déplacements.</summary>
    public static float MoveInterval = 0.35f;

    /// <summary>Limite horizontale absolue du centre du joueur / ennemi.</summary>
    public static float MaxX = 4.5f;

    /// <summary>Limite verticale absolue du centre du joueur / ennemi.</summary>
    public static float MaxY = 8.7f;

    /// <summary>Initialise la grille depuis un TBSettings.</summary>
    public static void InitFromSettings(TBSettings s)
    {
        StepSize     = s.stepSize;
        CheckRadius  = s.checkRadius;
        MoveInterval = s.moveInterval;
        MaxX         = s.maxX;
        MaxY         = s.maxY;
    }
}
