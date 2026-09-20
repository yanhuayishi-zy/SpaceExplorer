using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// 剧情过场：序章 / 十三宫 / 终章 / 无尽引子
public static class StoryData
{
    public const string KeyPrologue = "ST_Prologue";
    public const string KeyEpilogue = "ST_Epilogue";
    public const string KeyEndless = "ST_Endless";
    public const string KeyMid4 = "ST_Mid4";
    public const string KeyMid8 = "ST_Mid8";
    public const string KeyMid12 = "ST_Mid12";
    public const string KeyEndless10 = "ST_EW10";
    public const string KeyEndless20 = "ST_EW20";
    public const string KeyEndless35 = "ST_EW35";
    public static string KeyLevel(int lv) => "ST_Lv" + lv;
    public static string KeyClear(int lv) => "ST_Clear_" + lv;

    public static bool Seen(string key) => PlayerPrefs.GetInt(key, 0) == 1;

    public static void MarkSeen(string key)
    {
        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
    }

    public class Beat
    {
        public string speaker; // 空 = 旁白
        public string text;

        public Beat(string speaker, string text)
        {
            this.speaker = speaker;
            this.text = text;
        }
    }

    public static readonly Beat[] Prologue =
    {
        new Beat("星历官·墨提斯", "纪元末年的星历上，奥林匹斯神舰被一笔划去。诸神沉默，黄道十三宫只剩灯火在真空里明灭。"),
        new Beat("神舰·阿尔戈", "扫描异常：时之座裂解。污染指数上升。十二宫使魔信号…已被改写。"),
        new Beat("奥林匹斯回响", "『唯有继承诸神之翼的航者，能逐宫净化黄道，再临时之座，令星辰重新走回它们的轨道。』"),
        new Beat("航者", "……回响既然选中了我，那便出发吧。子技能破局，终极火力定胜负——十三宫，我来了。"),
        new Beat("星历官·墨提斯", "于是，我把这一册星历留给你。你的每一次击坠，都会成为新的一页。"),
    };

    public static readonly Beat[] Epilogue =
    {
        new Beat("神舰·阿尔戈", "时之座…崩解完成。污染清除。黄道灯火序列：重新点亮。"),
        new Beat("星历官·墨提斯", "你击碎了旧主的执念。星座将重新运行——而你的名字，会写进下一册星历的扉页。"),
        new Beat("航者", "神话没有终点。只要星图还在回响，我就会再一次起飞。"),
        new Beat("奥林匹斯回响", "『深渊仍在低语。若你愿意，无尽的回响将再次为航者开门。』"),
    };

    public static readonly Beat[] EndlessIntro =
    {
        new Beat("星历官·墨提斯", "在黄道主航线之外，深渊回响从未沉寂。破碎的星座碎片在虚空中重组，一波又一波，没有尽头。"),
        new Beat("神舰·阿尔戈", "检测到无限循环星图。建议：保留火力，记录每一分航程。"),
        new Beat("奥林匹斯回响", "『这里是神话的回声室。分数是里程，强化是补给——别让灯火在你手中熄灭。』"),
        new Beat("星历官·墨提斯", "每 1200 分可选择强化；Boss 生命降至 45% 时会锁血，持续攻击才能破防。活着，写下去。"),
    };

    public static readonly Beat[] Mid4 =
    {
        new Beat("星历官·墨提斯", "四盏宫灯已净。使魔的嘶吼开始变得整齐——仿佛背后有一只看不见的手，在拨动星座的指针。"),
        new Beat("奥林匹斯回响", "『半途的航者，狮子宫的鬃火正等着你。王者的咆哮会撕开你的心防。』"),
        new Beat("航者", "那就让它撕。只要引擎还转，我就不掉头。"),
    };

    public static readonly Beat[] Mid8 =
    {
        new Beat("神舰·阿尔戈", "八宫过半。毒针、箭雨与审判的天平，都在航线上留下灼痕。装甲损耗可接受。"),
        new Beat("星历官·墨提斯", "摩羯宫之后，星泉将倾泻而下。黄道的后半段，已开始为旧主守灵。"),
        new Beat("航者", "守灵也好，赴宴也罢——我只认准下一片星图。"),
    };

