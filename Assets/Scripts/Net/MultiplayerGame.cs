using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// 联机房间 UI + 远端玩家同步
public class MultiplayerGame : MonoBehaviour
{
    static MultiplayerGame instance;
    static readonly Dictionary<string, RemotePlayer> remotes = new Dictionary<string, RemotePlayer>();

    InputField serverInput;
    InputField roomInput;
    Text statusText;
    GameObject panel;
    bool uiBuilt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Object.FindObjectOfType<MultiplayerGame>() != null) return;
        var go = new GameObject("MultiplayerGame");
        Object.DontDestroyOnLoad(go);
        instance = go.AddComponent<MultiplayerGame>();
    }

    void Start()
    {
        BuildUi();
    }

    void Update()
    {
        // F1 开关面板
        if (Input.GetKeyDown(KeyCode.F1) && panel != null)
        {
            panel.SetActive(!panel.activeSelf);
        }
    }

    void BuildUi()
    {
        if (uiBuilt) return;
        uiBuilt = true;

        var canvasGo = new GameObject("MpCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        MobileTuning.ConfigureCanvas(scaler);
        canvasGo.AddComponent<GraphicRaycaster>();

        panel = new GameObject("MpPanel");
        panel.transform.SetParent(canvasGo.transform, false);
        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.72f);
        Stretch(panel.GetComponent<RectTransform>());

        var title = MakeText(panel.transform, "联机模式  (F1 关闭)", 40, new Vector2(0, 280));
        serverInput = MakeInput(panel.transform, "服务器地址 ws://IP:端口", new Vector2(0, 180), 520);
        serverInput.text = MultiplayerNet.Instance != null ? MultiplayerNet.Instance.GetServer() : "ws://127.0.0.1:8765";
        roomInput = MakeInput(panel.transform, "房间号（加入时填写）", new Vector2(0, 100), 320);

        MakeButton(panel.transform, "创建房间", new Vector2(-180, 0), () =>
        {
            if (MultiplayerNet.Instance == null) return;
            MultiplayerNet.Instance.SetServer(serverInput.text);
            MultiplayerNet.Instance.ConnectAndCreate();
        });
        MakeButton(panel.transform, "加入房间", new Vector2(180, 0), () =>
        {
            if (MultiplayerNet.Instance == null) return;
            MultiplayerNet.Instance.SetServer(serverInput.text);
            MultiplayerNet.Instance.ConnectAndJoin(roomInput.text);
        });
        MakeButton(panel.transform, "断开", new Vector2(0, -100), () =>
        {
            if (MultiplayerNet.Instance == null) return;
            MultiplayerNet.Instance.Leave();
            foreach (var kv in remotes)
            {
                if (kv.Value != null) Destroy(kv.Value.gameObject);
            }
            remotes.Clear();
            SetStatus("已断开");
        });

        statusText = MakeText(panel.transform, "未连接 · F1 打开/关闭面板", 26, new Vector2(0, -200));

        if (MultiplayerNet.Instance != null)
        {
            MultiplayerNet.Instance.OnJoined += () =>
            {
                var net = MultiplayerNet.Instance;
                SetStatus($"已进入房间 {net.RoomId}  你是 {net.PlayerId}  {(net.IsHost ? "房主" : "加入方")}");
                if (panel != null) panel.SetActive(false);
            };
            MultiplayerNet.Instance.OnError += (e) => SetStatus(e);
        }

        // 默认隐藏，避免挡游戏；按 F1 打开
        panel.SetActive(false);
    }

    public static void SetStatus(string s)
    {
        if (instance != null && instance.statusText != null)
        {
            instance.statusText.text = s;
            Debug.Log("[MP] " + s);
        }
        else
        {
            Debug.Log("[MP] " + s);
        }
    }

    /// 打开/关闭联机面板（主菜单入口）
    public static void TogglePanel()
    {
        if (instance == null) return;
        if (instance.panel == null) return;
        instance.panel.SetActive(!instance.panel.activeSelf);
        if (instance.panel.activeSelf) SetStatus("填写服务器地址后创建或加入房间");
    }

    public static void ShowPanel(bool show)
    {
        if (instance == null || instance.panel == null) return;
        instance.panel.SetActive(show);
        if (show) SetStatus("填写服务器地址后创建或加入房间");
    }

    public static void NotifyRoomReady()
    {
        SetStatus("房间就绪，等待对方…（对方加入后自动同步）");
        // 房主立刻开打；加入方也开，敌机主要由房主逻辑刷，客户端会收事件
        EnsureLocalPlayer();
    }

    public static void NotifyPeerJoin(string pid)
    {
        SetStatus("对方已加入: " + pid);
        SpawnRemote(pid);
        EnsureLocalPlayer();
    }

    public static void NotifyPeerLeave(string pid)
    {
        SetStatus("对方已离开: " + pid);
        if (remotes.TryGetValue(pid, out var r) && r != null)
        {
            Destroy(r.gameObject);
        }
        remotes.Remove(pid);
    }

    static void EnsureLocalPlayer()
    {
        var p = GameObject.Find("Player");
        if (p != null && p.GetComponent<NetLocalSync>() == null)
        {
            p.AddComponent<NetLocalSync>();
        }
    }

    static void SpawnRemote(string pid)
    {
        if (remotes.ContainsKey(pid)) return;
        var go = new GameObject("Remote_" + pid);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("PlayerShip");
        sr.color = new Color(0.55f, 0.9f, 1f, 0.95f); // 队友偏蓝
        sr.sortingOrder = 6;
        go.transform.localScale = Vector3.one * 0.9f;
        go.transform.position = new Vector3(2f, -2f, 0f);
        var rp = go.AddComponent<RemotePlayer>();
        rp.pid = pid;
        remotes[pid] = rp;
    }

    public static void ApplyRemoteState(MultiplayerNet.NetMsg msg)
    {
        if (string.IsNullOrEmpty(msg.pid)) return;
        if (!remotes.TryGetValue(msg.pid, out var rp) || rp == null)
        {
            SpawnRemote(msg.pid);
            rp = remotes[msg.pid];
        }
        if (rp == null) return;
        rp.Apply(msg);
    }

    public static void ApplyRemoteEvent(MultiplayerNet.NetMsg msg)
    {
        if (msg.ev == "shoot")
        {
            // 队友子弹（纯表现）
            var go = new GameObject("AllyBullet");
            go.transform.position = new Vector3(msg.x, msg.y, 0f);
            go.transform.localScale = Vector3.one * 0.3f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>("Bullet");
            sr.color = new Color(0.5f, 0.95f, 1f, 0.9f);
            sr.sortingOrder = 7;
            var mover = go.AddComponent<SimpleUpBullet>();
            mover.speed = 14f;
            Object.Destroy(go, 2.5f);
        }
    }

    static Text MakeText(Transform parent, string content, int size, Vector2 pos)
    {
        var go = new GameObject("Txt");
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<Text>();
        text.font = PixelUi.Font;
        text.fontSize = size;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = content;
        text.raycastTarget = false;
        var rt = text.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(900f, 60f);
        return text;
    }

    static InputField MakeInput(Transform parent, string placeholder, Vector2 pos, float width)
    {
        var go = new GameObject("Input");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(width, 48f);

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var text = textGo.AddComponent<Text>();
        text.font = PixelUi.Font;
        text.fontSize = 24;
        text.color = Color.white;
        text.supportRichText = false;
        Stretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(10, 6);
        text.rectTransform.offsetMax = new Vector2(-10, -6);

        var phGo = new GameObject("Placeholder");
        phGo.transform.SetParent(go.transform, false);
        var ph = phGo.AddComponent<Text>();
        ph.font = PixelUi.Font;
        ph.fontSize = 22;
        ph.fontStyle = FontStyle.Italic;
        ph.color = new Color(1, 1, 1, 0.35f);
        ph.text = placeholder;
        Stretch(ph.rectTransform);
        ph.rectTransform.offsetMin = new Vector2(10, 6);
        ph.rectTransform.offsetMax = new Vector2(-10, -6);

        var input = go.AddComponent<InputField>();
        input.textComponent = text;
        input.placeholder = ph;
        return input;
    }

    static void MakeButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.45f, 0.9f, 0.9f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(action);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(180f, 52f);

        var tgo = new GameObject("Label");
        tgo.transform.SetParent(go.transform, false);
        var text = tgo.AddComponent<Text>();
        text.font = PixelUi.Font;
        text.fontSize = 26;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = label;
        text.raycastTarget = false;
        Stretch(text.rectTransform);
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}

