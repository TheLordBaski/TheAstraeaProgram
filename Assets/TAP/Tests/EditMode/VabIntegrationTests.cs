using System.Linq;
using NUnit.Framework;
using TAP.Core;
using TAP.Game;
using UnityEngine;

namespace TAP.Tests
{
    public class VabIntegrationTests
    {
        [Test]
        public void AssemblyVab_LoadsAtRocketOrigin_WithClearPhysicalWorkspace()
        {
            var root = EditorVisuals.BuildHangar(out var bounds);
            try
            {
                var origin = root.GetComponentsInChildren<Transform>().Single(t => t.name == "RocketAssemblyOrigin");
                Assert.That(origin.position.sqrMagnitude, Is.LessThan(1e-6f));
                Assert.That(bounds.size.x, Is.EqualTo(24).Within(.001f));
                Assert.That(bounds.size.y, Is.EqualTo(67.9f).Within(.001f));
                Assert.That(bounds.size.z, Is.EqualTo(30).Within(.001f));
                Assert.That(root.GetComponentsInChildren<Renderer>().Length, Is.GreaterThan(80));
                Assert.IsNull(root.GetComponentInChildren<EditorPartTag>());
                Assert.IsTrue(root.GetComponentsInChildren<Collider>().All(c => c.gameObject.layer == Layers.Terrain));
                Physics.SyncTransforms();
                var hits = Physics.OverlapBox(bounds.center, bounds.extents - Vector3.one * .01f)
                    .Where(c => c.transform.IsChildOf(root.transform));
                Assert.IsEmpty(hits, "The centre must contain no building collider");
                var floor = root.GetComponentsInChildren<BoxCollider>().Single(c => c.name == "High bay floor");
                Assert.IsTrue(floor.Raycast(new Ray(Vector3.up, Vector3.down), out var hit, 2));
                Assert.That(hit.point.y, Is.EqualTo(0).Within(.001f));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase(6f, false)]
        [TestCase(60f, true)]
        public void FrameCraft_KeepsTheRocketInFrontOfTheNearPlane(float height, bool outside)
        {
            var rig = EditorCamera.Create();
            try
            {
                rig.AssemblyBounds = new Bounds(new Vector3(0,34.04f,0),new Vector3(24,67.9f,30));
                var craft = new Bounds(new Vector3(0,height/2,0),new Vector3(6,height,6));
                rig.Frame(craft, true);
                Assert.That(rig.Cam.nearClipPlane > .1f, Is.EqualTo(outside));
                foreach (int x in new[] {-1,1})
                    foreach (int y in new[] {-1,1})
                        foreach (int z in new[] {-1,1})
                        {
                            var point = craft.center + Vector3.Scale(craft.extents,new Vector3(x,y,z));
                            Assert.That(rig.Cam.WorldToViewportPoint(point).z, Is.GreaterThan(rig.Cam.nearClipPlane),
                                "Camera cutaway must not clip the rocket");
                        }
            }
            finally { Object.DestroyImmediate(rig.gameObject); }
        }

    }
}