    public static readonly Beat[] Mid12 =
    {
        new Beat("星历官·墨提斯", "十二宫灯火尽亮。前方只剩终焉的时之座——那是时间本身为敌的战场。"),
        new Beat("奥林匹斯回响", "『克洛诺斯会把你的航程塞进沙漏，一次次倒转。唯有破防的瞬间，能刺穿他的执念。』"),
        new Beat("航者", "破防……我记住了。走吧，去结束这一切。"),
    };

    public static readonly Beat[] Endless10 =
    {
        new Beat("神舰·阿尔戈", "深渊第十波。使魔残影开始叠加，像被同一段噩梦反复播放。"),
        new Beat("航者", "第十波……心跳和射速对上了。再来。"),
    };

    public static readonly Beat[] Endless20 =
    {
        new Beat("星历官·墨提斯", "第二十波。星图碎片拼出残缺宫位，却再没有完整的灯火。"),
        new Beat("奥林匹斯回响", "『你已深入回响的核心。分数会被铭记，也会被下一次风暴抹去。』"),
    };

    public static readonly Beat[] Endless35 =
    {
        new Beat("神舰·阿尔戈", "警告：传说级能量波动接近。它们…在对你弯腰。"),
        new Beat("航者", "不是臣服，是饥饿。那就让它们都来。黄道不灭，我就不停。"),
    };

    /// 单宫剧情：丰化 lore + Boss 台词
    public static Beat[] LevelBeats(ZodiacLevels.LevelDef def)
    {
        if (def == null) return System.Array.Empty<Beat>();
        string boss = def.bossName;
        switch (def.index)
        {
            case 1:
                return new[]
                {
                    new Beat("星历官·墨提斯", "白羊宫的灯火呈血色。风从未知处刮来，带着金羊毛燃烧的气息。"),
                    new Beat(boss, "『疾风是我的姓名。你若停下，便会被我踏碎。』"),
                    new Beat("航者", "那就比一比，谁更快。"),
                    new Beat("神舰·阿尔戈", "目标锁定：" + boss + "。净化本宫，可点亮第一盏灯火。"),
                };
            case 2:
                return new[]
                {
                    new Beat("神舰·阿尔戈", "金牛宫星轨沉重。巨角形能量场正在校准冲撞路径。"),
                    new Beat(boss, "『欧罗巴的泪已干，只剩冲撞。来，用你的翅膀试我的角。』"),
                    new Beat("星历官·墨提斯", def.lore),
                };
            case 3:
                return new[]
                {
                    new Beat("星历官·墨提斯", "双子宫的光一分为二。你看向左，右舷便传来冷笑。"),
                    new Beat("狄俄斯库里双子", "『我们从不单挑。镜像里的自己，也是敌人。』"),
                    new Beat("航者", "好。那我一次打两个。"),
                };
            case 4:
                return new[]
                {
                    new Beat("神舰·阿尔戈", "巨蟹宫装甲读数异常厚。硬壳合拢时，连星光都会被夹碎。"),
                    new Beat(boss, "『赫拉的守卫从不后退。硬壳之内，是猎场。』"),
                    new Beat("星历官·墨提斯", def.lore + " 破壳，才能见血。"),
                };
            case 5:
                return new[]
                {
                    new Beat("星历官·墨提斯", "狮子宫的鬃火炸开，弹幕如怒吼般扑面。王者从不躲藏。"),
                    new Beat("涅墨亚之狮", "『涅墨亚的兽皮刀枪不入。你的舰炮，够锋利吗？』"),
                    new Beat("航者", "不够就再开一炮。"),
                };
            case 6:
                return new[]
                {
                    new Beat("神舰·阿尔戈", "处女宫下起纯净之雨。每一滴都像是对肮脏航迹的审判。"),
                    new Beat("阿斯特赖亚", "『星雨会洗去污秽，也会洗去多余的存在。』"),
                    new Beat("奥林匹斯回响", "『半程将至。航者，别让雨水浇灭引擎的火。』"),
                    new Beat("星历官·墨提斯", def.lore),
                };
            case 7:
                return new[]
                {
                    new Beat("星历官·墨提斯", "天秤宫两侧同时压来。平衡与审判，本就是同一种重量。"),
                    new Beat(boss, "『左边是罪，右边是罚。航者，你更重在哪一边？』"),
                    new Beat("航者", "我选择把秤砸了。"),
                };
            case 8:
                return new[]
                {
                    new Beat("神舰·阿尔戈", "天蝎宫暗影潜行。毒针藏在星光缝隙，等你露出破绽。"),
                    new Beat(boss, "『猎户曾败于我。你？不过是下一枚针下的星屑。』"),
                    new Beat("星历官·墨提斯", def.lore),
                };
            case 9:
                return new[]
                {
                    new Beat("星历官·墨提斯", "射手宫万箭齐发。贤者喀戎的箭矢，从不射向无罪之人——而你已被判为‘入侵者’。"),
                    new Beat("喀戎", "『英雄也好，罪人也好，在我的射程里都只是一条航迹。』"),
                    new Beat("航者", "那就在箭落下之前，先冲过去。"),
                };
            case 10:
                return new[]
                {
                    new Beat("神舰·阿尔戈", "摩羯宫：深渊攀爬者领地。礁石星轨，拒绝任何轻盈飞越。"),
                    new Beat(boss, "『海羊自深海登顶。航者，你可曾像我一样向上爬过？』"),
                    new Beat("星历官·墨提斯", def.lore),
                };
            case 11:
                return new[]
                {
                    new Beat("星历官·墨提斯", "水瓶宫倾泻星泉。斟酒者的传说被改写成洪水——弹幕如雨，没有尽头。"),
                    new Beat("伽倪墨得斯", "『我斟下的不是酒，是坠落的星辰。喝下去，或被淹没。』"),
                    new Beat("神舰·阿尔戈", def.lore),
                };
            case 12:
                return new[]
                {
                    new Beat("神舰·阿尔戈", "双鱼宫：丝线缠绕，双向游弋。注意交叉火力。"),
                    new Beat("海缚双鱼", "『我们从不单独游动。缠住你，便缠住了整条航路。』"),
                    new Beat("奥林匹斯回响", "『最后一盏宫灯。过了这里，便是时间的王座。』"),
                    new Beat("星历官·墨提斯", def.lore),
                };
            case 13:
                return new[]
                {
                    new Beat("星历官·墨提斯", "你穿过了十二宫的灯火，前方不再是星座，而是时间的王座。沙漏悬空，刻度倒转。"),
                    new Beat("克洛诺斯", "『蝼蚁也敢篡改星辰？让你们的航程，在我的沙漏里重头再来。』"),
                    new Beat("航者", "那就再来一遍——直到沙漏碎掉。"),
                    new Beat("奥林匹斯回响", "『生命降至 45% 时，他会锁血。集中攻击破防，才能刺穿他的执念。』"),
                    new Beat("神舰·阿尔戈", def.lore),
                };
            default:
                return new[]
                {
                    new Beat("星历官·墨提斯", "黄道航路 · 第 " + def.index + " 宫 · " + def.title + "。" + def.lore),
                    new Beat("神舰·阿尔戈", "Boss：" + boss + "。净化本宫，才能点亮下一盏灯火。"),
                };
        }
    }

