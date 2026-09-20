using UnityEngine;

/// 可升级装备 + 可购买装配道具（本地存档）
public static class GearMeta
{
    const string KeyLv = "GE_Lv_";
    const string KeyItem = "GE_Item_";
    const string KeyEquip = "GE_Equip_";

    [System.Serializable]
    public class GearDef
    {
        public string id;
        public string name;
        public string desc;
        public int maxLevel;
        public int[] upgradeCost; // 升到 1..max 的花费
        public string statHint;

        public GearDef(string id, string name, string desc, int maxLevel, int[] upgradeCost, string statHint)
        {
            this.id = id;
            this.name = name;
            this.desc = desc;
            this.maxLevel = maxLevel;
            this.upgradeCost = upgradeCost;
            this.statHint = statHint;
        }
    }

    [System.Serializable]
    public class ItemDef
    {
        public string id;
        public string name;
        public string desc;
        public int price;
        public string effectHint;

        public ItemDef(string id, string name, string desc, int price, string effectHint)
        {
            this.id = id;
            this.name = name;
            this.desc = desc;
            this.price = price;
            this.effectHint = effectHint;
        }
    }

    public static readonly GearDef[] Gears =
    {
        new GearDef("engine", "推进引擎", "提升移动速度", 5, new[]{200,350,500,700,900}, "每级 +0.8 移速"),
        new GearDef("weapon", "火控系统", "缩短射击间隔", 5, new[]{250,400,600,800,1000}, "每级射速 +6%"),
        new GearDef("armor", "装甲板", "提升最大生命", 4, new[]{300,500,750,1000}, "每级最大生命 +20"),
        new GearDef("core", "能量核心", "延长持续技能与护盾", 3, new[]{400,700,1000}, "每级持续时间 +0.6 秒"),
        new GearDef("mag", "弹链", "提升子弹伤害", 4, new[]{350,600,900,1200}, "每级伤害 +1"),
        new GearDef("sight", "鹰眼瞄准", "暴击概率提升", 5, new[]{400,650,900,1200,1500}, "每级暴击率 +4%"),
        new GearDef("drone", "拾荒无人机", "提升道具与金币获取", 4, new[]{300,550,800,1100}, "每级掉落+8% · 金币+5%"),
        new GearDef("thorn", "反击装甲", "受击时小范围反击", 3, new[]{450,800,1200}, "每级触发率 +12%"),
    };

    public static readonly ItemDef[] Items =
    {
        new ItemDef("skill_core", "技能核心", "大招充能获取 +25%", 400, "充能+25%"),
        new ItemDef("spread_chip", "散射芯片", "开局自带散射 20 秒", 600, "开局散射"),
        new ItemDef("shield_cell", "护盾电池", "开局 4 秒无敌", 500, "开局护盾"),
        new ItemDef("coin_vip", "淘金许可", "金币获取 +50%", 800, "金币+50%"),
        new ItemDef("life_bead", "生命结晶", "开局最大生命 +20", 700, "生命+20"),
        new ItemDef("crit_chip", "破甲芯片", "暴击率 +5%，暴击伤害 +25%", 650, "暴击+5% · 暴伤+25%"),
    };

    public static int GetLevel(string gearId)
    {
        return PlayerPrefs.GetInt(KeyLv + gearId, 0);
    }

    public static int UpgradeCost(GearDef g)
    {
        int lv = GetLevel(g.id);
        if (lv >= g.maxLevel) return -1;
        return g.upgradeCost[lv];
    }

    public static bool Upgrade(GearDef g)
    {
        if (g == null) return false;
        int lv = GetLevel(g.id);
        if (lv >= g.maxLevel) return false;
        int cost = g.upgradeCost[lv];
        if (ShipMeta.Coins < cost) return false;
        ShipMeta.Coins -= cost;
        PlayerPrefs.SetInt(KeyLv + g.id, lv + 1);
        PlayerPrefs.Save();
        return true;
    }

    public static bool OwnItem(string itemId)
    {
        return PlayerPrefs.GetInt(KeyItem + itemId, 0) == 1;
    }

    public static bool IsEquipped(string itemId)
    {
        return PlayerPrefs.GetInt(KeyEquip + itemId, 0) == 1;
    }

    public static bool BuyItem(ItemDef item)
    {
        if (item == null || OwnItem(item.id)) return false;
        if (ShipMeta.Coins < item.price) return false;
        ShipMeta.Coins -= item.price;
        PlayerPrefs.SetInt(KeyItem + item.id, 1);
        PlayerPrefs.SetInt(KeyEquip + item.id, 1);
        PlayerPrefs.Save();
        return true;
    }

