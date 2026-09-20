using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// 星座闯关 / 无尽 Boss 流程（接管 AutoEnemyWaves）
public class ZodiacGameFlow : MonoBehaviour
{
    public static ZodiacGameFlow Instance { get; private set; }
    public static bool BossAlive;
    public static int ClearedCampaignLevel; // 本局通关的关（0=未通）

    static Sprite smallSprite, mediumSprite, largeSprite;
    readonly List<GameObject> alive = new List<GameObject>();
    int endlessWave;
    int lastEndlessBossScore;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Object.FindObjectOfType<ZodiacGameFlow>() != null) return;
        var go = new GameObject("ZodiacGameFlow");
        Object.DontDestroyOnLoad(go);
        Instance = go.AddComponent<ZodiacGameFlow>();

        // 关掉旧刷怪
        var old = Object.FindObjectOfType<AutoEnemyWaves>();
        if (old != null) old.enabled = false;
    }

    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public static bool BlockAutoRun; // 主菜单时暂停自动开打

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        StopAllCoroutines();
        alive.Clear();
        BossAlive = false;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.isGameOver = false;
            GameManager.Instance.isPaused = false;
        }
        Time.timeScale = 1f;
        EnsurePlayer();
        if (!BlockAutoRun)
        {
            StartCoroutine(Run());
        }
    }
    public static void ReturnToMainMenu()
    {
        BlockAutoRun = true;
        ZodiacTheme.Reset();
        if (Instance != null)
        {
            Instance.StopAllCoroutines();
            foreach (var e in Object.FindObjectsOfType<Enemy>())
            {
                if (e != null) Object.Destroy(e.gameObject);
            }
            Instance.alive.Clear();
        }
        BossAlive = false;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.isGameOver = false;
        }
        Time.timeScale = 0f;
        MobileGameShell.ShowModeGateFromGame();
    }

    public static void ResumeFromMenu()
    {
        BlockAutoRun = false;
        if (Instance != null)
        {
            Instance.StopAllCoroutines();
            Instance.alive.Clear();
            Instance.EnsurePlayer();
            Instance.StartCoroutine(Instance.Run());
        }
        Time.timeScale = 1f;
    }

    void Start()
    {
        if (GameManager.Instance == null)
        {
            var gm = new GameObject("GameManager");
            gm.AddComponent<GameManager>();
        }

        EnsurePlayer();

        smallSprite = Resources.Load<Sprite>("EnemySmall");
        mediumSprite = Resources.Load<Sprite>("EnemyMedium");
        largeSprite = Resources.Load<Sprite>("EnemyLarge");

        ClearedCampaignLevel = 0;
        BossAlive = false;
        // 默认等模式选择后再开打
        BlockAutoRun = true;
    }

    /// 场景里没有 Player 就自动创建一架
    void EnsurePlayer()
    {
        var player = GameObject.Find("Player");
        if (player == null)
        {
            player = new GameObject("Player");
            player.transform.position = new Vector3(0f, -3.2f, 0f);
            player.transform.localScale = Vector3.one * 0.9f;

            var sr = player.AddComponent<SpriteRenderer>();
            sr.sprite = ShipMeta.LoadShipSprite(ShipMeta.Current.id);
            sr.sortingOrder = 10;

            var rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            if (player.GetComponent<PlayerMovement>() == null) player.AddComponent<PlayerMovement>();
            if (player.GetComponent<PlayerShooting>() == null) player.AddComponent<PlayerShooting>();
            if (player.GetComponent<PlayerHealth>() == null) player.AddComponent<PlayerHealth>();

            Debug.Log("[Zodiac] 已自动创建 Player");
        }

        if (player.tag != "Player") player.tag = "Player";

        var sr2 = player.GetComponent<SpriteRenderer>();
        if (sr2 != null)
        {
            var spr = ShipMeta.LoadShipSprite(ShipMeta.Current.id);
            if (spr != null) sr2.sprite = spr;
        }
        if (sr2 != null) sr2.sortingOrder = 10;

        var col = player.GetComponent<BoxCollider2D>();
        if (col == null) col = player.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        if (col.size.x < 0.2f || col.size.y < 0.2f) col.size = new Vector2(0.7f, 0.7f);

        if (player.GetComponent<PlayerMovement>() == null) player.AddComponent<PlayerMovement>();
        if (player.GetComponent<PlayerShooting>() == null) player.AddComponent<PlayerShooting>();
        if (player.GetComponent<PlayerHealth>() == null) player.AddComponent<PlayerHealth>();

        ShipMeta.ApplyToPlayer(player);
    }

    bool IsDead => GameManager.Instance != null && GameManager.Instance.isGameOver;

    IEnumerator Run()
    {
        // 等开场；用真实时间，避免 timeScale=0 卡死
        float guard = 0f;
        while (!AutoIntroOutro.IntroDone && guard < 20f)
        {
            guard += Time.unscaledDeltaTime;
            yield return null;
        }
        if (!AutoIntroOutro.IntroDone)
        {
            AutoIntroOutro.SkipIntro();
        }
        yield return new WaitForSecondsRealtime(0.2f);
        if (IsDead) yield break;

        if (ZodiacLevels.IsCampaign)
        {
            ZodiacTheme.Reset();
            yield return RunCampaign(ZodiacLevels.CurrentLevel);
        }
        else
        {
            ZodiacTheme.Reset();
            yield return StoryUI.Play(StoryData.KeyEndless, StoryData.EndlessIntro);
            yield return RunEndless();
        }
    }

    IEnumerator RunCampaign(int level)
    {
        var def = ZodiacLevels.Get(level);
        AutoEnemyWaves.CurrentWave = level;
        ApplyTheme(def);

        // 剧情：首次进关时播
        if (level == 1)
            yield return StoryUI.Play(StoryData.KeyPrologue, StoryData.Prologue);
        yield return StoryUI.Play(StoryData.KeyLevel(level), StoryData.LevelBeats(def));

        ShowBanner("战术简报", StoryData.TacticalHint(level));
        yield return new WaitForSeconds(1.6f);

        ShowBanner("第 " + level + " 关 · " + def.title, def.bossName);
        yield return new WaitForSeconds(1.8f);

        // 前期三波快速教学，中期四波，后期五波；降低重复感但保留难度爬升。
        int waveCount = level <= 4 ? 3 : (level <= 8 ? 4 : 5);
        for (int wave = 0; wave < waveCount; wave++)
        {
            if (wave > 0)
            {
                string waveName = wave == waveCount - 1 && level >= 5 ? def.title + " 精锐" : "第 " + (wave + 1) + " 波来袭";
                ShowBanner("增援", waveName);
            }
            int count = Mathf.Clamp(def.minionCount - 2 + wave, 4, 11);
            float interval = Mathf.Max(0.28f, 0.44f - wave * 0.035f);
            yield return SpawnMinionWave(def, count, interval);
            yield return WaitClear(maxWait: 24f + wave * 2f, minTime: 3.5f + wave * 0.4f);
            if (IsDead) yield break;
        }

        ShowBanner("BOSS", def.bossName + "\n" + def.lore);
        yield return new WaitForSeconds(1.6f);
        var boss = SpawnBoss(def);
        BossAlive = true;
        yield return WaitUntilDead(boss, 180f);
        BossAlive = false;
        if (IsDead) yield break;

        ClearedCampaignLevel = level;
        // 通关星级
        int scoreNow = GameManager.Instance != null ? GameManager.Instance.score : 0;
        float hpRatio = 0f;
        var php = GameObject.Find("Player");
        var phc = php != null ? php.GetComponent<PlayerHealth>() : null;
        if (phc != null && phc.maxHealth > 0)
            hpRatio = Mathf.Clamp01((float)phc.currentHealth / phc.maxHealth);
        int stars = ZodiacLevels.CalcStars(level, scoreNow, hpRatio);
        ZodiacLevels.SetStars(level, stars);
        int clearReward = ZodiacLevels.ClaimClearReward(level, stars);
        ZodiacLevels.ClearLevel(level);
        Achievements.NotifyCampaignClear(level);
        ShipShopUI.RefreshCoins();
        yield return StoryUI.Play(StoryData.KeyClear(level), StoryData.VictoryBeats(def));
        var midKey = StoryData.MilestoneKey(level);
        if (midKey != null)
            yield return StoryUI.Play(midKey, StoryData.Milestone(level));
        bool final = level >= ZodiacLevels.Count;
        if (final)
            yield return StoryUI.Play(StoryData.KeyEpilogue, StoryData.Epilogue);
        yield return ShowClear(def, final, stars, clearReward);
    }

    IEnumerator RunEndless()
    {
        endlessWave = 0;
        lastEndlessBossScore = 0;
        // 无尽专属背景：开局一次，之后不随波次/Boss 更换
        ZodiacTheme.ApplyEndlessBg();
        EndlessRoguelike.ResetRun();
        // 血量百分制 + 引擎强化移速
        {
            var p = GameObject.Find("Player");
            if (p != null)
            {
                // 先恢复完整机体与永久装备，再清理上一局的肉鸽临时层。
                ShipMeta.ApplyToPlayer(p);
                var hp = p.GetComponent<PlayerHealth>();
                if (hp != null)
                {
                    hp.ResetEndlessMods();
                }
                var ps = p.GetComponent<PlayerShooting>();
                if (ps != null)
                {
                    ps.ResetRoguelikeMods();
                    ps.ApplyRoguelikeMove();
                }
            }
        }
        if (AutoAudio.Instance != null) AutoAudio.PlayLevelBgm(0);

        while (!IsDead)
        {
            // 分数达阈值 → 三选一强化
            EndlessRoguelike.Tick();
            if (Time.timeScale < 0.5f)
            {
                // 强化面板打开中，等选完
                yield return null;
                continue;
            }

            endlessWave++;
            AutoEnemyWaves.CurrentWave = endlessWave;
            Achievements.NotifyEndlessWave(endlessWave);

            var eKey = StoryData.EndlessWaveKey(endlessWave);
            if (eKey != null)
            {
                var eBeats = StoryData.EndlessWaveStory(endlessWave);
                if (eBeats != null && eBeats.Length > 0)
                    yield return StoryUI.Play(eKey, eBeats);
            }
            var theme = ZodiacLevels.RandomMinionTheme(endlessWave);

            ShowBanner("无尽 · 波次 " + endlessWave, theme.title + " 使魔");
            yield return new WaitForSeconds(0.9f);

            // 肉鸽式成长：数量/血量/速度/开火率随波次抬升
            int count = Mathf.Clamp(4 + endlessWave / 2, 4, 12);
            float speed = Mathf.Min(1.3f + endlessWave * 0.07f, 3.6f);
            int hp = GameBalance.EndlessMinionHp(endlessWave);
            yield return SpawnMinionWave(theme, count, Mathf.Max(0.22f, 0.4f - endlessWave * 0.008f),
                hpOverride: hp, speedOverride: speed);
            yield return WaitClear(maxWait: Mathf.Min(12f + endlessWave * 0.6f, 26f), minTime: Mathf.Min(2.5f + endlessWave * 0.4f, 8f));
            if (IsDead) yield break;

            EndlessRoguelike.Tick();
            if (Time.timeScale < 0.5f)
            {
                yield return null;
                continue;
            }

            // 分数达阈值出 Boss
            int score = GameManager.Instance != null ? GameManager.Instance.score : 0;
            int threshold = lastEndlessBossScore + GameBalance.BossScoreGap;
            if (endlessWave >= 3 && score >= threshold)
            {
                var bossDef = ZodiacLevels.EndlessBossForScore(score);
                lastEndlessBossScore = score;
                if (AutoAudio.Instance != null) AutoAudio.PlayLevelBgm(bossDef.index);
                ShowBanner("星座降临", bossDef.bossName);
                yield return new WaitForSeconds(1.2f);
                var boss = SpawnBoss(bossDef, endlessScale: true);
                BossAlive = true;
                yield return WaitUntilDead(boss, 90f);
                BossAlive = false;
                if (IsDead) yield break;
                // 击败星座 Boss：立刻再送一次强化三选一
                ShowBanner("星座陨落", "获得强化补给");
                EndlessRoguelike.ForcePick();
                yield return new WaitForSeconds(0.3f);
            }
        }
    }

    void ApplyTheme(ZodiacLevels.LevelDef def)
    {
        // 背景叠色
        ZodiacTheme.ApplyBg(def);
        // BGM 变奏
        if (AutoAudio.Instance != null)
        {
            AutoAudio.PlayLevelBgm(def.index);
        }
    }

    void ShowBanner(string title, string sub)
    {
        if (UIManager.Instance != null) UIManager.Instance.ShowWaveInfo(title);
        var go = new GameObject("ZodiacBanner");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // 底衬，避免和星空糊在一起
        var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(go.transform, false);
        var cimg = card.GetComponent<Image>();
        cimg.color = new Color(0.02f, 0.04f, 0.1f, 0.55f);
        cimg.raycastTarget = false;
        var crt = (RectTransform)card.transform;
        crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.anchoredPosition = new Vector2(0, 80);
        crt.sizeDelta = new Vector2(900f, 140f);

        var t1 = MakeBannerText(card.transform, title, 52, new Vector2(0, 28), new Color(1f, 0.9f, 0.5f));
        var t2 = MakeBannerText(card.transform, sub, 26, new Vector2(0, -28), new Color(0.85f, 0.9f, 1f));
        Destroy(go, 2.4f);
    }

    IEnumerator ShowClear(ZodiacLevels.LevelDef def, bool final, int stars = 1, int clearReward = 0)
    {
        var root = new GameObject("LevelClear");
        var img = root.AddComponent<Canvas>();
        img.renderMode = RenderMode.ScreenSpaceOverlay;
        img.sortingOrder = 160;
        root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        root.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        root.AddComponent<GraphicRaycaster>();
        var bgGo = new GameObject("Dim");
        bgGo.transform.SetParent(root.transform, false);
        var bg = bgGo.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.72f);
        var brt = bg.rectTransform;
        brt.anchorMin = Vector2.zero;
        brt.anchorMax = Vector2.one;
        brt.offsetMin = Vector2.zero;
        brt.offsetMax = Vector2.zero;

        string title = final ? "十三宫制霸" : "关卡通过";
        string sub = final
            ? "你击败了时间泰坦克洛诺斯\n黄道十二宫与旧神纪元皆已终结"
            : def.title + " 已净化\n" + def.bossName + " 陨落";
        MakeBannerText(root.transform, title, 56, new Vector2(0, 200), final ? new Color(1f, 0.75f, 0.3f) : new Color(0.5f, 1f, 0.65f));

        // 星级
        string starStr = "";
        for (int s = 0; s < 3; s++) starStr += s < stars ? "★" : "☆";
        MakeBannerText(root.transform, starStr, 48, new Vector2(0, 120), new Color(1f, 0.9f, 0.35f));

        MakeBannerText(root.transform, sub, 26, new Vector2(0, 20), Color.white);

        int score = GameManager.Instance != null ? GameManager.Instance.score : 0;
        MakeBannerText(root.transform, "本局分数  " + score + "　星级  " + stars + "/3　通关奖励  +" + clearReward, 26, new Vector2(0, -60), new Color(0.9f, 0.9f, 0.7f));

        string nextHint = final ? "可在主菜单进入无尽模式继续挑战" : ("下一关解锁：" + ZodiacLevels.Get(def.index + 1).title);
        if (!final)
        {
            MakeBannerText(root.transform, nextHint, 22, new Vector2(0, -120), new Color(0.75f, 0.85f, 1f));
        }

        // 按钮同一水平线
        MakeBtn(root.transform, final ? "无尽模式" : "下一关", new Vector2(-160, -240), () =>
        {
            if (final)
            {
                ZodiacLevels.IsCampaign = false;
            }
            else
            {
                ZodiacLevels.CurrentLevel = def.index + 1;
            }
            if (GameManager.Instance != null) GameManager.Instance.RestartGame();
            else UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        });
        MakeBtn(root.transform, "主菜单", new Vector2(160, -240), () =>
        {
            if (root != null) Object.Destroy(root.gameObject);
            ReturnToMainMenu();
        });

        Time.timeScale = 0f;
        // 等待按钮；不要自动销毁
        yield break;
    }

    static readonly System.Collections.Generic.Dictionary<string, Sprite> zodiacSprites =
        new System.Collections.Generic.Dictionary<string, Sprite>();

    static Sprite LoadZodiacSprite(string key, bool boss)
    {
        string id = (boss ? "ZodiacBoss_" : "ZodiacMinion_") + key;
        if (zodiacSprites.TryGetValue(id, out var cached) && cached != null) return cached;
        var spr = Resources.Load<Sprite>(id);
        if (spr == null && boss) spr = Resources.Load<Sprite>("EnemyLarge");
        if (spr == null) spr = Resources.Load<Sprite>("EnemySmall");
        if (spr != null) zodiacSprites[id] = spr;
        return spr;
    }

    IEnumerator SpawnMinionWave(ZodiacLevels.LevelDef def, int count, float interval, int hpOverride = -1, float speedOverride = -1f)
    {
        int hp = hpOverride > 0 ? hpOverride : def.minionHp;
        float speed = speedOverride > 0f ? speedOverride : def.minionSpeed;
        var minionSpr = LoadZodiacSprite(def.key, false);
        for (int i = 0; i < count; i++)
        {
            if (IsDead) yield break;
            bool mid = i > 0 && i % 4 == 3;
            Sprite spr = minionSpr != null ? minionSpr : (mid ? mediumSprite : smallSprite);
            float scale = mid ? 1.05f : 0.78f;
            int useHp = mid ? hp + 2 : hp;
            SpawnMinion(def, spr, useHp, speed * (mid ? 0.75f : 1f), scale, mid ? GameBalance.ScoreMid : GameBalance.ScoreSmall,
                mid || i % 3 == 2);
            if (interval > 0f) yield return new WaitForSeconds(interval);
        }
    }

    void SpawnMinion(ZodiacLevels.LevelDef def, Sprite sprite, int hp, float speed, float scale, int score, bool canShoot)
    {
        var go = new GameObject(def.key + "_Minion");
        go.tag = "Enemy";

        // 多路出怪：左/中/右 + 左右上角 + 侧翼
        int lane = Random.Range(0, 7);
        float x, y;
        switch (lane)
        {
            case 0: // 左路
                x = Random.Range(-5.0f, -3.0f); y = Random.Range(5.8f, 7.2f); break;
            case 1: // 中路
                x = Random.Range(-1.2f, 1.2f); y = Random.Range(5.8f, 7.2f); break;
            case 2: // 右路
                x = Random.Range(3.0f, 5.0f); y = Random.Range(5.8f, 7.2f); break;
            case 3: // 左上角
                x = Random.Range(-5.2f, -4.0f); y = Random.Range(4.8f, 6.0f); break;
            case 4: // 右上角
                x = Random.Range(4.0f, 5.2f); y = Random.Range(4.8f, 6.0f); break;
            case 5: // 左侧翼（略低）
                x = -5.0f; y = Random.Range(3.5f, 5.0f); break;
            default: // 右侧翼
                x = 5.0f; y = Random.Range(3.5f, 5.0f); break;
        }
        go.transform.position = new Vector3(x, y, 0f);
        go.transform.localScale = Vector3.one * scale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = Color.white;
        sr.sortingOrder = 5;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.8f, 0.8f);

        var enemy = go.AddComponent<Enemy>();
        enemy.health = hp;
        // 百分制血量（100+）：怪伤按难度抬升，满血约可抗 5–6 下
        if (!ZodiacLevels.IsCampaign)
        {
            enemy.damage = GameBalance.EndlessMinionDamage(AutoEnemyWaves.CurrentWave);
        }
        else
        {
            enemy.damage = GameBalance.CampaignMinionDamage(def != null ? def.index : 1);
        }
        enemy.scoreValue = score;
        enemy.moveSpeed = speed * Random.Range(0.9f, 1.15f);
        enemy.canShoot = canShoot;

        // 侧翼更偏锯齿/正弦，顶部偏直线
        if (lane >= 5)
        {
            enemy.movementPattern = Random.value > 0.5f
                ? Enemy.MovementPattern.Zigzag
                : Enemy.MovementPattern.Sine;
            enemy.amplitude = 2.8f;
        }
        else
        {
            int pat = Random.Range(0, 3);
            enemy.movementPattern = pat == 0 ? Enemy.MovementPattern.Straight
                                : pat == 1 ? Enemy.MovementPattern.Sine
                                : Enemy.MovementPattern.Zigzag;
            enemy.amplitude = lane == 1 ? 1.6f : 2.2f;
        }
        enemy.frequency = Random.Range(1.5f, 2.4f);

        if (canShoot)
        {
            var bullet = GetEnemyBullet();
            enemy.bulletPrefab = bullet;
            var fp = new GameObject("FirePoint");
            fp.transform.SetParent(go.transform, false);
            fp.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            enemy.firePoint = fp.transform;
            enemy.fireRate = Mathf.Max(1.8f, Random.Range(2.2f, 3.4f) - def.index * 0.08f);
        }

        var bar = go.AddComponent<EnemyHealthBar>();
        bar.Setup(hp);
        alive.Add(go);
    }

    GameObject SpawnBoss(ZodiacLevels.LevelDef def, bool endlessScale = false)
    {
        float hpMul = endlessScale ? 1.25f + AutoEnemyWaves.CurrentWave * 0.12f : 1f;
        int hp = Mathf.RoundToInt(def.bossHp * hpMul);

        var go = new GameObject(def.key + "_Boss");
        go.tag = "Enemy";
        go.transform.position = new Vector3(0f, 6.8f, 0f);
        go.transform.localScale = Vector3.one * def.bossScale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadZodiacSprite(def.key, true);
        sr.color = Color.white;
        sr.sortingOrder = 6;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.2f, 1.1f);

        var enemy = go.AddComponent<Enemy>();
        enemy.health = hp;
        if (!ZodiacLevels.IsCampaign)
        {
            enemy.damage = GameBalance.EndlessBossDamage(AutoEnemyWaves.CurrentWave);
        }
        else
        {
            enemy.damage = GameBalance.CampaignBossDamage(def != null ? def.index : 1);
        }
        enemy.scoreValue = def.bossScore;
        enemy.moveSpeed = def.bossSpeed;
        // 默认射击关掉，由专属 AI 出招
        enemy.canShoot = false;
        enemy.movementPattern = Enemy.MovementPattern.Straight;
        enemy.amplitude = 2.2f;
        enemy.frequency = 1.4f;

        var bullet = GetEnemyBullet();
        enemy.bulletPrefab = bullet;
        var fp = new GameObject("FirePoint");
        fp.transform.SetParent(go.transform, false);
        fp.transform.localPosition = new Vector3(0f, -0.7f, 0f);
        enemy.firePoint = fp.transform;
        enemy.fireRate = 1.5f;

        var bar = go.AddComponent<EnemyHealthBar>();
        bar.Setup(hp);
        bar.offset = new Vector2(0f, 1.15f);
        bar.size = new Vector2(1.5f, 0.18f);

        // 星座专属攻击
        var ai = go.AddComponent<ZodiacBossAI>();
        ai.Setup(def.key, bullet, enemy);

        // 45% 生命锁血 + 破防条
        var brk = go.AddComponent<BossBreakPhase>();
        brk.Setup(enemy, ai);

        alive.Add(go);
        return go;
    }

    static GameObject bulletTemplate;

    GameObject GetEnemyBullet()
    {
        if (bulletTemplate != null) return bulletTemplate;
        bulletTemplate = new GameObject("ZodiacEnemyBullet");
        bulletTemplate.SetActive(false);
        Object.DontDestroyOnLoad(bulletTemplate);
        var sr = bulletTemplate.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("Bullet");
        sr.color = new Color(1f, 0.4f, 0.45f, 1f);
        bulletTemplate.transform.localScale = Vector3.one * 0.65f;
        var rb = bulletTemplate.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        var col = bulletTemplate.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.5f, 0.5f);
        bulletTemplate.AddComponent<EnemyBullet>();
        return bulletTemplate;
    }

    IEnumerator WaitClear(float maxWait, float minTime = 0f)
    {
        float t = 0f;
        while (t < maxWait)
        {
            if (IsDead) yield break;
            alive.RemoveAll(x => x == null);
            bool cleared = alive.Count == 0 && GameObject.FindGameObjectsWithTag("Enemy").Length == 0;
            // 最短时长未到就继续等，避免清得太快 Boss 过早出现
            if (cleared && t >= minTime) yield break;
            t += Time.deltaTime;
            yield return null;
        }

        // 超时代表残敌已经脱离有效战斗节奏；清场后再进入下一波，避免敌群无限叠加。
        foreach (var enemy in Object.FindObjectsOfType<Enemy>())
        {
            if (enemy != null) Object.Destroy(enemy.gameObject);
        }
        alive.Clear();
    }

    IEnumerator WaitUntilDead(GameObject boss, float timeout)
    {
        float t = 0f;
        float nextWarning = Mathf.Max(30f, timeout);
        while (boss != null)
        {
            if (IsDead) yield break;
            t += Time.deltaTime;
            if (t >= nextWarning)
            {
                ShowBanner("持久战", "Boss 尚未击败，战斗继续");
                nextWarning += Mathf.Max(30f, timeout);
            }
            yield return null;
        }
    }

    static Text MakeBannerText(Transform parent, string content, int size, Vector2 pos, Color color)
    {
        var go = new GameObject("BT");
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
        rt.sizeDelta = new Vector2(1000, 90);
        return text;
    }

    static void MakeBtn(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject("Btn");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.55f, 0.9f, 0.95f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(action);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(200, 56);
        MakeBannerText(go.transform, label, 24, Vector2.zero, Color.white);
    }
}