    public static Beat[] Milestone(int level)
    {
        if (level == 4) return Mid4;
        if (level == 8) return Mid8;
        if (level == 12) return Mid12;
        return System.Array.Empty<Beat>();
    }

    public static string TacticalHint(int level)
    {
        switch (level)
        {
            case 1: return "冲锋前会锁定航线，横移后立刻反击";
            case 2: return "贴住横扫反方向，利用转身空档输出";
            case 3: return "双侧交叉射击，优先守住中线";
            case 4: return "钳形弹幕会合拢，提前寻找外侧缺口";
            case 5: return "鬃火环有固定间隙，切勿追着弹幕移动";
            case 6: return "星雨落点分批出现，小幅移动比急转更安全";
            case 7: return "天秤从两侧施压，保持左右空间";
            case 8: return "尾针蓄力后直射，看到瞄准线立即变向";
            case 9: return "箭雨追踪当前航向，用折线机动误导齐射";
            case 10: return "落石速度较慢，先清出一条安全航道";
            case 11: return "星泉扇面交替倾泻，跟随上一轮空隙";
            case 12: return "双鱼弹幕成对交叉，靠近屏幕下方观察";
            case 13: return "Boss 生命降至 45% 后集中火力破防，再在易伤期释放大招";
            default: return "保持连击积累大招，在 45% 生命护盾出现后集中破防";
        }
    }

