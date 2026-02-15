using UnityEngine;

public class PlayerCollectEffect : MonoBehaviour
{
    [Header("Scale Pulse Settings")]
    [SerializeField] private float pulseScale = 1.2f;
    [SerializeField] private float pulseDuration = 0.15f;

    [Header("Color Flash Settings")]
    [SerializeField] private Color flashColor = Color.green;
    [SerializeField] private float flashDuration = 0.15f;

    private Vector3 originalScale;
    private Color originalColor;
    private SpriteRenderer spriteRenderer;
    
    private float pulseTimer = 0f;
    private bool isPulsing = false;
    private bool isFlashing = false;

    private void Awake()
    {
        originalScale = transform.localScale;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged.AddListener(OnScoreChanged);
        }
    }

    private void OnScoreChanged(int score)
    {
        TriggerEffect();
    }

    public void TriggerEffect()
    {
        isPulsing = true;
        isFlashing = true;
        pulseTimer = 0f;
    }

    private void Update()
    {
        if (isPulsing)
        {
            UpdatePulse();
        }

        if (isFlashing)
        {
            UpdateFlash();
        }
    }

    private void UpdatePulse()
    {
        pulseTimer += Time.deltaTime;
        float progress = pulseTimer / pulseDuration;

        if (progress < 0.5f)
        {
            float scaleProgress = progress * 2f;
            transform.localScale = Vector3.Lerp(originalScale, originalScale * pulseScale, scaleProgress);
        }
        else if (progress < 1f)
        {
            float scaleProgress = (progress - 0.5f) * 2f;
            transform.localScale = Vector3.Lerp(originalScale * pulseScale, originalScale, scaleProgress);
        }
        else
        {
            transform.localScale = originalScale;
            isPulsing = false;
        }
    }

    private void UpdateFlash()
    {
        if (spriteRenderer == null)
        {
            isFlashing = false;
            return;
        }

        float progress = pulseTimer / flashDuration;

        if (progress < 0.5f)
        {
            float colorProgress = progress * 2f;
            spriteRenderer.color = Color.Lerp(originalColor, flashColor, colorProgress);
        }
        else if (progress < 1f)
        {
            float colorProgress = (progress - 0.5f) * 2f;
            spriteRenderer.color = Color.Lerp(flashColor, originalColor, colorProgress);
        }
        else
        {
            spriteRenderer.color = originalColor;
            isFlashing = false;
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged.RemoveListener(OnScoreChanged);
        }
    }
}
