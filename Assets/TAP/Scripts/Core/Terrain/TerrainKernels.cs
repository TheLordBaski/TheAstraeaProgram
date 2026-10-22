using System;
using Unity.Burst;
using UnityEngine;

namespace TAP.Core
{
    // The compiled terrain (LayeredTerrain.Burst.cs): plain structs mirroring LayeredTerrain's classes. A list of terms is a range of
    // TermRefs; a noise is an index into Noises (-1: none).

    internal struct NoiseOp
    {
        public LayeredTerrain.NoiseType Type;
        public int Perm, Octaves;
        public double Frequency, Lacunarity, Gain, Amount;
        public Vector3d Offset;
        public bool HasOffset;
    }

    internal struct FieldOp
    {
        public int Noise, Term, From, SumStart, SumCount, SpotStart, SpotCount;
        public LayeredTerrain.Shape Shape;
        public double Scale, Add, A, B;
        public bool AfterLayers, ForHeight;
    }

    internal struct FieldSum
    {
        public int Index;
        public double Weight;
    }

    internal struct TermOp
    {
        public LayeredTerrain.Src Source;
        public LayeredTerrain.Shape Shape;
        public int Field, SumStart, SumCount, Jitter;
        public Vector3d NearDir;
        public double A, B, Scale, R0, R1;
        public bool HasMin, HasMax, Invert, Remap;
    }

    internal struct LayerOp
    {
        public LayeredTerrain.LayerKind Kind;
        public LayeredTerrain.Shape Shape;
        public LayeredTerrain.BlendMode Blend;
        /// <summary>Craters, rift or dunes: their index; canyons: their noise.</summary>
        public int Noise, From, Sub, MaskStart, MaskCount;
        public double Constant, Step, Sharpness, A, B, Power, Amplitude, Add, CanyonWidth, CanyonDepth, CanyonSharpness;
        public Vector3d Axes;
        public bool Abs, Detail;
    }

    internal struct CraterOp
    {
        public int Seed, SizeStart, SizeCount, RaysField, FloorField, FreshField;
        public double RMin, RMax, Rim, RimIn, RimOut, Floor, Reach, Search, DepthScale, Fresh, Rays, RaysFrom, Erosion, FloorMinRadius, FreshMinRadius, Radius;
    }

    internal struct CraterSize
    {
        public double Cell, Chance, Depth;
    }

    internal struct RiftOp
    {
        public int PointStart, PointCount, Wobble, OutField;
        public double HalfWidth, Depth, Floor, Shoulders, Radius;
    }

    internal struct DunesOp
    {
        public Vector3d Axis;
        public double Wavelength, Height, Crest;
        public int Warp;
    }

    internal struct ColorOp
    {
        public Color32 Color;
        public int MaskStart, MaskCount;
    }

    internal struct FlatOp
    {
        public Vector3d Center, Normal;
        public double Height, Radius, Blend, OuterRadius, OuterRelief, CosReach, PlaneD;
    }

    internal unsafe struct TerrainProgram
    {
        public NoiseOp* Noises;
        public int* Perms;
        public FieldOp* Fields;
        /// <summary>FieldCount: the listed fields; AllFieldCount: with the layers' outputs (a sample's stride).</summary>
        public int FieldCount, AllFieldCount;
        public FieldSum* FieldSums;
        public LayeredTerrain.Spot* Spots;
        public TermOp* Terms;
        public int* TermRefs;
        public LayerOp* Layers;
        public int LayerCount;
        public CraterOp* Craters;
        public CraterSize* CraterSizes;
        public RiftOp* Rifts;
        public Vector3d* RiftPoints;
        public DunesOp* Dunes;
        public int* Outputs;
        public int OutputCount;
        public FlatOp* Flats;
        public int FlatCount;
        public ColorOp* Colors;
        public int ColorCount;
        public bool HasShading;
        public int ShadingNoise;
        public double ShadingAmount, ShadingSlope;
        public double Radius, SeaLevel;
        public bool HasOcean;
        public int ShoreField;
        /// <summary>Scratch fields for slope and shore distance (as LayeredTerrain's Aux).</summary>
        public double* Aux;
    }

