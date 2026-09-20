using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// 运行时自动创建 GameManager + 三种敌人 + 刷怪，无需在编辑器里拖引用
public class AutoEnemyWaves : MonoBehaviour
{
    public static int CurrentWave = 0;
    public static int TotalWaves = 999;
    static Sprite smallSprite, mediumSprite, largeSprite, bulletSprite;
    static readonly List<GameObject> alive = new List<GameObject>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        // 星座流程接管后不再启动旧刷怪
        if (Object.FindObjectOfType<ZodiacGameFlow>() != null) return;
        if (Object.FindObjectOfType<AutoEnemyWaves>() != null) return;

        var go = new GameObject("AutoEnemyWaves");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<AutoEnemyWaves>();
    }

    void Start()
    {
        if (GameManager.Instance == null)
        {
            var gm = new GameObject("GameManager");
            gm.AddComponent<GameManager>();
        }

        var player = GameObject.Find("Player");
        if (player != null)
        {
            if (player.tag != "Player") player.tag = "Player";
            var col = player.GetComponent<BoxCollider2D>();
            if (col == null) col = player.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            if (col.size.x < 0.2f || col.size.y < 0.2f)
            {
                col.size = new Vector2(0.7f, 0.7f);
            }
            if (player.GetComponent<PlayerShooting>() == null)
            {
                player.AddComponent<PlayerShooting>();
            }
        }

        smallSprite = Resources.Load<Sprite>("EnemySmall");
        mediumSprite = Resources.Load<Sprite>("EnemyMedium");
        largeSprite = Resources.Load<Sprite>("EnemyLarge");
        bulletSprite = Resources.Load<Sprite>("Bullet");

        if (Object.FindObjectOfType<ZodiacGameFlow>() != null)
        {
            enabled = false;
            return;
        }
        StartCoroutine(RunWaves());
    }

    IEnumerator RunWaves()
    {
        // 等开场动画结束
        float guard = 0f;
        while (!AutoIntroOutro.IntroDone && guard < 12f)
        {
            guard += Time.deltaTime;
            yield return null;
        }
        yield return new WaitForSeconds(0.4f);

        // 无尽模式：循环刷怪，难度随波次提升
        int cycle = 0;
        while (true)
        {
            if (GameManager.Instance != null && GameManager.Instance.isGameOver) yield break;

            cycle++;
            CurrentWave = cycle;
            float diff = 1f + (cycle - 1) * 0.18f;
            int smallCount = Mathf.Min(4 + cycle, 14);
            float smallSpeed = Mathf.Min(1.5f + cycle * 0.08f, 3.6f);
            int smallHp = 2 + (cycle / 3);

            yield return SpawnGroup(
                cycle == 1 ? "第1波：小型敌机" : "第" + cycle + "波",
                smallSprite, count: smallCount, hp: smallHp, speed: smallSpeed,
                scale: 0.7f, score: 50, interval: Mathf.Max(0.35f, 1.0f - cycle * 0.05f));
            yield return WaitClear(Mathf.Min(8f + cycle, 20f));
            if (GameManager.Instance != null && GameManager.Instance.isGameOver) yield break;

            // 中Boss：第2波起每3波一次
            if (cycle >= 2 && cycle % 3 == 2)
            {
                int midHp = 8 + cycle * 2;
                yield return SpawnGroup("中Boss 出现！", mediumSprite, count: 1, hp: midHp,
                    speed: Mathf.Min(0.95f + cycle * 0.04f, 1.8f), scale: 1.35f, score: 200 + cycle * 40,
                    canShoot: true, interval: 0f);
                yield return WaitClear(Mathf.Min(14f + cycle, 28f));
            }
            else if (cycle >= 2)
            {
                yield return SpawnGroup(null, mediumSprite, count: Mathf.Min(2 + cycle / 2, 6),
                    hp: 3 + cycle / 2, speed: 1.1f * diff * 0.7f, scale: 1f,
                    score: 120, canShoot: cycle >= 3, interval: 0.9f);
                yield return WaitClear(Mathf.Min(10f + cycle, 22f));
            }

            if (GameManager.Instance != null && GameManager.Instance.isGameOver) yield break;

            // 大Boss：第5波起每5波一次
            if (cycle >= 5 && cycle % 5 == 0)
            {
                int bossHp = 18 + cycle * 3;
                yield return SpawnGroup("Boss 来袭！", largeSprite, count: 1, hp: bossHp,
                    speed: Mathf.Min(0.55f + cycle * 0.03f, 1.2f), scale: 1.7f,
                    score: 800 + cycle * 80, canShoot: true, interval: 0f);
                yield return WaitClear(Mathf.Min(25f + cycle, 45f));
            }
        }
    }

    IEnumerator SpawnGroup(string banner, Sprite sprite, int count, int hp, float speed, float scale, int score, bool canShoot = false, float interval = 0.8f)
    {
        if (!string.IsNullOrEmpty(banner))
        {
            Debug.Log(banner);
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowWaveInfo(banner);
            }
        }

        for (int i = 0; i < count; i++)
        {
            SpawnEnemy(sprite, hp, speed, scale, score, canShoot);
            if (interval > 0f && i < count - 1)
            {
                yield return new WaitForSeconds(interval);
            }
        }
    }

    void SpawnEnemy(Sprite sprite, int hp, float speed, float scale, int score, bool canShoot)
    {
        var go = new GameObject("Enemy_Auto");
        go.tag = "Enemy";
        go.transform.position = new Vector3(Random.Range(-4.5f, 4.5f), 6.5f, 0f);
        go.transform.localScale = Vector3.one * scale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 5;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.85f, 0.85f);

        var enemy = go.AddComponent<Enemy>();
        enemy.health = hp;
        enemy.damage = 16;
        enemy.scoreValue = score;
        enemy.moveSpeed = speed;
        enemy.canShoot = canShoot;
        enemy.movementPattern = hp >= 3 ? Enemy.MovementPattern.Sine : Enemy.MovementPattern.Straight;
        enemy.amplitude = 1.5f;
        enemy.frequency = 2f;

        if (canShoot)
        {
            var bullet = CreateEnemyBulletTemplate();
            enemy.bulletPrefab = bullet;

            var fp = new GameObject("FirePoint");
            fp.transform.SetParent(go.transform, false);
            fp.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            enemy.firePoint = fp.transform;
            enemy.fireRate = hp >= 10 ? 1.6f : 2.8f;
        }

        var hpBar = go.AddComponent<EnemyHealthBar>();
        hpBar.Setup(hp);
        if (scale >= 1.4f)
        {
            hpBar.offset = new Vector2(0f, 1.1f);
            hpBar.size = new Vector2(1.4f, 0.16f);
        }

        alive.Add(go);
    }

    static GameObject bulletTemplate;

    GameObject CreateEnemyBulletTemplate()
    {
        if (bulletTemplate != null) return bulletTemplate;

        bulletTemplate = new GameObject("EnemyBulletTemplate");
        bulletTemplate.SetActive(false);
        Object.DontDestroyOnLoad(bulletTemplate);

        var sr = bulletTemplate.AddComponent<SpriteRenderer>();
        sr.sprite = bulletSprite != null ? bulletSprite : Resources.Load<Sprite>("Bullet");
        sr.color = new Color(1f, 0.35f, 0.35f, 1f);
        bulletTemplate.transform.localScale = Vector3.one * 0.35f;

        var rb = bulletTemplate.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        var col = bulletTemplate.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.5f, 0.5f);
        bulletTemplate.AddComponent<EnemyBullet>();

        return bulletTemplate;
    }

    IEnumerator WaitClear(float maxWait)
    {
        float t = 0f;
        while (t < maxWait)
        {
            alive.RemoveAll(x => x == null);
            if (alive.Count == 0 && GameObject.FindGameObjectsWithTag("Enemy").Length == 0)
            {
                yield break;
            }
            t += Time.deltaTime;
            yield return null;
        }
    }
}
