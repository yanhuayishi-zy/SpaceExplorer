using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildScript
{
    public static void BuildWebGL()
    {
        string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Builds/WebGL"));
        Directory.CreateDirectory(outDir);

        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/SampleScene.unity" },
            locationPathName = outDir,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        // 使用项目内手游风模板
        PlayerSettings.WebGL.template = "PROJECT:LockedApp";
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
        PlayerSettings.runInBackground = true;

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError("[Build] WebGL failed: " + report.summary.result);
            EditorApplication.Exit(1);
            return;
        }
        Debug.Log("[Build] WebGL OK -> " + outDir + " size=" + report.summary.totalSize);
        EditorApplication.Exit(0);
    }
}