public class RemotePlayer : MonoBehaviour
{
    public string pid;
    Vector3 target;
    Text hpLabel;

    public void Apply(MultiplayerNet.NetMsg msg)
    {
        target = new Vector3(msg.x, msg.y, 0f);
        if (hpLabel == null)
        {
            var go = new GameObject("Hp");
            go.transform.SetParent(transform, false);
            hpLabel = go.AddComponent<Text>();
            hpLabel.font = PixelUi.Font;
            hpLabel.fontSize = 16;
            hpLabel.alignment = TextAnchor.LowerCenter;
            hpLabel.color = new Color(0.6f, 1f, 1f);
            hpLabel.raycastTarget = false;
            var rt = hpLabel.rectTransform;
            rt.anchoredPosition = new Vector2(0, -40);
            rt.sizeDelta = new Vector2(80, 24);
        }
        hpLabel.text = "HP " + msg.hp;
    }

    void Update()
    {
        transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * 12f);
    }
}

public class NetLocalSync : MonoBehaviour
{
    PlayerShooting shooting;
    float shootEventTimer;

    void Start()
    {
        shooting = GetComponent<PlayerShooting>();
    }

    void Update()
    {
        if (MultiplayerNet.Instance == null || !MultiplayerNet.Instance.InRoom) return;
        // 本地开火时广播表现用子弹
        if (shooting != null && (Input.GetKey(KeyCode.Space) || MobileControls.IsMobile))
        {
            shootEventTimer -= Time.deltaTime;
            if (shootEventTimer <= 0f)
            {
                shootEventTimer = 0.2f;
                MultiplayerNet.Instance.SendEvent("shoot", transform.position.x, transform.position.y + 0.5f);
            }
        }
    }
}

public class SimpleUpBullet : MonoBehaviour
{
    public float speed = 14f;
    void Update()
    {
        transform.position += Vector3.up * speed * Time.deltaTime;
    }
}
