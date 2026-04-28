using System.Collections;
using UnityEngine;

/// <summary>
/// Explosion radiale blanche et rouge déclenchée lors du contact ennemi-joueur.
/// Crée plusieurs rings et éclats qui s'agrandissent et s'estompent.
/// Méthode statique <see cref="Spawn"/> pour un appel one-shot.
/// </summary>
public class TBExplosionFX : MonoBehaviour
{
    // ── Paramètres ────────────────────────────────────────────────────────────

    private const int   RingCount       = 3;
    private const int   ShardCount      = 10;
    private const float RingDuration    = 0.55f;
    private const float ShardDuration   = 0.45f;
    private const float ShardSpeedMin   = 2.5f;
    private const float ShardSpeedMax   = 6.0f;
    private const float ShardSizeStart  = 0.12f;

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>Instancie une explosion à la position donnée et se détruit automatiquement.</summary>
    public static void Spawn(Vector3 worldPos)
    {
        var go = new GameObject("TBExplosion");
        go.transform.position = worldPos;
        go.AddComponent<TBExplosionFX>().StartCoroutine(
            go.GetComponent<TBExplosionFX>().PlayAndDestroy());
    }

    // ── Explosion ─────────────────────────────────────────────────────────────

    private IEnumerator PlayAndDestroy()
    {
        StartCoroutine(SpawnRings());
        StartCoroutine(SpawnShards());

        float maxDuration = Mathf.Max(RingDuration, ShardDuration) + 0.1f;
        yield return new WaitForSeconds(maxDuration);
        Destroy(gameObject);
    }

    private IEnumerator SpawnRings()
    {
        for (int i = 0; i < RingCount; i++)
        {
            SpawnRing(i);
            yield return new WaitForSeconds(0.08f);
        }
    }

    private void SpawnRing(int index)
    {
        var go = new GameObject($"Ring_{index}");
        go.transform.SetParent(transform, false);

        // Alternance blanc / rouge
        Color col = index % 2 == 0
            ? new Color(1f, 1f, 1f, 0.9f)
            : new Color(1f, 0.10f, 0.10f, 0.85f);

        var sr          = go.AddComponent<SpriteRenderer>();
        sr.sprite       = SpriteGenerator.CreateRing(64, 0.18f);
        sr.color        = col;
        sr.sortingOrder = 20;

        float startScale = 0.15f + index * 0.08f;
        float endScale   = 1.8f + index * 0.35f;

        StartCoroutine(AnimateRing(go, sr, startScale, endScale, col, RingDuration));
    }

    private static IEnumerator AnimateRing(GameObject go, SpriteRenderer sr,
                                            float startScale, float endScale,
                                            Color col, float duration)
    {
        float t = 0f;
        while (t < duration && go != null)
        {
            t += Time.deltaTime;
            float ratio = t / duration;

            float size  = Mathf.Lerp(startScale, endScale, ratio);
            float alpha = Mathf.Lerp(col.a, 0f, ratio);

            go.transform.localScale = new Vector3(size, size, 1f);
            if (sr != null)
                sr.color = new Color(col.r, col.g, col.b, alpha);

            yield return null;
        }
    }

    private IEnumerator SpawnShards()
    {
        for (int i = 0; i < ShardCount; i++)
        {
            SpawnShard(i);
        }
        yield return new WaitForSeconds(ShardDuration);
    }

    private void SpawnShard(int index)
    {
        var go = new GameObject($"Shard_{index}");
        go.transform.SetParent(transform, false);
        go.transform.localScale = new Vector3(ShardSizeStart, ShardSizeStart, 1f);

        Color col = index % 3 == 0
            ? new Color(1f, 0.10f, 0.10f, 1f)
            : new Color(1f, 1f, 1f, 0.85f);

        var sr          = go.AddComponent<SpriteRenderer>();
        sr.sprite       = SpriteGenerator.CreateCircle(32);
        sr.color        = col;
        sr.sortingOrder = 21;

        float angle = index * (360f / ShardCount) + Random.Range(-15f, 15f);
        float rad   = angle * Mathf.Deg2Rad;
        float speed = Random.Range(ShardSpeedMin, ShardSpeedMax);
        var   dir   = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

        StartCoroutine(AnimateShard(go, sr, dir, speed, col, ShardDuration));
    }

    private static IEnumerator AnimateShard(GameObject go, SpriteRenderer sr,
                                             Vector2 dir, float speed,
                                             Color col, float duration)
    {
        float t = 0f;
        while (t < duration && go != null)
        {
            t += Time.deltaTime;
            float ratio = t / duration;

            go.transform.localPosition += (Vector3)(dir * speed * Time.deltaTime);
            float scale = Mathf.Lerp(ShardSizeStart, 0f, ratio);
            go.transform.localScale = new Vector3(scale, scale, 1f);
            if (sr != null)
                sr.color = new Color(col.r, col.g, col.b, Mathf.Lerp(col.a, 0f, ratio));

            yield return null;
        }
    }
}
