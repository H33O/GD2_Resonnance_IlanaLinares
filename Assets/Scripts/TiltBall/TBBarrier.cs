using UnityEngine;

/// <summary>
/// Barrière défensive statique placée dans le niveau TiltBall.
/// Bloque les ennemis (collision physique) mais laisse passer le joueur (trigger séparé).
/// Spawné par TBSceneSetup si l'amélioration "Barrière" a été achetée.
/// </summary>
public class TBBarrier : MonoBehaviour
{
    // ── Couleurs ──────────────────────────────────────────────────────────────

    private static readonly Color ColBarrier = new Color(0.10f, 0.90f, 0.50f, 0.85f);

    // ── Positions prédéfinies des barrières (une par slot) ────────────────────

    private static readonly Vector2[] BarrierPositions =
    {
        new Vector2(-3.0f,  3.5f),
        new Vector2( 3.0f, -3.5f),
        new Vector2(-3.0f, -3.5f),
        new Vector2( 3.0f,  3.5f),
    };

    // ── Spawn statique ────────────────────────────────────────────────────────

    /// <summary>
    /// Crée toutes les barrières demandées.
    /// <paramref name="count"/> : nombre de barrières à placer (1 à 4).
    /// </summary>
    public static void SpawnAll(int count)
    {
        int clamped = Mathf.Clamp(count, 0, BarrierPositions.Length);
        for (int i = 0; i < clamped; i++)
            SpawnAt(BarrierPositions[i], i);
    }

    private static void SpawnAt(Vector2 position, int index)
    {
        var go = new GameObject($"Barrier{index + 1}");
        go.tag = TBSceneSetup.LevelContentTag;

        // ── Visuel ────────────────────────────────────────────────────────────
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = SpriteGenerator.CreateWhiteSquare();
        sr.color        = ColBarrier;
        sr.sortingOrder = 2;

        go.transform.position   = new Vector3(position.x, position.y, 0f);
        go.transform.localScale = new Vector3(2.2f, 0.28f, 1f);

        // ── Collider physique (bloque les ennemis kinématiques) ───────────────
        go.AddComponent<BoxCollider2D>();

        go.AddComponent<TBBarrier>();
    }
}