    /// 首次击败每宫后的线索，让关卡结果继续推动主线。
    public static Beat[] VictoryBeats(ZodiacLevels.LevelDef def)
    {
        if (def == null) return System.Array.Empty<Beat>();
        switch (def.index)
        {
            case 1: return new[] { new Beat(def.bossName, "『风停了……可拨动星轨的，并不是我。』"), new Beat("神舰·阿尔戈", "取得异常刻度碎片。它来自尚未抵达的时之座。") };
            case 2: return new[] { new Beat(def.bossName, "『那道命令，让我守住一段早已过去的时间。』"), new Beat("星历官·墨提斯", "第二枚刻度与第一枚严丝合缝。有人把十二宫做成了沙漏。") };
            case 3: return new[] { new Beat("狄俄斯库里双子", "『我们看见两个你：一个前进，一个从终点归来。』"), new Beat("航者", "若终点的我已经回来，就说明这条路走得通。") };
            case 4: return new[] { new Beat(def.bossName, "『硬壳守护的不是宫殿，是旧主不肯醒来的梦。』"), new Beat("星历官·墨提斯", "四宫净化。幕后之手正从时之座收紧星轨。") };
            case 5: return new[] { new Beat("涅墨亚之狮", "『王者会败，恐惧却会披上下一张兽皮。』"), new Beat("航者", "那我就把下一张也撕开。") };
            case 6: return new[] { new Beat("阿斯特赖亚", "『我审判过无数航者。你是第一个没有被抹去名字的人。』"), new Beat("神舰·阿尔戈", "航者身份校验完成：你不属于克洛诺斯记录的任何一条时间线。") };
            case 7: return new[] { new Beat(def.bossName, "『秤盘之外，竟还有第三种答案。』"), new Beat("星历官·墨提斯", "不是胜利，也不是失败。你正在创造旧星历中不存在的未来。") };
            case 8: return new[] { new Beat(def.bossName, "『毒针刺不中没有过去的人……』"), new Beat("航者", "我有过去。只是不打算让它决定下一步。") };
            case 9: return new[] { new Beat("喀戎", "『箭已经看见终点：王座之后，是一颗尚未命名的新星。』"), new Beat("神舰·阿尔戈", "导航更新。目标仍是时之座，但终点坐标正在改变。") };
            case 10: return new[] { new Beat(def.bossName, "『我从深渊爬向王座，却忘了为何出发。别重蹈我的覆辙。』"), new Beat("航者", "我记得。为了让所有宫灯重新转动。") };
            case 11: return new[] { new Beat("伽倪墨得斯", "『最后一杯星泉，留给打碎沙漏的人。』"), new Beat("星历官·墨提斯", "时之座屏障正在退潮。只剩双鱼宫的最后一道星缚。") };
            case 12: return new[] { new Beat("海缚双鱼", "『丝线已断。去吧，别让旧主再把我们缝回昨日。』"), new Beat("奥林匹斯回响", "『十二灯同燃。通往时间王座的门，已经开启。』") };
            case 13: return new[] { new Beat("克洛诺斯", "『原来时间并非圆环……而是你身后不断亮起的航迹。』"), new Beat("航者", "航迹会消散，但方向由我们自己选择。") };
            default: return System.Array.Empty<Beat>();
        }
    }

    public static string MilestoneKey(int level)
    {
        if (level == 4) return KeyMid4;
        if (level == 8) return KeyMid8;
        if (level == 12) return KeyMid12;
        return null;
    }

    public static Beat[] EndlessWaveStory(int wave)
    {
        if (wave >= 35) return Endless35;
        if (wave >= 20) return Endless20;
        if (wave >= 10) return Endless10;
        return System.Array.Empty<Beat>();
    }

    public static string EndlessWaveKey(int wave)
    {
        if (wave >= 35) return KeyEndless35;
        if (wave >= 20) return KeyEndless20;
        if (wave >= 10) return KeyEndless10;
        return null;
    }
}

