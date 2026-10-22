using System;
using System.Collections.Generic;
using UnityEngine;

namespace TAP.Core
{
    /// <summary>
    /// The terrain compiled for Burst (FND-19): the same fields, terms, layers, flat areas and colours as flat arrays of
    /// plain structs, which <see cref="TerrainKernels"/> evaluates for a whole batch of samples. It runs the same arithmetic
    /// in the same order as the classes (Burst in strict float mode), so the results are the same bits; the classes stay
    /// as the reference, the fallback and the single-point queries (heights, biomes).
    /// </summary>
    public sealed partial class LayeredTerrain
    {
        /// <summary>Batches go to the Burst kernels; false (or <c>-terrain-managed</c> on the command line) uses the classes.</summary>
        public static bool UseBurst = Array.IndexOf(Environment.GetCommandLineArgs(), "-terrain-managed") < 0;

        /// <summary>Whether Burst compiles the kernels here; without it they still run, as plain C# (correct, but slow).</summary>
        public static bool BurstCompiled => Unity.Burst.BurstCompiler.IsEnabled;

        private NoiseOp[] _noiseOps;
        private int[] _perms;
        private FieldOp[] _fieldOps;
        private FieldSum[] _fieldSums;
        private Spot[] _spots;
        private TermOp[] _termOps;
        private int[] _termRefs;
        private LayerOp[] _layerOps;
        private CraterOp[] _craterOps;
        private CraterSize[] _craterSizes;
        private RiftOp[] _riftOps;
        private Vector3d[] _riftPoints;
        private DunesOp[] _dunesOps;
        private ColorOp[] _colorOps;
        private int _shadingNoise = -1;
        private volatile FlatOp[] _flatOps = new FlatOp[0];
        [ThreadStatic] private static double[] _kernelAux;

