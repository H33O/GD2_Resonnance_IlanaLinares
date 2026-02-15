using UnityEngine;

public class Collectible : MonoBehaviour
{
    [Header("Grid Movement Settings")]
    [SerializeField] private float gridSize = 0.5f;
    [SerializeField] private float stepDuration = 0.3f;
    [SerializeField] private float destroyY = -6f;
    [SerializeField] private float groundY = -4.5f;

    [Header("Score Value")]
    [SerializeField] private int scoreValue = 1;

    private float columnX;
    private bool columnSet = false;
    private bool wasCollected = false;
    private float nextStepTime;
    private float currentGridY;

    public void SetColumn(float xPosition)
    {
        columnX = xPosition;
        columnSet = true;
        currentGridY = Mathf.Round(transform.position.y / gridSize) * gridSize;
        transform.position = new Vector3(columnX, currentGridY, transform.position.z);
        nextStepTime = Time.time + stepDuration;
    }

    public void SetStepDuration(float duration)
    {
        stepDuration = duration;
    }

    private void Update()
    {
        if (!columnSet || wasCollected) return;

        if (Time.time >= nextStepTime)
        {
            currentGridY -= gridSize;
            transform.position = new Vector3(columnX, currentGridY, transform.position.z);
            nextStepTime = Time.time + stepDuration;
        }

        if (!wasCollected && currentGridY <= groundY)
        {
            MissedCollectible();
        }

        if (currentGridY < destroyY)
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
        {
            GameManager.Instance.AddScore(scoreValue);
        }

        Destroy(gameObject);
    }

    private void MissedCollectible()
    {
        wasCollected = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoseLife();
        }

        Destroy(gameObject);
    }
}