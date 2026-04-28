using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Marks a bubble as a bonus bubble. When hit directly by a projectile of the same color,
/// awards extra shots to the player.
/// </summary>
public class BonusBubble : MonoBehaviour
{
    public BubbleColor ColorType  { get; private set; }
    public int         BonusAmount { get; private set; }

    private SpriteRenderer glowSR;

    /// <summary>Initialises bonus data and overlays the "+X" label.</summary>
    public void Init(BubbleColor color, int bonusAmount)
    {
        ColorType   = color;
        BonusAmount = bonusAmount;

        // Compensation scale: bubble GO is already scaled by diameter,
        // so the label's localScale must cancel that out for correct world size.
        float diameter    = transform.localScale.x;
        float scaleComp   = diameter > 0.001f ? 1f / diameter : 2f;

        // ── Glow ring coloré (couleur de la bulle, pas doré) ─────────────────
        var glowGO = new GameObject("BonusGlow");
        glowGO.transform.SetParent(transform, false);
        glowGO.transform.localPosition = Vector3.zero;
        glowGO.transform.localScale    = Vector3.one * 1.40f;
        glowSR              = glowGO.AddComponent<SpriteRenderer>();
        glowSR.sprite       = SpriteGenerator.Circle();
        // Couleur de la bulle bonus pour indiquer quelle couleur est requise
        Color bubbleCol     = color.ToUnityColor();
        glowSR.color        = new Color(bubbleCol.r, bubbleCol.g, bubbleCol.b, 0.55f);
        glowSR.sortingOrder = -1;

        // ── "+X" white label centred on the bubble ────────────────────────────
        var labelGO = new GameObject("BonusLabel");
        labelGO.transform.SetParent(transform, false);
        labelGO.transform.localPosition = Vector3.zero;
        labelGO.transform.localScale    = Vector3.one * scaleComp;

        var tmp        = labelGO.AddComponent<TextMeshPro>();
        tmp.text       = $"+{bonusAmount}";
        tmp.fontSize   = 0.30f;
        tmp.fontStyle  = FontStyles.Bold;
        tmp.color      = Color.white;
        tmp.alignment  = TextAlignmentOptions.Center;
        tmp.sortingOrder = 3;

        StartCoroutine(GlowPulseRoutine(bubbleCol));
    }

    private IEnumerator GlowPulseRoutine(Color bubbleCol)
    {
        while (true)
        {
            float a = 0.35f + 0.25f * Mathf.Sin(Time.time * 4f);
            if (glowSR != null)
                glowSR.color = new Color(bubbleCol.r, bubbleCol.g, bubbleCol.b, a);
            yield return null;
        }
    }
}
