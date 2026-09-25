using System;
using TAP.Core;
using UnityEngine;

namespace TAP.Game
{
    /// <summary>Builds the assembly building scene (hangar, camera, editor) and keeps the engineer's report current.</summary>
    public sealed class AssemblyScene : MonoBehaviour
    {
        public static AssemblyScene Instance { get; private set; }
        public AssemblyEditor Editor;
        public EditorCamera CameraRig;
        public EditorEnvironment Environment = EditorEnvironment.TellusSeaLevel;
        public DesignStats Stats { get; private set; }
        /// <summary>Raised after the engineer's report was recomputed.</summary>
        public event Action StatsUpdated;

        private EditorVisuals.CraftMarkers _markers;
        private bool _dirty = true;

        public static AssemblyScene Build()
        {
            Layers.ConfigureCollisionMatrix();
            Time.timeScale = 1f;
            var go = new GameObject("AssemblyScene");
            var scene = go.AddComponent<AssemblyScene>();
            Instance = scene;
            EditorVisuals.BuildHangar();
            scene.CameraRig = EditorCamera.Create();
            scene.Editor = go.AddComponent<AssemblyEditor>();
            scene.Editor.DesignChanged += () => scene._dirty = true;
            scene.Editor.Init(scene.CameraRig.Cam, scene.CameraRig);
            scene._markers = new EditorVisuals.CraftMarkers();
            return scene;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SetEnvironment(EditorEnvironment env)
        {
            Environment = env;
            _dirty = true;
        }

        public void MarkDirty() => _dirty = true;

        private void LateUpdate()
        {
            if (_dirty && Editor != null)
            {
                _dirty = false;
                try
                {
                    Stats = DesignAnalysis.Analyze(Editor.Design, Editor.Db, Environment);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    Stats = new DesignStats();
                }
                StatsUpdated?.Invoke();
            }
            if (_markers != null && Editor != null) _markers.Update(Stats, Editor.CraftRoot, Editor.ShowMarkers);
        }

        /// <summary>Launches the current design. Returns an error message if the design can't fly.</summary>
        public string TryLaunch()
        {
            if (Editor.IsHolding) Editor.PutBackHeld();
            var stats = DesignAnalysis.Analyze(Editor.Design, Editor.Db, Environment);
            foreach (var w in stats.Warnings) if (w.Blocking) return w.Text;
            Editor.StoreSessionCraft();
            GameSession.Log($"{Editor.Design.name} launched from the assembly building");
            GameSession.Launch(Editor.Design);
            return null;
        }
    }
}
