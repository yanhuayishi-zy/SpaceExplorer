using UnityEngine;
using UnityEngine.UI;

/// 界面统一色板与控件样式（运行时 UI 共用）
public static class UiStyle
{
    // 深空底
    public static readonly Color BgDeep = new Color(0.02f, 0.04f, 0.10f, 0.96f);
    public static readonly Color Panel = new Color(0.05f, 0.08f, 0.15f, 0.92f);
    public static readonly Color Card = new Color(0.09f, 0.13f, 0.22f, 0.96f);
    public static readonly Color CardLocked = new Color(0.07f, 0.08f, 0.11f, 0.85f);
    public static readonly Color Border = new Color(0.28f, 0.40f, 0.58f, 0.85f);
    public static readonly Color BorderDim = new Color(0.18f, 0.20f, 0.28f, 0.8f);

    public static readonly Color TextPrimary = new Color(0.92f, 0.95f, 1f);
    public static readonly Color TextSecondary = new Color(0.65f, 0.72f, 0.82f);
    public static readonly Color TextMuted = new Color(0.45f, 0.50f, 0.58f);
    public static readonly Color Gold = new Color(1f, 0.85f, 0.32f);
    public static readonly Color Cyan = new Color(0.55f, 0.88f, 1f);
    public static readonly Color Green = new Color(0.35f, 0.85f, 0.50f);
    public static readonly Color Red = new Color(1f, 0.42f, 0.40f);
    public static readonly Color Blue = new Color(0.30f, 0.55f, 0.95f);
    public static readonly Color Orange = new Color(0.95f, 0.48f, 0.22f);

    /// 给按钮加「底衬 + 顶高光条」，扁平也能读出厚度
    public static void StyleButton(Image img, Button btn, Color baseColor, bool withShine = true)
    {
        if (img == null || btn == null) return;
        img.color = baseColor;
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;

        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.5f, 0.5f, 0.55f, 0.55f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        btn.colors = colors;

        // 去掉旧的装饰子节点（幂等）
        var tr = img.transform;
        for (int i = tr.childCount - 1; i >= 0; i--)
        {
            var ch = tr.GetChild(i);
            if (ch != null && (ch.name == "_shine" || ch.name == "_shadow" || ch.name == "_bar"))
                Object.Destroy(ch.gameObject);
        }

        var shadow = new GameObject("_shadow");
        shadow.transform.SetParent(tr, false);
        shadow.transform.SetAsFirstSibling();
        var simg = shadow.AddComponent<Image>();
        simg.color = new Color(0f, 0f, 0f, 0.35f);
        simg.raycastTarget = false;
        Stretch(simg.rectTransform);
        simg.rectTransform.offsetMin = new Vector2(2f, -4f);
        simg.rectTransform.offsetMax = new Vector2(-2f, -2f);

        if (withShine)
        {
            var shine = new GameObject("_shine");
            shine.transform.SetParent(tr, false);
            var shim = shine.AddComponent<Image>();
            shim.color = new Color(1f, 1f, 1f, 0.12f);
            shim.raycastTarget = false;
            var srt = shim.rectTransform;
            srt.anchorMin = new Vector2(0.06f, 0.72f);
            srt.anchorMax = new Vector2(0.94f, 0.92f);
            srt.offsetMin = Vector2.zero;
            srt.offsetMax = Vector2.zero;
        }
    }

    /// 标题下方的短装饰条
    public static void AddTitleUnderline(Transform parent, Vector2 pos, float width, Color color)
    {
        var go = new GameObject("_bar");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(width, 4f);
    }

    /// 卡片底：比面板更亮一层 + 细边框
    public static Image MakeCard(Transform parent, string name, Vector2 pos, Vector2 size, Color? fill = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var bg = go.AddComponent<Image>();
        bg.color = fill ?? Card;
        bg.raycastTarget = true;
        var rt = bg.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var border = new GameObject("_shadow");
        border.transform.SetParent(go.transform, false);
        border.transform.SetAsFirstSibling();
        var bimg = border.AddComponent<Image>();
        bimg.color = Border;
        bimg.raycastTarget = false;
        var brt = bimg.rectTransform;
        brt.anchorMin = Vector2.zero;
        brt.anchorMax = Vector2.one;
        brt.offsetMin = new Vector2(-2f, -2f);
        brt.offsetMax = new Vector2(2f, 2f);

        return bg;
    }

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// 按钮内左侧强调条
    public static void AddAccentEdge(Image btnImg, Color color, float width = 6f)
    {
        if (btnImg == null) return;
        var go = new GameObject("_bar");
        go.transform.SetParent(btnImg.transform, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(width, 0f);
    }
}
