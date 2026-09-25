using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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

        public static string Root
        {
            get
            {
                if (RootOverride != null) return RootOverride;
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

        public static void WriteSave(GameSave save, string slot)
        {
            Directory.CreateDirectory(SaveFolder(save.saveName));
            save.savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string path = SlotPath(save.saveName, slot);
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, ToJson(save));
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }

        public static bool SlotExists(string saveName, string slot) => File.Exists(SlotPath(saveName, slot));

        public static GameSave ReadSave(string saveName, string slot)
        {
            string path = SlotPath(saveName, slot);
            if (!File.Exists(path)) return null;
            return FromJson<GameSave>(File.ReadAllText(path));
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
