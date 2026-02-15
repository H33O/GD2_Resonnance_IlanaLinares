using UnityEngine;

public class ScorePulseEffect : MonoBehaviour
{
    [Header("Pulse Settings")]
    [SerializeField] private float pulseScale = 1.3f;
    [SerializeField] private float pulseDuration = 0.2f;

    private Vector3 originalScale;
    private float pulseTimer = 0f;
    private bool isPulsing = false;

    private void Awake()
    {
        originalScale = transform.localScale;
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
        TriggerPulse();
    }

    public void TriggerPulse()
    {
        isPulsing = true;
        pulseTimer = 0f;
    }

    private void Update()
    {
        if (!isPulsing) return;

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

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged.RemoveListener(OnScoreChanged);
        }
    }
}
