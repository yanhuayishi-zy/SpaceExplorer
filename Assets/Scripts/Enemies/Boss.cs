using UnityEngine;
using System.Collections;

public class Boss : MonoBehaviour
{
    [Header("Boss属性")]
    public int maxHealth = 100;
    public int currentHealth;
    public int damage = 2;
    public int scoreValue = 1000;
    
    [Header("移动设置")]
    public float moveSpeed = 3f;
    public float horizontalSpeed = 2f;
    public float minX = -6f;
    public float maxX = 6f;
    
    [Header("攻击设置")]
    public GameObject bulletPrefab;
    public Transform[] firePoints;
    public float fireRate = 1.8f;
    public float bulletSpeed = 4f;
    
    [Header("阶段设置")]
    public int currentPhase = 1;
    public int maxPhases = 3;
    public float phaseTransitionTime = 2f;
    
    private bool isAttacking = false;
    private bool isInvincible = false;
    private float nextFireTime = 0f;
    
    void Start()
    {
        currentHealth = maxHealth;
        StartCoroutine(MovementPattern());
    }
    
    void Update()
    {
        if (!isInvincible && Time.time > nextFireTime)
        {
            StartCoroutine(AttackPattern());
            nextFireTime = Time.time + fireRate;
        }
    }
    
    IEnumerator MovementPattern()
    {
        while (true)
        {
            // 水平移动
            float targetX = Random.Range(minX, maxX);
            Vector3 targetPosition = new Vector3(targetX, transform.position.y, 0);
            
            while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, horizontalSpeed * Time.deltaTime);
                yield return null;
            }
            
            yield return new WaitForSeconds(1f);
        }
    }
    
    IEnumerator AttackPattern()
    {
        if (isAttacking) yield break;
        
        isAttacking = true;
        
        switch (currentPhase)
        {
            case 1:
                yield return StartCoroutine(Phase1Attack());
                break;
            case 2:
                yield return StartCoroutine(Phase2Attack());
                break;
            case 3:
                yield return StartCoroutine(Phase3Attack());
                break;
        }
        
        isAttacking = false;
    }
    
    IEnumerator Phase1Attack()
    {
        // 阶段1：直线射击
        foreach (Transform firePoint in firePoints)
        {
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
            rb.velocity = Vector2.down * bulletSpeed;
            Destroy(bullet, 5f);
        }
        
        yield return new WaitForSeconds(0.5f);
    }
    
    IEnumerator Phase2Attack()
    {
        // 阶段2：扇形射击
        float spreadAngle = 30f;
        int bulletCount = 5;
        
        for (int i = 0; i < bulletCount; i++)
        {
            float angle = -spreadAngle / 2 + (spreadAngle / (bulletCount - 1)) * i;
            Vector2 direction = Quaternion.Euler(0, 0, angle) * Vector2.down;
            
            foreach (Transform firePoint in firePoints)
            {
                GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
                Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
                rb.velocity = direction * bulletSpeed;
                Destroy(bullet, 5f);
            }
            
            yield return new WaitForSeconds(0.1f);
        }
        
        yield return new WaitForSeconds(0.5f);
    }
    
    IEnumerator Phase3Attack()
    {
        // 阶段3：螺旋射击
        float spiralSpeed = 2f;
        float duration = 2f;
        float timer = 0f;
        
        while (timer < duration)
        {
            float angle = timer * spiralSpeed * 360f;
            Vector2 direction = Quaternion.Euler(0, 0, angle) * Vector2.down;
            
            foreach (Transform firePoint in firePoints)
            {
                GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
                Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
                rb.velocity = direction * bulletSpeed;
                Destroy(bullet, 5f);
            }
            
            timer += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }
        
        yield return new WaitForSeconds(0.5f);
    }
    
    public void TakeDamage(int damage)
    {
        if (isInvincible) return;
        
        currentHealth -= damage;
        
        // 检查阶段转换
        int healthPercentage = (currentHealth * 100) / maxHealth;
        int newPhase = 1;
        
        if (healthPercentage <= 33)
            newPhase = 3;
        else if (healthPercentage <= 66)
            newPhase = 2;
        
        if (newPhase > currentPhase)
        {
            currentPhase = newPhase;
            StartCoroutine(PhaseTransition());
        }
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    IEnumerator PhaseTransition()
    {
        isInvincible = true;
        
        // 闪烁效果
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        for (int i = 0; i < 5; i++)
        {
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.2f);
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.2f);
        }
        
        isInvincible = false;
    }
    
    void Die()
    {
        // 添加分数
        GameManager.Instance.AddScore(scoreValue);
        
        // 死亡效果
        // Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        
        // 游戏胜利
        GameManager.Instance.NextLevel();
        
        Destroy(gameObject);
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
        }
    }
}
