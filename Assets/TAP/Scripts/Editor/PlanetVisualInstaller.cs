using TAP.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace TAP.Editor
{
    public static class PlanetVisualInstaller
    {
        [MenuItem("TAP/Planet visuals/Install renderer")]
        public static void Install()
        {
            var shader=Shader.Find("Hidden/TAP/PlanetAtmosphereCloud");
            if(shader==null) throw new System.InvalidOperationException("Planet rendering shader not imported.");
            foreach(string path in new[] { "Assets/Settings/PC_Renderer.asset", "Assets/Settings/Mobile_Renderer.asset" }) {
                var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
                PlanetAtmosphereCloudFeature feature=null;
                foreach(var f in renderer.rendererFeatures) if(f is PlanetAtmosphereCloudFeature p) feature=p;
                if(feature==null) {
                    feature=ScriptableObject.CreateInstance<PlanetAtmosphereCloudFeature>();feature.name="Planet atmosphere and clouds";
                    AssetDatabase.AddObjectToAsset(feature,renderer);renderer.rendererFeatures.Add(feature);
                }
                feature.Shader=shader;feature.SetActive(true);feature.Create();EditorUtility.SetDirty(feature);renderer.SetDirty();EditorUtility.SetDirty(renderer);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
