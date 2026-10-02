using System.Linq;
using TAP.Game;
using UnityEditor;
using UnityEngine;

namespace TAP.EditorTools
{
    /// <summary>Content checks in the editor: TAP → Validate Content, and the file refresh F8 hot reload needs.</summary>
    [InitializeOnLoad]
    public static class ContentTools
    {
        static ContentTools()
        {
            // F8 in play mode reads the data files again; edits on disk reach Resources only through a reimport.
            ContentReload.RefreshFiles = ImportData;
        }

        /// <summary>Reimports the data and starter craft files, so Resources sees edits made outside Unity.</summary>
        public static void ImportData()
        {
            foreach (var folder in new[] { "Assets/TAP/Resources/Data", "Assets/TAP/Resources/Craft" })
                AssetDatabase.ImportAsset(folder, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ImportRecursive);
        }

        [MenuItem("TAP/Validate Content")]
        public static void ValidateMenu()
        {
            var r = Validate();
            const int shown = 12;
            string text = r.Report.IsClean
                ? "All content is valid:\n" + r.Counts + "."
                : r.Counts + ".\n\n" + r.Report.Counts + ":\n\n" +
                  string.Join("\n\n", r.Report.Problems.Take(shown).Select(p => (p.IsError ? "Error: " : "Warning: ") + p)) +
                  (r.Report.Problems.Count > shown ? $"\n\n…and {r.Report.Problems.Count - shown} more." : "") +
                  "\n\nThe console lists every problem.";
            EditorUtility.DisplayDialog("Validate Content", text, "OK");
        }

        /// <summary>Checks all content after reimporting it, and logs each problem (or one line when it is clean).</summary>
        public static ContentCheck.Result Validate()
        {
            ImportData();
            var r = ContentCheck.All();
            foreach (var p in r.Report.Errors) Debug.LogError("Content error: " + p);
            foreach (var p in r.Report.Warnings) Debug.LogWarning("Content warning: " + p);
            Debug.Log(r.Report.IsClean ? $"Content is valid: {r.Counts}." : $"Content checked ({r.Counts}): {r.Report.Counts}.");
            return r;
        }
    }
}
