using System;
using UnityEngine;

namespace TAP.Core
{
    /// <summary>
    /// Pure, thread-safe procedural height/colour function for a body's surface.
    /// Input directions are unit vectors in the body-fixed (rotating) frame.
    /// Heights are metres above the body's datum radius.
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

        /// <summary>Samples terrain height (m) and a generator-specific shading parameter.</summary>
        public abstract void Sample(Vector3d dir, out double height, out float shade);

        public double Height(Vector3d dir)
        {
            Sample(dir, out double h, out _);
            return h;
        }

        /// <summary>Surface colour for a sample. slope: 0 = flat, 1 = vertical.</summary>
        public abstract Color32 Colorize(Vector3d dir, double height, float shade, float slope);

        public static TerrainGenerator Create(BodyDefinition def, LaunchSiteDefinition site)
        {
            switch (def.terrain?.generator)
            {
                case "tellus":
                    return new TellusTerrain(def, site != null && site.body == def.id ? site : null);
                case "luma":
                    return new LumaTerrain(def);
                default:
                    return new FlatTerrain(def.radius, def.hasOcean);
            }
        }

        /// <summary>Unit direction for latitude/longitude (degrees) in the body-fixed frame.</summary>
        public static Vector3d DirectionFromLatLon(double latDeg, double lonDeg)
        {
            double lat = latDeg * MathD.Deg2Rad, lon = lonDeg * MathD.Deg2Rad;
            // lon 0 at +X, increasing eastward (towards -Z for prograde rotation about +Y).
            return new Vector3d(Math.Cos(lat) * Math.Cos(lon), Math.Sin(lat), -Math.Cos(lat) * Math.Sin(lon));
        }

        public static void LatLonFromDirection(Vector3d dir, out double latDeg, out double lonDeg)
        {
            Vector3d d = dir.normalized;
            latDeg = Math.Asin(MathD.Clamp(d.y, -1, 1)) * MathD.Rad2Deg;
            lonDeg = Math.Atan2(-d.z, d.x) * MathD.Rad2Deg;
        }

        protected static Color32 Mix(Color32 a, Color32 b, double t)
        {
            float f = (float)MathD.Clamp01(t);
            return new Color32(
                (byte)(a.r + (b.r - a.r) * f),
                (byte)(a.g + (b.g - a.g) * f),
                (byte)(a.b + (b.b - a.b) * f), 255);
        }

