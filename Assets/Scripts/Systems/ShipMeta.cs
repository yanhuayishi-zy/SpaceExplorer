using UnityEngine;

/// 战机定义与本地存档（金币 / 已购 / 当前选择）
public static class ShipMeta
{
    public const string KeyCoins = "SE_Coins";
    public const string KeyOwned = "SE_Owned_";
    public const string KeyCurrent = "SE_CurrentShip";

    public enum GodSkill
    {
        None = 0,
        ZeusThunder,      // 大招：全屏落雷
        PoseidonTide,     // 大招：横扫潮汐
        AresRage,         // 大招：狂暴连射
        AthenaAegis,      // 大招：神盾无敌
        ApolloSun,        // 大招：太阳轰炸
        ArtemisVolley,    // 大招：月神箭雨
        HermesDash,       // 大招：疾风突进
        HadesSoul,        // 大招：冥界收割
    }

    public enum SubSkill
    {
        None = 0,
        ZeusChain,        // 链式闪电
        PoseidonBubble,   // 水泡护体+环形弹
        AresCleave,       // 战矛横扫
        AthenaLance,      // 圣枪突刺
        ApolloFlare,      // 耀斑爆发
        ArtemisMark,      // 标记+追踪箭
        HermesWind,       // 风步加速
        HadesCurse,       // 诅咒减速敌群
    }

    public enum BulletStyle
    {
        Basic = 0,
        Lightning,   // 之字雷矢
        Orb,         // 水球
        Spear,       // 长矛
        Lance,       // 圣枪
        Fireball,    // 火球
        Arrow,       // 银箭
        Feather,     // 疾羽
        Soul,        // 魂火
    }

    [System.Serializable]
    public class ShipDef
    {
        public string id;
        public string name;
        public string title;
        public string desc;
        public string skillName;      // 大招
        public string skillDesc;
        public GodSkill skill;
        public string subName;        // 子技能
        public string subDesc;
        public SubSkill subSkill;
        public float subCd;
        public BulletStyle bulletStyle;
        public int price;
        public int maxHp;
        public float moveSpeed;
        public float fireRate;
        public int damage;
        public Color tint;
        public Color bulletColor;
        public float chargeNeed;

        public ShipDef(string id, string name, string title, string desc,
            string skillName, string skillDesc, GodSkill skill,
            string subName, string subDesc, SubSkill subSkill, float subCd, BulletStyle bulletStyle,
            int price, int maxHp, float moveSpeed, float fireRate, int damage,
            Color tint, Color bulletColor, float chargeNeed = 100f)
        {
            this.id = id;
            this.name = name;
            this.title = title;
            this.desc = desc;
            this.skillName = skillName;
            this.skillDesc = skillDesc;
            this.skill = skill;
            this.subName = subName;
            this.subDesc = subDesc;
            this.subSkill = subSkill;
            this.subCd = subCd;
            this.bulletStyle = bulletStyle;
            this.price = price;
            this.maxHp = maxHp;
            this.moveSpeed = moveSpeed;
            this.fireRate = fireRate;
            this.damage = damage;
            this.tint = tint;
            this.bulletColor = bulletColor;
            this.chargeNeed = chargeNeed;
        }
    }