    /// <summary>
    /// LayeredTerrain's evaluation (SampleCore, the fields, terms, layers, flat areas and Colorize) compiled by Burst, for batches of
    /// samples. Every expression mirrors the classes', in the same order, and strict float mode keeps IEEE arithmetic (no fused
    /// multiply-add, no reordering), so the results are the same bits; the tests compare the two. Called on worker threads.
    /// </summary>
    [BurstCompile(FloatMode = FloatMode.Strict, FloatPrecision = FloatPrecision.Standard, CompileSynchronously = true)]
    internal static unsafe class TerrainKernels
    {
        private struct Ctx
        {
            public Vector3d Dir;
            public double* F;
            public double Height, Slope, Lat, Shore;
        }

        [BurstCompile(FloatMode = FloatMode.Strict, FloatPrecision = FloatPrecision.Standard, CompileSynchronously = true)]
        public static void Sample(TerrainProgram* p, Vector3d* dirs, bool* full, double* heights, double* fields, int count)
        {
            for (int k = 0; k < count; k++) heights[k] = SampleCore(p, dirs[k], fields + (long)k * p->AllFieldCount, full[k], true, false);
        }

        [BurstCompile(FloatMode = FloatMode.Strict, FloatPrecision = FloatPrecision.Standard, CompileSynchronously = true)]
        public static void Colorize(TerrainProgram* p, Vector3d* dirs, double* heights, double* fields, int* samples, double* cosSlopes, Color32* colors, int count)
        {
            for (int i = 0; i < count; i++)
            {
                int k = samples[i];
                colors[i] = Colorize(p, dirs[k], heights[k], fields + (long)k * p->AllFieldCount, cosSlopes[i]);
            }
        }

        // ================================================================== sampling (LayeredTerrain.SampleCore)

        private static double SampleCore(TerrainProgram* p, Vector3d dir, double* F, bool full, bool flats, bool noDetail)
        {
            for (int i = 0; i < p->FieldCount; i++)
            {
                FieldOp* f = p->Fields + i;
                if (f->AfterLayers || (!full && !f->ForHeight)) continue;
                F[i] = Field(p, f, dir, F);
            }
            for (int i = 0; i < p->OutputCount; i++) F[p->Outputs[i]] = 0;

            var c = new Ctx { Dir = dir, F = F, Slope = 0, Lat = double.NaN, Shore = double.NaN };
            double h = 0;
            for (int i = 0; i < p->LayerCount; i++)
            {
                LayerOp* l = p->Layers + i;
                if (noDetail && l->Detail) continue;
                c.Height = h;
                double m = 1;
                if (l->MaskCount > 0)
                {
                    m = Product(p, l->MaskStart, l->MaskCount, ref c);
                    if (m == 0) continue;
                }
                h = Layer(p, l, h, m, ref c);
            }
            h -= p->SeaLevel;

            if (full)
                for (int i = 0; i < p->FieldCount; i++)
                    if (p->Fields[i].AfterLayers) F[i] = Field(p, p->Fields + i, dir, F);

            if (flats)
                for (int i = 0; i < p->FlatCount; i++) h = ApplyFlat(p, p->Flats + i, dir, h);
            return h;
        }

        private static double ApplyFlat(TerrainProgram* p, FlatOp* a, Vector3d dir, double h)
        {
            double cos = Vector3d.Dot(dir, a->Center);
            if (cos < a->CosReach) return h;
            double dist = DetMath.Angle(dir, a->Center) * p->Radius;
            double dn = Vector3d.Dot(dir, a->Normal);
            double target = dn > 1e-6 ? a->PlaneD / dn - p->Radius : a->Height; // FlatArea.PlaneHeight
            double s = MathD.SmoothStep(a->Radius, a->Radius + a->Blend, dist);
            if (a->OuterRadius > a->Radius) s *= a->OuterRelief + (1 - a->OuterRelief) * MathD.SmoothStep(a->Radius, a->OuterRadius, dist);
            return target + (h - target) * s;
        }