        private void CompileProgram()
        {
            var noises = new List<NoiseOp>();
            var noiseIndex = new Dictionary<NoiseEval, int>();
            var perms = new List<int>();
            var permIndex = new Dictionary<Noise3D, int>();
            var terms = new List<TermOp>();
            var termIndex = new Dictionary<Term, int>();
            var refs = new List<int>();
            var sums = new List<FieldSum>();
            var spots = new List<Spot>();
            var craters = new List<CraterOp>();
            var sizes = new List<CraterSize>();
            var rifts = new List<RiftOp>();
            var points = new List<Vector3d>();
            var dunes = new List<DunesOp>();

            int N(NoiseEval n)
            {
                if (n == null) return -1;
                if (noiseIndex.TryGetValue(n, out int i)) return i;
                if (!permIndex.TryGetValue(n.Noise, out int perm))
                {
                    permIndex[n.Noise] = perm = perms.Count;
                    perms.AddRange(n.Noise.Permutation);
                }
                noises.Add(new NoiseOp
                {
                    Perm = perm, Type = n.Type, Frequency = n.Frequency, Lacunarity = n.Lacunarity, Gain = n.Gain, Amount = n.Amount,
                    Octaves = n.Octaves, Offset = n.Offset, HasOffset = n.HasOffset,
                });
                return noiseIndex[n] = noises.Count - 1;
            }
            // A list of terms: its first index and count in the shared reference list (a null list has count 0).
            (int start, int count) Terms(Term[] list)
            {
                if (list == null) return (0, 0);
                var ids = new int[list.Length];
                for (int i = 0; i < list.Length; i++) ids[i] = T(list[i]);
                int start = refs.Count;
                refs.AddRange(ids);
                return (start, ids.Length);
            }
            int T(Term t)
            {
                if (termIndex.TryGetValue(t, out int i)) return i;
                var (sumStart, sumCount) = Terms(t.Sum);
                terms.Add(new TermOp
                {
                    Source = t.Source, Field = t.Field, NearDir = t.NearDir, SumStart = sumStart, SumCount = sumCount, Jitter = N(t.Jitter),
                    Shape = t.Shape, A = t.A, B = t.B, HasMin = t.HasMin, HasMax = t.HasMax, Invert = t.Invert, Scale = t.Scale,
                    Remap = t.Remap, R0 = t.R0, R1 = t.R1,
                });
                return termIndex[t] = terms.Count - 1;
            }

            _fieldOps = new FieldOp[_fields.Length];
            for (int i = 0; i < _fields.Length; i++)
            {
                var f = _fields[i];
                var op = new FieldOp
                {
                    Noise = N(f.Noise), Term = f.Term != null ? T(f.Term) : -1, From = f.From, SumStart = -1, Scale = f.Scale, Add = f.Add,
                    SpotStart = spots.Count, SpotCount = f.Spots?.Length ?? 0, Shape = f.Shape, A = f.A, B = f.B,
                    AfterLayers = f.AfterLayers, ForHeight = f.ForHeight,
                };
                if (f.Sum != null)
                {
                    op.SumStart = sums.Count;
                    op.SumCount = f.Sum.Length;
                    for (int k = 0; k < f.Sum.Length; k++) sums.Add(new FieldSum { Index = f.Sum[k], Weight = f.Weights[k] });
                }
                if (f.Spots != null) spots.AddRange(f.Spots);
                _fieldOps[i] = op;
            }

            _layerOps = new LayerOp[_layers.Length];
            for (int i = 0; i < _layers.Length; i++)
            {
                var l = _layers[i];
                var (maskStart, maskCount) = Terms(l.Mask);
                var op = new LayerOp
                {
                    Kind = l.Kind, Noise = N(l.Noise), From = l.From, Sub = -1, Constant = l.Constant, Step = l.Step, Sharpness = l.Sharpness,
                    Axes = l.Axes, Shape = l.Shape, A = l.A, B = l.B, Abs = l.Abs, Power = l.Power, Amplitude = l.Amplitude, Add = l.Add,
                    MaskStart = maskStart, MaskCount = maskCount, Blend = l.Blend, Detail = l.Detail,
                };
                switch (l.Kind)
                {
                    case LayerKind.Craters:
                    {
                        var c = l.Craters;
                        op.Sub = craters.Count;
                        craters.Add(new CraterOp
                        {
                            Seed = c.Seed, SizeStart = sizes.Count, SizeCount = c.Cells.Length, RMin = c.RMin, RMax = c.RMax, Rim = c.Rim,
                            RimIn = c.RimIn, RimOut = c.RimOut, Floor = c.Floor, Reach = c.Reach, Search = c.Search, DepthScale = c.DepthScale,
                            Fresh = c.Fresh, Rays = c.Rays, RaysFrom = c.RaysFrom, Erosion = c.Erosion, FloorMinRadius = c.FloorMinRadius,
                            FreshMinRadius = c.FreshMinRadius, RaysField = c.RaysField, FloorField = c.FloorField, FreshField = c.FreshField,
                            Radius = c.Radius,
                        });
                        for (int k = 0; k < c.Cells.Length; k++) sizes.Add(new CraterSize { Cell = c.Cells[k], Chance = c.Chances[k], Depth = c.Depths[k] });
                        break;
                    }
                    case LayerKind.Rift:
                    {
                        var r = l.Rift;
                        op.Sub = rifts.Count;
                        rifts.Add(new RiftOp
                        {
                            PointStart = points.Count, PointCount = r.Points.Length, HalfWidth = r.HalfWidth, Depth = r.Depth, Floor = r.Floor,
                            Shoulders = r.Shoulders, Radius = r.Radius, Wobble = N(r.Wobble), OutField = r.OutField,
                        });
                        points.AddRange(r.Points);
                        break;
                    }
                    case LayerKind.Canyons:
                        op.Sub = N(l.Canyons.Noise);
                        op.CanyonWidth = l.Canyons.Width;
                        op.CanyonDepth = l.Canyons.Depth;
                        op.CanyonSharpness = l.Canyons.Sharpness;
                        break;
                    case LayerKind.Dunes:
                    {
                        var d = l.Dunes;
                        op.Sub = dunes.Count;
                        dunes.Add(new DunesOp { Axis = d.Axis, Wavelength = d.Wavelength, Height = d.Height, Crest = d.Crest, Warp = N(d.Warp) });
                        break;
                    }
                }
                _layerOps[i] = op;
            }

            _colorOps = new ColorOp[_colors.Length];
            for (int i = 0; i < _colors.Length; i++)
            {
                var (maskStart, maskCount) = Terms(_colors[i].Mask);
                _colorOps[i] = new ColorOp { Color = _colors[i].Color, MaskStart = maskStart, MaskCount = maskCount };
            }
            if (_shading != null) _shadingNoise = N(_shading.Noise);

            _noiseOps = noises.ToArray();
            _perms = perms.ToArray();
            _termOps = terms.ToArray();
            _termRefs = refs.ToArray();
            _fieldSums = sums.ToArray();
            _spots = spots.ToArray();
            _craterOps = craters.ToArray();
            _craterSizes = sizes.ToArray();
            _riftOps = rifts.ToArray();
            _riftPoints = points.ToArray();
            _dunesOps = dunes.ToArray();
        }

        private static FlatOp[] FlatOps(FlatArea[] flats)
        {
            var ops = new FlatOp[flats.Length];
            for (int i = 0; i < flats.Length; i++)
            {
                var a = flats[i];
                ops[i] = new FlatOp
                {
                    Center = a.Center, Normal = a.Normal, Height = a.Height, Radius = a.Radius, Blend = a.Blend, OuterRadius = a.OuterRadius,
                    OuterRelief = a.OuterRelief, CosReach = a.CosReach, PlaneD = a.PlaneD,
                };
            }
            return ops;
        }

