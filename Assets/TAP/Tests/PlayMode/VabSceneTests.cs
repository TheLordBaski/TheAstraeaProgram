using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TAP.Game;
using TAP.Persistence;
using TAP.Simulation;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

namespace TAP.Tests
{
    public class VabSceneTests
    {
        [UnityTest]
        public IEnumerator LaunchSiteVab_IsAnAuthoredKinematicSurfaceObject()
        {
            var host = new GameObject("VAB launch-site test");
            try
            {
                var sim = host.AddComponent<FlightSim>();
                var planets = host.AddComponent<PlanetManager>();
                LaunchSiteBuilder.Build(sim,planets);
                yield return null;
                var vab = planets.SurfaceObjects.Single(s => s.Go.name == "AssemblyBuilding");
                Assert.IsTrue(vab.Rb.isKinematic);
                Assert.That(vab.Go.GetComponentsInChildren<Renderer>().Length, Is.GreaterThan(80));
                Assert.That(vab.Go.GetComponentsInChildren<Collider>().Length, Is.EqualTo(138));
                Assert.IsTrue(vab.Go.GetComponentsInChildren<Light>().All(l => !l.enabled));
            }
            finally { Object.Destroy(host); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator HubAndAssembly_LoadTheAuthoredVab_WithAnEditableStarterRocket()
        {
            var existing = SceneManager.GetActiveScene().GetRootGameObjects();
            var previousCraft = GameSession.EditorCraft;
            var previousSave = GameSession.Save;
            try
            {
                // Match the real scene bootstrap and keep the test's game state in memory.
                GameSession.NewGame("VAB integration test");
                var hub = SpaceCenterScene.Build();
                yield return null;
                var facility = hub.Facilities.Single(f => f.Id == SpaceCenterScene.Assembly);
                Assert.That(facility.GetComponentsInChildren<Renderer>().Length, Is.GreaterThan(80));
                Assert.IsTrue(facility.GetComponentsInChildren<Light>().All(l => !l.enabled));
                Physics.SyncTransforms();
                Assert.IsTrue(Physics.Raycast(new Ray(facility.Anchor,Vector3.down),out var hit,100));
                Assert.AreSame(facility,hit.collider.GetComponentInParent<Facility>());
                Object.Destroy(hub.gameObject);
                yield return null;

                GameSession.EditorCraft = SaveStorage.FromJson<CraftDesign>(Resources.Load<TextAsset>("Craft/Skylark_Suborbital").text);
                var assembly = AssemblyScene.Build();
                yield return null;
                Assert.That(assembly.Editor.PartObjects.Count, Is.GreaterThan(0));
                Assert.That(GameObject.Find("AssemblyBuilding").GetComponentsInChildren<Renderer>().Length, Is.GreaterThan(80));
                Assert.That(assembly.CameraRig.AssemblyBounds.size.y, Is.EqualTo(67.9f).Within(.001f));
                Assert.That(assembly.Editor.CraftBounds().min.y, Is.GreaterThanOrEqualTo(0));
                foreach (var part in assembly.Editor.PartObjects)
                    Assert.IsNotNull(part.GetComponent<EditorPartTag>(), "Stored rocket props must not replace editable craft parts");
                if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
                    Capture(assembly.CameraRig.Cam);
            }
            finally
            {
                GameSession.EditorCraft = previousCraft;
                GameSession.Save = previousSave;
                foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
                    if (!existing.Contains(go)) Object.Destroy(go);
            }
            yield return null;
        }

        static void Capture(Camera camera)
        {
            var target = new RenderTexture(1600,900,24);
            var pixels = new Texture2D(1600,900,TextureFormat.RGBA32,false);
            var previous = RenderTexture.active;
            try
            {
                RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest { destination=target });
                RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,1600,900),0,0);
                pixels.Apply();
                var output=Path.GetFullPath(Path.Combine(Application.dataPath,"../Art/SpaceCenter/Previews/VAB_InGame.png"));
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                File.WriteAllBytes(output,pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active=previous;
                Object.Destroy(target);
                Object.Destroy(pixels);
            }
        }
    }
}
