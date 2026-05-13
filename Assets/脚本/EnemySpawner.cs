using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn")]
    public GameObject enemyPrefab;
    public float spawnInterval = 0.55f;
    public int enemiesPerWave = 2;
    public float extraEnemyChance = 0.35f;
    public float spawnZ = 600f;
    public float minX = -250f;
    public float maxX = 150f;
    public float minSpacing = 45f;

    [Header("Boss")]
    public GameObject bossPrefab;
    public int bossTriggerScore = 50;
    public int bossScoreInterval = 200;
    public Vector3 bossSpawnPosition = new Vector3(-50f, 0f, 520f);
    public float bossScaleMultiplier = 12f;
    public float bossHorizontalSpacing = 140f;
    public int maxBossCount = 3;

    private float nextSpawnTime = 0f;
    private int nextBossScore;
    private int activeBossCount;

    void Start()
    {
        nextBossScore = Mathf.Max(1, bossTriggerScore);
    }

    void Update()
    {
        if (ScoreManager.score >= nextBossScore)
        {
            SpawnBossWave(GetBossCountForScore(nextBossScore));
            AdvanceBossScore();
        }

        if (Time.time >= nextSpawnTime)
        {
            SpawnWave();
            nextSpawnTime = Time.time + GetCurrentSpawnInterval();
        }
    }

    void SpawnWave()
    {
        if (enemyPrefab == null)
            return;

        int count = Mathf.Max(1, enemiesPerWave + DifficultyManager.EnemyWaveBonus);
        float bonusChance = Mathf.Clamp01(extraEnemyChance + DifficultyManager.ExtraEnemyChanceBonus);
        if (Random.value < bonusChance)
        {
            count++;
        }

        float lastX = float.NaN;
        for (int i = 0; i < count; i++)
        {
            float x = PickSpawnX(lastX);
            lastX = x;

            float zOffset = Random.Range(-35f, 35f);
            Vector3 spawnPos = new Vector3(x, 0f, spawnZ + zOffset);
            Instantiate(enemyPrefab, spawnPos, Quaternion.Euler(0, 180, 0));
        }
    }

    float GetCurrentSpawnInterval()
    {
        return Mathf.Max(0.18f, spawnInterval * DifficultyManager.SpawnIntervalMultiplier);
    }

    float PickSpawnX(float lastX)
    {
        for (int attempt = 0; attempt < 8; attempt++)
        {
            float x = Random.Range(minX, maxX);
            if (float.IsNaN(lastX) || Mathf.Abs(x - lastX) >= minSpacing)
            {
                return x;
            }
        }

        return Random.Range(minX, maxX);
    }

    int GetBossCountForScore(int scoreThreshold)
    {
        int interval = Mathf.Max(1, bossScoreInterval);
        if (scoreThreshold < interval)
            return 1;

        return Mathf.Max(2, scoreThreshold / interval + 1);
    }

    void AdvanceBossScore()
    {
        int interval = Mathf.Max(1, bossScoreInterval);
        if (nextBossScore < interval)
        {
            nextBossScore = interval;
        }
        else
        {
            nextBossScore += interval;
        }
    }

    void SpawnBossWave(int bossCount)
    {
        if (bossPrefab == null)
            return;

        int count = Mathf.Clamp(bossCount, 1, Mathf.Max(1, maxBossCount));
        float overflowHealthMultiplier = DifficultyManager.GetBossOverflowHealthMultiplier(bossCount, maxBossCount);
        float centerIndex = (count - 1) * 0.5f;

        for (int i = 0; i < count; i++)
        {
            Vector3 position = bossSpawnPosition + Vector3.right * ((i - centerIndex) * bossHorizontalSpacing);
            GameObject bossObject = Instantiate(bossPrefab, position, Quaternion.Euler(0f, 180f, 0f));
            bossObject.transform.localScale *= Mathf.Max(1f, bossScaleMultiplier);

            BossController boss = bossObject.GetComponent<BossController>();
            if (boss == null)
            {
                boss = bossObject.AddComponent<BossController>();
            }

            activeBossCount++;
            boss.extraHealthMultiplier = overflowHealthMultiplier;
            boss.Initialize(this, GetEnemyBulletPrefab(), activeBossCount - 1);
        }
    }

    GameObject GetEnemyBulletPrefab()
    {
        if (enemyPrefab == null)
            return null;

        Enemy enemy = enemyPrefab.GetComponentInChildren<Enemy>();
        return enemy != null ? enemy.bulletPrefab : null;
    }

    public void OnBossDefeated()
    {
        activeBossCount = Mathf.Max(0, activeBossCount - 1);
    }
}
