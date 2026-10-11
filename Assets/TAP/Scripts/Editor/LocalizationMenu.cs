using TAP.Game;
using UnityEditor;
using UnityEngine;

namespace TAP.EditorTools
{
    /// <summary>TAP → Localization → Update English Table: lists the texts of the screens and the game data in Strings/en.json.</summary>
    public static class LocalizationMenu
    {
        [MenuItem("TAP/Localization/Update English Table")]
        public static void UpdateEnglish()
        {
            int n = LocalizationCatalog.Write(null);
            AssetDatabase.Refresh();
            Debug.Log($"[Localization] Strings/en.json now lists {n} texts.");
        }
    }
}
