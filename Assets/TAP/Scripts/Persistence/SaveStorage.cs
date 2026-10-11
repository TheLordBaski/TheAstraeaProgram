using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace TAP.Persistence
{
    /// <summary>JSON serialization and file locations for craft files and game saves.</summary>
    public static class SaveStorage
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            Converters = { new StringEnumConverter() },
            NullValueHandling = NullValueHandling.Ignore,
            FloatFormatHandling = FloatFormatHandling.DefaultValue,
        };

        /// <summary>Root folder for user data. Overridable for tests.</summary>
        public static string RootOverride;

        /// <summary>
        /// Editor setting (PlayerPrefs) for test play: user data goes to this folder instead. Unlike
        /// <see cref="RootOverride"/> it lasts through entering play mode, so a test never writes over a real save.
        /// </summary>
        public const string EditorRootPref = "TAP.SaveRoot";

        public static string Root
        {
            get
            {
                if (RootOverride != null) return RootOverride;
                if (Application.isEditor)
                {
                    string test = PlayerPrefs.GetString(EditorRootPref, "");
                    if (test.Length > 0) return test;
                }
                string root = Path.Combine(Application.persistentDataPath, "TAP");
                if (!_legacyChecked) { _legacyChecked = true; CopyLegacyData(root); }
                return root;
            }
        }

        private static bool _legacyChecked;

        /// <summary>
        /// First run after the rename from Starwright: copies ships and saves from the old folder
        /// (LocalLow/Starwright Works/Starwright/Starwright) so nothing is lost. The old folder is left untouched.
        /// </summary>
        private static void CopyLegacyData(string root)
        {
            try
            {
                if (Directory.Exists(root)) return;
                var company = Directory.GetParent(Application.persistentDataPath)?.Parent;
                if (company == null) return;
                string legacy = Path.Combine(company.FullName, "Starwright Works", "Starwright", "Starwright");
                if (!Directory.Exists(legacy)) return;
                foreach (string file in Directory.GetFiles(legacy, "*", SearchOption.AllDirectories))
                {
                    string target = Path.Combine(root, Path.GetRelativePath(legacy, file));
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    File.Copy(file, target, false);
                }
                Debug.Log($"Copied ships and saves from the Starwright folder {legacy}");
            }
            catch (Exception e) { Debug.LogWarning($"Could not copy ships and saves from the Starwright folder: {e.Message}"); }
        }
        public static string ShipsFolder => Path.Combine(Root, "Ships");
        public static string SavesFolder => Path.Combine(Root, "Saves");

        public static string ToJson<T>(T obj) => JsonConvert.SerializeObject(obj, Settings);
        public static T FromJson<T>(string json) => JsonConvert.DeserializeObject<T>(json, Settings);

        public static T DeepClone<T>(T obj) => FromJson<T>(ToJson(obj));

        public static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) name = "Untitled";
            foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return name.Trim();
        }

        // ------------------------------------------------------------------ craft

        public static string CraftPath(string craftName) => Path.Combine(ShipsFolder, SanitizeFileName(craftName) + ".craft.json");

        public static void SaveCraft(CraftDesign design)
        {
            Directory.CreateDirectory(ShipsFolder);
            File.WriteAllText(CraftPath(design.name), ToJson(design));
        }

        public static CraftDesign LoadCraft(string path)
        {
            return FromJson<CraftDesign>(File.ReadAllText(path));
        }

        public static List<string> ListCraftFiles()
        {
            var list = new List<string>();
            if (Directory.Exists(ShipsFolder))
                list.AddRange(Directory.GetFiles(ShipsFolder, "*.craft.json"));
            list.Sort(StringComparer.OrdinalIgnoreCase);
            return list;
        }

        public static bool DeleteCraft(string path)
        {
            if (!File.Exists(path)) return false;
            File.Delete(path);
            return true;
        }

        /// <summary>Built-in starter craft shipped in Resources/Craft.</summary>
        public static List<CraftDesign> LoadStarterCraft()
        {
            var result = new List<CraftDesign>();
            foreach (var ta in Resources.LoadAll<TextAsset>("Craft"))
            {
                try { result.Add(FromJson<CraftDesign>(ta.text)); }
                catch (Exception e) { Debug.LogError($"Starter craft {ta.name} failed to load: {e.Message}"); }
            }
            result.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
            return result;
        }

        // ------------------------------------------------------------------ game saves

        public static string SaveFolder(string saveName) => Path.Combine(SavesFolder, SanitizeFileName(saveName));

        public static string SlotPath(string saveName, string slot) => Path.Combine(SaveFolder(saveName), slot + ".json");

        /// <summary>How many earlier "persistent" saves are kept in the save's Backups folder.</summary>
        public const int KeptBackups = 5;

        /// <summary>Autosave interval in minutes while flying; 0 turns it off. The settings screen (FND-08) binds to this.</summary>
        public static int AutosaveMinutes
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt("TAP.AutosaveMinutes", 5), 0, 60);
            set { PlayerPrefs.SetInt("TAP.AutosaveMinutes", Mathf.Clamp(value, 0, 60)); PlayerPrefs.Save(); }
        }

        public static string BackupFolder(string saveName) => Path.Combine(SaveFolder(saveName), "Backups");

        public static void WriteSave(GameSave save, string slot)
        {
            Directory.CreateDirectory(SaveFolder(save.saveName));
            save.savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string path = SlotPath(save.saveName, slot);
            if (slot == "persistent") BackUp(path, save.saveName);
            WriteAtomic(path, ToJson(save));
        }

        /// <summary>
        /// Writes a temporary file, flushes it to disk, then swaps it in. A crash leaves the old file or the new one,
        /// never half of either.
        /// </summary>
        public static void WriteAtomic(string path, string text)
        {
            string tmp = path + ".tmp";
            var bytes = new System.Text.UTF8Encoding(false).GetBytes(text);
            using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write))
            {
                fs.Write(bytes, 0, bytes.Length);
                fs.Flush(true);
            }
            if (File.Exists(path)) File.Replace(tmp, path, null);
            else File.Move(tmp, path);
        }

        /// <summary>Copies the save about to be replaced into Backups, dropping the oldest beyond <see cref="KeptBackups"/>.</summary>
        private static void BackUp(string path, string saveName)
        {
            try
            {
                // A damaged file is never rotated in: it would push out a good backup.
                if (!File.Exists(path) || !TryParseSave(File.ReadAllText(path), out _)) return;
                string dir = BackupFolder(saveName);
                Directory.CreateDirectory(dir);
                File.Copy(path, Path.Combine(dir, "persistent-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + ".json"), true);
                var all = Backups(saveName);   // newest first
                for (int i = KeptBackups; i < all.Count; i++) File.Delete(all[i]);
            }
            catch (Exception e) { Debug.LogWarning($"Could not back up save '{saveName}': {e.Message}"); }
        }

        /// <summary>Backups of a save, newest first (the names sort by time).</summary>
        public static List<string> Backups(string saveName)
        {
            var list = new List<string>();
            string dir = BackupFolder(saveName);
            if (Directory.Exists(dir)) list.AddRange(Directory.GetFiles(dir, "persistent-*.json"));
            list.Sort(StringComparer.Ordinal);
            list.Reverse();
            return list;
        }

        public static bool SlotExists(string saveName, string slot) => File.Exists(SlotPath(saveName, slot));

        /// <summary>Parses save JSON of any format version, upgrading it to the current one.</summary>
        public static GameSave ParseSave(string json)
        {
            JObject o;
            using (var reader = new JsonTextReader(new StringReader(json)) { DateParseHandling = DateParseHandling.None })
                o = JObject.Load(reader);
            return SaveMigration.Upgrade(o).ToObject<GameSave>(JsonSerializer.Create(Settings));
        }

        private static bool TryParseSave(string json, out GameSave save)
        {
            try { save = ParseSave(json); return save != null; }
            catch (JsonException) { save = null; return false; }
        }

        public static GameSave ReadSave(string saveName, string slot) => ReadSave(saveName, slot, out _);

        /// <summary>
        /// Reads a save slot. A damaged "persistent" file is replaced by the newest good backup and <paramref name="notice"/>
        /// says so; with no good backup this throws <see cref="SaveException"/>. A missing slot gives null.
        /// </summary>
        public static GameSave ReadSave(string saveName, string slot, out string notice)
        {
            notice = null;
            string path = SlotPath(saveName, slot);
            if (!File.Exists(path)) return null;
            string text = File.ReadAllText(path);
            try { return ParseSave(text); }
            catch (SaveTooNewException) { throw; }
            catch (JsonException e)
            {
                if (slot == "persistent")
                    foreach (string backup in Backups(saveName))
                    {
                        if (!TryParseSave(File.ReadAllText(backup), out var good)) continue;
                        File.Copy(path, path + ".damaged", true);   // keep the evidence
                        File.Copy(backup, path, true);
                        notice = $"The save '{saveName}' was damaged. Restored the backup from {File.GetLastWriteTime(backup):yyyy-MM-dd HH:mm}.";
                        Debug.LogWarning(notice + " " + e.Message);
                        return good;
                    }
                throw new SaveException($"The {slot} save of '{saveName}' is damaged and no backup could be read: {e.Message}", e);
            }
        }

        public static List<string> ListSaves()
        {
            var list = new List<string>();
            if (Directory.Exists(SavesFolder))
                foreach (var d in Directory.GetDirectories(SavesFolder)) list.Add(Path.GetFileName(d));
            list.Sort(StringComparer.OrdinalIgnoreCase);
            return list;
        }
    }
}
