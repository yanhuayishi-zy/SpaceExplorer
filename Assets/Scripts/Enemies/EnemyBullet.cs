using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public int damage = 1;
    public float lifeTime = 5f;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var ph = other.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(damage);
            }
            Destroy(gameObject);
            return;
        }

        // 离开屏幕自动清理
        if (transform.position.y < -7f || transform.position.y > 8f ||
            transform.position.x < -8f || transform.position.x > 8f)
        {
            Destroy(gameObject);
        }
    }
}