    public static void ToggleEquip(string itemId)
    {
        if (!OwnItem(itemId)) return;
        bool on = !IsEquipped(itemId);
        PlayerPrefs.SetInt(KeyEquip + itemId, on ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static int ItemCount(string itemId)
    {
        // 炸弹补给可叠 2 次：用 SE_ItemCnt_
        return PlayerPrefs.GetInt("GE_Cnt_" + itemId, OwnItem(itemId) ? 1 : 0);
    }

    public static bool BuyItemStack(ItemDef item, int maxStack)
    {
        if (item == null) return false;
        int have = ItemCount(item.id);
        if (have >= maxStack) return false;
        if (ShipMeta.Coins < item.price) return false;
        ShipMeta.Coins -= item.price;
        PlayerPrefs.SetInt(KeyItem + item.id, 1);
        PlayerPrefs.SetInt(KeyEquip + item.id, 1);
        PlayerPrefs.SetInt("GE_Cnt_" + item.id, have + 1);
        PlayerPrefs.Save();
        return true;
    }

    /// 汇总加成
    public struct Bonuses
    {
        public float moveAdd;
        public float fireRateMul;
        public int hpAdd;
        public float specialAdd;
        public float damageAdd;
        public float chargeMul;
        public bool startSpread;
        public float startShield;
        public float coinMul;
        public float critChance;   // 0-1
        public float critMul;      // 暴击伤害倍率
        public float dropBonus;    // 掉落概率加成
        public float thorns;       // 受击反伤概率 0-1
    }

    public static Bonuses Collect()
    {
        var b = new Bonuses
        {
            fireRateMul = 1f,
            coinMul = 1f,
            chargeMul = 1f,
            critMul = 1.8f,
        };

        b.moveAdd = GetLevel("engine") * 0.8f;
        int wlv = GetLevel("weapon");
        b.fireRateMul = Mathf.Pow(0.94f, wlv);
        b.hpAdd = GetLevel("armor") + (IsEquipped("life_bead") ? 1 : 0);
        b.specialAdd = GetLevel("core") * 0.6f;
        b.damageAdd = GetLevel("mag");
        b.critChance = GetLevel("sight") * 0.04f;
        int droneLevel = GetLevel("drone");
        b.dropBonus = droneLevel * 0.08f;
        b.coinMul *= 1f + droneLevel * 0.05f;
        b.thorns = Mathf.Clamp01(GetLevel("thorn") * 0.12f);
        if (OwnItem("skill_core") && IsEquipped("skill_core")) b.chargeMul = 1.25f;
        if (OwnItem("spread_chip") && IsEquipped("spread_chip")) b.startSpread = true;
        if (OwnItem("shield_cell") && IsEquipped("shield_cell")) b.startShield = 4f;
        if (OwnItem("coin_vip") && IsEquipped("coin_vip")) b.coinMul *= 1.5f;
        if (OwnItem("crit_chip") && IsEquipped("crit_chip"))
        {
            b.critChance += 0.05f;
            b.critMul += 0.25f;
        }
        b.critChance = Mathf.Clamp01(b.critChance);

        return b;
    }

    public static float CritChance => Collect().critChance;
    public static float CritMul => Collect().critMul;
    public static float DropBonus => Collect().dropBonus;
    public static float Thorns => Collect().thorns;

    public static int CoinGain(int baseGain)
    {
        if (baseGain <= 0) return 0;
        return Mathf.RoundToInt(baseGain * Collect().coinMul);
    }

    public static void ApplyToPlayer(GameObject player)
    {
        if (player == null) return;
        var b = Collect();

        var move = player.GetComponent<PlayerMovement>();
        if (move != null)
        {
            // 以战机基准再叠加
            move.moveSpeed += MobileTuning.MoveSpeed(b.moveAdd);
        }

        var shoot = player.GetComponent<PlayerShooting>();
        if (shoot != null)
        {
            shoot.fireRate = Mathf.Max(0.05f, shoot.fireRate * b.fireRateMul);
            shoot.damage = Mathf.Max(1, Mathf.RoundToInt(shoot.damage + b.damageAdd));
            shoot.chargePerKill *= b.chargeMul;
            shoot.chargePerSec *= b.chargeMul;
            shoot.durationBonus = b.specialAdd;
            if (b.startSpread)
            {
                shoot.ActivateSpread(20f);
            }
        }

        var hp = player.GetComponent<PlayerHealth>();
        if (hp != null)
        {
            hp.maxHealth += b.hpAdd * PlayerHealth.HpScale;
            // 装甲扩容后当前血跟着抬，避免开局不满
            if (b.hpAdd > 0) hp.Heal(b.hpAdd * PlayerHealth.HpScale);
            if (b.startShield > 0f)
            {
                hp.ActivateShield(b.startShield);
            }
        }
    }
}
