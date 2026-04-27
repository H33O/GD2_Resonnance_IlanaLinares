using System.Collections;
using UnityEngine;

/// <summary>
/// Trou dans lequel le joueur doit entrer pour progresser.
///
/// Lit automatiquement TBGameManager.Instance.RequireKey pour savoir
/// s'il faut attendre la clé — aucun champ à configurer dans l'Inspector.
/// </summary>
public class TBHole : MonoBehaviour
{
    // ── Couleurs fallback (utilisées uniquement si le sprite est le cercle procédural) ───

    private static readonly Color ColOpen   = new Color(0.20f, 0.80f, 0.20f, 1f);   // vert visible
    private static readonly Color ColLocked = new Color(0.45f, 0.22f, 0.02f, 1f);   // orange-brun
    private static readonly Color ColFlash  = new Color(1f, 0.50f, 0f, 1f);

    // ── État ──────────────────────────────────────────────────────────────────

    private SpriteRenderer sr;
    private bool           triggered;

    /// <summary>True si le sprite est un asset réel (pas le cercle procédural blanc).</summary>
    private bool hasCustomSprite;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();

        // Considère qu'un sprite est "custom" si sa couleur initiale est blanche
        // (BuildHole pose Color.white quand goalSprite != null)
        hasCustomSprite = sr != null && sr.color == Color.white;

        RefreshColor();

        if (TBGameManager.Instance != null)
            TBGameManager.Instance.OnKeyCollected.AddListener(OnKeyCollected);
    }

    private void OnDestroy()
    {
        if (TBGameManager.Instance != null)
            TBGameManager.Instance.OnKeyCollected.RemoveListener(OnKeyCollected);
    }

    // ── Trigger ───────────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;

        var p = other.GetComponent<TBPlayerController>();
        if (p == null || !p.IsAlive) return;

        bool locked = TBGameManager.Instance != null
            && TBGameManager.Instance.RequireKey
            && !TBGameManager.Instance.HasKey;

        if (locked)
        {
            StartCoroutine(FlashLocked());
            return;
        }

        triggered = true;
        p.EnterHole(transform.position);
    }

    // ── Visuels ───────────────────────────────────────────────────────────────

    private void OnKeyCollected() => RefreshColor();

    private void RefreshColor()
    {
        if (sr == null) return;

        // Si le sprite est un asset réel, on ne touche pas à la couleur blanche
        // (le tinter ferait disparaître le sprite graphiquement)
        if (hasCustomSprite) return;

        bool locked = TBGameManager.Instance != null
            && TBGameManager.Instance.RequireKey
            && !TBGameManager.Instance.HasKey;
        sr.color = locked ? ColLocked : ColOpen;
    }

    private IEnumerator FlashLocked()
    {
        Color original = sr != null ? sr.color : ColLocked;
        for (int i = 0; i < 4; i++)
        {
            if (sr) sr.color = ColFlash;
            yield return new WaitForSeconds(0.08f);
            if (sr) sr.color = original;
            yield return new WaitForSeconds(0.08f);
        }
    }
}
