using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TAP.Core;
using UnityEngine;

namespace TAP.Tests
{
    /// <summary>
    /// The planetary system as a whole (SYS-16): orbits obey Kepler, moons and planets keep out of each other's spheres of
    /// influence, locked moons stay locked, warp limits clear the air and the ground, and the gas giants, the rings and the
    /// star can be flown. Reads <c>Data/system.json</c> only: no body is named, so the tests hold for any system file.
    /// </summary>
    public class PlanetarySystemTests
    {
        private static CelestialSystem Home => CelestialSystem.LoadFromResources();

        private static IEnumerable<CelestialBody> WithParent(CelestialSystem s) => s.Bodies.Where(b => b.Parent != null);

        [Test]
        public void System_IsAFamilyOfWorlds()
        {
            var s = Home;
            Assert.GreaterOrEqual(s.Bodies.Count, 20, "a star, its planets and their moons");
            Assert.IsTrue(s.Bodies.Any(b => b.Def.type == "dwarf"), "a dwarf planet");
            Assert.GreaterOrEqual(s.Bodies.Count(b => b.IsGasGiant), 2, "a gas giant and a ringed one");
            Assert.IsTrue(s.Bodies.Any(b => b.IsGasGiant && b.Rings.Count > 0), "a ringed giant");
            Assert.IsTrue(s.Bodies.Any(b => b.IsGasGiant && b.Rings.Count == 0), "a giant without rings");
            Assert.IsTrue(s.Bodies.Any(b => !b.IsStar && b.Parent.IsStar && b.Atmosphere != null && !b.IsGasGiant), "a planet with air");
            Assert.IsTrue(s.Bodies.Any(b => b.Def.type == "moon" && b.Atmosphere != null), "a moon with air");
            Assert.IsTrue(s.Bodies.Any(b => b.Def.type == "moon" && b.Parent.IsGasGiant), "moons of a giant");
            foreach (var g in s.Bodies.Where(b => b.IsGasGiant)) Assert.Greater(g.Children.Count, 0, $"{g.Name} has moons");
        }

        [Test]
        public void Orbits_FollowKeplersThirdLaw_AndLaplacesSphere()
        {
            foreach (var b in WithParent(Home))
            {
                double a = b.Orbit.SemiMajorAxis;
                double period = MathD.TwoPi * Math.Sqrt(a * a * a / b.Parent.GM);
                Assert.AreEqual(period, b.Orbit.Period, period * 1e-9, $"{b.Name}'s period");
                double soi = a * Math.Pow(b.GM / b.Parent.GM, 0.4);
                Assert.AreEqual(soi, b.SOIRadius, soi * 1e-9, $"{b.Name}'s sphere of influence");
            }
        }

        [Test]
        public void Moons_FitInsideTheirParentsSphere_AndClearItsAir()
        {
            foreach (var m in WithParent(Home).Where(b => !b.Parent.IsStar))
            {
                var p = m.Parent;
                Assert.Less(m.Orbit.ApoapsisRadius + m.SOIRadius, p.SOIRadius, $"{m.Name} and its sphere fit inside {p.Name}'s");
                double air = p.Atmosphere != null ? p.Atmosphere.Height : 0;
                double ground = p.Terrain != null ? p.Terrain.MaxHeight : 0;
                Assert.Greater(m.Orbit.PeriapsisRadius - m.SOIRadius, p.Radius + Math.Max(air, ground), $"{m.Name}'s orbit clears {p.Name}'s air and ground");
                Assert.Less(m.SOIRadius, p.SOIRadius * 0.5, $"{m.Name}'s sphere is small beside {p.Name}'s");
            }
        }

        [Test]
        public void Siblings_NeverShareASphereOfInfluence()
        {
            var s = Home;
            foreach (var parent in s.Bodies)
            {
                var kids = parent.Children.OrderBy(c => c.Orbit.SemiMajorAxis).ToList();
                for (int i = 0; i + 1 < kids.Count; i++)
                {
                    var x = kids[i];
                    var y = kids[i + 1];
                    Assert.Less(x.Orbit.ApoapsisRadius + x.SOIRadius, y.Orbit.PeriapsisRadius - y.SOIRadius,
                        $"{x.Name}'s and {y.Name}'s spheres never meet around {parent.Name}");
                }
            }
        }

