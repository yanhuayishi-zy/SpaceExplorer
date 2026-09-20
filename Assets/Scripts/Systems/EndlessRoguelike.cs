using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// 无尽肉鸽：分数达阈值弹出三选一强化；机体有专属池
public static class EndlessRoguelike
{
    public const int ScorePerPick = GameBalance.RogueScorePerPick;

    static int nextPickScore = ScorePerPick;
    static int pickCount;
    static int pendingPicks;
    static bool picking;
    static GameObject panelRoot;
    static readonly Dictionary<string, UpStack> owned = new Dictionary<string, UpStack>();

    public class UpStack
    {
        public string key;
        public string title;
        public string shortLabel;
        public Color color;
        public int count;
    }

    public static IReadOnlyDictionary<string, UpStack> Owned => owned;
    public static int PickCount => pickCount;

    public static void ResetRun()
    {
        nextPickScore = ScorePerPick;
        pickCount = 0;
        pendingPicks = 0;
        picking = false;
        owned.Clear();
        ClearPanel();
        ResetPlayerMods();
        RogueUpHud.RefreshAll();
    }

    static void RegisterPick(UpOpt opt)
    {
        if (opt == null || string.IsNullOrEmpty(opt.key)) return;
        if (!owned.TryGetValue(opt.key, out var st))
        {
            st = new UpStack
            {
                key = opt.key,
                title = opt.title,
                shortLabel = opt.shortLabel,
                color = opt.color,
            };
            owned[opt.key] = st;
        }
        st.count++;
        RogueUpHud.RefreshAll();
    }

    static void ResetPlayerMods()
    {
        var p = GameObject.Find("Player");
        if (p == null) return;
        var s = p.GetComponent<PlayerShooting>();
        if (s != null) s.ResetRoguelikeMods();
        var h = p.GetComponent<PlayerHealth>();
        if (h != null) h.ResetEndlessMods();
    }

    /// 每帧由无尽流程调用
    public static void Tick()
    {
        if (ZodiacLevels.IsCampaign) return;
        var gm = GameManager.Instance;
        if (gm == null || gm.isGameOver) return;
        while (gm.score >= nextPickScore)
        {
            nextPickScore += ScorePerPick;
            pendingPicks++;
        }
        TryStartPick();
    }

    /// Boss 击杀等强制弹出强化（不占分数阈值）
    public static void ForcePick()
    {
        if (ZodiacLevels.IsCampaign) return;
        pendingPicks++;
        TryStartPick();
    }

    static void TryStartPick()
    {
        if (picking || pendingPicks <= 0) return;
        var host = Object.FindObjectOfType<ZodiacGameFlow>();
        if (host == null) return;
        pendingPicks--;
        pickCount++;
        host.StartCoroutine(ShowPick());
    }

    static IEnumerator ShowPick()
    {
        picking = true;
        Time.timeScale = 0f;
        AutoAudio.PlaySpecial();

        var options = RollOptions();
        yield return BuildPanel(options);

        // 等待点选
        while (panelRoot != null && picking)
            yield return null;

        Time.timeScale = 1f;
        picking = false;
        TryStartPick();
    }

    class UpOpt
    {
        public string key;
        public string title;
        public string shortLabel;
        public string desc;
        public Color color;
        public System.Action apply;
    }

