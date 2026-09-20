using UnityEngine;

/// 十二星座 + 时间泰坦 关卡数据
public static class ZodiacLevels
{
    public class LevelDef
    {
        public int index;          // 1..13
        public string key;
        public string title;       // 白羊宫
        public string bossName;    // 金羊毛·克律索马罗斯
        public string lore;
        public int bossHp;
        public float bossSpeed;
        public float bossScale;
        public int bossScore;
        public int minionCount;
        public int minionHp;
        public float minionSpeed;
        public Color bossColor;
        public Color minionColor;
        public Color bgColor;      // 背景叠色
        public int bgHue;          // 0-360 用于生成/选择变体
        public Enemy.MovementPattern minionPattern;
        public bool bossShoots;

        public LevelDef(int index, string key, string title, string bossName, string lore,
            int bossHp, float bossSpeed, float bossScale, int bossScore,
            int minionCount, int minionHp, float minionSpeed,
            Color bossColor, Color minionColor, Color bgColor,
            Enemy.MovementPattern minionPattern = Enemy.MovementPattern.Sine)
        {
            this.index = index;
            this.key = key;
            this.title = title;
            this.bossName = bossName;
            this.lore = lore;
            this.bossHp = bossHp;
            this.bossSpeed = bossSpeed;
            this.bossScale = bossScale;
            this.bossScore = bossScore;
            this.minionCount = minionCount;
            this.minionHp = minionHp;
            this.minionSpeed = minionSpeed;
            this.bossColor = bossColor;
            this.minionColor = minionColor;
            this.bgColor = bgColor;
            this.bgHue = index * 27;
            this.minionPattern = minionPattern;
            this.bossShoots = true;
        }
    }

    public static readonly LevelDef[] All =
    {
        new LevelDef(1, "aries", "白羊宫", "金羊毛·克律索马罗斯",
            "疾风般的冲锋，不给敌人喘息。",
            280, 1.1f, 1.45f, 350, 5, 2, 1.9f,
            new Color(1f, 0.35f, 0.3f), new Color(1f, 0.55f, 0.4f), new Color(0.45f, 0.08f, 0.12f),
            Enemy.MovementPattern.Straight),

        new LevelDef(2, "taurus", "金牛宫", "欧罗巴之牛·塔罗斯",
            "巨力冲撞，阵型如牛角般锐利。",
            360, 0.85f, 1.55f, 420, 6, 3, 1.5f,
            new Color(0.95f, 0.7f, 0.25f), new Color(0.9f, 0.65f, 0.3f), new Color(0.35f, 0.22f, 0.05f),
            Enemy.MovementPattern.Zigzag),

        new LevelDef(3, "gemini", "双子宫", "狄俄斯库里双子",
            "成对出现，镜像夹击。",
            460, 1.2f, 1.4f, 460, 8, 3, 1.8f,
            new Color(0.75f, 0.85f, 1f), new Color(0.7f, 0.8f, 1f), new Color(0.12f, 0.18f, 0.4f),
            Enemy.MovementPattern.Sine),

        new LevelDef(4, "cancer", "巨蟹宫", "赫拉之蟹·卡基诺斯",
            "硬壳护卫，蟹群围猎。",
            580, 0.7f, 1.6f, 520, 8, 4, 1.3f,
            new Color(0.55f, 0.9f, 0.85f), new Color(0.5f, 0.85f, 0.8f), new Color(0.05f, 0.28f, 0.3f),
            Enemy.MovementPattern.Zigzag),

        new LevelDef(5, "leo", "狮子宫", "涅墨亚之狮",
            "王者咆哮，弹幕如鬃毛炸裂。",
            700, 0.9f, 1.7f, 600, 7, 5, 1.4f,
            new Color(1f, 0.75f, 0.2f), new Color(1f, 0.7f, 0.25f), new Color(0.4f, 0.25f, 0.02f),
            Enemy.MovementPattern.Sine),

        new LevelDef(6, "virgo", "处女宫", "群星侍女·阿斯特赖亚",
            "纯净之雨，精准坠落。",
            850, 1.0f, 1.5f, 650, 9, 4, 1.7f,
            new Color(0.95f, 0.85f, 1f), new Color(0.9f, 0.8f, 1f), new Color(0.28f, 0.15f, 0.38f),
            Enemy.MovementPattern.Sine),

        new LevelDef(7, "libra", "天秤宫", "忒弥斯之秤",
            "平衡与审判，两侧同时压来。",
            1020, 0.8f, 1.55f, 720, 10, 5, 1.4f,
            new Color(0.7f, 0.95f, 0.75f), new Color(0.65f, 0.9f, 0.7f), new Color(0.08f, 0.3f, 0.15f),
            Enemy.MovementPattern.Zigzag),

        new LevelDef(8, "scorpio", "天蝎宫", "猎户之敌·天蝎",
            "尾针剧毒，暗影潜行。",
            1210, 1.05f, 1.55f, 800, 9, 5, 1.85f,
            new Color(0.85f, 0.25f, 0.45f), new Color(0.8f, 0.3f, 0.5f), new Color(0.32f, 0.05f, 0.18f),
            Enemy.MovementPattern.Sine),

        new LevelDef(9, "sagittarius", "射手宫", "贤者喀戎",
            "万箭齐发，远程压制。",
            1430, 0.95f, 1.6f, 880, 10, 6, 1.6f,
            new Color(0.95f, 0.45f, 0.2f), new Color(0.95f, 0.5f, 0.25f), new Color(0.35f, 0.15f, 0.05f),
            Enemy.MovementPattern.Zigzag),

        new LevelDef(10, "capricorn", "摩羯宫", "海羊阿玛尔忒亚",
            "深渊攀爬，礁石般坚韧。",
            1680, 0.75f, 1.65f, 960, 11, 6, 1.35f,
            new Color(0.55f, 0.7f, 0.55f), new Color(0.55f, 0.75f, 0.6f), new Color(0.12f, 0.22f, 0.18f),
            Enemy.MovementPattern.Straight),

        new LevelDef(11, "aquarius", "水瓶宫", "斟酒者伽倪墨得斯",
            "倾泻的星泉，弹幕如雨。",
            1960, 0.9f, 1.55f, 1050, 12, 7, 1.7f,
            new Color(0.4f, 0.75f, 1f), new Color(0.45f, 0.8f, 1f), new Color(0.05f, 0.22f, 0.42f),
            Enemy.MovementPattern.Sine),

        new LevelDef(12, "pisces", "双鱼宫", "海中双鱼·阿芙罗狄忒之缚",
            "丝线缠绕，双向游弋。",
            2280, 1.0f, 1.6f, 1200, 12, 7, 1.75f,
            new Color(0.55f, 0.55f, 1f), new Color(0.6f, 0.6f, 1f), new Color(0.18f, 0.12f, 0.4f),
            Enemy.MovementPattern.Sine),

        new LevelDef(13, "kronos", "终焉·时之座", "时间泰坦·克洛诺斯",
            "黄道纪元的旧主，吞噬星辰与时刻。十二宫尽头，时间本身为敌。",
            3000, 0.55f, 2.15f, 2800, 14, 8, 1.45f,
            new Color(0.95f, 0.75f, 0.25f), new Color(0.75f, 0.55f, 0.2f), new Color(0.18f, 0.08f, 0.02f),
            Enemy.MovementPattern.Zigzag),
    };

