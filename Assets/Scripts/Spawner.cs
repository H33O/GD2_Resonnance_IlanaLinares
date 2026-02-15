using UnityEngine;

public class Spawner : MonoBehaviour
{
    [Header("Grid Spawn Settings")]
    [SerializeField] private GameObject collectiblePrefab;
    [SerializeField] private float baseSpawnInterval = 1.5f;
    [SerializeField] private int numberOfColumns = 3;
    [SerializeField] private float columnSpacing = 2f;
    [SerializeField] private float spawnY = 5f;

    private float[] columnPositions;
    private float nextSpawnTime;
    private float currentSpawnInterval;
    private float currentStepDuration = 0.3f;

    private void Awake()
    {
        InitializeColumns();
        currentSpawnInterval = baseSpawnInterval;
    }

    private void InitializeColumns()
    {
        columnPositions = new float[numberOfColumns];
        float totalWidth = (numberOfColumns - 1) * columnSpacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < numberOfColumns; i++)
        {
            columnPositions[i] = startX + (i * columnSpacing);
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsGameActive)
            return;

        if (Time.time >= nextSpawnTime)
        {
            SpawnCollectible();
            currentSpawnInterval = GameManager.Instance.GetCurrentSpawnInterval();
            currentStepDuration = GameManager.Instance.GetCurrentStepDuration();
            nextSpawnTime = Time.time + currentSpawnInterval;
        }
    }

    private void SpawnCollectible()
    {
        if (collectiblePrefab == null)
        {
            Debug.LogWarning("Collectible prefab is not assigned!");
            return;
        }

        int randomColumn = Random.Range(0, numberOfColumns);
        Vector3 spawnPosition = new Vector3(columnPositions[randomColumn], spawnY, 0f);

        GameObject collectible = Instantiate(collectiblePrefab, spawnPosition, Quaternion.identity);
        
        Collectible collectibleScript = collectible.GetComponent<Collectible>();
        if (collectibleScript != null)
        {
            collectibleScript.SetColumn(columnPositions[randomColumn]);
            collectibleScript.SetStepDuration(currentStepDuration);
        }
    }

    public void IncreaseSpeed()
    {
        Debug.Log($"Difficulté augmentée ! Interval: {currentSpawnInterval:F2}s, Step: {currentStepDuration:F2}s");
    }

    private void OnDrawGizmos()
    {
        if (columnPositions == null || columnPositions.Length == 0)
        {
            InitializeColumns();
        }

        Gizmos.color = Color.yellow;
        foreach (float columnX in columnPositions)
        {
            Vector3 topPoint = new Vector3(columnX, spawnY, 0f);
            Vector3 bottomPoint = new Vector3(columnX, -5f, 0f);
            
            Gizmos.DrawLine(topPoint, bottomPoint);
            Gizmos.DrawWireSphere(topPoint, 0.3f);
            
            for (float y = spawnY; y > -5f; y -= 0.5f)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                Gizmos.DrawWireCube(new Vector3(columnX, y, 0f), new Vector3(0.4f, 0.4f, 0.1f));
            }
        }
    }
}
