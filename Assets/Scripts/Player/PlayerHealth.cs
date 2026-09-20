using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("生命值设置")]
    public int maxHealth = 100;
    public int currentHealth;
    public float invincibilityDuration = GameBalance.InvulnAfterHit;
    public float flashInterval = 0.1f;

    /// 机体基础 HP（ShipMeta.maxHp）换算到百分制血条
    public const int HpScale = GameBalance.BaseHpScale; // 5 → 100
    int gearBonusHp;

    /// 统一百分制血量：机体基础 + 装备加成
    public void ApplyScaledVitals(int shipMaxHp)
    {
        gearBonusHp = 0;
        int baseHp = Mathf.Max(1, shipMaxHp) * HpScale;
        maxHealth = baseHp;
        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    public void AddMaxHealth(int amount)
    {
        gearBonusHp += amount;
        maxHealth += amount;
        UpdateHealthUI();
    }

    public void ResetEndlessMods()
    {
        invincibilityDuration = GameBalance.InvulnAfterHit;
    }

    [Header("UI引用")]
    Image healthBarFill;
    Text healthBarText;
    public GameObject gameOverPanel;

    bool isInvincible;
    float invincibilityTimer;
    SpriteRenderer spriteRenderer;
    static int uiCreateCount;

    void Start()
    {
        ShipMeta.ApplyToPlayer(gameObject);
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        var col = GetComponent<BoxCollider2D>();
        if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        if (col.size.x < 0.2f || col.size.y < 0.2f) col.size = new Vector2(0.7f, 0.7f);
        if (gameObject.tag != "Player") gameObject.tag = "Player";

        if (healthBarFill == null || healthBarFill.Equals(null))
        {
            CreateHealthBar();
        }
        UpdateHealthUI();
    }

    /// 条状血条：左上角，底条 + 填充
    void CreateHealthBar()
    {
        uiCreateCount++;
        var canvasGo = new GameObject("HealthCanvas_" + uiCreateCount);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // 底条
        var bgGo = new GameObject("HealthBarBG");
        bgGo.transform.SetParent(canvasGo.transform, false);
        var bg = bgGo.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.06f, 0.10f, 0.88f);
        bg.raycastTarget = false;
        var bgRt = bg.rectTransform;
        bgRt.anchorMin = new Vector2(0f, 1f);
        bgRt.anchorMax = new Vector2(0f, 1f);
        bgRt.pivot = new Vector2(0f, 1f);
        bgRt.anchoredPosition = new Vector2(20f, -88f);
        bgRt.sizeDelta = new Vector2(320f, 36f);

        // 边框
        var frame = new GameObject("Frame");
        frame.transform.SetParent(bgGo.transform, false);
        var fimg = frame.AddComponent<Image>();
        fimg.color = new Color(0.35f, 0.45f, 0.6f, 0.7f);
        fimg.raycastTarget = false;
        var frt = fimg.rectTransform;
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = new Vector2(-2f, -2f);
        frt.offsetMax = new Vector2(2f, 2f);
        frame.transform.SetAsFirstSibling();

        // 填充
        var fillGo = new GameObject("HealthBarFill");
        fillGo.transform.SetParent(bgGo.transform, false);
        var fill = fillGo.AddComponent<Image>();
        fill.color = new Color(0.9f, 0.2f, 0.22f, 1f);
        fill.raycastTarget = false;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 1f;
        var fillRt = fill.rectTransform;
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(2f, 2f);
        fillRt.offsetMax = new Vector2(-2f, -2f);

        healthBarFill = fill;

        // 血量数字
        var txtGo = new GameObject("HealthBarText");
        txtGo.transform.SetParent(bgGo.transform, false);
        var txt = txtGo.AddComponent<Text>();
        txt.font = PixelUi.Font;
        txt.fontSize = 22;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.raycastTarget = false;
        txt.text = currentHealth + " / " + maxHealth;
        var trt = txt.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        healthBarText = txt;

        Debug.Log("条状血条已自动创建");
    }

    void Update()
    {
        if (isInvincible)
        {
            invincibilityTimer -= Time.deltaTime;
            if (invincibilityTimer <= 0)
            {
                isInvincible = false;
                if (spriteRenderer != null) spriteRenderer.color = Color.white;
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (isInvincible) return;

        currentHealth -= damage;
        ComboBombSystem.NotifyPlayerHit();
        UpdateHealthUI();

        GameFx.PlayerHit(transform.position);
        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        TryCounterAttack(damage);
        StartCoroutine(InvincibilityFlash());
    }

    void TryCounterAttack(int incomingDamage)
    {
        float chance = GearMeta.Thorns;
        if (chance <= 0f || Random.value >= chance) return;

        int counterDamage = Mathf.Max(4, Mathf.RoundToInt(incomingDamage * 0.35f));
        float radiusSqr = 2.4f * 2.4f;
        foreach (var enemy in Object.FindObjectsOfType<Enemy>())
        {
            if (enemy != null && (enemy.transform.position - transform.position).sqrMagnitude <= radiusSqr)
                enemy.TakeDamage(counterDamage);
        }
        UltVfx.Ring(transform.position, new Color(0.9f, 0.65f, 0.3f), 0.35f, 2.4f, 0.3f);
        UltVfx.Burst(transform.position, new Color(1f, 0.55f, 0.25f), 10, 5f, 0.3f);
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        UpdateHealthUI();
        if (amount > 0)
            GameFx.Pickup(transform.position, new Color(0.4f, 1f, 0.55f));
    }

    System.Collections.IEnumerator InvincibilityFlash()
    {
        isInvincible = true;
        invincibilityTimer = invincibilityDuration;

        float timer = 0f;
        while (timer < invincibilityDuration)
        {
            if (spriteRenderer != null) spriteRenderer.color = new Color(1, 1, 1, 0.5f);
            yield return new WaitForSeconds(flashInterval);
            if (spriteRenderer != null) spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(flashInterval);
            timer += flashInterval * 2;
        }

        if (spriteRenderer != null) spriteRenderer.color = Color.white;
        isInvincible = false;
    }

    void UpdateHealthUI()
    {
        try
        {
            if (healthBarFill != null && !healthBarFill.Equals(null))
            {
                float ratio = maxHealth <= 0 ? 0f : Mathf.Clamp01((float)currentHealth / maxHealth);
                healthBarFill.fillAmount = ratio;
                if (ratio > 0.5f) healthBarFill.color = new Color(0.25f, 0.85f, 0.3f);
                else if (ratio > 0.25f) healthBarFill.color = new Color(0.95f, 0.8f, 0.2f);
                else healthBarFill.color = new Color(0.9f, 0.2f, 0.22f);
            }
            if (healthBarText != null && !healthBarText.Equals(null))
            {
                healthBarText.text = Mathf.Max(0, currentHealth) + " / " + maxHealth;
            }
        }
        catch (MissingReferenceException)
        {
            healthBarFill = null;
            healthBarText = null;
        }
    }

    void Die()
    {
        AutoAudio.PlayCrash();
        GameFx.PlayerHit(transform.position);
        UltVfx.HitStop(0.15f, 0.05f);
        UltVfx.ScreenFlash(new Color(1f, 0.2f, 0.15f, 0.5f), 0.4f);
        UltVfx.Shake(0.6f, 0.2f);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
        else
        {
            Time.timeScale = 0f;
            MobileGameShell.ShowGameOverBoard();
        }
        gameObject.SetActive(false);
    }

    public void ActivateShield(float duration)
    {
        StopAllCoroutines();
        StartCoroutine(ShieldRoutine(duration));
    }

    /// 换机/重置时清掉护盾闪烁等视觉状态
    public void ResetVisualState()
    {
        StopAllCoroutines();
        isInvincible = false;
        invincibilityTimer = 0f;
        if (spriteRenderer != null) spriteRenderer.color = Color.white;
    }

    System.Collections.IEnumerator ShieldRoutine(float duration)
    {
        isInvincible = true;
        invincibilityTimer = duration;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(0.4f, 0.85f, 1f, 0.9f);
        }
        // 暂停和剧情期间不消耗护盾时长。
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            yield return null;
        }
        if (spriteRenderer != null) spriteRenderer.color = Color.white;
        isInvincible = false;
    }
}
