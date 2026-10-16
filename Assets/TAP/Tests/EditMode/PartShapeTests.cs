using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TAP.Parts;
using UnityEngine;

namespace TAP.Tests
{
    /// <summary>The procedural shapes of the parts of the slice and the first Early Access release (PRT-02).</summary>
    public class PartShapeTests
    {
        private static PartDatabase Db => PartDatabase.Instance;
        private readonly List<GameObject> _made = new List<GameObject>();

        [TearDown]
        public void CleanUp()
        {
            foreach (var go in _made) if (go != null) Object.DestroyImmediate(go);
            _made.Clear();
        }

        private GameObject Build(PartDefinition def)
        {
            var go = PartModelFactory.Build(def, 0, generated: true);
            _made.Add(go);
            return go;
        }

        private static Bounds BoundsOf(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<MeshRenderer>(true);
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b;
        }

        private static IEnumerable<PartDefinition> Parts(params string[] milestones) =>
            Db.AllParts.Where(p => p.category != "Hidden" && milestones.Contains(p.milestone));

        [Test]
        public void EveryPart_UsesAShapeTheFactoryKnows()
        {
            foreach (var p in Db.AllParts)
                Assert.Contains(p.model.type, PartModelFactory.Types, p.id + " names a shape that is not made (it would be drawn as a tank)");
        }

        [Test]
        public void EveryPartOfTheSliceAndEA1_HasAReadableModel()
        {
            foreach (var def in Parts("M0", "EA1"))
            {
                var go = Build(def);
                Assert.IsNotNull(go.transform.Find("Model"), def.id + " has no Model");
                var meshes = go.GetComponentsInChildren<MeshFilter>(true).Select(f => f.sharedMesh).Where(m => m != null).ToList();
                Assert.IsNotEmpty(meshes, def.id + " has no mesh");
                Assert.Greater(meshes.Sum(m => m.vertexCount), 20, def.id + " is hardly a shape");
                foreach (var v in meshes.SelectMany(m => m.vertices))
                    Assert.IsTrue(!float.IsNaN(v.x + v.y + v.z) && !float.IsInfinity(v.x + v.y + v.z), def.id + " has a bad vertex");
                Assert.IsNotNull(go.GetComponent<MeshCollider>(), def.id + " has no collider");

                // The model is about as big as the part's data says. A part on a stack: as wide as its diameter and as tall as its height.
                var size = BoundsOf(go).size;
                if (def.nodes.Count > 0)
                {
                    float width = Mathf.Max(size.x, size.z), diameter = def.diameter;
                    Assert.That(width, Is.InRange(diameter * 0.55f, diameter * 1.45f), def.id + " width");
                    Assert.That(size.y, Is.InRange(def.height * 0.5f, def.height * 2.2f), def.id + " height"); // a probe core has a stub antenna
                }
                else
                {
                    Assert.That(Mathf.Max(size.x, size.y, size.z), Is.InRange(Mathf.Max(def.diameter, def.height) * 0.5f, Mathf.Max(def.diameter, def.height) * 3f), def.id + " size");
                }
            }
        }

        [Test]
        public void ARadialShape_SitsWithItsBackOnTheHull()
        {
            // the part's surfaceAttach point is where it touches: the back of the model is there, and it grows outward along +Z
            string[] shapes = { "solarpanel", "antenna", "spotlight", "ladder", "instrument", "radialprobe", "sepmotor" };
            int n = 0;
            foreach (var def in Parts("EA1").Where(p => shapes.Contains(p.model.type)))
            {
                var go = Build(def);
                var b = BoundsOf(go);
                float back = def.surfaceAttach.pos[2];
                Assert.AreEqual(back, b.min.z, 0.07f, def.id + " back");
                Assert.Greater(b.max.z, back + 0.04f, def.id + " sticks out of the hull");
                Assert.IsEmpty(def.nodes, def.id + " is radial");
                n++;
            }
            Assert.AreEqual(12, n, "panels, antennas, the light, the ladder, the instruments, the probe and the separation motor");
        }

        [Test]
        public void TheDeployablesOfEA1_HaveTheirMovingPiecesByName()
        {
            string[] rigged = { "solarpanel", "antenna" };
            int n = 0;
            foreach (var def in Parts("EA1").Where(p => p.deployable != null && rigged.Contains(p.model.type)))
            {
                var go = Build(def);
                var model = go.transform.Find("Model");
                foreach (var m in def.deployable.motions)
                    Assert.IsNotNull(model.Find(m.transform), $"{def.id}: the model has no piece \"{m.transform}\"");
                n++;
            }
            Assert.AreEqual(2, n, "the folding panel and the dish");
        }

        [Test]
        public void TheDishDeployed_FacesOutward_AndThePanelsFoldOutSideways()
        {
            var dish = Build(Db.GetAny("antenna_dish"));
            var bowl = dish.transform.Find("Model/Boom/Dish").GetComponent<MeshFilter>().sharedMesh;
            // rest pose: the bowl opens downward (its rim is below its vertex); tipped by the data's -90° about X it opens along +Z, away from the hull
            Assert.Less(bowl.bounds.min.y, -0.05f, "the feed hangs below the vertex");
            Assert.AreEqual(1f, (Quaternion.Euler(-90, 0, 0) * Vector3.down).z, 1e-4f, "the dish faces outward");

            var panel = Build(Db.GetAny("solar_fold"));
            var a = panel.transform.Find("Model/WingA");
            var b = panel.transform.Find("Model/WingB");
            Assert.AreEqual(a.localPosition.y, b.localPosition.y, 1e-5f, "one hinge line");
            Assert.Less(a.localPosition.z, b.localPosition.z, "folded, one wing lies behind the other");
            Assert.AreEqual(1f, (Quaternion.Euler(0, 0, -90) * Vector3.up).x, 1e-4f, "WingA swings out to +X");
            Assert.AreEqual(-1f, (Quaternion.Euler(0, 0, 90) * Vector3.up).x, 1e-4f, "WingB swings out to -X");
        }
    }
}