        /// <summary>
        /// Heights (and fields) of many samples, like <see cref="TerrainGenerator.Sample"/> for each: sample k writes its fields at
        /// fields[k·FieldCount]; full[k] as Sample's full. The Burst kernels, unless <see cref="UseBurst"/> is off.
        /// </summary>
        public override unsafe void SampleMany(Vector3d[] dirs, bool[] full, double[] heights, double[] fields, int count)
        {
            if (!UseBurst || count == 0) { base.SampleMany(dirs, full, heights, fields, count); return; }
            var flats = _flatOps;
            var aux = Aux(ref _kernelAux);
            fixed (NoiseOp* noises = _noiseOps) fixed (int* perms = _perms) fixed (FieldOp* fieldOps = _fieldOps) fixed (FieldSum* sums = _fieldSums)
            fixed (Spot* spots = _spots) fixed (TermOp* terms = _termOps) fixed (int* refs = _termRefs) fixed (LayerOp* layers = _layerOps)
            fixed (CraterOp* craters = _craterOps) fixed (CraterSize* sizes = _craterSizes) fixed (RiftOp* rifts = _riftOps)
            fixed (Vector3d* points = _riftPoints) fixed (DunesOp* dunes = _dunesOps) fixed (int* outputs = _outputs) fixed (FlatOp* flatOps = flats)
            fixed (ColorOp* colors = _colorOps) fixed (double* auxPtr = aux)
            fixed (Vector3d* d = dirs) fixed (bool* f = full) fixed (double* h = heights) fixed (double* fs = fields)
            {
                var p = Program(noises, perms, fieldOps, sums, spots, terms, refs, layers, craters, sizes, rifts, points, dunes, outputs, flatOps, flats.Length, colors, auxPtr);
                TerrainKernels.Sample(&p, d, f, h, fs, count);
            }
        }

        /// <summary>
        /// Colours of many full samples, like <see cref="TerrainGenerator.Colorize"/>: colour i is sample samples[i] (its direction,
        /// height and fields) with the ground's tilt cosSlopes[i].
        /// </summary>
        public override unsafe void ColorizeMany(Vector3d[] dirs, double[] heights, double[] fields, int[] samples, double[] cosSlopes, Color32[] colors, int count)
        {
            if (!UseBurst || count == 0) { base.ColorizeMany(dirs, heights, fields, samples, cosSlopes, colors, count); return; }
            var flats = _flatOps;
            var aux = Aux(ref _kernelAux);
            fixed (NoiseOp* noises = _noiseOps) fixed (int* perms = _perms) fixed (FieldOp* fieldOps = _fieldOps) fixed (FieldSum* sums = _fieldSums)
            fixed (Spot* spots = _spots) fixed (TermOp* terms = _termOps) fixed (int* refs = _termRefs) fixed (LayerOp* layers = _layerOps)
            fixed (CraterOp* craters = _craterOps) fixed (CraterSize* sizes = _craterSizes) fixed (RiftOp* rifts = _riftOps)
            fixed (Vector3d* points = _riftPoints) fixed (DunesOp* dunes = _dunesOps) fixed (int* outputs = _outputs) fixed (FlatOp* flatOps = flats)
            fixed (ColorOp* colorOps = _colorOps) fixed (double* auxPtr = aux)
            fixed (Vector3d* d = dirs) fixed (double* h = heights) fixed (double* fs = fields) fixed (int* idx = samples) fixed (double* cs = cosSlopes)
            fixed (Color32* outColors = colors)
            {
                var p = Program(noises, perms, fieldOps, sums, spots, terms, refs, layers, craters, sizes, rifts, points, dunes, outputs, flatOps, flats.Length, colorOps, auxPtr);
                TerrainKernels.Colorize(&p, d, h, fs, idx, cs, outColors, count);
            }
        }

        private double[] Aux(ref double[] scratch) => scratch != null && scratch.Length >= Math.Max(1, FieldCount) ? scratch : (scratch = new double[Math.Max(16, FieldCount)]);

        private unsafe TerrainProgram Program(NoiseOp* noises, int* perms, FieldOp* fieldOps, FieldSum* sums, Spot* spots, TermOp* terms, int* refs,
            LayerOp* layers, CraterOp* craters, CraterSize* sizes, RiftOp* rifts, Vector3d* points, DunesOp* dunes, int* outputs, FlatOp* flats,
            int flatCount, ColorOp* colors, double* aux) => new TerrainProgram
        {
            Noises = noises, Perms = perms, Fields = fieldOps, FieldCount = _fieldOps.Length, AllFieldCount = FieldCount, FieldSums = sums, Spots = spots,
            Terms = terms, TermRefs = refs, Layers = layers, LayerCount = _layerOps.Length, Craters = craters, CraterSizes = sizes, Rifts = rifts,
            RiftPoints = points, Dunes = dunes, Outputs = outputs, OutputCount = _outputs.Length, Flats = flats, FlatCount = flatCount,
            Colors = colors, ColorCount = _colorOps.Length, HasShading = _shading != null, ShadingNoise = _shadingNoise,
            ShadingAmount = _shading?.Amount ?? 0, ShadingSlope = _shading?.SlopeDarkening ?? 0, Radius = Radius, SeaLevel = _seaLevel,
            HasOcean = HasOcean, ShoreField = _shoreField, Aux = aux,
        };
    }
}