        private static double Product(TerrainProgram* p, int start, int count, ref Ctx c)
        {
            double m = 1;
            for (int i = 0; i < count && m != 0; i++) m *= Term(p, p->TermRefs[start + i], ref c);
            return m;
        }

        private static double ApplyShape(LayeredTerrain.Shape s, double v, double a, double b)
        {
            switch (s)
            {
                case LayeredTerrain.Shape.Smooth: return MathD.SmoothStep(a, b, v);
                case LayeredTerrain.Shape.Linear: return MathD.Clamp01((v - a) / (b - a));
                default: return v;
            }
        }

        // ================================================================== fields and terms

        private static double Field(TerrainProgram* p, FieldOp* f, Vector3d d, double* F)
        {
            double v;
            if (f->Noise >= 0) v = Noise(p, p->Noises + f->Noise, d);
            else if (f->Term >= 0)
            {
                var c = new Ctx { Dir = d, F = F, Lat = double.NaN, Shore = double.NaN };
                v = Term(p, f->Term, ref c);
            }
            else if (f->SumStart >= 0)
            {
                v = 0;
                for (int i = 0; i < f->SumCount; i++)
                {
                    FieldSum s = p->FieldSums[f->SumStart + i];
                    v += F[s.Index] * s.Weight;
                }
            }
            else v = F[f->From];
            v = v * f->Scale + f->Add;
            for (int i = 0; i < f->SpotCount; i++)
            {
                LayeredTerrain.Spot* s = p->Spots + f->SpotStart + i;
                if (Vector3d.Dot(d, s->Dir) < s->CosReach) continue;
                double x = DetMath.Angle(d, s->Dir) * p->Radius / s->Radius;
                double w = DetMath.Exp(-x * x);
                if (s->Pull) v += (s->Toward - v) * MathD.Clamp01(w * s->Strength);
                else v += s->Amount * w;
            }
            return ApplyShape(f->Shape, v, f->A, f->B);
        }

        private static double Term(TerrainProgram* p, int index, ref Ctx c)
        {
            TermOp* t = p->Terms + index;
            double v;
            switch (t->Source)
            {
                case LayeredTerrain.Src.Field: v = c.F[t->Field]; break;
                case LayeredTerrain.Src.Height: v = c.Height; break;
                case LayeredTerrain.Src.Slope:
                    if (double.IsNaN(c.Slope)) c.Slope = SlopeAt(p, c.Dir, c.Height);
                    v = c.Slope;
                    break;
                case LayeredTerrain.Src.Latitude: v = Latitude(ref c); break;
                case LayeredTerrain.Src.AbsLatitude: v = Math.Abs(Latitude(ref c)); break;
                case LayeredTerrain.Src.Longitude: v = DetMath.Atan2(-c.Dir.z, c.Dir.x) * MathD.Rad2Deg; break;
                case LayeredTerrain.Src.Shore:
                    if (double.IsNaN(c.Shore)) c.Shore = ShoreDistance(p, c.Dir, c.F);
                    v = c.Shore;
                    break;
                case LayeredTerrain.Src.Near: v = DetMath.Angle(c.Dir, t->NearDir) * p->Radius; break;
                default:
                    v = 0;
                    for (int i = 0; i < t->SumCount; i++) v += Term(p, p->TermRefs[t->SumStart + i], ref c);
                    break;
            }
            if (t->Jitter >= 0)
            {
                NoiseOp* j = p->Noises + t->Jitter;
                if (JitterMatters(t, j->Amount, v)) v += Noise(p, j, c.Dir) * j->Amount;
            }
            switch (t->Shape)
            {
                case LayeredTerrain.Shape.Smooth: v = MathD.SmoothStep(t->A, t->B, v); break;
                case LayeredTerrain.Shape.Linear: v = MathD.Clamp01((v - t->A) / (t->B - t->A)); break;
                case LayeredTerrain.Shape.Range: v = (!t->HasMin || v >= t->A) && (!t->HasMax || v < t->B) ? 1 : 0; break;
                case LayeredTerrain.Shape.Below: v = v < t->A ? 1 : 0; break;
                case LayeredTerrain.Shape.AtLeast: v = v >= t->A ? 1 : 0; break;
            }
            if (t->Invert) v = 1 - v;
            if (t->Scale != 1) v *= t->Scale;
            if (t->Remap) v = t->R0 + (t->R1 - t->R0) * v;
            return v;
        }

