using TAP.Core;
using TAP.Parts;
using TAP.Simulation;
using UnityEngine;

namespace TAP.Game
{
    /// <summary>Builds the launch complex (pad deck, flame trench, service tower, assembly building) as surface objects.</summary>
    public static class LaunchSiteBuilder
    {
        public static void Build(FlightSim sim, PlanetManager planets)
        {
            var site = sim.System.Def.launchSite;
            var body = sim.System.Get(site.body) ?? sim.System.Root;
            Vector3d up = TerrainGenerator.DirectionFromLatLon(site.latitude, site.longitude);
            QuaternionD rot = LaunchService.SiteRotationBF(body, site); // +Y up, +Z east
            Vector3d deckTop = up * (body.Radius + site.padAltitude + site.padDeckHeight);

            // Pad deck: its top surface is at deckTop.
            var pad = new GameObject("LaunchPad");
            var mb = new MeshBuilder();
            float deckH = (float)site.padDeckHeight + 2.5f; // extends below the terrain surface
            mb.Box((int)MatSlot.Concrete, new Vector3(0, -deckH / 2, 0), new Vector3(34, deckH, 34));
            // flame trench marks and stripes
            mb.Box((int)MatSlot.Dark, new Vector3(0, 0.01f, 0), new Vector3(7, 0.02f, 7));
            mb.Box((int)MatSlot.Yellow, new Vector3(0, 0.012f, 12), new Vector3(20, 0.02f, 0.6f));
            mb.Box((int)MatSlot.Yellow, new Vector3(0, 0.012f, -12), new Vector3(20, 0.02f, 0.6f));
            for (int i = 0; i < 4; i++)
            {
                Quaternion q = Quaternion.AngleAxis(45 + i * 90, Vector3.up);
                mb.OrientedBox((int)MatSlot.MetalDark, q * new Vector3(0, 1.0f, 4.2f), q, new Vector3(0.8f, 2f, 0.8f));
            }
            var mesh = mb.ToMesh("launchpad", out int[] slots);
            PartModelFactory.AddRenderer(pad, mesh, slots);
            var col = pad.AddComponent<BoxCollider>();
            col.center = new Vector3(0, -deckH / 2, 0);
            col.size = new Vector3(34, deckH, 34);
            pad.layer = Layers.Terrain;
            planets.AddSurfaceObject(body, pad, deckTop, rot, true);

            // Service tower (to the north-west of the pad)
            var tower = new GameObject("ServiceTower");
            var tb = new MeshBuilder();
            float th = 42f;
            Vector3 tc = new Vector3(-11, 0, -9);
            for (int i = 0; i < 4; i++)
            {
                float dx = (i % 2 == 0 ? -1.5f : 1.5f), dz = (i < 2 ? -1.5f : 1.5f);
                tb.Box((int)MatSlot.Red, tc + new Vector3(dx, th / 2, dz), new Vector3(0.35f, th, 0.35f));
            }
            for (float y = 3; y < th; y += 4)
            {
                tb.Box((int)MatSlot.Grey, tc + new Vector3(0, y, -1.5f), new Vector3(3.2f, 0.25f, 0.25f));
                tb.Box((int)MatSlot.Grey, tc + new Vector3(0, y, 1.5f), new Vector3(3.2f, 0.25f, 0.25f));
                tb.Box((int)MatSlot.Grey, tc + new Vector3(-1.5f, y, 0), new Vector3(0.25f, 0.25f, 3.2f));
                tb.Box((int)MatSlot.Grey, tc + new Vector3(1.5f, y, 0), new Vector3(0.25f, 0.25f, 3.2f));
            }
            tb.Box((int)MatSlot.Dark, tc + new Vector3(0, th + 0.5f, 0), new Vector3(4, 1, 4));
            // Swing arm: attached to the east face of the tower, pointing at the pad but ending ~8 m short of the rocket.
            tb.Box((int)MatSlot.Grey, tc + new Vector3(1.5f + 3f, 22f, 1.0f), new Vector3(6f, 0.5f, 1.2f));
            tb.Box((int)MatSlot.Grey, tc + new Vector3(1.5f + 3f, 23.1f, 0.45f), new Vector3(6f, 0.12f, 0.12f));
            tb.Box((int)MatSlot.Grey, tc + new Vector3(1.5f + 3f, 23.1f, 1.55f), new Vector3(6f, 0.12f, 0.12f));
            var tmesh = tb.ToMesh("tower", out int[] tslots);
            PartModelFactory.AddRenderer(tower, tmesh, tslots);
            var tcol = tower.AddComponent<BoxCollider>();
            tcol.center = tc + new Vector3(0, th / 2, 0);
            tcol.size = new Vector3(3.4f, th, 3.4f);
            tower.layer = Layers.Terrain;
            planets.AddSurfaceObject(body, tower, deckTop, rot, true);

            // Assembly building ~420 m west of the pad
            var vab = new GameObject("AssemblyBuilding");
            var vb = new MeshBuilder();
            Vector3 vc = new Vector3(0, 0, -420);
            vb.Box((int)MatSlot.White, vc + new Vector3(0, 32, 0), new Vector3(70, 64, 50));
            vb.Box((int)MatSlot.Dark, vc + new Vector3(0, 28, 25.1f), new Vector3(26, 56, 0.4f));
            vb.Box((int)MatSlot.Blue, vc + new Vector3(0, 62, 25.2f), new Vector3(40, 3, 0.3f));
            vb.Box((int)MatSlot.Grey, vc + new Vector3(45, 8, 0), new Vector3(20, 16, 30));
            var vmesh = vb.ToMesh("vab", out int[] vslots);
            PartModelFactory.AddRenderer(vab, vmesh, vslots);
            var vcol = vab.AddComponent<BoxCollider>();
            vcol.center = vc + new Vector3(0, 32, 0);
            vcol.size = new Vector3(70, 64, 50);
            vab.layer = Layers.Terrain;
            // Building sits on the terrain at padAltitude (not the deck).
            Vector3d ground = up * (body.Radius + site.padAltitude);
            planets.AddSurfaceObject(body, vab, ground, rot, true);
        }
    }
}