    static List<UpOpt> RollOptions()
    {
        var p = GameObject.Find("Player");
        var shoot = p != null ? p.GetComponent<PlayerShooting>() : null;
        var hp = p != null ? p.GetComponent<PlayerHealth>() : null;
        var skill = shoot != null ? shoot.godSkill : ShipMeta.GodSkill.None;

        var pool = new List<UpOpt>
        {
            new UpOpt { key="dmg", shortLabel="伤", title = "火力增幅", desc = "普攻伤害 +1", color = new Color(1f,0.45f,0.35f),
                apply = () => { if (shoot != null) shoot.dmgAdd = Mathf.Min(12, shoot.dmgAdd + 1); } },
            new UpOpt { key="rate", shortLabel="速", title = "急速扳机", desc = "射速 +12%", color = new Color(1f,0.75f,0.3f),
                apply = () => { if (shoot != null) shoot.rateMul = Mathf.Max(0.45f, shoot.rateMul * 0.88f); } },
            new UpOpt { key="split", shortLabel="散", title = "分裂弹", desc = "每次射击额外 +1 发", color = new Color(0.9f,0.85f,0.4f),
                apply = () => { if (shoot != null) shoot.extraProj = Mathf.Min(8, shoot.extraProj + 1); } },
            new UpOpt { key="hp", shortLabel="甲", title = "装甲扩容", desc = "最大生命 +20，并回 20", color = new Color(0.4f,0.9f,0.55f),
                apply = () => { if (hp != null) { hp.AddMaxHealth(20); hp.Heal(20); } } },
            new UpOpt { key="steal", shortLabel="吸", title = "战地急救", desc = "击杀回复 +2 生命", color = new Color(0.45f,1f,0.6f),
                apply = () => { if (shoot != null) shoot.lifesteal = Mathf.Min(12f, shoot.lifesteal + 2f); } },
            new UpOpt { key="move", shortLabel="疾", title = "引擎超频", desc = "移速 +12%", color = new Color(0.45f,1f,0.8f),
                apply = () => { if (shoot != null) { shoot.speedMul = Mathf.Min(1.8f, shoot.speedMul * 1.12f); shoot.ApplyRoguelikeMove(); } } },
            new UpOpt { key="charge", shortLabel="充", title = "能量回流", desc = "大招充能获取 +20%", color = new Color(0.7f,0.85f,1f),
                apply = () => { if (shoot != null) shoot.chargeMul = Mathf.Min(2.2f, shoot.chargeMul + 0.2f); } },
            new UpOpt { key="cd", shortLabel="冷", title = "战术冷却", desc = "子技能 CD -12%", color = new Color(0.55f,0.9f,1f),
                apply = () => { if (shoot != null) shoot.subCdMul = Mathf.Max(0.5f, shoot.subCdMul * 0.88f); } },
        };

        // 机体专属（至少放 1 个进池）
        foreach (var ex in ShipExclusive(skill, shoot, hp))
            pool.Add(ex);

        var usefulPool = pool.FindAll(opt => !IsCapped(opt.key, shoot, hp));
        if (usefulPool.Count >= 3) pool = usefulPool;

        // 抽 3 个不重复
        var picked = new List<UpOpt>();
        for (int i = 0; i < 3 && pool.Count > 0; i++)
        {
            int idx = Random.Range(0, pool.Count);
            picked.Add(pool[idx]);
            pool.RemoveAt(idx);
        }
        return picked;
    }

    static bool IsCapped(string key, PlayerShooting shoot, PlayerHealth hp)
    {
        if (shoot == null) return false;
        switch (key)
        {
            case "dmg": return shoot.dmgAdd >= 12;
            case "rate": return shoot.rateMul <= 0.451f;
            case "split": return shoot.extraProj >= 8;
            case "steal":
            case "apo_steal":
            case "had_steal": return shoot.lifesteal >= 12f;
            case "move": return shoot.speedMul >= 1.799f;
            case "charge": return shoot.chargeMul >= 2.199f;
            case "cd":
            case "her_cd": return shoot.subCdMul <= 0.501f;
            case "zeus_chain": return shoot.extraChain >= 10;
            case "zeus_ult":
            case "pos_ult":
            case "ares_ult":
            case "apo_ult":
            case "had_ult": return shoot.ultMul >= 2.999f;
            case "pos_shield":
            case "ath_shield": return shoot.shieldMul >= 2.499f;
            case "ares_rate": return shoot.rageBonus >= 0.149f;
            case "ath_inv": return hp != null && hp.invincibilityDuration >= 2.199f;
            case "art_proj": return shoot.extraProj >= 8;
            case "art_mix": return shoot.dmgAdd >= 12 && shoot.rateMul <= 0.451f;
            case "her_mix": return shoot.speedMul >= 1.799f && shoot.rateMul <= 0.451f;
            default: return false;
        }
    }

