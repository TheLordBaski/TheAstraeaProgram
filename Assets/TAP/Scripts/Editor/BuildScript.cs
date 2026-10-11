using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TAP.EditorTools
{
    /// <summary>
    /// Standalone player builds for Windows and Linux (menu, eval automation, <c>-executeMethod</c>, or CI). The version from
    /// <c>git describe</c> is stamped into the player (<c>Application.version</c>): the main menu and the logs show it.
    /// </summary>
    public static class BuildScript
    {
        public const string OutputDir = "Build/Windows";
        public const string LinuxOutputDir = "Build/Linux";
        public const string ExeName = "TAP.exe";
        public const string LinuxExeName = "TAP.x86_64";

        [MenuItem("TAP/Build Windows Player")]
        public static void BuildWindowsMenu() => EditorUtility.DisplayDialog("TAP build", BuildWindows(), "OK");

        [MenuItem("TAP/Build Linux Player")]
        public static void BuildLinuxMenu() => EditorUtility.DisplayDialog("TAP build", BuildLinux(), "OK");

        /// <summary>Builds the scenes into Build/Windows/TAP.exe and returns a one-line summary.</summary>
        public static string BuildWindows() => Build(BuildTarget.StandaloneWindows64, Path.Combine(OutputDir, ExeName));

        /// <summary>Builds the scenes into Build/Linux/TAP.x86_64 (Mono, as mods need it, D-11) and returns a one-line summary.</summary>
        public static string BuildLinux() => Build(BuildTarget.StandaloneLinux64, Path.Combine(LinuxOutputDir, LinuxExeName));

        /// <summary>Unity -batchmode -executeMethod TAP.EditorTools.BuildScript.BuildWindowsBatch -quit</summary>
        public static void BuildWindowsBatch() => Exit(BuildWindows());

        /// <summary>Unity -batchmode -executeMethod TAP.EditorTools.BuildScript.BuildLinuxBatch -quit</summary>
        public static void BuildLinuxBatch() => Exit(BuildLinux());

        /// <summary>
        /// The build method for GameCI's unity-builder (<c>buildMethod</c>): it passes <c>-customBuildTarget</c> and
        /// <c>-customBuildPath</c>, and the player goes where the action looks for it.
        /// </summary>
        public static void CiBuild()
        {
            string target = Arg("-customBuildTarget") ?? "StandaloneLinux64";
            string path = Arg("-customBuildPath") ?? "Build";
            string name = Arg("-customBuildName") ?? "TAP";
            var buildTarget = (BuildTarget)Enum.Parse(typeof(BuildTarget), target);
            string file = buildTarget == BuildTarget.StandaloneWindows64 ? name + ".exe" : buildTarget == BuildTarget.StandaloneLinux64 ? name + ".x86_64" : name;
            Exit(Build(buildTarget, Path.Combine(path, file)));
        }

        private static void Exit(string result)
        {
            if (!result.StartsWith("Succeeded", StringComparison.Ordinal)) EditorApplication.Exit(1);
        }

        private static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == name) return args[i + 1];
            return null;
        }

        private static string Build(BuildTarget target, string relativeOutput)
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string output = Path.IsPathRooted(relativeOutput) ? relativeOutput : Path.Combine(root, relativeOutput);
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            string oldVersion = PlayerSettings.bundleVersion;
            try
            {
                PlayerSettings.companyName = "Astraea Works";
                PlayerSettings.productName = "The Astraea Program";
                PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
                PlayerSettings.defaultScreenWidth = 1920;
                PlayerSettings.defaultScreenHeight = 1080;
                PlayerSettings.resizableWindow = true;
                PlayerSettings.runInBackground = true;
                // Mono, not IL2CPP: plugin mods are loaded as assemblies (D-11).
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
                PlayerSettings.bundleVersion = Version(root);
                var options = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = output,
                    target = target,
                    targetGroup = BuildPipeline.GetBuildTargetGroup(target),
                    options = BuildOptions.None,
                };
                BuildReport report = BuildPipeline.BuildPlayer(options);
                var s = report.summary;
                string msg = $"{s.result}: {s.totalErrors} errors, {s.totalWarnings} warnings, {s.totalSize / (1024 * 1024)} MB, {s.totalTime.TotalSeconds:F0} s, version {PlayerSettings.bundleVersion} -> {output}";
                UnityEngine.Debug.Log("[Build] " + msg);
                return msg;
            }
            finally
            {
                PlayerSettings.bundleVersion = oldVersion; // the project keeps its own version; only the player gets the stamp
            }
        }

        /// <summary>The version of this checkout: <c>git describe --tags --always --dirty</c>, or "unknown" without git.</summary>
        public static string Version(string root = null)
        {
            try
            {
                var p = new Process
                {
                    StartInfo = new ProcessStartInfo("git", "describe --tags --always --dirty")
                    {
                        WorkingDirectory = root ?? Path.GetFullPath(Path.Combine(Application.dataPath, "..")),
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    },
                };
                p.Start();
                string v = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit(10000);
                return p.ExitCode == 0 && v.Length > 0 ? v : "unknown";
            }
            catch (Exception) { return "unknown"; }
        }
    }
}
