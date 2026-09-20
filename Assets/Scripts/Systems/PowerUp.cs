using UnityEngine;

public class PowerUp : MonoBehaviour
{
    [Header("道具设置")]
    public PowerUpType powerUpType;
    public float duration = 6f;
    public int value = 1;

    [Header("移动设置")]
    public float moveSpeed = 1.6f;

    public enum PowerUpType
    {
        Health,
        Speed,
        Damage,
        Shield,
        Score,
        Spread
    }

    float bobPhase;
    Vector3 baseScale;

    void Start()
    {
        baseScale = transform.localScale;
        bobPhase = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        transform.Translate(Vector2.down * moveSpeed * Time.deltaTime, Space.World);
        // 轻微脉动，更好识别
        bobPhase += Time.deltaTime * 4f;
        float s = 1f + 0.08f * Mathf.Sin(bobPhase);
        transform.localScale = baseScale * s;
        float despawnY = MobileTuning.Active ? MobileTuning.Bottom(0.8f) : -6.5f;
        if (transform.position.y < despawnY)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        AutoAudio.PlayPowerUp();
        Color c = ColorForType();
        GameFx.Pickup(transform.position, c);
        ApplyPowerUp(other.gameObject);
        Destroy(gameObject);
    }

    Color ColorForType()
    {
        switch (powerUpType)
        {
            case PowerUpType.Health: return new Color(0.4f, 1f, 0.5f);
            case PowerUpType.Speed: return new Color(0.5f, 1f, 0.85f);
            case PowerUpType.Damage: return new Color(1f, 0.45f, 0.35f);
            case PowerUpType.Shield: return new Color(0.5f, 0.85f, 1f);
            case PowerUpType.Score: return new Color(1f, 0.9f, 0.35f);
            case PowerUpType.Spread: return new Color(0.9f, 0.7f, 1f);
            default: return Color.white;
        }
    }

    void ApplyPowerUp(GameObject p)
    {
        switch (powerUpType)
        {
            case PowerUpType.Health:
            {
                var health = p.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    health.Heal(15 * Mathf.Max(1, value));
                }
                break;
            }
            case PowerUpType.Speed:
            {
                var movement = p.GetComponent<PlayerMovement>();
                if (movement != null)
                {
                    var timed = p.GetComponent<TimedPowerUpEffects>();
                    if (timed == null) timed = p.AddComponent<TimedPowerUpEffects>();
                    timed.AddSpeed(value, duration);
                }
                break;
            }
            case PowerUpType.Damage:
            {
                var shooting = p.GetComponent<PlayerShooting>();
                if (shooting != null)
                {
                    var timed = p.GetComponent<TimedPowerUpEffects>();
                    if (timed == null) timed = p.AddComponent<TimedPowerUpEffects>();
                    timed.AddDamage(value, duration);
                }
                break;
            }
            case PowerUpType.Shield:
            {
                var health = p.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    health.ActivateShield(duration);
                }
                break;
            }
            case PowerUpType.Score:
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.AddScore(value * 100);
                }
                ShipMeta.AddCoins(GearMeta.CoinGain(value * 20));
                ShipShopUI.RefreshCoins();
                break;
            }
            case PowerUpType.Spread:
            {
                var shooting = p.GetComponent<PlayerShooting>();
                if (shooting != null)
                {
                    shooting.ActivateSpread(duration > 0 ? duration : 8f);
                }
                break;
            }
        }
    }

}

/// 临时道具计时器必须挂在玩家上，道具拾取物销毁后效果才能正常结束。
public class TimedPowerUpEffects : MonoBehaviour
{
    public float ActiveSpeedBonus { get; private set; }

    public void AddSpeed(float amount, float duration)
    {
        var movement = GetComponent<PlayerMovement>();
        if (movement == null) return;
        ActiveSpeedBonus += amount;
        movement.moveSpeed += amount;
        StartCoroutine(RemoveSpeed(amount, duration));
    }

    System.Collections.IEnumerator RemoveSpeed(float amount, float duration)
    {
        yield return new WaitForSeconds(Mathf.Max(0.1f, duration));
        ActiveSpeedBonus = Mathf.Max(0f, ActiveSpeedBonus - amount);
        var movement = GetComponent<PlayerMovement>();
        if (movement != null) movement.moveSpeed = Mathf.Max(4f, movement.moveSpeed - amount);
    }

    public void AddDamage(int amount, float duration)
    {
        var shooting = GetComponent<PlayerShooting>();
        if (shooting == null) return;
        shooting.damage += amount;
        StartCoroutine(RemoveDamage(amount, duration));
    }

    System.Collections.IEnumerator RemoveDamage(int amount, float duration)
    {
        yield return new WaitForSeconds(Mathf.Max(0.1f, duration));
        var shooting = GetComponent<PlayerShooting>();
        if (shooting != null) shooting.damage = Mathf.Max(1, shooting.damage - amount);
    }
}