    public const int Count = 13;

    public static LevelDef Get(int index1Based)
    {
        if (index1Based < 1) index1Based = 1;
        if (index1Based > Count) index1Based = Count;
        return All[index1Based - 1];
    }

    public const string KeyUnlocked = "ZC_Unlocked"; // 已解锁到第几关（1=已通第1关）
    public const string KeySelectedMode = "ZC_Mode"; // 0 campaign 1 endless
    public const string KeyLastLevel = "ZC_LastLevel";
    static string KeyStars(int lv) => "ZC_Stars_" + lv;
    static string KeyClearReward(int lv) => "ZC_ClearReward_" + lv;

    public static int GetStars(int level)
    {
        return Mathf.Clamp(PlayerPrefs.GetInt(KeyStars(level), 0), 0, 3);
    }

    public static void SetStars(int level, int stars)
    {
        stars = Mathf.Clamp(stars, 0, 3);
        int old = GetStars(level);
        if (stars <= old) return;
        PlayerPrefs.SetInt(KeyStars(level), stars);
        PlayerPrefs.Save();
    }

    /// 通关星级：分数 + 剩余血量 + 用时/波次
    public static int CalcStars(int level, int score, float hpRatio)
    {
        int stars = 1; // 过关即 1 星
        if (hpRatio >= 0.5f) stars++;
        if (score >= 800 + level * 250 && hpRatio >= 0.25f) stars++;
        return Mathf.Clamp(stars, 1, 3);
    }

    public static int UnlockedLevel
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(KeyUnlocked, 1), 1, Count);
        set
        {
            PlayerPrefs.SetInt(KeyUnlocked, Mathf.Clamp(value, 1, Count));
            PlayerPrefs.Save();
        }
    }

    public static bool IsUnlocked(int level) => level <= UnlockedLevel;

    public static void ClearLevel(int level)
    {
        if (level >= UnlockedLevel && level < Count)
        {
            UnlockedLevel = level + 1;
        }
        PlayerPrefs.SetInt(KeyLastLevel, level);
        PlayerPrefs.Save();
    }

    public static int ClaimClearReward(int level, int stars)
    {
        bool firstClear = PlayerPrefs.GetInt(KeyClearReward(level), 0) == 0;
        int reward = GameBalance.CampaignClearReward(level, stars, firstClear);
        ShipMeta.AddCoins(reward);
        if (firstClear) PlayerPrefs.SetInt(KeyClearReward(level), 1);
        PlayerPrefs.Save();
        return reward;
    }

    public static bool IsCampaign
    {
        get => PlayerPrefs.GetInt(KeySelectedMode, 0) == 0;
        set
        {
            PlayerPrefs.SetInt(KeySelectedMode, value ? 0 : 1);
            PlayerPrefs.Save();
        }
    }

    public static int CurrentLevel
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(KeyLastLevel, 1), 1, Count);
        set
        {
            PlayerPrefs.SetInt(KeyLastLevel, Mathf.Clamp(value, 1, Count));
            PlayerPrefs.Save();
        }
    }

    /// 无尽：按分数阈值选星座 Boss
    public static LevelDef EndlessBossForScore(int score)
    {
        int idx = score / 2500; // 每 2500 分一个星座
        idx = Mathf.Clamp(idx, 0, Count - 1);
        return All[idx];
    }

    public static LevelDef RandomMinionTheme(int wave)
    {
        int idx = (wave - 1) % 12; // 前12星座轮换小怪
        return All[idx];
    }
}
