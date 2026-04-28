using System.Collections;
using UnityEngine;

/// <summary>
/// Effets visuels d'amélioration du joueur TiltBall :
/// - Feedback "++" blanc qui monte et s'estompe au moment de l'achat
/// - Éclat blanc radial (flash de puissance)
/// - Matériau du joueur de plus en plus blanc/éclatant selon le niveau de puissance
///
/// Utilise uniquement des SpriteRenderers world-space.
/// </summary>
public class TBUpgradeFX : MonoBehaviour
{
    // ── Paramètres ────────────────────────────────────────────────────────────

    private const float PlusRiseDuration  = 0.80f;
    private const float PlusRiseHeight    = 2.5f;
    private const float FlashDuration     = 0.45f;
    private const float FlashRingCount    = 2;
    private const float GlowPulseSpeed    = 2.5f;
    private const float GlowAlphaPerLevel = 0.20f;   // contribu par niveau d'upgrade

    // Teintes du glow selon la puissance (0=transparent → 3=éclatant)
    private static readonly Color ColGlowBase = new Color(1f, 1f, 1f, 0f);

    // ── Référence joueur ──────────────────────────────────────────────────────

    private SpriteRenderer _playerSr;
    private Color          _playerBaseColor;
    private SpriteRenderer _glowSr;
    private Transform      _glowTr;
    private int            _powerLevel;        // 0..3
    private float          _glowPhase;

    // ── Init ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Attache les effets de puissance au joueur.
    /// À appeler une fois depuis <see cref="TBSceneSetup"/>.
    /// </summary>
    public void Init(SpriteRenderer playerRenderer)
    {
        _playerSr        = playerRenderer;
        _playerBaseColor = playerRenderer.color;
        _glowPhase       = 0f;

        BuildGlow();
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>
    /// Déclenche le feedback d'amélioration : flash + labels + montée de puissance.
    /// À appeler depuis <see cref="TBUpgradeShopWidget"/> après chaque achat.
    /// </summary>
    public static void TriggerUpgrade(Vector3 worldPos)
    {
        var fx = FindFirstObjectByType<TBUpgradeFX>();
        if (fx == null) return;

        fx._powerLevel = Mathf.Min(fx._powerLevel + 1, 3);
        fx.StartCoroutine(fx.DoFlash(worldPos));
        fx.StartCoroutine(fx.DoFloatingLabels(worldPos));
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Update()
    {
        if (_glowSr == null) return;

        // Glow permanent même à puissance 0 (joueur blanc brillant dès le départ)
        int effectiveLevel = Mathf.Max(_powerLevel, 1);

        float t     = Mathf.Sin(Time.time * GlowPulseSpeed + _glowPhase) * 0.5f + 0.5f;
        float alpha = effectiveLevel * GlowAlphaPerLevel * Mathf.Lerp(0.5f, 1f, t);

        // Taille réduite de 50% : oscille entre 0.55 et 0.70 (au lieu de 1.10–1.40)
        float sizeBase = 0.55f + _powerLevel * 0.05f;
        float sizeTop  = 0.70f + _powerLevel * 0.08f;
        _glowTr.localScale = new Vector3(
            Mathf.Lerp(sizeBase, sizeTop, t),
            Mathf.Lerp(sizeBase, sizeTop, t),
            1f);

        _glowSr.color = new Color(1f, 1f, 1f, alpha);

        // Teinte du corps : blanc pur au niveau 0, de plus en plus éclatant ensuite
        float brightness = 1f + _powerLevel * 0.18f;
        _playerSr.color  = new Color(
            Mathf.Min(_playerBaseColor.r * brightness, 1f),
            Mathf.Min(_playerBaseColor.g * brightness, 1f),
            Mathf.Min(_playerBaseColor.b * brightness, 1f),
            _playerBaseColor.a);
    }

    // ── Glow continu ──────────────────────────────────────────────────────────

    private void BuildGlow()
    {
        var go = new GameObject("PlayerGlow");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale    = Vector3.one;

        var sr          = go.AddComponent<SpriteRenderer>();
        sr.sprite       = SpriteGenerator.CreateCircle(64);
        sr.sortingOrder = _playerSr.sortingOrder - 1;
        sr.color        = new Color(1f, 1f, 1f, 0f);

        _glowSr = sr;
        _glowTr = go.transform;
    }

    // ── Flash d'amélioration ──────────────────────────────────────────────────

    private IEnumerator DoFlash(Vector3 pos)
    {
        for (int i = 0; i < FlashRingCount; i++)
        {
            SpawnFlashRing(pos, i);
            yield return new WaitForSeconds(0.08f);
        }
    }

    private void SpawnFlashRing(Vector3 pos, int index)
    {
        var go = new GameObject($"UpgradeRing_{index}");
        go.transform.position   = pos;
        go.transform.localScale = Vector3.one * 0.2f;

        var sr          = go.AddComponent<SpriteRenderer>();
        sr.sprite       = SpriteGenerator.CreateRing(64, 0.15f);
        sr.color        = Color.white;
        sr.sortingOrder = 25;

        StartCoroutine(AnimateFlashRing(go, sr));
    }

    private static IEnumerator AnimateFlashRing(GameObject go, SpriteRenderer sr)
    {
        float t        = 0f;
        float endScale = 2.5f;

        while (t < FlashDuration && go != null)
        {
            t += Time.deltaTime;
            float r = t / FlashDuration;
            go.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, endScale, r);
            if (sr != null) sr.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0f, r));
            yield return null;
        }

        if (go != null) Destroy(go);
    }

    // ── Labels "++" flottants ─────────────────────────────────────────────────

    private IEnumerator DoFloatingLabels(Vector3 startPos)
    {
        int count = _powerLevel;   // 1 à 3 labels selon le niveau de puissance
        for (int i = 0; i < count; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-0.6f, 0.6f), i * 0.5f, 0f);
            StartCoroutine(SpawnPlusLabel(startPos + offset));
            yield return new WaitForSeconds(0.08f);
        }
    }

    private static IEnumerator SpawnPlusLabel(Vector3 startPos)
    {
        // Construit un "+" à partir de deux sprites carrés blancs croisés
        var root = new GameObject("UpgradePlus");
        root.transform.position = startPos;

        SpawnBar(root.transform, new Vector3(0f, 0f, 0f), new Vector3(0.35f, 0.09f, 1f));
        SpawnBar(root.transform, new Vector3(0f, 0f, 0f), new Vector3(0.09f, 0.35f, 1f));

        // Collecte les SpriteRenderers pour le fade
        var srs = root.GetComponentsInChildren<SpriteRenderer>();

        float t = 0f;
        while (t < PlusRiseDuration && root != null)
        {
            t += Time.deltaTime;
            float r = t / PlusRiseDuration;

            root.transform.position = startPos + new Vector3(0f, r * PlusRiseHeight, 0f);

            float alpha = Mathf.Lerp(1f, 0f, Mathf.Pow(r, 0.6f));
            foreach (var sr in srs)
                if (sr != null) sr.color = new Color(1f, 1f, 1f, alpha);

            yield return null;
        }

        if (root != null) Destroy(root);
    }

    private static void SpawnBar(Transform parent, Vector3 localPos, Vector3 scale)
    {
        var go = new GameObject("Bar");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = scale;

        var sr          = go.AddComponent<SpriteRenderer>();
        sr.sprite       = SpriteGenerator.CreateWhiteSquare();
        sr.color        = Color.white;
        sr.sortingOrder = 25;
    }
}
