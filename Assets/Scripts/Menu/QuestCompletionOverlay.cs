using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Overlay plein-écran affiché quand le joueur valide une quête.
///
/// Effets :
///   - Fond semi-transparent flouté (simulé via un panneau noir très opaque)
///   - Widget centré "Quête validée" + titre de la quête + récompense
///   - Particle system d'éclats dorés en 2D (via UI Image animées)
///   - Animation d'apparition : scale punch + fade in
///   - L'overlay se ferme automatiquement après <see cref="DisplayDuration"/> secondes
///   - Au moment du crédit, le <see cref="CoinWalletWidget"/> reçoit ses pièces et pulse
/// </summary>
public class QuestCompletionOverlay : MonoBehaviour
{
    // ── Timings ───────────────────────────────────────────────────────────────

    private const float FadeInDur      = 0.25f;
    private const float PunchInDur     = 0.35f;
    private const float DisplayDuration = 2.80f;
    private const float FadeOutDur     = 0.30f;

    // ── Particules ────────────────────────────────────────────────────────────

    private const int   ParticleCount   = 18;
    private const float ParticleMinSize = 10f;
    private const float ParticleMaxSize = 26f;
    private const float ParticleLifeMin = 0.55f;
    private const float ParticleLifeMax = 1.10f;
    private const float ParticleSpread  = 320f;   // rayon max en px canvas

    // ── Palette ───────────────────────────────────────────────────────────────

    private static readonly Color ColBlur       = new Color(0f,    0f,    0f,    0.70f);
    private static readonly Color ColCard       = new Color(0.07f, 0.06f, 0.14f, 0.97f);
    private static readonly Color ColAccent     = new Color(1.00f, 0.82f, 0.18f, 1.00f);
    private static readonly Color ColTitle      = new Color(1f,    1f,    1f,    1.00f);
    private static readonly Color ColSub        = new Color(1f,    1f,    1f,    0.60f);
    private static readonly Color ColReward     = new Color(1.00f, 0.82f, 0.18f, 1.00f);
    private static readonly Color ColParticleA  = new Color(1.00f, 0.82f, 0.18f, 1.00f);
    private static readonly Color ColParticleB  = new Color(1.00f, 0.55f, 0.10f, 1.00f);
    private static readonly Color ColParticleC  = new Color(1.00f, 1.00f, 0.55f, 1.00f);

    // ── Références runtime ────────────────────────────────────────────────────

    private RectTransform   _root;
    private CanvasGroup     _rootGroup;
    private RectTransform   _card;
    private CanvasGroup     _cardGroup;
    private RectTransform   _particleLayer;

    // ── File d'attente ────────────────────────────────────────────────────────

    private static QuestCompletionOverlay _instance;
    private bool _isShowing;

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>Crée l'overlay dans le canvas et le retourne (appelé une seule fois).</summary>
    public static QuestCompletionOverlay Create(RectTransform canvasRT)
    {
        var go   = new GameObject("QuestCompletionOverlay");
        go.transform.SetParent(canvasRT, false);

        var rt       = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var cg            = go.AddComponent<CanvasGroup>();
        cg.alpha          = 0f;
        cg.blocksRaycasts = false;
        cg.interactable   = false;

        var overlay       = go.AddComponent<QuestCompletionOverlay>();
        overlay._root     = rt;
        overlay._rootGroup = cg;
        overlay.BuildStructure(rt);

        _instance = overlay;
        return overlay;
    }

    // ── Construction ──────────────────────────────────────────────────────────

