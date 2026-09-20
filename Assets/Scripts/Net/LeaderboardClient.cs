using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

/// 排行榜 HTTP 客户端：仅上报无尽（无限战争）分数
public static class LeaderboardClient
{
    public static void SubmitEndless(string name, int score, string baseUrl)
    {
        // 仅无尽模式上榜，避免闯关分数混入
        if (ZodiacLevels.IsCampaign) return;
        if (string.IsNullOrEmpty(baseUrl)) return;
        var runner = new GameObject("LB_Submit");
        Object.DontDestroyOnLoad(runner);
        bool hide = PlayerProfile.HideId;
        string display = PlayerProfile.LeaderboardDisplay(name);
        runner.AddComponent<LeaderboardRunner>().Submit(name, score, baseUrl, hide, display);
    }

    /// 兼容旧调用名
    public static void Submit(string name, int score, string baseUrl)
    {
        SubmitEndless(name, score, baseUrl);
    }

    public static void FetchTop(string baseUrl, System.Action<string> onText)
    {
        if (string.IsNullOrEmpty(baseUrl)) return;
        var runner = new GameObject("LB_Fetch");
        Object.DontDestroyOnLoad(runner);
        runner.AddComponent<LeaderboardRunner>().Fetch(baseUrl, onText);
    }
}

public class LeaderboardRunner : MonoBehaviour
{
    [System.Serializable]
    public class ScorePayload
    {
        public string name;
        public int score;
        public bool hide;
        public string display;
    }

    public void Submit(string name, int score, string baseUrl, bool hide = false, string display = null)
    {
        StartCoroutine(SubmitCo(name, score, baseUrl.TrimEnd('/'), hide, display));
    }

    public void Fetch(string baseUrl, System.Action<string> onText)
    {
        StartCoroutine(FetchCo(baseUrl.TrimEnd('/'), onText));
    }

    IEnumerator SubmitCo(string name, int score, string baseUrl, bool hide, string display)
    {
        var payload = JsonUtility.ToJson(new ScorePayload
        {
            name = name,
            score = score,
            hide = hide,
            display = string.IsNullOrEmpty(display) ? name : display
        });
        using (var req = new UnityWebRequest(baseUrl + "/submit", "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(payload);
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("排行榜提交失败: " + req.error);
            }
        }
        Destroy(gameObject);
    }

    IEnumerator FetchCo(string baseUrl, System.Action<string> onText)
    {
        using (var req = UnityWebRequest.Get(baseUrl + "/top"))
        {
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
            {
                onText?.Invoke(req.downloadHandler.text);
            }
            else
            {
                Debug.LogWarning("排行榜拉取失败: " + req.error);
                onText?.Invoke("[]");
            }
        }
        Destroy(gameObject);
    }
}
