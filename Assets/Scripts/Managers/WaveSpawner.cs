using UnityEngine;
using System.Collections;

[System.Serializable]
public class Wave
{
    public string waveName;
    public EnemyWave[] enemyWaves;
    public float timeBetweenWaves = 5f;
}

[System.Serializable]
public class EnemyWave
{
    public GameObject enemyPrefab;
    public int count;
    public float spawnInterval = 0.5f;
    public float startDelay = 0f;
}

public class WaveSpawner : MonoBehaviour
{
    [Header("波次设置")]
    public Wave[] waves;
    public Transform[] spawnPoints;
    public float timeBetweenWaves = 5f;
    
    private int currentWaveIndex = 0;
    
    
    void Start()
    {
        StartCoroutine(SpawnWaves());
    }
    
    IEnumerator SpawnWaves()
    {
        for (int i = 0; i < waves.Length; i++)
        {
            currentWaveIndex = i;
            Wave wave = waves[i];
            
            // 显示波次信息
            UIManager.Instance.ShowWaveInfo(wave.waveName);
            
            // 等待波次开始
            yield return new WaitForSeconds(timeBetweenWaves);
            
            // 生成敌人
            yield return StartCoroutine(SpawnEnemyWave(wave));
            
            // 等待所有敌人被消灭
            yield return new WaitUntil(() => GameObject.FindGameObjectsWithTag("Enemy").Length == 0);
        }
        
        // 所有波次完成
        GameManager.Instance.NextLevel();
    }
    
    IEnumerator SpawnEnemyWave(Wave wave)
    {
        for (int i = 0; i < wave.enemyWaves.Length; i++)
        {
            EnemyWave enemyWave = wave.enemyWaves[i];
            
            // 等待开始延迟
            yield return new WaitForSeconds(enemyWave.startDelay);
            
            // 生成敌人
            for (int j = 0; j < enemyWave.count; j++)
            {
                SpawnEnemy(enemyWave.enemyPrefab);
                yield return new WaitForSeconds(enemyWave.spawnInterval);
            }
        }
    }
    
    void SpawnEnemy(GameObject enemyPrefab)
    {
        // 随机选择生成点
        int spawnPointIndex = Random.Range(0, spawnPoints.Length);
        Transform spawnPoint = spawnPoints[spawnPointIndex];
        
        // 生成敌人
        Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
    }
}
