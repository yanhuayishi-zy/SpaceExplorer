using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// 成就/里程碑：本地解锁，发金币，可挂称号
public static class Achievements
{
    public const string KeyPrefix = "ACH_";
    public const string KeyTitle = "SE_Title";

    public class Def
    {
        public string id;
        public string name;
        public string desc;
        public int coinReward;
        public string title; // 称号，空则无

        public Def(string id, string name, string desc, int coinReward, string title = "")
        {
            this.id = id;
            this.name = name;
            this.desc = desc;
            this.coinReward = coinReward;
            this.title = title;
        }
    }

    public static readonly Def[] All =
    {
        new Def("clear_1",  "初战告捷",   "首次通关白羊宫", 150, "白羊新锐"),
        new Def("clear_2",  "金牛之力",   "通关金牛宫", 180, "牛角冲阵"),
        new Def("clear_3",  "双子镜像",   "通关双子宫", 200, "双子星辉"),
        new Def("clear_4",  "硬壳护卫",   "通关巨蟹宫", 220, "甲壳壁垒"),
        new Def("clear_5",  "狮王咆哮",   "通关狮子宫", 260, "鬃焰王者"),
        new Def("clear_6",  "半壁黄道",   "通关至处女宫", 300, "黄道行者"),
        new Def("clear_7",  "天秤之衡",   "通关天秤宫", 340, "审判之衡"),
        new Def("clear_8",  "尾针剧毒",   "通关天蝎宫", 380, "暗影毒刺"),
        new Def("clear_9",  "万箭穿云",   "通关射手宫", 420, "贤者之弓"),
        new Def("clear_10", "礁石攀者",   "通关摩羯宫", 460, "深海山羊"),
        new Def("clear_11", "星泉倾泻",   "通关水瓶宫", 500, "斟酒者"),
        new Def("clear_12", "双鱼缠绕",   "通关双鱼宫", 550, "海缚双鱼"),
        new Def("clear_13", "十三宫制霸", "击败时间泰坦克洛诺斯", 800, "黄道征服者"),
        new Def("endless_10", "波次十",   "无尽模式到达第 10 波", 200, "深空浪客"),
        new Def("endless_20", "波次二十", "无尽模式到达第 20 波", 400, "虚空猎手"),
        new Def("endless_35", "波次三十五","无尽模式到达第 35 波", 700, "不灭星火"),
        new Def("combo_15", "连击十五",   "单局连击达到 15", 150, "连弹艺人"),
        new Def("combo_30", "连击三十",   "单局连击达到 30", 350, "弹幕风暴"),
        new Def("combo_50", "连击五十",   "单局连击达到 50", 600, "无双"),
        new Def("boss_1",   "弑神者",     "首次击败任意星座 Boss", 200, "弑神者"),
    };

    public static bool IsUnlocked(string id) => PlayerPrefs.GetInt(KeyPrefix + id, 0) == 1;

    public static bool IsClaimed(string id) => PlayerPrefs.GetInt(KeyPrefix + id + "_C", 0) == 1;

    /// 已解锁且未领奖
    public static bool CanClaim(string id) => IsUnlocked(id) && !IsClaimed(id);

    public static string CurrentTitle => PlayerPrefs.GetString(KeyTitle, "");

    public static void SetTitle(string title)
    {
        if (string.IsNullOrEmpty(title)) return;
        PlayerPrefs.SetString(KeyTitle, title);
        PlayerPrefs.Save();
    }

    /// 领取成就金币；成功返回 true
    public static bool TryClaim(string id)
    {
        if (!CanClaim(id)) return false;
        Def def = Find(id);
        if (def == null) return false;
        PlayerPrefs.SetInt(KeyPrefix + id + "_C", 1);
        PlayerPrefs.Save();
        if (def.coinReward > 0)
        {
            ShipMeta.AddCoins(def.coinReward);
            ShipShopUI.RefreshCoins();
        }
        if (!string.IsNullOrEmpty(def.title)) SetTitle(def.title);
        return true;
    }

    public static int ClaimableCount()
    {
        int n = 0;
        for (int i = 0; i < All.Length; i++)
            if (CanClaim(All[i].id)) n++;
        return n;
    }

