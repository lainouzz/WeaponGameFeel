using UnityEngine;

public class Spawnbehavior : MonoBehaviour
{
    public Vector3 size = Vector3.one;
    [Range(0f, 1f)]
    public float alpha;

    public GameObject enemyPrefab;
    public GameObject player;
    public int maxEnemies = 10;
    public float spawnInterval = 2f;

    private int currentEnemies = 0;
    private float nextSpawnTime = 0f;

    public static Spawnbehavior Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Optional: spawn initial enemies
    }

    void Update()
    {
        // Optional: spawn enemies over time
        if (Time.time >= nextSpawnTime && currentEnemies < maxEnemies)
        {
            SpawnEnemy();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    /// <summary>
    /// Spawn an enemy on the edge of the spawn zone at a random position
    /// </summary>
    public void SpawnEnemy()
    {
        // Random position within the spawn zone bounds
        float randX = Random.Range(-size.x / 2f, size.x / 2f);
        float randZ = Random.Range(-size.z / 2f, size.z / 2f);

        // Choose a random edge (0 = left, 1 = right, 2 = front, 3 = back)
        int edge = Random.Range(0, 4);
        Vector3 spawnPos = transform.position;

        switch (edge)
        {
            case 0: // Left edge
                spawnPos += new Vector3(-size.x / 2f, 0f, randZ);
                break;
            case 1: // Right edge
                spawnPos += new Vector3(size.x / 2f, 0f, randZ);
                break;
            case 2: // Front edge
                spawnPos += new Vector3(randX, 0f, -size.z / 2f);
                break;
            case 3: // Back edge
                spawnPos += new Vector3(randX, 0f, size.z / 2f);
                break;
        }

        GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        enemy.transform.rotation = Quaternion.LookRotation(player.transform.position - spawnPos);
        currentEnemies++;
        
        enemy.GetComponent<EnemyMovement>().SetTarget(player.GetComponent<ITarget>());
        //Debug.Log($"[SpawnBehavior] Spawned enemy at {spawnPos} (edge: {edge})");
    }

    public void SpawnEnemyAnywhere()
    {
        float randX = Random.Range(-size.x / 2f, size.x / 2f);
        float randZ = Random.Range(-size.z / 2f, size.z / 2f);

        Vector3 spawnPos = transform.position + new Vector3(randX, 0f, randZ);
        GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        
        //Debug.Log($"[SpawnBehavior] Spawned enemy at {spawnPos}");
    }

    public void OnEnemyDeath()
    {
        currentEnemies--;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0f, 0f, alpha);
        Gizmos.DrawCube(transform.position, size);

        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(transform.position, size);
    }
}
