using System.Collections;
using UnityEngine;

/// 自动普攻 + 原神式：子技能CD(Q) + 大招充能(E) + 专属弹道
public class PlayerShooting : MonoBehaviour
{
    [Header("射击")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 0.16f;
    public float bulletSpeed = 18f;
    public int damage = 1;
    public Color bulletColor = Color.white;
    public ShipMeta.BulletStyle bulletStyle = ShipMeta.BulletStyle.Basic;
    public string shipId = "mortal";

    Sprite godBulletSprite;

    public void RefreshGodBullet()
    {
        if (string.IsNullOrEmpty(shipId)) shipId = "mortal";
        godBulletSprite = Resources.Load<Sprite>("PlayerBullet_" + shipId);
        if (godBulletSprite == null) godBulletSprite = Resources.Load<Sprite>("Bullet");
    }

    [Header("大招（充能，类元素爆发）")]
    public ShipMeta.GodSkill godSkill = ShipMeta.GodSkill.None;
    public string skillName = "大招";
    public float chargeNeed = 100f;
    public float charge;
    public float chargePerKill = GameBalance.UltChargePerKill;
    public float chargePerSec = GameBalance.UltChargePerSec;

    [Header("子技能（冷却，类元素战技）")]
    public ShipMeta.SubSkill subSkill = ShipMeta.SubSkill.None;
    public string subName = "子技能";
    public float subCd = 6f;
    public float subTimer; // 剩余冷却

    public float spreadDuration = 8f;
    float nextFireTime;
    float spreadTimer;
    float rageTimer;
    float windTimer;
    float aegisTimer;
    Sprite bulletSprite;
    bool locked;
    /// 技能特效代数：换机/取消时自增，旧协程失效
    int skillFxId;

    [Header("无尽肉鸽强化（局内叠加）")]
    public int dmgAdd;
    public float rateMul = 1f;
    public int extraProj;
    public float chargeMul = 1f;
    public float lifesteal;
    public float speedMul = 1f;
    public float ultMul = 1f;
    public int extraChain;
    public float shieldMul = 1f;
    public float subCdMul = 1f;
    public float rageBonus;
    public float durationBonus;

    public void ResetRoguelikeMods()
    {
        dmgAdd = 0;
        rateMul = 1f;
        extraProj = 0;
        chargeMul = 1f;
        lifesteal = 0f;
        speedMul = 1f;
        ultMul = 1f;
        extraChain = 0;
        shieldMul = 1f;
        subCdMul = 1f;
        rageBonus = 0f;
        // 恢复机体基础移速
        var mv = GetComponent<PlayerMovement>();
        if (mv != null && ShipMeta.Current != null)
            mv.moveSpeed = ShipMeta.Current.moveSpeed + GearMeta.Collect().moveAdd;
    }

    /// 无尽：应用引擎强化后的移速
    public void ApplyRoguelikeMove()
    {
        var mv = GetComponent<PlayerMovement>();
        if (mv == null) return;
        float baseSpd = (ShipMeta.Current != null ? ShipMeta.Current.moveSpeed : 10f)
            + GearMeta.Collect().moveAdd;
        var timed = GetComponent<TimedPowerUpEffects>();
        float temporaryBonus = timed != null ? timed.ActiveSpeedBonus : 0f;
        mv.moveSpeed = baseSpd * speedMul + temporaryBonus;
    }

    public bool IsSpecialReady() => charge >= chargeNeed && !locked;
    public bool IsSubReady() => subTimer <= 0f && !locked;
    public float Charge01 => chargeNeed <= 0f ? 0f : Mathf.Clamp01(charge / chargeNeed);
    public float Sub01 => subCd <= 0f ? 1f : 1f - Mathf.Clamp01(subTimer / subCd);
    public bool IsSpreadActive() => spreadTimer > 0f;

    /// 取消进行中的大招/子技能视觉与增益（换机、重开时必须调用）
    public void CancelSkillFx()
    {
        skillFxId++;
        StopAllCoroutines();
        locked = false;
        spreadTimer = 0f;
        rageTimer = 0f;
        windTimer = 0f;
        aegisTimer = 0f;
        RestoreShipColor();
    }

    void RestoreShipColor()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = Color.white;
    }

    void OnDisable()
    {
        CancelSkillFx();
    }

    void Start()
    {
        bulletSprite = Resources.Load<Sprite>("Bullet");
        RefreshGodBullet();
        EnsureFirePoint();
        if (bulletPrefab == null) bulletPrefab = CreateTemplate("PB", Color.white, 0.35f);
        EnsurePlayerCollider();
        if (tag != "Player") tag = "Player";
        ShipMeta.ApplyToPlayer(gameObject);
    }

    void EnsureFirePoint()
    {
        if (firePoint != null) return;
        var t = transform.Find("FirePoint");
        if (t == null)
        {
            var go = new GameObject("FirePoint");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            t = go.transform;
        }
        firePoint = t;
    }

    void EnsurePlayerCollider()
    {
        var col = GetComponent<BoxCollider2D>();
        if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        if (col.size.x < 0.2f) col.size = new Vector2(0.7f, 0.7f);
    }

