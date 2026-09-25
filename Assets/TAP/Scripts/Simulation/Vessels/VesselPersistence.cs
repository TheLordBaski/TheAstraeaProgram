using System.Collections.Generic;
using TAP.Core;
using TAP.Persistence;
using UnityEngine;

namespace TAP.Simulation
{
    public sealed partial class Vessel
    {
        /// <summary>Builds this vessel around already-existing part GameObjects (after a split).</summary>
        public void InitializeLive(List<Part> parts, Part root)
        {
            Resources = new ResourceSystem(this);
            Rb = gameObject.AddComponent<Rigidbody>();
            ConfigureRigidbody();
            RootPart = root;
            foreach (var p in parts)
            {
                p.transform.SetParent(transform, true);
                p.Vessel = this;
                Parts.Add(p);
            }
            root.AttachNode = null;
            root.ParentNode = null;
            UpdateMassProperties();
            foreach (var p in Parts) foreach (var m in p.Modules) m.OnVesselChanged();
        }

        /// <summary>Writes the complete part/module/control state into <see cref="Record"/>.</summary>
        public void WriteRecord(double ut)
        {
            var rec = Record;
            rec.parts.Clear();
            var index = new Dictionary<Part, int>();
            // Parents before children: breadth-first from the root.
            var order = new List<Part>();
            if (RootPart != null)
            {
                var q = new Queue<Part>();
                q.Enqueue(RootPart);
                while (q.Count > 0)
                {
                    var p = q.Dequeue();
                    order.Add(p);
                    foreach (var c in p.Children) q.Enqueue(c);
                }
            }
            for (int i = 0; i < order.Count; i++) index[order[i]] = i;
            Transform rt = RootPart != null ? RootPart.transform : transform;
            foreach (var p in order)
            {
                var pr = new PartRecord
                {
                    uid = p.Uid,
                    partId = p.Def.id,
                    parent = p.ParentPart != null && index.TryGetValue(p.ParentPart, out int pi) ? pi : -1,
                    parentNode = p.ParentNode,
                    attachNode = p.AttachNode,
                    pos = FromV3(rt.InverseTransformPoint(p.transform.position)),
                    rot = FromQ(Quaternion.Inverse(rt.rotation) * p.transform.rotation),
                    stage = p.Stage,
                    symmetryGroup = p.SymmetryGroup,
                    skinTemp = p.SkinTemp,
                    internalTemp = p.InternalTemp,
                    settings = p.Settings != null && p.Settings.Count > 0 ? new Dictionary<string, double>(p.Settings) : null,
                };
                foreach (var r in p.Resources) pr.resources.Add(new ResourceRecord { id = r.Def.id, amount = r.Amount, max = r.Max });
                foreach (var m in p.Modules)
                {
                    var st = new Dictionary<string, string>();
                    m.Save(st);
                    if (st.Count > 0) pr.modules[m.ModuleName] = st;
                }
                pr.crew.AddRange(p.Crew);
                rec.parts.Add(pr);
            }
            rec.rootIndex = 0;
            rec.currentStage = CurrentStage;
            rec.control = Ctrl.ToRecord();
            rec.rotation = FromQ(rt.rotation);
            rec.angularVelocity = Rb != null ? FromV3(Rb.angularVelocity) : new float[] { 0, 0, 0 };
            rec.comOffset = Rb != null ? FromV3(Quaternion.Inverse(rt.rotation) * (Rb.worldCenterOfMass - rt.position)) : new float[] { 0, 0, 0 };
            rec.bodyId = Sim != null ? Sim.Frame.Body.Id : rec.bodyId;
        }
    }
}
