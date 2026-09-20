using System.Collections;
using UnityEngine;

/// 十三宫 Boss 专属 AI：按星座特征切换攻击
public class ZodiacBossAI : MonoBehaviour
{
    public string zodiacKey = "aries";
    public int phase; // 0 常规 1 狂暴
    public GameObject bulletPrefab;

    Enemy enemy;
    Transform firePoint;
    Sprite bulletSprite;
    float patternTimer;
    float nextPattern = 2.5f;
    int startHp;
    bool busy;

    Vector3 home;
    float t0;

    public void Setup(string key, GameObject bullet, Enemy e)
    {
        zodiacKey = key;
        bulletPrefab = bullet;
        enemy = e;
        if (enemy != null)
        {
            enemy.canShoot = false; // 由本 AI 出招
            enemy.movementPattern = Enemy.MovementPattern.Straight;
            enemy.moveSpeed = 0f; // 禁止默认下压，由 AI 固定位
            var rb = enemy.GetComponent<Rigidbody2D>();
            if (rb != null) rb.velocity = Vector2.zero;
            startHp = enemy.health;
        }

        var fp = transform.Find("FirePoint");
        if (fp == null)
        {
            var go = new GameObject("FirePoint");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, -0.7f, 0f);
            fp = go.transform;
        }
        firePoint = fp;
        bulletSprite = Resources.Load<Sprite>("Bullet");
        home = transform.position;
        t0 = Time.time;
        StartCoroutine(IntroDrop());
        StartCoroutine(PatternLoop());
    }

    /// 破防后强制进入狂暴二阶段
    public void ForceEnrage()
    {
        if (phase >= 1) return;
        phase = 1;
        nextPattern = Mathf.Max(1.0f, nextPattern * 0.55f);
        Flash(new Color(1f, 0.3f, 0.2f, 0.9f), 0.35f);
        GameFx.PhaseShift(transform.position, new Color(1f, 0.35f, 0.2f));
    }

    IEnumerator IntroDrop()
    {
        Vector3 target = new Vector3(0f, 3.2f, 0f);
        float t = 0f;
        while (t < 1.2f)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(home, target, t / 1.2f);
            yield return null;
        }
        home = target;
        transform.position = target;
        if (enemy != null) enemy.moveSpeed = 0f;
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = Vector2.zero;
    }

    void Update()
    {
        if (enemy == null || enemy.Equals(null)) return;
        if (Time.time - t0 < 1.3f) return;

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = Vector2.zero;

        // 出招中：技能自己控制位移，不抢
        if (busy) return;

        // 待机：上半屏左右游走 + 轻微上下，绝不停着发呆
        float tt = Time.time - t0;
        float bobY = home.y + Mathf.Sin(tt * 0.9f) * 0.35f;
        float patrolX = home.x + Mathf.Sin(tt * 0.55f) * 2.4f * (phase == 0 ? 1f : 1.35f);
        // 狂暴时更靠近玩家一侧但仍保持高度
        if (phase == 1)
        {
            float px = PlayerPos().x;
            patrolX = Mathf.Lerp(patrolX, px, 0.25f + 0.15f * Mathf.Sin(tt));
        }
        // 高度钳制：不冲到玩家脸上
        float y = Mathf.Clamp(bobY, 2.0f, 3.8f);
        Vector3 want = new Vector3(Mathf.Clamp(patrolX, -4.2f, 4.2f), y, 0f);
        transform.position = Vector3.Lerp(transform.position, want, Time.deltaTime * 3.5f);
        home = new Vector3(Mathf.Lerp(home.x, patrolX * 0.5f, Time.deltaTime * 0.5f), home.y, 0f);
        // home 的 y 保持战区，出招后便于复位
        home.y = 3.2f;
    }

    IEnumerator PatternLoop()
    {
        yield return new WaitForSeconds(1.4f);
        while (enemy == null || enemy.health > 0)
        {
            if (enemy != null && startHp > 0 && enemy.health <= startHp / 2 && phase == 0)
            {
                phase = 1;
                nextPattern = Mathf.Max(1.2f, nextPattern * 0.7f);
                Flash(new Color(1f, 0.35f, 0.25f, 0.8f), 0.3f);
                GameFx.PhaseShift(transform.position, new Color(1f, 0.35f, 0.25f));
            }

            if (!busy)
            {
                busy = true;
                yield return StartCoroutine(RunPattern());
                busy = false;
                // 出招结束拉回战区顶部，避免贴脸或掉出屏
                yield return StartCoroutine(ReturnToArena());
            }
            yield return new WaitForSeconds(nextPattern);
        }
    }

    IEnumerator ReturnToArena()
    {
        Vector3 from = transform.position;
        Vector3 to = new Vector3(
            Mathf.Clamp(from.x, -3.5f, 3.5f),
            Mathf.Clamp(from.y, 2.4f, 3.6f),
            0f);
        float t = 0f;
        float dur = 0.45f;
        while (t < dur && transform != null)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(from, to, t / dur);
            yield return null;
        }
        if (transform != null) transform.position = to;
        home = new Vector3(to.x, 3.2f, 0f);
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = Vector2.zero;
    }

    IEnumerator RunPattern()
    {
        switch (zodiacKey)
        {
            case "aries": yield return AriesCharge(); break;
            case "taurus": yield return TaurusSweep(); break;
            case "gemini": yield return GeminiTwin(); break;
            case "cancer": yield return CancerPincer(); break;
            case "leo": yield return LeoMane(); break;
            case "virgo": yield return VirgoRain(); break;
            case "libra": yield return LibraScale(); break;
            case "scorpio": yield return ScorpioSting(); break;
            case "sagittarius": yield return SagArrow(); break;
            case "capricorn": yield return CapricornRam(); break;
            case "aquarius": yield return AquariusPour(); break;
            case "pisces": yield return PiscesSwim(); break;
            case "kronos": yield return KronosRings(); break;
            default: yield return LeoMane(); break;
        }
    }

    static readonly System.Collections.Generic.Dictionary<string, Sprite> bulletSprites =
        new System.Collections.Generic.Dictionary<string, Sprite>();

    static Sprite LoadBulletSprite(string key)
    {
        if (string.IsNullOrEmpty(key)) return Resources.Load<Sprite>("Bullet");
        if (bulletSprites.TryGetValue(key, out var cached) && cached != null) return cached;
        var spr = Resources.Load<Sprite>("ZodiacBullet_" + key);
        if (spr == null) spr = Resources.Load<Sprite>("Bullet");
        if (spr != null) bulletSprites[key] = spr;
        return spr;
    }

    GameObject Fire(Vector3 pos, Vector2 vel, float scale, Color color, int dmg = 0)
    {
        if (bulletPrefab == null) return null;
        var b = Object.Instantiate(bulletPrefab, pos, Quaternion.identity);
        b.SetActive(true);
        b.transform.localScale = Vector3.one * Mathf.Max(0.55f, scale);
        var rb = b.GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = vel;
        var sr = b.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            var zspr = LoadBulletSprite(zodiacKey);
            if (zspr != null) sr.sprite = zspr;
            sr.color = color;
        }
        var eb = b.GetComponent<EnemyBullet>();
        if (eb != null)
        {
            int contactDamage = enemy != null ? enemy.damage : GameBalance.BossDmgMin;
            eb.damage = dmg > 1 ? dmg : GameBalance.BossBulletDamage(contactDamage);
        }
        Object.Destroy(b, 6f);
        return b;
    }

    Vector3 PlayerPos()
    {
        var p = GameObject.Find("Player");
        return p != null ? p.transform.position : new Vector3(0f, -3f, 0f);
    }

    void Flash(Color c, float t) { StartCoroutine(FlashCo(c, t)); }
    IEnumerator FlashCo(Color c, float t)
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) yield break;
        var old = sr.color;
        sr.color = c;
        yield return new WaitForSeconds(t);
        if (sr != null) sr.color = Color.white;
    }

    // ---------- 白羊：冲锋 ----------
    IEnumerator AriesCharge()
    {
        for (int i = 0; i < (phase == 0 ? 2 : 3); i++)
        {
            Vector3 start = transform.position;
            Vector3 target = new Vector3(PlayerPos().x, Mathf.Max(-1f, PlayerPos().y + 1.4f), 0f);
            // 先固定并显示冲锋航线，给玩家明确的横移反应时间。
            UltVfx.Bolt(start, target, new Color(1f, 0.35f, 0.25f, 0.75f), 8, 0.3f);
            yield return new WaitForSeconds(0.35f);
            // 冲锋拉长，降低突进速度
            float chargeDur = 0.75f;
            float t = 0f;
            while (t < chargeDur)
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(start, target, t / chargeDur);
                yield return null;
            }
            // 撞击波
            for (int k = -2; k <= 2; k++)
                Fire(transform.position, new Vector2(k * 1.2f, -5.5f), 0.4f, new Color(1f, 0.45f, 0.3f));
            yield return new WaitForSeconds(0.2f);
            t = 0f;
            float backDur = 0.6f;
            while (t < backDur)
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(target, home, t / backDur);
                yield return null;
            }
            yield return new WaitForSeconds(0.35f);
        }
    }

    // ---------- 金牛：横扫冲撞 ----------
    IEnumerator TaurusSweep()
    {
        int times = phase == 0 ? 1 : 2;
        for (int n = 0; n < times; n++)
        {
            float dir = Random.value > 0.5f ? 1f : -1f;
            Vector3 start = transform.position;
            Vector3 end = new Vector3(dir * 4.2f, home.y, 0f);
            float t = 0f;
            while (t < 0.55f)
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(start, end, t / 0.55f);
                // 横向冲撞弹幕
                Fire(transform.position + Vector3.down * 0.6f, new Vector2(-dir * 5f, -2f), 0.45f, new Color(0.95f, 0.7f, 0.25f));
                yield return new WaitForSeconds(0.08f);
            }
            t = 0f;
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(end, home, t / 0.4f);
                yield return null;
            }
            yield return new WaitForSeconds(0.3f);
        }
    }

    // ---------- 双子：双炮镜像 ----------
    IEnumerator GeminiTwin()
    {
        for (int volley = 0; volley < (phase == 0 ? 3 : 5); volley++)
        {
            Vector3 L = transform.position + new Vector3(-0.7f, -0.3f, 0);
            Vector3 R = transform.position + new Vector3(0.7f, -0.3f, 0);
            Vector3 p = PlayerPos();
            Vector2 dirL = ((Vector2)(p - L)).normalized;
            Vector2 dirR = ((Vector2)(p - R)).normalized;
            Fire(L, dirL * 8f, 0.35f, new Color(0.75f, 0.85f, 1f));
            Fire(R, dirR * 8f, 0.35f, new Color(0.75f, 0.85f, 1f));
            // 镜像偏转
            Fire(L, Quaternion.Euler(0, 0, 15f) * dirL * 7f, 0.3f, new Color(0.9f, 0.9f, 1f));
            Fire(R, Quaternion.Euler(0, 0, -15f) * dirR * 7f, 0.3f, new Color(0.9f, 0.9f, 1f));
            yield return new WaitForSeconds(0.35f);
        }
    }

    // ---------- 巨蟹：双钳合围 ----------
    IEnumerator CancerPincer()
    {
        // 左右钳弹合拢
        for (int wave = 0; wave < (phase == 0 ? 2 : 3); wave++)
        {
            for (int i = 0; i < 5; i++)
            {
                float y = home.y - 0.5f - i * 0.35f;
                Fire(new Vector3(-5.2f, y, 0), new Vector2(4.5f, -1.2f), 0.4f, new Color(0.5f, 0.9f, 0.85f));
                Fire(new Vector3(5.2f, y, 0), new Vector2(-4.5f, -1.2f), 0.4f, new Color(0.5f, 0.9f, 0.85f));
                yield return new WaitForSeconds(0.08f);
            }
            // 硬壳：短时减速自身
            float old = enemy != null ? enemy.moveSpeed : 0f;
            if (enemy != null) enemy.moveSpeed *= 0.3f;
            yield return new WaitForSeconds(0.6f);
            if (enemy != null) enemy.moveSpeed = old;
        }
    }

    // ---------- 狮子：鬃毛环射 ----------
    IEnumerator LeoMane()
    {
        int rings = phase == 0 ? 2 : 3;
        int count = phase == 0 ? 12 : 16;
        for (int r = 0; r < rings; r++)
        {
            for (int i = 0; i < count; i++)
            {
                float ang = (i * 360f / count + r * 10f) * Mathf.Deg2Rad;
                Fire(transform.position, new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 5.5f, 0.38f, new Color(1f, 0.75f, 0.2f));
            }
            yield return new WaitForSeconds(0.55f);
        }
        // 正面咆哮直射
        for (int i = 0; i < 4; i++)
        {
            Fire(transform.position, Vector2.down * 9f, 0.5f, new Color(1f, 0.85f, 0.3f), 1);
            yield return new WaitForSeconds(0.12f);
        }
    }

    // ---------- 处女：星雨精准 ----------
    IEnumerator VirgoRain()
    {
        for (int i = 0; i < (phase == 0 ? 8 : 12); i++)
        {
            float x = PlayerPos().x + Random.Range(-0.8f, 0.8f);
            Fire(new Vector3(x, 5.5f, 0), Vector2.down * 6.5f, 0.32f, new Color(0.95f, 0.85f, 1f));
            if (phase > 0 && i % 3 == 0)
            {
                Fire(new Vector3(x + 0.5f, 5.5f, 0), Vector2.down * 6f, 0.28f, new Color(0.85f, 0.75f, 1f));
            }
            yield return new WaitForSeconds(0.18f);
        }
    }

    // ---------- 天秤：左右审判 ----------
    IEnumerator LibraScale()
    {
        for (int round = 0; round < (phase == 0 ? 3 : 4); round++)
        {
            bool left = round % 2 == 0;
            for (int i = 0; i < 6; i++)
            {
                float x = left ? -4.5f + i * 0.3f : 4.5f - i * 0.3f;
                Fire(new Vector3(x, home.y, 0), new Vector2(left ? 1.2f : -1.2f, -6f), 0.36f, new Color(0.7f, 0.95f, 0.75f));
                yield return new WaitForSeconds(0.07f);
            }
            yield return new WaitForSeconds(0.25f);
        }
    }

    // ---------- 天蝎：毒刺锁定 ----------
    IEnumerator ScorpioSting()
    {
        for (int i = 0; i < (phase == 0 ? 5 : 8); i++)
        {
            Vector3 p = PlayerPos();
            Vector2 dir = ((Vector2)(p - transform.position)).normalized;
            // 瞄准线
            var aim = Fire(transform.position, dir * 0.1f, 0.5f, new Color(0.85f, 0.25f, 0.45f, 0.5f));
            yield return new WaitForSeconds(0.15f);
            if (aim != null) Object.Destroy(aim);
            Fire(transform.position, dir * 10f, 0.42f, new Color(0.9f, 0.3f, 0.5f), 1);
            yield return new WaitForSeconds(0.22f);
        }
    }

    // ---------- 射手：箭雨齐发 ----------
    IEnumerator SagArrow()
    {
        for (int volley = 0; volley < (phase == 0 ? 3 : 4); volley++)
        {
            for (int a = -3; a <= 3; a++)
            {
                var dir = Quaternion.Euler(0, 0, a * 9f) * Vector2.down;
                Fire(transform.position, dir * 7.5f, 0.35f, new Color(1f, 0.55f, 0.25f));
            }
            // 对准玩家一箭
            Vector2 toP = ((Vector2)(PlayerPos() - transform.position)).normalized;
            Fire(transform.position, toP * 9f, 0.4f, new Color(1f, 0.7f, 0.3f), 1);
            yield return new WaitForSeconds(0.5f);
        }
    }

    // ---------- 摩羯：深渊重装 ----------
    IEnumerator CapricornRam()
    {
        // 缓慢压下 + 礁石坠落
        Vector3 start = transform.position;
        Vector3 low = new Vector3(start.x, start.y - 1.2f, 0f);
        float t = 0f;
        while (t < 0.8f)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(start, low, t / 0.8f);
            yield return null;
        }
        for (int i = 0; i < (phase == 0 ? 6 : 9); i++)
        {
            float x = Random.Range(-4.5f, 4.5f);
            Fire(new Vector3(x, 5.2f, 0), Vector2.down * 3.5f, 0.55f, new Color(0.55f, 0.7f, 0.55f), 1);
            yield return new WaitForSeconds(0.2f);
        }
        t = 0f;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(low, home, t / 0.6f);
            yield return null;
        }
    }

    // ---------- 水瓶：倾泻 ----------
    IEnumerator AquariusPour()
    {
        float side = Random.value > 0.5f ? 1f : -1f;
        Vector3 mouth = transform.position + new Vector3(side * 0.8f, -0.4f, 0);
        for (int i = 0; i < (phase == 0 ? 14 : 20); i++)
        {
            Vector2 dir = new Vector2(-side * 0.8f, -1f).normalized;
            Fire(mouth, dir * 6f, 0.32f, new Color(0.4f, 0.75f, 1f));
            // 水面横流
            if (i % 4 == 0)
                Fire(mouth, new Vector2(-side * 5f, -0.5f), 0.3f, new Color(0.5f, 0.85f, 1f));
            yield return new WaitForSeconds(0.1f);
        }
    }

    // ---------- 双鱼：双鱼游弋 ----------
    IEnumerator PiscesSwim()
    {
        for (int cycle = 0; cycle < (phase == 0 ? 2 : 3); cycle++)
        {
            // 8 字轨迹
            float dur = 1.6f;
            float t = 0f;
            Vector3 c = home;
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = t / dur * Mathf.PI * 2f;
                transform.position = c + new Vector3(Mathf.Sin(u) * 2.8f, Mathf.Sin(u * 2f) * 0.5f, 0f);
                if (t % 0.25f < Time.deltaTime)
                {
                    Fire(transform.position + new Vector3(-0.5f, -0.4f, 0), Vector2.down * 5f, 0.3f, new Color(0.55f, 0.55f, 1f));
                    Fire(transform.position + new Vector3(0.5f, -0.4f, 0), Vector2.down * 5f, 0.3f, new Color(0.65f, 0.65f, 1f));
                }
                yield return null;
            }
            transform.position = c;
            yield return new WaitForSeconds(0.3f);
        }
    }

    // ---------- 克洛诺斯：黄道时环 ----------
    IEnumerator KronosRings()
    {
        int rings = phase == 0 ? 2 : 3;
        for (int r = 0; r < rings; r++)
        {
            // 十二宫环
            for (int i = 0; i < 12; i++)
            {
                float ang = (i * 30f + r * 15f) * Mathf.Deg2Rad;
                float spd = 4.2f + r * 0.5f;
                Fire(transform.position, new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spd, 0.36f, new Color(0.95f, 0.75f, 0.25f), 1);
            }
            // 时针扫射
            float hand = 0f;
            float handT = 0f;
            while (handT < 0.8f)
            {
                handT += Time.deltaTime;
                hand += 140f * Time.deltaTime;
                float a = hand * Mathf.Deg2Rad;
                Fire(transform.position, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 7f, 0.3f, new Color(1f, 0.9f, 0.5f));
                yield return new WaitForSeconds(0.06f);
            }
            yield return new WaitForSeconds(0.4f);
        }
    }
}