        private static bool JitterMatters(TermOp* t, double amount, double v)
        {
            double m = 1.1 * Math.Abs(amount);
            switch (t->Shape)
            {
                case LayeredTerrain.Shape.Smooth:
                case LayeredTerrain.Shape.Linear: return v > Math.Min(t->A, t->B) - m && v < Math.Max(t->A, t->B) + m;
                case LayeredTerrain.Shape.Range: return (t->HasMin && Math.Abs(v - t->A) < m) || (t->HasMax && Math.Abs(v - t->B) < m);
                case LayeredTerrain.Shape.Below:
                case LayeredTerrain.Shape.AtLeast: return Math.Abs(v - t->A) < m;
                default: return true;
            }
        }

        private static double Latitude(ref Ctx c)
        {
            if (double.IsNaN(c.Lat)) c.Lat = DetMath.Asin(MathD.Clamp(-c.Dir.y, -1, 1)) * MathD.Rad2Deg;
            return c.Lat;
        }

        /// <summary>LayeredTerrain.SlopeAt.</summary>
        private static double SlopeAt(TerrainProgram* p, Vector3d dir, double h)
        {
            Tangents(dir, out var e1, out var e2);
            const double step = 20;
            double a = step / p->Radius;
            double h1 = SampleCore(p, Normalize(dir + e1 * a), p->Aux, false, false, false);
            double h2 = SampleCore(p, Normalize(dir + e2 * a), p->Aux, false, false, false);
            double gx = (h1 - h) / step, gy = (h2 - h) / step;
            return DetMath.Atan(Math.Sqrt(gx * gx + gy * gy)) * MathD.Rad2Deg;
        }

        /// <summary>LayeredTerrain.ShoreDistance.</summary>
        private static double ShoreDistance(TerrainProgram* p, Vector3d dir, double* F)
        {
            if (!p->HasOcean) return double.PositiveInfinity;
            double f0 = p->ShoreField >= 0 ? F[p->ShoreField] : ShoreValue(p, dir);
            Tangents(dir, out var e1, out var e2);
            const double step = 100;
            double a = step / p->Radius;
            double gx = (ShoreValue(p, Normalize(dir + e1 * a)) - ShoreValue(p, Normalize(dir - e1 * a))) / (2 * step);
            double gy = (ShoreValue(p, Normalize(dir + e2 * a)) - ShoreValue(p, Normalize(dir - e2 * a))) / (2 * step);
            double g = Math.Sqrt(gx * gx + gy * gy);
            if (g < 1e-15) return f0 >= 0 ? 1e9 : -1e9;
            return MathD.Clamp(f0 / g, -1e9, 1e9);
        }

        private static double ShoreValue(TerrainProgram* p, Vector3d d)
        {
            double* A = p->Aux;
            if (p->ShoreField < 0) return SampleCore(p, d, A, false, false, true);
            for (int i = 0; i <= p->ShoreField; i++)
                if (!p->Fields[i].AfterLayers) A[i] = Field(p, p->Fields + i, d, A);
            return A[p->ShoreField];
        }

