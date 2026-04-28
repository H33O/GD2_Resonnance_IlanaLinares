using System.Collections;
using UnityEngine;

/// <summary>
/// Effet d'explosion déclenché quand un slash touche un ennemi dans le Parry Game.
/// Deux couches :
///   1. Burst de particules rouges/orange éjectées radialement.
///   2. Ring blanc qui s'étend et s'estompe.
///
/// Appel : <c>PGExplosionFX.Spawn(worldPosition);</c>
/// </summary>
public class PGExplosionFX : MonoBehaviour
{
    // ── Constantes ────────────────────────────────────────────────────────────

    private const int   ParticleCount   = 10;
    private const float ParticleDur     = 0.40f;
    private const float ParticleSpeed   = 3.5f;
    private const float RingDur         = 0.35f;

    private static readonly Color ColParticle = new Color(1f, 0.20f, 0.05f, 1f);
    private static readonly Color ColRing     = new Color(1f, 0.55f, 0.10f, 1f);

    // ── Entrée publique ───────────────────────────────────────────────────────

    /// <summary>Spawne l'explosion en world-space à <paramref name="pos"/>.</summary>
    public static void Spawn(Vector3 pos)
    {
        var go = new GameObject("PGExplosion");
        go.transform.position = pos;
        go.AddComponent<PGExplosionFX>().Play();
    }

    // ── Animation ─────────────────────────────────────────────────────────────

    private void Play()
    {
        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        // Spawn toutes les particules et le ring simultanément
        for (int i = 0; i < ParticleCount; i++)
            StartCoroutine(ParticleRoutine(i));

        StartCoroutine(RingRoutine());

        // Attend la fin de tous les effets puis se détruit
        yield return new WaitForSeconds(Mathf.Max(ParticleDur, RingDur) + 0.05f);
        Destroy(gameObject);
    }

    private IEnumerator ParticleRoutine(int index)
    {
        // Direction radiale aléatoire
        float angle = (360f / ParticleCount) * index + Random.Range(-18f, 18f);
        float rad   = angle * Mathf.Deg2Rad;
        var   dir   = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);

        float speed = ParticleSpeed * Random.Range(0.6f, 1.4f);
        float size  = Random.Range(0.04f, 0.10f);

        // Crée le sprite de particule
        var go = new GameObject("P");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale    = Vector3.one * size;

        var sr          = go.AddComponent<SpriteRenderer>();
        sr.sprite       = SpriteGenerator.CreateCircle(16);
        sr.color        = ColParticle;
        sr.sortingOrder = 20;

        float t = 0f;
        while (t < ParticleDur && go != null)
        {
            t += Time.deltaTime;
            float r = t / ParticleDur;
            go.transform.localPosition = dir * (speed * t);
            // Ralentissement + fade
            float alpha = Mathf.Lerp(1f, 0f, Mathf.Pow(r, 0.5f));
            sr.color    = new Color(ColParticle.r, ColParticle.g, ColParticle.b, alpha);
            yield return null;
        }

        if (go != null) Destroy(go);
    }

    private IEnumerator RingRoutine()
    {
        var go = new GameObject("Ring");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale    = Vector3.one * 0.1f;

        var sr          = go.AddComponent<SpriteRenderer>();
        sr.sprite       = SpriteGenerator.CreateRing(64, 0.18f);
        sr.color        = ColRing;
        sr.sortingOrder = 19;

        float t = 0f;
        while (t < RingDur && go != null)
        {
            t += Time.deltaTime;
            float r     = t / RingDur;
            float scale = Mathf.Lerp(0.1f, 1.4f, r);
            float alpha = Mathf.Lerp(1f, 0f, r);
            go.transform.localScale = Vector3.one * scale;
            sr.color = new Color(ColRing.r, ColRing.g, ColRing.b, alpha);
            yield return null;
        }

        if (go != null) Destroy(go);
    }
}