        protected static Color32 Scale(Color32 c, double s)
        {
            return new Color32(
                (byte)MathD.Clamp(c.r * s, 0, 255),
                (byte)MathD.Clamp(c.g * s, 0, 255),
                (byte)MathD.Clamp(c.b * s, 0, 255), 255);
        }
    }

    public sealed class FlatTerrain : TerrainGenerator
    {
        public FlatTerrain(double radius, bool ocean) : base(radius, ocean) { MaxHeight = 0; MinHeight = 0; }
        public override void Sample(Vector3d dir, out double height, out float shade) { height = 0; shade = 0; }
        public override Color32 Colorize(Vector3d dir, double height, float shade, float slope) => new Color32(128, 128, 128, 255);
    }

    /// <summary>
    /// Earth-like home world: noise continents and oceans, ridged mountain ranges, beaches and snow,
    /// with the launch complex placed on a flattened coastal plain facing an ocean to the east.
    /// </summary>
    public sealed class TellusTerrain : TerrainGenerator
    {
        private readonly Noise3D _continent, _mount, _hills, _detail;
        private readonly double _mountainHeight;
        private readonly bool _hasSite;
        private readonly Vector3d _siteDir, _bayDir;
        private readonly double _padAlt, _flatR, _blendR;

        private static readonly Color32 DeepWater = new Color32(14, 42, 92, 255);
        private static readonly Color32 ShallowWater = new Color32(30, 104, 148, 255);
        private static readonly Color32 Sand = new Color32(206, 190, 142, 255);
        private static readonly Color32 Grass = new Color32(86, 128, 58, 255);
        private static readonly Color32 GrassDry = new Color32(128, 138, 72, 255);
        private static readonly Color32 Forest = new Color32(44, 84, 40, 255);
        private static readonly Color32 Highland = new Color32(118, 108, 78, 255);
        private static readonly Color32 Rock = new Color32(112, 102, 92, 255);
        private static readonly Color32 Snow = new Color32(236, 240, 246, 255);

        public TellusTerrain(BodyDefinition def, LaunchSiteDefinition site) : base(def.radius, def.hasOcean)
        {
            int seed = def.terrain?.seed ?? 1;
            _continent = new Noise3D(seed);
            _mount = new Noise3D(seed + 101);
            _hills = new Noise3D(seed + 202);
            _detail = new Noise3D(seed + 303);
            _mountainHeight = def.terrain?.mountainHeight ?? 3000;
            MaxHeight = def.terrain?.maxHeight ?? 6000;
            MinHeight = def.terrain?.minHeight ?? -3500;
            if (site != null)
            {
                _hasSite = true;
                _siteDir = DirectionFromLatLon(site.latitude, site.longitude);
                // Ocean bay ~95 km east of the launch site so ascents fly over water.
                double eastLon = site.longitude + (95000.0 / def.radius) * MathD.Rad2Deg;
                _bayDir = DirectionFromLatLon(site.latitude - 0.05, eastLon);
                _padAlt = site.padAltitude;
                _flatR = site.flatRadius;
                _blendR = site.blendRadius;
            }
        }

        public override void Sample(Vector3d d, out double height, out float shade)
        {
            double c = _continent.Fbm(d * 1.35 + new Vector3d(3.1, 7.7, 1.3), 7, 2.0, 0.52) * 1.7 + 0.03;

            double siteDist = double.MaxValue;
            if (_hasSite)
            {
                siteDist = Vector3d.AngleRad(d, _siteDir) * Radius;
                double bayDist = Vector3d.AngleRad(d, _bayDir) * Radius;
                c += 0.32 * Math.Exp(-Sq(siteDist / 70000.0));
                c -= 0.55 * Math.Exp(-Sq(bayDist / 55000.0));
            }

            double h;
            if (c < 0)
            {
                // Ocean floor: shallow shelf near the coast, deep basins further out.
                h = -3400 * MathD.SmoothStep(0, 0.35, -c) - 30 * MathD.SmoothStep(0, 0.02, -c);
                shade = 0;
            }
            else
            {
                double lowland = 12 + c * 1600;
                double mountainMask = MathD.SmoothStep(0.08, 0.34, c);
                double m = 0;
                if (mountainMask > 0)
                {
                    double r = _mount.Ridged(d * 5.2 + new Vector3d(11.1, 2.2, 5.3), 6);
                    m = Math.Pow(r, 1.6) * _mountainHeight * mountainMask;
                }
                double hills = _hills.Fbm(d * 32.0, 4) * 160 * MathD.SmoothStep(0.0, 0.06, c);
                h = lowland + m + hills;
                shade = (float)_hills.Fbm(d * 90.0 + new Vector3d(5, 5, 5), 2); // vegetation variation
            }

            // Fine detail (only noticeable close to the ground).
            h += _detail.Fbm(d * 2600.0, 3) * 7.0 + _detail.Sample(d * 21000.0) * 0.9;

            if (_hasSite && siteDist < 30000)
            {
                double s1 = MathD.SmoothStep(_flatR, _flatR + _blendR, siteDist);
                double s2 = MathD.SmoothStep(_flatR, 30000, siteDist);
                h = _padAlt + (h - _padAlt) * s1 * (0.15 + 0.85 * s2);
            }

            height = h;
        }

        public override Color32 Colorize(Vector3d d, double h, float shade, float slope)
        {
            if (h < 0)
            {
                double depthT = MathD.SmoothStep(0, 400, -h);
                return Mix(ShallowWater, DeepWater, depthT);
            }
            Color32 c;
            if (h < 18) c = Sand;
            else
            {
                double v = shade * 0.5 + 0.5;
                c = Mix(Grass, GrassDry, MathD.SmoothStep(0.35, 0.75, v));
                c = Mix(c, Forest, MathD.SmoothStep(0.55, 0.8, 1 - v) * (1 - MathD.SmoothStep(1200, 2000, h)));
                c = Mix(c, Highland, MathD.SmoothStep(900, 2200, h));
                if (h < 30) c = Mix(Sand, c, (h - 18) / 12.0);
            }
            c = Mix(c, Rock, MathD.SmoothStep(0.28, 0.5, slope) + MathD.SmoothStep(2600, 3400, h) * 0.6);
            double snow = MathD.SmoothStep(3300, 3900, h) * (1 - MathD.SmoothStep(0.35, 0.55, slope));
            return Mix(c, Snow, snow);
        }

        private static double Sq(double x) => x * x;
    }

    /// <summary>Airless moon: cratered highlands, dark smooth maria, fresh bright-rayed craters.</summary>
    public sealed class LumaTerrain : TerrainGenerator
    {
        private readonly Noise3D _base, _maria, _detail;
        private readonly int _seed;
        private readonly double _depthScale;

        private static readonly double[] CellSizes = { 42000, 14000, 4600, 1500, 480, 150 };
        private static readonly double[] Probabilities = { 0.22, 0.32, 0.42, 0.5, 0.5, 0.45 };

        private static readonly Color32 Highland = new Color32(150, 148, 144, 255);
        private static readonly Color32 Mare = new Color32(82, 82, 84, 255);
        private static readonly Color32 Bright = new Color32(200, 198, 194, 255);

        public LumaTerrain(BodyDefinition def) : base(def.radius, false)
        {
            _seed = def.terrain?.seed ?? 7;
            _base = new Noise3D(_seed);
            _maria = new Noise3D(_seed + 17);
            _detail = new Noise3D(_seed + 29);
            _depthScale = def.terrain?.craterDepthScale ?? 1.0;
            MaxHeight = def.terrain?.maxHeight ?? 4500;
            MinHeight = def.terrain?.minHeight ?? -4000;
        }

        public override void Sample(Vector3d d, out double height, out float shade)
        {
            double b = _base.Fbm(d * 1.9, 6) * 1600 + _base.Fbm(d * 14.0 + new Vector3d(9, 1, 4), 4) * 260;
            double mariaMask = MathD.SmoothStep(0.06, 0.2, _maria.Fbm(d * 0.95 + new Vector3d(2.5, 8.1, 3.3), 4));
            double h = b * (1 - 0.8 * mariaMask) - 1100 * mariaMask;

            double albedo = 0;
            Vector3d p = d * Radius;
            for (int k = 0; k < CellSizes.Length; k++)
            {
                double s = CellSizes[k];
                double reach = 0.85 * s;
                long x0 = (long)Math.Floor((p.x - reach) / s), x1 = (long)Math.Floor((p.x + reach) / s);
                long y0 = (long)Math.Floor((p.y - reach) / s), y1 = (long)Math.Floor((p.y + reach) / s);
                long z0 = (long)Math.Floor((p.z - reach) / s), z1 = (long)Math.Floor((p.z + reach) / s);
                // Smaller craters are shallower on maria (partially filled) — keep them all.
                for (long cx = x0; cx <= x1; cx++)
                for (long cy = y0; cy <= y1; cy++)
                for (long cz = z0; cz <= z1; cz++)
                {
                    int cs = _seed * 31 + k * 7919;
                    if (Noise3D.Hash01(cx, cy, cz, cs) > Probabilities[k]) continue;
                    double jx = Noise3D.Hash01(cx, cy, cz, cs + 1);
                    double jy = Noise3D.Hash01(cx, cy, cz, cs + 2);
                    double jz = Noise3D.Hash01(cx, cy, cz, cs + 3);
                    double rr = Noise3D.Hash01(cx, cy, cz, cs + 4);
                    Vector3d center = new Vector3d((cx + jx) * s, (cy + jy) * s, (cz + jz) * s);
                    double radius = s * (0.14 + 0.3 * rr);
                    double dx = p.x - center.x, dy = p.y - center.y, dz = p.z - center.z;
                    double dist = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                    double xn = dist / radius;
                    if (xn > 2.4) continue;
                    double depth = radius * (k <= 1 ? 0.07 : 0.18) * _depthScale;
                    double rim = depth * 0.28;
                    double ch;
                    if (xn < 1)
                    {
                        ch = Math.Max((xn * xn - 1) * depth, -depth * 0.85);
                        ch += rim * Math.Exp(-Sq((xn - 1) / 0.2));
                    }
                    else
                    {
                        ch = rim * Math.Exp(-Sq((xn - 1) / 0.28));
                    }
                    h += ch;
                    double fresh = Noise3D.Hash01(cx, cy, cz, cs + 5);
                    if (fresh > 0.72) albedo += (1 - MathD.SmoothStep(0.6, 2.4, xn)) * 0.8;
                }
            }

            h += _detail.Fbm(d * 3000.0, 3) * 5.0 + _detail.Sample(d * 26000.0) * 0.6;
            height = h;
            shade = (float)(albedo - mariaMask);
        }

        public override Color32 Colorize(Vector3d d, double h, float shade, float slope)
        {
            double v = _detail.Fbm(d * 40.0, 2) * 0.12;
            Color32 c = Highland;
            if (shade < 0) c = Mix(Highland, Mare, -shade);
            else if (shade > 0) c = Mix(Highland, Bright, Math.Min(1.0, shade));
            c = Scale(c, 1.0 + v - slope * 0.25);
            return c;
        }

        private static double Sq(double x) => x * x;
    }
}
