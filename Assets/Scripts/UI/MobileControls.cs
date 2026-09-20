using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// 移动端：自动射击；右下圆形技能钮（原神式径向充能/CD）
/// 编辑器按 M 可模拟触控 UI
public class MobileControls : MonoBehaviour
{
    public static bool IsMobile
    {
        get
        {
#if UNITY_EDITOR
            return forceMobile;
#else
            return Application.isMobilePlatform
                || SystemInfo.deviceType == DeviceType.Handheld
                || (Screen.width < 1200 && Input.touchSupported);
#endif
        }
    }

#if UNITY_EDITOR
    public static bool forceMobile = false;
#endif

    static bool specialRequested;
    static bool restartRequested;
    static bool subSkillRequested;

    public static bool ConsumeSpecial()
    {
        if (!specialRequested) return false;
        specialRequested = false;
        return true;
    }

    public static bool ConsumeSubSkill()
    {
        if (!subSkillRequested) return false;
        subSkillRequested = false;
        return true;
    }

    public static bool ConsumeRestart()
    {
        if (!restartRequested) return false;
        restartRequested = false;
        return true;
    }

    public static bool IsPointerOverUi(int fingerId = -1)
    {
        if (EventSystem.current == null) return false;
        if (fingerId >= 0)
            return EventSystem.current.IsPointerOverGameObject(fingerId);
        return EventSystem.current.IsPointerOverGameObject();
    }

    // 大招
    Image ultFill;
    Image ultReadyRing;
    Text ultName;
    Button ultBtn;
    // 子技能
    Image subFill;
    Image subReadyRing;
    Text subName;
    Button subBtn;

    PlayerShooting shooting;
    static Sprite circleSpr;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Object.FindObjectOfType<MobileControls>() != null) return;
        var go = new GameObject("MobileControls");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<MobileControls>();
    }

    public void RebuildSkillButtons()
    {
        DestroyTouchUi();
        BuildTouchUi();
    }

    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene s, UnityEngine.SceneManagement.LoadSceneMode m)
    {
        // 场景切换后确保圆钮还在
        if (GameObject.Find("MobileTouchCanvas") == null)
            BuildTouchUi();
        shooting = null;
    }

    void Start()
    {
        EnsureEventSystem();
        BuildTouchUi();
    }

    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.M))
        {
            forceMobile = !forceMobile;
            if (forceMobile) BuildTouchUi();
            else DestroyTouchUi();
            Debug.Log("移动端模拟: " + forceMobile);
        }
