using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using TAP.Core;
using TAP.Persistence;
using UnityEngine;

namespace TAP.Game
{
    /// <summary>
    /// <i>Report a problem</i> (FND-12): one zip in <c>Reports/</c> in the user folder with what is needed to reproduce a bug: the logs,
    /// the save as it is now, a screenshot, the settings and controls, the mods and the machine. Nothing is sent; the player attaches
    /// the file to a report on the feedback page.
    /// </summary>
    public static class BugReport
    {
        public static string Folder => Path.Combine(SaveStorage.Root, "Reports");

        /// <summary>Builds the report and returns the path of the zip. A part that cannot be read is left out and said so in report.txt.</summary>
        public static string Create(string note, byte[] screenshotPng = null)
        {
            GameLog.Flush();
            Directory.CreateDirectory(Folder);
            string path = Path.Combine(Folder, $"report-{DateTime.Now:yyyyMMdd-HHmmss}.zip");
            var problems = new StringBuilder();
            using (var zip = new ZipArchive(new FileStream(path, FileMode.Create, FileAccess.Write), ZipArchiveMode.Create))
            {
                void Text(string name, Func<string> make)
                {
                    try { Add(zip, name, Encoding.UTF8.GetBytes(make())); }
                    catch (Exception e) { problems.AppendLine($"{name}: {e.Message}"); }
                }
                void CopyFile(string name, string file)
                {
                    try { if (File.Exists(file)) Add(zip, name, ReadShared(file)); }
                    catch (Exception e) { problems.AppendLine($"{name}: {e.Message}"); }
                }

                if (Directory.Exists(GameLog.Folder))
                    foreach (var f in Directory.GetFiles(GameLog.Folder, "tap-*.log")) CopyFile("logs/" + Path.GetFileName(f), f);
                CopyFile("logs/" + Path.GetFileName(Application.consoleLogPath ?? "Player.log"), Application.consoleLogPath);

                // The game as it is this moment (in flight a capture of every vessel), and the files on disk.
                Text("save/current-state.json", () => SaveStorage.ToJson(GameSession.LiveSaveProvider?.Invoke() ?? GameSession.Save));
                var save = GameSession.Save;
                if (save != null)
                    foreach (var slot in new[] { "persistent", "quicksave" })
                        CopyFile($"save/{slot}.json", SaveStorage.SlotPath(save.saveName, slot));
                CopyFile("settings.json", GameSettings.FilePath);
                CopyFile("controls.json", Controls.FilePath);
                if (screenshotPng != null) Add(zip, "screenshot.png", screenshotPng);
                Text("mods.txt", Mods);
                Text("system.txt", GameLog.SystemDescription);
                Text("report.txt", () => Summary(note, problems.ToString()));
            }
            return path;
        }

        private static string Summary(string note, string problems)
        {
            var sb = new StringBuilder();
            sb.AppendLine("The Astraea Program: problem report");
            sb.AppendLine($"Made {DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}");
            sb.AppendLine(GameLog.SystemDescription());
            sb.AppendLine($"Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            var s = GameSession.Save;
            if (s != null) sb.AppendLine($"Save: {s.saveName} ({s.mode}), {MathD.FormatUT(s.ut)}, {s.vessels.Count} vessel(s), format {s.version}");
            sb.AppendLine();
            sb.AppendLine("What the player wrote:");
            sb.AppendLine(string.IsNullOrWhiteSpace(note) ? "(nothing)" : note.Trim());
            sb.AppendLine();
            sb.AppendLine($"Unhandled exceptions this run: {GameLog.Exceptions.Count}");
            foreach (var e in GameLog.Exceptions) sb.AppendLine($"  {e.When:HH:mm:ss} {e.Message.Split('\n')[0]}");
            var content = ContentLog.Session;
            if (content != null && !content.IsClean) sb.AppendLine($"Game data problems: {content.Counts}");
            if (problems.Length > 0) { sb.AppendLine(); sb.AppendLine("Left out of this report:"); sb.Append(problems); }
            return sb.ToString();
        }

        /// <summary>The folders and files in the user folder's Mods directory (the mod system arrives with MOD-01).</summary>
        private static string Mods()
        {
            string dir = Path.Combine(SaveStorage.Root, "Mods");
            if (!Directory.Exists(dir)) return "No mods are installed.";
            var entries = Directory.GetFileSystemEntries(dir).Select(Path.GetFileName).OrderBy(n => n).ToList();
            return entries.Count == 0 ? "No mods are installed." : string.Join("\n", entries);
        }

        private static void Add(ZipArchive zip, string name, byte[] data)
        {
            var entry = zip.CreateEntry(name, System.IO.Compression.CompressionLevel.Optimal);
            using (var s = entry.Open()) s.Write(data, 0, data.Length);
        }

        /// <summary>A file the game may still be writing (the log): read without taking it from the writer.</summary>
        private static byte[] ReadShared(string file)
        {
            using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var ms = new MemoryStream())
            {
                fs.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}
