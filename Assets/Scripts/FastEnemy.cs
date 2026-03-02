using UnityEngine;

public class FastEnemy : MonoBehaviour
{
    [Header("Grid Movement Settings")]
    [SerializeField] private float gridSize = 0.5f;
    [SerializeField] private float stepDuration = 0.4f;
    [SerializeField] private float destroyY = -6f;
    [SerializeField] private float groundY = -4.5f;

    [Header("Score Value")]
    [SerializeField] private int scoreValue = 10;

    [Header("Visuel")]
    [SerializeField] private Sprite enemySprite;

    private float columnX;
    private bool  columnSet    = false;
    private bool  wasCollected = false;
    private float currentGridY;

    private void Awake()
    {
        if (enemySprite != null)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.sprite = enemySprite;
        }
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>Snaps the enemy to its column and registers it to the global grid tick.</summary>
    public void SetColumn(float xPosition)
    {
        columnX      = xPosition;
        columnSet    = true;
        currentGridY = Mathf.Round(transform.position.y / gridSize) * gridSize;
        transform.position = new Vector3(columnX, currentGridY, transform.position.z);
    }

    /// <summary>No-op — step rate is now driven by the global GameManager tick.</summary>
    public void SetStepDuration(float duration) { }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGridStep += OnStep;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGridStep -= OnStep;
    }

    // ── Grid step (2 pas par tick = deux fois plus rapide) ────────────────────

    private void OnStep()
    {
        if (!columnSet || wasCollected) return;

        currentGridY -= gridSize * 2f;
        transform.position = new Vector3(columnX, currentGridY, transform.position.z);

        if (currentGridY <= groundY)
        {
            MissedEnemy();
        }
        else if (currentGridY < destroyY)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !wasCollected)
        {
            Collect();
        }
    }

    private void Collect()
    {
        wasCollected = true;

        if (GameManager.Instance != null)
            GameManager.Instance.AddScore(scoreValue);

        UIManager.Instance?.ShowScoreGain(scoreValue);
        UIManager.Instance?.ShowPerfectEffect();
        ScreenGlitch.Instance?.Trigger();

        Destroy(gameObject);
    }

    private void MissedEnemy()
    {
        wasCollected = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoseLife();
        }

        Destroy(gameObject);
    }
}
