using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// 本地选图：编辑器/PC 扫描目录；网页端走浏览器文件选择框
public static class LocalAvatarPicker
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")]
    static extern void OpenImagePicker();

    public static void TryBrowserPick()
    {
        try { OpenImagePicker(); }
        catch (System.Exception e) { Debug.LogWarning("OpenImagePicker: " + e.Message); }
    }
#else
    public static void TryBrowserPick() { }
#endif

    public static readonly string[] SearchDirs = new[]
    {
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyPictures),
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop),
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) + "\\Downloads",
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) + "\\Pictures",
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) + "\\Desktop",
    };

    public static List<string> CollectImages(int max = 24)
    {
        var list = new List<string>();
        var seen = new HashSet<string>();
        foreach (var dir in SearchDirs)
        {
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) continue;
            try
            {
                foreach (var f in Directory.GetFiles(dir, "*.png"))
                {
                    if (f.IndexOf("icon", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;
                    if (!seen.Add(Path.GetFullPath(f).ToLowerInvariant())) continue;
                    list.Add(f);
                }
                foreach (var f in Directory.GetFiles(dir, "*.jpg"))
                {
                    if (f.IndexOf("icon", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;
                    if (!seen.Add(Path.GetFullPath(f).ToLowerInvariant())) continue;
                    list.Add(f);
                }
            }
            catch { }
        }
        list.Sort((a, b) => File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));
        if (list.Count > max) list.RemoveRange(max, list.Count - max);
        return list;
    }

    /// 把选中的图复制到存档目录并加载
    public static bool UseFile(string srcPath)
    {
        if (string.IsNullOrEmpty(srcPath) || !File.Exists(srcPath)) return false;
        try
        {
            if (!Directory.Exists(PlayerProfile.CustomAvatarDir))
                Directory.CreateDirectory(PlayerProfile.CustomAvatarDir);
            string dst = PlayerProfile.CustomAvatarPath;
            File.Copy(srcPath, dst, true);
            PlayerProfile.ReloadCustomAvatar();
            PlayerProfile.AvatarId = "custom";
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("UseFile fail: " + e.Message);
            return false;
        }
    }

#if UNITY_EDITOR
    /// 编辑器：系统文件选择框
    public static bool TryEditorPick()
    {
        try
        {
            string path = UnityEditor.EditorUtility.OpenFilePanel("选择头像", "", "png,jpg");
            if (string.IsNullOrEmpty(path)) return false;
            return UseFile(path);
        }
        catch { return false; }
    }
#endif
}