        [Test]
        public void TidalLocks_StayLocked_AndTurnWithTheirOrbit()
        {
            foreach (var m in Home.Bodies.Where(b => b.Def.tidallyLocked))
            {
                Assert.AreEqual(m.Orbit.Period, Math.Abs(m.RotationPeriod), 1e-6, $"{m.Name} turns once per orbit");
                Assert.AreEqual(m.Orbit.W.y < 0, m.RotationPeriod < 0, $"{m.Name} turns the way it orbits");
                if (m.Orbit.Eccentricity > 0.01) continue;
                // The same face (its longitude: an inclined orbit nods the latitude) towards the parent at other times.
                Vector3d Face(double ut)
                {
                    Vector3d f = m.InertialToBodyFixed(-m.Orbit.GetPositionAtUT(ut), ut);
                    return new Vector3d(f.x, 0, f.z).normalized;
                }
                Vector3d f0 = Face(0);
                // Bodies spin about +Y only, so the nod of an inclined orbit (its tilt away from the equator, squared) shows as libration.
                double tilt = Math.Acos(Math.Min(1, Math.Abs(m.Orbit.W.y)));
                foreach (double t in new[] { 0.33, 0.71 })
                    Assert.Less((Face(t * m.Orbit.Period) - f0).magnitude, 2e-3 + tilt * tilt, $"{m.Name} keeps its face to {m.Parent.Name}");
            }
        }

        [Test]
        public void LockedMoons_StartWithTheirNearSideTowardsTheParent()
        {
            // Longitude 0 faces the parent at the start for every locked moon made with the system (Luma, the first, predates it).
            foreach (var m in Home.Bodies.Where(b => b.Def.tidallyLocked && b.Id != "luma" && b.Orbit.Eccentricity < 0.05 && b.Orbit.W.y > 0))
            {
                Vector3d f = m.InertialToBodyFixed(-m.Orbit.GetPositionAtUT(0), 0);
                double angle = Vector3d.AngleRad(new Vector3d(f.x, 0, f.z), Geo.FromLatLon(0, 0)) * MathD.Rad2Deg;
                Assert.Less(angle, 4, $"{m.Name}'s longitude 0 faces {m.Parent.Name} at the start (off by {angle:F1}°)");
            }
        }

        [Test]
        public void RetrogradeMoon_TurnsTheOtherWay()
        {
            var retro = Home.Bodies.Where(b => b.Parent != null && b.Orbit.W.y < 0).ToList();
            Assert.IsNotEmpty(retro, "a retrograde moon, like Triton");
            foreach (var m in retro.Where(b => b.Def.tidallyLocked))
            {
                Assert.Less(m.RotationPeriod, 0, $"{m.Name} turns clockwise");
                Assert.Less(m.AngularVelocity.y, 0);
                // Its surface moves with its orbit, not against it: the day length is finite and positive.
                Assert.Greater(m.SolarDay, 0);
            }
        }

        [Test]
        public void WarpLimits_SitAboveTheAirAndTheGround()
        {
            foreach (var b in Home.Bodies)
            {
                var lim = b.WarpAltitudeLimits;
                Assert.IsNotNull(lim, $"{b.Name} has warp limits");
                Assert.AreEqual(8, lim.Length, $"{b.Name}: one limit per rails rate");
                for (int i = 1; i < lim.Length; i++) Assert.GreaterOrEqual(lim[i], lim[i - 1], $"{b.Name}'s limits rise with the rate");
                double top = Math.Max(b.Atmosphere != null ? b.Atmosphere.Height : 0, b.Terrain != null ? b.Terrain.MaxHeight : 0);
                Assert.GreaterOrEqual(lim[1], top, $"{b.Name}: the slowest rails rate stays above its highest ground and air");
                Assert.GreaterOrEqual(lim[7], lim[1] * 2, $"{b.Name}: the fastest rate needs more room than the slowest");
            }
        }

        [Test]
        public void TheStar_CanBeOrbitedAndWarped()
        {
            var star = Home.Star;
            Assert.NotNull(star);
            // Rails warp at 50x must be open 60 Mm up (an orbit of 8.7 h), 1,000x at 400 Mm, so an orbit isn't a day-long wait.
            Assert.LessOrEqual(star.WarpAltitudeLimits[3], 60e6, "50x allowed at 60 Mm");
            Assert.LessOrEqual(star.WarpAltitudeLimits[5], 400e6, "1,000x allowed at 400 Mm");
            double r = star.Radius + 60e6;
            var o = new Orbit(new Vector3d(r, 0, 0), new Vector3d(0, 0, -Math.Sqrt(star.GM / r)), 0, star.GM);
            Assert.AreEqual(0, o.Eccentricity, 1e-9, "a circular orbit round the star");
            Assert.Less((o.GetPositionAtUT(3 * o.Period) - new Vector3d(r, 0, 0)).magnitude, 1.0, "three revolutions bring it back");
            Assert.Greater(o.Period, 3600, "the period is an honest few hours");
        }

