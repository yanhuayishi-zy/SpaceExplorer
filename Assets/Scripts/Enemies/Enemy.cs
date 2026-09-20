using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("敌人属性")]
    public int health = 1;
    public int damage = 1;
    public int scoreValue = 100;
    public float moveSpeed = 5f;
    
    [Header("攻击设置")]
    public bool canShoot = false;
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 1f;
    
    [Header("移动模式")]
    public MovementPattern movementPattern = MovementPattern.Straight;
    public float amplitude = 2f;
    public float frequency = 1f;
    
    private float nextFireTime = 0f;
    private float startX;
    private float startTime;
    
    public enum MovementPattern
    {
        Straight,
        Sine,
        Zigzag
    }
    
    void Start()
    {
        startX = transform.position.x;
        startTime = Time.time;
        startHealth = health;
    }
    
    void Update()
    {
        // 移动
        Move();
        
        // 射击
        if (canShoot && Time.time > nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
        
        // 检查是否离开屏幕
        if (transform.position.y < -6f)
        {
            Destroy(gameObject);
        }
    }
    
    void Move()
    {
        switch (movementPattern)
        {
            case MovementPattern.Straight:
                transform.Translate(Vector2.down * moveSpeed * Time.deltaTime);
                break;
            case MovementPattern.Sine:
                float newX = startX + Mathf.Sin((Time.time - startTime) * frequency) * amplitude;
                newX = Mathf.Clamp(newX, -5.2f, 5.2f);
                transform.position = new Vector3(newX, transform.position.y - moveSpeed * Time.deltaTime, 0);
                break;
            case MovementPattern.Zigzag:
                float zigzagX = startX + Mathf.PingPong(
                    (Time.time - startTime) * frequency + amplitude, amplitude * 2) - amplitude;
                zigzagX = Mathf.Clamp(zigzagX, -5.2f, 5.2f);
                transform.position = new Vector3(zigzagX, transform.position.y - moveSpeed * Time.deltaTime, 0);
                break;
        }
    }
    
    int startHealth;
    bool enraged;
    float bossDamageRemainder;

    void Shoot()
    {
        if (bulletPrefab == null || firePoint == null) return;

        // 中Boss/大Boss 血量过半后狂暴
        if (startHealth >= 8 && health <= startHealth / 2)
        {
            if (!enraged)
            {
                enraged = true;
                fireRate = Mathf.Max(0.55f, fireRate * 0.55f);
                moveSpeed *= 1.25f;
            }
            RadialBurst(8);
            return;
        }

        FireOne(firePoint.position, Vector2.down * 3.5f);
        // 大 Boss 额外两发斜射
        if (startHealth >= 15)
        {
            FireOne(firePoint.position + new Vector3(-0.35f, 0f, 0f), new Vector2(-1.2f, -3f));
            FireOne(firePoint.position + new Vector3(0.35f, 0f, 0f), new Vector2(1.2f, -3f));
        }
    }

    void FireOne(Vector3 pos, Vector2 vel)
    {
        GameObject bullet = Instantiate(bulletPrefab, pos, Quaternion.identity);
        bullet.SetActive(true);
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = vel;
        var eb = bullet.GetComponent<EnemyBullet>();
        if (eb == null) eb = bullet.AddComponent<EnemyBullet>();
        eb.damage = Mathf.Max(1, damage);
        Destroy(bullet, 5f);
    }

    void RadialBurst(int count)
    {
        for (int i = 0; i < count; i++)
        {
            float ang = i * (360f / count) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            FireOne(transform.position, dir * 2.8f);
        }
    }
    
    public void TakeDamage(int damageAmount)
    {
        bool critical = damageAmount > 0 && Random.value < GearMeta.CritChance;
        if (critical)
            damageAmount = Mathf.Max(1, Mathf.RoundToInt(damageAmount * GearMeta.CritMul));

        // Boss 先结算减伤和易伤，再交给锁血阶段，避免大伤害跨过阶段。
        var brk = GetComponent<BossBreakPhase>();
        if (startHealth >= 80)
        {
            // 保留小数余量，让低伤害武器也能正确体现 20% 减伤和机体差异。
            float scaled = damageAmount * GameBalance.BossDamageTakenMul + bossDamageRemainder;
            damageAmount = Mathf.Max(1, Mathf.FloorToInt(scaled));
            bossDamageRemainder = Mathf.Max(0f, scaled - damageAmount);
        }
        if (brk != null && brk.IsVulnerable)
            damageAmount = Mathf.Max(1, Mathf.RoundToInt(damageAmount * BossBreakPhase.VulnerableMul));
        if (brk != null) damageAmount = brk.ModifyDamage(damageAmount);

        health -= damageAmount;
        AutoAudio.PlayHit();

        // 受击反馈：飘字 + 爆点 + 白闪
        int shown = Mathf.Max(0, damageAmount);
        if (shown > 0)
        {
            Color dmgC = critical ? new Color(1f, 0.35f, 0.9f)
                : shown >= 4 ? new Color(1f, 0.55f, 0.2f) : new Color(1f, 0.95f, 0.55f);
            GameFx.DamageNumber(transform.position, shown, dmgC,
                critical || shown >= 6 || (brk != null && brk.IsVulnerable));
        }
        GameFx.HitPop(transform.position, new Color(1f, 0.9f, 0.5f, 0.9f));
        StartCoroutine(HitFlashCo(0.06f));

        var bar = GetComponent<EnemyHealthBar>();
        if (bar != null) bar.ApplyDamage(damageAmount);
        if (health <= 0)
        {
            Die();
        }
    }

    System.Collections.IEnumerator HitFlashCo(float t)
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) yield break;
        Color old = sr.color;
        sr.color = new Color(1f, 1f, 1f, 1f);
        yield return new WaitForSeconds(t);
        if (sr != null && health > 0) sr.color = old;
    }

    void Die()
    {
        int gained = scoreValue;
        bool boss = startHealth >= 80 || scoreValue >= 800;
        if (GameManager.Instance != null)
        {
            if (ComboBombSystem.Instance != null)
            {
                gained = ComboBombSystem.Instance.RegisterKill(scoreValue);
            }
            else
            {
                gained = scoreValue;
            }
            // 无尽压分，避免分数膨胀过快
            if (!ZodiacLevels.IsCampaign)
                gained = GameBalance.EndlessScore(gained);
            GameManager.Instance.AddScore(gained);
            GameManager.Instance.AddKill();
            ShipMeta.AddCoins(GearMeta.CoinGain(GameBalance.KillCoins(scoreValue)));
            ShipShopUI.RefreshCoins();
        }

        // 技能充能
        var player = GameObject.Find("Player");
        if (player != null)
        {
            var ps = player.GetComponent<PlayerShooting>();
            if (ps != null) ps.NotifyKillCharge();
        }

        if (boss) Achievements.NotifyBossKilled();

        // 死亡爆闪
        var srs = GetComponent<SpriteRenderer>();
        Color tint = srs != null && srs.color.a > 0.05f
            ? new Color(srs.color.r, srs.color.g, srs.color.b, 1f)
            : new Color(1f, 0.55f, 0.3f);
        float size = boss ? 2.2f : Mathf.Max(0.5f, transform.localScale.x);
        GameFx.EnemyDeath(transform.position, size, tint, scoreValue);

        // 小兵概率掉道具（Boss 必掉）
        if (boss)
        {
            if (ZodiacLevels.IsCampaign)
            {
                ShipMeta.AddCoins(GearMeta.CoinGain(GameBalance.CampaignBossBounty));
                ShipShopUI.RefreshCoins();
            }
            else
            {
                DropPowerUp(true);
            }
        }
        else if (Random.value < 0.12f + GearMeta.DropBonus)
        {
            DropPowerUp(false);
        }

        Destroy(gameObject);
    }

    void DropPowerUp(bool premium)
    {
        if (AutoHudPowerUps.Instance == null) return;
        AutoHudPowerUps.Instance.SpawnAt(transform.position, premium);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
            // Boss 碰撞只造成接触伤害，不能因对象被销毁而被流程误判为击杀。
            if (GetComponent<ZodiacBossAI>() == null)
            {
                GameFx.EnemyDeath(transform.position, Mathf.Max(0.6f, transform.localScale.x),
                    new Color(1f, 0.4f, 0.3f), 0);
                Destroy(gameObject);
            }
            else
            {
                GameFx.HitPop(transform.position, new Color(1f, 0.4f, 0.3f, 0.9f));
            }
        }
    }
}
