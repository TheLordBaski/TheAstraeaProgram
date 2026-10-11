using System.IO;
using System.Linq;
using NUnit.Framework;
using TAP.Game;
using UnityEditor;

namespace TAP.Tests
{
    public class SceneFlowTests
    {
        [Test]
        public void EveryScene_IsInTheBuild()
        {
            var inBuild = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => Path.GetFileNameWithoutExtension(s.path)).ToList();
            foreach (var scene in new[] { GameSession.MenuScene, GameSession.HubScene, GameSession.EditorScene, GameSession.FlightScene })
                Assert.Contains(scene, inBuild, scene + " is not in the build settings");
        }
    }
}
