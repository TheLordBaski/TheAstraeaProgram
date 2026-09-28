using System;
using System.Collections.Generic;
using TAP.Core;
using TAP.Parts;
using TAP.Persistence;
using UnityEngine;

namespace TAP.Game
{
    /// <summary>Turns a craft design into a vessel record standing on the launch pad.</summary>
    public static class LaunchService
    {
        /// <summary>
        /// Vessel orientation on the pad, as in KSP: nose up, right side (+X) east, belly (+Z) north. D (yaw right)
        /// tips the rocket east for the gravity turn, W/S pitch it north/south.
        /// </summary>
        public static QuaternionD PadRotationBF(CelestialBody body, LaunchSiteDefinition site) => PadRotationAt(site.UpBF);

        /// <summary>
        /// The pad's frame at a body-fixed up direction: +Y up, +Z north, +X = up × north = east. Built in doubles: turned
        /// about the body's centre, a float frame would be centimetres off at the surface.
        /// </summary>
        public static QuaternionD PadRotationAt(Vector3d up)
        {
            Vector3d y = up.normalized, z = Geo.North(y), x = Vector3d.Cross(y, z).normalized;
            // The rotation whose matrix has the columns x, y, z.
            double m00 = x.x, m10 = x.y, m20 = x.z, m01 = y.x, m11 = y.y, m21 = y.z, m02 = z.x, m12 = z.y, m22 = z.z;
            double tr = m00 + m11 + m22, s;
            if (tr > 0)
            {
                s = Math.Sqrt(tr + 1) * 2;
                return new QuaternionD((m21 - m12) / s, (m02 - m20) / s, (m10 - m01) / s, 0.25 * s);
            }
            if (m00 > m11 && m00 > m22)
            {
                s = Math.Sqrt(1 + m00 - m11 - m22) * 2;
                return new QuaternionD(0.25 * s, (m01 + m10) / s, (m02 + m20) / s, (m21 - m12) / s);
            }
            if (m11 > m22)
            {
                s = Math.Sqrt(1 + m11 - m00 - m22) * 2;
                return new QuaternionD((m01 + m10) / s, 0.25 * s, (m12 + m21) / s, (m02 - m20) / s);
            }
            s = Math.Sqrt(1 + m22 - m00 - m11) * 2;
            return new QuaternionD((m02 + m20) / s, (m12 + m21) / s, 0.25 * s, (m10 - m01) / s);
        }

        /// <summary>
        /// Rockets still waiting on the pad (rolled out, never launched) stand on today's pad. When the pad has moved since
        /// the save (FND-02 moved it 84.6° west), they move with it: turned about the body's centre from the old pad's frame
        /// to the new one, so they keep their height and their attitude to the pad. Returns how many moved.
        /// </summary>
        public static int MoveWaitingRocketsToPad(List<VesselRecord> vessels, CelestialSystem sys)
        {
            var site = sys.Def.launchSite;
            var body = sys.Get(site.body);
            if (body == null) return 0;
            Vector3d pad = PadPositionBF(body, site);
            QuaternionD frame = PadRotationBF(body, site);
            int moved = 0;
            foreach (var v in vessels)
            {
                if (!v.landed || v.landedPos == null || v.landedRot == null || v.launchUT >= 0 || v.situation != Situation.Prelaunch) continue;
                if (v.systemId != sys.Id || v.bodyId != body.Id) continue;
                var p = new Vector3d(v.landedPos[0], v.landedPos[1], v.landedPos[2]);
                if ((p - pad).magnitude < 200) continue;
                var rot = new QuaternionD(v.landedRot[0], v.landedRot[1], v.landedRot[2], v.landedRot[3]);
                var com = v.comOffset != null && v.comOffset.Length == 3 ? new Vector3d(v.comOffset[0], v.comOffset[1], v.comOffset[2]) : Vector3d.zero;
                // The root stands above the old pad's centre.
                Vector3d root = p - rot * com;
                QuaternionD turn = frame * PadRotationAt(root.normalized).Inverse();
                Vector3d np = turn * p;
                QuaternionD nr = turn * rot;
                v.landedPos = new[] { np.x, np.y, np.z };
                v.landedRot = new[] { (float)nr.x, (float)nr.y, (float)nr.z, (float)nr.w };
                moved++;
            }
            return moved;
        }

        /// <summary>Orientation of the launch complex buildings: +Y up, +Z east (the layout the site was designed in).</summary>
        public static QuaternionD SiteRotationBF(CelestialBody body, LaunchSiteDefinition site)
        {
            Vector3d up = site.UpBF;
            Quaternion q = Quaternion.LookRotation((Vector3)Geo.East(up), (Vector3)up);
            return QuaternionD.FromQuaternion(q);
        }

