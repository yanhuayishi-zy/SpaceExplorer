using UnityEngine;
using UnityEngine.UI;

/// 屏幕上方 Boss 血条（十三宫 / 无尽 Boss）
public class BossBarUI : MonoBehaviour
{
    static BossBarUI instance;
    Image fill;
    Text nameText;
    Text hpText;
    GameObject root;
    Enemy target;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (instance != null) return;
        var go = new GameObject("BossBarUI");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<BossBarUI>();
    }

    void Awake()
    {
        instance = this;
        Build();
    }

    void Build()
    {
        var cgo = new GameObject("BossBarCanvas");
        cgo.transform.SetParent(transform, false);
        var canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 70;
        var scaler = cgo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1f;

        root = new GameObject("Root");
        root.transform.SetParent(cgo.transform, false);
        var rrt = root.AddComponent<RectTransform>();
        rrt.anchorMin = rrt.anchorMax = new Vector2(0.5f, 1f);
        rrt.pivot = new Vector2(0.5f, 1f);
        rrt.anchoredPosition = new Vector2(0, -92f);
        rrt.sizeDelta = new Vector2(720f, 56f);

        var bg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(root.transform, false);
        var bimg = bg.GetComponent<Image>();
        bimg.color = new Color(0.04f, 0.04f, 0.08f, 0.85f);
        bimg.raycastTarget = false;
        var brt = (RectTransform)bg.transform;
        brt.anchorMin = new Vector2(0f, 0f);
        brt.anchorMax = new Vector2(1f, 0f);
        brt.pivot = new Vector2(0.5f, 0f);
        brt.anchoredPosition = new Vector2(0, 0);
        brt.sizeDelta = new Vector2(0f, 22f);

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(bg.transform, false);
        fill = fillGo.AddComponent<Image>();
        fill.color = new Color(0.85f, 0.2f, 0.25f, 0.95f);
        fill.raycastTarget = false;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 1f;
        var frt = fill.rectTransform;
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = new Vector2(2f, 2f);
        frt.offsetMax = new Vector2(-2f, -2f);

        nameText = MakeText(root.transform, "", 24, new Vector2(-220, 28), TextAnchor.MiddleLeft, UiStyle.Gold);
        hpText = MakeText(root.transform, "", 20, new Vector2(220, 28), TextAnchor.MiddleRight, UiStyle.TextSecondary);

        root.SetActive(false);
    }

    static Text MakeText(Transform parent, string content, int size, Vector2 pos, TextAnchor align, Color color)
    {
        var go = new GameObject("Txt");
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<Text>();
        text.font = PixelUi.Font;
        text.fontSize = size;
        text.alignment = align;
        text.color = color;
        text.text = content;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        var rt = text.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(400f, size + 8);
        return text;
    }

    void Update()
    {
        if (root == null) return;
        var gm = GameManager.Instance;
        bool playing = gm == null || (!gm.isGameOver && !MobileGameShell.IsMenuOpen);

        if (target == null || target.Equals(null) || !ZodiacGameFlow.BossAlive || !playing)
        {
            // 扫描场上的高血 Boss
            target = null;
            if (ZodiacGameFlow.BossAlive && playing)
            {
                foreach (var e in Object.FindObjectsOfType<Enemy>())
                {
                    if (e == null) continue;
                    if (e.gameObject.name.Contains("Boss") || e.gameObject.GetComponent<ZodiacBossAI>() != null)
                    {
                        target = e;
                        break;
                    }
                }
            }
        }

        if (target == null || target.Equals(null))
        {
            target = null;
            if (root != null && !root.Equals(null) && root.activeSelf) root.SetActive(false);
            return;
        }

        if (root != null && !root.activeSelf) root.SetActive(true);

        EnemyHealthBar bar = null;
        int hp = 0;
        int max = 1;
        BossBreakPhase brk = null;
        string bossName = "";
        try
        {
            brk = target.GetComponent<BossBreakPhase>();
            hp = Mathf.Max(0, target.health);
            bar = target.GetComponent<EnemyHealthBar>();
            if (bar != null) max = Mathf.Max(1, bar.maxHealth);
            bossName = target.gameObject.name.Replace("_Boss", "");
        }
        catch (MissingReferenceException)
        {
            target = null;
            if (root != null && !root.Equals(null)) root.SetActive(false);
            return;
        }

        float ratio = max > 0 ? Mathf.Clamp01((float)hp / max) : 0f;
        if (fill != null && !fill.Equals(null))
        {
            fill.fillAmount = ratio;
            fill.color = brk != null && brk.IsVulnerable
                ? new Color(0.4f, 1f, 0.55f, 0.95f)
                : (ratio < 0.35f ? new Color(1f, 0.35f, 0.2f, 0.95f) : new Color(0.85f, 0.2f, 0.25f, 0.95f));
        }
        if (nameText != null && !nameText.Equals(null)) nameText.text = bossName;
        if (hpText != null && !hpText.Equals(null)) hpText.text = hp + " / " + max;
    }
}
