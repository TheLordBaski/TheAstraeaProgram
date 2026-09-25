using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TAP.EditorTools
{
    /// <summary>Standalone Windows build (menu, eval automation or -executeMethod).</summary>
    public static class BuildScript
    {
        public const string OutputDir = "Build/Windows";
        public const string ExeName = "TAP.exe";

        [MenuItem("TAP/Build Windows Player")]
        public static void BuildWindowsMenu()
        {
            var r = BuildWindows();
            EditorUtility.DisplayDialog("TAP build", r, "OK");
        }

        /// <summary>Builds the three scenes into Build/Windows/TAP.exe and returns a one-line summary.</summary>
        public static string BuildWindows()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string dir = Path.Combine(root, OutputDir);
            Directory.CreateDirectory(dir);
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            PlayerSettings.companyName = "Astraea Works";
            PlayerSettings.productName = "The Astraea Program";
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(dir, ExeName),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            string msg = $"{s.result}: {s.totalErrors} errors, {s.totalWarnings} warnings, {s.totalSize / (1024 * 1024)} MB, {s.totalTime.TotalSeconds:F0} s -> {options.locationPathName}";
            Debug.Log("[Build] " + msg);
            return msg;
        }

        /// <summary>Entry point for batch mode: Unity -batchmode -executeMethod TAP.EditorTools.BuildScript.BuildWindowsBatch -quit</summary>
        public static void BuildWindowsBatch()
        {
            string r = BuildWindows();
            if (!r.StartsWith("Succeeded", StringComparison.Ordinal)) EditorApplication.Exit(1);
        }
    }
}
