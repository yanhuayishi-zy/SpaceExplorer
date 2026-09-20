using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// 自动开场动画 + 结束/胜利动画
public class AutoIntroOutro : MonoBehaviour
{
    public static bool IntroDone { get; private set; }
    public static AutoIntroOutro Instance { get; private set; }

    Canvas canvas;
    Image dim;
    Text title;
    Text subtitle;
    Text center;
    bool outroPlaying;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Object.FindObjectOfType<AutoIntroOutro>() != null) return;
        var go = new GameObject("AutoIntroOutro");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<AutoIntroOutro>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        IntroDone = false;
        BuildUi();
        // 不在启动时播开场：等 ID/模式选完再 BeginIntro()
        if (canvas != null) canvas.gameObject.SetActive(false);
        if (dim != null)
        {
            dim.color = new Color(0f, 0f, 0f, 0f);
            dim.raycastTarget = false;
        }
    }

    /// 闸门结束后调用，开始开场动画
    public static void BeginIntro()
    {
        if (Instance == null) return;
        if (IntroDone) return;
        Instance.StartCoroutine(Instance.PlayIntro());
    }

    /// 跳过开场（直接开打）
    public static void SkipIntro()
    {
        if (Instance == null)
        {
            IntroDone = true;
            return;
        }
        Instance.StopAllCoroutines();
        Instance.FinishIntroVisual();
        IntroDone = true;
    }

    void FinishIntroVisual()
    {
        if (title != null) title.text = "";
        if (subtitle != null) subtitle.text = "";
        if (center != null) center.text = "";
        if (dim != null)
        {
            dim.color = new Color(0f, 0f, 0f, 0f);
            dim.raycastTarget = false;
        }
        if (canvas != null) canvas.gameObject.SetActive(false);
    }

    void BuildUi()
    {
        var canvasGo = new GameObject("IntroOutroCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        dim = CreateImage(canvasGo.transform, "Dim", Color.black);
        Stretch(dim.rectTransform);

        title = CreateText(canvasGo.transform, "Title", 64, new Color(0.75f, 0.95f, 1f));
        subtitle = CreateText(canvasGo.transform, "Subtitle", 28, new Color(0.85f, 0.85f, 0.9f));
        center = CreateText(canvasGo.transform, "Center", 96, Color.white);

        Place(title.rectTransform, 0f, 80f);
        Place(subtitle.rectTransform, 0f, 10f);
        Place(center.rectTransform, 0f, -40f);

        title.text = "";
        subtitle.text = "";
        center.text = "";
        dim.color = new Color(0f, 0f, 0f, 1f);
    }

    static Image CreateImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static Text CreateText(Transform parent, string name, int size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<Text>();
        text.font = PixelUi.Font;
        text.fontSize = size;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.text = "";
        var rt = text.rectTransform;
        rt.sizeDelta = new Vector2(900f, 120f);
        return text;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void Place(RectTransform rt, float x, float y)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
    }

    IEnumerator PlayIntro()
    {
        if (canvas != null) canvas.gameObject.SetActive(true);

        // 阻止开局被打
        if (GameManager.Instance != null)
        {
            GameManager.Instance.isGameOver = false;
        }
        Time.timeScale = 1f;

        yield return FadeDim(1f, 0.55f, 0.8f);

        title.text = "黄道征途";
        title.transform.localScale = Vector3.one * 0.6f;
        yield return ScaleTo(title.transform, 1.15f, 0.45f);
        yield return ScaleTo(title.transform, 1f, 0.2f);

        subtitle.text = "Space Explorer";
        yield return FadeText(subtitle, 0f, 1f, 0.35f);
        yield return new WaitForSecondsRealtime(0.8f);

        subtitle.text = "自动射击  ·  释放技能  ·  接住补给";
        yield return new WaitForSecondsRealtime(1.2f);

        string[] counts = { "3", "2", "1", "出发!" };
        foreach (var c in counts)
        {
            center.text = c;
            center.color = c == "出发!" ? new Color(0.4f, 1f, 0.55f) : Color.white;
            center.transform.localScale = Vector3.one * 1.4f;
            yield return ScaleTo(center.transform, 1f, 0.28f);
            yield return new WaitForSecondsRealtime(c == "出发!" ? 0.45f : 0.35f);
        }

        center.text = "";
        title.text = "";
        subtitle.text = "";
        yield return FadeDim(dim.color.a, 0f, 0.45f);
        dim.raycastTarget = false;
        if (canvas != null) canvas.gameObject.SetActive(false);
        IntroDone = true;
    }

    public void PlayGameOver()
    {
        if (outroPlaying) return;
        outroPlaying = true;
        StartCoroutine(OutroRoutine(false));
    }

    public void PlayVictory()
    {
        // 无尽模式：不再通关，转为结束结算
        PlayGameOver();
    }

    IEnumerator OutroRoutine(bool victory)
    {
        // 确保结束动画画布可见
        if (canvas != null) canvas.gameObject.SetActive(true);
        Time.timeScale = 0f;

        if (dim != null)
        {
            dim.raycastTarget = true;
            dim.color = new Color(0f, 0f, 0f, 0.65f);
        }

        if (victory)
        {
            if (title != null) { title.text = "通关"; title.color = new Color(0.45f, 1f, 0.65f); }
            if (center != null) { center.text = "恭喜通关"; center.color = new Color(0.55f, 1f, 0.7f); }
        }
        else
        {
            if (title != null) { title.text = "游戏结束"; title.color = new Color(1f, 0.45f, 0.4f); }
            if (center != null) { center.text = "GAME OVER"; center.color = new Color(1f, 0.55f, 0.5f); }
        }

        if (title != null) title.transform.localScale = Vector3.one;
        int score = GameManager.Instance != null ? GameManager.Instance.score : 0;
        int kills = GameManager.Instance != null ? GameManager.Instance.killCount : 0;
        int high = GameManager.Instance != null ? GameManager.Instance.highScore : score;
        if (subtitle != null)
        {
            subtitle.text = $"分数 {score}    击杀 {kills}    最高 {high}\n再次挑战 / 返回主页面";
        }

        MobileGameShell.ShowGameOverBoard();
        yield break;
    }

    void Update()
    {
        if (!outroPlaying) return;
        bool restart = Input.GetKeyDown(KeyCode.R) || MobileControls.ConsumeRestart();
        if (restart)
        {
            Time.timeScale = 1f;
            IntroDone = false;
            outroPlaying = false;
            FinishIntroVisual();
            MobileGameShell.ClearGameOverBoards();
            // 重开直接进局，跳过长开场
            SkipIntro();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame();
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            }
        }
    }

    IEnumerator FadeDim(float from, float to, float time)
    {
        float t = 0f;
        var c = dim.color;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(from, to, t / time);
            dim.color = new Color(c.r, c.g, c.b, a);
            yield return null;
        }
        dim.color = new Color(c.r, c.g, c.b, to);
    }

    IEnumerator FadeDimRealtime(float from, float to, float time)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(from, to, t / time);
            dim.color = new Color(0f, 0f, 0f, a);
            yield return null;
        }
        dim.color = new Color(0f, 0f, 0f, to);
    }

    IEnumerator ScaleTo(Transform target, float to, float time)
    {
        Vector3 from = target.localScale;
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            target.localScale = Vector3.Lerp(from, Vector3.one * to, t / time);
            yield return null;
        }
        target.localScale = Vector3.one * to;
    }

    IEnumerator ScaleToRealtime(Transform target, float to, float time)
    {
        Vector3 from = target.localScale;
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            target.localScale = Vector3.Lerp(from, Vector3.one * to, t / time);
            yield return null;
        }
        target.localScale = Vector3.one * to;
    }

    IEnumerator FadeText(Text text, float from, float to, float time)
    {
        float t = 0f;
        var c = text.color;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            text.color = new Color(c.r, c.g, c.b, Mathf.Lerp(from, to, t / time));
            yield return null;
        }
        text.color = new Color(c.r, c.g, c.b, to);
    }

    IEnumerator FadeTextRealtime(Text text, float from, float to, float time)
    {
        float t = 0f;
        var c = text.color;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            text.color = new Color(c.r, c.g, c.b, Mathf.Lerp(from, to, t / time));
            yield return null;
        }
        text.color = new Color(c.r, c.g, c.b, to);
    }
}