    static List<UpOpt> ShipExclusive(ShipMeta.GodSkill skill, PlayerShooting shoot, PlayerHealth hp)
    {
        var list = new List<UpOpt>();
        switch (skill)
        {
            case ShipMeta.GodSkill.ZeusThunder:
                list.Add(new UpOpt { key="zeus_chain", shortLabel="链", title = "神威过载", desc = "链式闪电额外连锁 +2", color = new Color(1f,0.95f,0.4f),
                    apply = () => { if (shoot != null) shoot.extraChain = Mathf.Min(10, shoot.extraChain + 2); } });
                list.Add(new UpOpt { key="zeus_ult", shortLabel="雷", title = "雷暴充能", desc = "大招伤害倍率 +18%", color = new Color(1f,0.9f,0.3f),
                    apply = () => { if (shoot != null) shoot.ultMul = Mathf.Min(3f, shoot.ultMul + 0.18f); } });
                break;
            case ShipMeta.GodSkill.PoseidonTide:
                list.Add(new UpOpt { key="pos_shield", shortLabel="盾", title = "深海壁垒", desc = "护盾时间 +40%", color = new Color(0.35f,0.8f,1f),
                    apply = () => { if (shoot != null) shoot.shieldMul = Mathf.Min(2.5f, shoot.shieldMul + 0.4f); } });
                list.Add(new UpOpt { key="pos_ult", shortLabel="潮", title = "潮汐奔涌", desc = "大招伤害倍率 +18%", color = new Color(0.3f,0.75f,1f),
                    apply = () => { if (shoot != null) shoot.ultMul = Mathf.Min(3f, shoot.ultMul + 0.18f); } });
                break;
            case ShipMeta.GodSkill.AresRage:
                list.Add(new UpOpt { key="ares_rate", shortLabel="狂", title = "嗜血狂热", desc = "大招期间射速再加快", color = new Color(1f,0.3f,0.2f),
                    apply = () => { if (shoot != null) shoot.rageBonus = Mathf.Min(0.15f, shoot.rageBonus + 0.12f); } });
                list.Add(new UpOpt { key="ares_ult", shortLabel="怒", title = "战神之怒", desc = "大招伤害倍率 +20%", color = new Color(1f,0.25f,0.15f),
                    apply = () => { if (shoot != null) shoot.ultMul = Mathf.Min(3f, shoot.ultMul + 0.2f); } });
                break;
            case ShipMeta.GodSkill.AthenaAegis:
                list.Add(new UpOpt { key="ath_shield", shortLabel="庇", title = "神圣庇护", desc = "护盾时间 +50%", color = new Color(0.75f,0.9f,1f),
                    apply = () => { if (shoot != null) shoot.shieldMul = Mathf.Min(2.5f, shoot.shieldMul + 0.5f); } });
                list.Add(new UpOpt { key="ath_inv", shortLabel="御", title = "坚壁清野", desc = "受击无敌时间 +0.4s", color = new Color(0.7f,0.88f,1f),
                    apply = () => { if (hp != null) hp.invincibilityDuration = Mathf.Min(2.2f, hp.invincibilityDuration + 0.4f); } });
                break;
            case ShipMeta.GodSkill.ApolloSun:
                list.Add(new UpOpt { key="apo_ult", shortLabel="日", title = "骄阳不灭", desc = "大招伤害倍率 +20%", color = new Color(1f,0.75f,0.2f),
                    apply = () => { if (shoot != null) shoot.ultMul = Mathf.Min(3f, shoot.ultMul + 0.2f); } });
                list.Add(new UpOpt { key="apo_steal", shortLabel="灼", title = "日冕灼烧", desc = "击杀回复 +3", color = new Color(1f,0.7f,0.25f),
                    apply = () => { if (shoot != null) shoot.lifesteal = Mathf.Min(12f, shoot.lifesteal + 3f); } });
                break;
            case ShipMeta.GodSkill.ArtemisVolley:
                list.Add(new UpOpt { key="art_proj", shortLabel="月", title = "月神齐射", desc = "额外弹道 +2", color = new Color(0.9f,0.95f,1f),
                    apply = () => { if (shoot != null) shoot.extraProj = Mathf.Min(8, shoot.extraProj + 2); } });
                list.Add(new UpOpt { key="art_mix", shortLabel="猎", title = "狩猎本能", desc = "普攻伤害 +1、射速 +8%", color = new Color(0.85f,0.92f,1f),
                    apply = () => { if (shoot != null) { shoot.dmgAdd = Mathf.Min(12, shoot.dmgAdd + 1); shoot.rateMul = Mathf.Max(0.45f, shoot.rateMul * 0.92f); } } });
                break;
            case ShipMeta.GodSkill.HermesDash:
                list.Add(new UpOpt { key="her_mix", shortLabel="风", title = "神行无影", desc = "移速 +18%、射速 +10%", color = new Color(0.5f,1f,0.75f),
                    apply = () => { if (shoot != null) { shoot.speedMul = Mathf.Min(1.8f, shoot.speedMul * 1.18f); shoot.rateMul = Mathf.Max(0.45f, shoot.rateMul * 0.9f); shoot.ApplyRoguelikeMove(); } } });
                list.Add(new UpOpt { key="her_cd", shortLabel="信", title = "风之眷顾", desc = "子技能 CD -20%", color = new Color(0.45f,0.95f,0.7f),
                    apply = () => { if (shoot != null) shoot.subCdMul = Mathf.Max(0.5f, shoot.subCdMul * 0.8f); } });
                break;
            case ShipMeta.GodSkill.HadesSoul:
                list.Add(new UpOpt { key="had_steal", shortLabel="魂", title = "灵魂收割", desc = "击杀回复 +4", color = new Color(0.7f,0.35f,1f),
                    apply = () => { if (shoot != null) shoot.lifesteal = Mathf.Min(12f, shoot.lifesteal + 4f); } });
                list.Add(new UpOpt { key="had_ult", shortLabel="冥", title = "冥界威压", desc = "大招伤害倍率 +18%", color = new Color(0.65f,0.3f,0.95f),
                    apply = () => { if (shoot != null) shoot.ultMul = Mathf.Min(3f, shoot.ultMul + 0.18f); } });
                break;
            default:
                list.Add(new UpOpt { key="mor", shortLabel="凡", title = "凡人意志", desc = "伤害 +1、最大生命 +10", color = Color.white,
                    apply = () => {
                        if (shoot != null) shoot.dmgAdd = Mathf.Min(12, shoot.dmgAdd + 1);
                        if (hp != null) { hp.AddMaxHealth(10); hp.Heal(10); }
                    } });
                break;
        }
        return list;
    }

