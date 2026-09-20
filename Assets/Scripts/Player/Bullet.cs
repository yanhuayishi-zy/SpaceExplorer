using UnityEngine;

public class Bullet : MonoBehaviour
{
    public int damage = 1;
    public GameObject hitEffectPrefab;
    
    void OnTriggerEnter2D(Collider2D other)
    {
        // 击中敌人
        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                GameFx.HitPop(transform.position, new Color(1f, 0.95f, 0.6f, 0.9f));
            }

            // 击中效果
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            }

            Destroy(gameObject);
        }

        // 击中敌方子弹：打掉
        var eb = other.GetComponent<EnemyBullet>();
        if (eb != null)
        {
            GameFx.BulletClash(other.transform.position);
            Destroy(other.gameObject);
            Destroy(gameObject);
            return;
        }

        // 击中边界
        if (other.GetComponent<Boundary>() != null)
        {
            Destroy(gameObject);
        }
    }
}