    /// 解锁；已解锁返回 false。只解锁+弹条，金币需去成就页领取
    public static bool TryUnlock(string id)
    {
        if (string.IsNullOrEmpty(id) || IsUnlocked(id)) return false;
        PlayerPrefs.SetInt(KeyPrefix + id, 1);
        PlayerPrefs.Save();

        Def def = Find(id);
        if (def == null) return true;

        // 称号仍自动挂上；金币改手动领取
        if (!string.IsNullOrEmpty(def.title)) SetTitle(def.title);
        AchievementToasts.Show(def);
        return true;
    }

    static Def Find(string id)
    {
        for (int i = 0; i < All.Length; i++)
            if (All[i].id == id) return All[i];
        return null;
    }

    // ---- 事件钩子 ----

    public static void NotifyCampaignClear(int level)
    {
        if (level < 1) return;
        // 每关一个称号
        for (int lv = 1; lv <= Mathf.Min(level, ZodiacLevels.Count); lv++)
            TryUnlock("clear_" + lv);
    }

    public static void NotifyEndlessWave(int wave)
    {
        if (wave >= 10) TryUnlock("endless_10");
        if (wave >= 20) TryUnlock("endless_20");
        if (wave >= 35) TryUnlock("endless_35");
    }

    public static void NotifyCombo(int combo)
    {
        if (combo >= 15) TryUnlock("combo_15");
        if (combo >= 30) TryUnlock("combo_30");
        if (combo >= 50) TryUnlock("combo_50");
    }

    public static void NotifyBossKilled()
    {
        TryUnlock("boss_1");
    }
}

/// 解锁弹条
public class AchievementToasts : MonoBehaviour
{
    static AchievementToasts instance;
    readonly Queue<(string title, string desc, int coins)> queue = new Queue<(string, string, int)>();
    bool showing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (instance != null) return;
        var go = new GameObject("AchievementToasts");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<AchievementToasts>();
    }

    public static void Show(Achievements.Def def)
    {
        if (instance == null || def == null) return;
        instance.queue.Enqueue((def.name, def.desc, def.coinReward));
        if (!instance.showing) instance.StartCoroutine(instance.Drain());
    }

    System.Collections.IEnumerator Drain()
    {
        showing = true;
        while (queue.Count > 0)
        {
            var item = queue.Dequeue();
            yield return ToastCo(item.title, item.desc, item.coins);
            yield return new WaitForSecondsRealtime(0.15f);
        }
        showing = false;
    }

    System.Collections.IEnumerator ToastCo(string title, string desc, int coins)
    {
        var canvasGo = new GameObject("AchToast");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 350;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        MobileTuning.ConfigureCanvas(scaler);

        var bgGo = new GameObject("Bg");
        bgGo.transform.SetParent(canvasGo.transform, false);
        var bg = bgGo.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.12f, 0.18f, 0.92f);
        bg.raycastTarget = false;
        var rt = bg.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0, -90f);
        rt.sizeDelta = new Vector2(560f, 96f);

        var bar = new GameObject("Bar");
        bar.transform.SetParent(bgGo.transform, false);
        var bimg = bar.AddComponent<Image>();
        bimg.color = UiStyle.Gold;
        bimg.raycastTarget = false;
        var brt = bimg.rectTransform;
        brt.anchorMin = new Vector2(0f, 0f);
        brt.anchorMax = new Vector2(0f, 1f);
        brt.pivot = new Vector2(0f, 0.5f);
        brt.sizeDelta = new Vector2(6f, 0f);

        MakeText(bgGo.transform, "成就解锁 · " + title, 24, new Vector2(0, 22), UiStyle.Gold);
        string line = desc;
        line += "   前往成就页领取奖励";
        MakeText(bgGo.transform, line, 16, new Vector2(0, -14), UiStyle.TextSecondary);

        float t = 0f;
        const float life = 2.6f;
        while (t < life)
        {
            t += Time.unscaledDeltaTime;
            float u = t / life;
            // 滑入
            float y = u < 0.12f ? Mathf.Lerp(-40f, -90f, u / 0.12f) : -90f;
            rt.anchoredPosition = new Vector2(0, y);
            float a = u > 0.75f ? 1f - (u - 0.75f) / 0.25f : 1f;
            var c = bg.color;
            bg.color = new Color(c.r, c.g, c.b, 0.92f * a);
            yield return null;
        }
        Destroy(canvasGo);
    }

    static void MakeText(Transform parent, string content, int size, Vector2 pos, Color color)
    {
        var go = new GameObject("Txt");
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<Text>();
        text.font = PixelUi.Font;
        text.fontSize = size;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.text = content;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        var rt = text.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(520f, size + 12);
    }
}