        private static void Tangents(Vector3d dir, out Vector3d e1, out Vector3d e2)
        {
            e1 = Vector3d.Cross(dir, new Vector3d(0, 1, 0));
            if (e1.sqrMagnitude < 1e-12) e1 = Vector3d.Cross(dir, new Vector3d(1, 0, 0));
            e1 = Normalize(e1);
            e2 = Normalize(Vector3d.Cross(dir, e1));
        }

        /// <summary>Vector3d.normalized, without its static zero.</summary>
        private static Vector3d Normalize(Vector3d v)
        {
            double m = v.magnitude;
            return m > 1e-300 ? new Vector3d(v.x / m, v.y / m, v.z / m) : new Vector3d(0, 0, 0);
        }

        // ================================================================== layers

        private static double Layer(TerrainProgram* p, LayerOp* l, double h, double m, ref Ctx c)
        {
            double v;
            switch (l->Kind)
            {
                case LayeredTerrain.LayerKind.Terraces:
                {
                    double x = h / l->Step, k = Math.Floor(x), f = x - k;
                    double terraced = (k + MathD.SmoothStep(l->Sharpness, 1, f)) * l->Step;
                    return h + (terraced - h) * m;
                }
                case LayeredTerrain.LayerKind.Noise: v = Noise(p, p->Noises + l->Noise, c.Dir); break;
                case LayeredTerrain.LayerKind.Field: v = c.F[l->From]; break;
                case LayeredTerrain.LayerKind.Constant: v = l->Constant; break;
                case LayeredTerrain.LayerKind.Craters: v = Craters(p, p->Craters + l->Sub, c.Dir, c.F); break;
                case LayeredTerrain.LayerKind.Rift: v = Rift(p, p->Rifts + l->Sub, c.Dir, c.F); break;
                case LayeredTerrain.LayerKind.Canyons:
                {
                    double t = Math.Abs(Noise(p, p->Noises + l->Sub, c.Dir)) / l->CanyonWidth;
                    v = t >= 1 ? 0 : -l->CanyonDepth * DetMath.Pow(1 - MathD.SmoothStep(0, 1, t), l->CanyonSharpness);
                    break;
                }
                case LayeredTerrain.LayerKind.Ellipsoid:
                {
                    double x = c.Dir.x * l->Axes.x, y = c.Dir.y * l->Axes.y, z = c.Dir.z * l->Axes.z;
                    v = 1 / Math.Sqrt(x * x + y * y + z * z) - p->Radius;
                    break;
                }
                default: v = Dunes(p, p->Dunes + l->Sub, c.Dir); break;
            }
            v = ApplyShape(l->Shape, v, l->A, l->B);
            if (l->Abs) v = Math.Abs(v);
            if (l->Power != 1) v = DetMath.Pow(Math.Max(0, v), l->Power);
            v = v * l->Amplitude + l->Add;
            switch (l->Blend)
            {
                case LayeredTerrain.BlendMode.Max: return h + (Math.Max(h, v) - h) * m;
                case LayeredTerrain.BlendMode.Min: return h + (Math.Min(h, v) - h) * m;
                default: return h + v * m;
            }
        }

