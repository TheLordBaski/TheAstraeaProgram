using System;
using System.Collections.Generic;
using UnityEngine;

namespace TAP.Core
{
    /// <summary>
    /// A body's surface: height, colour and biome for any direction (a unit vector in the body-fixed, rotating frame).
    /// Heights are metres above the body's datum radius, which is sea level. Thread-safe: terrain chunks are generated
    /// on worker threads while the game adds and removes flat areas on the main thread.
    /// </summary>
    public abstract class TerrainGenerator
    {
        public readonly double Radius;
        public readonly bool HasOcean;
        public double MaxHeight { get; protected set; }
        public double MinHeight { get; protected set; }

        protected TerrainGenerator(double radius, bool hasOcean)
        {
            Radius = radius;
            HasOcean = hasOcean;
        }

        /// <summary>How many named values (fields) a sample carries for colours and biomes.</summary>
        public abstract int FieldCount { get; }

        /// <summary>
        /// Height (m) at a direction. Writes the sample's fields to fields[offset..offset + FieldCount); full: also those
        /// only colours and biomes use.
        /// </summary>
        public abstract double Sample(Vector3d dir, double[] fields, int offset, bool full);

        [ThreadStatic] private static double[] _scratch;

        public double Height(Vector3d dir)
        {
            var f = _scratch;
            if (f == null || f.Length < FieldCount) _scratch = f = new double[Math.Max(FieldCount, 16)];
            return Sample(dir, f, 0, false);
        }

        /// <summary>Surface colour of a full sample. cosSlope: cosine of the ground's tilt (1 = level).</summary>
        public abstract Color32 Colorize(Vector3d dir, double height, double[] fields, int offset, double cosSlope);

        /// <summary>The biome at a direction. Biomes belong to the natural ground: flat areas never change them.</summary>
        public abstract Biome BiomeAt(Vector3d dir);

        public abstract IReadOnlyList<Biome> Biomes { get; }

        /// <summary>Raised on the main thread when a flat area appears or goes: its centre and angular reach (rad).</summary>
        public event Action<Vector3d, double> Edited;

        protected void RaiseEdited(Vector3d centre, double reach) => Edited?.Invoke(centre, reach);

        public static TerrainGenerator Create(BodyDefinition def) =>
            new LayeredTerrain(def.terrain ?? new TerrainDefinition(), def.radius, def.hasOcean, def.id);

        /// <summary>
        /// Unit direction for a latitude/longitude (degrees) in the terrain's own convention, where latitude counts
        /// towards +Y (geographic south; see <see cref="Geo"/>). Game code uses <see cref="Geo.FromLatLon"/>.
        /// </summary>
        public static Vector3d DirectionFromLatLon(double latDeg, double lonDeg)
        {
            double lat = latDeg * MathD.Deg2Rad, lon = lonDeg * MathD.Deg2Rad;
            double cl = DetMath.Cos(lat);
            // lon 0 at +X, increasing eastward (towards -Z for prograde rotation about +Y).
            return new Vector3d(cl * DetMath.Cos(lon), DetMath.Sin(lat), -cl * DetMath.Sin(lon));
        }

        public static void LatLonFromDirection(Vector3d dir, out double latDeg, out double lonDeg)
        {
            Vector3d d = dir.normalized;
            latDeg = DetMath.Asin(MathD.Clamp(d.y, -1, 1)) * MathD.Rad2Deg;
            lonDeg = DetMath.Atan2(-d.z, d.x) * MathD.Rad2Deg;
        }
    }

    /// <summary>A named region of a body's surface, for science and the map overlay.</summary>
    public sealed class Biome
    {
        public string Id;
        public string Name;
        /// <summary>Colour on the biome map overlay.</summary>
        public Color32 Color;
        /// <summary>Surface material set used to render it (terrain shader v2).</summary>
        public int Material;
        public int Index;

        public override string ToString() => Name;
    }

    /// <summary>
    /// Ground pulled onto a plane: the launch complex, or the ground under a base. The plane passes through the centre
    /// at <see cref="Height"/> and may tilt with the land (buildings stand on foundations that make up the difference).
    /// Inside <see cref="Radius"/> the ground is the plane; over <see cref="Blend"/> it eases back into the land.
    /// </summary>
    public sealed class FlatArea
    {
        public string Id;
        public string Name;
        /// <summary>Body-fixed unit direction of the centre.</summary>
        public Vector3d Center;
        /// <summary>Height of the plane at the centre (m above datum).</summary>
        public double Height;
        /// <summary>Body-fixed unit normal of the plane (the centre's direction when level).</summary>
        public Vector3d Normal;
        public double Radius;
        public double Blend;
        /// <summary>Optional wider softening: out to OuterRadius the land keeps only part of its relief.</summary>
        public double OuterRadius;
        public double OuterRelief = 1;

        internal double CosReach, PlaneD;

        /// <summary>How far it reaches (m): where the land is untouched again.</summary>
        public double Reach => Math.Max(Radius + Blend, OuterRadius);

        /// <summary>Tilt of the plane from level (degrees).</summary>
        public double SlopeDeg => DetMath.Acos(MathD.Clamp(Vector3d.Dot(Normal, Center), -1, 1)) * MathD.Rad2Deg;

        internal void Prepare(double bodyRadius)
        {
            Center = Center.normalized;
            Normal = Normal.sqrMagnitude > 0 ? Normal.normalized : Center;
            PlaneD = Vector3d.Dot(Center * (bodyRadius + Height), Normal);
            CosReach = DetMath.Cos(Math.Min(Math.PI, Reach / bodyRadius));
        }

        /// <summary>The plane's height (m above datum) along a direction.</summary>
        public double PlaneHeight(Vector3d dir, double bodyRadius)
        {
            double dn = Vector3d.Dot(dir, Normal);
            return dn > 1e-6 ? PlaneD / dn - bodyRadius : Height;
        }

        /// <summary>The definition that recreates this area (for saves).</summary>
        public FlatAreaDef ToDef()
        {
            Geo.ToLatLon(Center, out double lat, out double lon);
            return new FlatAreaDef
            {
                id = Id, name = Name, lat = lat, lon = lon, height = Height, radius = Radius, blend = Blend,
                outerRadius = OuterRadius, outerRelief = OuterRelief,
                normal = new[] { Normal.x, Normal.y, Normal.z },
            };
        }
    }
}
