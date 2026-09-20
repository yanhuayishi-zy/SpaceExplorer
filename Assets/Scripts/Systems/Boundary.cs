using UnityEngine;

public class Boundary : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        // 如果是子弹，销毁
        if (other.CompareTag("Bullet"))
        {
            Destroy(other.gameObject);
        }
        
        // 如果是敌人，销毁
        if (other.CompareTag("Enemy"))
        {
            Destroy(other.gameObject);
        }
        
        // 如果是道具，销毁
        if (other.CompareTag("PowerUp"))
        {
            Destroy(other.gameObject);
        }
    }
}
