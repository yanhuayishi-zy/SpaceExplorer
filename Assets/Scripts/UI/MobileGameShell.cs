using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// 手游风主界面闸门：首次设 ID，之后显示开始/机库/排行榜
/// 无尽模式：关掉胜利，死亡结算 + 上榜
public class MobileGameShell : MonoBehaviour
{
    static MobileGameShell instance;

    GameObject profileGate;
    GameObject modeGate;
    GameObject hudRoot;
    GameObject pauseRoot;
    GameObject pauseMenu;
    InputField idInput;
    Toggle hideToggle;
    Text idErrorText;
    Text idHintText;
    Text idLabel;
    Text scoreLabel;
    Text waveLabel;
    Text coinLabel;
    Text boardText;
    bool shellBuilt;
    bool gameStarted;
    int coinsAtRunStart;
    public static bool IsMenuOpen { get; private set; }
    int lastScore = -1;
    int lastWave = -1;
    int lastCoins = -1;
    public static bool HandlesPauseHotkey => instance != null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Object.FindObjectOfType<MobileGameShell>() != null) return;

        // 先保证有 EventSystem
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            Object.DontDestroyOnLoad(es);
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        var go = new GameObject("MobileGameShell");
        Object.DontDestroyOnLoad(go);
        instance = go.AddComponent<MobileGameShell>();
    }

    void Start()
    {
        BuildShell();
        // 本机已有 ID：直接进模式选择，不再要求输入
        if (PlayerProfile.ProfileReady)
        {
            ShowProfileGate(false);
            ShowModeGate();
            ApplyIdToHud();
            Time.timeScale = 0f;
        }
        else
        {
            ShowProfileGate(true);
            Time.timeScale = 0f;
        }
    }

    void BuildShell()
    {
        if (shellBuilt) return;
        shellBuilt = true;

        var canvasGo = new GameObject("MobileShellCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1f;
        MobileTuning.ConfigureCanvas(scaler);
        canvasGo.AddComponent<GraphicRaycaster>();

        // 确保有 EventSystem，否则按钮点不动
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        BuildProfileGate(canvasGo.transform);
        BuildModeGate(canvasGo.transform);
        BuildTopHud(canvasGo.transform);
        BuildPauseHint(canvasGo.transform);
        BuildPauseMenu(canvasGo.transform);
    }

    GameObject modeMainPanel;
    GameObject levelSelectPanel;

    void BuildModeGate(Transform canvas)
    {
        modeGate = new GameObject("ModeGate");
        modeGate.transform.SetParent(canvas, false);
        var bg = modeGate.AddComponent<Image>();
        bg.color = UiStyle.BgDeep;
        Stretch(modeGate.GetComponent<RectTransform>());

        // ---- 主模式页：只留玩法 + 机库 + 档案 ----
        modeMainPanel = MakePanel("ModeMain", modeGate.transform);

        MakeText(modeMainPanel.transform, "黄道征途", 62, new Vector2(0, 300), UiStyle.Cyan);
        UiStyle.AddTitleUnderline(modeMainPanel.transform, new Vector2(0, 255), 200f, UiStyle.Gold);
        MakeText(modeMainPanel.transform, "十三宫征途 · 无尽挑战", 22, new Vector2(0, 220), UiStyle.TextSecondary);

        var btnZodiac = MakeBigButton(modeMainPanel.transform, "征战之路", new Vector2(0, 110), () =>
        {
            ShowLevelSelect(true);
        }, UiStyle.Blue, 460f, 100f);
        UiStyle.AddAccentEdge(btnZodiac.GetComponent<Image>(), UiStyle.Cyan, 8f);
        MakeText(modeMainPanel.transform, "逐关讨伐星座 Boss", 18, new Vector2(0, 58), UiStyle.TextMuted);

        var btnEndless = MakeBigButton(modeMainPanel.transform, "无尽模式", new Vector2(0, -50), () =>
        {
            ZodiacLevels.IsCampaign = false;
            StartGameFromGate();
        }, UiStyle.Orange, 460f, 100f);
        UiStyle.AddAccentEdge(btnEndless.GetComponent<Image>(), UiStyle.Gold, 8f);
        MakeText(modeMainPanel.transform, "波次成长 · 肉鸽强化", 18, new Vector2(0, -102), UiStyle.TextMuted);

        // 次要入口：机库 / 档案
        var btnShop = MakeBigButton(modeMainPanel.transform, "机库", new Vector2(-140, -200), () =>
        {
            ShipShopUI.OpenFromMenu();
        }, new Color(0.18f, 0.48f, 0.40f), 240f, 76f);
        UiStyle.AddAccentEdge(btnShop.GetComponent<Image>(), UiStyle.Green, 6f);

        MakeBigButton(modeMainPanel.transform, "档案", new Vector2(140, -200), () =>
        {
            OpenProfile(0);
        }, new Color(0.42f, 0.32f, 0.58f), 240f, 76f);

        // 当前机体 + 头像 + 称号
        var avGo = new GameObject("MainAvatar");
        avGo.transform.SetParent(modeMainPanel.transform, false);
        mainAvatarImg = avGo.AddComponent<Image>();
        mainAvatarImg.preserveAspect = true;
        mainAvatarImg.raycastTarget = false;
        var avRt = mainAvatarImg.rectTransform;
        avRt.anchorMin = avRt.anchorMax = new Vector2(0.5f, 0.5f);
        avRt.anchoredPosition = new Vector2(-280f, -290f);
        avRt.sizeDelta = new Vector2(88f, 88f);

        modeShipLabel = MakeText(modeMainPanel.transform, "当前：—", 22,
            new Vector2(40, -275), UiStyle.TextSecondary);
        modeShipLabel.alignment = TextAnchor.MiddleLeft;
        modeShipLabel.rectTransform.sizeDelta = new Vector2(420f, 36f);

        modeUnlockLabel = MakeText(modeMainPanel.transform, "已解锁至第 " + ZodiacLevels.UnlockedLevel + " 关", 22,
            new Vector2(40, -315), UiStyle.Gold);
        modeUnlockLabel.alignment = TextAnchor.MiddleLeft;
        modeUnlockLabel.rectTransform.sizeDelta = new Vector2(420f, 32f);

        BuildProfileHub(modeGate.transform);

        // ---- 关卡选择页：征战之路 ----
        levelSelectPanel = MakePanel("LevelSelect", modeGate.transform);

        MakeText(levelSelectPanel.transform, "征战之路", 56, new Vector2(0, 310), UiStyle.Gold);
        UiStyle.AddTitleUnderline(levelSelectPanel.transform, new Vector2(0, 268), 160f, UiStyle.Cyan);
        MakeText(levelSelectPanel.transform, "逐宫讨伐 · 击败 Boss 解锁下一关", 22, new Vector2(0, 240), UiStyle.TextSecondary);

        MakeBigButton(levelSelectPanel.transform, "返回", new Vector2(-360, 370), () =>
        {
            ShowLevelSelect(false);
        }, new Color(0.22f, 0.26f, 0.36f), 160f, 56f);

        BuildCampaignPath(levelSelectPanel.transform);

        levelUnlockLabel = MakeText(levelSelectPanel.transform, "已解锁至第 " + ZodiacLevels.UnlockedLevel + " 关", 22,
            new Vector2(0, -320), UiStyle.Gold);

        ShowLevelSelect(false);
        modeGate.SetActive(false);
    }

    GameObject profilePanel;
    Image mainAvatarImg;
    readonly List<Image> profileTabs = new List<Image>();
    int profileTab;
    Text boardBody;

    /// 档案枢纽：页签 + 内容区；内容每次切页重建，避免空引用
    Transform profileBody;
    readonly List<GameObject> profileBodyItems = new List<GameObject>();

    class StoryArchiveEntry
    {
        public string key;
        public string title;
        public string subtitle;
        public StoryData.Beat[] beats;

        public StoryArchiveEntry(string key, string title, string subtitle, StoryData.Beat[] beats)
        {
            this.key = key;
            this.title = title;
            this.subtitle = subtitle;
            this.beats = beats;
        }
    }

    void BuildProfileHub(Transform parent)
    {
        profilePanel = MakePanel("ProfilePanel", parent);
        var pImg = profilePanel.GetComponent<Image>();
        if (pImg != null) pImg.raycastTarget = true;

        // 全部顶对齐：标题 → 页签 → 内容，避免互相叠
        var titleTx = MakeText(profilePanel.transform, "档案", 52, Vector2.zero, UiStyle.Cyan);
        var trt = titleTx.rectTransform;
        trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = new Vector2(0f, -16f);
        trt.sizeDelta = new Vector2(400f, 64f);

        var under = new GameObject("Underline", typeof(RectTransform), typeof(Image));
        under.transform.SetParent(profilePanel.transform, false);
        var uimg = under.GetComponent<Image>();
        uimg.color = UiStyle.Gold;
        uimg.raycastTarget = false;
        var urt = (RectTransform)under.transform;
        urt.anchorMin = urt.anchorMax = new Vector2(0.5f, 1f);
        urt.pivot = new Vector2(0.5f, 1f);
        urt.anchoredPosition = new Vector2(0f, -78f);
        urt.sizeDelta = new Vector2(120f, 4f);

        var back = MakeBigButton(profilePanel.transform, "返回", new Vector2(-380, -36), () =>
        {
            HideExtraPanels();
            if (modeMainPanel != null) modeMainPanel.SetActive(true);
        }, new Color(0.22f, 0.26f, 0.36f), 150f, 52f);
        // 返回按钮也顶对齐
        var brt = back.GetComponent<RectTransform>();
        brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 1f);
        brt.pivot = new Vector2(0.5f, 1f);
        brt.anchoredPosition = new Vector2(-380f, -20f);

        string[] tabs = { "成就", "称号", "头像", "排行", "星历" };
        profileTabs.Clear();
        const float tabGap = 152f;
        float tabStart = -(tabs.Length - 1) * tabGap * 0.5f;
        for (int i = 0; i < tabs.Length; i++)
        {
            int idx = i;
            float x = tabStart + i * tabGap;
            var go = new GameObject("Tab_" + tabs[i], typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(profilePanel.transform, false);
            var img = go.GetComponent<Image>();
            var btn = go.GetComponent<Button>();
            img.color = new Color(0.14f, 0.18f, 0.28f);
            img.raycastTarget = true;
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.ColorTint;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(x, -100f);
            rt.sizeDelta = new Vector2(140f, 52f);
            MakeText(go.transform, tabs[i], 24, Vector2.zero, UiStyle.TextPrimary);
            profileTabs.Add(img);
            btn.onClick.AddListener(() => ShowProfileTab(idx));
        }

        // 内容根：页签下方
        var bodyGo = new GameObject("ProfileBody", typeof(RectTransform));
        bodyGo.transform.SetParent(profilePanel.transform, false);
        profileBody = bodyGo.transform;
        var brt2 = (RectTransform)profileBody;
        brt2.anchorMin = brt2.anchorMax = new Vector2(0.5f, 1f);
        brt2.pivot = new Vector2(0.5f, 1f);
        brt2.anchoredPosition = new Vector2(0f, -160f);
        brt2.sizeDelta = new Vector2(900f, 560f);

        profilePanel.SetActive(false);
    }

    void OpenProfile(int tab)
    {
        if (modeMainPanel != null) modeMainPanel.SetActive(false);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
        if (profilePanel != null) profilePanel.SetActive(true);
        ShowProfileTab(tab);
    }

    void ClearProfileBody()
    {
        foreach (var go in profileBodyItems) if (go != null) Destroy(go);
        profileBodyItems.Clear();
    }

    void ShowProfileTab(int idx)
    {
        profileTab = idx;

        for (int i = 0; i < profileTabs.Count; i++)
        {
            if (profileTabs[i] == null) continue;
            profileTabs[i].color = i == idx
                ? new Color(0.18f, 0.40f, 0.62f)
                : new Color(0.12f, 0.15f, 0.22f);
        }

        ClearProfileBody();
        Transform host = profileBody != null ? profileBody
            : (profilePanel != null ? profilePanel.transform : null);
        if (host == null) return;

        switch (idx)
        {
            case 0: BuildAchievementsInto(host); break;
            case 1: BuildTitlesInto(host); break;
            case 2: BuildAvatarsInto(host); break;
            case 3: BuildBoardInto(host); break;
            case 4: BuildStoryArchiveInto(host); break;
        }
    }

    void ShowExtraPanel(GameObject show)
    {
        OpenProfile(0);
    }

    void HideExtraPanels()
    {
        if (profilePanel != null) profilePanel.SetActive(false);
        ClearProfileBody();
    }

    void BuildAchievementsInto(Transform parent)
    {
        int claimable = Achievements.ClaimableCount();
        var hint = MakeText(parent, claimable > 0 ? ("可领取 " + claimable + " 项奖励") : "完成条件后可在此领取金币",
            24, Vector2.zero, UiStyle.TextSecondary);
        var hrt = hint.rectTransform;
        hrt.anchorMin = hrt.anchorMax = new Vector2(0.5f, 1f);
        hrt.pivot = new Vector2(0.5f, 1f);
        hrt.anchoredPosition = new Vector2(0, 0);
        hrt.sizeDelta = new Vector2(700f, 36f);
        profileBodyItems.Add(hint.gameObject);

        Transform content;
        var scroll = MakeTopScroll(parent, "AchScroll", out content, 800f, 360f, 40f);
        profileBodyItems.Add(scroll.gameObject);

        float y = 0f;
        float rowH = 90f;
        for (int i = 0; i < Achievements.All.Length; i++)
        {
            var def = Achievements.All[i];
            bool unlocked = Achievements.IsUnlocked(def.id);
            bool claimed = Achievements.IsClaimed(def.id);

            var row = new GameObject("AchRow" + i, typeof(RectTransform), typeof(Image));
            row.transform.SetParent(content, false);
            var rrt = (RectTransform)row.transform;
            rrt.anchorMin = new Vector2(0f, 1f);
            rrt.anchorMax = new Vector2(1f, 1f);
            rrt.pivot = new Vector2(0.5f, 1f);
            rrt.anchoredPosition = new Vector2(0f, -y);
            rrt.sizeDelta = new Vector2(0f, rowH - 10f);
            var rowBg = row.GetComponent<Image>();
            rowBg.color = unlocked ? new Color(0.10f, 0.14f, 0.22f, 0.95f) : new Color(0.07f, 0.08f, 0.11f, 0.9f);

            MakeText(row.transform, def.name, 24, new Vector2(-180, 14),
                unlocked ? UiStyle.TextPrimary : UiStyle.TextMuted);
            MakeText(row.transform, def.desc, 18, new Vector2(-180, -16),
                unlocked ? UiStyle.TextSecondary : UiStyle.TextMuted);
            MakeText(row.transform, "+" + def.coinReward + " 币", 20, new Vector2(100, 0),
                unlocked ? UiStyle.Gold : UiStyle.TextMuted);

            string btnLabel = !unlocked ? "未完成" : (claimed ? "已领取" : "领取");
            var btnGo = MakeBigButton(row.transform, btnLabel, new Vector2(300, 0), () =>
            {
                if (Achievements.TryClaim(def.id))
                {
                    ShowProfileTab(0);
                    RefreshModeGateInfo();
                }
            }, claimed ? new Color(0.25f, 0.28f, 0.32f)
                : unlocked ? UiStyle.Green
                : new Color(0.18f, 0.20f, 0.24f), 130f, 48f);
            var b = btnGo.GetComponent<Button>();
            if (b != null) b.interactable = unlocked && !claimed;

            y += rowH;
        }
        if (content is RectTransform tcr)
            tcr.sizeDelta = new Vector2(0f, Mathf.Max(460f, y + 20f));
    }

    static ScrollRect MakeTopScroll(Transform parent, string name, out Transform content,
        float width, float height, float topOffset)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        var rootImg = root.AddComponent<Image>();
        rootImg.color = new Color(0, 0, 0, 0.03f);
        rootImg.raycastTarget = false;
        var rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = rootRt.anchorMax = new Vector2(0.5f, 1f);
        rootRt.pivot = new Vector2(0.5f, 1f);
        rootRt.anchoredPosition = new Vector2(0f, -topOffset);
        rootRt.sizeDelta = new Vector2(width, height);

        var vp = new GameObject("Viewport");
        vp.transform.SetParent(root.transform, false);
        var vpImg = vp.AddComponent<Image>();
        vpImg.color = new Color(1, 1, 1, 0.01f);
        vpImg.raycastTarget = true;
        vp.AddComponent<RectMask2D>();
        var vpRt = vp.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = Vector2.zero;
        vpRt.offsetMax = Vector2.zero;

        var contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.transform.SetParent(vp.transform, false);
        content = contentGo.transform;
        var crt = (RectTransform)contentGo.transform;
        crt.anchorMin = new Vector2(0f, 1f);
        crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot = new Vector2(0.5f, 1f);
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta = new Vector2(0f, height);

        var scroll = root.AddComponent<ScrollRect>();
        scroll.viewport = vpRt;
        scroll.content = crt;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.elasticity = 0.08f;
        scroll.scrollSensitivity = 36f;
        return scroll;
    }

    void BuildStoryArchiveInto(Transform parent)
    {
        var entries = new List<StoryArchiveEntry>
        {
            new StoryArchiveEntry(StoryData.KeyPrologue, "序章 · 失落的神舰", "航者接受奥林匹斯回响", StoryData.Prologue),
        };

        for (int level = 1; level <= ZodiacLevels.Count; level++)
        {
            var def = ZodiacLevels.Get(level);
            if (def == null) continue;
            entries.Add(new StoryArchiveEntry(StoryData.KeyLevel(level),
                "第 " + level + " 宫 · " + def.title, "战前记录 · " + def.bossName,
                StoryData.LevelBeats(def)));
            entries.Add(new StoryArchiveEntry(StoryData.KeyClear(level),
                "净化记录 · " + def.title, "击破后的星历线索",
                StoryData.VictoryBeats(def)));

            string milestoneKey = StoryData.MilestoneKey(level);
            if (milestoneKey != null)
                entries.Add(new StoryArchiveEntry(milestoneKey,
                    level == 12 ? "十二宫同燃" : level + " 宫航程", "主线里程碑",
                    StoryData.Milestone(level)));
        }

        entries.Add(new StoryArchiveEntry(StoryData.KeyEpilogue, "终章 · 星轨重启", "时之座决战之后", StoryData.Epilogue));
        entries.Add(new StoryArchiveEntry(StoryData.KeyEndless, "无尽 · 回声室", "无限星图首次记录", StoryData.EndlessIntro));
        entries.Add(new StoryArchiveEntry(StoryData.KeyEndless10, "无尽 · 第十波", "使魔残影开始叠加", StoryData.Endless10));
        entries.Add(new StoryArchiveEntry(StoryData.KeyEndless20, "无尽 · 第二十波", "深入回响核心", StoryData.Endless20));
        entries.Add(new StoryArchiveEntry(StoryData.KeyEndless35, "无尽 · 第三十五波", "传说级能量波动", StoryData.Endless35));

        int unlockedCount = 0;
        for (int i = 0; i < entries.Count; i++)
            if (StoryData.Seen(entries[i].key)) unlockedCount++;

        var hint = MakeText(parent, "已收录 " + unlockedCount + " / " + entries.Count + " · 点击回放已解锁记录",
            22, Vector2.zero, UiStyle.TextSecondary);
        AnchorTop(hint.rectTransform, 0f);
        hint.rectTransform.sizeDelta = new Vector2(760f, 34f);
        profileBodyItems.Add(hint.gameObject);

        Transform content;
        var scroll = MakeTopScroll(parent, "StoryArchiveScroll", out content, 800f, 430f, 42f);
        profileBodyItems.Add(scroll.gameObject);

        const float rowHeight = 82f;
        float y = 0f;
        for (int i = 0; i < entries.Count; i++)
        {
            StoryArchiveEntry entry = entries[i];
            bool unlocked = StoryData.Seen(entry.key);
            var row = new GameObject("StoryRow" + i, typeof(RectTransform), typeof(Image));
            row.transform.SetParent(content, false);
            var rowRt = (RectTransform)row.transform;
            rowRt.anchorMin = new Vector2(0f, 1f);
            rowRt.anchorMax = new Vector2(1f, 1f);
            rowRt.pivot = new Vector2(0.5f, 1f);
            rowRt.anchoredPosition = new Vector2(0f, -y);
            rowRt.sizeDelta = new Vector2(0f, rowHeight - 8f);
            row.GetComponent<Image>().color = unlocked
                ? new Color(0.10f, 0.14f, 0.22f, 0.95f)
                : new Color(0.07f, 0.08f, 0.11f, 0.9f);

            var title = MakeText(row.transform, unlocked ? entry.title : "未解锁记录", 22,
                new Vector2(-165f, 13f), unlocked ? UiStyle.TextPrimary : UiStyle.TextMuted);
            title.alignment = TextAnchor.MiddleLeft;
            title.rectTransform.sizeDelta = new Vector2(430f, 30f);
            var subtitle = MakeText(row.transform, unlocked ? entry.subtitle : "继续推进航程以解锁", 17,
                new Vector2(-165f, -15f), unlocked ? UiStyle.TextSecondary : UiStyle.TextMuted);
            subtitle.alignment = TextAnchor.MiddleLeft;
            subtitle.rectTransform.sizeDelta = new Vector2(430f, 25f);

            StoryData.Beat[] capturedBeats = entry.beats;
            var button = MakeBigButton(row.transform, unlocked ? "回放" : "锁定", new Vector2(300f, 0f),
                () => StoryUI.Replay(capturedBeats),
                unlocked ? UiStyle.Blue : new Color(0.18f, 0.20f, 0.24f), 120f, 46f);
            var replayButton = button.GetComponent<Button>();
            if (replayButton != null) replayButton.interactable = unlocked;
            y += rowHeight;
        }

        if (content is RectTransform contentRt)
            contentRt.sizeDelta = new Vector2(0f, Mathf.Max(430f, y + 16f));
    }

    void BuildBoardInto(Transform parent)
    {
        var tipLb = MakeText(parent, "无尽模式 · 分数排行（仅无尽上报）", 22, Vector2.zero, UiStyle.TextSecondary);
        AnchorTop(tipLb.rectTransform, 0f);
        tipLb.rectTransform.sizeDelta = new Vector2(720f, 30f);
        profileBodyItems.Add(tipLb.gameObject);
        boardBody = MakeText(parent, "加载中…", 26, Vector2.zero, UiStyle.TextSecondary);
        boardBody.alignment = TextAnchor.UpperCenter;
        AnchorTop(boardBody.rectTransform, 36f);
        boardBody.rectTransform.sizeDelta = new Vector2(820f, 320f);
        profileBodyItems.Add(boardBody.gameObject);

        var refresh = MakeBigButton(parent, "刷新", Vector2.zero, () =>
        {
            RefreshLeaderboard();
        }, UiStyle.Blue, 160f, 52f);
        AnchorTop(refresh.GetComponent<RectTransform>(), -340f);
        profileBodyItems.Add(refresh);

        boardBody.text = "加载中…";
        try
        {
            LeaderboardClient.FetchTop(PlayerProfile.LeaderboardUrl, json =>
            {
                if (boardBody != null) boardBody.text = FormatBoard(json);
            });
        }
        catch (System.Exception e)
        {
            if (boardBody != null) boardBody.text = "暂无排行数据（未连接服务器）";
            Debug.LogWarning(e.Message);
        }
    }

    void RefreshLeaderboard()
    {
        if (boardBody == null) return;
        boardBody.text = "加载中…";
        try
        {
            LeaderboardClient.FetchTop(PlayerProfile.LeaderboardUrl, json =>
            {
                if (boardBody != null) boardBody.text = FormatBoard(json);
            });
        }
        catch (System.Exception e)
        {
            if (boardBody != null) boardBody.text = "暂无排行数据";
            Debug.LogWarning(e.Message);
        }
    }

    void BuildTitlesInto(Transform parent)
    {
        var avGo = new GameObject("CurAvatar");
        avGo.transform.SetParent(parent, false);
        var av = avGo.AddComponent<Image>();
        av.sprite = PlayerProfile.LoadAvatarSprite();
        av.preserveAspect = true;
        av.raycastTarget = false;
        AnchorTop(av.rectTransform, 0f);
        av.rectTransform.anchoredPosition = new Vector2(-320f, 0f);
        av.rectTransform.sizeDelta = new Vector2(64f, 64f);
        profileBodyItems.Add(avGo);

        string curT = Achievements.CurrentTitle;
        var curLab = MakeText(parent, "当前：" + (string.IsNullOrEmpty(curT) ? "（未装备）" : curT),
            30, Vector2.zero, UiStyle.TextPrimary);
        curLab.alignment = TextAnchor.MiddleLeft;
        AnchorTop(curLab.rectTransform, -20f);
        curLab.rectTransform.anchoredPosition = new Vector2(20f, -20f);
        curLab.rectTransform.sizeDelta = new Vector2(520f, 40f);
        profileBodyItems.Add(curLab.gameObject);

        Transform content;
        var scroll = MakeTopScroll(parent, "TitleScroll", out content, 800f, 380f, 70f);
        profileBodyItems.Add(scroll.gameObject);

        float y = 0f;
        float rowH = 92f;
        for (int i = 0; i < Achievements.All.Length; i++)
        {
            var d = Achievements.All[i];
            if (string.IsNullOrEmpty(d.title)) continue;
            bool owned = Achievements.IsUnlocked(d.id);
            bool equipped = owned && curT == d.title;

            var row = new GameObject("TRow" + i, typeof(RectTransform), typeof(Image));
            row.transform.SetParent(content, false);
            var rrt = (RectTransform)row.transform;
            rrt.anchorMin = new Vector2(0f, 1f);
            rrt.anchorMax = new Vector2(1f, 1f);
            rrt.pivot = new Vector2(0.5f, 1f);
            rrt.anchoredPosition = new Vector2(0f, -y);
            rrt.sizeDelta = new Vector2(0f, rowH - 8f);
            var rowBg = row.GetComponent<Image>();
            rowBg.color = equipped
                ? new Color(0.16f, 0.28f, 0.20f, 0.95f)
                : (owned ? new Color(0.09f, 0.13f, 0.20f, 0.92f) : new Color(0.07f, 0.08f, 0.11f, 0.85f));

            var t1 = MakeText(row.transform, d.title, 30, new Vector2(-180, 12),
                owned ? UiStyle.Gold : UiStyle.TextMuted);
            t1.alignment = TextAnchor.MiddleLeft;
            t1.rectTransform.sizeDelta = new Vector2(340f, 36f);

            var t2 = MakeText(row.transform, d.name, 20, new Vector2(-180, -16),
                owned ? UiStyle.TextSecondary : UiStyle.TextMuted);
            t2.alignment = TextAnchor.MiddleLeft;
            t2.rectTransform.sizeDelta = new Vector2(340f, 26f);

            string lab = !owned ? "未获得" : (equipped ? "已装备" : "装备");
            var btnGo = MakeBigButton(row.transform, lab, new Vector2(250, 0), () =>
            {
                Achievements.SetTitle(d.title);
                ShowProfileTab(1);
            }, equipped ? UiStyle.Green
                : owned ? UiStyle.Blue
                : new Color(0.18f, 0.20f, 0.24f), 130f, 50f);
            var b = btnGo.GetComponent<Button>();
            if (b != null) b.interactable = owned && !equipped;

            y += rowH;
        }
        if (content is RectTransform tcr)
            tcr.sizeDelta = new Vector2(0f, Mathf.Max(380f, y + 20f));
    }

    readonly List<Image> avatarSelMarks = new List<Image>();

    static void AnchorTop(RectTransform rt, float y)
    {
        if (rt == null) return;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
    }

    void BuildAvatarsInto(Transform parent)
    {
        // 机体头像网格在上
        avatarSelMarks.Clear();
        var ids = PlayerProfile.AvatarIds;
        int gridCount = ids.Length; // 含 custom，最后一格
        int shipRows = (gridCount + 4) / 5;

        for (int i = 0; i < ids.Length; i++)
        {
            string aid = ids[i];
            int col = i % 5;
            int row = i / 5;
            float x = -360f + col * 180f;
            float y = -10f - row * 180f;

            var go = new GameObject("Av_" + aid);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = PlayerProfile.LoadAvatarSprite(aid);
            img.preserveAspect = true;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.ColorTint;
            AnchorTop(img.rectTransform, y);
            img.rectTransform.anchoredPosition = new Vector2(x, y);
            img.rectTransform.sizeDelta = new Vector2(120f, 120f);
            profileBodyItems.Add(go);

            string shipName = aid == "custom" ? "自定义" : aid;
            var defs = ShipMeta.Ships;
            for (int s = 0; s < defs.Length; s++)
                if (defs[s] != null && defs[s].id == aid) { shipName = defs[s].name; break; }
            var nameTx = MakeText(parent, shipName, 20, Vector2.zero, UiStyle.TextSecondary);
            AnchorTop(nameTx.rectTransform, y - 128f);
            nameTx.rectTransform.anchoredPosition = new Vector2(x, y - 128f);
            profileBodyItems.Add(nameTx.gameObject);

            var mark = new GameObject("Mark");
            mark.transform.SetParent(go.transform, false);
            var mimg = mark.AddComponent<Image>();
            // 仅细描边，不铺满盖住立绘
            mimg.color = new Color(UiStyle.Gold.r, UiStyle.Gold.g, UiStyle.Gold.b, 0f);
            mimg.raycastTarget = false;
            var mrt = mimg.rectTransform;
            mrt.anchorMin = Vector2.zero;
            mrt.anchorMax = Vector2.one;
            mrt.offsetMin = new Vector2(-3f, -3f);
            mrt.offsetMax = new Vector2(3f, 3f);
            avatarSelMarks.Add(mimg);

            string captured = aid;
            btn.onClick.AddListener(() =>
            {
                if (captured == "custom")
                    PlayerProfile.ReloadCustomAvatar();
                PlayerProfile.AvatarId = captured;
                ShowProfileTab(2);
            });
        }

        // 自定义入口放网格下方（与头像名留够间距）
        float below = -10f - shipRows * 180f - 30f;

        string path = PlayerProfile.CustomAvatarPath;
        string status = string.IsNullOrEmpty(PlayerProfile.LastAvatarStatus)
            ? "将图片命名为 avatar.png 放入文件夹后点刷新"
            : PlayerProfile.LastAvatarStatus;
        var tip = MakeText(parent, status, 18, Vector2.zero, UiStyle.TextSecondary);
        AnchorTop(tip.rectTransform, below);
        tip.rectTransform.sizeDelta = new Vector2(860f, 26f);
        profileBodyItems.Add(tip.gameObject);

        var tip2 = MakeText(parent, "路径：" + path, 16, Vector2.zero, UiStyle.TextMuted);
        AnchorTop(tip2.rectTransform, below - 26f);
        tip2.rectTransform.sizeDelta = new Vector2(900f, 24f);
        profileBodyItems.Add(tip2.gameObject);

        var tip3 = MakeText(parent, "编辑器也可：Assets/Resources/CustomAvatar.png", 16, Vector2.zero, UiStyle.TextMuted);
        AnchorTop(tip3.rectTransform, below - 50f);
        tip3.rectTransform.sizeDelta = new Vector2(800f, 22f);
        profileBodyItems.Add(tip3.gameObject);

        var b1 = MakeBigButton(parent, "选择本地图片", new Vector2(0, 0), () =>
        {
            OpenLocalAvatarPicker(parent);
        }, UiStyle.Blue, 240f, 52f);
        AnchorTop(b1.GetComponent<RectTransform>(), below - 88f);
        b1.GetComponent<RectTransform>().anchoredPosition = new Vector2(-130f, below - 88f);
        profileBodyItems.Add(b1);

        var b2 = MakeBigButton(parent, "刷新自定义", new Vector2(0, 0), () =>
        {
            PlayerProfile.ReloadCustomAvatar();
            ShowProfileTab(2);
        }, new Color(0.30f, 0.45f, 0.55f), 200f, 52f);
        AnchorTop(b2.GetComponent<RectTransform>(), below - 88f);
        b2.GetComponent<RectTransform>().anchoredPosition = new Vector2(130f, below - 88f);
        profileBodyItems.Add(b2);

        string cur = PlayerProfile.AvatarId;
        for (int i = 0; i < avatarSelMarks.Count && i < ids.Length; i++)
        {
            if (avatarSelMarks[i] == null) continue;
            // 改为四边细条选中框，不用整块色块
            var markGo = avatarSelMarks[i].gameObject;
            if (ids[i] == cur)
            {
                if (markGo.transform.childCount == 0)
                    AddGoldFrame(markGo.transform);
            }
            else
            {
                for (int c = markGo.transform.childCount - 1; c >= 0; c--)
                    Destroy(markGo.transform.GetChild(c).gameObject);
            }
            avatarSelMarks[i].color = new Color(0f, 0f, 0f, 0f);
        }
    }

    static void AddGoldFrame(Transform parent)
    {
        void Edge(Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            var e = new GameObject("Edge", typeof(RectTransform), typeof(Image));
            e.transform.SetParent(parent, false);
            var img = e.GetComponent<Image>();
            img.color = new Color(UiStyle.Gold.r, UiStyle.Gold.g, UiStyle.Gold.b, 0.9f);
            img.raycastTarget = false;
            var rt = (RectTransform)e.transform;
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = oMin;
            rt.offsetMax = oMax;
        }
        float t = 3f;
        Edge(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -t), Vector2.zero);
        Edge(Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, t));
        Edge(Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(t, 0f));
        Edge(new Vector2(1f, 0f), Vector2.one, new Vector2(-t, 0f), Vector2.zero);
    }

    GameObject localPickPanel;

    /// 游戏内选图：列出本机图片目录中的照片
    void OpenLocalAvatarPicker(Transform host)
    {
#if UNITY_EDITOR
        if (LocalAvatarPicker.TryEditorPick())
        {
            ShowProfileTab(2);
            return;
        }
#endif
        if (localPickPanel != null) Destroy(localPickPanel);
        var root = new GameObject("LocalPickPanel", typeof(RectTransform), typeof(Image));
        localPickPanel = root;
        root.transform.SetParent(profilePanel != null ? profilePanel.transform : host, false);
        var bg = root.GetComponent<Image>();
        bg.color = new Color(0.02f, 0.04f, 0.08f, 0.96f);
        bg.raycastTarget = true;
        var rrt = (RectTransform)root.transform;
        rrt.anchorMin = rrt.anchorMax = new Vector2(0.5f, 1f);
        rrt.pivot = new Vector2(0.5f, 1f);
        rrt.anchoredPosition = new Vector2(0f, -40f);
        rrt.sizeDelta = new Vector2(920f, 620f);

        MakeText(root.transform, "从「图片 / 桌面 / 下载」选择 · 点缩略图即可", 22, Vector2.zero, UiStyle.TextSecondary);
        var hintT = root.transform.GetChild(root.transform.childCount - 1);
        if (hintT != null)
        {
            var hr = (RectTransform)hintT;
            hr.anchorMin = hr.anchorMax = new Vector2(0.5f, 1f);
            hr.pivot = new Vector2(0.5f, 1f);
            hr.anchoredPosition = new Vector2(0f, -8f);
            hr.sizeDelta = new Vector2(700f, 30f);
        }

        var closeGo = MakeBigButton(root.transform, "关闭", new Vector2(320f, -8f), () =>
        {
            if (localPickPanel != null) { Destroy(localPickPanel); localPickPanel = null; }
        }, new Color(0.22f, 0.26f, 0.36f), 140f, 48f);
        var crt2 = closeGo.GetComponent<RectTransform>();
        crt2.anchorMin = crt2.anchorMax = new Vector2(0.5f, 1f);
        crt2.pivot = new Vector2(0.5f, 1f);
        crt2.anchoredPosition = new Vector2(340f, -8f);

        var files = LocalAvatarPicker.CollectImages(20);
        if (files.Count == 0)
        {
            var empty = MakeText(root.transform, "未找到本机图片，请先把图放到「图片」或「桌面」", 20,
                Vector2.zero, UiStyle.TextMuted);
            AnchorTop(empty.rectTransform, 80f);
            empty.rectTransform.sizeDelta = new Vector2(800f, 40f);
            return;
        }

        // 网格
        for (int i = 0; i < files.Count && i < 20; i++)
        {
            string path = files[i];
            int col = i % 5;
            int row = i / 5;
            float x = -340f + col * 170f;
            float y = -70f - row * 150f;

            var go = new GameObject("Img" + i);
            go.transform.SetParent(root.transform, false);
            var img = go.AddComponent<Image>();
            img.preserveAspect = true;
            try
            {
                var bytes = File.ReadAllBytes(path);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                    img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }
            catch { }
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.ColorTint;
            var rt = img.rectTransform;
            AnchorTop(rt, y);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(140f, 120f);

            var name = MakeText(root.transform, Path.GetFileName(path), 14, Vector2.zero, UiStyle.TextMuted);
            AnchorTop(name.rectTransform, y + 124f);
            name.rectTransform.anchoredPosition = new Vector2(x, y + 124f);
            name.rectTransform.sizeDelta = new Vector2(160f, 20f);

            string cap = path;
            btn.onClick.AddListener(() =>
            {
                if (LocalAvatarPicker.UseFile(cap))
                {
                    if (localPickPanel != null) { Destroy(localPickPanel); localPickPanel = null; }
                    ShowProfileTab(2);
                }
            });
        }
    }

    ScrollRect campaignScroll;

    /// 纵向征战之路：13 关全部可见（可上下拖），每关 Boss 立绘 + 文案
    void BuildCampaignPath(Transform parent)
    {
        var root = new GameObject("PathScroll");
        root.transform.SetParent(parent, false);
        var rootImg = root.AddComponent<Image>();
        rootImg.color = new Color(0f, 0f, 0f, 0f);
        rootImg.raycastTarget = true;
        rootImg.enabled = true; // 需要可拖空白区
        var rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = rootRt.anchorMax = new Vector2(0.5f, 0.5f);
        rootRt.anchoredPosition = new Vector2(0, -10);
        rootRt.sizeDelta = new Vector2(1100f, 560f);

        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(root.transform, false);
        var vpImg = viewport.AddComponent<Image>();
        vpImg.color = new Color(0f, 0f, 0f, 0f);
        vpImg.raycastTarget = true;
        viewport.AddComponent<RectMask2D>();
        var vpRt = viewport.GetComponent<RectTransform>();
        Stretch(vpRt);

        var contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.transform.SetParent(viewport.transform, false);
        var contentRt = (RectTransform)contentGo.transform;
        // 顶对齐，方便从第 1 关往下拖
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.offsetMin = new Vector2(0f, 0f);
        contentRt.offsetMax = new Vector2(0f, 0f);

        campaignScroll = root.AddComponent<ScrollRect>();
        campaignScroll.viewport = vpRt;
        campaignScroll.content = contentRt;
        campaignScroll.horizontal = false;
        campaignScroll.vertical = true;
        campaignScroll.movementType = ScrollRect.MovementType.Elastic;
        campaignScroll.elasticity = 0.08f;
        campaignScroll.scrollSensitivity = 40f;
        campaignScroll.inertia = true;
        campaignScroll.decelerationRate = 0.12f;

        float rowH = 175f;
        float padY = 16f;
        int n = ZodiacLevels.Count;
        float contentH = padY * 2 + n * rowH;
        contentRt.sizeDelta = new Vector2(0f, contentH);

        for (int i = 1; i <= n; i++)
        {
            int lv = i;
            var def = ZodiacLevels.Get(lv);
            bool open = ZodiacLevels.IsUnlocked(lv);
            bool cleared = lv < ZodiacLevels.UnlockedLevel;
            bool current = lv == ZodiacLevels.UnlockedLevel && open;

            // 行容器
            var row = new GameObject("PathLv" + lv, typeof(RectTransform));
            row.transform.SetParent(contentGo.transform, false);
            var rowRt = (RectTransform)row.transform;
            rowRt.anchorMin = new Vector2(0f, 1f);
            rowRt.anchorMax = new Vector2(1f, 1f);
            rowRt.pivot = new Vector2(0.5f, 1f);
            rowRt.anchoredPosition = new Vector2(0f, -padY - (lv - 1) * rowH);
            rowRt.sizeDelta = new Vector2(0f, rowH - 8f);

            // 左右交错
            bool left = (lv % 2 == 1);
            float x = left ? -180f : 180f;

            // 点击热区（透明）
            var hit = new GameObject("Hit");
            hit.transform.SetParent(row.transform, false);
            var himg = hit.AddComponent<Image>();
            himg.color = new Color(0f, 0f, 0f, 0f);
            himg.raycastTarget = true;
            var hrt = himg.rectTransform;
            hrt.anchorMin = hrt.anchorMax = new Vector2(0.5f, 0.5f);
            hrt.anchoredPosition = new Vector2(x * 0.35f, 0f);
            hrt.sizeDelta = new Vector2(860f, 160f);
            var btn = hit.AddComponent<Button>();
            btn.targetGraphic = himg;
            btn.transition = Selectable.Transition.None;
            btn.interactable = open;

            // Boss 立绘
            var bossGo = new GameObject("BossArt");
            bossGo.transform.SetParent(hit.transform, false);
            var bossImg = bossGo.AddComponent<Image>();
            bossImg.preserveAspect = true;
            bossImg.raycastTarget = false;
            var bossRt = bossImg.rectTransform;
            bossRt.anchorMin = bossRt.anchorMax = new Vector2(0.5f, 0.5f);
            bossRt.anchoredPosition = new Vector2(x, 0f);
            bossRt.sizeDelta = new Vector2(160f, 160f);
            var bossSpr = Resources.Load<Sprite>("ZodiacBoss_" + def.key);
            if (bossSpr != null)
            {
                bossImg.sprite = bossSpr;
                bossImg.color = open ? Color.white : new Color(0.4f, 0.4f, 0.45f, 0.65f);
            }
            else
            {
                Debug.LogWarning("[Campaign] missing boss sprite: ZodiacBoss_" + def.key);
                bossImg.color = open ? def.bossColor * new Color(1, 1, 1, 0.5f) : new Color(0.2f, 0.2f, 0.25f, 0.3f);
            }

            // 文案块：每一关都固定写三行，格式完全一致
            float tx = left ? 160f : -160f;
            var textGo = new GameObject("Info", typeof(RectTransform));
            textGo.transform.SetParent(hit.transform, false);
            var trt = (RectTransform)textGo.transform;
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
            trt.anchoredPosition = new Vector2(tx, 0f);
            trt.sizeDelta = new Vector2(400f, 130f);

            string title = open ? def.title : "？？？";
            string boss = open ? def.bossName : "尚未解锁";
            string state = !open ? "未解锁"
                : cleared ? "已讨伐"
                : current ? "当前目标"
                : "可挑战";
            if (open)
            {
                int st = ZodiacLevels.GetStars(lv);
                if (st > 0)
                {
                    string stars = "";
                    for (int s = 0; s < 3; s++) stars += s < st ? "★" : "☆";
                    state = state + "  " + stars;
                }
            }
            Color cTitle = open ? UiStyle.TextPrimary : UiStyle.TextMuted;
            Color cBoss = open ? UiStyle.Gold : UiStyle.TextMuted;
            Color cState = cleared ? UiStyle.Green
                : current ? UiStyle.Cyan
                : open ? UiStyle.TextSecondary
                : UiStyle.TextMuted;

            MakeInfoLine(textGo.transform, "第 " + lv + " 关 · " + title, 26, 40f, cTitle);
            MakeInfoLine(textGo.transform, boss, 20, 4f, cBoss);
            MakeInfoLine(textGo.transform, state, 20, -30f, cState);

            if (open)
            {
                btn.onClick.AddListener(() =>
                {
                    ZodiacLevels.IsCampaign = true;
                    ZodiacLevels.CurrentLevel = lv;
                    StartGameFromGate();
                    AutoIntroOutro.SkipIntro();
                });
            }
        }

        StartCoroutine(ScrollCampaignToCurrent(contentRt, rowH, padY));
    }

    /// 征战之路统一信息行
    static Text MakeInfoLine(Transform parent, string content, int size, float y, Color color)
    {
        var go = new GameObject("Line");
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<Text>();
        text.font = PixelUi.Font;
        text.fontSize = size;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = color;
        text.text = content;
        text.supportRichText = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        var rt = text.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(360f, size + 10);
        return text;
    }

    System.Collections.IEnumerator ScrollCampaignToCurrent(RectTransform contentRt, float rowH, float padY)
    {
        yield return null;
        if (campaignScroll == null || contentRt == null) yield break;
        int lv = ZodiacLevels.UnlockedLevel;
        float contentH = contentRt.rect.height;
        float viewH = campaignScroll.viewport != null ? campaignScroll.viewport.rect.height : 520f;
        // 第 lv 关中心大约在 padY + (lv-0.5)*rowH（从上往下）
        float nodeY = padY + (lv - 0.5f) * rowH;
        float maxScroll = Mathf.Max(0f, contentH - viewH);
        // verticalNormalizedPosition: 1=顶, 0=底
        float fromTop = Mathf.Clamp(nodeY - viewH * 0.5f, 0f, maxScroll);
        campaignScroll.verticalNormalizedPosition = maxScroll <= 1f ? 1f : 1f - (fromTop / maxScroll);
    }

    Text modeShipLabel;
    Text modeUnlockLabel;
    Text levelUnlockLabel;

    void RefreshModeGateInfo()
    {
        var cur = ShipMeta.Current;
        if (modeShipLabel != null)
        {
            string title = Achievements.CurrentTitle;
            string line = "当前：" + cur.name + " · " + cur.skillName;
            if (!string.IsNullOrEmpty(title)) line += "  「" + title + "」";
            modeShipLabel.text = line;
        }
        if (modeUnlockLabel != null)
            modeUnlockLabel.text = "已解锁至第 " + ZodiacLevels.UnlockedLevel + " 关";
        if (levelUnlockLabel != null)
            levelUnlockLabel.text = "已解锁至第 " + ZodiacLevels.UnlockedLevel + " 关";
        if (mainAvatarImg != null)
            mainAvatarImg.sprite = PlayerProfile.LoadAvatarSprite();
    }

    void ShowLevelSelect(bool show)
    {
        HideExtraPanels();
        if (modeMainPanel != null) modeMainPanel.SetActive(!show);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(show);
        if (show) RefreshModeGateInfo();
    }

    void HideModeGate()
    {
        if (modeGate != null) modeGate.SetActive(false);
        ShowLevelSelect(false);
        IsMenuOpen = false;
        ApplyIdToHud();
    }

    void ShowModeGate()
    {
        if (pauseMenu != null) pauseMenu.SetActive(false);
        if (profileGate != null) profileGate.SetActive(false);
        if (modeGate != null) modeGate.SetActive(true);
        HideExtraPanels();
        ShowLevelSelect(false); // 回主模式页
        if (modeMainPanel != null) modeMainPanel.SetActive(true);
        RefreshModeGateInfo();
        Time.timeScale = 0f;
        IsMenuOpen = true;
    }

    public static void ShowModeGateFromGame()
    {
        if (instance == null) return;
        ClearGameOverBoards();
        instance.ShowModeGate();
    }

    /// 机库打开时藏起主菜单，避免透出标题
    public static void HideModeGateTemporarily()
    {
        if (instance == null) return;
        if (instance.modeGate != null) instance.modeGate.SetActive(false);
    }

    /// 从闸门进入对局
    void StartGameFromGate()
    {
        HideModeGate();
        if (profileGate != null) profileGate.SetActive(false);
        ClearGameOverBoards();
        coinsAtRunStart = ShipMeta.Coins;
        if (GameManager.Instance != null) GameManager.Instance.ResetRunStats();
        else Time.timeScale = 1f;
        ApplyIdToHud();
        lastScore = -1;
        lastWave = -1;
        lastCoins = -1;
        ZodiacGameFlow.ResumeFromMenu();
        AutoIntroOutro.BeginIntro();
    }

    public static void ClearGameOverBoards()
    {
        if (instance == null) return;
        for (int i = instance.transform.childCount - 1; i >= 0; i--)
        {
            var child = instance.transform.GetChild(i);
            if (child == null) continue;
            if (child.name == "GameOverBoard" || child.name == "GameOverCanvas")
            {
                Object.Destroy(child.gameObject);
            }
        }
        // 也清场景里可能残留的
        foreach (var go in Object.FindObjectsOfType<Canvas>())
        {
            if (go != null && go.gameObject.name == "GameOverCanvas")
            {
                Object.Destroy(go.gameObject);
            }
        }
        instance.boardText = null;
    }

    void BuildProfileGate(Transform canvas)
    {
        profileGate = new GameObject("ProfileGate");
        profileGate.transform.SetParent(canvas, false);
        var bg = profileGate.AddComponent<Image>();
        bg.color = UiStyle.BgDeep;
        bg.raycastTarget = true;
        Stretch(profileGate.GetComponent<RectTransform>());

        MakeText(profileGate.transform, "黄道征途", 64, new Vector2(0, 300), UiStyle.Cyan);
        UiStyle.AddTitleUnderline(profileGate.transform, new Vector2(0, 258), 200f, UiStyle.Gold);
        MakeText(profileGate.transform, "设置飞行员 ID", 30, new Vector2(0, 210), UiStyle.TextPrimary);
        idHintText = MakeText(profileGate.transform, "2–6 个字符（中英文皆可）", 20, new Vector2(0, 165), UiStyle.TextSecondary);

        idInput = MakeInput(profileGate.transform, "输入 ID", new Vector2(0, 100), 520);
        // 不要用 characterLimit，中文输入法会出问题；在代码里校验长度
        idInput.characterLimit = 0;
        idInput.lineType = InputField.LineType.SingleLine;
        idInput.onEndEdit.AddListener(_ =>
        {
            // 失焦时先试一次校验，方便用户看到提示
            TryConfirmId(showSuccess: false);
        });

        idErrorText = MakeText(profileGate.transform, "", 22, new Vector2(0, 40), UiStyle.Red);

        hideToggle = MakeToggle(profileGate.transform, "在排行榜隐藏完整 ID", new Vector2(0, -20));
        hideToggle.isOn = PlayerProfile.HideId;

        // 一键：校验 ID 并进入模式选择
        var btnEnter = MakeBigButton(profileGate.transform, "确定并进入游戏", new Vector2(0, -140), () =>
        {
            OnClickEnterGame();
        }, UiStyle.Green);
        UiStyle.AddAccentEdge(btnEnter.GetComponent<Image>(), new Color(0.55f, 1f, 0.65f), 7f);

        // 随机合法 ID
        MakeBigButton(profileGate.transform, "随机一个 ID", new Vector2(0, -240), () =>
        {
            idInput.text = PlayerProfile.MakeRandomId();
            if (idErrorText != null)
            {
                idErrorText.color = UiStyle.Green;
                idErrorText.text = "已填入：" + idInput.text;
            }
        }, new Color(0.22f, 0.36f, 0.62f), 300f, 70f);

        MakeText(profileGate.transform, "输入后点下方绿色大按钮即可", 18,
            new Vector2(0, -320), UiStyle.TextMuted);
    }

    void OnClickEnterGame()
    {
        // 先让输入框失焦，避免中文输入法未提交
        if (idInput != null)
        {
            idInput.DeactivateInputField();
            if (UnityEngine.EventSystems.EventSystem.current != null)
            {
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            }
        }

        if (!TryConfirmId(showSuccess: true))
        {
            Debug.LogWarning("[Shell] ID 校验未通过: " + (idErrorText != null ? idErrorText.text : ""));
            return;
        }

        PlayerProfile.HideId = hideToggle != null && hideToggle.isOn;
        // 排行榜地址不再在主页展示，使用本地已存配置

        ShowProfileGate(false);
        ShowModeGate();
        ApplyIdToHud();
        Debug.Log("[Shell] 进入模式选择, ProfileReady=" + PlayerProfile.ProfileReady);
    }

    void BuildTopHud(Transform canvas)
    {
        hudRoot = MakePanel("TopHud", canvas);

        // 顶栏底
        var bar = new GameObject("Bar");
        bar.transform.SetParent(hudRoot.transform, false);
        var barImg = bar.AddComponent<Image>();
        barImg.color = new Color(0.04f, 0.06f, 0.12f, 0.62f);
        barImg.raycastTarget = false;
        var brt = bar.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0, 1);
        brt.anchorMax = new Vector2(1, 1);
        brt.pivot = new Vector2(0.5f, 1);
        brt.anchoredPosition = Vector2.zero;
        brt.sizeDelta = new Vector2(0, 72);

        // 顶栏底边高光
        var edge = new GameObject("Edge");
        edge.transform.SetParent(bar.transform, false);
        var eimg = edge.AddComponent<Image>();
        eimg.color = new Color(UiStyle.Cyan.r, UiStyle.Cyan.g, UiStyle.Cyan.b, 0.35f);
        eimg.raycastTarget = false;
        var ert = eimg.rectTransform;
        ert.anchorMin = new Vector2(0f, 0f);
        ert.anchorMax = new Vector2(1f, 0f);
        ert.pivot = new Vector2(0.5f, 0f);
        ert.anchoredPosition = Vector2.zero;
        ert.sizeDelta = new Vector2(0f, 2f);

        idLabel = MakeHudText(bar.transform, "ID", new Vector2(20, 0), TextAnchor.MiddleLeft, 22, UiStyle.Cyan);
        scoreLabel = MakeHudText(bar.transform, "分数 0", new Vector2(0, -8), TextAnchor.MiddleCenter, 26, UiStyle.TextPrimary);
        waveLabel = MakeHudText(bar.transform, "波次 1", new Vector2(0, 24), TextAnchor.MiddleCenter, 16, UiStyle.Gold);
        coinLabel = MakeHudText(bar.transform, "金币 0", new Vector2(-20, 0), TextAnchor.MiddleRight, 22, UiStyle.Gold);

        // 暂停固定左下角，绝不与右上数据板重叠
        MakeSmallBtn(hudRoot.transform, "暂停", new Vector2(20f, 24f), () =>
        {
            OpenPauseMenu();
        }, bottomLeft: true);
    }

    void BuildPauseMenu(Transform canvas)
    {
        pauseMenu = MakePanel("PauseMenu", canvas);
        var dim = pauseMenu.GetComponent<Image>();
        if (dim != null)
        {
            dim.color = new Color(0f, 0f, 0f, 0.72f);
            dim.raycastTarget = true;
        }

        // 中央卡片
        UiStyle.MakeCard(pauseMenu.transform, "PauseCard", new Vector2(0, -10), new Vector2(580f, 560f));

        MakeText(pauseMenu.transform, "暂停", 56, new Vector2(0, 190), UiStyle.Cyan);
        UiStyle.AddTitleUnderline(pauseMenu.transform, new Vector2(0, 150), 120f, UiStyle.Gold);
        string title = Achievements.CurrentTitle;
        string idLine = "ID  " + PlayerProfile.DisplayName(PlayerProfile.PlayerId);
        if (!string.IsNullOrEmpty(title)) idLine += "  ·  " + title;
        MakeText(pauseMenu.transform, idLine, 24,
            new Vector2(0, 110), UiStyle.TextSecondary);

        MakeBigButton(pauseMenu.transform, "继续游戏", new Vector2(0, 30), () =>
        {
            ClosePauseMenu();
        }, UiStyle.Green, 400f, 84f);

        MakeBigButton(pauseMenu.transform, "返回主菜单", new Vector2(0, -80), () =>
        {
            ClosePauseMenu();
            ZodiacGameFlow.ReturnToMainMenu();
        }, UiStyle.Blue, 400f, 84f);

        MakeBigButton(pauseMenu.transform, "修改 ID", new Vector2(0, -180), () =>
        {
            ClosePauseMenu();
            Time.timeScale = 0f;
            ShowProfileGate(true);
            if (idInput != null) idInput.text = PlayerProfile.PlayerId;
            if (hideToggle != null) hideToggle.isOn = PlayerProfile.HideId;
        }, new Color(0.28f, 0.30f, 0.42f), 400f, 76f);

        pauseMenu.SetActive(false);
    }

    public void OpenPauseMenu()
    {
        if (pauseMenu == null) return;
        // 对局中才暂停
        var gm = GameManager.Instance;
        if (gm != null && gm.isGameOver) return;
        if (profileGate != null && profileGate.activeSelf) return;
        if (modeGate != null && modeGate.activeSelf) return;

        pauseMenu.SetActive(true);
        if (gm != null) gm.isPaused = true;
        Time.timeScale = 0f;
    }

    public void ClosePauseMenu()
    {
        if (pauseMenu != null) pauseMenu.SetActive(false);
        // 若不在其它闸门，恢复运行
        bool gateOn = (profileGate != null && profileGate.activeSelf)
            || (modeGate != null && modeGate.activeSelf);
        if (!gateOn)
        {
            if (GameManager.Instance != null) GameManager.Instance.isPaused = false;
            Time.timeScale = 1f;
        }
    }

    void UpdatePauseHotkey()
    {
        if (StoryUI.IsPlaying) return;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pauseMenu != null && pauseMenu.activeSelf) ClosePauseMenu();
            else OpenPauseMenu();
        }
    }

    void BuildPauseHint(Transform canvas)
    {
        // 局内不再常驻操作提示文案
        pauseRoot = MakePanel("Tip", canvas);
        pauseRoot.SetActive(false);
    }

    void ShowProfileGate(bool show)
    {
        if (profileGate != null) profileGate.SetActive(show);
        if (show && idErrorText != null) idErrorText.text = "";
    }

    /// 校验并保存 ID；成功返回 true
    bool TryConfirmId(bool showSuccess = true)
    {
        if (idInput == null) return false;
        string raw = idInput.text;
        string err;
        if (!PlayerProfile.TryCompleteProfile(raw, out err))
        {
            if (idErrorText != null)
            {
                idErrorText.color = new Color(1f, 0.45f, 0.4f);
                idErrorText.text = err;
            }
            return false;
        }
        if (showSuccess && idErrorText != null)
        {
            idErrorText.color = new Color(0.45f, 0.95f, 0.55f);
            idErrorText.text = "ID 已确认：" + PlayerProfile.PlayerId;
        }
        idInput.text = PlayerProfile.PlayerId;
        ApplyIdToHud();
        return true;
    }

    void ApplyIdToHud()
    {
        if (idLabel == null) return;
        try
        {
            idLabel.text = "ID " + PlayerProfile.DisplayName(PlayerProfile.PlayerId);
        }
        catch
        {
            idLabel = null;
        }
    }

    void Update()
    {
        UpdatePauseHotkey();
        if (hudRoot == null) return;
        bool gateOn = (profileGate != null && profileGate.activeSelf) || (modeGate != null && modeGate.activeSelf);
        hudRoot.SetActive(!gateOn);

        if (!gateOn)
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                if (gm.score != lastScore && scoreLabel != null)
                {
                    lastScore = gm.score;
                    scoreLabel.text = "分数  " + lastScore;
                }
                int coins = ShipMeta.Coins;
                if (coins != lastCoins && coinLabel != null)
                {
                    lastCoins = coins;
                    coinLabel.text = "金币  " + coins;
                }
            }
            int w = AutoEnemyWaves.CurrentWave;
            if (w != lastWave && w > 0 && waveLabel != null)
            {
                lastWave = w;
                waveLabel.text = "波次  " + w;
            }
        }
    }

    /// 死亡结算：上传排行榜并弹结算板
    public static void ShowGameOverBoard()
    {
        if (instance == null)
        {
            Debug.LogError("[GameOver] MobileGameShell instance is null");
            return;
        }
        instance.InternalGameOver();
    }

    void InternalGameOver()
    {
        Debug.Log("[GameOver] building board, score="
            + (GameManager.Instance != null ? GameManager.Instance.score : -1));

        Time.timeScale = 0f;
        ClearGameOverBoards();
        AutoIntroOutro.SkipIntro();

        foreach (var b in Object.FindObjectsOfType<EnemyBullet>())
        {
            if (b != null) Object.Destroy(b.gameObject);
        }

        int score = GameManager.Instance != null ? GameManager.Instance.score : 0;
        int kills = GameManager.Instance != null ? GameManager.Instance.killCount : 0;

        if (!ZodiacLevels.IsCampaign)
        {
            try { LeaderboardClient.SubmitEndless(PlayerProfile.PlayerId, score, PlayerProfile.LeaderboardUrl); }
            catch (System.Exception e) { Debug.LogWarning(e.Message); }
        }

        // 独立最高层级画布，避免被其它 UI 盖住
        var canvasGo = new GameObject("GameOverCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        MobileTuning.ConfigureCanvas(scaler);
        canvasGo.AddComponent<GraphicRaycaster>();

        var root = new GameObject("GameOverBoard");
        root.transform.SetParent(canvasGo.transform, false);
        root.transform.SetAsLastSibling();
        var img = root.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.82f);
        img.raycastTarget = true;
        Stretch(root.GetComponent<RectTransform>());

        UiStyle.MakeCard(root.transform, "GoCard", new Vector2(0, 30), new Vector2(700f, 820f));

        MakeText(root.transform, "游戏结束", 60, new Vector2(0, 330), UiStyle.Red);
        UiStyle.AddTitleUnderline(root.transform, new Vector2(0, 288), 160f, UiStyle.Gold);

        string title = Achievements.CurrentTitle;
        string idLine = "ID  " + PlayerProfile.DisplayName(PlayerProfile.PlayerId);
        if (!string.IsNullOrEmpty(title)) idLine += "  ·  " + title;
        MakeText(root.transform, idLine, 28, new Vector2(0, 248), UiStyle.Cyan);
        MakeText(root.transform, "分数  " + score + "     击杀  " + kills, 38, new Vector2(0, 188), UiStyle.TextPrimary);

        int earned = Mathf.Max(0, ShipMeta.Coins - coinsAtRunStart);
        MakeText(root.transform, "本局金币  +" + earned + "（累计 " + ShipMeta.Coins + "）", 28,
            new Vector2(0, 132), UiStyle.Gold);

        string modeLabel = ZodiacLevels.IsCampaign
            ? ("关卡  " + ZodiacLevels.CurrentLevel + " · " + ZodiacLevels.Get(ZodiacLevels.CurrentLevel).title)
            : ("无尽波次  " + AutoEnemyWaves.CurrentWave);
        MakeText(root.transform, modeLabel, 26, new Vector2(0, 90), UiStyle.TextSecondary);

        boardText = MakeText(root.transform, "排行榜加载中…", 24, new Vector2(0, -10), UiStyle.TextSecondary);
        boardText.alignment = TextAnchor.UpperCenter;
        var btrt = boardText.rectTransform;
        btrt.sizeDelta = new Vector2(640f, 220f);
        try
        {
            LeaderboardClient.FetchTop(PlayerProfile.LeaderboardUrl, json =>
            {
                if (boardText == null) return;
                boardText.text = FormatBoard(json);
            });
        }
        catch (System.Exception e)
        {
            if (boardText != null) boardText.text = "暂无排行数据";
            Debug.LogWarning(e.Message);
        }

        MakeBigButton(root.transform, "再次挑战", new Vector2(0, -200), () =>
        {
            ClearGameOverBoards();
            Time.timeScale = 1f;
            AutoIntroOutro.SkipIntro();
            ZodiacGameFlow.BlockAutoRun = false;
            coinsAtRunStart = ShipMeta.Coins;
            if (GameManager.Instance != null) GameManager.Instance.RestartGame();
            else UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }, UiStyle.Green, 360f, 80f);

        MakeBigButton(root.transform, "返回主页面", new Vector2(0, -295), () =>
        {
            ClearGameOverBoards();
            ZodiacGameFlow.ReturnToMainMenu();
        }, UiStyle.Blue, 360f, 80f);

        MakeButton(root.transform, "修改ID", new Vector2(0, -375), () =>
        {
            ClearGameOverBoards();
            Time.timeScale = 1f;
            ShowProfileGate(true);
            if (idInput != null) idInput.text = PlayerProfile.PlayerId;
            if (hideToggle != null) hideToggle.isOn = PlayerProfile.HideId;
        }, new Color(0.28f, 0.30f, 0.42f));

        Debug.Log("[GameOver] board ready");
    }

    string FormatBoard(string json)
    {
        if (string.IsNullOrEmpty(json) || json == "[]") return "暂无排行数据\n（请检查排行榜地址）";
        var list = new List<string>();
        int idx = 0;
        int rank = 1;
        while (idx < json.Length && rank <= 12)
        {
            int n = json.IndexOf("\"name\"", idx, System.StringComparison.Ordinal);
            if (n < 0) break;
            int q1 = json.IndexOf('"', n + 6);
            int q2 = json.IndexOf('"', q1 + 1);
            int q3 = json.IndexOf('"', q2 + 1);
            if (q1 < 0 || q2 < 0 || q3 < 0) break;
            string rawName = json.Substring(q2 + 1, q3 - q2 - 1);

            // 优先用服务端 display（含称号；隐藏时客户端已不带称号）
            string show = rawName;
            int dPos = json.IndexOf("\"display\"", q3, System.StringComparison.Ordinal);
            if (dPos > 0 && dPos < q3 + 200)
            {
                int dq1 = json.IndexOf('"', dPos + 9);
                int dq2 = json.IndexOf('"', dq1 + 1);
                int dq3 = json.IndexOf('"', dq2 + 1);
                if (dq1 > 0 && dq2 > 0 && dq3 > 0)
                {
                    string disp = json.Substring(dq2 + 1, dq3 - dq2 - 1);
                    if (!string.IsNullOrEmpty(disp)) show = disp;
                }
            }

            int s = json.IndexOf("\"score\"", q3, System.StringComparison.Ordinal);
            if (s < 0) break;
            int colon = json.IndexOf(':', s);
            int comma = json.IndexOfAny(new[] { ',', '}' }, colon + 1);
            if (colon < 0 || comma < 0) break;
            string scoreStr = json.Substring(colon + 1, comma - colon - 1).Trim();

            // 自己这一行始终可见完整信息；隐藏只影响别人看到的
            if (rawName == PlayerProfile.PlayerId)
            {
                string title = Achievements.CurrentTitle;
                string me = PlayerProfile.PlayerId;
                if (string.IsNullOrEmpty(me)) me = "匿名";
                show = string.IsNullOrEmpty(title) ? me : (me + "「" + title + "」");
            }

            list.Add(rank + "  " + show + "　　" + scoreStr);
            rank++;
            idx = comma;
        }
        if (list.Count == 0) return "暂无排行数据";
        return string.Join("\n\n", list);
    }

    static string MaskAny(string id)
    {
        if (string.IsNullOrEmpty(id)) return "*";
        if (id.Length <= 2) return id[0] + "*";
        return id[0] + new string('*', Mathf.Max(1, id.Length - 2)) + id[id.Length - 1];
    }

    // ---- UI helpers ----
    static GameObject MakePanel(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        // 必须先加 UI 组件，才有 RectTransform
        var img = go.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);
        img.raycastTarget = false;
        Stretch(go.GetComponent<RectTransform>());
        return go;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
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
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        var rt = text.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(900, 80);
        return text;
    }

    static Text MakeHudText(Transform parent, string content, Vector2 pos, TextAnchor align, int size, Color color)
    {
        var go = new GameObject("HudTxt");
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<Text>();
        text.font = PixelUi.Font;
        text.fontSize = size;
        text.alignment = align;
        text.color = color;
        text.text = content;
        text.raycastTarget = false;
        var rt = text.rectTransform;
        if (align == TextAnchor.MiddleLeft)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
        }
        else if (align == TextAnchor.MiddleRight)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
        }
        else
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
        }
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(360, 40);
        return text;
    }

    static InputField MakeInput(Transform parent, string placeholder, Vector2 pos, float width)
    {
        var go = new GameObject("Input");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.12f, 0.14f, 0.22f, 0.98f);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(width, 56);

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var text = textGo.AddComponent<Text>();
        text.font = PixelUi.Font;
        text.fontSize = 26;
        text.color = Color.white;
        text.supportRichText = false;
        Stretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(12, 8);
        text.rectTransform.offsetMax = new Vector2(-12, -8);

        var phGo = new GameObject("Ph");
        phGo.transform.SetParent(go.transform, false);
        var ph = phGo.AddComponent<Text>();
        ph.font = PixelUi.Font;
        ph.fontSize = 22;
        ph.fontStyle = FontStyle.Italic;
        ph.color = new Color(1, 1, 1, 0.35f);
        ph.text = placeholder;
        Stretch(ph.rectTransform);
        ph.rectTransform.offsetMin = new Vector2(12, 8);
        ph.rectTransform.offsetMax = new Vector2(-12, -8);

        var input = go.AddComponent<InputField>();
        input.textComponent = text;
        input.placeholder = ph;
        return input;
    }

    static Toggle MakeToggle(Transform parent, string label, Vector2 pos)
    {
        var go = new GameObject("Toggle");
        go.transform.SetParent(parent, false);
        // 必须先有 UI 组件，GameObject 才会带 RectTransform
        var rootImg = go.AddComponent<Image>();
        rootImg.color = new Color(0, 0, 0, 0);
        rootImg.raycastTarget = true;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(420, 40);

        var bg = new GameObject("Bg");
        bg.transform.SetParent(go.transform, false);
        var bimg = bg.AddComponent<Image>();
        bimg.color = new Color(0.2f, 0.22f, 0.3f, 1f);
        var brt = bimg.rectTransform;
        brt.anchorMin = brt.anchorMax = new Vector2(0f, 0.5f);
        brt.pivot = new Vector2(0f, 0.5f);
        brt.anchoredPosition = new Vector2(0, 0);
        brt.sizeDelta = new Vector2(36, 36);

        var mark = new GameObject("Mark");
        mark.transform.SetParent(bg.transform, false);
        var mimg = mark.AddComponent<Image>();
        mimg.color = new Color(0.35f, 0.9f, 0.45f);
        Stretch(mimg.rectTransform);
        mimg.rectTransform.offsetMin = new Vector2(6, 6);
        mimg.rectTransform.offsetMax = new Vector2(-6, -6);

        var text = MakeText(go.transform, label, 22, new Vector2(140, 0), new Color(0.9f, 0.9f, 0.92f));
        text.alignment = TextAnchor.MiddleLeft;
        text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        text.rectTransform.pivot = new Vector2(0f, 0.5f);
        text.rectTransform.anchoredPosition = new Vector2(50, 0);
        text.rectTransform.sizeDelta = new Vector2(380, 36);

        var toggle = go.AddComponent<Toggle>();
        toggle.targetGraphic = bimg;
        toggle.graphic = mimg;
        return toggle;
    }

    static void MakeButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction action, Color color)
    {
        MakeBigButton(parent, label, pos, action, color, 220f, 60f);
    }

    static GameObject MakeBigButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction action, Color color, float w = 420f, float h = 84f)
    {
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.raycastTarget = true;
        var btn = go.AddComponent<Button>();
        UiStyle.StyleButton(img, btn, color);
        btn.onClick.AddListener(action);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = MobileTuning.Active
            ? new Vector2(Mathf.Max(w, 300f), Mathf.Max(h, 82f))
            : new Vector2(w, h);
        var text = MakeText(go.transform, label, MobileTuning.Active ? 30 : 28, Vector2.zero, Color.white);
        Stretch(text.rectTransform);
        return go;
    }

    static void MakeSmallBtn(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction action, bool bottomLeft = false)
    {
        var go = new GameObject("SBtn_" + label);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        var btn = go.AddComponent<Button>();
        UiStyle.StyleButton(img, btn, new Color(0.14f, 0.22f, 0.40f, 0.88f), withShine: false);
        btn.onClick.AddListener(action);
        var rt = go.GetComponent<RectTransform>();
        if (bottomLeft)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
        }
        else
        {
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
        }
        rt.anchoredPosition = pos;
        rt.sizeDelta = MobileTuning.Active ? new Vector2(156, 66) : new Vector2(110, 48);
        var text = MakeText(go.transform, label, MobileTuning.Active ? 24 : 20, Vector2.zero, Color.white);
        Stretch(text.rectTransform);
    }
}
