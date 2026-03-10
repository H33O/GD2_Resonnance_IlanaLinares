using UnityEngine;

/// <summary>
/// Représentation visuelle du joueur sous forme d'un disque blanc lumineux
/// entouré d'éclairs procéduraux. Grandit à chaque collectible ramassé.
/// </summary>
public class PlayerVisuals : MonoBehaviour
{
    [Header("Cercle joueur")]
    [SerializeField] private float baseRadius  = 0.30f;
    [SerializeField] private Color playerColor = Color.white;
    [SerializeField] private Color glowColor   = new Color(1f, 1f, 1f, 0.25f);

    /// <summary>Accès à l'effet d'éclairs pour notifier une collecte.</summary>
    public PlayerLightningEffect Lightning { get; private set; }

    private SpriteRenderer bodyRenderer;
    private SpriteRenderer glowRenderer;

    private void Start()
    {
        BuildCircle();
        BuildGlow();
        BuildLightning();
        AdjustCollider();
    }

    private void BuildCircle()
    {
        bodyRenderer = GetComponent<SpriteRenderer>();
        if (bodyRenderer == null)
            bodyRenderer = gameObject.AddComponent<SpriteRenderer>();

        bodyRenderer.sprite       = SpriteGenerator.CreateCircle(128);
        bodyRenderer.color        = playerColor;
        bodyRenderer.sortingOrder = 10;

        float diameter       = baseRadius * 2f;
        transform.localScale = Vector3.one * diameter;
    }

    private void BuildGlow()
    {
        var glowGO                  = new GameObject("PlayerGlow");
        glowGO.transform.SetParent(transform, false);
        glowGO.transform.localScale    = Vector3.one * 1.8f;
        glowGO.transform.localPosition = Vector3.zero;

        glowRenderer              = glowGO.AddComponent<SpriteRenderer>();
        glowRenderer.sprite       = SpriteGenerator.CreateCircle(128);
        glowRenderer.color        = glowColor;
        glowRenderer.sortingOrder = 9;
    }

    private void BuildLightning()
    {
        Lightning = gameObject.AddComponent<PlayerLightningEffect>();
    }

    private void AdjustCollider()
    {
        // Conserve le BoxCollider2D existant s'il y en a un, sinon ajoute un CircleCollider2D
        var box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            box.size = Vector2.one;
            return;
        }

        var col  = GetComponent<CircleCollider2D>();
        if (col == null) col = gameObject.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;
    }

    private void Update()
    {
        // Pulsation douce du glow
        if (glowRenderer != null)
        {
            float alpha       = 0.18f + 0.10f * Mathf.Sin(Time.time * 2.8f);
            glowRenderer.color = new Color(glowColor.r, glowColor.g, glowColor.b, alpha);
        }
    }

    /// <summary>
    /// Appelé depuis le système de collecte — déclenche la croissance et le flash d'éclairs.
    /// </summary>
    public void OnCollect()
    {
        Lightning?.OnCollect();
    }
}