        public static Vector3d PadPositionBF(CelestialBody body, LaunchSiteDefinition site)
        {
            Vector3d up = site.UpBF;
            return up * (body.Radius + site.padAltitude + site.padDeckHeight);
        }

        public static VesselRecord CreateLaunchRecord(CraftDesign design, PartDatabase db, double ut)
        {
            var sys = CelestialSystem.Default;
            var site = sys.Def.launchSite;
            var body = sys.Get(site.body) ?? sys.HomeBody;
            var rec = new VesselRecord
            {
                name = design.name,
                designName = design.name,
                kind = VesselKind.Ship,
                situation = Situation.Prelaunch,
                systemId = sys.Id,
                bodyId = body.Id,
                landed = true,
            };
            int maxStage = -1;
            foreach (var pn in design.parts)
            {
                var def = db.Get(pn.partId);
                var pr = new PartRecord
                {
                    uid = pn.uid, partId = pn.partId, parent = pn.parent, parentNode = pn.parentNode, attachNode = pn.attachNode,
                    pos = pn.pos, rot = pn.rot, stage = pn.stage, symmetryGroup = pn.symmetryGroup,
                    settings = pn.settings != null ? new Dictionary<string, double>(pn.settings) : null,
                    skinTemp = 288, internalTemp = 288,
                };
                if (def != null)
                    foreach (var r in def.resources) pr.resources.Add(new ResourceRecord { id = r.id, amount = r.amount, max = r.Max });
                if (def?.crew != null && def.crew.seats > 0)
                    pr.crew.AddRange(GameSession.TakeAvailableCrew(def.crew.seats, rec.id));
                rec.parts.Add(pr);
                if (pn.stage > maxStage) maxStage = pn.stage;
            }
            rec.currentStage = maxStage;
            rec.control = new ControlRecord { throttle = 0, sas = false };

            // Place the root on the pad; the flight scene lowers/raises it to rest exactly on the deck.
            Vector3d padPos = PadPositionBF(body, site);
            QuaternionD rot = PadRotationBF(body, site);
            // Centre of mass offset from root in root space
            Vector3 com = DesignCenterOfMass(design, db);
            float lowest = DesignLowestPoint(design, db);
            Vector3d upDir = padPos.normalized;
            Vector3d rootPos = padPos + upDir * (-lowest + 0.05);
            Vector3d comBF = rootPos + rot * (Vector3d)com;
            rec.landedPos = new[] { comBF.x, comBF.y, comBF.z };
            rec.landedRot = new[] { (float)rot.x, (float)rot.y, (float)rot.z, (float)rot.w };
            rec.comOffset = new[] { com.x, com.y, com.z };
            rec.launchUT = -1;
            return rec;
        }

        public static Vector3 DesignCenterOfMass(CraftDesign d, PartDatabase db)
        {
            double m = 0; Vector3d s = Vector3d.zero;
            foreach (var p in d.parts)
            {
                var def = db.Get(p.partId);
                if (def == null) continue;
                double pm = def.WetMass(db);
                m += pm;
                s += new Vector3d(p.pos[0], p.pos[1], p.pos[2]) * pm;
            }
            return m > 0 ? (Vector3)(s / m) : Vector3.zero;
        }

        /// <summary>Lowest point of the design along its local -Y axis (root space), from part bounding cylinders.</summary>
        public static float DesignLowestPoint(CraftDesign d, PartDatabase db)
        {
            VerticalExtent(d.parts, db, Quaternion.identity, out float lowest, out _);
            return lowest;
        }

        /// <summary>
        /// Lowest and highest point of parts (poses relative to their root) turned by <paramref name="frame"/>, from part
        /// bounding cylinders; stowed landing legs count as short. Both 0 without parts.
        /// </summary>
        public static void VerticalExtent(List<PartNodeRecord> parts, PartDatabase db, Quaternion frame, out float lowest, out float highest)
        {
            lowest = float.MaxValue;
            highest = float.MinValue;
            foreach (var p in parts)
            {
                var def = db.Get(p.partId);
                if (def == null) continue;
                Vector3 pos = frame * new Vector3(p.pos[0], p.pos[1], p.pos[2]);
                Quaternion q = frame * new Quaternion(p.rot[0], p.rot[1], p.rot[2], p.rot[3]);
                float r = def.diameter * 0.5f, h = def.height * 0.5f;
                if (def.model != null && def.model.type == "leg") { h = 0.3f; }
                // extreme points of the bounding cylinder along world Y
                Vector3 axis = q * Vector3.up;
                float extent = Mathf.Abs(axis.y) * h + Mathf.Sqrt(Mathf.Max(0, 1 - axis.y * axis.y)) * r;
                lowest = Mathf.Min(lowest, pos.y - extent);
                highest = Mathf.Max(highest, pos.y + extent);
            }
            if (lowest > highest) lowest = highest = 0;
        }
    }
}