/// 背景主题着色
public static class ZodiacTheme
{
    static SpriteRenderer tintLayer;
    static int lastLevel = -1;
    static bool endlessBgApplied;

    public static void Reset()
    {
        lastLevel = -1;
        endlessBgApplied = false;
    }

    /// 无尽模式专属背景，只应用一次
    public static void ApplyEndlessBg()
    {
        EnsureLayer();
        HideScrollingBg();
        var spr = Resources.Load<Sprite>("BG_endless");
        if (spr != null)
        {
            FitSprite(spr);
            tintLayer.color = Color.white;
        }
        else
        {
            // 回退：深空纯色
            EnsureWhitePx();
            tintLayer.sprite = whitePx;
            tintLayer.transform.localScale = new Vector3(40f, 40f, 1f);
            tintLayer.color = new Color(0.03f, 0.04f, 0.10f, 1f);
        }
        endlessBgApplied = true;
        lastLevel = 0;
    }

    public static bool EndlessBgApplied => endlessBgApplied;

    public static void ApplyBg(ZodiacLevels.LevelDef def)
    {
        if (def == null) return;
        // 无尽专属背景已锁定时，不再随关卡/波次更换
        if (endlessBgApplied && !ZodiacLevels.IsCampaign) return;

        EnsureLayer();
        // 优先加载关卡专属背景图，并盖住旧滚动星空
        Sprite bgSprite = Resources.Load<Sprite>("BG_" + def.key);
        HideScrollingBg();
        if (bgSprite != null)
        {
            FitSprite(bgSprite);
            tintLayer.color = Color.white;
        }
        else
        {
            // 回退：纯色叠层
            EnsureWhitePx();
            if (tintLayer.sprite == null) tintLayer.sprite = whitePx;
            tintLayer.transform.localScale = new Vector3(40f, 40f, 1f);
            tintLayer.color = new Color(def.bgColor.r, def.bgColor.g, def.bgColor.b, 0.85f);
        }
        lastLevel = def.index;
    }

