using System.IO;
using TAP.Construction;
using TAP.Parts;
using TAP.Persistence;
using UnityEditor;
using UnityEngine;

namespace TAP.EditorTools
{
    public static class StarterCraftWriter
    {
        [MenuItem("TAP/Generate Starter Craft")]
        public static void Generate()
        {
            var db = PartDatabase.LoadFromResources();
            string dir = AssetBuilder.Res + "/Craft";
            Directory.CreateDirectory(dir);
            foreach (var c in StarterCraft.All(db))
            {
                string path = $"{dir}/{SaveStorage.SanitizeFileName(c.name).Replace(' ', '_')}.json";
                File.WriteAllText(path, SaveStorage.ToJson(c));
                AssetDatabase.ImportAsset(path);
                var a = new CraftAssembler(c, db);
                Debug.Log($"[StarterCraft] {c.name}: {c.parts.Count} parts, {a.TotalMass():F0} kg -> {path}");
            }
        }
    }
}
