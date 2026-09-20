using UnityEngine;

public class PowerUpSpawner : MonoBehaviour
{
    [Header("生成设置")]
    public GameObject[] powerUpPrefabs;
    public float spawnRate = 1f;
    public float spawnInterval = 10f;
    
    [Header("生成范围")]
    public float minX = -7f;
    public float maxX = 7f;
    public float spawnY = 5f;
    
    private float nextSpawnTime = 0f;
    
    void Update()
    {
        if (Time.time > nextSpawnTime)
        {
            SpawnPowerUp();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }
    
    void SpawnPowerUp()
    {
        if (powerUpPrefabs.Length == 0) return;
        
        // 随机选择道具
        int randomIndex = Random.Range(0, powerUpPrefabs.Length);
        GameObject powerUpPrefab = powerUpPrefabs[randomIndex];
        
        // 随机位置
        float randomX = Random.Range(minX, maxX);
        Vector3 spawnPosition = new Vector3(randomX, spawnY, 0);
        
        // 生成道具
        Instantiate(powerUpPrefab, spawnPosition, Quaternion.identity);
    }
}