/// 剧情 UI：暗场 + 对白 + 继续/跳过
public class StoryUI : MonoBehaviour
{
    static StoryUI instance;
    static bool waiting;
    public static bool IsPlaying { get; private set; }
    static Text lastSpeaker;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (instance != null) return;
        var go = new GameObject("StoryUI");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<StoryUI>();
    }

    public static IEnumerator Play(string markKey, StoryData.Beat[] beats)
    {
        if (beats == null || beats.Length == 0) yield break;
        // 已看过则不播（打同关不重复）
        if (!string.IsNullOrEmpty(markKey) && StoryData.Seen(markKey)) yield break;
        if (instance == null) yield break;
        yield return instance.PlayCo(markKey, beats);
    }

    public static void Replay(StoryData.Beat[] beats)
    {
        if (instance == null || IsPlaying || beats == null || beats.Length == 0) return;
        instance.StartCoroutine(instance.ReplayCo(beats));
    }

    IEnumerator ReplayCo(StoryData.Beat[] beats)
    {
        // 避开点击“回放”的同一帧，防止该次点击直接翻过第一句。
        yield return null;
        yield return PlayCo(null, beats);
    }

    IEnumerator PlayCo(string markKey, StoryData.Beat[] beats)
    {
        float previousTimeScale = Time.timeScale;
        IsPlaying = true;
        Time.timeScale = 0f;
        var canvasGo = new GameObject("StoryCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 360;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        var dimGo = new GameObject("Dim");
        dimGo.transform.SetParent(canvasGo.transform, false);
        var dim = dimGo.AddComponent<Image>();
        dim.color = new Color(0.01f, 0.02f, 0.06f, 0.92f);
        dim.raycastTarget = true;
        Stretch(dim.rectTransform);

        var title = MakeText(canvasGo.transform, "剧情", 20, new Vector2(0, 320), UiStyle.TextMuted);

        // 底部对白框
        var boxGo = new GameObject("DlgBox", typeof(RectTransform), typeof(Image));
        boxGo.transform.SetParent(canvasGo.transform, false);
        var boxImg = boxGo.GetComponent<Image>();
        boxImg.color = new Color(0.04f, 0.06f, 0.12f, 0.92f);
        boxImg.raycastTarget = true;
        var boxRt = (RectTransform)boxGo.transform;
        boxRt.anchorMin = new Vector2(0.5f, 0f);
        boxRt.anchorMax = new Vector2(0.5f, 0f);
        boxRt.pivot = new Vector2(0.5f, 0f);
        boxRt.anchoredPosition = new Vector2(0f, 48f);
        boxRt.sizeDelta = new Vector2(980f, 260f);

        var edge = new GameObject("Edge", typeof(RectTransform), typeof(Image));
        edge.transform.SetParent(boxGo.transform, false);
        var eimg = edge.GetComponent<Image>();
        eimg.color = new Color(UiStyle.Cyan.r, UiStyle.Cyan.g, UiStyle.Cyan.b, 0.45f);
        eimg.raycastTarget = false;
        var ert = (RectTransform)edge.transform;
        ert.anchorMin = new Vector2(0f, 1f);
        ert.anchorMax = new Vector2(1f, 1f);
        ert.pivot = new Vector2(0.5f, 1f);
        ert.anchoredPosition = Vector2.zero;
        ert.sizeDelta = new Vector2(0f, 3f);

        var speaker = MakeText(boxGo.transform, "", 30, Vector2.zero, UiStyle.Gold);
        var sprRt = speaker.rectTransform;
        sprRt.anchorMin = sprRt.anchorMax = new Vector2(0.5f, 1f);
        sprRt.pivot = new Vector2(0.5f, 1f);
        sprRt.anchoredPosition = new Vector2(-320f, -12f);
        sprRt.sizeDelta = new Vector2(280f, 40f);
        speaker.alignment = TextAnchor.MiddleLeft;
        StoryUI.lastSpeaker = speaker;

        var body = MakeText(boxGo.transform, "", 28, Vector2.zero, UiStyle.TextPrimary);
        var brt = body.rectTransform;
        brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 1f);
        brt.pivot = new Vector2(0.5f, 1f);
        brt.anchoredPosition = new Vector2(0f, -64f);
        brt.sizeDelta = new Vector2(880f, 160f);
        body.verticalOverflow = VerticalWrapMode.Overflow;
        body.alignment = TextAnchor.UpperLeft;

        // 人物立绘：对话框上方
        var portGo = new GameObject("Portrait");
        portGo.transform.SetParent(canvasGo.transform, false);
        var port = portGo.AddComponent<Image>();
        port.preserveAspect = true;
        port.raycastTarget = false;
        var prt = port.rectTransform;
        prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0f);
        prt.pivot = new Vector2(0.5f, 0f);
        prt.anchoredPosition = new Vector2(-340f, 320f);
        prt.sizeDelta = new Vector2(280f, 320f);

        var cont = MakeText(canvasGo.transform, "点击或轻触继续", 18,
            new Vector2(380f, 0), UiStyle.TextMuted);
        var crt = cont.rectTransform;
        crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0f);
        crt.pivot = new Vector2(0.5f, 0f);
        crt.anchoredPosition = new Vector2(340f, 28f);

        bool skip = false;
        bool skipRequested = false;
        var skipGo = new GameObject("Skip", typeof(RectTransform), typeof(Image), typeof(Button));
        skipGo.transform.SetParent(canvasGo.transform, false);
        var skipImage = skipGo.GetComponent<Image>();
        skipImage.color = new Color(0.12f, 0.16f, 0.24f, 0.92f);
        var skipButton = skipGo.GetComponent<Button>();
        skipButton.targetGraphic = skipImage;
        skipButton.onClick.AddListener(() => skipRequested = true);
        var skipRt = (RectTransform)skipGo.transform;
        skipRt.anchorMin = skipRt.anchorMax = new Vector2(1f, 1f);
        skipRt.pivot = new Vector2(1f, 1f);
        skipRt.anchoredPosition = new Vector2(-48f, -40f);
        skipRt.sizeDelta = new Vector2(132f, 52f);
        var skipLabel = MakeText(skipGo.transform, "跳过", 20, Vector2.zero, UiStyle.TextSecondary);
        Stretch(skipLabel.rectTransform);

        for (int i = 0; i < beats.Length && !skip; i++)
        {
            var b = beats[i];
            speaker.text = string.IsNullOrEmpty(b.speaker) ? "旁白" : b.speaker;
            speaker.color = SpeakerColor(b.speaker);
            body.text = b.text;
            title.text = "剧情  " + (i + 1) + " / " + beats.Length;

            var spr = LoadSpeakerPortrait(b.speaker);
            port.sprite = spr;
            port.color = spr != null ? Color.white : new Color(0f, 0f, 0f, 0f);

            waiting = true;
            while (waiting && !skip)
            {
                if (skipRequested || Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
                {
                    skip = true;
                    break;
                }
                bool touchBegan = Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
                if (Input.anyKeyDown || Input.GetMouseButtonDown(0) || touchBegan)
                    waiting = false;
                yield return null;
            }
            yield return null;
        }

        if (!string.IsNullOrEmpty(markKey)) StoryData.MarkSeen(markKey);
        Destroy(canvasGo);
        IsPlaying = false;
        Time.timeScale = previousTimeScale;
    }

    static Sprite LoadSpeakerPortrait(string speaker)
    {
        if (string.IsNullOrEmpty(speaker) || speaker == "旁白") return null;
        if (speaker.Contains("墨提斯") || speaker.Contains("星历"))
            return Resources.Load<Sprite>("StoryPortrait_metis");
        if (speaker.Contains("阿尔戈") || speaker.Contains("神舰"))
            return Resources.Load<Sprite>("StoryPortrait_argo");
        if (speaker.Contains("回响") || speaker.Contains("奥林匹斯") || speaker.Contains("神谕"))
            return Resources.Load<Sprite>("StoryPortrait_echo");
        if (speaker.Contains("克洛诺斯") || speaker.Contains("泰坦"))
            return Resources.Load<Sprite>("StoryPortrait_kronos");
        if (speaker.Contains("航者"))
            return Resources.Load<Sprite>("StoryPortrait_pilot");
        return null;
    }

    static Color SpeakerColor(string speaker)
    {
        if (string.IsNullOrEmpty(speaker) || speaker == "旁白") return UiStyle.TextSecondary;
        if (speaker.Contains("墨提斯") || speaker.Contains("星历")) return new Color(0.75f, 0.85f, 1f);
        if (speaker.Contains("阿尔戈") || speaker.Contains("神舰")) return new Color(0.45f, 0.95f, 1f);
        if (speaker.Contains("回响") || speaker.Contains("奥林匹斯") || speaker.Contains("神谕")) return new Color(0.65f, 0.9f, 1f);
        if (speaker.Contains("克洛诺斯") || speaker.Contains("泰坦")) return new Color(0.9f, 0.45f, 0.35f);
        if (speaker.Contains("航者")) return new Color(0.7f, 1f, 0.85f);
        return new Color(1f, 0.75f, 0.55f);
    }

    static Text MakeText(Transform parent, string content, int size, Vector2 pos, Color color)
    {
        var go = new GameObject("Txt");
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<Text>();
        text.font = PixelUi.Font;
        text.fontSize = size;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.text = content;
        text.supportRichText = false;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        var rt = text.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(900f, size + 20);
        return text;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
