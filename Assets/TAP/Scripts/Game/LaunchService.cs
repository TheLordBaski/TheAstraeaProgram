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
        public static QuaternionD PadRotationBF(CelestialBody body, LaunchSiteDefinition site)
        {
            Vector3d up = TerrainGenerator.DirectionFromLatLon(site.latitude, site.longitude);
            Vector3d north = Geo.North(up);
            // Rotation whose +Y = up, +Z = north, +X = up x north = east
            Quaternion q = Quaternion.LookRotation((Vector3)north, (Vector3)up);
            return QuaternionD.FromQuaternion(q);
        }

        /// <summary>Orientation of the launch complex buildings: +Y up, +Z east (the layout the site was designed in).</summary>
        public static QuaternionD SiteRotationBF(CelestialBody body, LaunchSiteDefinition site)
        {
            Vector3d up = TerrainGenerator.DirectionFromLatLon(site.latitude, site.longitude);
            Quaternion q = Quaternion.LookRotation((Vector3)Geo.East(up), (Vector3)up);
            return QuaternionD.FromQuaternion(q);
        }

        public static Vector3d PadPositionBF(CelestialBody body, LaunchSiteDefinition site)
        {
            Vector3d up = TerrainGenerator.DirectionFromLatLon(site.latitude, site.longitude);
            return up * (body.Radius + site.padAltitude + site.padDeckHeight);
        }

        public static VesselRecord CreateLaunchRecord(CraftDesign design, PartDatabase db, double ut)
        {
            var sys = CelestialSystem.Default;
            var site = sys.Def.launchSite;
            var body = sys.Get(site.body) ?? sys.Root;
            var rec = new VesselRecord
            {
                name = design.name,
                designName = design.name,
                kind = VesselKind.Ship,
                situation = Situation.Prelaunch,
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
            float lowest = float.MaxValue;
            foreach (var p in d.parts)
            {
                var def = db.Get(p.partId);
                if (def == null) continue;
                Vector3 pos = new Vector3(p.pos[0], p.pos[1], p.pos[2]);
                Quaternion q = new Quaternion(p.rot[0], p.rot[1], p.rot[2], p.rot[3]);
                float r = def.diameter * 0.5f, h = def.height * 0.5f;
                if (def.model != null && def.model.type == "leg") { h = 0.3f; }
                // extreme points of the bounding cylinder along world Y
                Vector3 axis = q * Vector3.up;
                float extent = Mathf.Abs(axis.y) * h + Mathf.Sqrt(Mathf.Max(0, 1 - axis.y * axis.y)) * r;
                lowest = Mathf.Min(lowest, pos.y - extent);
            }
            return lowest == float.MaxValue ? 0 : lowest;
        }
    }
}
