using UnityEngine;

/// <summary>
/// Construit le visuel procédural d'un ennemi du Parry Game :
/// corps ovale rouge vif + deux petits yeux noirs.
/// S'attache sur le GameObject racine de l'ennemi.
/// </summary>
public static class PGEnemyVisuals
{
    private static readonly Color ColBody = new Color(0.92f, 0.10f, 0.10f, 1f);
    private static readonly Color ColEye  = new Color(0.05f, 0.02f, 0.02f, 0.95f);

    /// <summary>
    /// Crée le corps rouge + les yeux sur <paramref name="root"/>.
    /// Retourne le SpriteRenderer du corps (utilisé par PGEnemy pour la couleur alpha).
    /// </summary>
    public static SpriteRenderer Build(Transform root)
    {
        // ── Corps ovale ───────────────────────────────────────────────────────
        var bodyGO = new GameObject("EnemyBody");
        bodyGO.transform.SetParent(root, false);
        bodyGO.transform.localPosition = Vector3.zero;
        bodyGO.transform.localScale    = new Vector3(1.0f, 0.75f, 1f); // ovale

        var bodySR          = bodyGO.AddComponent<SpriteRenderer>();
        bodySR.sprite       = SpriteGenerator.CreateCircle(128);
        bodySR.color        = ColBody;
        bodySR.sortingOrder = 4;

        // ── Œil gauche ────────────────────────────────────────────────────────
        BuildEye(root,  0.22f, 0.18f);
        // ── Œil droit ─────────────────────────────────────────────────────────
        BuildEye(root, -0.22f, 0.18f);

        return bodySR;
    }

    private static void BuildEye(Transform parent, float localX, float localY)
    {
        var eye = new GameObject("Eye");
        eye.transform.SetParent(parent, false);
        eye.transform.localPosition = new Vector3(localX, localY, 0f);
        eye.transform.localScale    = new Vector3(0.14f, 0.16f, 1f);

        var sr          = eye.AddComponent<SpriteRenderer>();
        sr.sprite       = SpriteGenerator.CreateCircle(32);
        sr.color        = ColEye;
        sr.sortingOrder = 5;
    }
}
