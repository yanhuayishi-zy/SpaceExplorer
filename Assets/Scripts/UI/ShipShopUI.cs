using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// 机库/商城：仅主菜单打开（游戏外选机）
public class ShipShopUI : MonoBehaviour
{
    static ShipShopUI instance;
    GameObject panel;
    Text coinText;
    Text tipText;
    Transform shipPage;
    Transform gearPage;
    Transform itemPage;
    readonly List<GameObject> shipCards = new List<GameObject>();
    readonly List<GameObject> gearCards = new List<GameObject>();
    readonly List<GameObject> itemCards = new List<GameObject>();
    int tab;
    bool built;
    bool openedFromMenu;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Object.FindObjectOfType<ShipShopUI>() != null) return;
        var go = new GameObject("ShipShopUI");
        Object.DontDestroyOnLoad(go);
        instance = go.AddComponent<ShipShopUI>();
    }

    void Start()
    {
        Build();
        ApplySelectedShip();
    }

    void ApplySelectedShip()
    {
        var player = GameObject.Find("Player");
        if (player != null) ShipMeta.ApplyToPlayer(player);
        // 换机后刷新圆形技能钮配色/字
        var mc = Object.FindObjectOfType<MobileControls>();
        if (mc != null) mc.RebuildSkillButtons();
    }

    void Update()
    {
        // 对局中禁止换机
        if (!Input.GetKeyDown(KeyCode.F2)) return;
        var gm = GameManager.Instance;
        bool inBattle = gm != null && !gm.isGameOver && !MobileGameShell.IsMenuOpen;
        if (inBattle) return;
        if (panel != null)
        {
            panel.SetActive(!panel.activeSelf);
            if (panel.activeSelf) Refresh();
            else if (openedFromMenu) CloseToMenu();
        }
    }

    public static void RefreshCoins()
    {
        if (instance == null) return;
        if (instance.coinText == null) return;
        instance.coinText.text = "金币  " + ShipMeta.Coins;
    }

    public static void Open()
    {
        if (instance == null || instance.panel == null) return;
        instance.panel.SetActive(true);
        instance.Refresh();
    }

    /// 从主菜单打开选机
    public static void OpenFromMenu()
    {
        if (instance == null || instance.panel == null) return;
        instance.openedFromMenu = true;
        Time.timeScale = 0f;
        MobileGameShell.HideModeGateTemporarily();
        instance.panel.SetActive(true);
        instance.Refresh();
    }

    public static void CloseToMenu()
    {
        if (instance == null || instance.panel == null) return;
        instance.panel.SetActive(false);
        instance.openedFromMenu = false;
        instance.ApplySelectedShip();
        MobileGameShell.ShowModeGateFromGame();
    }

    void Build()
    {
        if (built) return;
        built = true;

        var canvasGo = new GameObject("ShopCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 320;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1f; // 竖向适配，界面更满
        MobileTuning.ConfigureCanvas(scaler);
        canvasGo.AddComponent<GraphicRaycaster>();

        panel = new GameObject("ShopPanel");
        panel.transform.SetParent(canvasGo.transform, false);
        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.02f, 0.03f, 0.08f, 1f);
        bg.raycastTarget = true;
        Stretch(panel.GetComponent<RectTransform>());

        MakeText(panel.transform, "机库 · 战机与装备", 42, new Vector2(0, 430), UiStyle.Cyan);
        UiStyle.AddTitleUnderline(panel.transform, new Vector2(0, 392), 180f, UiStyle.Gold);
        coinText = MakeText(panel.transform, "金币  0", 28, new Vector2(0, 358), UiStyle.Gold);
        tipText = MakeText(panel.transform, "左右拖动查看更多 · 开局自动应用", 18, new Vector2(0, -490), UiStyle.TextSecondary);

        Transform shipContent, gearContent, itemContent;
        CreateScrollView("ShipPage", out shipPage, out shipContent);
        CreateScrollView("GearPage", out gearPage, out gearContent);
        CreateScrollView("ItemPage", out itemPage, out itemContent);

        MakeTabButton("战机", 0, new Vector2(-200, 300));
        MakeTabButton("装备", 1, new Vector2(0, 300));
        MakeTabButton("道具", 2, new Vector2(200, 300));

        var closeBtn = new GameObject("CloseBtn");
        closeBtn.transform.SetParent(panel.transform, false);
        var cimg = closeBtn.AddComponent<Image>();
        var cbtn = closeBtn.AddComponent<Button>();
        UiStyle.StyleButton(cimg, cbtn, new Color(0.22f, 0.26f, 0.36f));
        cbtn.onClick.AddListener(() => CloseToMenu());
        var crt = closeBtn.GetComponent<RectTransform>();
        crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.anchoredPosition = new Vector2(0, -455);
        crt.sizeDelta = new Vector2(260, 56);
        var clabel = MakeText(closeBtn.transform, "返回主菜单", 22, Vector2.zero, Color.white);
        Stretch(clabel.rectTransform);
        clabel.raycastTarget = false;

        // 卡片：缩小 + 拉大间距，避免挤在一起
        float x = 32f;
        float shipW = 270f, gearW = 240f, gap = 28f;
        for (int i = 0; i < ShipMeta.Ships.Length; i++)
        {
            shipCards.Add(CreateShipCard(ShipMeta.Ships[i], shipContent, x));
            x += shipW + gap;
        }
        SetContentWidth(shipContent, x + 32f);

        x = 32f;
        for (int i = 0; i < GearMeta.Gears.Length; i++)
        {
            gearCards.Add(CreateGearCard(GearMeta.Gears[i], gearContent, x));
            x += gearW + gap;
        }
        SetContentWidth(gearContent, x + 32f);

        x = 32f;
        for (int i = 0; i < GearMeta.Items.Length; i++)
        {
            itemCards.Add(CreateItemCard(GearMeta.Items[i], itemContent, x));
            x += gearW + gap;
        }
        SetContentWidth(itemContent, x + 32f);

        if (closeBtn != null) closeBtn.transform.SetAsLastSibling();
        foreach (var b in panel.GetComponentsInChildren<Button>(true))
        {
            if (b != null && b.gameObject.name.StartsWith("Tab_"))
                b.transform.SetAsLastSibling();
        }
        if (closeBtn != null) closeBtn.transform.SetAsLastSibling();

        ShowTab(0);
        panel.SetActive(false);
    }

    void CreateScrollView(string name, out Transform pageRoot, out Transform content)
    {
        var root = new GameObject(name);
        root.transform.SetParent(panel.transform, false);
        var rootImg = root.AddComponent<Image>();
        rootImg.color = new Color(0, 0, 0, 0);
        rootImg.raycastTarget = false;
        var rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0f, 0.5f);
        rootRt.anchorMax = new Vector2(1f, 0.5f);
        rootRt.pivot = new Vector2(0.5f, 0.5f);
        // 整体下移，避开页签，卡片区更矮
        rootRt.anchoredPosition = new Vector2(0, -55);
        rootRt.sizeDelta = new Vector2(-40f, 560f);

        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(root.transform, false);
        var vpImg = viewport.AddComponent<Image>();
        vpImg.color = new Color(0, 0, 0, 0);
        vpImg.raycastTarget = true;
        viewport.AddComponent<RectMask2D>();
        var vpRt = viewport.GetComponent<RectTransform>();
        Stretch(vpRt);

        var contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.transform.SetParent(viewport.transform, false);
        var contentRt = (RectTransform)contentGo.transform;
        contentRt.anchorMin = new Vector2(0f, 0.5f);
        contentRt.anchorMax = new Vector2(0f, 0.5f);
        contentRt.pivot = new Vector2(0f, 0.5f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(2000f, 860f);

        var scroll = root.AddComponent<ScrollRect>();
        scroll.viewport = vpRt;
        scroll.content = contentRt;
        scroll.horizontal = true;
        scroll.vertical = false;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.elasticity = 0.08f;
        scroll.scrollSensitivity = 30f;
        scroll.inertia = true;
        scroll.decelerationRate = 0.135f;

        pageRoot = root.transform;
        content = contentGo.transform;
    }

    static void SetContentWidth(Transform content, float width)
    {
        var rt = content as RectTransform;
        if (rt == null) return;
        rt.sizeDelta = new Vector2(Mathf.Max(width, 100f), rt.sizeDelta.y);
    }

    readonly List<Image> tabImages = new List<Image>();

    void MakeTabButton(string label, int index, Vector2 pos)
    {
        var go = new GameObject("Tab_" + label);
        go.transform.SetParent(panel.transform, false);
        var img = go.AddComponent<Image>();
        var btn = go.AddComponent<Button>();
        UiStyle.StyleButton(img, btn, new Color(0.14f, 0.18f, 0.28f), withShine: false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(180, 56);
        MakeText(go.transform, label, 28, Vector2.zero, UiStyle.TextPrimary);
        tabImages.Add(img);
        btn.onClick.AddListener(() => ShowTab(index));
    }

    void ShowTab(int index)
    {
        tab = index;
        if (shipPage != null) shipPage.gameObject.SetActive(index == 0);
        if (gearPage != null) gearPage.gameObject.SetActive(index == 1);
        if (itemPage != null) itemPage.gameObject.SetActive(index == 2);
        for (int i = 0; i < tabImages.Count; i++)
        {
            if (tabImages[i] == null) continue;
            bool on = i == index;
            tabImages[i].color = on
                ? new Color(0.18f, 0.42f, 0.68f)
                : new Color(0.12f, 0.15f, 0.22f);
        }
        Refresh();
    }

    GameObject CreateShipCard(ShipMeta.ShipDef def, Transform parent, float xPos)
    {
        var card = CreateCardRoot(parent, "Card_" + def.id, xPos, new Vector2(270, 540));
        var preview = MakeIcon(card.transform, def.tint, new Vector2(0, 170), new Vector2(130, 130));
        if (preview != null)
        {
            var godSpr = Resources.Load<Sprite>("GodShip_" + def.id);
            if (godSpr != null)
            {
                preview.sprite = godSpr;
                preview.color = Color.white;
            }
        }
        MakeText(card.transform, def.name, 30, new Vector2(0, 90), UiStyle.TextPrimary);
        MakeText(card.transform, def.title, 18, new Vector2(0, 58), UiStyle.TextSecondary);
        MakeText(card.transform, "「" + def.skillName + "」", 20, new Vector2(0, 22), UiStyle.Gold);
        MakeText(card.transform, "Q「" + def.subName + "」CD " + def.subCd.ToString("0.#") + "s", 16, new Vector2(0, -8), UiStyle.Cyan);
        MakeText(card.transform, $"HP {def.maxHp * PlayerHealth.HpScale}  伤害 {def.damage}\n射速 {(1f/def.fireRate):0.#}/s  移速 {def.moveSpeed:0.#}",
            16, new Vector2(0, -55), new Color(0.75f, 0.85f, 0.9f));
        var priceText = MakeText(card.transform, "", 20, new Vector2(0, -115), new Color(1f, 0.9f, 0.4f));
        var (label, bimg) = MakeActionButton(card.transform, new Vector2(0, -175), 180f, 52f);

        var holder = card.AddComponent<ShipCardRefs>();
        holder.def = def;
        holder.priceText = priceText;
        holder.buttonLabel = label;
        holder.buttonImage = bimg;
        holder.preview = preview;

        var btn = card.transform.Find("Action").GetComponent<Button>();
        btn.onClick.AddListener(() =>
        {
            if (ShipMeta.IsOwned(def.id))
            {
                ShipMeta.Select(def.id);
                ApplySelectedShip();
                SetTip("已选择：" + def.name + " · " + def.skillName);
            }
            else if (ShipMeta.Buy(def))
            {
                ApplySelectedShip();
                SetTip("购买成功：" + def.name);
            }
            else SetTip("金币不足，需要 " + def.price);
            Refresh();
        });
        return card;
    }

    GameObject CreateGearCard(GearMeta.GearDef def, Transform parent, float xPos)
    {
        var card = CreateCardRoot(parent, "Gear_" + def.id, xPos, new Vector2(240, 500));
        MakeIcon(card.transform, GearTint(def.id), new Vector2(0, 140), new Vector2(100, 100));
        MakeText(card.transform, def.name, 26, new Vector2(0, 55), Color.white);
        MakeText(card.transform, def.desc, 16, new Vector2(0, 20), new Color(0.82f, 0.87f, 0.92f));
        MakeText(card.transform, def.statHint, 15, new Vector2(0, -12), new Color(0.7f, 0.78f, 0.85f));
        var lvText = MakeText(card.transform, "Lv 0", 24, new Vector2(0, -70), new Color(0.6f, 1f, 0.8f));
        var priceText = MakeText(card.transform, "", 20, new Vector2(0, -115), new Color(1f, 0.9f, 0.4f));
        var (label, bimg) = MakeActionButton(card.transform, new Vector2(0, -165), 180f, 48f);

        var holder = card.AddComponent<GearCardRefs>();
        holder.def = def;
        holder.lvText = lvText;
        holder.priceText = priceText;
        holder.buttonLabel = label;
        holder.buttonImage = bimg;

        var btn = card.transform.Find("Action").GetComponent<Button>();
        btn.onClick.AddListener(() =>
        {
            if (GearMeta.Upgrade(def))
            {
                SetTip("升级成功：" + def.name + " → Lv" + GearMeta.GetLevel(def.id));
                ApplySelectedShip();
            }
            else
            {
                int lv = GearMeta.GetLevel(def.id);
                if (lv >= def.maxLevel) SetTip(def.name + " 已满级");
                else SetTip("金币不足，升级需要 " + GearMeta.UpgradeCost(def));
            }
            Refresh();
        });
        return card;
    }

    GameObject CreateItemCard(GearMeta.ItemDef def, Transform parent, float xPos)
    {
        var card = CreateCardRoot(parent, "Item_" + def.id, xPos, new Vector2(240, 500));
        MakeIcon(card.transform, ItemTint(def.id), new Vector2(0, 140), new Vector2(100, 100));
        MakeText(card.transform, def.name, 26, new Vector2(0, 55), Color.white);
        MakeText(card.transform, def.desc, 16, new Vector2(0, 20), new Color(0.82f, 0.87f, 0.92f));
        MakeText(card.transform, def.effectHint, 15, new Vector2(0, -12), new Color(0.7f, 0.78f, 0.85f));
        var priceText = MakeText(card.transform, "", 20, new Vector2(0, -100), new Color(1f, 0.9f, 0.4f));
        var (label, bimg) = MakeActionButton(card.transform, new Vector2(0, -155), 180f, 48f);

        var holder = card.AddComponent<ItemCardRefs>();
        holder.def = def;
        holder.priceText = priceText;
        holder.buttonLabel = label;
        holder.buttonImage = bimg;

        var btn = card.transform.Find("Action").GetComponent<Button>();
        btn.onClick.AddListener(() =>
        {
            if (GearMeta.OwnItem(def.id))
            {
                GearMeta.ToggleEquip(def.id);
                SetTip((GearMeta.IsEquipped(def.id) ? "已装配：" : "已卸下：") + def.name);
            }
            else
            {
                if (GearMeta.BuyItem(def))
                {
                    SetTip("购买并装配：" + def.name);
                }
                else SetTip("金币不足，需要 " + def.price);
            }
            Refresh();
        });
        return card;
    }

    static Color GearTint(string id)
    {
        switch (id)
        {
            case "engine": return new Color(0.4f, 0.85f, 1f);
            case "weapon": return new Color(1f, 0.45f, 0.4f);
            case "armor": return new Color(1f, 0.75f, 0.3f);
            case "core": return new Color(0.75f, 0.45f, 1f);
            case "mag": return new Color(0.5f, 1f, 0.55f);
            default: return Color.white;
        }
    }

    static Color ItemTint(string id)
    {
        switch (id)
        {
            case "skill_core": return new Color(1f, 0.55f, 0.2f);
            case "spread_chip": return new Color(0.95f, 0.9f, 0.35f);
            case "shield_cell": return new Color(0.4f, 0.75f, 1f);
            case "coin_vip": return new Color(1f, 0.85f, 0.25f);
            case "life_bead": return new Color(1f, 0.4f, 0.55f);
            default: return Color.white;
        }
    }

    GameObject CreateCardRoot(Transform parent, string name, float xPos, Vector2 size)
    {
        var card = new GameObject(name);
        card.transform.SetParent(parent, false);
        var img = card.AddComponent<Image>();
        img.color = UiStyle.Card;
        var rt = card.GetComponent<RectTransform>();
        // 左对齐，便于横向滚动
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(xPos, 0f);
        rt.sizeDelta = size;

        // 细边框
        var border = new GameObject("_border");
        border.transform.SetParent(card.transform, false);
        border.transform.SetAsFirstSibling();
        var bimg = border.AddComponent<Image>();
        bimg.color = UiStyle.BorderDim;
        bimg.raycastTarget = false;
        var brt = bimg.rectTransform;
        brt.anchorMin = Vector2.zero;
        brt.anchorMax = Vector2.one;
        brt.offsetMin = new Vector2(-2f, -2f);
        brt.offsetMax = new Vector2(2f, 2f);

        // 选中金边（Refresh 控制透明度）
        var sel = new GameObject("_sel");
        sel.transform.SetParent(card.transform, false);
        sel.transform.SetAsFirstSibling();
        var simg = sel.AddComponent<Image>();
        simg.color = new Color(UiStyle.Gold.r, UiStyle.Gold.g, UiStyle.Gold.b, 0f);
        simg.raycastTarget = false;
        var srt = simg.rectTransform;
        srt.anchorMin = Vector2.zero;
        srt.anchorMax = Vector2.one;
        srt.offsetMin = new Vector2(-3f, -3f);
        srt.offsetMax = new Vector2(3f, 3f);

        return card;
    }

    static void SetCardSelected(GameObject card, bool selected)
    {
        if (card == null) return;
        // 只描金边，不把整卡刷成金色
        var sel = card.transform.Find("_sel");
        if (sel == null) return;
        // 清掉可能的实心底
        var rootImg = sel.GetComponent<Image>();
        if (rootImg != null) rootImg.color = new Color(0f, 0f, 0f, 0f);
        // 边框子节点
        if (selected)
        {
            if (sel.childCount == 0) AddGoldEdges(sel, new Color(UiStyle.Gold.r, UiStyle.Gold.g, UiStyle.Gold.b, 0.95f), 3f);
        }
        else
        {
            for (int c = sel.childCount - 1; c >= 0; c--)
                Object.Destroy(sel.GetChild(c).gameObject);
        }
    }

    static void AddGoldEdges(Transform parent, Color color, float t)
    {
        void Edge(Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            var e = new GameObject("Edge", typeof(RectTransform), typeof(Image));
            e.transform.SetParent(parent, false);
            var img = e.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            var rt = (RectTransform)e.transform;
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = oMin;
            rt.offsetMax = oMax;
        }
        Edge(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -t), Vector2.zero);
        Edge(Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, t));
        Edge(Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(t, 0f));
        Edge(new Vector2(1f, 0f), Vector2.one, new Vector2(-t, 0f), Vector2.zero);
    }

    Image MakeIcon(Transform parent, Color tint, Vector2 pos, Vector2 size)
    {
        var go = new GameObject("Icon");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = Resources.Load<Sprite>("PlayerShip");
        if (img.sprite == null) img.sprite = Resources.Load<Sprite>("Star");
        img.color = tint;
        img.preserveAspect = true;
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return img;
    }

    (Text, Image) MakeActionButton(Transform parent, Vector2 pos, float w = 220f, float h = 60f)
    {
        var btnGo = new GameObject("Action");
        btnGo.transform.SetParent(parent, false);
        var bimg = btnGo.AddComponent<Image>();
        var btn = btnGo.AddComponent<Button>();
        UiStyle.StyleButton(bimg, btn, UiStyle.Blue);
        var brt = btnGo.GetComponent<RectTransform>();
        brt.anchoredPosition = pos;
        brt.sizeDelta = new Vector2(w, h);
        var label = MakeText(btnGo.transform, "选择", 26, Vector2.zero, Color.white);
        label.transform.SetAsLastSibling();
        return (label, bimg);
    }

    void Refresh()
    {
        coinText.text = "金币  " + ShipMeta.Coins;
        string cur = ShipMeta.Current.id;

        foreach (var go in shipCards)
        {
            var r = go.GetComponent<ShipCardRefs>();
            if (r == null || r.def == null) continue;
            var def = r.def;
            bool owned = ShipMeta.IsOwned(def.id);
            bool selected = cur == def.id;
            SetCardSelected(go, selected);
            r.priceText.text = owned ? "已拥有" : (def.price <= 0 ? "免费" : def.price + " 金币");
            r.buttonLabel.text = selected ? "使用中" : (owned ? "选择" : "购买");
            r.buttonImage.color = selected
                ? UiStyle.Green
                : (owned ? UiStyle.Blue : UiStyle.Orange);
            var godSpr = Resources.Load<Sprite>("GodShip_" + def.id);
            if (r.preview != null)
            {
                if (godSpr != null)
                {
                    r.preview.sprite = godSpr;
                    r.preview.color = selected ? Color.white : new Color(0.75f, 0.75f, 0.75f, 1f);
                }
                else
                {
                    r.preview.color = selected ? def.tint : def.tint * new Color(0.75f, 0.75f, 0.75f, 1f);
                }
            }
        }

        foreach (var go in gearCards)
        {
            var r = go.GetComponent<GearCardRefs>();
            if (r == null || r.def == null) continue;
            int lv = GearMeta.GetLevel(r.def.id);
            r.lvText.text = "Lv " + lv + " / " + r.def.maxLevel;
            if (lv >= r.def.maxLevel)
            {
                r.priceText.text = "已满级";
                r.buttonLabel.text = "MAX";
                r.buttonImage.color = new Color(0.30f, 0.32f, 0.38f);
            }
            else
            {
                int cost = GearMeta.UpgradeCost(r.def);
                r.priceText.text = cost + " 金币";
                r.buttonLabel.text = "升级";
                r.buttonImage.color = new Color(0.22f, 0.58f, 0.40f);
            }
        }

        foreach (var go in itemCards)
        {
            var r = go.GetComponent<ItemCardRefs>();
            if (r == null || r.def == null) continue;
            bool owned = GearMeta.OwnItem(r.def.id);
            bool eq = GearMeta.IsEquipped(r.def.id);
            r.priceText.text = owned ? "已拥有" : (r.def.price + " 金币");
            r.buttonLabel.text = owned ? (eq ? "卸下" : "装配") : "购买";
            r.buttonImage.color = owned
                ? (eq ? UiStyle.Green : new Color(0.28f, 0.38f, 0.62f))
                : UiStyle.Orange;
        }
    }

    void SetTip(string s)
    {
        tipText.text = s;
    }

    static Text MakeText(Transform parent, string content, int size, Vector2 pos, Color? color = null)
    {
        var go = new GameObject("Txt");
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<Text>();
        text.font = PixelUi.Font;
        text.fontSize = size;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color ?? Color.white;
        text.text = content;
        text.supportRichText = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        var rt = text.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(240, 90);
        return text;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    class ShipCardRefs : MonoBehaviour
    {
        public ShipMeta.ShipDef def;
        public Text priceText;
        public Text buttonLabel;
        public Image buttonImage;
        public Image preview;
    }

    class GearCardRefs : MonoBehaviour
    {
        public GearMeta.GearDef def;
        public Text lvText;
        public Text priceText;
        public Text buttonLabel;
        public Image buttonImage;
    }

    class ItemCardRefs : MonoBehaviour
    {
        public GearMeta.ItemDef def;
        public Text priceText;
        public Text buttonLabel;
        public Image buttonImage;
    }
}