    static IEnumerator BuildPanel(List<UpOpt> options)
    {
        ClearPanel();
        var canvasGo = new GameObject("RoguePickCanvas");
        panelRoot = canvasGo;
        Object.DontDestroyOnLoad(canvasGo);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        var dimGo = new GameObject("Dim");
        dimGo.transform.SetParent(canvasGo.transform, false);
        var dim = dimGo.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.78f);
        dim.raycastTarget = true;
        Stretch(dim.rectTransform);

        MakeLabel(canvasGo.transform, "强化选择 · 第 " + pickCount + " 次", 48, new Vector2(0, 300), new Color(1f, 0.9f, 0.4f));
        MakeLabel(canvasGo.transform, "选择一项永久强化", 24, new Vector2(0, 248), new Color(0.75f, 0.8f, 0.9f));

        for (int i = 0; i < options.Count; i++)
        {
            var opt = options[i];
            float x = (i - (options.Count - 1) * 0.5f) * 400f;
            var card = new GameObject("Opt" + i);
            card.transform.SetParent(canvasGo.transform, false);
            var img = card.AddComponent<Image>();
            img.color = new Color(0.08f, 0.11f, 0.18f, 0.96f);
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, -30f);
            rt.sizeDelta = new Vector2(360f, 420f);

            // 顶色条
            var bar = new GameObject("Bar");
            bar.transform.SetParent(card.transform, false);
            var bimg = bar.AddComponent<Image>();
            bimg.color = opt.color;
            bimg.raycastTarget = false;
            var brt = bimg.rectTransform;
            brt.anchorMin = new Vector2(0f, 1f);
            brt.anchorMax = new Vector2(1f, 1f);
            brt.pivot = new Vector2(0.5f, 1f);
            brt.anchoredPosition = Vector2.zero;
            brt.sizeDelta = new Vector2(0f, 10f);

            MakeLabel(card.transform, opt.shortLabel, 48, new Vector2(0, 100), opt.color);
            MakeLabel(card.transform, opt.title, 32, new Vector2(0, 30), opt.color);
            MakeLabel(card.transform, opt.desc, 20, new Vector2(0, -20), Color.white);

            var btnGo = new GameObject("Pick");
            btnGo.transform.SetParent(card.transform, false);
            var b2 = btnGo.AddComponent<Image>();
            b2.color = new Color(opt.color.r, opt.color.g, opt.color.b, 0.85f);
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = b2;
            btn.transition = Selectable.Transition.ColorTint;
            var brt2 = b2.rectTransform;
            brt2.anchorMin = brt2.anchorMax = new Vector2(0.5f, 0f);
            brt2.pivot = new Vector2(0.5f, 0f);
            brt2.anchoredPosition = new Vector2(0, 36f);
            brt2.sizeDelta = new Vector2(240f, 60f);
            MakeLabel(btnGo.transform, "选择", 28, Vector2.zero, Color.white);

            var chosen = opt;
            btn.onClick.AddListener(() =>
            {
                chosen.apply?.Invoke();
                RegisterPick(chosen);
                GameFx.Pickup(new Vector3(0, -2, 0), chosen.color);
                UltVfx.ScreenFlash(new Color(chosen.color.r, chosen.color.g, chosen.color.b, 0.25f), 0.2f);
                picking = false;
                ClearPanel();
            });
        }

        yield return null;
    }

    static void MakeLabel(Transform parent, string content, int size, Vector2 pos, Color color)
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
        rt.sizeDelta = new Vector2(300f, size + 16);
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void ClearPanel()
    {
        if (panelRoot != null)
        {
            Object.Destroy(panelRoot);
            panelRoot = null;
        }
    }
}

