using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// 大招/技能特效工具：爆闪、粒子、冲击波、闪电柱、顿帧、震屏。
/// 全部运行时生成，不依赖预制体，贴图从 Resources 取 Star/Bullet。
public class UltVfx : MonoBehaviour
{
    public static UltVfx Instance { get; private set; }

    static Sprite star;
    static Sprite bulletSpr;
    static Sprite fxSpark;
    static Sprite fxBlast;
    static Sprite fxOrb;
    static Sprite fxRing;
    static Canvas flashCanvas;
    static Image flashImg;
    static float nextShakeEnd;
    static Vector3 camBase;
    static bool camBaseSet;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("UltVfx");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<UltVfx>();
    }

    void Awake()
    {
        Instance = this;
        star = Resources.Load<Sprite>("Star");
        bulletSpr = Resources.Load<Sprite>("Bullet");
        fxSpark = Resources.Load<Sprite>("Fx_Spark");
        fxBlast = Resources.Load<Sprite>("Fx_Blast");
        fxOrb = Resources.Load<Sprite>("Fx_Orb");
        fxRing = Resources.Load<Sprite>("Fx_Ring");
        EnsureFlashCanvas();
    }

    /// 专用攻击特效贴图（新生成，不复用 Star/Bullet）
    static Sprite SparkSpr => fxSpark != null ? fxSpark : Resources.Load<Sprite>("Fx_Spark");
    static Sprite BlastSpr => fxBlast != null ? fxBlast : Resources.Load<Sprite>("Fx_Blast");
    static Sprite OrbSpr => fxOrb != null ? fxOrb : Resources.Load<Sprite>("Fx_Orb");
    static Sprite RingSpr => fxRing != null ? fxRing : Resources.Load<Sprite>("Fx_Ring");
    static Sprite Star => star != null ? star : Resources.Load<Sprite>("Star");
    static Sprite BulletSpr => bulletSpr != null ? bulletSpr : Resources.Load<Sprite>("Bullet");

    // ---------- 屏幕反馈 ----------

    static void EnsureFlashCanvas()
    {
        if (flashCanvas != null && flashImg != null) return;
        var go = new GameObject("UltFlashCanvas");
        DontDestroyOnLoad(go);
        flashCanvas = go.AddComponent<Canvas>();
        flashCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        flashCanvas.sortingOrder = 200;
        var imgGo = new GameObject("Flash");
        imgGo.transform.SetParent(go.transform, false);
        flashImg = imgGo.AddComponent<Image>();
        flashImg.color = new Color(0, 0, 0, 0);
        flashImg.raycastTarget = false;
        var rt = flashImg.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// 全屏闪色（柔和上限，避免晃眼）
    public const float MaxFlashAlpha = 0.14f;
    const float FlashMinInterval = 0.12f;
    static float lastFlashTime;
    static float lastFlashAlpha;

    /// 全屏闪色：自动压暗 + 限频，time 为淡出时长（秒）
    public static void ScreenFlash(Color color, float time)
    {
        if (Instance == null) return;
        EnsureFlashCanvas();

        float a = Mathf.Clamp(color.a, 0f, MaxFlashAlpha);
        // 连续闪：后一次更暗，避免连闪刺眼
        if (Time.unscaledTime - lastFlashTime < FlashMinInterval)
            a *= 0.35f;
        if (a < 0.02f) return;
        lastFlashTime = Time.unscaledTime;
        lastFlashAlpha = a;

        var c = new Color(color.r, color.g, color.b, a);
        Instance.StopCoroutine("FlashRoutine");
        Instance.StartCoroutine(Instance.FlashRoutine(c, Mathf.Max(0.08f, time * 0.85f)));
    }

    IEnumerator FlashRoutine(Color color, float time)
    {
        EnsureFlashCanvas();
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            // 缓出，峰值只保持很短
            float u = t / time;
            float a = color.a * (1f - u) * (1f - u);
            if (flashImg != null) flashImg.color = new Color(color.r, color.g, color.b, a);
            yield return null;
        }
        if (flashImg != null) flashImg.color = new Color(0, 0, 0, 0);
    }

    /// 震屏：dur 秒，amp 世界单位幅度
    public static void Shake(float dur, float amp)
    {
        if (Instance == null) return;
        Instance.StartCoroutine(Instance.ShakeRoutine(dur, amp));
    }

    IEnumerator ShakeRoutine(float dur, float amp)
    {
        var cam = Camera.main;
        if (cam == null) yield break;
        if (!camBaseSet || Time.time > nextShakeEnd)
        {
            camBase = cam.transform.localPosition;
            camBaseSet = true;
        }
        float end = Time.unscaledTime + dur;
        nextShakeEnd = Mathf.Max(nextShakeEnd, end);
        Vector3 start = cam.transform.localPosition;
        while (Time.unscaledTime < end)
        {
            float left = end - Time.unscaledTime;
            float k = amp * (left / dur);
            cam.transform.localPosition = camBase + (Vector3)(Random.insideUnitCircle * k);
            yield return null;
        }
        // 只有所有震屏都结束才复位
        if (Time.unscaledTime >= nextShakeEnd && cam != null)
            cam.transform.localPosition = camBase;
    }

    /// 顿帧：短暂压低 timeScale，增强打击感
    public static void HitStop(float duration, float scale = 0.08f)
    {
        if (Instance == null) return;
        Instance.StartCoroutine(Instance.HitStopRoutine(duration, scale));
    }

    IEnumerator HitStopRoutine(float duration, float scale)
    {
        float prev = Time.timeScale;
        Time.timeScale = Mathf.Clamp(scale, 0.01f, 1f);
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = prev;
    }

    // ---------- 精灵基础件 ----------

    static SpriteRenderer MakeSr(Vector3 pos, float scale, Color color, int order = 30, Sprite sprite = null)
    {
        var go = new GameObject("Fx");
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite != null ? sprite
            : (BlastSpr != null ? BlastSpr : (SparkSpr != null ? SparkSpr : (Star != null ? Star : BulletSpr)));
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    /// 爆闪：放大 + 淡出
    public static void Boom(Vector3 pos, float startScale, Color color, float life = 0.35f, float endScale = 2.2f)
    {
        if (Instance == null) return;
        var spr = BlastSpr != null ? BlastSpr : SparkSpr;
        var sr = MakeSr(pos, startScale, color, 30, spr);
        Instance.StartCoroutine(Instance.ScaleFade(sr, startScale, endScale, life));
    }

    /// 扩散冲击波环
    public static void Ring(Vector3 pos, Color color, float startR, float endR, float life = 0.45f, int segs = 28)
    {
        if (Instance == null) return;
        Instance.StartCoroutine(Instance.RingRoutine(pos, color, startR, endR, life, segs));
    }

    IEnumerator RingRoutine(Vector3 pos, Color c, float r0, float r1, float life, int segs)
    {
        // 有专用环贴图时用单图放大；否则用点阵
        if (RingSpr != null)
        {
            var ring = MakeSr(pos, r0 * 2f, c, 30, RingSpr);
            float t0 = 0f;
            while (t0 < life && ring != null)
            {
                t0 += Time.deltaTime;
                float u = t0 / life;
                float r = Mathf.Lerp(r0, r1, u);
                ring.transform.localScale = Vector3.one * (r * 2f);
                ring.color = new Color(c.r, c.g, c.b, 1f - u);
                yield return null;
            }
            if (ring != null) Destroy(ring.gameObject);
            yield break;
        }

        var srs = new SpriteRenderer[segs];
        var spr = SparkSpr != null ? SparkSpr : (OrbSpr != null ? OrbSpr : Star);
        for (int i = 0; i < segs; i++)
        {
            float ang = i * (360f / segs) * Mathf.Deg2Rad;
            srs[i] = MakeSr(pos + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * r0, 0.22f, c, 30, spr);
        }
        float t = 0f;
        while (t < life)
        {
            t += Time.deltaTime;
            float u = t / life;
            float r = Mathf.Lerp(r0, r1, u);
            float a = 1f - u;
            for (int i = 0; i < segs; i++)
            {
                if (srs[i] == null) continue;
                float ang = i * (360f / segs) * Mathf.Deg2Rad;
                srs[i].transform.position = pos + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * r;
                srs[i].color = new Color(c.r, c.g, c.b, a);
            }
            yield return null;
        }
        for (int i = 0; i < segs; i++)
            if (srs[i] != null) Destroy(srs[i].gameObject);
    }

    /// 径向粒子爆发
    public static void Burst(Vector3 pos, Color color, int count = 12, float speed = 6f, float life = 0.5f, float size = 0.28f)
    {
        if (Instance == null) return;
        Instance.StartCoroutine(Instance.BurstRoutine(pos, color, count, speed, life, size));
    }

    IEnumerator BurstRoutine(Vector3 pos, Color c, int count, float speed, float life, float size)
    {
        var srs = new SpriteRenderer[count];
        var vels = new Vector2[count];
        var spr = SparkSpr != null ? SparkSpr : (OrbSpr != null ? OrbSpr : Star);
        for (int i = 0; i < count; i++)
        {
            float ang = (i / (float)count) * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
            float sp = speed * Random.Range(0.6f, 1.25f);
            vels[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * sp;
            srs[i] = MakeSr(pos, size * Random.Range(0.7f, 1.2f), c, 30, spr);
        }
        float t = 0f;
        while (t < life)
        {
            t += Time.deltaTime;
            float u = t / life;
            for (int i = 0; i < count; i++)
            {
                if (srs[i] == null) continue;
                srs[i].transform.position += (Vector3)(vels[i] * Time.deltaTime * (1f - u * 0.6f));
                srs[i].color = new Color(c.r, c.g, c.b, 1f - u);
                srs[i].transform.localScale *= (1f - Time.deltaTime * 0.8f);
            }
            yield return null;
        }
        for (int i = 0; i < count; i++)
            if (srs[i] != null) Destroy(srs[i].gameObject);
    }

    /// 落雷柱：锯齿闪电 + 闪光 + 残迹
    public static void LightningColumn(float x, Color color, float topY = 5.2f, float botY = -4.5f, int segs = 10)
    {
        if (Instance == null) return;
        Instance.StartCoroutine(Instance.LightningColumnRoutine(x, color, topY, botY, segs));
    }

    IEnumerator LightningColumnRoutine(float x, Color c, float y0, float y1, int segs)
    {
        var srs = new SpriteRenderer[segs];
        var spr = SparkSpr != null ? SparkSpr : OrbSpr;
        for (int i = 0; i < segs; i++)
        {
            float t = i / (float)(segs - 1);
            float y = Mathf.Lerp(y0, y1, t);
            float jag = (i == 0 || i == segs - 1) ? 0f : Random.Range(-0.28f, 0.28f);
            srs[i] = MakeSr(new Vector3(x + jag, y, 0f), 0.38f, c, 30, spr);
            srs[i].transform.localScale = new Vector3(0.22f, 0.55f, 1f);
        }
        // 顶部炸点
        Boom(new Vector3(x, y0, 0f), 0.7f, c, 0.28f, 1.6f);
        Boom(new Vector3(x, y1, 0f), 0.55f, c * 0.9f, 0.22f, 1.4f);
        Burst(new Vector3(x, y1, 0f), c, 8, 5f, 0.35f, 0.22f);

        float life = 0.22f;
        float t2 = 0f;
        while (t2 < life)
        {
            t2 += Time.deltaTime;
            float a = 1f - t2 / life;
            for (int i = 0; i < segs; i++)
                if (srs[i] != null) srs[i].color = new Color(c.r, c.g, c.b, a);
            yield return null;
        }
        for (int i = 0; i < segs; i++)
            if (srs[i] != null) Destroy(srs[i].gameObject);
    }

    /// 闪电链：两点之间锯齿线
    public static void Bolt(Vector3 a, Vector3 b, Color color, int segs = 8, float life = 0.28f)
    {
        if (Instance == null) return;
        Instance.StartCoroutine(Instance.BoltRoutine(a, b, color, segs, life));
    }

    IEnumerator BoltRoutine(Vector3 a, Vector3 b, Color c, int segs, float life)
    {
        var srs = new SpriteRenderer[segs];
        Vector3 dir = b - a;
        var spr = SparkSpr != null ? SparkSpr : OrbSpr;
        for (int i = 0; i < segs; i++)
        {
            float t = (i + 0.5f) / segs;
            Vector3 p = Vector3.Lerp(a, b, t);
            if (i > 0 && i < segs - 1)
            {
                Vector3 perp = new Vector3(-dir.y, dir.x, 0f).normalized;
                p += perp * Random.Range(-0.25f, 0.25f);
            }
            srs[i] = MakeSr(p, 0.3f, c, 30, spr);
            srs[i].transform.localScale = new Vector3(0.2f, 0.45f, 1f);
        }
        Boom(b, 0.5f, c, 0.25f, 1.3f);
        float t2 = 0f;
        while (t2 < life)
        {
            t2 += Time.deltaTime;
            float al = 1f - t2 / life;
            for (int i = 0; i < segs; i++)
                if (srs[i] != null) srs[i].color = new Color(c.r, c.g, c.b, al);
            yield return null;
        }
        for (int i = 0; i < segs; i++)
            if (srs[i] != null) Destroy(srs[i].gameObject);
    }

    /// 垂直光柱（太阳坠落 / 月光）
    public static void Beam(Vector3 center, float width, float height, Color color, float life = 0.35f)
    {
        if (Instance == null) return;
        Instance.StartCoroutine(Instance.BeamRoutine(center, width, height, color, life));
    }

    IEnumerator BeamRoutine(Vector3 center, float w, float h, Color c, float life)
    {
        var srs = new SpriteRenderer[6];
        for (int i = 0; i < 6; i++)
        {
            float y = center.y + (i - 2.5f) * (h / 6f);
            srs[i] = MakeSr(new Vector3(center.x + Random.Range(-0.1f, 0.1f), y, 0f), 1f, c);
            srs[i].transform.localScale = new Vector3(w * Random.Range(0.7f, 1.1f), h / 7f, 1f);
        }
        float t = 0f;
        while (t < life)
        {
            t += Time.deltaTime;
            float al = 1f - t / life;
            for (int i = 0; i < 6; i++)
                if (srs[i] != null)
                {
                    var sc = srs[i].transform.localScale;
                    srs[i].transform.localScale = new Vector3(sc.x * (1f + Time.deltaTime * 0.4f), sc.y, 1f);
                    srs[i].color = new Color(c.r, c.g, c.b, al);
                }
            yield return null;
        }
        for (int i = 0; i < 6; i++)
            if (srs[i] != null) Destroy(srs[i].gameObject);
    }

    /// 残影：拷贝 SpriteRenderer 淡出（用于高速冲刺）
    public static void Afterimage(Vector3 pos, Vector3 scale, Sprite sprite, Color color, float life = 0.35f)
    {
        if (Instance == null) return;
        var go = new GameObject("Afterimage");
        go.transform.position = pos;
        go.transform.localScale = scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite != null ? sprite : (Star != null ? Star : BulletSpr);
        sr.color = color;
        sr.sortingOrder = 15;
        Instance.StartCoroutine(Instance.FadeDestroy(sr, life));
    }

    public static void Afterimage(Vector3 pos, Vector3 scale, Color color, float life = 0.35f)
    {
        Afterimage(pos, scale, null, color, life);
    }

    IEnumerator FadeDestroy(SpriteRenderer sr, float life)
    {
        float t = 0f;
        Color c = sr.color;
        while (t < life && sr != null)
        {
            t += Time.deltaTime;
            float a = 1f - t / life;
            sr.color = new Color(c.r, c.g, c.b, a * 0.7f);
            yield return null;
        }
        if (sr != null) Destroy(sr.gameObject);
    }

    IEnumerator ScaleFade(SpriteRenderer sr, float s0, float s1, float life)
    {
        float t = 0f;
        Color c = sr.color;
        while (t < life && sr != null)
        {
            t += Time.deltaTime;
            float u = t / life;
            float s = Mathf.Lerp(s0, s1, u);
            sr.transform.localScale = Vector3.one * s;
            sr.color = new Color(c.r, c.g, c.b, 1f - u);
            yield return null;
        }
        if (sr != null) Destroy(sr.gameObject);
    }

    /// 持续喷吐：每隔 interval 在 pos 附近炸一下
    public static void PulseBoom(Vector3 pos, Color color, float interval, int times, float scale = 0.5f)
    {
        if (Instance == null) return;
        Instance.StartCoroutine(Instance.PulseBoomRoutine(pos, color, interval, times, scale));
    }

    IEnumerator PulseBoomRoutine(Vector3 pos, Color c, float interval, int times, float scale)
    {
        for (int i = 0; i < times; i++)
        {
            Boom(pos + (Vector3)(Random.insideUnitCircle * 0.35f), scale * Random.Range(0.7f, 1.3f), c);
            yield return new WaitForSeconds(interval);
        }
    }

    /// 机体呼吸光环：锁定期间机身颜色脉冲 + 周围火花
    public static IEnumerator ShipAura(Transform ship, SpriteRenderer sr, Color c, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            if (sr != null)
            {
                float pulse = 0.72f + 0.28f * Mathf.Sin(t * 14f);
                sr.color = new Color(c.r, c.g, c.b, pulse);
            }
            if (ship != null && Random.value > 0.45f)
            {
                Vector3 p = ship.position + (Vector3)(Random.insideUnitCircle * 0.6f);
                Boom(p, 0.22f, c, 0.22f, 0.7f);
            }
            yield return new WaitForSeconds(0.04f);
        }
        if (sr != null) sr.color = Color.white;
    }

    /// 给当前玩家加一层短暂轮廓光晕（半透明放大副本）
    public static void Halo(Transform ship, Color color, float life = 0.8f, float grow = 1.6f)
    {
        if (Instance == null || ship == null) return;
        var psr = ship.GetComponent<SpriteRenderer>();
        var go = new GameObject("Halo");
        go.transform.position = ship.position;
        go.transform.localScale = ship.localScale * 1.05f;
        var sr = go.AddComponent<SpriteRenderer>();
        if (psr != null && psr.sprite != null) sr.sprite = psr.sprite;
        else sr.sprite = Star != null ? Star : BulletSpr;
        sr.color = new Color(color.r, color.g, color.b, 0.55f);
        sr.sortingOrder = 12;
        Instance.StartCoroutine(Instance.HaloRoutine(go.transform, sr, life, grow));
    }

    IEnumerator HaloRoutine(Transform tr, SpriteRenderer sr, float life, float grow)
    {
        float t = 0f;
        Vector3 s0 = tr.localScale;
        Color c = sr.color;
        while (t < life && sr != null)
        {
            t += Time.deltaTime;
            float u = t / life;
            tr.localScale = s0 * Mathf.Lerp(1f, grow, u);
            sr.color = new Color(c.r, c.g, c.b, (1f - u) * 0.55f);
            yield return null;
        }
        if (sr != null) Destroy(sr.gameObject);
    }
}