    static void EnsureLayer()
    {
        if (tintLayer != null) return;
        var go = new GameObject("ZodiacBgTint");
        Object.DontDestroyOnLoad(go);
        go.transform.position = new Vector3(0f, 0f, 12f);
        tintLayer = go.AddComponent<SpriteRenderer>();
        tintLayer.sortingOrder = -50;
        tintLayer.color = Color.white;
    }

    static Sprite whitePx;
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

    static void FitSprite(Sprite bgSprite)
    {
        tintLayer.sprite = bgSprite;
        float ppu = bgSprite.pixelsPerUnit;
        if (ppu <= 0f) ppu = 100f;
        float worldH = bgSprite.rect.height / ppu;
        float worldW = bgSprite.rect.width / ppu;

        var cam = Camera.main;
        float camH = cam != null && cam.orthographic ? cam.orthographicSize * 2f : 10f;
        float camW = camH * (cam != null ? cam.aspect : 16f / 9f);
        float scale = Mathf.Max((camH * 1.08f) / worldH, (camW * 1.08f) / worldW);
        tintLayer.transform.localScale = new Vector3(scale, scale, 1f);
        tintLayer.transform.position = new Vector3(0f, 0f, 12f);
    }

    static void HideScrollingBg()
    {
        var scroll = Object.FindObjectOfType<AutoScrollingBackground>();
        if (scroll != null)
        {
            scroll.enabled = false;
            foreach (var sr in scroll.GetComponentsInChildren<SpriteRenderer>(true))
            {
                sr.enabled = false;
            }
        }
        foreach (var sr in Object.FindObjectsOfType<SpriteRenderer>())
        {
            if (sr == null || sr.gameObject.name == null) continue;
            var n = sr.gameObject.name.ToLowerInvariant();
            if (n.Contains("spacebackground") || n.Contains("bg_a") || n.Contains("bg_b"))
            {
                sr.enabled = false;
            }
        }
    }
}