        private static double Craters(TerrainProgram* pr, CraterOp* c, Vector3d d, double* F)
        {
            double h = 0, rays = 0, floor = 0, fresh = 0;
            Vector3d p = d * c->Radius;
            for (int k = 0; k < c->SizeCount; k++)
            {
                CraterSize size = pr->CraterSizes[c->SizeStart + k];
                double s = size.Cell;
                double reach = c->Search * s;
                long x0 = (long)Math.Floor((p.x - reach) / s), x1 = (long)Math.Floor((p.x + reach) / s);
                long y0 = (long)Math.Floor((p.y - reach) / s), y1 = (long)Math.Floor((p.y + reach) / s);
                long z0 = (long)Math.Floor((p.z - reach) / s), z1 = (long)Math.Floor((p.z + reach) / s);
                int cs = unchecked(c->Seed * 31 + k * 7919);
                for (long cx = x0; cx <= x1; cx++)
                for (long cy = y0; cy <= y1; cy++)
                for (long cz = z0; cz <= z1; cz++)
                {
                    if (Noise3D.Hash01(cx, cy, cz, cs) > size.Chance) continue;
                    double jx = Noise3D.Hash01(cx, cy, cz, cs + 1);
                    double jy = Noise3D.Hash01(cx, cy, cz, cs + 2);
                    double jz = Noise3D.Hash01(cx, cy, cz, cs + 3);
                    double rr = Noise3D.Hash01(cx, cy, cz, cs + 4);
                    double r = s * (c->RMin + (c->RMax - c->RMin) * rr);
                    double dx = p.x - (cx + jx) * s, dy = p.y - (cy + jy) * s, dz = p.z - (cz + jz) * s;
                    double xn = Math.Sqrt(dx * dx + dy * dy + dz * dz) / r;
                    if (xn > c->Reach) continue;
                    double depth = r * size.Depth * c->DepthScale;
                    double rim = depth * c->Rim;
                    double ch;
                    if (xn < 1)
                    {
                        ch = Math.Max((xn * xn - 1) * depth, -depth * c->Floor);
                        double q = (xn - 1) / c->RimIn;
                        ch += rim * DetMath.Exp(-q * q);
                    }
                    else
                    {
                        double q = (xn - 1) / c->RimOut;
                        ch = rim * DetMath.Exp(-q * q);
                    }
                    double age = 1 - Noise3D.Hash01(cx, cy, cz, cs + 5);
                    if (c->Erosion > 0)
                    {
                        double soft = -depth * 0.5 * DetMath.Exp(-2.2 * xn * xn);
                        ch += (soft - ch) * c->Erosion * age;
                    }
                    h += ch;
                    if (age < c->Fresh)
                    {
                        rays += (1 - MathD.SmoothStep(c->RaysFrom, c->Reach, xn)) * c->Rays;
                        if (r >= c->FreshMinRadius && c->FreshField >= 0) fresh = Math.Max(fresh, 1 - MathD.SmoothStep(1.0, c->Reach, xn));
                    }
                    if (r >= c->FloorMinRadius && c->FloorField >= 0) floor = Math.Max(floor, 1 - MathD.SmoothStep(0.75, 0.95, xn));
                }
            }
            if (c->RaysField >= 0) F[c->RaysField] += rays;
            if (c->FloorField >= 0) F[c->FloorField] = Math.Max(F[c->FloorField], floor);
            if (c->FreshField >= 0) F[c->FreshField] = Math.Max(F[c->FreshField], fresh);
            return h;
        }

        private static double Rift(TerrainProgram* p, RiftOp* r, Vector3d d, double* F)
        {
            double best = double.MaxValue;
            for (int i = 0; i + 1 < r->PointCount; i++)
                best = Math.Min(best, SegmentAngle(d, p->RiftPoints[r->PointStart + i], p->RiftPoints[r->PointStart + i + 1]));
            double dist = best * r->Radius;
            if (r->Wobble >= 0)
            {
                NoiseOp* w = p->Noises + r->Wobble;
                dist += Noise(p, w, d) * w->Amount;
            }
            double t = Math.Max(0, dist) / r->HalfWidth;
            double inside = 1 - MathD.SmoothStep(r->Floor, 1, t);
            if (r->OutField >= 0) F[r->OutField] = Math.Max(F[r->OutField], inside);
            double v = -r->Depth * inside;
            if (r->Shoulders != 0)
            {
                double q = (t - 1.25) / 0.35;
                v += r->Shoulders * r->Depth * DetMath.Exp(-q * q);
            }
            return v;
        }

