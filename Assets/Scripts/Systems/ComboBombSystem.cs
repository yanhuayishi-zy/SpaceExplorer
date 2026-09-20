using UnityEngine;
using UnityEngine.UI;

/// 连击倍率（炸弹已移除）
public class ComboBombSystem : MonoBehaviour
{
    public static ComboBombSystem Instance { get; private set; }

    public int combo { get; private set; }
    public float comboTimer;
    public float comboWindow = 2.2f;

    Text comboText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Object.FindObjectOfType<ComboBombSystem>() != null) return;
        var go = new GameObject("ComboBombSystem");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<ComboBombSystem>();
    }

    void Awake()
    {
        Instance = this;
        BuildHud();
    }

    void BuildHud()
    {
        var canvasGo = GameObject.Find("HudCanvas");
        Transform parent;
        if (canvasGo != null) parent = canvasGo.transform;
        else
        {
            var cgo = new GameObject("ComboHudCanvas");
            var canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 55;
            var scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            cgo.AddComponent<GraphicRaycaster>();
            parent = cgo.transform;
        }

        comboText = MakeText(parent, "ComboText", new Vector2(0.5f, 1f), new Vector2(0, -120), 42, new Color(1f, 0.85f, 0.25f));
        comboText.text = "";
    }

    Text MakeText(Transform parent, string name, Vector2 anchor, Vector2 pos, int size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<Text>();
        text.font = PixelUi.Font;
        text.fontSize = size;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        var rt = text.rectTransform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(400, 50);
        return text;
    }

    void Update()
    {
        if (comboTimer > 0f)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f) combo = 0;
        }
        RefreshHud();
    }

    public int RegisterKill(int baseScore)
    {
        combo++;
        comboTimer = comboWindow;
        int mult = ComboMultiplier();
        Achievements.NotifyCombo(combo);
        return baseScore * mult;
    }

    public void BreakCombo()
    {
        combo = 0;
        comboTimer = 0f;
    }

    public int ComboMultiplier()
    {
        if (combo >= 30) return 4;
        if (combo >= 18) return 3;
        if (combo >= 10) return 2;
        return 1;
    }

    void RefreshHud()
    {
        if (comboText == null) return;
        try
        {
            int mult = ComboMultiplier();
            if (combo >= 3)
            {
                comboText.text = combo + " 连击  x" + mult;
                comboText.color = mult >= 4 ? new Color(1f, 0.4f, 0.3f) : new Color(1f, 0.85f, 0.25f);
                comboText.transform.localScale = Vector3.one * (1f + 0.05f * Mathf.Sin(Time.unscaledTime * 10f));
            }
            else
            {
                comboText.text = "";
                comboText.transform.localScale = Vector3.one;
            }
        }
        catch
        {
            comboText = null;
        }
    }

    public static void NotifyPlayerHit()
    {
        if (Instance != null) Instance.BreakCombo();
    }
}
