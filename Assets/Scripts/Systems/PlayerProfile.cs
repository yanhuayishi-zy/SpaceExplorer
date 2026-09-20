using System.Collections.Generic;
using UnityEngine;

/// 玩家 ID 与排行榜隐私
public static class PlayerProfile
{
    public const string KeyId = "SE_PlayerId";
    public const string KeyHide = "SE_HideId";
    public const string KeyLbUrl = "SE_LbUrl";
    public const string KeyProfileDone = "SE_ProfileDone";
    public const string KeyAvatar = "SE_Avatar";

    /// 可选头像（机体立绘 id + custom）
    public static readonly string[] AvatarIds =
    {
        "mortal", "zeus", "poseidon", "ares", "athena",
        "apollo", "artemis", "hermes", "hades",
        "custom",
    };

    /// 自定义头像目录
    public static string CustomAvatarDir =>
        System.IO.Path.Combine(Application.persistentDataPath, "Avatars");

    /// 首选自定义图路径
    public static string CustomAvatarPath =>
        System.IO.Path.Combine(CustomAvatarDir, "avatar.png");

    /// 资源兜底：Assets/Resources/CustomAvatar.png（编辑器丢进去即可）
    public const string ResourceCustomAvatar = "CustomAvatar";

    public static string LastAvatarStatus = "";

    public static string AvatarId
    {
        get => PlayerPrefs.GetString(KeyAvatar, "mortal");
        set
        {
            PlayerPrefs.SetString(KeyAvatar, string.IsNullOrEmpty(value) ? "mortal" : value);
            PlayerPrefs.Save();
        }
    }

    static Sprite customAvatarSprite;

    public static Sprite LoadAvatarSprite(string id = null)
    {
        if (string.IsNullOrEmpty(id)) id = AvatarId;
        if (id == "custom")
        {
            if (customAvatarSprite != null) return customAvatarSprite;
            customAvatarSprite = LoadCustomAvatar();
            if (customAvatarSprite != null) return customAvatarSprite;
            id = "mortal";
        }
        var spr = Resources.Load<Sprite>("GodShip_" + id);
        if (spr == null) spr = Resources.Load<Sprite>("PlayerShip");
        return spr;
    }

    public static void ReloadCustomAvatar()
    {
        customAvatarSprite = null;
        customAvatarSprite = LoadCustomAvatar();
    }

    static Sprite LoadCustomAvatar()
    {
        // 1) Resources 里放 CustomAvatar（最省事，编辑器）
        var fromRes = Resources.Load<Sprite>(ResourceCustomAvatar);
        if (fromRes != null)
        {
            LastAvatarStatus = "已使用 Resources/CustomAvatar";
            return fromRes;
        }

        // 2) 存档目录 avatar.png / 任意 png
        string path = CustomAvatarPath;
        if (!System.IO.File.Exists(path) && System.IO.Directory.Exists(CustomAvatarDir))
        {
            var files = System.IO.Directory.GetFiles(CustomAvatarDir, "*.png");
            if (files.Length == 0) files = System.IO.Directory.GetFiles(CustomAvatarDir, "*.jpg");
            if (files.Length > 0) path = files[0];
        }

        // 3) StreamingAssets/Avatars
        if (!System.IO.File.Exists(path))
        {
            string sa = System.IO.Path.Combine(Application.streamingAssetsPath, "Avatars");
            string saFile = System.IO.Path.Combine(sa, "avatar.png");
            if (System.IO.File.Exists(saFile)) path = saFile;
        }

        if (!System.IO.File.Exists(path))
        {
            LastAvatarStatus = "未找到自定义头像，请放入 avatar.png";
            return null;
        }

        try
        {
            var bytes = System.IO.File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(bytes))
            {
                LastAvatarStatus = "图片解码失败，请用 png/jpg";
                return null;
            }
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            LastAvatarStatus = "已加载: " + System.IO.Path.GetFileName(path);
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
        catch (System.Exception e)
        {
            LastAvatarStatus = "读取失败: " + e.Message;
            return null;
        }
    }

    /// 用系统打开头像目录（便于放自定义图）
    public static void OpenCustomAvatarFolder()
    {
        try
        {
            if (!System.IO.Directory.Exists(CustomAvatarDir))
                System.IO.Directory.CreateDirectory(CustomAvatarDir);
            string url = "file:///" + CustomAvatarDir.Replace('\\', '/');
            Application.OpenURL(url);
            LastAvatarStatus = "请把图片命名为 avatar.png 放入该文件夹，再点刷新";
        }
        catch (System.Exception e)
        {
            LastAvatarStatus = "无法打开文件夹: " + CustomAvatarDir;
            Debug.LogWarning(LastAvatarStatus + " " + e.Message);
        }
    }

    /// 提交到排行榜的显示名：隐藏时打码且不带称号（只影响他人所见）
    public static string LeaderboardDisplay(string rawId = null)
    {
        string id = string.IsNullOrEmpty(rawId) ? PlayerId : rawId;
        if (HideId)
            return DisplayName(id);
        string title = Achievements.CurrentTitle;
        if (string.IsNullOrEmpty(title)) return DisplayName(id);
        return DisplayName(id) + "「" + title + "」";
    }

