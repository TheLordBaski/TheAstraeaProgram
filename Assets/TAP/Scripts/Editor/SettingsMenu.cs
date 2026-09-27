using TAP.Game;
using UnityEditor;
using UnityEngine;

namespace TAP.EditorTools
{
    /// <summary>Menu entries that open the game's settings assets in the inspector.</summary>
    public static class SettingsMenu
    {
        public const string AssemblyBuildingPath = AssetBuilder.Res + "/" + AssemblyBuildingSettings.ResourcePath + ".asset";

        /// <summary>Selects the assembly building settings (height limit), creating the asset with the defaults if it is missing.</summary>
        [MenuItem("TAP/Assembly Building Settings")]
        public static void SelectAssemblyBuilding()
        {
            var s = AssetDatabase.LoadAssetAtPath<AssemblyBuildingSettings>(AssemblyBuildingPath);
            if (s == null)
            {
                string dir = System.IO.Path.GetDirectoryName(AssemblyBuildingPath).Replace('\\', '/');
                if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder(AssetBuilder.Res, System.IO.Path.GetFileName(dir));
                s = ScriptableObject.CreateInstance<AssemblyBuildingSettings>();
                AssetDatabase.CreateAsset(s, AssemblyBuildingPath);
                AssetDatabase.SaveAssets();
            }
            Selection.activeObject = s;
            EditorGUIUtility.PingObject(s);
        }
    }
}
