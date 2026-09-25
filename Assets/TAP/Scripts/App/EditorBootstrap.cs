using TAP.Game;
using UnityEngine;

namespace TAP.App
{
    /// <summary>Entry point of the assembly (vehicle editor) scene.</summary>
    public sealed class EditorBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            CommandLine.Apply();
            GameSession.EnsureGame();
            PostFx.Create();
            var scene = AssemblyScene.Build();
            TAP.UI.EditorUI.Create(scene);
        }
    }
}
