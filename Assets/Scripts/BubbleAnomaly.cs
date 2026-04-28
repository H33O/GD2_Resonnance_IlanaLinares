using UnityEngine;

/// <summary>
/// Anomalie blanche organique en bas-gauche et bas-droite de la scène Bulles.
/// Chaque tache est un cluster de cercles blancs superposés dont la taille globale
/// augmente à chaque appel à <see cref="SetLevel"/>. L'effet de bruit de Perlin
/// anime les cercles individuels pour donner un aspect « virus qui grandit ».
/// </summary>
public class BubbleAnomaly : MonoBehaviour
{
    // ── Paramètres ────────────────────────────────────────────────────────────

    private const int   BlobsPerSide   = 22;     // cercles par côté
    private const float NoiseSpeed     = 0.35f;  // vitesse d'animation Perlin
    private const float BaseScale      = 0.55f;  // taille globale au niveau 1
    private const float ScalePerLevel  = 0.65f;  // agrandissement par niveau supplémentaire
    private const float MaxAlpha       = 0.72f;
    private const float MinAlpha       = 0.08f;
    private const int   SortingOrder   = -8;

    // ── État runtime ──────────────────────────────────────────────────────────

    private struct AnomalyBlob
    {
        public Transform      Tr;
        public SpriteRenderer Sr;
        public Vector2        LocalOffset;   // position de base relative au coin
        public float          NoiseOffsetX;
        public float          NoiseOffsetY;
        public float          SizeBase;
        public float          AlphaBase;
    }

    private AnomalyBlob[] _left;
    private AnomalyBlob[] _right;

    private float  _halfW, _halfH;
    private float  _currentScale = 1f;
    private Sprite _circle;

    // ── Init ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Crée les deux clusters d'anomalie.
    /// À appeler une seule fois depuis <see cref="BubbleSceneSetup"/>.
    /// </summary>
    public void Init()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        _halfH  = cam.orthographicSize;
        _halfW  = _halfH * cam.aspect;
        _circle = SpriteGenerator.CreateCircle(32);

        _left  = BuildCluster("AnomalyLeft",  -1f);
        _right = BuildCluster("AnomalyRight",  1f);

        RefreshScale();
    }

    /// <summary>
    /// Définit le niveau courant (1-based). Agrandit les anomalies de façon cumulative.
    /// </summary>
    /// <param name="levelNumber">Numéro de niveau : 1, 2, 3…</param>
    public void SetLevel(int levelNumber)
    {
        _currentScale = BaseScale + (levelNumber - 1) * ScalePerLevel;
        RefreshScale();
    }

    // ── Update ────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (_left == null || _right == null) return;

        float t = Time.time * NoiseSpeed;
        AnimateCluster(_left,  -_halfW, -_halfH, t, 0f);
        AnimateCluster(_right,  _halfW, -_halfH, t, 100f);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private AnomalyBlob[] BuildCluster(string rootName, float sideSign)
    {
        var root = new GameObject(rootName);
        root.transform.SetParent(transform, false);

        var blobs = new AnomalyBlob[BlobsPerSide];

        for (int i = 0; i < BlobsPerSide; i++)
        {
            var go = new GameObject($"Blob_{i}");
            go.transform.SetParent(root.transform, false);

            var sr          = go.AddComponent<SpriteRenderer>();
            sr.sprite       = _circle;
            sr.sortingOrder = SortingOrder;

            // Offset local : répartis en éventail depuis le coin
            float angle  = Random.Range(10f, 80f) * Mathf.Deg2Rad;
            float radius = Random.Range(0.1f, 1.2f);

            // Pour le coin gauche : angle vers le haut et vers la droite (intérieur).
            // Pour le coin droit  : même chose mais miroir.
            float ox = Mathf.Cos(angle) * radius * sideSign * -1f; // vers l'intérieur
            float oy = Mathf.Sin(angle) * radius;                   // vers le haut

            float sizeBase  = Random.Range(0.15f, 0.55f);
            float alphaBase = Random.Range(MinAlpha, MaxAlpha);

            blobs[i] = new AnomalyBlob
            {
                Tr           = go.transform,
                Sr           = sr,
                LocalOffset  = new Vector2(ox, oy),
                NoiseOffsetX = Random.Range(0f, 100f),
                NoiseOffsetY = Random.Range(0f, 100f),
                SizeBase     = sizeBase,
                AlphaBase    = alphaBase,
            };
        }

        return blobs;
    }

    private void AnimateCluster(AnomalyBlob[] blobs, float cornerX, float cornerY,
                                 float t, float noiseShift)
    {
        for (int i = 0; i < blobs.Length; i++)
        {
            ref var b = ref blobs[i];
            if (b.Tr == null) continue;

            // Perlin noise pour faire « vivre » chaque blob
            float nx   = Mathf.PerlinNoise(t + b.NoiseOffsetX + noiseShift, 0f);    // 0–1
            float ny   = Mathf.PerlinNoise(0f, t + b.NoiseOffsetY + noiseShift);

            // Position : coin + offset de base animé par un léger drift Perlin
            float drift = 0.18f * _currentScale;
            float px    = cornerX + b.LocalOffset.x * _currentScale + (nx - 0.5f) * drift;
            float py    = cornerY + b.LocalOffset.y * _currentScale + (ny - 0.5f) * drift;

            b.Tr.position = new Vector3(px, py, 0f);

            // Taille : animée par Perlin
            float na   = Mathf.PerlinNoise(b.NoiseOffsetX, t + noiseShift);
            float size = b.SizeBase * _currentScale * Mathf.Lerp(0.6f, 1.4f, na);
            b.Tr.localScale = new Vector3(size, size, 1f);

            // Alpha : pulsation lente
            float alpha = b.AlphaBase * Mathf.Lerp(0.5f, 1f, na);
            b.Sr.color  = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
        }
    }

    private void RefreshScale()
    {
        // Appelé immédiatement après un changement de niveau pour appliquer la nouvelle scale
        // avant le prochain Update (évite un frame de latence).
        if (_left  != null) AnimateCluster(_left,  -_halfW, -_halfH, 0f, 0f);
        if (_right != null) AnimateCluster(_right,  _halfW, -_halfH, 0f, 100f);
    }
}