        private static double SegmentAngle(Vector3d d, Vector3d a, Vector3d b)
        {
            Vector3d n = Vector3d.Cross(a, b);
            double nl = n.magnitude;
            if (nl < 1e-12) return DetMath.Angle(d, a);
            n = n / nl;
            Vector3d p = d - n * Vector3d.Dot(d, n);
            if (Vector3d.Dot(Vector3d.Cross(a, p), n) >= 0 && Vector3d.Dot(Vector3d.Cross(p, b), n) >= 0)
                return Math.Abs(DetMath.Atan2(Vector3d.Dot(d, n), p.magnitude));
            return Math.Min(DetMath.Angle(d, a), DetMath.Angle(d, b));
        }

        private static double Dunes(TerrainProgram* p, DunesOp* du, Vector3d d)
        {
            double phase = DetMath.Angle(d, du->Axis) * p->Radius / du->Wavelength;
            if (du->Warp >= 0)
            {
                NoiseOp* w = p->Noises + du->Warp;
                phase += Noise(p, w, d) * w->Amount;
            }
            double f = phase - Math.Floor(phase);
            double y = f < du->Crest ? MathD.SmoothStep(0, du->Crest, f) : 1 - MathD.SmoothStep(du->Crest, 1, f);
            return y * du->Height;
        }

        // ================================================================== noise (Noise3D)

        private static double Noise(TerrainProgram* pr, NoiseOp* n, Vector3d d)
        {
            Vector3d p = d * n->Frequency;
            if (n->HasOffset) p = p + n->Offset;
            int* perm = pr->Perms + n->Perm;
            switch (n->Type)
            {
                case LayeredTerrain.NoiseType.Ridged: return Ridged(perm, p, n->Octaves, n->Lacunarity, n->Gain);
                case LayeredTerrain.NoiseType.Billow: return Billow(perm, p, n->Octaves, n->Lacunarity, n->Gain);
                case LayeredTerrain.NoiseType.Perlin: return Gradient(perm, p.x, p.y, p.z);
                default: return Fbm(perm, p, n->Octaves, n->Lacunarity, n->Gain);
            }
        }

        private static double Fade(double t) => t * t * t * (t * (t * 6 - 15) + 10);

        // Noise3D's gradient tables.
        private static readonly double[] GX = { 1, -1, 1, -1, 1, -1, 1, -1, 0, 0, 0, 0, 1, 0, -1, 0 };
        private static readonly double[] GY = { 1, 1, -1, -1, 0, 0, 0, 0, 1, -1, 1, -1, 1, -1, 1, -1 };
        private static readonly double[] GZ = { 0, 0, 0, 0, 1, 1, -1, -1, 1, 1, -1, -1, 0, 1, 0, -1 };

        private static double Grad(int hash, double x, double y, double z)
        {
            int h = hash & 15;
            return GX[h] * x + GY[h] * y + GZ[h] * z;
        }

        private static double Lerp(double t, double a, double b) => a + t * (b - a);

        /// <summary>Noise3D.Sample.</summary>
        private static double Gradient(int* _p, double x, double y, double z)
        {
            long ix = (long)x, iy = (long)y, iz = (long)z;
            if (x < ix) ix--;
            if (y < iy) iy--;
            if (z < iz) iz--;
            int X = (int)(ix & 255), Y = (int)(iy & 255), Z = (int)(iz & 255);
            x -= ix; y -= iy; z -= iz;
            double u = Fade(x), v = Fade(y), w = Fade(z);
            int A = _p[X] + Y, AA = _p[A] + Z, AB = _p[A + 1] + Z;
            int B = _p[X + 1] + Y, BA = _p[B] + Z, BB = _p[B + 1] + Z;

            double x1 = x - 1, y1 = y - 1, z1 = z - 1;
            double l1 = Lerp(u, Grad(_p[AA], x, y, z), Grad(_p[BA], x1, y, z));
            double l2 = Lerp(u, Grad(_p[AB], x, y1, z), Grad(_p[BB], x1, y1, z));
            double l3 = Lerp(u, Grad(_p[AA + 1], x, y, z1), Grad(_p[BA + 1], x1, y, z1));
            double l4 = Lerp(u, Grad(_p[AB + 1], x, y1, z1), Grad(_p[BB + 1], x1, y1, z1));
            return Lerp(w, Lerp(v, l1, l2), Lerp(v, l3, l4));
        }

