using UnityEngine;

/// 全局数值表：集中调手感，避免散落魔法数
public static class GameBalance
{
    // ---- 玩家 ----
    public const int BaseHpScale = 20;          // 机体 HP×20 → 百分制
    public const float InvulnAfterHit = 1.15f;  // 受击无敌（秒）
    public const float UltChargePerSec = 1.1f;
    public const float UltChargePerKill = 11f;
    public const float UltBossChargeMul = 3f;

    // ---- 敌人伤害（对 100 血，满血约 5–6 下）----
    public const int MinionDmgMin = 15;
    public const int MinionDmgMax = 34;
    public const int BossDmgMin = 26;
    public const int BossDmgMax = 50;
    public const float BossDamageTakenMul = 0.8f;

    /// 十三宫小怪伤害：15 + 关×1.8
    public static int CampaignMinionDamage(int level)
    {
        return Mathf.Clamp(Mathf.RoundToInt(MinionDmgMin + (level - 1) * 1.8f), MinionDmgMin, MinionDmgMax);
    }

    /// 十三宫 Boss 伤害
    public static int CampaignBossDamage(int level)
    {
        return Mathf.Clamp(Mathf.RoundToInt(BossDmgMin + (level - 1) * 2f), BossDmgMin, BossDmgMax);
    }

    /// 无尽小怪伤害：15 + 波次/3
    public static int EndlessMinionDamage(int wave)
    {
        return Mathf.Clamp(MinionDmgMin + wave / 3, MinionDmgMin, MinionDmgMax);
    }

    /// 无尽 Boss 伤害
    public static int EndlessBossDamage(int wave)
    {
        return Mathf.Clamp(BossDmgMin + wave / 3, BossDmgMin, BossDmgMax);
    }

    // ---- 分数 / 经济 ----
    public const int ScoreSmall = 60;
    public const int ScoreMid = 100;
    public const float CoinPerScore = 0.04f;    // 击杀金币 ≈ score×0.04，至少 2
    /// 无尽分数系数：压低总分膨胀，避免几分钟过万
    public const float EndlessScoreMul = 0.55f;
    public const int CampaignBossBounty = 30;

    public static int KillCoins(int scoreValue)
    {
        return Mathf.Max(2, Mathf.RoundToInt(scoreValue * CoinPerScore));
    }

    /// 无尽击杀得分（含连击后）
    public static int EndlessScore(int scoreAfterCombo)
    {
        return Mathf.Max(1, Mathf.RoundToInt(scoreAfterCombo * EndlessScoreMul));
    }

    // ---- 无尽节奏 ----
    public const int RogueScorePerPick = 1200;
    public const int BossScoreGap = 2400;
    public const int MinionHpBase = 5;
    public const float BossBreakHpRatio = 0.055f;
    public const int BossBreakMin = 28;
    public const int BossBreakMax = 180;
    public static int EndlessMinionHp(int wave)
    {
        return MinionHpBase + wave + wave / 5;
    }

    // Boss 弹幕使用百分制伤害。旧版固定 1 点会让所有弹幕失去威胁。
    public static int BossBulletDamage(int contactDamage)
    {
        return Mathf.Clamp(Mathf.RoundToInt(contactDamage * 0.48f), 10, 24);
    }

    public static int CampaignClearReward(int level, int stars, bool firstClear)
    {
        int baseReward = 35 + level * 7 + Mathf.Clamp(stars, 1, 3) * 12;
        return firstClear ? baseReward + 70 + level * 8 : baseReward;
    }

    // ---- 大招充能需求（机体可在 ShipMeta 上微调）----
    public const float UltNeedMortal = 90f;
    public const float UltNeedGOD = 95f;
    public const float UltNeedTank = 105f;
    public const float UltNeedHades = 110f;
}