    private void BuildStructure(RectTransform root)
    {
        // Fond flouté (panel plein écran semi-transparent)
        var bgGO    = new GameObject("BlurBg");
        bgGO.transform.SetParent(root, false);
        var bgImg   = bgGO.AddComponent<Image>();
        bgImg.sprite = SpriteGenerator.CreateWhiteSquare();
        bgImg.color  = ColBlur;
        bgImg.raycastTarget = true;
        var bgRT    = bgImg.rectTransform;
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

        // Couche particules (devant tout)
        var plGO       = new GameObject("ParticleLayer");
        plGO.transform.SetParent(root, false);
        _particleLayer = plGO.AddComponent<RectTransform>();
        _particleLayer.anchorMin = new Vector2(0.5f, 0.5f);
        _particleLayer.anchorMax = new Vector2(0.5f, 0.5f);
        _particleLayer.pivot     = new Vector2(0.5f, 0.5f);
        _particleLayer.sizeDelta = Vector2.zero;

        // Card centrale
        var cardGO   = new GameObject("QuestCard");
        cardGO.transform.SetParent(root, false);
        cardGO.transform.SetSiblingIndex(1);   // derrière les particules

        _cardGroup          = cardGO.AddComponent<CanvasGroup>();
        _cardGroup.alpha    = 1f;

        var cardImg         = cardGO.AddComponent<Image>();
        cardImg.sprite      = SpriteGenerator.CreateWhiteSquare();
        cardImg.color       = ColCard;
        cardImg.raycastTarget = false;

        _card               = cardImg.rectTransform;
        _card.anchorMin     = new Vector2(0.5f, 0.5f);
        _card.anchorMax     = new Vector2(0.5f, 0.5f);
        _card.pivot         = new Vector2(0.5f, 0.5f);
        _card.sizeDelta     = new Vector2(820f, 360f);
        _card.anchoredPosition = Vector2.zero;

        // Bande dorée en haut
        var accentGO  = new GameObject("Accent");
        accentGO.transform.SetParent(_card, false);
        var accentImg = accentGO.AddComponent<Image>();
        accentImg.sprite = SpriteGenerator.CreateWhiteSquare();
        accentImg.color  = ColAccent;
        accentImg.raycastTarget = false;
        var accentRT  = accentImg.rectTransform;
        accentRT.anchorMin = new Vector2(0f, 0.88f);
        accentRT.anchorMax = Vector2.one;
        accentRT.offsetMin = accentRT.offsetMax = Vector2.zero;

        // "✓ QUÊTE VALIDÉE"
        AddLabel(_card, "TitleLabel", "✓  QUÊTE VALIDÉE",
            new Vector2(0.05f, 0.54f), new Vector2(0.95f, 0.88f),
            52f, ColTitle, FontStyles.Bold);

        // Nom de la quête (placeholder, mis à jour dans Show)
        AddLabel(_card, "QuestName", "",
            new Vector2(0.05f, 0.30f), new Vector2(0.95f, 0.55f),
            30f, ColSub, FontStyles.Normal);

        // Récompense
        AddLabel(_card, "RewardLabel", "",
            new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.30f),
            42f, ColReward, FontStyles.Bold);
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>Affiche l'overlay pour la quête donnée. Peut être appelé depuis n'importe quel système.</summary>
    public static void Show(QuestDefinition quest)
    {
        if (_instance == null) return;
        _instance.StartCoroutine(_instance.RunSequence(quest));
    }

    // ── Séquence principale ───────────────────────────────────────────────────

    private IEnumerator RunSequence(QuestDefinition quest)
    {
        // Si un autre overlay est déjà actif, attendre
        while (_isShowing) yield return null;
        _isShowing = true;

        // Remplir les textes
        SetText("QuestName",   quest.Description);
        SetText("RewardLabel", $"+{quest.RewardCoins} pièces");

        // Activer le raycasting pour bloquer les clics
        _rootGroup.blocksRaycasts = true;
        _rootGroup.interactable   = true;

        // Reset scale de la card
        _card.localScale = Vector3.one * 0.75f;

        // Fade in du fond + punch de la card
        float t = 0f;
        while (t < FadeInDur)
        {
            t += Time.deltaTime;
            float e       = Mathf.Clamp01(t / FadeInDur);
            _rootGroup.alpha = e;
            yield return null;
        }
        _rootGroup.alpha = 1f;

        // Punch scale de la card
        yield return StartCoroutine(PunchScale(_card, 0.75f, 1.08f, 1f, PunchInDur));

        // Lancer les particules
        StartCoroutine(SpawnParticles());

        // Maintien
        yield return new WaitForSeconds(DisplayDuration);

        // Fade out
        t = 0f;
        while (t < FadeOutDur)
        {
            t += Time.deltaTime;
            float e       = Mathf.Clamp01(t / FadeOutDur);
            _rootGroup.alpha = 1f - e;
            yield return null;
        }

        _rootGroup.alpha          = 0f;
        _rootGroup.blocksRaycasts = false;
        _rootGroup.interactable   = false;
        _isShowing = false;
    }

