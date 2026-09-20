using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// WebSocket 联机客户端（System.Net，无需第三方包）
public class MultiplayerNet : MonoBehaviour
{
    public static MultiplayerNet Instance { get; private set; }

    public bool IsConnected { get; private set; }
    public bool InRoom { get; private set; }
    public string RoomId { get; private set; } = "";
    public string PlayerId { get; private set; } = "";
    public string HostId { get; private set; } = "";
    public int Seed { get; private set; }
    public bool IsHost => !string.IsNullOrEmpty(HostId) && HostId == PlayerId;

    ClientWebSocket ws;
    CancellationTokenSource cts;
    readonly ConcurrentQueue<string> inbox = new ConcurrentQueue<string>();
    float stateTimer;
    string serverUrl = "ws://127.0.0.1:8765";

    public event Action OnJoined;
    public event Action<string> OnError;
    public event Action<string> OnPeerJoin;
    public event Action<string> OnPeerLeave;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindObjectOfType<MultiplayerNet>() != null) return;
        var go = new GameObject("MultiplayerNet");
        DontDestroyOnLoad(go);
        go.AddComponent<MultiplayerNet>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        serverUrl = PlayerPrefs.GetString("MP_Server", serverUrl);
    }

    public void SetServer(string url)
    {
        serverUrl = url.Trim();
        PlayerPrefs.SetString("MP_Server", serverUrl);
        PlayerPrefs.Save();
    }

    public string GetServer() => serverUrl;

    public async void ConnectAndCreate()
    {
        await ConnectInternal();
        if (IsConnected)
        {
            SendRaw(new { t = "create", seed = UnityEngine.Random.Range(1, 999999) });
        }
    }

    public async void ConnectAndJoin(string room)
    {
        room = (room ?? "").Trim().ToUpper();
        if (room.Length == 0)
        {
            OnError?.Invoke("请输入房间号");
            return;
        }
        await ConnectInternal();
        if (IsConnected)
        {
            SendRaw(new { t = "join", room = room });
        }
    }

    async Task ConnectInternal()
    {
        try
        {
            Disconnect();
            cts = new CancellationTokenSource();
            ws = new ClientWebSocket();
            ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            var uri = new Uri(serverUrl);
            await ws.ConnectAsync(uri, cts.Token);
            IsConnected = true;
            _ = Task.Run(ReceiveLoop);
            Debug.Log("已连接服务器 " + serverUrl);
        }
        catch (Exception e)
        {
            IsConnected = false;
            OnError?.Invoke("连接失败: " + e.Message);
            Debug.LogWarning(e);
        }
    }

    async Task ReceiveLoop()
    {
        var buffer = new byte[64 * 1024];
        var sb = new StringBuilder();
        try
        {
            while (ws != null && ws.State == WebSocketState.Open && !cts.IsCancellationRequested)
            {
                sb.Clear();
                WebSocketReceiveResult result;
                do
                {
                    result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        IsConnected = false;
                        InRoom = false;
                        return;
                    }
                    sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                } while (!result.EndOfMessage);

                inbox.Enqueue(sb.ToString());
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("WS receive end: " + e.Message);
            IsConnected = false;
            InRoom = false;
        }
    }

    void Update()
    {
        while (inbox.TryDequeue(out var raw))
        {
            HandleMessage(raw);
        }

        // 本地玩家状态上行 12Hz
        if (InRoom && IsConnected)
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
            {
                stateTimer = 1f / 12f;
                SendLocalState();
            }
        }
    }

    void HandleMessage(string raw)
    {
        try
        {
            var msg = JsonUtility.FromJson<NetMsg>(raw);
            if (msg == null) return;
            switch (msg.t)
            {
                case "welcome":
                    PlayerId = msg.pid;
                    break;
                case "joined":
                    PlayerId = msg.pid;
                    HostId = msg.host;
                    RoomId = msg.room;
                    Seed = msg.seed;
                    InRoom = true;
                    OnJoined?.Invoke();
                    MultiplayerGame.NotifyRoomReady();
                    break;
                case "join":
                    OnPeerJoin?.Invoke(msg.pid);
                    MultiplayerGame.NotifyPeerJoin(msg.pid);
                    break;
                case "leave":
                    OnPeerLeave?.Invoke(msg.pid);
                    MultiplayerGame.NotifyPeerLeave(msg.pid);
                    break;
                case "host":
                    HostId = msg.pid;
                    break;
                case "error":
                    OnError?.Invoke(string.IsNullOrEmpty(msg.msg) ? "联机错误" : msg.msg);
                    break;
                case "state":
                    MultiplayerGame.ApplyRemoteState(msg);
                    break;
                case "event":
                    MultiplayerGame.ApplyRemoteEvent(msg);
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("parse fail " + e.Message + " raw=" + raw);
        }
    }

    void SendLocalState()
    {
        var player = GameObject.Find("Player");
        if (player == null) return;
        var p = player.transform.position;
        var shooting = player.GetComponent<PlayerShooting>();
        var health = player.GetComponent<PlayerHealth>();
        var net = new NetMsg
        {
            t = "state",
            pid = PlayerId,
            x = p.x,
            y = p.y,
            hp = health != null ? health.currentHealth : 3,
            score = GameManager.Instance != null ? GameManager.Instance.score : 0,
            spread = shooting != null && shooting.IsSpreadActive() ? 1 : 0,
        };
        SendRaw(net);
    }

    public void SendEvent(string ev, float x, float y, int a = 0)
    {
        if (!InRoom || !IsConnected) return;
        SendRaw(new NetMsg { t = "event", ev = ev, x = x, y = y, a = a, pid = PlayerId });
    }

    void SendRaw(object obj)
    {
        if (ws == null || ws.State != WebSocketState.Open) return;
        try
        {
            var json = JsonUtility.ToJson(obj);
            var bytes = Encoding.UTF8.GetBytes(json);
            _ = ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cts.Token);
        }
        catch (Exception e)
        {
            Debug.LogWarning("send fail " + e.Message);
        }
    }

    public void Leave()
    {
        SendRaw(new { t = "leave" });
        Disconnect();
        InRoom = false;
        RoomId = "";
    }

    void Disconnect()
    {
        try { cts?.Cancel(); } catch { }
        try { ws?.Dispose(); } catch { }
        ws = null;
        IsConnected = false;
        InRoom = false;
    }

    void OnDestroy()
    {
        Disconnect();
    }

    [Serializable]
    public class NetMsg
    {
        public string t;
        public string pid;
        public string room;
        public string host;
        public string msg;
        public string ev;
        public int seed;
        public float x;
        public float y;
        public int hp;
        public int score;
        public int spread;
        public int a;
    }
}
