using UnityEngine;

public class Collectible : MonoBehaviour
{
    [Header("Grid Movement Settings")]
    [SerializeField] private float gridSize = 0.5f;
    [SerializeField] private float destroyY = -6f;
    [SerializeField] private float groundY  = -4.5f;

    [Header("Score Value")]
    [SerializeField] private int scoreValue = 1;

    private float columnX;
    private bool  columnSet    = false;
    private bool  wasCollected = false;
    private float currentGridY;

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>Snaps the collectible to its column and registers it to the global grid tick.</summary>
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

    // ── Grid step ─────────────────────────────────────────────────────────────

    private void OnStep()
    {
        if (!columnSet || wasCollected) return;

        currentGridY -= gridSize;
        transform.position = new Vector3(columnX, currentGridY, transform.position.z);

        if (currentGridY <= groundY)
        {
            MissedCollectible();
        }
        else if (currentGridY < destroyY)
        {
            Destroy(gameObject);
        }
    }

    // ── Trigger ───────────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !wasCollected)
            Collect();
    }

    private void Collect()
    {
        wasCollected = true;
        GameManager.Instance?.AddScore(scoreValue);

        // Notifie le PlayerVisuals pour déclencher la croissance des éclairs
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            player.GetComponent<PlayerVisuals>()?.OnCollect();

        Destroy(gameObject);
    }

    private void MissedCollectible()
    {
        wasCollected = true;
        GameManager.Instance?.LoseLife();
        Destroy(gameObject);
    }
}