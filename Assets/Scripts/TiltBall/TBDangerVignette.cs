using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quatre halos blancs sur les bords de l'écran (gauche, droite, haut, bas) qui pulsent
/// quand un ennemi s'approche du joueur. Plus l'ennemi est proche, plus le pulse est intense.
/// Ce composant se place en Canvas UI (ScreenSpaceOverlay).
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class TBDangerVignette : MonoBehaviour
{
    // ── Paramètres ────────────────────────────────────────────────────────────

    /// <summary>Distance monde en dessous de laquelle le pulse commence.</summary>
    private const float DangerRange      = 5.0f;
    /// <summary>Distance monde où l'intensité est à son maximum.</summary>
    private const float CriticalRange    = 1.5f;
    private const float PulseSpeedMin    = 1.5f;
    private const float PulseSpeedMax    = 6.0f;
    private const float AlphaMax         = 0.55f;
    private const float AlphaResting     = 0.0f;

    // ── Références ────────────────────────────────────────────────────────────

    private Image[] _panels;   // 4 panneaux : gauche, droite, haut, bas
    private float   _pulsePhase;

    // ── Init ──────────────────────────────────────────────────────────────────

    /// <summary>Crée les quatre panneaux de vignette dans le <paramref name="canvasRT"/> donné.</summary>
    public void Init(RectTransform canvasRT)
    {
        _panels = new Image[4];

        // Positions des panneaux : (anchorMin, anchorMax)
        var anchors = new (Vector2 min, Vector2 max)[]
        {
            (new Vector2(0f, 0f), new Vector2(0.18f, 1f)),   // gauche
            (new Vector2(0.82f, 0f), new Vector2(1f, 1f)),   // droite
            (new Vector2(0f, 0.82f), new Vector2(1f, 1f)),   // haut
            (new Vector2(0f, 0f),    new Vector2(1f, 0.18f)),// bas
        };

        string[] names = { "VigLeft", "VigRight", "VigTop", "VigBottom" };

        for (int i = 0; i < 4; i++)
        {
            var go  = new GameObject(names[i]);
            go.transform.SetParent(canvasRT, false);

            var img = go.AddComponent<Image>();
            img.sprite        = SpriteGenerator.CreateWhiteSquare();
            img.color         = new Color(1f, 1f, 1f, AlphaResting);
            img.raycastTarget = false;

            var rt       = img.rectTransform;
            rt.anchorMin = anchors[i].min;
            rt.anchorMax = anchors[i].max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            _panels[i] = img;
        }
    }

    // ── Update ────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (_panels == null) return;

        float danger = ComputeDangerLevel();
        if (danger <= 0f)
        {
            SetAlpha(0f);
            return;
        }

        _pulsePhase += Time.deltaTime * Mathf.Lerp(PulseSpeedMin, PulseSpeedMax, danger);
        float pulse = Mathf.Sin(_pulsePhase) * 0.5f + 0.5f;
        SetAlpha(danger * AlphaMax * pulse);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Retourne 0-1 selon la proximité de l'ennemi le plus proche du joueur.</summary>
    private static float ComputeDangerLevel()
    {
        var player = FindFirstObjectByType<TBPlayerController>();
        if (player == null || !player.IsAlive) return 0f;

        Vector2 playerPos = player.transform.position;
        float   minDist   = float.MaxValue;

        foreach (var enemy in FindObjectsByType<TBEnemyController>(FindObjectsSortMode.None))
        {
            float d = Vector2.Distance(playerPos, enemy.transform.position);
            if (d < minDist) minDist = d;
        }

        if (minDist >= DangerRange) return 0f;
        return Mathf.Clamp01(1f - (minDist - CriticalRange) / (DangerRange - CriticalRange));
    }

    private void SetAlpha(float alpha)
    {
        foreach (var p in _panels)
            if (p != null)
                p.color = new Color(1f, 1f, 1f, alpha);
    }
}
