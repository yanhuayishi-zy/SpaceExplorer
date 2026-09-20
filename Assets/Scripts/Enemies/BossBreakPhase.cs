using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// Boss 专属机制：45% 生命锁血 → 破防条 → 破防窗口 → 二阶段狂暴
public class BossBreakPhase : MonoBehaviour
{
    public const float LockHpRatio = 0.45f;   // 低于此比例锁血
    public const float VulnerableSec = 4f;    // 破防后易伤时间
    public const float VulnerableMul = 1.6f;  // 易伤倍率

    Enemy enemy;
    ZodiacBossAI ai;
    int startHp;
    bool lockEngaged;
    bool broken;
    float breakProgress;
    float breakNeed;
    float vulnerableUntil;
    int shieldHpFloor;
    Image barFill;
    GameObject barRoot;

    public void Setup(Enemy e, ZodiacBossAI bossAi)
    {
        enemy = e;
        ai = bossAi;
        startHp = Mathf.Max(1, e.health);
        shieldHpFloor = Mathf.RoundToInt(startHp * LockHpRatio);
        breakNeed = Mathf.Clamp(Mathf.RoundToInt(startHp * GameBalance.BossBreakHpRatio),
            GameBalance.BossBreakMin, GameBalance.BossBreakMax);
        BuildBar();
    }

    void BuildBar()
    {
        barRoot = new GameObject("BossBreakBar");
        var canvasGo = new GameObject("BossBreakCanvas");
        Object.DontDestroyOnLoad(canvasGo);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        MobileTuning.ConfigureCanvas(scaler);
        barRoot.transform.SetParent(canvasGo.transform, false);

        var bg = new GameObject("Bg");
        bg.transform.SetParent(barRoot.transform, false);
        var bimg = bg.AddComponent<Image>();
        bimg.color = new Color(0.05f, 0.06f, 0.1f, 0.8f);
        bimg.raycastTarget = false;
        var rt = bimg.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = MobileTuning.Active ? new Vector2(0, -246f) : new Vector2(0, -88f);
        rt.sizeDelta = MobileTuning.Active ? new Vector2(760f, 24f) : new Vector2(420f, 18f);

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(bg.transform, false);
        barFill = fillGo.AddComponent<Image>();
        barFill.color = new Color(1f, 0.75f, 0.2f, 0.95f);
        barFill.raycastTarget = false;
        barFill.type = Image.Type.Filled;
        barFill.fillMethod = Image.FillMethod.Horizontal;
        barFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        barFill.fillAmount = 0f;
        var frt = barFill.rectTransform;
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = new Vector2(2f, 2f);
        frt.offsetMax = new Vector2(-2f, -2f);

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(barRoot.transform, false);
        var text = labelGo.AddComponent<Text>();
        text.font = PixelUi.Font;
        text.fontSize = MobileTuning.Active ? 22 : 14;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(1f, 0.9f, 0.5f, 0.9f);
        text.text = "Boss 护盾 · 攻击以破防";
        text.raycastTarget = false;
        var trt = text.rectTransform;
        trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = MobileTuning.Active ? new Vector2(0, -214f) : new Vector2(0, -72f);
        trt.sizeDelta = MobileTuning.Active ? new Vector2(760f, 28f) : new Vector2(420f, 20f);

        barRoot.SetActive(false);
    }

    /// 由 Enemy.TakeDamage 前调用：返回实际生效伤害
    public int ModifyDamage(int dmg)
    {
        if (enemy == null || broken) return dmg;

        if (!lockEngaged)
        {
            int healthToFloor = Mathf.Max(0, enemy.health - shieldHpFloor);
            if (dmg < healthToFloor) return dmg;

            int overflow = Mathf.Max(0, dmg - healthToFloor);
            EngageLock();
            AddBreakDamage(overflow);
            return healthToFloor;
        }

        // 锁血期：伤害全部进入破防条，不再偷扣生命。
        AddBreakDamage(dmg);
        return 0;
    }

    void AddBreakDamage(int damage)
    {
        if (damage <= 0 || broken) return;
        breakProgress += damage;
        RefreshBar();
        if (barFill != null) barFill.color = new Color(1f, 0.55f, 0.15f, 0.95f);

        if (breakProgress >= breakNeed)
            StartCoroutine(BreakRoutine());
    }

    void EngageLock()
    {
        if (lockEngaged || broken) return;
        lockEngaged = true;
        if (enemy != null && enemy.health < shieldHpFloor) enemy.health = shieldHpFloor;
        if (barRoot != null) barRoot.SetActive(true);
        RefreshBar();
        GameFx.PhaseShift(transform.position, new Color(1f, 0.7f, 0.2f));
        UltVfx.ScreenFlash(new Color(1f, 0.8f, 0.3f, 0.3f), 0.25f);
    }

    void RefreshBar()
    {
        if (barFill == null) return;
        barFill.fillAmount = breakNeed <= 0f ? 0f : Mathf.Clamp01(breakProgress / breakNeed);
    }

    void Update()
    {
        if (enemy == null) return;
        if (!lockEngaged && !broken && enemy.health <= shieldHpFloor)
        {
            enemy.health = shieldHpFloor;
            EngageLock();
        }
    }

    IEnumerator BreakRoutine()
    {
        broken = true;
        lockEngaged = false;
        UltVfx.HitStop(0.08f, 0.1f);
        UltVfx.Shake(0.45f, 0.14f);
        UltVfx.Ring(transform.position, new Color(1f, 0.9f, 0.35f), 0.5f, 6f, 0.5f, 28);
        UltVfx.Boom(transform.position, 2f, new Color(1f, 0.95f, 0.5f), 0.45f, 5f);
        UltVfx.Burst(transform.position, new Color(1f, 0.85f, 0.3f), 22, 9f, 0.5f);
        UltVfx.ScreenFlash(new Color(1f, 0.9f, 0.4f, 0.4f), 0.3f);
        GameFx.PhaseShift(transform.position, new Color(1f, 0.4f, 0.25f));

        if (barRoot != null)
        {
            var texts = barRoot.GetComponentsInChildren<Text>();
            foreach (var t in texts) if (t != null) t.text = "破防！";
            if (barFill != null) barFill.color = new Color(0.4f, 1f, 0.55f, 0.95f);
        }

        // 强制进入二阶段攻击
        if (ai != null) ai.ForceEnrage();

        float t2 = 0f;
        vulnerableUntil = Time.time + VulnerableSec;
        while (t2 < VulnerableSec)
        {
            t2 += Time.deltaTime;
            yield return null;
        }
        vulnerableUntil = 0f;
        if (barRoot != null) barRoot.SetActive(false);
    }

    void OnDestroy()
    {
        if (barRoot != null)
        {
            var parent = barRoot.transform.parent;
            if (parent != null) Destroy(parent.gameObject);
            else Destroy(barRoot);
            barRoot = null;
        }
    }

    public bool IsVulnerable => broken && Time.time < vulnerableUntil;
}