        [Test]
        public void Atmospheres_ThinWithAltitude()
        {
            foreach (var b in Home.Bodies.Where(x => x.Atmosphere != null))
            {
                var a = b.Atmosphere;
                double previous = double.MaxValue;
                for (int i = 0; i <= 40; i++)
                {
                    double p = a.Pressure(a.Height * i / 40.0);
                    Assert.LessOrEqual(p, previous + 1e-9, $"{b.Name}'s pressure falls with altitude");
                    previous = p;
                }
                Assert.Greater(a.Temperature(0), 0);
                Assert.AreEqual(0, a.Pressure(a.Height), 1e-9, $"{b.Name}'s air ends at its top");
            }
        }

        [Test]
        public void GasGiants_AreAllAir_AndCrush()
        {
            foreach (var g in Home.Bodies.Where(b => b.IsGasGiant))
            {
                Assert.NotNull(g.Atmosphere, $"{g.Name} is all air");
                Assert.IsFalse(g.HasOcean, $"{g.Name} has no sea");
                Assert.Greater(g.CrushPressure, 0, $"{g.Name} crushes a vessel");
                Assert.Less(g.CrushPressure, g.Atmosphere.SeaLevelPressure, $"{g.Name}: vessels are crushed above the datum");
                double crushAltitude = g.Atmosphere.AltitudeForPressure(g.CrushPressure);
                Assert.Greater(crushAltitude, 0, $"{g.Name}'s crush depth is a way above its datum");
                Assert.Less(crushAltitude, g.Atmosphere.Height * 0.4, $"{g.Name}'s crush depth is deep in the clouds");
                // An orbit at the top of the air must be able to last: almost no pressure there.
                Assert.Less(g.Atmosphere.Pressure(g.Atmosphere.Height * 0.9), 150, $"{g.Name}'s air is thin where orbits start");
                // The datum is smooth: there is no ground to land on.
                Assert.LessOrEqual(g.Terrain.MaxHeight, 50);
                Assert.GreaterOrEqual(g.Terrain.MinHeight, -50);
            }
        }

        [Test]
        public void Rings_AreBandsWithGaps()
        {
            var ringed = Home.Bodies.First(b => b.Rings.Count > 0);
            RingProfile.Range(ringed.Rings, out double inner, out double outer);
            Assert.Greater(inner, ringed.Radius, "rings start above the clouds");
            Assert.Less(outer, ringed.SOIRadius * 0.01, "rings sit deep inside the sphere of influence");
            Assert.AreEqual(0, RingProfile.Opacity(ringed.Rings, inner * 0.9), "nothing inside the rings");
            Assert.AreEqual(0, RingProfile.Opacity(ringed.Rings, outer * 1.1), "nothing beyond them");
            double densest = 0, thinnest = 1;
            for (int i = 1; i < 400; i++)
            {
                double o = RingProfile.Opacity(ringed.Rings, inner + (outer - inner) * i / 400);
                densest = Math.Max(densest, o);
                thinnest = Math.Min(thinnest, o);
            }
            Assert.Greater(densest, 0.7, "a dense ring");
            Assert.AreEqual(0, thinnest, 1e-9, "a gap in the rings");
            // Every moon orbits outside the rings.
            foreach (var m in ringed.Children) Assert.Greater(m.Orbit.PeriapsisRadius - m.SOIRadius, outer, $"{m.Name} clears the rings");
        }

        [Test]
        public void EveryWorld_HasBiomes_AndTheyCanBeFound()
        {
            foreach (var b in Home.Bodies.Where(x => !x.IsStar))
            {
                var t = b.Terrain;
                Assert.NotNull(t, b.Name);
                var seen = new HashSet<string>();
                var r = new System.Random(11);
                for (int i = 0; i < 4000; i++)
                {
                    Vector3d v;
                    do v = new Vector3d(r.NextDouble() * 2 - 1, r.NextDouble() * 2 - 1, r.NextDouble() * 2 - 1);
                    while (v.sqrMagnitude < 1e-6 || v.sqrMagnitude > 1);
                    seen.Add(t.BiomeAt(v.normalized).Id);
                }
                Assert.GreaterOrEqual(t.Biomes.Count, b.IsGasGiant || b.Radius < 100e3 ? 3 : 5, $"{b.Name} declares enough biomes");
                Assert.GreaterOrEqual(seen.Count, Math.Max(3, (int)Math.Ceiling(t.Biomes.Count * 0.6)),
                    $"{b.Name}: found {seen.Count} of {t.Biomes.Count} biomes ({string.Join(", ", seen)})");
            }
        }

        [Test]
        public void SmallBodies_StayAboveTheirCentre()
        {
            foreach (var b in Home.Bodies.Where(x => !x.IsStar))
                Assert.Greater(b.Radius + b.Terrain.MinHeight, b.Radius * 0.4, $"{b.Name}'s lowest ground is well above its centre");
        }
    }
}
