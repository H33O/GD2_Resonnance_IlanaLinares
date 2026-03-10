using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Balle rebondissante dans un Canvas Screen Space Overlay.
/// Utilise des coordonnées anchoredPosition relatives au centre du Canvas.
/// Détecte les bords de l'écran et les boutons Canvas enregistrés.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class MenuBall : MonoBehaviour
{
    // ── Configuration ─────────────────────────────────────────────────────────

    private const float Speed          = 520f;
    private const float MinSpeed       = 400f;
    private const float MaxSpeed       = 680f;
    private const float SpeedVariation = 28f;
    private const float BallRadius     = 22f;
    private const float GlowRadius     = 52f;

    // ── État ──────────────────────────────────────────────────────────────────

    private RectTransform rt;
    private Canvas        rootCanvas;
    private RectTransform canvasRT;
    private Vector2       velocity;
    private Image         glowImage;

    private readonly List<MenuCanvasButton> buttons = new();

    /// <summary>Déclenché à chaque rebond : position canvas, est-ce un bouton ?</summary>
    public event System.Action<Vector2, bool> OnBounce;

    // ── Initialisation ────────────────────────────────────────────────────────

    private void Awake() => rt = GetComponent<RectTransform>();

    private void Start()
    {
        canvasRT = rootCanvas.GetComponent<RectTransform>();
        BuildVisuals();
        Launch();
    }

    /// <summary>Injecte le canvas racine.</summary>
    public void SetCanvas(Canvas canvas) => rootCanvas = canvas;

    private void BuildVisuals()
    {
        rt.sizeDelta = Vector2.one * BallRadius * 2f;
        var img      = gameObject.AddComponent<Image>();
        img.sprite   = SpriteGenerator.Circle();
        img.color    = Color.white;
        img.raycastTarget = false;

        var glowGO           = new GameObject("BallGlow");
        glowGO.transform.SetParent(transform, false);
        glowImage            = glowGO.AddComponent<Image>();
        var glowRT           = glowImage.rectTransform;
        glowRT.sizeDelta     = Vector2.one * GlowRadius * 2f;
        glowRT.anchoredPosition = Vector2.zero;
        glowImage.sprite     = SpriteGenerator.Circle();
        glowImage.color      = new Color(1f, 1f, 1f, 0.15f);
        glowImage.raycastTarget = false;
        glowGO.transform.SetAsFirstSibling();
    }

    private void Launch()
    {
        float angle = Random.Range(25f, 65f) + Random.Range(0, 4) * 90f;
        velocity    = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad),
                                  Mathf.Sin(angle * Mathf.Deg2Rad)) * Speed;
    }

    // ── Enregistrement ────────────────────────────────────────────────────────

    /// <summary>Enregistre un bouton Canvas pour la détection de collision.</summary>
    public void RegisterButton(MenuCanvasButton btn)
    {
        if (!buttons.Contains(btn)) buttons.Add(btn);
    }

    // ── Boucle ────────────────────────────────────────────────────────────────

    private void Update()
    {
        rt.anchoredPosition += velocity * Time.deltaTime;
        CheckBorders();
        CheckButtons();
        PulseGlow();
    }

    private void CheckBorders()
    {
        Vector2 pos    = rt.anchoredPosition;
        Vector2 half   = canvasRT.rect.size * 0.5f;
        bool    bounced = false;

        if (pos.x - BallRadius < -half.x)     { pos.x = -half.x + BallRadius; velocity.x =  Mathf.Abs(velocity.x); bounced = true; }
        else if (pos.x + BallRadius > half.x) { pos.x =  half.x - BallRadius; velocity.x = -Mathf.Abs(velocity.x); bounced = true; }

        if (pos.y - BallRadius < -half.y)     { pos.y = -half.y + BallRadius; velocity.y =  Mathf.Abs(velocity.y); bounced = true; }
        else if (pos.y + BallRadius > half.y) { pos.y =  half.y - BallRadius; velocity.y = -Mathf.Abs(velocity.y); bounced = true; }

        rt.anchoredPosition = pos;

        if (bounced) { Vary(); OnBounce?.Invoke(pos, false); }
    }

    private void CheckButtons()
    {
        Vector2 ballPos = rt.anchoredPosition;

        foreach (var btn in buttons)
        {
            if (btn == null) continue;

            var     btnRT  = btn.GetRect();
            Vector2 btnPos = btnRT.anchoredPosition;
            Vector2 half   = btnRT.rect.size * 0.5f;

            Vector2 closest = new Vector2(
                Mathf.Clamp(ballPos.x, btnPos.x - half.x, btnPos.x + half.x),
                Mathf.Clamp(ballPos.y, btnPos.y - half.y, btnPos.y + half.y)
            );

            if (Vector2.Distance(ballPos, closest) >= BallRadius) continue;

            Vector2 normal      = (ballPos - closest).normalized;
            if (normal == Vector2.zero) normal = Vector2.up;

            rt.anchoredPosition = closest + normal * BallRadius;
            velocity            = Vector2.Reflect(velocity, normal);
            Vary();

            OnBounce?.Invoke(rt.anchoredPosition, true);
            btn.TriggerBounceReaction();
            break;
        }
    }

    private void Vary()
    {
        float s  = velocity.magnitude + Random.Range(-SpeedVariation, SpeedVariation);
        velocity = velocity.normalized * Mathf.Clamp(s, MinSpeed, MaxSpeed);
    }

    private void PulseGlow()
    {
        if (glowImage == null) return;
        // Reuse color struct — avoids per-frame heap allocation
        Color c = glowImage.color;
        c.a            = 0.10f + 0.08f * Mathf.Sin(Time.time * 3.5f);
        glowImage.color = c;
    }

    /// <summary>Position canvas actuelle de la balle.</summary>
    public Vector2 GetCanvasPosition() => rt.anchoredPosition;
}
