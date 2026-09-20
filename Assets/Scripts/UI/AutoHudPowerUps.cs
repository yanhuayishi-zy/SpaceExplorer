using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// 自动创建：分数/最高分/击杀/波次 计数器 + 道具掉落
public class AutoHudPowerUps : MonoBehaviour
{
    public static AutoHudPowerUps Instance { get; private set; }

    Text scoreText;
    Text highText;
    Text killText;
    Text waveText;
    Text powerText;
    int lastScore = -1;
    int lastKills = -1;
    int lastHigh = -1;
    int lastWave = -1;

    static Sprite healthSprite, coinSprite, damageSprite, shieldSprite, speedSprite, starSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Object.FindObjectOfType<AutoHudPowerUps>() != null)
        {
            Instance = Object.FindObjectOfType<AutoHudPowerUps>();
            return;
        }
        var go = new GameObject("AutoHudPowerUps");
        Object.DontDestroyOnLoad(go);
        Instance = go.AddComponent<AutoHudPowerUps>();
    }

    void Start()
    {
        Instance = this;
        if (GameManager.Instance == null)
        {
            var gm = new GameObject("GameManager");
            gm.AddComponent<GameManager>();
        }

        healthSprite = Resources.Load<Sprite>("HealthPack");
        starSprite = Resources.Load<Sprite>("Star");
        coinSprite = Resources.Load<Sprite>("Coin");
        damageSprite = Resources.Load<Sprite>("DamageBoost");
        shieldSprite = Resources.Load<Sprite>("Shield");
        speedSprite = Resources.Load<Sprite>("SpeedBoost");

        BuildHud();
        StartCoroutine(SpawnPowerUps());
    }

    void BuildHud()
    {
        var canvasGo = new GameObject("HudCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // 右上数据板：避免与顶栏叠字，本组放击杀+得分明细
        var board = new GameObject("StatsBg", typeof(RectTransform), typeof(Image));
        board.transform.SetParent(canvasGo.transform, false);
        var bimg = board.GetComponent<Image>();
        bimg.color = new Color(0.02f, 0.04f, 0.08f, 0.55f);
        bimg.raycastTarget = false;
        var brt = (RectTransform)board.transform;
        brt.anchorMin = brt.anchorMax = new Vector2(1f, 1f);
        brt.pivot = new Vector2(1f, 1f);
        brt.anchoredPosition = new Vector2(-12f, -12f);
        brt.sizeDelta = new Vector2(260f, 150f);

        scoreText = CreateText(board.transform, "ScoreText", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -12f), TextAnchor.UpperRight, 34);
        highText = CreateText(board.transform, "HighText", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -52f), TextAnchor.UpperRight, 24, new Color(0.95f, 0.9f, 0.45f));
        killText = CreateText(board.transform, "KillText", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -86f), TextAnchor.UpperRight, 24, new Color(0.7f, 0.9f, 1f));
        waveText = CreateText(canvasGo.transform, "WaveText", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), TextAnchor.UpperCenter, 28, new Color(0.95f, 0.9f, 0.7f));
        powerText = CreateText(canvasGo.transform, "PowerText", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 24f), TextAnchor.LowerLeft, 22, new Color(0.6f, 1f, 0.7f));
        // 金币与顶栏重复，这里不显示
        powerText.text = "";
    }

    Text CreateText(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 pos, TextAnchor align, int size, Color? color = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<Text>();
        text.font = PixelUi.Font;
        text.fontSize = size;
        text.alignment = align;
        text.color = color ?? Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.text = name;

        var rt = text.rectTransform;
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.pivot = (aMin == aMax && aMin.x > 0.5f) ? new Vector2(1f, 1f)
                 : (aMin == aMax && aMin.x < 0.5f && aMin.y < 0.5f) ? new Vector2(0f, 0f)
                 : (aMin.x == 0.5f) ? new Vector2(0.5f, 1f) : new Vector2(1f, 1f);
        if (aMin.y < 0.5f && aMin.x < 0.5f) rt.pivot = new Vector2(0f, 0f);
        if (aMin.x == 0.5f) rt.pivot = new Vector2(0.5f, 1f);
        if (aMin.x > 0.5f) rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(420f, 40f);
        return text;
    }

    void Update()
    {
        // UI 可能在场景切换后被销毁
        if (scoreText == null || scoreText.Equals(null)) return;

        var gm = GameManager.Instance;
        if (gm == null) return;

        if (gm.score != lastScore && scoreText != null)
        {
            lastScore = gm.score;
            scoreText.text = "得分 " + lastScore;
        }
        if (gm.highScore != lastHigh && highText != null)
        {
            lastHigh = gm.highScore;
            highText.text = "最高 " + lastHigh;
        }
        if (gm.killCount != lastKills && killText != null)
        {
            lastKills = gm.killCount;
            killText.text = "击杀 " + lastKills;
        }
        if (AutoEnemyWaves.CurrentWave != lastWave && waveText != null)
        {
            lastWave = AutoEnemyWaves.CurrentWave;
            if (lastWave > 0)
                waveText.text = "波次 " + lastWave;
        }
    }

    IEnumerator SpawnPowerUps()
    {
        yield return new WaitForSeconds(6f);
        while (true)
        {
            SpawnOne();
            float wait = Random.Range(7f, 12f);
            yield return new WaitForSeconds(wait);
        }
    }

    void SpawnOne()
    {
        var types = new[]
        {
            new { sprite = healthSprite, type = PowerUp.PowerUpType.Health, value = 1, color = Color.white, label = "回血" },
            new { sprite = coinSprite, type = PowerUp.PowerUpType.Score, value = 1, color = Color.white, label = "金币" },
            new { sprite = damageSprite, type = PowerUp.PowerUpType.Damage, value = 1, color = Color.white, label = "伤害+" },
            new { sprite = speedSprite, type = PowerUp.PowerUpType.Speed, value = 3, color = Color.white, label = "加速" },
            new { sprite = shieldSprite, type = PowerUp.PowerUpType.Shield, value = 1, color = Color.white, label = "护盾" },
            new { sprite = starSprite, type = PowerUp.PowerUpType.Spread, value = 1, color = Color.white, label = "散射" },
        };

        int idx = Random.Range(0, types.Length);
        var pick = types[idx];
        if (pick.sprite == null)
        {
            Debug.LogWarning("道具贴图缺失");
            return;
        }

        CreatePowerUp(pick.sprite, pick.type, pick.value, new Vector3(Random.Range(-4.2f, 4.2f), 6.2f, 0f), 0.55f, 1.6f, 6f);

        if (powerText != null)
        {
            powerText.text = "道具刷新: " + pick.label;
            CancelInvoke(nameof(ClearPowerText));
            Invoke(nameof(ClearPowerText), 2.5f);
        }
    }

    /// 击杀掉落：在敌人死亡点生成
    public void SpawnAt(Vector3 worldPos, bool premium)
    {
        Sprite spr;
        PowerUp.PowerUpType type;
        int value = 1;
        float dur = 6f;

        if (premium)
        {
            // Boss：优先保命/火力
            int p = Random.Range(0, 3);
            if (p == 0) { spr = healthSprite; type = PowerUp.PowerUpType.Health; value = 2; }
            else if (p == 1) { spr = shieldSprite; type = PowerUp.PowerUpType.Shield; dur = 5f; }
            else { spr = damageSprite; type = PowerUp.PowerUpType.Damage; value = 2; dur = 8f; }
        }
        else
        {
            int p = Random.Range(0, 5);
            if (p == 0) { spr = coinSprite; type = PowerUp.PowerUpType.Score; value = 1; }
            else if (p == 1) { spr = healthSprite; type = PowerUp.PowerUpType.Health; value = 1; }
            else if (p == 2) { spr = speedSprite; type = PowerUp.PowerUpType.Speed; value = 2; }
            else if (p == 3) { spr = starSprite; type = PowerUp.PowerUpType.Spread; }
            else { spr = coinSprite; type = PowerUp.PowerUpType.Score; value = 1; }
        }

        if (spr == null)
        {
            spr = starSprite != null ? starSprite : healthSprite;
            type = PowerUp.PowerUpType.Spread;
        }

        CreatePowerUp(spr, type, value, worldPos, premium ? 0.7f : 0.5f, premium ? 1.2f : 1.5f, dur);
        GameFx.Pickup(worldPos, new Color(1f, 0.9f, 0.4f));
    }

    void CreatePowerUp(Sprite sprite, PowerUp.PowerUpType type, int value, Vector3 pos, float scale, float speed, float duration)
    {
        var go = new GameObject("PowerUp_" + type);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * scale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 8;

        go.AddComponent<Rigidbody2D>().gravityScale = 0f;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.45f;

        var pu = go.AddComponent<PowerUp>();
        pu.powerUpType = type;
        pu.value = value;
        pu.moveSpeed = speed;
        pu.duration = duration;
    }

    void ClearPowerText()
    {
        if (powerText != null) powerText.text = "";
    }
}
