using UnityEngine;

public class Spawner : MonoBehaviour
{
    [Header("Grid Spawn Settings")]
    [SerializeField] private GameObject collectiblePrefab;
    [SerializeField] private GameObject fastEnemyPrefab;
    [SerializeField] private GameObject redEnemyPrefab;
    [SerializeField] private float baseSpawnInterval = 1.5f;
    [SerializeField] private int numberOfColumns = 3;
    [SerializeField] private float columnSpacing = 2f;
    [SerializeField] private float spawnY = 5f;

    [Header("Spawn Probabilities")]
    [SerializeField][Range(0f, 1f)] private float fastEnemySpawnChance = 0.3f;

    private float[] columnPositions;
    private float nextSpawnTime;
    private float currentSpawnInterval;
    private float currentStepDuration = 0.3f;

    // Bounce pattern state — reproduit le défilé ordonné style Game & Watch
    private int spawnColIdx = 0;
    private int spawnColDir = 1;

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
            SpawnObject();
            currentSpawnInterval = GameManager.Instance.GetCurrentSpawnInterval();
            currentStepDuration = GameManager.Instance.GetCurrentStepDuration();
            nextSpawnTime = Time.time + currentSpawnInterval;
        }
    }

    private void SpawnObject()
    {
        int spawnCount = GameManager.Instance != null
            ? GameManager.Instance.GetMultiSpawnCount()
            : 1;

        if (spawnCount == 1)
        {
            // Bounce : 0 → 1 → 2 → 1 → 0 → 1 → 2 ... (style Game & Watch)
            SpawnSingleObject(columnPositions[NextColumnBounce()]);
        }
        else if (spawnCount == 2)
        {
            // Les deux extrémités simultanément
            SpawnSingleObject(columnPositions[0]);
            SpawnSingleObject(columnPositions[numberOfColumns - 1]);
        }
        else
        {
            // Toutes les colonnes
            for (int i = 0; i < Mathf.Min(spawnCount, numberOfColumns); i++)
                SpawnSingleObject(columnPositions[i]);
        }
    }

    /// <summary>Returns the next column index following a ping-pong bounce pattern.</summary>
    private int NextColumnBounce()
    {
        int col = spawnColIdx;
        spawnColIdx += spawnColDir;

        if (spawnColIdx >= numberOfColumns)
        {
            spawnColDir = -1;
            spawnColIdx = numberOfColumns - 2;
        }
        else if (spawnColIdx < 0)
        {
            spawnColDir = 1;
            spawnColIdx = 1;
        }

        return Mathf.Clamp(col, 0, numberOfColumns - 1);
    }

    private void SpawnSingleObject(float xPos)
    {
        float fastChance = GameManager.Instance != null
            ? GameManager.Instance.GetFastEnemyChance()
            : fastEnemySpawnChance;

        float redChance = GameManager.Instance != null
            ? GameManager.Instance.GetRedEnemyChance()
            : 0f;

        // Résolution de la priorité : rouge > rapide > collectible
        // On tire un seul dé [0, 1] et on tranche par seuils.
        float roll = Random.value;

        GameObject prefabToSpawn;
        if (roll < redChance && redEnemyPrefab != null)
            prefabToSpawn = redEnemyPrefab;
        else if (roll < redChance + fastChance && fastEnemyPrefab != null)
            prefabToSpawn = fastEnemyPrefab;
        else
            prefabToSpawn = collectiblePrefab;

        if (prefabToSpawn == null)
        {
            Debug.LogWarning("Prefab is not assigned!");
            return;
        }

        Vector3 spawnPosition   = new Vector3(xPos, spawnY, 0f);
        GameObject spawnedObject = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);

        Collectible collectibleScript = spawnedObject.GetComponent<Collectible>();
        if (collectibleScript != null)
        {
            collectibleScript.SetColumn(xPos);
            collectibleScript.SetStepDuration(currentStepDuration);
        }

        FastEnemy fastEnemyScript = spawnedObject.GetComponent<FastEnemy>();
        if (fastEnemyScript != null)
        {
            fastEnemyScript.SetColumn(xPos);
            fastEnemyScript.SetStepDuration(currentStepDuration);
        }

        RedEnemy redEnemyScript = spawnedObject.GetComponent<RedEnemy>();
        if (redEnemyScript != null)
        {
            redEnemyScript.SetColumn(xPos);
            redEnemyScript.SetStepDuration(currentStepDuration);
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
