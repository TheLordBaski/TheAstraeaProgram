using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TAP.EditorTools
{
    /// <summary>The development scene has no gameplay bootstrap and is never a player scene.</summary>
    public static class BodyLabScene
    {
        public const string Path = "Assets/TAP/Editor/Scenes/CelestialBodyLab.unity";

        public static Scene Open()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Stop Play mode before opening the body lab.");
            var existing = SceneManager.GetSceneByPath(Path);
            if (existing.IsValid() && existing.isLoaded) { SceneManager.SetActiveScene(existing); return existing; }
            if (!File.Exists(Path))
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                EditorSceneManager.SaveScene(scene, Path);
                SceneManager.SetActiveScene(scene);
                return scene;
            }
            var loaded = EditorSceneManager.OpenScene(Path, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(loaded);
            return loaded;
        }

        public static void Frame(bool patch)
        {
            var view = SceneView.lastActiveSceneView ?? EditorWindow.GetWindow<SceneView>();
            view.LookAt(Vector3.zero, Quaternion.Euler(patch ? 45 : 15, patch ? 0 : 110, 0), patch ? 12 : 30);
            view.Repaint();
        }
    }

    /// <summary>Fails early if the development scene was accidentally enabled in a player build.</summary>
    public sealed class BodyLabBuildGuard : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report)
        {
            foreach (var scene in EditorBuildSettings.scenes)
                if (scene.enabled && scene.path == BodyLabScene.Path)
                    throw new BuildFailedException("CelestialBodyLab is an editor-only development scene. Remove it from the player scene list.");
        }
    }
}