    // ── Particules ────────────────────────────────────────────────────────────

    private IEnumerator SpawnParticles()
    {
        Color[] palette = { ColParticleA, ColParticleB, ColParticleC };

        for (int i = 0; i < ParticleCount; i++)
        {
            SpawnParticle(palette[i % palette.Length]);
            yield return new WaitForSeconds(Random.Range(0.02f, 0.08f));
        }
    }

    private void SpawnParticle(Color baseColor)
    {
        var go  = new GameObject("Particle");
        go.transform.SetParent(_particleLayer, false);

        var img    = go.AddComponent<Image>();
        img.sprite = SpriteGenerator.CreateWhiteSquare();
        img.color  = baseColor;
        img.raycastTarget = false;

        float size = Random.Range(ParticleMinSize, ParticleMaxSize);

        var rt     = img.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = Vector2.zero;

        float angle    = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float speed    = Random.Range(180f, ParticleSpread);
        Vector2 dir    = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        float life     = Random.Range(ParticleLifeMin, ParticleLifeMax);
        float rotation = Random.Range(-360f, 360f);

        StartCoroutine(AnimateParticle(go, img, rt, dir * speed, rotation, life, baseColor));
    }

    private static IEnumerator AnimateParticle(GameObject go, Image img, RectTransform rt,
        Vector2 velocity, float rotationSpeed, float lifetime, Color baseColor)
    {
        float t = 0f;
        Vector2 pos = Vector2.zero;

        while (t < lifetime)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / lifetime);

            // Gravité simulée
            pos += velocity * Time.deltaTime;
            velocity += Vector2.down * 400f * Time.deltaTime;

            rt.anchoredPosition = pos;
            rt.localRotation    = Quaternion.Euler(0f, 0f, rotationSpeed * t);

            // Fade out sur la deuxième moitié
            float alpha = n < 0.4f ? 1f : Mathf.Lerp(1f, 0f, (n - 0.4f) / 0.6f);
            img.color   = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

            // Scale : grandit un peu puis rétrécit
            float s = Mathf.Sin(n * Mathf.PI) * 0.6f + 0.4f;
            rt.localScale = Vector3.one * s;

            yield return null;
        }

        Destroy(go);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static IEnumerator PunchScale(RectTransform rt, float from, float overshoot, float to, float dur)
    {
        float half = dur * 0.55f;
        float t    = 0f;

        // Phase montante
        while (t < half)
        {
            t += Time.deltaTime;
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / half), 3f);
            rt.localScale = Vector3.one * Mathf.Lerp(from, overshoot, e);
            yield return null;
        }

        // Phase retombée
        t = 0f;
        float rest = dur - half;
        while (t < rest)
        {
            t += Time.deltaTime;
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / rest), 3f);
            rt.localScale = Vector3.one * Mathf.Lerp(overshoot, to, e);
            yield return null;
        }

        rt.localScale = Vector3.one * to;
    }

    private void SetText(string childName, string text)
    {
        var label = _card.Find(childName)?.GetComponent<TextMeshProUGUI>();
        if (label != null) label.text = text;
    }

    private static void AddLabel(RectTransform parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax, float size, Color color, FontStyles style)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text        = text;
        tmp.fontSize    = size;
        tmp.fontStyle   = style;
        tmp.color       = color;
        tmp.alignment   = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        MenuAssets.ApplyFont(tmp);

        var rt    = tmp.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
