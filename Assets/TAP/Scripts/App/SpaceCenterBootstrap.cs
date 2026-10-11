using TAP.Game;
using UnityEngine;

namespace TAP.App
{
    /// <summary>Entry point of the space center scene.</summary>
    public sealed class SpaceCenterBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            CommandLine.Apply();
            // Started straight from this scene in the editor: a new game, so the hub has a save to show.
            GameSession.EnsureGame();
            PostFx.Create();
            var scene = SpaceCenterScene.Build();
            TAP.UI.SpaceCenterUI.Create(scene);
        }
    }
}