    public const int MinIdLen = 2;
    public const int MaxIdLen = 6;

    /// 敏感词（小写匹配；中文直接包含）
    static readonly HashSet<string> Banned = new HashSet<string>
    {
        "爸", "妈", "爹", "娘", "爷爷", "奶奶", "儿子", "女儿",
        "傻逼", "煞笔", "傻b", "sb", "操", "艹", "逼", "鸡巴", "jb",
        "fuck", "shit", "bitch", "nmsl", "nmb", "cnm", "wtf",
        "垃圾", "废物", "去死", "死全家", "贱人", "婊子", "妓女",
        "共产党", "法轮", "色情", "porn", "sex", "约炮",
    };

    public static string PlayerId
    {
        get => PlayerPrefs.GetString(KeyId, "");
        set
        {
            PlayerPrefs.SetString(KeyId, value ?? "");
            PlayerPrefs.Save();
        }
    }

    public static bool HideId
    {
        get => PlayerPrefs.GetInt(KeyHide, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(KeyHide, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static string LeaderboardUrl
    {
        get => PlayerPrefs.GetString(KeyLbUrl, "https://yx1.1565568.xyz");
        set
        {
            PlayerPrefs.SetString(KeyLbUrl, (value ?? "").Trim());
            PlayerPrefs.Save();
        }
    }

    public static bool ProfileReady
    {
        get
        {
            // 只要本机已有合法 ID 就算完成，不必每次再输
            string id = PlayerId;
            if (!string.IsNullOrEmpty(id) && id.Length >= MinIdLen) return true;
            return PlayerPrefs.GetInt(KeyProfileDone, 0) == 1 && !string.IsNullOrEmpty(id);
        }
    }

    /// 返回 null 表示通过；否则返回错误提示
    public static string ValidateId(string raw)
    {
        string id = (raw ?? "").Trim();
        if (id.Length < MinIdLen) return "ID 至少 " + MinIdLen + " 个字符";
        if (id.Length > MaxIdLen) return "ID 最多 " + MaxIdLen + " 个字符";

        string lower = id.ToLowerInvariant();
        foreach (var w in Banned)
        {
            if (lower.Contains(w)) return "ID 含有敏感词，请换一个";
        }
        return null;
    }

    public static bool TryCompleteProfile(string raw, out string error)
    {
        error = ValidateId(raw);
        if (error != null) return false;
        CompleteProfile(raw.Trim());
        return true;
    }

    public static void CompleteProfile(string id)
    {
        id = (id ?? "").Trim();
        if (id.Length == 0) id = "Pilot" + Random.Range(1000, 9999);
        if (id.Length > MaxIdLen) id = id.Substring(0, MaxIdLen);
        if (id.Length < MinIdLen) id = (id + "Pilot").Substring(0, MinIdLen);
        PlayerId = id;
        PlayerPrefs.SetInt(KeyProfileDone, 1);
        PlayerPrefs.Save();
        SaveIdFile(id);
    }

    static string IdFilePath
    {
        get { return System.IO.Path.Combine(Application.persistentDataPath, "se_player_id.txt"); }
    }

    static void SaveIdFile(string id)
    {
        try
        {
            System.IO.File.WriteAllText(IdFilePath, id);
        }
        catch { }
    }

    /// 启动时若 PlayerPrefs 丢了，尝试从文件恢复
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RestoreOnce()
    {
        try
        {
            string cur = PlayerPrefs.GetString(KeyId, "");
            if (!string.IsNullOrEmpty(cur)) return;
            if (!System.IO.File.Exists(IdFilePath)) return;
            string id = System.IO.File.ReadAllText(IdFilePath).Trim();
            if (id.Length < MinIdLen || id.Length > MaxIdLen) return;
            PlayerPrefs.SetString(KeyId, id);
            PlayerPrefs.SetInt(KeyProfileDone, 1);
            PlayerPrefs.Save();
        }
        catch { }
    }

    /// 生成 2–6 字合法随机 ID
    public static string MakeRandomId()
    {
        string[] heads = { "星", "焰", "雷", "银", "苍", "夜", "风", "光", "Ace", "Neo", "Sky", "Ray" };
        string head = heads[Random.Range(0, heads.Length)];
        int n = Random.Range(10, 100);
        string id = head + n;
        if (id.Length > MaxIdLen) id = id.Substring(0, MaxIdLen);
        if (ValidateId(id) != null) id = "P" + n;
        return id;
    }

    /// 排行榜显示名：隐藏时打码
    public static string DisplayName(string rawId)
    {
        string id = string.IsNullOrEmpty(rawId) ? PlayerId : rawId;
        if (string.IsNullOrEmpty(id)) return "匿名";
        if (!HideId) return id;
        if (id.Length <= 2) return id[0] + "*";
        return id[0] + new string('*', Mathf.Max(1, id.Length - 2)) + id[id.Length - 1];
    }
}