        private static double Fbm(int* perm, Vector3d p, int octaves, double lacunarity, double gain)
        {
            double sum = 0, amp = 1, norm = 0;
            double x = p.x, y = p.y, z = p.z;
            for (int i = 0; i < octaves; i++)
            {
                sum += amp * Gradient(perm, x, y, z);
                norm += amp;
                amp *= gain;
                x *= lacunarity; y *= lacunarity; z *= lacunarity;
                x += 17.13; y += 31.71; z += 7.77;
            }
            return sum / norm;
        }

        private static double Ridged(int* perm, Vector3d p, int octaves, double lacunarity, double gain)
        {
            double sum = 0, amp = 1, norm = 0, weight = 1;
            double x = p.x, y = p.y, z = p.z;
            for (int i = 0; i < octaves; i++)
            {
                double n = 1.0 - Math.Abs(Gradient(perm, x, y, z));
                n *= n;
                n *= weight;
                weight = MathD.Clamp01(n * 2.0);
                sum += amp * n;
                norm += amp;
                amp *= gain;
                x *= lacunarity; y *= lacunarity; z *= lacunarity;
                x += 5.31; y += 11.17; z += 23.3;
            }
            return sum / norm;
        }

        private static double Billow(int* perm, Vector3d p, int octaves, double lacunarity, double gain)
        {
            double sum = 0, amp = 1, norm = 0;
            double x = p.x, y = p.y, z = p.z;
            for (int i = 0; i < octaves; i++)
            {
                sum += amp * (2.0 * Math.Abs(Gradient(perm, x, y, z)) - 1.0);
                norm += amp;
                amp *= gain;
                x *= lacunarity; y *= lacunarity; z *= lacunarity;
                x += 13.37; y += 5.29; z += 19.61;
            }
            return sum / norm;
        }

        // ================================================================== colour (LayeredTerrain.Colorize)

        private static Color32 Colorize(TerrainProgram* p, Vector3d dir, double height, double* F, double cosSlope)
        {
            cosSlope = MathD.Clamp(cosSlope, -1, 1);
            var c = new Ctx { Dir = dir, F = F, Height = height, Lat = double.NaN, Shore = double.NaN, Slope = DetMath.Acos(cosSlope) * MathD.Rad2Deg };
            double r = 128, g = 128, b = 128;
            for (int i = 0; i < p->ColorCount; i++)
            {
                ColorOp* rule = p->Colors + i;
                double m = rule->MaskCount > 0 ? MathD.Clamp01(Product(p, rule->MaskStart, rule->MaskCount, ref c)) : 1;
                if (m <= 0) continue;
                r += (rule->Color.r - r) * m;
                g += (rule->Color.g - g) * m;
                b += (rule->Color.b - b) * m;
            }
            if (p->HasShading)
            {
                double s = 1 - p->ShadingSlope * (1 - cosSlope);
                if (p->ShadingNoise >= 0) s += Noise(p, p->Noises + p->ShadingNoise, dir) * p->ShadingAmount;
                r *= s; g *= s; b *= s;
            }
            return new Color32(ToByte(r), ToByte(g), ToByte(b), 255);
        }

        private static byte ToByte(double v) => (byte)(v <= 0 ? 0 : v >= 255 ? 255 : v + 0.5);
    }
}