#endif
        // 保证右下圆钮始终存在（防止被误删）
        if (ultFill == null || ultFill.Equals(null))
        {
            if (GameObject.Find("MobileTouchCanvas") == null)
                BuildTouchUi();
        }
        RefreshSkillButtons();
    }

    void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    static Sprite CircleSprite()
    {
        if (circleSpr != null) return circleSpr;
        const int N = 128;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
        var px = new Color[N * N];
        float c = (N - 1) * 0.5f;
        float r = N * 0.5f - 1f;
        for (int y = 0; y < N; y++)
        for (int x = 0; x < N; x++)
        {
            float dx = x - c, dy = y - c;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            float a = Mathf.Clamp01(r - d); // 1px 抗锯齿
            px[y * N + x] = new Color(1f, 1f, 1f, a);
        }
        tex.SetPixels(px);
        tex.Apply();
        circleSpr = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
        return circleSpr;
    }

    /// 原神式技能钮：外圈底 + 专属图标 + 径向充能 + 就绪光环
    Image MakeSkillButton(Transform parent, Vector2 pos, float size,
        out Image fill, out Image readyRing, out Text label, out Button btn,
        Color baseColor, Color fillColor, Sprite icon, string initial)
    {
        var root = new GameObject("SkillBtn");
        root.transform.SetParent(parent, false);
        var rt = root.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(size, size);

        // 外圈描边感底
        var outer = new GameObject("Outer");
        outer.transform.SetParent(root.transform, false);
        var oImg = outer.AddComponent<Image>();
        oImg.sprite = CircleSprite();
        oImg.color = new Color(0.05f, 0.06f, 0.10f, 0.92f);
        oImg.raycastTarget = false;
        Stretch(oImg.rectTransform);
        oImg.rectTransform.offsetMin = new Vector2(-4f, -4f);
        oImg.rectTransform.offsetMax = new Vector2(4f, 4f);

        // 图标底
        var iconBg = new GameObject("IconBg");
        iconBg.transform.SetParent(root.transform, false);
        var iImg = iconBg.AddComponent<Image>();
        iImg.sprite = CircleSprite();
        iImg.color = new Color(baseColor.r * 0.35f + 0.04f, baseColor.g * 0.35f + 0.05f, baseColor.b * 0.35f + 0.10f, 0.95f);
        iImg.raycastTarget = false;
        Stretch(iImg.rectTransform);

        // 专属技能图标
        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(root.transform, false);
        var iconImg = iconGo.AddComponent<Image>();
        iconImg.sprite = icon != null ? icon : CircleSprite();
        iconImg.color = Color.white;
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;
        Stretch(iconImg.rectTransform);
        iconImg.rectTransform.offsetMin = new Vector2(size * 0.14f, size * 0.14f);
        iconImg.rectTransform.offsetMax = new Vector2(-size * 0.14f, -size * 0.14f);

        // 径向充能层（Clockwise，从顶开始）
        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(root.transform, false);
        fill = fillGo.AddComponent<Image>();
        fill.sprite = CircleSprite();
        fill.color = fillColor;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Radial360;
        fill.fillOrigin = (int)Image.Origin360.Top;
        fill.fillClockwise = true;
        fill.fillAmount = 0f;
        fill.raycastTarget = false;
        Stretch(fill.rectTransform);
        fill.rectTransform.offsetMin = new Vector2(4f, 4f);
        fill.rectTransform.offsetMax = new Vector2(-4f, -4f);

        // 就绪金环
        var ringGo = new GameObject("Ready");
        ringGo.transform.SetParent(root.transform, false);
        readyRing = ringGo.AddComponent<Image>();
        readyRing.sprite = CircleSprite();
        readyRing.color = new Color(1f, 0.9f, 0.35f, 0f);
        readyRing.raycastTarget = false;
        Stretch(readyRing.rectTransform);
        readyRing.rectTransform.offsetMin = new Vector2(-2f, -2f);
        readyRing.rectTransform.offsetMax = new Vector2(2f, 2f);

        // 文字（就绪/百分比/CD）
        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(root.transform, false);
        label = labelGo.AddComponent<Text>();
        label.font = PixelUi.Font;
        label.fontSize = size > 120f ? 22 : 16;
        label.alignment = TextAnchor.LowerCenter;
        label.color = Color.white;
        label.text = initial;
        label.raycastTarget = false;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        var lrt = label.rectTransform;
        lrt.anchorMin = new Vector2(0f, 0f);
        lrt.anchorMax = new Vector2(1f, 0.28f);
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;

        btn = root.AddComponent<Button>();
        btn.targetGraphic = iconImg;
        btn.transition = Selectable.Transition.ColorTint;
        var cols = btn.colors;
        cols.normalColor = Color.white;
        cols.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        cols.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        btn.colors = cols;

        return fill;
    }

    static Sprite LoadUltIcon(string shipId)
    {
        if (string.IsNullOrEmpty(shipId)) shipId = "mortal";
        return Resources.Load<Sprite>("SkillIcon_ult_" + shipId);
    }

    static Sprite LoadSubIcon(string shipId)
    {
        if (string.IsNullOrEmpty(shipId)) shipId = "mortal";
        var spr = Resources.Load<Sprite>("SkillIcon_sub_" + shipId);
        return spr != null ? spr : LoadUltIcon(shipId);
    }

    void BuildTouchUi()
    {
        if (GameObject.Find("MobileTouchCanvas") != null) return;

        var canvasGo = new GameObject("MobileTouchCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var cur = ShipMeta.Current;
        Color ultTint = SkillTint(cur != null ? cur.skill : ShipMeta.GodSkill.None);
        Color subTint = new Color(0.45f, 0.75f, 1f);
        string shipId = cur != null ? cur.id : "mortal";
        Sprite ultIcon = LoadUltIcon(shipId);
        Sprite subIcon = LoadSubIcon(shipId);

        Image f1, r1; Text t1; Button b1;
        MakeSkillButton(canvasGo.transform, new Vector2(-40f, 40f), 156f,
            out f1, out r1, out t1, out b1,
            ultTint, new Color(ultTint.r, ultTint.g, ultTint.b, 0.72f),
            ultIcon, "");
        ultFill = f1; ultReadyRing = r1; ultName = t1; ultBtn = b1;
        b1.onClick.AddListener(() => specialRequested = true);

        Image f2, r2; Text t2; Button b2;
        MakeSkillButton(canvasGo.transform, new Vector2(-40f, 220f), 118f,
            out f2, out r2, out t2, out b2,
            subTint, new Color(subTint.r, subTint.g, subTint.b, 0.65f),
            subIcon, "");
        subFill = f2; subReadyRing = r2; subName = t2; subBtn = b2;
        b2.onClick.AddListener(() => subSkillRequested = true);
    }

    static string SkillChar(ShipMeta.ShipDef def, bool ult)
    {
        if (def == null) return ult ? "大" : "Q";
        string n = ult ? def.skillName : def.subName;
        if (string.IsNullOrEmpty(n)) return ult ? "大" : "Q";
        return n.Substring(0, 1);
    }

    static Color SkillTint(ShipMeta.GodSkill s)
    {
        switch (s)
        {
            case ShipMeta.GodSkill.ZeusThunder: return new Color(1f, 0.95f, 0.4f);
            case ShipMeta.GodSkill.PoseidonTide: return new Color(0.35f, 0.8f, 1f);
            case ShipMeta.GodSkill.AresRage: return new Color(1f, 0.3f, 0.2f);
            case ShipMeta.GodSkill.AthenaAegis: return new Color(0.7f, 0.9f, 1f);
            case ShipMeta.GodSkill.ApolloSun: return new Color(1f, 0.75f, 0.2f);
            case ShipMeta.GodSkill.ArtemisVolley: return new Color(0.9f, 0.95f, 1f);
            case ShipMeta.GodSkill.HermesDash: return new Color(0.5f, 1f, 0.75f);
            case ShipMeta.GodSkill.HadesSoul: return new Color(0.75f, 0.4f, 1f);
            default: return Color.white;
        }
    }

    void DestroyTouchUi()
    {
        var go = GameObject.Find("MobileTouchCanvas");
        if (go != null) Object.Destroy(go);
        ultFill = null; ultReadyRing = null; ultName = null; ultBtn = null;
        subFill = null; subReadyRing = null; subName = null; subBtn = null;
    }

    void RefreshSkillButtons()
    {
        if (ultFill == null && subFill == null) return;
        // 场景切换后 UI 可能被销毁，先做 destroyed 检查
        if (ultFill != null && ultFill.Equals(null)) ultFill = null;
        if (ultName != null && ultName.Equals(null)) ultName = null;
        if (ultReadyRing != null && ultReadyRing.Equals(null)) ultReadyRing = null;
        if (subFill != null && subFill.Equals(null)) subFill = null;
        if (subName != null && subName.Equals(null)) subName = null;
        if (subReadyRing != null && subReadyRing.Equals(null)) subReadyRing = null;
        if (ultFill == null && subFill == null)
        {
            if (GameObject.Find("MobileTouchCanvas") == null)
                BuildTouchUi();
            return;
        }

        if (shooting == null || shooting.Equals(null))
        {
            var player = GameObject.Find("Player");
            if (player != null) shooting = player.GetComponent<PlayerShooting>();
        }
        if (shooting == null) return;

        try
        {
            float charge = shooting.Charge01;
            bool ultReady = shooting.IsSpecialReady();
            if (ultFill != null)
            {
                ultFill.fillAmount = charge;
                Color c = ultFill.color;
                if (ultReady)
                {
                    float pulse = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 8f);
                    ultFill.color = new Color(c.r, c.g, c.b, 0.55f + 0.45f * pulse);
                }
                else
                {
                    ultFill.color = new Color(c.r, c.g, c.b, 0.55f);
                }
            }
            if (ultReadyRing != null)
            {
                float a = ultReady ? 0.45f + 0.35f * Mathf.Sin(Time.unscaledTime * 8f) : 0f;
                ultReadyRing.color = new Color(1f, 0.9f, 0.35f, a);
            }
            if (ultName != null)
            {
                ultName.text = ultReady ? "就绪" : Mathf.RoundToInt(charge * 100f).ToString();
                ultName.color = ultReady ? new Color(1f, 0.95f, 0.5f) : Color.white;
                if (ultReady)
                {
                    float s = 1f + 0.06f * Mathf.Sin(Time.unscaledTime * 8f);
                    ultName.transform.localScale = Vector3.one * s;
                }
                else ultName.transform.localScale = Vector3.one;
            }

            float sub01 = shooting.Sub01;
            bool subReady = shooting.IsSubReady();
            if (subFill != null)
            {
                subFill.fillAmount = sub01;
                subFill.color = subReady
                    ? new Color(0.45f, 0.85f, 1f, 0.9f)
                    : new Color(0.35f, 0.55f, 0.8f, 0.55f);
            }
            if (subReadyRing != null)
            {
                float a = subReady ? 0.4f : 0f;
                subReadyRing.color = new Color(0.55f, 0.9f, 1f, a);
            }
            if (subName != null)
            {
                subName.text = subReady ? "就绪" : Mathf.CeilToInt(shooting.subTimer).ToString();
            }
        }
        catch (MissingReferenceException)
        {
            ultFill = null; ultName = null; ultReadyRing = null;
            subFill = null; subName = null; subReadyRing = null;
        }
    }

    public void RequestSpecial() => specialRequested = true;
    public void RequestRestart() => restartRequested = true;

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