    public static readonly ShipDef[] Ships =
    {
        new ShipDef("mortal", "凡人", "行者", "初始机体 · 均衡",
            "齐射", "短时三向散射", GodSkill.None,
            "疾射", "立即向前方连射一轮", SubSkill.None, 5f, BulletStyle.Basic,
            0, 5, 10f, 0.14f, 2, Color.white, Color.white, GameBalance.UltNeedMortal),

        new ShipDef("zeus", "宙斯", "众神之王", "雷霆主宰 · 高爆发",
            "神威天雷", "全屏落雷重创敌机", GodSkill.ZeusThunder,
            "链式闪电", "向最近敌人释放连锁闪电", SubSkill.ZeusChain, 6f, BulletStyle.Lightning,
            800, 5, 10f, 0.13f, 2,
            new Color(1f, 0.92f, 0.45f), new Color(1f, 0.95f, 0.55f), GameBalance.UltNeedGOD),

        new ShipDef("poseidon", "波塞冬", "海神", "深渊潮汐 · 控场",
            "怒海狂涛", "横向潮汐冲击敌群", GodSkill.PoseidonTide,
            "沧浪环", "释放环形水弹并短暂护体", SubSkill.PoseidonBubble, 7f, BulletStyle.Orb,
            900, 6, 9.5f, 0.15f, 2,
            new Color(0.35f, 0.75f, 1f), new Color(0.45f, 0.9f, 1f), GameBalance.UltNeedTank),

        new ShipDef("ares", "阿瑞斯", "战神", "嗜战狂怒 · 近战压制",
            "血怒风暴", "短时超级连射", GodSkill.AresRage,
            "战矛横扫", "前方扇形矛雨", SubSkill.AresCleave, 5.5f, BulletStyle.Spear,
            700, 5, 11f, 0.12f, 2,
            new Color(1f, 0.35f, 0.3f), new Color(1f, 0.45f, 0.35f), 90f),

        new ShipDef("athena", "雅典娜", "智慧女神", "神盾守护 · 生存",
            "埃癸斯", "无敌并清空敌弹", GodSkill.AthenaAegis,
            "圣枪突刺", "贯穿直线圣枪", SubSkill.AthenaLance, 6.5f, BulletStyle.Lance,
            850, 6, 10f, 0.14f, 2,
            new Color(0.75f, 0.85f, 1f), new Color(0.85f, 0.95f, 1f), GameBalance.UltNeedTank),

        new ShipDef("apollo", "阿波罗", "太阳神", "骄阳箭雨 · 范围",
            "日轮轰炸", "太阳火球轰炸全场", GodSkill.ApolloSun,
            "耀斑爆发", "自身周围灼热爆发", SubSkill.ApolloFlare, 7f, BulletStyle.Fireball,
            1000, 5, 10.5f, 0.13f, 3,
            new Color(1f, 0.7f, 0.25f), new Color(1f, 0.85f, 0.4f), GameBalance.UltNeedGOD),

        new ShipDef("artemis", "阿尔忒弥斯", "月神猎手", "银月齐射 · 精准",
            "月神箭雨", "多向银箭覆盖屏幕", GodSkill.ArtemisVolley,
            "狩猎标记", "标记敌人并射出追踪箭", SubSkill.ArtemisMark, 6f, BulletStyle.Arrow,
            950, 5, 11.5f, 0.12f, 2,
            new Color(0.85f, 0.9f, 1f), new Color(0.9f, 0.95f, 1f), GameBalance.UltNeedGOD),

        new ShipDef("hermes", "赫尔墨斯", "神使", "极速信使 · 机动",
            "神行天下", "冲刺并留下伤害轨迹", GodSkill.HermesDash,
            "风步", "短时大幅提升移速射速", SubSkill.HermesWind, 5f, BulletStyle.Feather,
            750, 4, 14f, 0.11f, 2,
            new Color(0.55f, 1f, 0.75f), new Color(0.7f, 1f, 0.8f), 85f),

        new ShipDef("hades", "哈迪斯", "冥王", "冥界之主 · 收割",
            "亡者收割", "群伤并回复生命", GodSkill.HadesSoul,
            "冥府诅咒", "减速场上敌机并持续伤害", SubSkill.HadesCurse, 8f, BulletStyle.Soul,
            1500, 6, 9f, 0.14f, 3,
            new Color(0.65f, 0.35f, 0.95f), new Color(0.75f, 0.45f, 1f), GameBalance.UltNeedHades),
    };

