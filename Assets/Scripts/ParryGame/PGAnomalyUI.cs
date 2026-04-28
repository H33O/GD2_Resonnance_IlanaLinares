using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Anomalie blanche organique en bas-gauche et bas-droite du canvas UI du Parry Game.
/// Utilise des Images UI (espace canvas) pour rester compatible avec la caméra perspective.
/// Reproduit l'esthétique de <see cref="BubbleAnomaly"/> sans dépendre de l'orthographicSize.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class PGAnomalyUI : MonoBehaviour
{
    // ── Paramètres ────────────────────────────────────────────────────────────

    private const int   BlobsPerSide   = 22;
    private const float NoiseSpeed     = 0.35f;
    private const float MaxAlpha       = 0.60f;
    private const float MinAlpha       = 0.06f;

    // Taille des blobs en pixels de référence (1080×1920)
    private const float BlobSizeMin    = 30f;
    private const float BlobSizeMax    = 130f;

    // Étendue du cluster depuis le coin, en pixels de référence
    private const float ClusterSpread  = 380f;

    // ── État runtime ──────────────────────────────────────────────────────────

    private struct AnomalyBlob
    {
        public RectTransform RT;
        public Image         Img;
        public Vector2       LocalOffset;
        public float         NoiseOffsetX;
        public float         NoiseOffsetY;
        public float         SizeBase;
        public float         AlphaBase;
    }

    private AnomalyBlob[] _left;
    private AnomalyBlob[] _right;
    private RectTransform _canvasRT;
    private Sprite        _circle;

    // ── Init ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Crée les deux clusters dans le <paramref name="canvasRT"/> fourni.
    /// À appeler depuis <see cref="PGSceneSetup"/>.
    /// </summary>
    public void Init(RectTransform canvasRT)
    {
        _canvasRT = canvasRT;
        _circle   = SpriteGenerator.CreateCircle(32);

        _left  = BuildCluster("PGAnomalyLeft",  canvasRT, -1f);
        _right = BuildCluster("PGAnomalyRight", canvasRT,  1f);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (_left == null || _right == null || _canvasRT == null) return;

        float hw   = _canvasRT.rect.width  * 0.5f;
        float hh   = _canvasRT.rect.height * 0.5f;
        float t    = Time.time * NoiseSpeed;

        AnimateCluster(_left,  -hw, -hh, t, 0f);
        AnimateCluster(_right,  hw, -hh, t, 100f);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private AnomalyBlob[] BuildCluster(string rootName, RectTransform parent, float sideSign)
    {
        var root = new GameObject(rootName);
        root.transform.SetParent(parent, false);

        var rootRT       = root.AddComponent<RectTransform>();
        rootRT.anchorMin = new Vector2(0.5f, 0.5f);
        rootRT.anchorMax = new Vector2(0.5f, 0.5f);
        rootRT.sizeDelta = Vector2.zero;

        var blobs = new AnomalyBlob[BlobsPerSide];

        for (int i = 0; i < BlobsPerSide; i++)
        {
            var go = new GameObject($"Blob_{i}");
            go.transform.SetParent(root.transform, false);

            var img          = go.AddComponent<Image>();
            img.sprite       = _circle;
            img.raycastTarget = false;

            var rt           = img.rectTransform;
            rt.anchorMin     = new Vector2(0.5f, 0.5f);
            rt.anchorMax     = new Vector2(0.5f, 0.5f);
            rt.pivot         = new Vector2(0.5f, 0.5f);

            float angle  = Random.Range(10f, 80f) * Mathf.Deg2Rad;
            float radius = Random.Range(0.05f, 1f) * ClusterSpread;

            float ox = Mathf.Cos(angle) * radius * sideSign * -1f;
            float oy = Mathf.Sin(angle) * radius;

            float sizeBase  = Random.Range(BlobSizeMin, BlobSizeMax);
            float alphaBase = Random.Range(MinAlpha, MaxAlpha);

            blobs[i] = new AnomalyBlob
            {
                RT           = rt,
                Img          = img,
                LocalOffset  = new Vector2(ox, oy),
                NoiseOffsetX = Random.Range(0f, 100f),
                NoiseOffsetY = Random.Range(0f, 100f),
                SizeBase     = sizeBase,
                AlphaBase    = alphaBase,
            };
        }

        return blobs;
    }

    private static void AnimateCluster(AnomalyBlob[] blobs, float cornerX, float cornerY,
                                        float t, float noiseShift)
    {
        for (int i = 0; i < blobs.Length; i++)
        {
            ref var b = ref blobs[i];
            if (b.RT == null) continue;

            float nx   = Mathf.PerlinNoise(t + b.NoiseOffsetX + noiseShift, 0f);
            float ny   = Mathf.PerlinNoise(0f, t + b.NoiseOffsetY + noiseShift);
            float na   = Mathf.PerlinNoise(b.NoiseOffsetX, t + noiseShift);

            float drift = 30f;
            float px    = cornerX + b.LocalOffset.x + (nx - 0.5f) * drift;
            float py    = cornerY + b.LocalOffset.y + (ny - 0.5f) * drift;

            b.RT.anchoredPosition = new Vector2(px, py);

            float size  = b.SizeBase * Mathf.Lerp(0.6f, 1.4f, na);
            b.RT.sizeDelta = new Vector2(size, size);

            float alpha = b.AlphaBase * Mathf.Lerp(0.5f, 1f, na);
            b.Img.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
        }
    }
}