    GameObject CreateTemplate(string name, Color color, float scale)
    {
        var go = new GameObject(name);
        DontDestroyOnLoad(go);
        go.SetActive(false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = bulletSprite;
        sr.color = color;
        go.transform.localScale = Vector3.one * scale;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.45f, 0.6f);
        go.AddComponent<Bullet>();
        return go;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        bool playing = (gm == null || !gm.isGameOver) && Time.timeScale > 0.01f;
        if (spreadTimer > 0f) spreadTimer -= Time.deltaTime;
        if (rageTimer > 0f) rageTimer -= Time.deltaTime;
        if (windTimer > 0f) windTimer -= Time.deltaTime;
        if (aegisTimer > 0f) aegisTimer -= Time.deltaTime;
        if (subTimer > 0f) subTimer -= Time.deltaTime;

        if (!playing) return;

        if (charge < chargeNeed)
        {
            float cps = chargePerSec * chargeMul;
            // 打 Boss 时回能显著加快
            if (ZodiacGameFlow.BossAlive) cps *= GameBalance.UltBossChargeMul;
            charge = Mathf.Min(chargeNeed, charge + cps * Time.deltaTime);
        }

        float rate = fireRate * rateMul;
        if (rageTimer > 0f) rate *= Mathf.Max(0.2f, 0.35f - rageBonus);
        if (windTimer > 0f) rate *= 0.7f;
        if (Time.time >= nextFireTime)
        {
            AutoAttack();
            nextFireTime = Time.time + rate;
        }

        // Q = 子技能 CD；E / 右键 = 大招充能
        if (Input.GetKeyDown(KeyCode.Q) || MobileControls.ConsumeSubSkill())
            TryCastSub();
        if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(1) || MobileControls.ConsumeSpecial())
            TryCastUltimate();
    }

    public void AddCharge(float a) => charge = Mathf.Min(chargeNeed, charge + a * chargeMul);
    public void NotifyKillCharge()
    {
        float amt = chargePerKill;
        if (ZodiacGameFlow.BossAlive) amt *= GameBalance.UltBossChargeMul;
        AddCharge(amt);
        if (lifesteal > 0f)
        {
            var hp = GetComponent<PlayerHealth>();
            if (hp != null) hp.Heal(Mathf.RoundToInt(lifesteal));
        }
    }
    public void ActivateSpread(float d) => spreadTimer = Mathf.Max(spreadTimer, d);

    // ---------- 专属普攻 ----------
    void AutoAttack()
    {
        EnsureFirePoint();
        if (firePoint == null) return;
        int dmg = damage + dmgAdd;
        Color tint = SkillTint(godSkill);
        Color fireC = Color.Lerp(bulletColor, tint, 0.45f);

        switch (bulletStyle)
        {
            case ShipMeta.BulletStyle.Lightning:
                Fire(firePoint.position, Vector2.up * bulletSpeed, dmg, fireC, 0.32f);
                Fire(firePoint.position + new Vector3(0.15f, 0, 0), new Vector2(0.15f, 1f).normalized * bulletSpeed, dmg, fireC * new Color(1, 1, 0.85f), 0.28f);
                MuzzleFx(firePoint.position, tint, MuzzleKind.Zap);
                break;
            case ShipMeta.BulletStyle.Orb:
                Fire(firePoint.position, Vector2.up * (bulletSpeed * 0.85f), dmg, fireC, 0.48f);
                MuzzleFx(firePoint.position, tint, MuzzleKind.Bubble);
                break;
            case ShipMeta.BulletStyle.Spear:
                Fire(firePoint.position, Vector2.up * (bulletSpeed * 1.15f), dmg, fireC, new Vector2(0.22f, 0.7f));
                MuzzleFx(firePoint.position, tint, MuzzleKind.Slash);
                break;
            case ShipMeta.BulletStyle.Lance:
                Fire(firePoint.position, Vector2.up * (bulletSpeed * 1.05f), dmg, fireC, new Vector2(0.18f, 0.85f));
                MuzzleFx(firePoint.position, tint, MuzzleKind.Javelin);
                break;
            case ShipMeta.BulletStyle.Fireball:
                Fire(firePoint.position, Vector2.up * (bulletSpeed * 0.9f), dmg, fireC, 0.55f);
                MuzzleFx(firePoint.position, tint, MuzzleKind.Flare);
                break;
            case ShipMeta.BulletStyle.Arrow:
                Fire(firePoint.position + new Vector3(-0.12f, 0, 0), Vector2.up * bulletSpeed, dmg, fireC, new Vector2(0.16f, 0.55f));
                Fire(firePoint.position + new Vector3(0.12f, 0, 0), Vector2.up * bulletSpeed, dmg, fireC, new Vector2(0.16f, 0.55f));
                MuzzleFx(firePoint.position, tint, MuzzleKind.Dual);
                break;
            case ShipMeta.BulletStyle.Feather:
                Fire(firePoint.position, Vector2.up * (bulletSpeed * 1.25f), dmg, fireC, new Vector2(0.14f, 0.45f));
                MuzzleFx(firePoint.position, tint, MuzzleKind.Wind);
                break;
            case ShipMeta.BulletStyle.Soul:
                Fire(firePoint.position, Vector2.up * (bulletSpeed * 0.8f), dmg, fireC, 0.42f);
                MuzzleFx(firePoint.position, tint, MuzzleKind.Soul);
                break;
            default:
                Fire(firePoint.position, Vector2.up * bulletSpeed, dmg, fireC, 0.35f);
                MuzzleFx(firePoint.position, tint, MuzzleKind.Spark);
                break;
        }

        // 肉鸽额外弹道
        for (int e = 0; e < extraProj; e++)
        {
            float ang = (e % 2 == 0 ? 1f : -1f) * (8f + e * 5f);
            Fire(firePoint.position, Quaternion.Euler(0, 0, ang) * Vector2.up * bulletSpeed, dmg, fireC, 0.28f);
        }

        if (spreadTimer > 0f)
        {
            Fire(firePoint.position, Quaternion.Euler(0, 0, 18f) * Vector2.up * bulletSpeed, dmg, fireC, 0.3f);
            Fire(firePoint.position, Quaternion.Euler(0, 0, -18f) * Vector2.up * bulletSpeed, dmg, fireC, 0.3f);
        }

        if (Time.frameCount % 3 == 0) AutoAudio.PlayShoot();
    }

    enum MuzzleKind { Zap, Bubble, Slash, Javelin, Flare, Dual, Wind, Soul, Spark }

    /// 各弹道专属枪口特效，拉开机体辨识度
    void MuzzleFx(Vector3 pos, Color tint, MuzzleKind kind)
    {
        switch (kind)
        {
            case MuzzleKind.Zap:
                if (Time.frameCount % 2 == 0)
                    UltVfx.Bolt(pos, pos + Vector3.up * 0.9f, tint, 4, 0.1f);
                UltVfx.Boom(pos, 0.16f, tint, 0.1f, 0.45f);
                break;
            case MuzzleKind.Bubble:
                if (Time.frameCount % 2 == 0)
                    UltVfx.Ring(pos + Vector3.up * 0.15f, tint, 0.08f, 0.35f, 0.12f, 12);
                UltVfx.Boom(pos, 0.2f, tint, 0.1f, 0.5f);
                break;
            case MuzzleKind.Slash:
                UltVfx.Beam(pos + Vector3.up * 0.25f, 0.35f, 0.2f, tint, 0.08f);
                UltVfx.Boom(pos, 0.14f, tint, 0.08f, 0.4f);
                break;
            case MuzzleKind.Javelin:
                UltVfx.Boom(pos + Vector3.up * 0.1f, 0.18f, tint, 0.09f, 0.55f);
                if (Time.frameCount % 2 == 0)
                    UltVfx.Burst(pos, tint, 3, 2.5f, 0.12f, 0.12f);
                break;
            case MuzzleKind.Flare:
                UltVfx.Boom(pos, 0.22f, tint, 0.1f, 0.7f);
                if (Time.frameCount % 3 == 0)
                    UltVfx.Burst(pos, tint, 5, 3f, 0.15f, 0.14f);
                break;
            case MuzzleKind.Dual:
                UltVfx.Boom(pos + new Vector3(-0.12f, 0.05f, 0f), 0.12f, tint, 0.08f, 0.35f);
                UltVfx.Boom(pos + new Vector3(0.12f, 0.05f, 0f), 0.12f, tint, 0.08f, 0.35f);
                break;
            case MuzzleKind.Wind:
                UltVfx.Beam(pos + Vector3.up * 0.2f, 0.08f, 0.5f, tint, 0.08f);
                if (Time.frameCount % 2 == 0)
                    UltVfx.Boom(pos + (Vector3)(Random.insideUnitCircle * 0.15f), 0.1f, tint, 0.08f, 0.3f);
                break;
            case MuzzleKind.Soul:
                UltVfx.Boom(pos + Vector3.up * 0.12f, 0.18f, tint, 0.12f, 0.5f);
                if (Time.frameCount % 3 == 0)
                    UltVfx.Burst(pos, tint, 4, 2.2f, 0.18f, 0.12f);
                break;
            default:
                if (Time.frameCount % 2 == 0)
                    UltVfx.Boom(pos, 0.12f, tint, 0.08f, 0.35f);
                break;
        }
    }

    void Fire(Vector3 pos, Vector2 vel, int dmg, Color color, float scale)
    {
        var b = Instantiate(bulletPrefab, pos, Quaternion.identity);
        b.SetActive(true);
        b.transform.localScale = new Vector3(scale, scale * 1.15f, 1f);
        var rb = b.GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = vel;
        var sr = b.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (godBulletSprite != null) sr.sprite = godBulletSprite;
            sr.color = color;
        }
        var sc = b.GetComponent<Bullet>();
        if (sc != null) sc.damage = dmg;
        Destroy(b, 4f);
    }

    void Fire(Vector3 pos, Vector2 vel, int dmg, Color color, Vector2 size)
    {
        var b = Instantiate(bulletPrefab, pos, Quaternion.identity);
        b.SetActive(true);
        b.transform.localScale = new Vector3(size.x, size.y, 1f);
        var rb = b.GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = vel;
        var sr = b.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (godBulletSprite != null) sr.sprite = godBulletSprite;
            sr.color = color;
        }
        var sc = b.GetComponent<Bullet>();
        if (sc != null) sc.damage = dmg;
        Destroy(b, 4f);
    }

    // ---------- 大招 E ----------
    public bool TryCastUltimate()
    {
        if (locked || charge < chargeNeed) return false;
        charge = 0f;
        StartCoroutine(CastUltimate(++skillFxId));
        return true;
    }

    IEnumerator CastUltimate(int fxId)
    {
        locked = true;
        AutoAudio.PlaySpecial();
        Color tint = SkillTint(godSkill);

        // 起手：充能爆发 + 光环 + 短震
        UltVfx.ScreenFlash(new Color(tint.r, tint.g, tint.b, 0.35f), 0.28f);
        UltVfx.Boom(transform.position, 1.2f, tint, 0.4f, 3.2f);
        UltVfx.Ring(transform.position, tint, 0.3f, 4.5f, 0.4f);
        UltVfx.Burst(transform.position, tint, 16, 7f, 0.45f, 0.3f);
        UltVfx.Halo(transform, tint, 0.9f, 2.4f);
        UltVfx.Shake(0.35f, 0.1f);
        StartCoroutine(UltAura(godSkill, fxId));
        StartCoroutine(SkillBanner(skillName, tint));

        // 再切具体大招
        switch (godSkill)
        {
            case ShipMeta.GodSkill.ZeusThunder:
                yield return UltZeusThunder();
                break;
            case ShipMeta.GodSkill.PoseidonTide:
                yield return UltPoseidonTsunami();
                break;
            case ShipMeta.GodSkill.AresRage:
                yield return UltAresRage();
                break;
            case ShipMeta.GodSkill.AthenaAegis:
                yield return UltAthenaAegis();
                break;
            case ShipMeta.GodSkill.ApolloSun:
                yield return UltApolloSun();
                break;
            case ShipMeta.GodSkill.ArtemisVolley:
                yield return UltArtemisRain();
                break;
            case ShipMeta.GodSkill.HermesDash:
                yield return UltHermesDash();
                break;
            case ShipMeta.GodSkill.HadesSoul:
                yield return UltHadesSoul();
                break;
            default:
                spreadTimer = 8f + durationBonus;
                UltVfx.ScreenFlash(Color.white, 0.2f);
                break;
        }

        // 大招结束：若未被换机取消，解锁并强制恢复机身色
        if (skillFxId == fxId)
        {
            locked = false;
            RestoreShipColor();
        }
    }

    /// 大招名称横幅：顶部闪现后淡出
    IEnumerator SkillBanner(string name, Color tint)
    {
        if (string.IsNullOrEmpty(name)) name = "大招";
        var go = new GameObject("UltBanner");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 190;
        var tGo = new GameObject("Name");
        tGo.transform.SetParent(go.transform, false);
        var text = tGo.AddComponent<UnityEngine.UI.Text>();
        text.font = PixelUi.Font;
        text.fontSize = 48;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(tint.r, tint.g, tint.b, 1f);
        text.text = "「" + name + "」";
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        var rt = text.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -120f);
        rt.sizeDelta = new Vector2(600f, 70f);

        float t = 0f;
        const float life = 1.1f;
        while (t < life)
        {
            t += Time.unscaledDeltaTime;
            float u = t / life;
            // 弹入 → 停顿 → 淡出
            float scale = u < 0.15f ? Mathf.Lerp(1.6f, 1f, u / 0.15f) : 1f;
            text.transform.localScale = Vector3.one * scale;
            float a = u < 0.55f ? 1f : 1f - (u - 0.55f) / 0.45f;
            text.color = new Color(tint.r, tint.g, tint.b, a);
            yield return null;
        }
        Destroy(go);
    }

    Color SkillTint(ShipMeta.GodSkill s)
    {
        switch (s)
        {
            case ShipMeta.GodSkill.ZeusThunder: return new Color(1f, 0.95f, 0.4f);
            case ShipMeta.GodSkill.PoseidonTide: return new Color(0.35f, 0.8f, 1f);
            case ShipMeta.GodSkill.AresRage: return new Color(1f, 0.3f, 0.2f);
            case ShipMeta.GodSkill.AthenaAegis: return new Color(0.7f, 0.9f, 1f);
            case ShipMeta.GodSkill.ApolloSun: return new Color(1f, 0.75f, 0.2f);
            case ShipMeta.GodSkill.ArtemisVolley: return new Color(0.9f, 0.95f, 1f);
            case ShipMeta.GodSkill.HermesDash: return new Color(0.5f, 1f, 0.75f);
            case ShipMeta.GodSkill.HadesSoul: return new Color(0.75f, 0.4f, 1f);
            default: return Color.white;
        }
    }

    IEnumerator UltAura(ShipMeta.GodSkill s, int fxId)
    {
        var sr = GetComponent<SpriteRenderer>();
        Color c = SkillTint(s);
        float t = 0f;
        const float duration = 2.6f;
        // 用 unscaled，避免顿帧/暂停把染色卡死
        while (t < duration && skillFxId == fxId && this != null)
        {
            t += Time.unscaledDeltaTime;
            if (sr != null)
            {
                float pulse = 0.72f + 0.28f * Mathf.Sin(t * 14f);
                sr.color = new Color(c.r, c.g, c.b, pulse);
            }
            if (Random.value > 0.45f)
                UltVfx.Boom(transform.position + (Vector3)(Random.insideUnitCircle * 0.6f), 0.22f, c, 0.22f, 0.7f);
            yield return null;
        }
        // 只有当前代大招结束才复位，换机取消交给 CancelSkillFx
        if (skillFxId == fxId && sr != null) sr.color = Color.white;
    }

    /// 宙斯：全屏雷霆 — 落雷柱 + 雷网 + 屏闪
    IEnumerator UltZeusThunder()
    {
        Color gold = new Color(1f, 0.95f, 0.35f);
        UltVfx.Shake(1.3f, 0.16f);
        UltVfx.ScreenFlash(new Color(1f, 0.95f, 0.5f, 0.55f), 0.3f);
        yield return new WaitForSeconds(0.05f);
        // 预警闪烁
        for (int i = 0; i < 3; i++)
        {
            UltVfx.ScreenFlash(new Color(1f, 1f, 0.6f, 0.22f), 0.05f);
            yield return new WaitForSeconds(0.05f);
        }
        for (int wave = 0; wave < 5; wave++)
        {
            for (int c = 0; c < 9; c++)
            {
                float x = -4.8f + c * 1.2f + Random.Range(-0.25f, 0.25f);
                UltVfx.LightningColumn(x, gold);
                for (int r = 0; r < 3; r++)
                    UltVfx.Boom(new Vector3(x + Random.Range(-0.2f, 0.2f), 4.5f - r * 1.6f, 0f),
                        0.4f, new Color(1f, 1f, 0.6f), 0.28f, 1.2f);
                UltDamageNear(new Vector3(x, 0f, 0f), 1.0f, 3 + damage);
            }
            UltDamageAll(2 + damage);
            Flash(new Color(1f, 1f, 0.75f, 1f), 0.08f);
            UltVfx.ScreenFlash(new Color(1f, 0.95f, 0.45f, 0.38f), 0.12f);
            UltVfx.Shake(0.2f, 0.08f);
            yield return new WaitForSeconds(0.24f);
        }
        // 收束雷暴：玩家为中心放电
        UltVfx.Burst(transform.position, gold, 24, 10f, 0.5f, 0.35f);
        UltVfx.Ring(transform.position, gold, 0.4f, 5.5f, 0.5f);
        for (int i = 0; i < 16; i++)
        {
            float ang = i * 22.5f * Mathf.Deg2Rad;
            Fire(transform.position, new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 9f,
                UltDamage(2 + damage), new Color(1f, 0.9f, 0.3f), 0.42f);
        }
        UltVfx.Boom(transform.position, 2f, gold, 0.45f, 5f);
        UltVfx.ScreenFlash(new Color(1f, 0.9f, 0.3f, 0.45f), 0.25f);
        UltVfx.HitStop(0.06f, 0.12f);
        UltVfx.Shake(0.35f, 0.14f);
    }

    /// 波塞冬：海啸席卷 — 多层潮汐墙
    IEnumerator UltPoseidonTsunami()
    {
        Color ocean = new Color(0.3f, 0.75f, 1f);
        UltVfx.Shake(1.5f, 0.12f);
        UltVfx.ScreenFlash(new Color(0.2f, 0.55f, 1f, 0.5f), 0.32f);
        // 海平面升腾预兆
        for (int i = 0; i < 12; i++)
        {
            float x = -5f + i * 0.9f;
            UltVfx.Boom(new Vector3(x, -4.8f, 0f), 0.35f, ocean, 0.4f, 1.4f);
        }
        UltVfx.Ring(new Vector3(0f, -4f, 0f), ocean, 1f, 6f, 0.5f);
        yield return new WaitForSeconds(0.1f);
        for (int wave = 0; wave < 4; wave++)
        {
            float y0 = -5.2f + wave * 0.25f;
            Color wc = new Color(0.3f, 0.7f + wave * 0.06f, 1f);
            // 潮墙视觉：横扫闪光 + 粒子
            UltVfx.Beam(new Vector3(0f, y0 + 0.8f, 0f), 11f, 1.6f, wc, 0.28f);
            for (int row = 0; row < 7; row++)
            {
                for (int k = 0; k < 10; k++)
                {
                    float x = -5f + k * 1.1f;
                    Fire(new Vector3(x, y0 - row * 0.4f, 0f),
                        new Vector2(Random.Range(-0.5f, 0.5f), 12f + wave * 1.2f),
                        UltDamage(3 + damage), wc, 0.6f);
                }
            }
            UltDamageAll(2 + damage);
            Flash(new Color(0.35f, 0.75f, 1f, 0.75f), 0.1f);
            UltVfx.ScreenFlash(new Color(0.25f, 0.6f, 1f, 0.28f), 0.14f);
            UltVfx.Burst(new Vector3(0f, y0 + 1f, 0f), wc, 14, 6f, 0.4f, 0.3f);
            UltVfx.Shake(0.18f, 0.06f);
            yield return new WaitForSeconds(0.38f);
        }
        UltVfx.Ring(transform.position, ocean, 0.5f, 5f, 0.45f);
        UltVfx.HitStop(0.05f, 0.15f);
    }

    /// 阿瑞斯：血怒风暴
    IEnumerator UltAresRage()
    {
        Color blood = new Color(1f, 0.25f, 0.15f);
        UltVfx.ScreenFlash(new Color(1f, 0.15f, 0.1f, 0.48f), 0.32f);
        UltVfx.Ring(transform.position, blood, 0.4f, 3.5f, 0.35f);
        UltVfx.Burst(transform.position, blood, 18, 8f, 0.4f);
        UltVfx.Shake(0.25f, 0.1f);
        float duration = 5.5f + durationBonus;
        rageTimer = duration;
        int rageDamage = damage + 2;
        EnsureFirePoint();
        float t = 0f;
        while (t < duration)
        {
            t += 0.1f;
            Fire(firePoint.position, Vector2.up * bulletSpeed, UltDamage(rageDamage), bulletColor, 0.38f);
            Fire(firePoint.position, Quaternion.Euler(0, 0, 22f) * Vector2.up * bulletSpeed, UltDamage(rageDamage), bulletColor, 0.32f);
            Fire(firePoint.position, Quaternion.Euler(0, 0, -22f) * Vector2.up * bulletSpeed, UltDamage(rageDamage), bulletColor, 0.32f);
            if (Mathf.Repeat(t, 0.5f) < 0.1f)
            {
                Fire(firePoint.position, Quaternion.Euler(0, 0, 45f) * Vector2.up * bulletSpeed, UltDamage(rageDamage), bulletColor, 0.28f);
                Fire(firePoint.position, Quaternion.Euler(0, 0, -45f) * Vector2.up * bulletSpeed, UltDamage(rageDamage), bulletColor, 0.28f);
                UltVfx.Burst(firePoint.position, blood, 6, 4f, 0.25f, 0.22f);
            }
            // 枪口焰
            UltVfx.Boom(firePoint.position + Vector3.up * 0.2f, 0.28f, blood, 0.15f, 0.8f);
            var psr = GetComponent<SpriteRenderer>();
            UltVfx.Afterimage(transform.position, transform.localScale,
                psr != null ? psr.sprite : null, new Color(1f, 0.3f, 0.2f, 0.5f), 0.25f);
            ScreenFlash(new Color(1f, 0.2f, 0.1f, 0.1f), 0.06f);
            yield return new WaitForSeconds(0.1f);
        }
        UltVfx.Burst(transform.position, blood, 14, 6f, 0.4f);
        UltVfx.ScreenFlash(new Color(1f, 0.2f, 0.1f, 0.3f), 0.22f);
    }

    /// 雅典娜：埃癸斯
    IEnumerator UltAthenaAegis()
    {
        Color aegis = new Color(0.7f, 0.88f, 1f);
        UltVfx.ScreenFlash(new Color(0.7f, 0.85f, 1f, 0.42f), 0.28f);
        UltVfx.Shake(0.3f, 0.08f);
        float duration = 5f + durationBonus;
        aegisTimer = duration;
        var hp = GetComponent<PlayerHealth>();
        if (hp != null) hp.ActivateShield(duration * shieldMul);
        foreach (var b in Object.FindObjectsOfType<EnemyBullet>()) Object.Destroy(b.gameObject);
        UltVfx.Ring(transform.position, aegis, 0.5f, 7f, 0.55f, 32);
        for (int i = 0; i < 6; i++)
        {
            float r = 0.7f + i * 1.05f;
            UltVfx.Ring(transform.position, aegis, r * 0.3f, r * 1.4f, 0.35f);
            for (int a = 0; a < 16; a++)
            {
                float ang = a * 22.5f * Mathf.Deg2Rad;
                Fire(transform.position, new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (5.5f + i * 1.8f),
                    UltDamage(2 + damage), new Color(0.75f, 0.9f, 1f), 0.38f);
            }
            UltVfx.Boom(transform.position, r * 0.6f, aegis, 0.3f, r * 1.5f);
            UltDamageNear(transform.position, r, 2 + damage);
            UltVfx.ScreenFlash(new Color(0.75f, 0.9f, 1f, 0.22f), 0.1f);
            yield return new WaitForSeconds(0.2f);
        }
        UltVfx.Halo(transform, aegis, 1.2f, 2f);
    }

    /// 阿波罗：日轮坠落
    IEnumerator UltApolloSun()
    {
        Color sunC = new Color(1f, 0.75f, 0.2f);
        UltVfx.Shake(1.4f, 0.18f);
        UltVfx.ScreenFlash(new Color(1f, 0.75f, 0.2f, 0.5f), 0.32f);
        Vector3 sun = new Vector3(0f, 5.5f, 0f);
        // 日轮下坠
        float t = 0f;
        while (t < 0.7f)
        {
            t += Time.deltaTime;
            sun.y = Mathf.Lerp(5.5f, 1.2f, t / 0.7f);
            UltVfx.Boom(sun, 1.8f + Mathf.Sin(t * 20f) * 0.3f, sunC, 0.12f, 2.6f);
            UltVfx.Beam(new Vector3(sun.x, sun.y - 1.5f, 0f), 1.4f, 3f, sunC, 0.1f);
            ScreenFlash(new Color(1f, 0.75f, 0.2f, 0.12f), 0.06f);
            yield return null;
        }
        // 落地引爆
        UltVfx.HitStop(0.05f, 0.1f);
        UltVfx.Shake(0.4f, 0.2f);
        UltVfx.Boom(sun, 3f, new Color(1f, 0.95f, 0.4f), 0.45f, 7f);
        UltVfx.Ring(sun, sunC, 0.5f, 6f, 0.5f);
        UltVfx.Burst(sun, sunC, 28, 9f, 0.55f, 0.35f);
        UltVfx.ScreenFlash(new Color(1f, 0.9f, 0.35f, 0.55f), 0.28f);
        for (int ring = 0; ring < 4; ring++)
        {
            for (int a = 0; a < 20; a++)
            {
                float ang = a * 18f * Mathf.Deg2Rad + ring * 8f;
                Fire(sun, new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (7f + ring * 2f),
                    UltDamage(3 + damage), new Color(1f, 0.7f, 0.15f), 0.5f);
            }
            UltVfx.Boom(sun, 2.2f, new Color(1f, 0.9f, 0.3f), 0.3f, 4f);
            UltDamageNear(sun, 3.4f, 4 + damage);
            UltVfx.ScreenFlash(new Color(1f, 0.85f, 0.3f, 0.3f), 0.12f);
            yield return new WaitForSeconds(0.2f);
        }
        // 全场余烬
        for (int i = 0; i < 14; i++)
        {
            var p = new Vector3(Random.Range(-4.5f, 4.5f), Random.Range(-1.5f, 4f), 0);
            UltVfx.Boom(p, 0.8f, new Color(1f, 0.6f, 0.1f), 0.3f, 2f);
            UltVfx.Burst(p, sunC, 5, 3f, 0.3f, 0.2f);
            UltDamageNear(p, 1.9f, 3 + damage);
            yield return new WaitForSeconds(0.07f);
        }
    }

    /// 阿尔忒弥斯：月神箭雨
    IEnumerator UltArtemisRain()
    {
        Color moon = new Color(0.9f, 0.95f, 1f);
        Color silver = new Color(0.85f, 0.92f, 1f);
        UltVfx.ScreenFlash(new Color(0.85f, 0.9f, 1f, 0.38f), 0.28f);
        UltVfx.Shake(0.9f, 0.1f);
        // 月光柱笼罩自身
        UltVfx.Beam(transform.position + Vector3.up * 2f, 1.2f, 8f, moon, 0.5f);
        UltVfx.Ring(transform.position, moon, 0.4f, 3.5f, 0.4f);
        EnsureFirePoint();
        for (int volley = 0; volley < 10; volley++)
        {
            for (int c = 0; c < 9; c++)
            {
                float x = -4.8f + c * 1.2f + Random.Range(-0.2f, 0.2f);
                Fire(new Vector3(x, 5.8f, 0f), Vector2.down * (12f + volley * 0.6f), UltDamage(2 + damage),
                    silver, new Vector2(0.18f, 0.75f));
                // 箭雨顶端落点预兆
                if (c % 3 == 0)
                    UltVfx.Boom(new Vector3(x, 5.5f, 0f), 0.25f, moon, 0.15f, 0.7f);
            }
            // 银色指引箭：朝最近敌人
            Vector3 aim = transform.position + Vector3.up * 2f;
            var enemies = Object.FindObjectsOfType<Enemy>();
            float best = 9999f;
            foreach (var e in enemies)
            {
                if (e == null) continue;
                float d = (e.transform.position - transform.position).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    aim = e.transform.position;
                }
            }
            Vector2 toT = ((Vector2)(aim - transform.position)).normalized;
            if (toT.sqrMagnitude < 0.01f) toT = Vector2.up;
            Fire(firePoint.position, toT * 15f, UltDamage(3 + damage), Color.white, new Vector2(0.22f, 0.85f));
            UltVfx.Bolt(transform.position, aim, moon, 6, 0.2f);
            UltVfx.ScreenFlash(new Color(0.9f, 0.95f, 1f, 0.12f), 0.06f);
            yield return new WaitForSeconds(0.11f);
        }
        UltVfx.Ring(transform.position, moon, 0.5f, 4.5f, 0.4f);
        UltVfx.Burst(transform.position, moon, 16, 7f, 0.45f);
    }

    /// 赫尔墨斯：神行天下
    IEnumerator UltHermesDash()
    {
        Color wind = new Color(0.5f, 1f, 0.75f);
        UltVfx.ScreenFlash(new Color(0.4f, 1f, 0.7f, 0.32f), 0.22f);
        UltVfx.Ring(transform.position, wind, 0.3f, 2.8f, 0.3f);
        UltVfx.Shake(0.2f, 0.06f);
        var mv = GetComponent<PlayerMovement>();
        float dashBonus = mv != null ? mv.moveSpeed * 0.8f : 0f;
        if (mv != null) mv.moveSpeed += dashBonus;
        for (int i = 0; i < 16; i++)
        {
            UltDamageNear(transform.position, 1.4f, 2 + damage);
            UltVfx.Boom(transform.position, 0.4f, wind, 0.22f, 1.1f);
            Fire(transform.position + new Vector3(0f, -0.15f, 0f), Vector2.up * 18f, UltDamage(1 + damage), bulletColor, 0.22f);
            // 身后残影轨迹
            var psrH = GetComponent<SpriteRenderer>();
            UltVfx.Afterimage(transform.position + new Vector3(0f, -0.35f, 0f),
                transform.localScale, psrH != null ? psrH.sprite : null,
                new Color(0.4f, 0.9f, 0.65f, 0.65f), 0.4f);
            // 速度线
            if (i % 2 == 0)
            {
                for (int s = 0; s < 3; s++)
                {
                    float ox = Random.Range(-0.6f, 0.6f);
                    UltVfx.Beam(transform.position + new Vector3(ox, -1.2f, 0f), 0.12f, 2.2f, wind, 0.15f);
                }
            }
            yield return new WaitForSeconds(0.085f);
        }
        if (mv != null) mv.moveSpeed = Mathf.Max(4f, mv.moveSpeed - dashBonus);
        UltVfx.Burst(transform.position, wind, 14, 6f, 0.4f);
        UltVfx.ScreenFlash(new Color(0.45f, 1f, 0.7f, 0.2f), 0.18f);
    }

    /// 哈迪斯：亡者收割
    IEnumerator UltHadesSoul()
    {
        Color soul = new Color(0.7f, 0.35f, 1f);
        UltVfx.Shake(1.1f, 0.14f);
        UltVfx.ScreenFlash(new Color(0.55f, 0.2f, 0.9f, 0.5f), 0.35f);
        UltVfx.Ring(transform.position, soul, 0.4f, 6f, 0.55f);
        int n = 0;
        for (int wave = 0; wave < 4; wave++)
        {
            var enemies = Object.FindObjectsOfType<Enemy>();
            foreach (var e in enemies)
            {
                if (e == null) continue;
                e.TakeDamage(UltDamage(2 + damage));
                n++;
                UltVfx.Boom(e.transform.position, 0.6f, soul, 0.3f, 1.6f);
                UltVfx.Burst(e.transform.position, soul, 6, 4f, 0.3f, 0.22f);
                // 魂火吸向玩家
                Fire(e.transform.position, ((Vector2)(transform.position - e.transform.position)).normalized * 7f,
                    UltDamage(1 + damage), new Color(0.7f, 0.35f, 1f), 0.4f);
                UltVfx.Bolt(e.transform.position, transform.position, soul, 5, 0.18f);
            }
            Flash(new Color(0.7f, 0.35f, 1f, 0.9f), 0.08f);
            UltVfx.ScreenFlash(new Color(0.6f, 0.25f, 0.95f, 0.22f), 0.1f);
            UltVfx.Shake(0.15f, 0.05f);
            yield return new WaitForSeconds(0.2f);
        }
        var h2 = GetComponent<PlayerHealth>();
        if (h2 != null && n > 0) h2.Heal(Mathf.Min(40, n * 4));
        UltVfx.Burst(transform.position, new Color(0.5f, 1f, 0.6f), 12, 5f, 0.45f);
        UltVfx.ScreenFlash(new Color(0.6f, 0.25f, 0.95f, 0.28f), 0.22f);
    }

    void LightningColumn(float x, Color color)
    {
        UltVfx.LightningColumn(x, color);
    }

    int UltDamage(int rawDamage)
    {
        return Mathf.Max(1, Mathf.RoundToInt(rawDamage * ultMul));
    }

    void UltDamageAll(int rawDamage)
    {
        DamageAll(UltDamage(rawDamage));
    }

    void UltDamageNear(Vector3 center, float radius, int rawDamage)
    {
        DamageNear(center, radius, UltDamage(rawDamage));
    }

    void ScreenFlash(Color color, float time)
    {
        UltVfx.ScreenFlash(color, time);
    }

    // ---------- 子技能 Q（CD） ----------
    public bool TryCastSub()
    {
        if (locked || subTimer > 0f) return false;
        subTimer = subCd * subCdMul;
        StartCoroutine(CastSub());
        return true;
    }

    IEnumerator CastSub()
    {
        locked = true;
        AutoAudio.PlayHit();
        EnsureFirePoint();
        Color tint = SkillTint(godSkill);

        // 子技能起手也有专属色，一眼能看出是哪一机
        UltVfx.Boom(transform.position, 0.7f, tint, 0.22f, 1.8f);
        UltVfx.Ring(transform.position, tint * 0.9f, 0.25f, 1.6f, 0.22f);

        switch (subSkill)
        {
            case ShipMeta.SubSkill.ZeusChain:
            {
                Color gold = new Color(1f, 0.95f, 0.4f);
                var targets = Object.FindObjectsOfType<Enemy>();
                Enemy first = null; float best = 999f;
                foreach (var e in targets)
                {
                    if (e == null) continue;
                    float d = (e.transform.position - transform.position).sqrMagnitude;
                    if (d < best) { best = d; first = e; }
                }
                if (first != null)
                {
                    first.TakeDamage(2 + damage + dmgAdd);
                    UltVfx.Bolt(transform.position, first.transform.position, gold, 8, 0.28f);
                    UltVfx.Boom(first.transform.position, 0.7f, gold, 0.3f, 1.8f);
                    UltVfx.Burst(first.transform.position, gold, 10, 5f, 0.35f);
                    // 连锁最多 3 个 + 肉鸽加成
                    Enemy prev = first;
                    int chained = 0;
                    int chainCap = 3 + extraChain;
                    int chainDmg = 1 + damage + dmgAdd;
                    foreach (var e in targets)
                    {
                        if (e == null || e == first || chained >= chainCap) continue;
                        if ((e.transform.position - prev.transform.position).sqrMagnitude < 12f)
                        {
                            e.TakeDamage(chainDmg);
                            UltVfx.Bolt(prev.transform.position, e.transform.position, gold, 6, 0.22f);
                            UltVfx.Boom(e.transform.position, 0.5f, gold, 0.25f, 1.4f);
                            prev = e;
                            chained++;
                        }
                    }
                }
                else
                {
                    // 无目标：向上放一道霹雳，避免看起来“没反应”
                    UltVfx.Bolt(transform.position, transform.position + Vector3.up * 4.5f, gold, 9, 0.3f);
                    UltVfx.LightningColumn(transform.position.x, gold, 5f, transform.position.y, 8);
                }
                UltVfx.ScreenFlash(new Color(1f, 0.95f, 0.5f, 0.12f), 0.1f);
                break;
            }
            case ShipMeta.SubSkill.PoseidonBubble:
            {
                Color ocean = new Color(0.35f, 0.8f, 1f);
                // 双环水弹
                for (int i = 0; i < 12; i++)
                {
                    float ang = i * 30f * Mathf.Deg2Rad;
                    Fire(transform.position, new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 10f,
                        2 + damage, ocean, 0.42f);
                }
                for (int i = 0; i < 8; i++)
                {
                    float ang = (i * 45f + 22.5f) * Mathf.Deg2Rad;
                    Fire(transform.position, new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 7f,
                        1 + damage, ocean * 0.85f, 0.32f);
                }
                UltVfx.Ring(transform.position, ocean, 0.4f, 4.5f, 0.4f);
                UltVfx.Ring(transform.position, ocean * 0.8f, 0.2f, 2.8f, 0.35f);
                UltVfx.Burst(transform.position, ocean, 14, 6f, 0.4f);
                DamageNear(transform.position, 1.8f, 1 + damage);
                var hp = GetComponent<PlayerHealth>();
                if (hp != null) hp.ActivateShield((1.8f + durationBonus) * shieldMul);
                UltVfx.ScreenFlash(new Color(0.25f, 0.6f, 1f, 0.12f), 0.12f);
                break;
            }
            case ShipMeta.SubSkill.AresCleave:
            {
                Color blood = new Color(1f, 0.3f, 0.2f);
                for (int a = -4; a <= 4; a++)
                {
                    var dir = Quaternion.Euler(0, 0, a * 12f) * Vector2.up;
                    Fire(firePoint.position, dir * 16f, 3 + damage, blood, new Vector2(0.28f, 0.85f));
                }
                // 横扫刀光
                UltVfx.Beam(firePoint.position + Vector3.up * 1.2f, 4.5f, 0.35f, blood, 0.22f);
                UltVfx.Burst(firePoint.position, blood, 12, 7f, 0.35f);
                UltVfx.Shake(0.12f, 0.05f);
                DamageNear(transform.position + Vector3.up * 1.5f, 2.2f, 1 + damage);
                break;
            }
            case ShipMeta.SubSkill.AthenaLance:
            {
                Color aegis = new Color(0.75f, 0.9f, 1f);
                // 贯穿圣枪：粗光柱 + 连发
                UltVfx.Beam(firePoint.position + Vector3.up * 3f, 0.9f, 7f, aegis, 0.3f);
                for (int i = 0; i < 6; i++)
                {
                    Fire(firePoint.position + Vector3.up * i * 0.4f, Vector2.up * 24f,
                        2 + damage, aegis, new Vector2(0.22f, 1.25f));
                }
                // 直线判定
                foreach (var e in Object.FindObjectsOfType<Enemy>())
                {
                    if (e == null) continue;
                    Vector3 p = e.transform.position;
                    if (Mathf.Abs(p.x - transform.position.x) < 0.85f && p.y > transform.position.y)
                    {
                        e.TakeDamage(3 + damage);
                        UltVfx.Boom(p, 0.55f, aegis, 0.25f, 1.4f);
                    }
                }
                UltVfx.Ring(transform.position, aegis, 0.3f, 2.2f, 0.28f);
                UltVfx.ScreenFlash(new Color(0.75f, 0.9f, 1f, 0.1f), 0.1f);
                break;
            }
            case ShipMeta.SubSkill.ApolloFlare:
            {
                Color sun = new Color(1f, 0.75f, 0.2f);
                UltVfx.Boom(transform.position, 1.6f, sun, 0.35f, 4f);
                UltVfx.Ring(transform.position, sun, 0.4f, 5f, 0.4f);
                UltVfx.Burst(transform.position, sun, 18, 8f, 0.45f);
                UltVfx.ScreenFlash(new Color(1f, 0.8f, 0.3f, 0.18f), 0.14f);
                DamageNear(transform.position, 2.6f, 3 + damage);
                for (int i = 0; i < 10; i++)
                {
                    float ang = i * 36f * Mathf.Deg2Rad;
                    Fire(transform.position, new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 10f,
                        2 + damage, sun, 0.38f);
                }
                UltVfx.Shake(0.15f, 0.06f);
                break;
            }
            case ShipMeta.SubSkill.ArtemisMark:
            {
                Color moon = new Color(0.9f, 0.95f, 1f);
                var targets = Object.FindObjectsOfType<Enemy>();
                int shot = 0;
                foreach (var e in targets)
                {
                    if (e == null || shot >= 5) continue;
                    // 标记环
                    UltVfx.Ring(e.transform.position, moon, 0.3f, 1.1f, 0.25f, 16);
                    Vector2 dir = ((Vector2)(e.transform.position - transform.position)).normalized;
                    Fire(firePoint.position, dir * 18f, 2 + damage, moon, new Vector2(0.18f, 0.7f));
                    UltVfx.Bolt(transform.position, e.transform.position, moon * 0.85f, 5, 0.18f);
                    shot++;
                }
                if (shot == 0)
                {
                    for (int a = -2; a <= 2; a++)
                        Fire(firePoint.position, Quaternion.Euler(0, 0, a * 14f) * Vector2.up * 17f,
                            3 + damage, moon, new Vector2(0.2f, 0.75f));
                    UltVfx.Beam(firePoint.position + Vector3.up * 2f, 0.5f, 5f, moon, 0.2f);
                }
                UltVfx.Burst(transform.position, moon, 8, 4f, 0.3f);
                break;
            }
            case ShipMeta.SubSkill.HermesWind:
            {
                Color wind = new Color(0.5f, 1f, 0.75f);
                float duration = 3.5f + durationBonus;
                windTimer = duration;
                var mv = GetComponent<PlayerMovement>();
                const float windSpeedBonus = 4f;
                if (mv != null) mv.moveSpeed += windSpeedBonus;
                UltVfx.Ring(transform.position, wind, 0.3f, 3.2f, 0.35f);
                UltVfx.ScreenFlash(new Color(0.4f, 1f, 0.7f, 0.1f), 0.12f);
                float t = 0f;
                while (t < duration)
                {
                    t += Time.deltaTime;
                    UltVfx.Afterimage(transform.position, transform.localScale,
                        GetComponent<SpriteRenderer>() != null ? GetComponent<SpriteRenderer>().sprite : null,
                        new Color(0.4f, 0.9f, 0.65f, 0.45f), 0.3f);
                    if (Random.value > 0.55f)
                        Fire(transform.position + new Vector3(0f, -0.1f, 0f),
                            Quaternion.Euler(0, 0, Random.Range(-25f, 25f)) * Vector2.up * 16f,
                            1 + damage, wind, 0.22f);
                    yield return null;
                }
                if (mv != null) mv.moveSpeed = Mathf.Max(4f, mv.moveSpeed - windSpeedBonus);
                UltVfx.Burst(transform.position, wind, 10, 5f, 0.3f);
                break;
            }
            case ShipMeta.SubSkill.HadesCurse:
            {
                Color soul = new Color(0.7f, 0.35f, 1f);
                UltVfx.Ring(transform.position, soul, 0.5f, 7f, 0.55f, 32);
                UltVfx.ScreenFlash(new Color(0.55f, 0.2f, 0.9f, 0.14f), 0.15f);
                foreach (var e in Object.FindObjectsOfType<Enemy>())
                {
                    if (e == null) continue;
                    StartCoroutine(CurseEnemy(e, 3f + durationBonus, 1 + damage));
                    UltVfx.Boom(e.transform.position, 0.55f, soul, 0.3f, 1.5f);
                    UltVfx.Burst(e.transform.position, soul, 6, 3.5f, 0.3f, 0.2f);
                    UltVfx.Bolt(transform.position, e.transform.position, soul * 0.9f, 4, 0.18f);
                }
                break;
            }
            default:
            {
                // 凡人疾射：三连直射 + 枪口焰
                Color flash = new Color(1f, 1f, 0.85f);
                for (int i = 0; i < 3; i++)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        Fire(firePoint.position + new Vector3((k - 1) * 0.18f, 0f, 0f),
                            Vector2.up * bulletSpeed * 1.15f, 1 + damage, flash, 0.3f);
                    }
                    UltVfx.Boom(firePoint.position + Vector3.up * 0.15f, 0.3f, flash, 0.12f, 0.8f);
                    yield return new WaitForSeconds(0.08f);
                }
                break;
            }
        }
        locked = false;
    }

    IEnumerator CurseEnemy(Enemy target, float duration, int tickDamage)
    {
        if (target == null) yield break;
        float originalSpeed = target.moveSpeed;
        target.moveSpeed *= 0.55f;
        float elapsed = 0f;
        while (target != null && elapsed < duration)
        {
            target.TakeDamage(tickDamage);
            yield return new WaitForSeconds(0.75f);
            elapsed += 0.75f;
        }
        if (target != null) target.moveSpeed = originalSpeed;
    }

    void DamageAll(int dmg)
    {
        foreach (var e in Object.FindObjectsOfType<Enemy>())
            if (e != null) e.TakeDamage(dmg);
    }

    void DamageNear(Vector3 c, float r, int dmg)
    {
        float r2 = r * r;
        foreach (var e in Object.FindObjectsOfType<Enemy>())
            if (e != null && (e.transform.position - c).sqrMagnitude <= r2) e.TakeDamage(dmg);
    }

    void BoomAll(Color c)
    {
        foreach (var e in Object.FindObjectsOfType<Enemy>())
            if (e != null) Boom(e.transform.position, 0.65f, c);
    }

    void Boom(Vector3 pos, float s, Color c)
    {
        UltVfx.Boom(pos, s, c, 0.32f, s * 2.1f);
    }

    void Flash(Color c, float t)
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) StartCoroutine(FlashCo(sr, c, t));
    }

    IEnumerator FlashCo(SpriteRenderer sr, Color c, float t)
    {
        sr.color = c;
        yield return new WaitForSeconds(t);
        if (sr != null) sr.color = Color.white;
    }
}
