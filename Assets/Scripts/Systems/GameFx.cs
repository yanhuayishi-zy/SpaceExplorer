using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// 战斗反馈：伤害飘字、敌人死亡、受击/拾取爆闪
public class GameFx : MonoBehaviour
{
    public static GameFx Instance { get; private set; }

    static Sprite star;
    static Sprite whitePx;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("GameFx");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<GameFx>();
    }

    void Awake()
    {
        Instance = this;
        star = Resources.Load<Sprite>("Star");
        EnsureWhitePx();
    }

    static void EnsureWhitePx()
    {
        if (whitePx != null) return;
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var px = new Color[16];
        for (int i = 0; i < px.Length; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();
        whitePx = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
    }

    /// 伤害飘字
    public static void DamageNumber(Vector3 worldPos, int dmg, Color color, bool crit = false)
    {
        if (Instance == null || dmg <= 0) return;
        Instance.StartCoroutine(Instance.DamageNumberCo(worldPos, dmg, color, crit));
    }

    IEnumerator DamageNumberCo(Vector3 worldPos, int dmg, Color color, bool crit)
    {
        // 世界坐标画布跟随
        var canvasGo = new GameObject("DmgNum");
        canvasGo.transform.position = worldPos + (Vector3)(Random.insideUnitCircle * 0.15f);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 40;
        var rt = canvasGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200f, 60f);
        // 相对相机大约合适
        rt.localScale = Vector3.one * 0.012f;

        var tGo = new GameObject("T");
        tGo.transform.SetParent(canvasGo.transform, false);
        var text = tGo.AddComponent<Text>();
        text.font = PixelUi.Font;
        text.fontSize = crit ? 36 : 28;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.text = crit ? ("!" + dmg) : dmg.ToString();
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        var trt = text.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        float life = crit ? 0.55f : 0.4f;
        float t = 0f;
        Vector3 start = canvasGo != null ? canvasGo.transform.position : Vector3.zero;
        while (t < life && text != null && !text.Equals(null) && canvasGo != null && !canvasGo.Equals(null))
        {
            t += Time.deltaTime;
            float u = t / life;
            canvasGo.transform.position = start + Vector3.up * (u * 0.9f) + (Vector3)(Random.insideUnitCircle * 0.02f);
            float a = 1f - u;
            text.color = new Color(color.r, color.g, color.b, a);
            // 起跳弹一下
            float s = crit
                ? (u < 0.2f ? Mathf.Lerp(1.4f, 1f, u / 0.2f) : 1f)
                : (u < 0.25f ? Mathf.Lerp(1.2f, 1f, u / 0.25f) : 1f);
            text.transform.localScale = Vector3.one * s;
            yield return null;
        }
        if (canvasGo != null && !canvasGo.Equals(null)) Destroy(canvasGo);
    }

    /// 敌人死亡：分档爆炸 + 冲击波
    public static void EnemyDeath(Vector3 pos, float size, Color tint, int scoreValue = 0)
    {
        if (Instance == null) return;
        bool boss = scoreValue >= 800 || size >= 1.8f;
        if (boss)
        {
            BossDeath(pos, tint, size);
            return;
        }

        float s = Mathf.Clamp(size * 1.4f, 0.55f, 1.6f);
        UltVfx.Boom(pos, s, tint, 0.32f, s * 2.4f);
        UltVfx.Burst(pos, tint, boss ? 20 : 10, 5.5f, 0.4f, 0.26f);
        UltVfx.Ring(pos, tint * 0.85f, s * 0.4f, s * 2.2f, 0.3f, 16);
        UltVfx.Shake(0.12f, 0.04f);
    }

    /// Boss 死亡：多段爆 + 顿帧 + 闪屏
    public static void BossDeath(Vector3 pos, Color tint, float size)
    {
        if (Instance == null) return;
        Instance.StartCoroutine(Instance.BossDeathCo(pos, tint, size));
    }

    IEnumerator BossDeathCo(Vector3 pos, Color tint, float size)
    {
        UltVfx.Shake(0.8f, 0.18f);
        UltVfx.HitStop(0.12f, 0.08f);
        UltVfx.ScreenFlash(new Color(tint.r, tint.g, tint.b, 0.45f), 0.35f);
        for (int i = 0; i < 5; i++)
        {
            Vector3 p = pos + (Vector3)(Random.insideUnitCircle * size * 0.6f);
            float s = size * Random.Range(0.6f, 1.1f);
            UltVfx.Boom(p, s, tint, 0.4f, s * 2.5f);
            UltVfx.Burst(p, tint, 12, 7f, 0.45f, 0.3f);
            yield return new WaitForSeconds(0.08f);
        }
        UltVfx.Ring(pos, tint, 0.5f, 7f, 0.55f, 32);
        UltVfx.Boom(pos, size * 1.5f, Color.white * 0.9f, 0.45f, size * 4f);
        UltVfx.Burst(pos, tint, 28, 10f, 0.55f, 0.35f);
        UltVfx.ScreenFlash(new Color(1f, 0.95f, 0.7f, 0.35f), 0.25f);
        UltVfx.Shake(0.4f, 0.12f);
    }

    /// 单发命中爆点
    public static void HitPop(Vector3 pos, Color color)
    {
        if (Instance == null) return;
        UltVfx.Boom(pos, 0.22f, color, 0.15f, 0.65f);
    }

    /// 拾取道具
    public static void Pickup(Vector3 pos, Color color)
    {
        if (Instance == null) return;
        UltVfx.Boom(pos, 0.5f, color, 0.3f, 1.6f);
        UltVfx.Ring(pos, color, 0.2f, 1.4f, 0.28f);
        UltVfx.Burst(pos, color, 8, 4f, 0.3f, 0.22f);
    }

    /// 玩家受击
    public static void PlayerHit(Vector3 pos)
    {
        if (Instance == null) return;
        UltVfx.Boom(pos, 0.9f, new Color(1f, 0.35f, 0.3f), 0.35f, 2.2f);
        UltVfx.Ring(pos, new Color(1f, 0.4f, 0.35f), 0.3f, 2.5f, 0.3f);
        UltVfx.Burst(pos, new Color(1f, 0.45f, 0.4f), 12, 6f, 0.35f);
        UltVfx.Shake(0.28f, 0.1f);
        UltVfx.ScreenFlash(new Color(1f, 0.15f, 0.12f, 0.28f), 0.18f);
    }

    /// 敌方弹命中（打掉子弹时小爆）
    public static void BulletClash(Vector3 pos)
    {
        if (Instance == null) return;
        UltVfx.Boom(pos, 0.28f, new Color(0.7f, 0.9f, 1f), 0.16f, 0.8f);
    }

    /// Boss 狂暴/相位
    public static void PhaseShift(Vector3 pos, Color tint)
    {
        if (Instance == null) return;
        UltVfx.Ring(pos, tint, 0.5f, 5f, 0.5f, 28);
        UltVfx.Boom(pos, 1.5f, tint, 0.4f, 4f);
        UltVfx.Burst(pos, tint, 18, 7f, 0.45f);
        UltVfx.Shake(0.3f, 0.1f);
        UltVfx.ScreenFlash(new Color(tint.r, tint.g, tint.b, 0.22f), 0.2f);
    }
}
