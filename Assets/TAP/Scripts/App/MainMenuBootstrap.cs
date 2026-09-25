using TAP.Game;
using UnityEngine;

namespace TAP.App
{
    /// <summary>Entry point of the main menu scene.</summary>
    public sealed class MainMenuBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            CommandLine.Apply();
            if (!string.IsNullOrEmpty(GameSession.AutoTest))
            {
                // Automated verification runs go straight to the flight scene.
                GameSession.NewGame("AutoTest");
                GameSession.Launch(MissionAutopilot.CraftFor(GameSession.AutoTest));
                return;
            }
            Time.timeScale = 1f;
            PostFx.Create();
            TAP.UI.MainMenuUI.Create();
        }
    }
}