    public static int Coins
    {
        get => PlayerPrefs.GetInt(KeyCoins, 0);
        set
        {
            PlayerPrefs.SetInt(KeyCoins, Mathf.Max(0, value));
            PlayerPrefs.Save();
        }
    }

    public static void AddCoins(int amount)
    {
        if (amount == 0) return;
        Coins = Coins + amount;
    }

    public static bool IsOwned(string id)
    {
        if (id == "mortal" || id == "rookie") return true;
        return PlayerPrefs.GetInt(KeyOwned + id, 0) == 1;
    }

    public static bool Buy(ShipDef def)
    {
        if (def == null || IsOwned(def.id)) return false;
        if (Coins < def.price) return false;
        Coins -= def.price;
        PlayerPrefs.SetInt(KeyOwned + def.id, 1);
        PlayerPrefs.SetString(KeyCurrent, def.id);
        PlayerPrefs.Save();
        return true;
    }

    public static ShipDef Current
    {
        get
        {
            string id = PlayerPrefs.GetString(KeyCurrent, "mortal");
            if (id == "rookie") id = "mortal";
            if (!IsOwned(id)) id = "mortal";
            for (int i = 0; i < Ships.Length; i++)
            {
                if (Ships[i].id == id) return Ships[i];
            }
            return Ships[0];
        }
    }

    public static void Select(string id)
    {
        if (!IsOwned(id)) return;
        PlayerPrefs.SetString(KeyCurrent, id);
        PlayerPrefs.Save();
    }

    public static void ApplyToPlayer(GameObject player)
    {
        if (player == null) return;
        var def = Current;

        // 先掐掉上一机体的大招染色/增益协程，避免换机后残留
        var shoot0 = player.GetComponent<PlayerShooting>();
        if (shoot0 != null) shoot0.CancelSkillFx();
        var hp0 = player.GetComponent<PlayerHealth>();
        if (hp0 != null) hp0.ResetVisualState();

        var sr = player.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // 总是按当前选择换机体贴图
            var spr = LoadShipSprite(def.id);
            if (spr != null) sr.sprite = spr;
            sr.color = Color.white;
        }

        // 机模保持原尺寸（勿再缩小）

        var hp = player.GetComponent<PlayerHealth>();
        if (hp != null)
        {
            // 百分制：机体 maxHp × 20（凡人 5 → 100）
            hp.ApplyScaledVitals(def.maxHp);
        }

        var move = player.GetComponent<PlayerMovement>();
        if (move != null)
        {
            move.moveSpeed = def.moveSpeed;
        }

        var shoot = player.GetComponent<PlayerShooting>();
        if (shoot != null)
        {
            shoot.fireRate = def.fireRate;
            shoot.damage = def.damage;
            shoot.bulletColor = def.bulletColor;
            shoot.godSkill = def.skill;
            shoot.subSkill = def.subSkill;
            shoot.subName = def.subName;
            shoot.subCd = def.subCd;
            shoot.bulletStyle = def.bulletStyle;
            shoot.shipId = def.id;
            shoot.chargeNeed = def.chargeNeed;
            shoot.chargePerKill = GameBalance.UltChargePerKill;
            shoot.chargePerSec = GameBalance.UltChargePerSec;
            shoot.durationBonus = 0f;
            shoot.skillName = def.skillName;
            shoot.RefreshGodBullet();
        }
        else
        {
            player.AddComponent<ShipStatApplier>();
        }

        GearMeta.ApplyToPlayer(player);
    }

    public static Sprite LoadShipSprite(string id)
    {
        var spr = Resources.Load<Sprite>("GodShip_" + id);
        if (spr == null) spr = Resources.Load<Sprite>("PlayerShip");
        return spr;
    }
}

/// 保证射击脚本晚挂时也能吃到战机属性
public class ShipStatApplier : MonoBehaviour
{
    void Start()
    {
        ShipMeta.ApplyToPlayer(gameObject);
        Destroy(this);
    }
}
