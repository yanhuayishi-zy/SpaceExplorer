using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

/// 排行榜 HTTP 客户端：仅上报无尽分数；带头像/称号/隐藏标记
public static class LeaderboardClient
{
    public static void SubmitEndless(string name, int score, string baseUrl)
    {
        if (ZodiacLevels.IsCampaign) return;
        if (string.IsNullOrEmpty(baseUrl)) return;
        var runner = new GameObject("LB_Submit");
        Object.DontDestroyOnLoad(runner);
        bool hide = PlayerProfile.HideId;
        string display = PlayerProfile.LeaderboardDisplay(name);
        // 自定义头像上传到服务器，供其他人显示；机体 id 直接写榜
        string avatarId = PlayerProfile.AvatarId;
        bool isCustom = avatarId == "custom";
        string avatar = avatarId;
        if (string.IsNullOrEmpty(avatar) || isCustom)
        {
            var ship = ShipMeta.Current;
            avatar = isCustom ? "custom" : (ship != null && !string.IsNullOrEmpty(ship.id) ? ship.id : "mortal");
        }
        string title = Achievements.CurrentTitle;
        runner.AddComponent<LeaderboardRunner>().Submit(name, score, baseUrl, hide, display, avatar, title, isCustom);
    }

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
        public string avatar;
        public string title;
    }

    public void Submit(string name, int score, string baseUrl, bool hide = false,
        string display = null, string avatar = null, string title = null, bool uploadCustomAvatar = false)
    {
        StartCoroutine(SubmitCo(name, score, baseUrl.TrimEnd('/'), hide, display, avatar, title, uploadCustomAvatar));
    }

    public void Fetch(string baseUrl, System.Action<string> onText)
    {
        StartCoroutine(FetchCo(baseUrl.TrimEnd('/'), onText));
    }

    IEnumerator SubmitCo(string name, int score, string baseUrl, bool hide,
        string display, string avatar, string title, bool uploadCustomAvatar)
    {
        // 先上传自定义头像，再报分
        if (uploadCustomAvatar && !hide)
        {
            var imgBytes = PlayerProfile.GetCustomAvatarBytes();
            if (imgBytes != null && imgBytes.Length > 8)
            {
                var av = new AvatarPayload
                {
                    name = name,
                    image = System.Convert.ToBase64String(imgBytes)
                };
                using (var areq = new UnityWebRequest(baseUrl + "/avatar", "POST"))
                {
                    byte[] abody = Encoding.UTF8.GetBytes(JsonUtility.ToJson(av));
                    areq.uploadHandler = new UploadHandlerRaw(abody);
                    areq.downloadHandler = new DownloadHandlerBuffer();
                    areq.SetRequestHeader("Content-Type", "application/json");
                    yield return areq.SendWebRequest();
                    if (areq.result != UnityWebRequest.Result.Success)
                        Debug.LogWarning("头像上传失败: " + areq.error);
                }
            }
        }

        var payload = JsonUtility.ToJson(new ScorePayload
        {
            name = name,
            score = score,
            hide = hide,
            display = string.IsNullOrEmpty(display) ? name : display,
            avatar = avatar ?? "",
            title = title ?? ""
        });
        using (var req = new UnityWebRequest(baseUrl + "/submit", "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(payload);
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
                Debug.LogWarning("排行榜提交失败: " + req.error);
        }
        Destroy(gameObject);
    }

    [System.Serializable]
    class AvatarPayload
    {
        public string name;
        public string image;
    }

    IEnumerator FetchCo(string baseUrl, System.Action<string> onText)
    {
        using (var req = UnityWebRequest.Get(baseUrl + "/top"))
        {
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
                onText?.Invoke(req.downloadHandler.text);
            else
            {
                Debug.LogWarning("排行榜拉取失败: " + req.error);
                onText?.Invoke("[]");
            }
        }
        Destroy(gameObject);
    }
}
