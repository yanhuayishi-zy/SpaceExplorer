using System.Collections.Generic;
using System.IO;
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

    /// 网页端：选图后写入本地存档，下次打开无需再传
    public static bool SetCustomAvatarFromBytes(byte[] bytes)
    {
        if (bytes == null || bytes.Length < 8) return false;
        try
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(bytes))
            {
                LastAvatarStatus = "图片解码失败，请用 png/jpg";
                return false;
            }
            customAvatarSprite = Sprite.Create(
                tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            AvatarId = "custom";
            // 持久化：文件 + PlayerPrefs（网页端 IndexedDB）
            try
            {
                if (!Directory.Exists(CustomAvatarDir)) Directory.CreateDirectory(CustomAvatarDir);
                File.WriteAllBytes(CustomAvatarPath, bytes);
            }
            catch (System.Exception fe) { Debug.LogWarning("save avatar file: " + fe.Message); }
            try
            {
                PlayerPrefs.SetString(KeyCustomAvatarB64, System.Convert.ToBase64String(bytes));
                PlayerPrefs.Save();
            }
            catch { }
            LastAvatarStatus = "已保存本地头像（只需上传一次）";
            return true;
        }
        catch (System.Exception e)
        {
            LastAvatarStatus = "头像加载失败";
            Debug.LogWarning(e.Message);
            return false;
        }
    }

    public const string KeyCustomAvatarB64 = "SE_CustomAvatarB64";

    public static byte[] GetCustomAvatarBytes()
    {
        try
        {
            if (File.Exists(CustomAvatarPath))
            {
                var b = File.ReadAllBytes(CustomAvatarPath);
                if (b != null && b.Length > 8) return b;
            }
        }
        catch { }
        try
        {
            string b64 = PlayerPrefs.GetString(KeyCustomAvatarB64, "");
            if (!string.IsNullOrEmpty(b64))
            {
                var b = System.Convert.FromBase64String(b64);
                if (b != null && b.Length > 8) return b;
            }
        }
        catch { }
        return null;
    }

    static Sprite LoadCustomAvatar()
    {
        // 0) PlayerPrefs 里的网页存档（刷新后优先）
        try
        {
            string b64 = PlayerPrefs.GetString(KeyCustomAvatarB64, "");
            if (!string.IsNullOrEmpty(b64))
            {
                var b = System.Convert.FromBase64String(b64);
                var tex0 = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (b != null && b.Length > 8 && tex0.LoadImage(b))
                {
                    LastAvatarStatus = "已恢复本地头像";
                    return Sprite.Create(tex0, new Rect(0, 0, tex0.width, tex0.height), new Vector2(0.5f, 0.5f), 100f);
                }
            }
        }
        catch { }

        // 1) Resources
        var fromRes = Resources.Load<Sprite>(ResourceCustomAvatar);
        if (fromRes != null)
        {
            LastAvatarStatus = "已使用 Resources/CustomAvatar";
            return fromRes;
        }

        // 2) 存档目录
        string path = CustomAvatarPath;
        if (!System.IO.File.Exists(path) && System.IO.Directory.Exists(CustomAvatarDir))
        {
            var files = System.IO.Directory.GetFiles(CustomAvatarDir, "*.png");
            if (files.Length == 0) files = System.IO.Directory.GetFiles(CustomAvatarDir, "*.jpg");
            if (files.Length > 0) path = files[0];
        }

        if (!System.IO.File.Exists(path))
        {
            LastAvatarStatus = "未找到自定义头像，请选择一次本地图片";
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
        // 亲属辱骂
        "爸", "妈", "爹", "娘", "爷爷", "奶奶", "儿子", "女儿",
        "祖宗", "全家", "死全家", "户口本",

        // 常见脏话/拼音缩写（含 bb / sm / nmzl / cnm）
        "bb", "sm", "nmzl", "cnm", "wcnm", "rnm", "rnmb", "nmsl", "nmb", "nm",
        "sb", "shabi", "傻逼", "煞笔", "傻b", "傻B",
        "jb", "j8", "鸡巴", "操", "艹", "草泥马", "操你", "干你",
        "tmd", "tm", "tnnd", "tnmd", "妈的", "妈批", "尼玛", "你妈",
        "fuck", "fucker", "shit", "bitch", "dick", "cock", "pussy", "ass",
        "wtf", "stfu", "nazi", "hitler", "nigga", "nigger",
        "垃圾", "废物", "去死", "贱人", "婊子", "妓女", "鸡", "鸭子",
        "狗杂", "杂种", "畜生", "贱货", "烂货", "破鞋",

        // 高敏政治/违法/色情
        "共产党", "国民党", "习近平", "毛泽东", "六四", "天安门",
        "法轮", "法轮功", "邪教", "台独", "藏独", "疆独", "港独",
        "反动", "卖国", "汉奸",
        "色情", "porn", "sex", "sexy", "约炮", "援交", "嫖", "妓",
        "av", "gv", "裸聊", "开房", "冰毒", "吸毒", "赌博", "枪支",
        "自杀", "自残", "恐怖", "爆炸", "杀人",
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