/// 左下角：本局已叠强化图标（叠层显示 ×N）
public class RogueUpHud : MonoBehaviour
{
    static RogueUpHud instance;
    Transform chipRoot;
    Canvas canvas;
    readonly Dictionary<string, GameObject> chips = new Dictionary<string, GameObject>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (instance != null) return;
        var go = new GameObject("RogueUpHud");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<RogueUpHud>();
    }

    void Awake()
    {
        instance = this;
        EnsureUi();
    }

    public static void RefreshAll()
    {
        if (instance == null) return;
        instance.EnsureUi();
        instance.Rebuild();
    }

    void EnsureUi()
    {
        if (canvas != null && chipRoot != null) return;
        var cgo = new GameObject("RogueUpHudCanvas");
        cgo.transform.SetParent(transform, false);
        canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 55;
        var scaler = cgo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var rootGo = new GameObject("Chips");
        rootGo.transform.SetParent(cgo.transform, false);
        chipRoot = rootGo.transform;
        var rt = rootGo.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(24f, 70f);
        rt.sizeDelta = new Vector2(420f, 160f);
    }

    void Update()
    {
        if (canvas == null) return;
        bool show = !ZodiacLevels.IsCampaign
            && !MobileGameShell.IsMenuOpen
            && EndlessRoguelike.Owned.Count > 0;
        if (canvas.gameObject.activeSelf != show)
            canvas.gameObject.SetActive(show);
    }

    void Rebuild()
    {
        if (chipRoot == null) return;
        foreach (var kv in chips)
            if (kv.Value != null) Destroy(kv.Value);
        chips.Clear();

        int i = 0;
        foreach (var kv in EndlessRoguelike.Owned)
        {
            var st = kv.Value;
            if (st == null) continue;
            float x = (i % 6) * 52f;
            float y = -(i / 6) * 48f;
            var chip = MakeChip(st.shortLabel, st.color, st.count, new Vector2(x, y));
            chips[st.key] = chip;
            i++;
        }
    }

    GameObject MakeChip(string shortLabel, Color color, int stacks, Vector2 pos)
    {
        var go = new GameObject("Chip_" + shortLabel);
        go.transform.SetParent(chipRoot, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(color.r * 0.35f + 0.05f, color.g * 0.35f + 0.06f, color.b * 0.35f + 0.1f, 0.92f);
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(46f, 42f);

        // 左色条
        var bar = new GameObject("Bar");
        bar.transform.SetParent(go.transform, false);
        var bimg = bar.AddComponent<Image>();
        bimg.color = color;
        bimg.raycastTarget = false;
        var brt = bimg.rectTransform;
        brt.anchorMin = new Vector2(0f, 0f);
        brt.anchorMax = new Vector2(0f, 1f);
        brt.pivot = new Vector2(0f, 0.5f);
        brt.sizeDelta = new Vector2(4f, 0f);

        var label = new GameObject("S");
        label.transform.SetParent(go.transform, false);
        var text = label.AddComponent<Text>();
        text.font = PixelUi.Font;
        text.fontSize = 18;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = shortLabel;
        text.raycastTarget = false;
        var trt = text.rectTransform;
        trt.anchorMin = new Vector2(0f, 0f);
        trt.anchorMax = new Vector2(0.85f, 1f);
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        if (stacks > 1)
        {
            var nGo = new GameObject("N");
            nGo.transform.SetParent(go.transform, false);
            var nt = nGo.AddComponent<Text>();
            nt.font = PixelUi.Font;
            nt.fontSize = 14;
            nt.alignment = TextAnchor.LowerRight;
            nt.color = UiStyle.Gold;
            nt.text = "×" + stacks;
            nt.raycastTarget = false;
            nt.horizontalOverflow = HorizontalWrapMode.Overflow;
            nt.verticalOverflow = VerticalWrapMode.Overflow;
            var nrt = nt.rectTransform;
            nrt.anchorMin = new Vector2(0.5f, 0f);
            nrt.anchorMax = new Vector2(1f, 0.35f);
            nrt.offsetMin = Vector2.zero;
            nrt.offsetMax = new Vector2(-2f, 0f);
        }

        return go;
    }
}
