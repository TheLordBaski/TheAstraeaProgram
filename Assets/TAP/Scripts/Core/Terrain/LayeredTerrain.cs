using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace TAP.Core
{
    /// <summary>
    /// The terrain of any body, built from a <see cref="TerrainDefinition"/>: fields, layers, flat areas, colours and
    /// biomes (see the definition for the format). Deterministic: the same definition gives the same bits on every
    /// platform (plain arithmetic and <see cref="DetMath"/>, no System.Math transcendentals).
    /// </summary>
    public sealed class LayeredTerrain : TerrainGenerator
    {
        public readonly TerrainDefinition Def;
        public readonly string BodyId;
        private readonly int _seed;
        private readonly string[] _fieldIds;
        private readonly Field[] _fields;
        private readonly Layer[] _layers;
        private readonly int[] _outputs;
        private readonly ColorRule[] _colors;
        private readonly Shading _shading;
        private readonly Biome[] _biomes;
        private readonly Term[][] _biomeRules;
        private readonly Dictionary<string, Vector3d> _places = new Dictionary<string, Vector3d>();
        private readonly Dictionary<int, Noise3D> _noises = new Dictionary<int, Noise3D>();
        private readonly int _shoreField = -1;
        private readonly double _seaLevel;
        private volatile FlatArea[] _flats = new FlatArea[0];
        private readonly object _flatLock = new object();

        public override int FieldCount => _fieldIds.Length;
        public override IReadOnlyList<Biome> Biomes => _biomes;
        public IReadOnlyList<string> FieldIds => _fieldIds;
        public IReadOnlyList<FlatArea> FlatAreas => _flats;

        public LayeredTerrain(TerrainDefinition def, double radius, bool hasOcean, string bodyId = null) : base(radius, hasOcean)
        {
            Def = def;
            BodyId = bodyId;
            _seed = def.seed;
            MinHeight = def.minHeight;
            MaxHeight = def.maxHeight;
            _seaLevel = def.seaLevel;

            // Field ids: the listed fields, then the layers' outputs.
            var ids = new List<string>();
            if (def.fields != null)
                foreach (var f in def.fields)
                {
                    if (string.IsNullOrEmpty(f.id)) throw Error("a field has no id");
                    if (ids.Contains(f.id)) throw Error($"field \"{f.id}\" is defined twice");
                    ids.Add(f.id);
                }
            if (def.layers != null)
                foreach (var l in def.layers)
                {
                    var outs = l.craters != null ? new[] { l.craters.raysField, l.craters.floorField, l.craters.freshField }
                        : l.rift != null ? new[] { l.rift.field } : new string[0];
                    foreach (var o in outs)
                        if (!string.IsNullOrEmpty(o))
                        {
                            if (ids.Contains(o)) throw Error($"layer output \"{o}\" clashes with a field of that name");
                            ids.Add(o);
                        }
                }
            _fieldIds = ids.ToArray();

            // Places: flat areas and named points, for "near" terms.
            if (def.flatAreas != null)
                foreach (var a in def.flatAreas)
                    if (!string.IsNullOrEmpty(a.id)) _places[a.id] = Geo.FromLatLon(a.lat, a.lon);
            if (def.places != null)
                foreach (var p in def.places)
                    if (!string.IsNullOrEmpty(p.id)) _places[p.id] = Geo.FromLatLon(p.lat, p.lon);

            // Fields.
            var outputs = new List<int>();
            _fields = new Field[def.fields?.Count ?? 0];
            for (int i = 0; i < _fields.Length; i++) _fields[i] = CompileField(def.fields[i], i);
            // Layers.
            _layers = new Layer[def.layers?.Count ?? 0];
            for (int i = 0; i < _layers.Length; i++) _layers[i] = CompileLayer(def.layers[i], i, outputs);
            _outputs = outputs.ToArray();
            // A field that reads a layer's output (directly or through another field) is worked out after the layers.
            for (int i = 0; i < _fields.Length; i++)
                foreach (int src in _fields[i].Sources)
                    if (src >= _fields.Length || _fields[src].AfterLayers) _fields[i].AfterLayers = true;
            // What the height needs: fields read by the layers (and what those fields read).
            var needed = new bool[_fieldIds.Length];
            foreach (var l in _layers) l.MarkNeeds(needed);
            for (int i = _fields.Length - 1; i >= 0; i--)
                if (needed[i])
                    foreach (int src in _fields[i].Sources) needed[src] = true;
            for (int i = 0; i < _fields.Length; i++)
            {
                _fields[i].ForHeight = needed[i];
                if (_fields[i].AfterLayers && needed[i]) throw Error($"field \"{_fields[i].Id}\" reads a layer's output but a layer needs it");
            }

            if (def.shore != null && !string.IsNullOrEmpty(def.shore.field))
            {
                _shoreField = FieldIndex(def.shore.field, "shore");
                if (_shoreField >= _fields.Length || _fields[_shoreField].AfterLayers)
                    throw Error("the shore field must not depend on a layer's output");
            }

            _colors = new ColorRule[def.colors?.Count ?? 0];
            for (int i = 0; i < _colors.Length; i++)
            {
                var c = def.colors[i];
                _colors[i] = new ColorRule { Color = ParseColor(c.color, $"colour {i + 1}"), Mask = CompileTerms(c.mask, $"colour {i + 1}") };
            }
            if (def.shading != null)
                _shading = new Shading { Noise = def.shading.noise != null ? CompileNoise(def.shading.noise) : null, Amount = def.shading.amount, SlopeDarkening = def.shading.slopeDarkening };

            int nb = def.biomes?.Count ?? 0;
            _biomes = new Biome[Math.Max(1, nb)];
            _biomeRules = new Term[_biomes.Length][];
            if (nb == 0)
                _biomes[0] = new Biome { Id = "surface", Name = "Surface", Color = new Color32(128, 128, 128, 255) };
            for (int i = 0; i < nb; i++)
            {
                var b = def.biomes[i];
                if (string.IsNullOrEmpty(b.id)) throw Error($"biome {i + 1} has no id");
                _biomes[i] = new Biome { Id = b.id, Name = string.IsNullOrEmpty(b.name) ? b.id : b.name, Color = ParseColor(b.color, $"biome \"{b.id}\""), Material = b.material, Index = i };
                _biomeRules[i] = CompileTerms(b.when, $"biome \"{b.id}\"");
            }

            // Flat areas from the definition (a fitted one samples the ground made so far).
            if (def.flatAreas != null)
                foreach (var a in def.flatAreas) AddFlatArea(a, notify: false);
        }

        private Exception Error(string message) => new FormatException($"Terrain of {BodyId ?? "a body"}: {message}");

        private int FieldIndex(string id, string where)
        {
            int i = Array.IndexOf(_fieldIds, id);
            if (i < 0) throw Error($"{where} reads the field \"{id}\", which is not defined");
            return i;
        }

        private Noise3D NoiseFor(int seed)
        {
            if (!_noises.TryGetValue(seed, out var n)) _noises[seed] = n = new Noise3D(seed);
            return n;
        }

        // ================================================================== sampling

        public override double Sample(Vector3d dir, double[] fields, int offset, bool full) => SampleCore(dir, fields, offset, full, true, false);

        /// <summary>
        /// The height at a direction. flats: apply flat areas (biomes read the natural ground without them). noDetail:
        /// leave out layers marked as detail (for distances to the shore).
        /// </summary>
        private double SampleCore(Vector3d dir, double[] F, int O, bool full, bool flats, bool noDetail)
        {
            for (int i = 0; i < _fields.Length; i++)
            {
                var f = _fields[i];
                if (f.AfterLayers || (!full && !f.ForHeight)) continue;
                F[O + i] = f.Eval(dir, F, O, this);
            }
            foreach (int o in _outputs) F[O + o] = 0;

            var c = new Ctx { Dir = dir, F = F, O = O, Slope = 0, Lat = double.NaN, Shore = double.NaN };
            double h = 0;
            for (int i = 0; i < _layers.Length; i++)
            {
                var l = _layers[i];
                if (noDetail && l.Detail) continue;
                c.Height = h;
                double m = 1;
                if (l.Mask != null)
                {
                    m = Product(l.Mask, ref c);
                    if (m == 0) continue;
                }
                h = l.Apply(h, m, ref c, this);
            }
            h -= _seaLevel;

            if (full)
                for (int i = 0; i < _fields.Length; i++)
                    if (_fields[i].AfterLayers) F[O + i] = _fields[i].Eval(dir, F, O, this);

            if (flats)
            {
                var areas = _flats;
                for (int i = 0; i < areas.Length; i++) h = ApplyFlat(areas[i], dir, h);
            }
            return h;
        }

        private double ApplyFlat(FlatArea a, Vector3d dir, double h)
        {
            double cos = Vector3d.Dot(dir, a.Center);
            if (cos < a.CosReach) return h;
            double dist = DetMath.Angle(dir, a.Center) * Radius;
            double target = a.PlaneHeight(dir, Radius);
            double s = MathD.SmoothStep(a.Radius, a.Radius + a.Blend, dist);
            if (a.OuterRadius > a.Radius) s *= a.OuterRelief + (1 - a.OuterRelief) * MathD.SmoothStep(a.Radius, a.OuterRadius, dist);
            return target + (h - target) * s;
        }

        private static double Product(Term[] terms, ref Ctx c)
        {
            double m = 1;
            for (int i = 0; i < terms.Length && m != 0; i++) m *= terms[i].Eval(ref c);
            return m;
        }

        // ================================================================== colour

        public override Color32 Colorize(Vector3d dir, double height, double[] fields, int offset, double cosSlope)
        {
            cosSlope = MathD.Clamp(cosSlope, -1, 1);
            var c = new Ctx
            {
                Dir = dir, F = fields, O = offset, Height = height, Lat = double.NaN, Shore = double.NaN,
                Slope = DetMath.Acos(cosSlope) * MathD.Rad2Deg,
            };
            double r = 128, g = 128, b = 128;
            for (int i = 0; i < _colors.Length; i++)
            {
                var rule = _colors[i];
                double m = rule.Mask != null ? MathD.Clamp01(Product(rule.Mask, ref c)) : 1;
                if (m <= 0) continue;
                r += (rule.Color.r - r) * m;
                g += (rule.Color.g - g) * m;
                b += (rule.Color.b - b) * m;
            }
            if (_shading != null)
            {
                double s = 1 - _shading.SlopeDarkening * (1 - cosSlope);
                if (_shading.Noise != null) s += _shading.Noise.Eval(dir) * _shading.Amount;
                r *= s; g *= s; b *= s;
            }
            return new Color32(ToByte(r), ToByte(g), ToByte(b), 255);
        }

        private static byte ToByte(double v) => (byte)(v <= 0 ? 0 : v >= 255 ? 255 : v + 0.5);

        // ================================================================== biomes

        [ThreadStatic] private static double[] _biomeScratch, _auxScratch;

        public override Biome BiomeAt(Vector3d dir)
        {
            dir = dir.normalized;
            var F = _biomeScratch;
            if (F == null || F.Length < FieldCount) _biomeScratch = F = new double[Math.Max(FieldCount, 16)];
            double h = SampleCore(dir, F, 0, true, false, false);
            var c = new Ctx { Dir = dir, F = F, O = 0, Height = h, Slope = double.NaN, Lat = double.NaN, Shore = double.NaN };
            for (int i = 0; i < _biomes.Length; i++)
            {
                var rule = _biomeRules[i];
                bool ok = true;
                if (rule != null)
                    for (int k = 0; k < rule.Length && ok; k++) ok = rule[k].Eval(ref c) >= 0.5;
                if (ok) return _biomes[i];
            }
            return _biomes[_biomes.Length - 1];
        }

        private double[] Aux()
        {
            var f = _auxScratch;
            if (f == null || f.Length < FieldCount) _auxScratch = f = new double[Math.Max(FieldCount, 16)];
            return f;
        }

        /// <summary>Tilt of the natural ground (degrees), from heights 20 m around the point.</summary>
        internal double SlopeAt(Vector3d dir, double h)
        {
            var F = Aux();
            Tangents(dir, out var e1, out var e2);
            const double step = 20;
            double a = step / Radius;
            double h1 = SampleCore((dir + e1 * a).normalized, F, 0, false, false, false);
            double h2 = SampleCore((dir + e2 * a).normalized, F, 0, false, false, false);
            double gx = (h1 - h) / step, gy = (h2 - h) / step;
            return DetMath.Atan(Math.Sqrt(gx * gx + gy * gy)) * MathD.Rad2Deg;
        }

        /// <summary>
        /// Signed distance to the shore (m, positive on land): the shore field (or the height without detail layers) over
        /// its gradient, sampled 100 m around the point. Exact at the coast, a fair guess tens of kilometres out.
        /// </summary>
        internal double ShoreDistance(Vector3d dir, double[] F, int O)
        {
            if (!HasOcean) return double.PositiveInfinity;
            var A = Aux();
            double ShoreValue(Vector3d d)
            {
                if (_shoreField < 0) return SampleCore(d, A, 0, false, false, true);
                for (int i = 0; i <= _shoreField; i++)
                    if (!_fields[i].AfterLayers) A[i] = _fields[i].Eval(d, A, 0, this);
                return A[_shoreField];
            }
            double f0 = _shoreField >= 0 ? F[O + _shoreField] : ShoreValue(dir);
            Tangents(dir, out var e1, out var e2);
            const double step = 100;
            double a = step / Radius;
            double gx = (ShoreValue((dir + e1 * a).normalized) - ShoreValue((dir - e1 * a).normalized)) / (2 * step);
            double gy = (ShoreValue((dir + e2 * a).normalized) - ShoreValue((dir - e2 * a).normalized)) / (2 * step);
            double g = Math.Sqrt(gx * gx + gy * gy);
            if (g < 1e-15) return f0 >= 0 ? 1e9 : -1e9;
            return MathD.Clamp(f0 / g, -1e9, 1e9);
        }

        private static void Tangents(Vector3d dir, out Vector3d e1, out Vector3d e2)
        {
            e1 = Vector3d.Cross(dir, Vector3d.up);
            if (e1.sqrMagnitude < 1e-12) e1 = Vector3d.Cross(dir, Vector3d.right);
            e1 = e1.normalized;
            e2 = Vector3d.Cross(dir, e1).normalized;
        }

        // ================================================================== flat areas

        /// <summary>Adds (or replaces, by id) a flat area. Returns it; call on the main thread.</summary>
        public FlatArea AddFlatArea(FlatAreaDef def, bool notify = true)
        {
            var a = new FlatArea
            {
                Id = def.id, Name = def.name, Center = Geo.FromLatLon(def.lat, def.lon), Height = def.height,
                Radius = Math.Max(0, def.radius), Blend = Math.Max(1, def.blend), OuterRadius = def.outerRadius,
                OuterRelief = def.outerRadius > 0 ? MathD.Clamp01(def.outerRelief) : 1,
            };
            if (def.fit || def.level) FitPlane(a, def.level, def.maxSlope);
            else if (def.normal != null && def.normal.Length == 3) a.Normal = new Vector3d(def.normal[0], def.normal[1], def.normal[2]);
            else a.Normal = TiltedNormal(a.Center, def.slope, def.slopeAzimuth);
            a.Prepare(Radius);
            lock (_flatLock)
            {
                var list = new List<FlatArea>(_flats);
                int i = string.IsNullOrEmpty(a.Id) ? -1 : list.FindIndex(x => x.Id == a.Id);
                if (i >= 0) list[i] = a;
                else list.Add(a);
                _flats = list.ToArray();
            }
            if (!string.IsNullOrEmpty(a.Id)) _places[a.Id] = a.Center;
            if (notify) RaiseEdited(a.Center, a.Reach / Radius);
            return a;
        }

        /// <summary>
        /// Flattens the ground for a base: a plane fitted to the land under a circle of the given radius (m), tilted with
        /// it up to maxSlope (degrees), or level. It blends back into the land over blend metres.
        /// </summary>
        public FlatArea Flatten(string id, Vector3d centerDir, double radius, double blend, bool level = false, double maxSlope = 20)
        {
            Geo.ToLatLon(centerDir.normalized, out double lat, out double lon);
            return AddFlatArea(new FlatAreaDef { id = id, lat = lat, lon = lon, radius = radius, blend = blend, fit = !level, level = level, maxSlope = maxSlope });
        }

        public bool RemoveFlatArea(string id)
        {
            FlatArea removed = null;
            lock (_flatLock)
            {
                var list = new List<FlatArea>(_flats);
                int i = list.FindIndex(x => x.Id == id);
                if (i < 0) return false;
                removed = list[i];
                list.RemoveAt(i);
                _flats = list.ToArray();
            }
            RaiseEdited(removed.Center, removed.Reach / Radius);
            return true;
        }

        private static Vector3d TiltedNormal(Vector3d up, double slopeDeg, double azimuthDeg)
        {
            if (slopeDeg == 0) return up;
            Vector3d north = Geo.North(up), east = Geo.East(up);
            double az = azimuthDeg * MathD.Deg2Rad, s = slopeDeg * MathD.Deg2Rad;
            Vector3d downhill = north * DetMath.Cos(az) + east * DetMath.Sin(az);
            // A plane sloping down towards "downhill" leans its normal that way.
            return (up * DetMath.Cos(s) + downhill * DetMath.Sin(s)).normalized;
        }

        /// <summary>Least-squares plane through the ground (as it is now) under the area's circle.</summary>
        private void FitPlane(FlatArea a, bool level, double maxSlopeDeg)
        {
            Vector3d up = a.Center.normalized, north = Geo.North(up), east = Geo.East(up);
            double r = Math.Max(a.Radius, 5);
            // Rings of samples, uniform over the disc: sums for h = h0 + gx·x + gy·y in local metres.
            double sx = 0, sy = 0, sxx = 0, syy = 0, sxy = 0, sh = 0, sxh = 0, syh = 0;
            int n = 0;
            for (int ring = 0; ring <= 6; ring++)
            {
                double rr = r * ring / 6.0;
                int count = ring == 0 ? 1 : ring * 8;
                for (int k = 0; k < count; k++)
                {
                    double ang = DetMath.Pi * 2 * k / count;
                    double x = rr * DetMath.Cos(ang), y = rr * DetMath.Sin(ang);
                    Vector3d d = (up + (east * x + north * y) / Radius).normalized;
                    double h = Height(d);
                    if (HasOcean && h < 0) h = 0;
                    sx += x; sy += y; sxx += x * x; syy += y * y; sxy += x * y; sh += h; sxh += x * h; syh += y * h;
                    n++;
                }
            }
            double mx = sx / n, my = sy / n, mh = sh / n;
            double cxx = sxx / n - mx * mx, cyy = syy / n - my * my, cxy = sxy / n - mx * my;
            double cxh = sxh / n - mx * mh, cyh = syh / n - my * mh;
            double det = cxx * cyy - cxy * cxy;
            double gx = 0, gy = 0;
            if (!level && det > 1e-9)
            {
                gx = (cxh * cyy - cyh * cxy) / det;
                gy = (cyh * cxx - cxh * cxy) / det;
                double maxRad = MathD.Clamp(maxSlopeDeg, 0, 80) * MathD.Deg2Rad;
                double g = Math.Sqrt(gx * gx + gy * gy), gMax = DetMath.Sin(maxRad) / DetMath.Cos(maxRad);
                if (g > gMax && g > 0) { gx *= gMax / g; gy *= gMax / g; }
            }
            a.Height = mh - gx * mx - gy * my;
            // The surface h = h0 + gx·x + gy·y has the normal (−gx, −gy, 1) in (east, north, up).
            a.Normal = (up - east * gx - north * gy).normalized;
        }

        // ================================================================== compiling

        private Field CompileField(TerrainFieldDef d, int index)
        {
            string where = $"field \"{d.id}\"";
            var f = new Field { Id = d.id, Scale = d.scale, Add = d.add };
            int sources = (d.noise != null ? 1 : 0) + (d.from != null ? 1 : 0) + (d.sum != null ? 1 : 0) + (d.term != null ? 1 : 0);
            if (sources != 1) throw Error($"{where} needs exactly one of noise, from, sum or term");
            var src = new List<int>();
            if (d.noise != null) f.Noise = CompileNoise(d.noise);
            if (d.term != null)
            {
                f.Term = CompileTerm(d.term, where);
                if (f.Term.Reads(Src.Height) || f.Term.Reads(Src.Slope) || f.Term.Reads(Src.Shore))
                    throw Error($"{where}: a field's term cannot read the height, slope or shore (they come after the fields)");
                var reads = new bool[_fieldIds.Length];
                f.Term.MarkNeeds(reads);
                for (int i = 0; i < reads.Length; i++)
                    if (reads[i]) src.Add(i);
            }
            int listed = Def.fields?.Count ?? 0;
            // A field reads fields listed before it, or the layers' outputs (it is then worked out after the layers).
            bool Ok(int i) => i < index || i >= listed;
            if (d.from != null)
            {
                f.From = FieldIndex(d.from, where);
                if (!Ok(f.From)) throw Error($"{where} reads \"{d.from}\", which must come before it");
                src.Add(f.From);
            }
            if (d.sum != null)
            {
                f.Sum = new int[d.sum.Count];
                f.Weights = new double[d.sum.Count];
                for (int i = 0; i < d.sum.Count; i++)
                {
                    f.Sum[i] = FieldIndex(d.sum[i].of, where);
                    if (!Ok(f.Sum[i])) throw Error($"{where} reads \"{d.sum[i].of}\", which must come before it");
                    f.Weights[i] = d.sum[i].weight;
                    src.Add(f.Sum[i]);
                }
            }
            foreach (int i in src)
                if (!Ok(i)) throw Error($"{where} reads \"{_fieldIds[i]}\", which must come before it");
            f.Sources = src.ToArray();
            if (d.spots != null)
            {
                f.Spots = new Spot[d.spots.Count];
                for (int i = 0; i < d.spots.Count; i++)
                {
                    var s = d.spots[i];
                    if (s.radius <= 0) throw Error($"{where}: spot {i + 1} needs a radius");
                    f.Spots[i] = new Spot
                    {
                        Dir = Geo.FromLatLon(s.lat, s.lon), Radius = s.radius, Amount = s.amount, Pull = s.toward.HasValue,
                        Toward = s.toward ?? 0, Strength = s.strength, CosReach = DetMath.Cos(Math.Min(Math.PI, 5 * s.radius / Radius)),
                    };
                }
            }
            f.Shape = ShapeOf(d.smooth, d.linear, null, null, null, where, out f.A, out f.B, out _, out _);
            return f;
        }

        private NoiseEval CompileNoise(NoiseDef d)
        {
            var n = new NoiseEval
            {
                Noise = NoiseFor(unchecked(_seed + d.seed)),
                Frequency = d.frequency,
                Octaves = Math.Max(1, d.octaves),
                Gain = d.gain,
                Amount = d.amount,
            };
            switch ((d.type ?? "fbm").ToLowerInvariant())
            {
                case "fbm": n.Type = NoiseType.Fbm; break;
                case "ridged": n.Type = NoiseType.Ridged; break;
                case "billow": n.Type = NoiseType.Billow; break;
                case "perlin": n.Type = NoiseType.Perlin; break;
                default: throw Error($"unknown noise type \"{d.type}\" (fbm, ridged, billow or perlin)");
            }
            n.Lacunarity = d.lacunarity ?? (n.Type == NoiseType.Ridged ? 2.1 : 2.0);
            if (d.offset != null)
            {
                if (d.offset.Length != 3) throw Error("a noise offset needs three numbers");
                n.Offset = new Vector3d(d.offset[0], d.offset[1], d.offset[2]);
                n.HasOffset = true;
            }
            return n;
        }

        private Layer CompileLayer(TerrainLayerDef d, int index, List<int> outputs)
        {
            string where = $"layer {index + 1}" + (string.IsNullOrEmpty(d.note) ? "" : $" ({d.note})");
            var l = new Layer { Power = d.power, Amplitude = d.amplitude, Add = d.add, Abs = d.abs, Detail = d.detail };
            int sources = (d.noise != null ? 1 : 0) + (d.from != null ? 1 : 0) + (d.constant.HasValue ? 1 : 0) + (d.craters != null ? 1 : 0)
                          + (d.rift != null ? 1 : 0) + (d.canyons != null ? 1 : 0) + (d.terraces != null ? 1 : 0) + (d.dunes != null ? 1 : 0)
                          + (d.ellipsoid != null ? 1 : 0);
            if (sources != 1) throw Error($"{where} needs exactly one source: noise, from, constant, craters, rift, canyons, terraces, dunes or ellipsoid");
            if (d.noise != null) { l.Kind = LayerKind.Noise; l.Noise = CompileNoise(d.noise); }
            else if (d.from != null) { l.Kind = LayerKind.Field; l.From = FieldIndex(d.from, where); }
            else if (d.constant.HasValue) { l.Kind = LayerKind.Constant; l.Constant = d.constant.Value; }
            else if (d.ellipsoid != null)
            {
                if (d.ellipsoid.Length != 3 || d.ellipsoid[0] <= 0 || d.ellipsoid[1] <= 0 || d.ellipsoid[2] <= 0)
                    throw Error($"{where}: an ellipsoid needs three positive semi-axes [x, y, z]");
                l.Kind = LayerKind.Ellipsoid;
                l.Axes = new Vector3d(1 / d.ellipsoid[0], 1 / d.ellipsoid[1], 1 / d.ellipsoid[2]);
            }
            else if (d.craters != null) { l.Kind = LayerKind.Craters; l.Craters = CompileCraters(d.craters, where, outputs); }
            else if (d.rift != null) { l.Kind = LayerKind.Rift; l.Rift = CompileRift(d.rift, where, outputs); }
            else if (d.canyons != null)
            {
                if (d.canyons.noise == null) throw Error($"{where}: canyons need a noise pattern");
                l.Kind = LayerKind.Canyons;
                l.Canyons = new Canyons { Noise = CompileNoise(d.canyons.noise), Width = Math.Max(1e-6, d.canyons.width), Depth = d.canyons.depth, Sharpness = Math.Max(0.1, d.canyons.sharpness) };
            }
            else if (d.terraces != null)
            {
                if (d.terraces.step <= 0) throw Error($"{where}: terraces need a step");
                l.Kind = LayerKind.Terraces;
                l.Step = d.terraces.step;
                l.Sharpness = MathD.Clamp(d.terraces.sharpness, 0, 0.98);
            }
            else
            {
                var du = d.dunes;
                if (du.wavelength <= 0) throw Error($"{where}: dunes need a wavelength");
                l.Kind = LayerKind.Dunes;
                l.Dunes = new Dunes
                {
                    Axis = Geo.FromLatLon(du.axisLat, du.axisLon), Wavelength = du.wavelength, Height = du.height,
                    Crest = MathD.Clamp(du.crest, 0.05, 0.95), Warp = du.warp != null ? CompileNoise(du.warp) : null,
                };
            }
            l.Shape = ShapeOf(d.smooth, d.linear, null, null, null, where, out l.A, out l.B, out _, out _);
            l.Mask = CompileTerms(d.mask, where);
            if (l.Mask != null)
                foreach (var t in l.Mask)
                    if (t.Reads(Src.Slope) || t.Reads(Src.Shore)) throw Error($"{where}: a layer's mask cannot use slope or shore (they come from the finished height)");
            switch ((d.blend ?? "add").ToLowerInvariant())
            {
                case "add": l.Blend = BlendMode.Add; break;
                case "max": l.Blend = BlendMode.Max; break;
                case "min": l.Blend = BlendMode.Min; break;
                default: throw Error($"{where}: unknown blend \"{d.blend}\" (add, max or min)");
            }
            return l;
        }

        private Craters CompileCraters(CraterFieldDef d, string where, List<int> outputs)
        {
            if (d.sizes == null || d.sizes.Count == 0) throw Error($"{where}: craters need sizes");
            if (d.radius == null || d.radius.Length != 2) throw Error($"{where}: crater radius needs [min, max]");
            var c = new Craters
            {
                Seed = unchecked(_seed + d.seed),
                Cells = new double[d.sizes.Count], Chances = new double[d.sizes.Count], Depths = new double[d.sizes.Count],
                RMin = d.radius[0], RMax = d.radius[1], Rim = d.rim, RimIn = Math.Max(1e-3, d.rimInside), RimOut = Math.Max(1e-3, d.rimOutside),
                Floor = d.floor, Reach = Math.Max(1.05, d.reach), DepthScale = d.depthScale, Fresh = MathD.Clamp01(d.fresh),
                Rays = d.rays, RaysFrom = d.raysFrom, Erosion = MathD.Clamp01(d.erosion), FloorMinRadius = d.floorMinRadius,
                FreshMinRadius = d.freshMinRadius,
                RaysField = string.IsNullOrEmpty(d.raysField) ? -1 : Array.IndexOf(_fieldIds, d.raysField),
                FloorField = string.IsNullOrEmpty(d.floorField) ? -1 : Array.IndexOf(_fieldIds, d.floorField),
                FreshField = string.IsNullOrEmpty(d.freshField) ? -1 : Array.IndexOf(_fieldIds, d.freshField),
                Radius = Radius,
            };
            for (int k = 0; k < d.sizes.Count; k++)
            {
                if (d.sizes[k].cell <= 0) throw Error($"{where}: crater size {k + 1} needs a cell size");
                c.Cells[k] = d.sizes[k].cell;
                c.Chances[k] = d.sizes[k].chance;
                c.Depths[k] = d.sizes[k].depth;
            }
            c.Search = d.search > 0 ? d.search : c.RMax * c.Reach;
            if (c.RaysField >= 0) outputs.Add(c.RaysField);
            if (c.FloorField >= 0) outputs.Add(c.FloorField);
            if (c.FreshField >= 0) outputs.Add(c.FreshField);
            return c;
        }

        private Rift CompileRift(RiftDef d, string where, List<int> outputs)
        {
            if (d.path == null || d.path.Count < 2) throw Error($"{where}: a rift needs a path of at least two [lat, lon] points");
            var pts = new Vector3d[d.path.Count];
            for (int i = 0; i < pts.Length; i++)
            {
                if (d.path[i] == null || d.path[i].Length != 2) throw Error($"{where}: rift points are [lat, lon]");
                pts[i] = Geo.FromLatLon(d.path[i][0], d.path[i][1]);
            }
            var rift = new Rift
            {
                Points = pts, HalfWidth = Math.Max(1, d.width * 0.5), Depth = d.depth, Floor = MathD.Clamp(d.floor, 0, 0.95),
                Shoulders = d.shoulders, Wobble = d.wobble != null ? CompileNoise(d.wobble) : null, Radius = Radius,
                OutField = string.IsNullOrEmpty(d.field) ? -1 : Array.IndexOf(_fieldIds, d.field),
            };
            if (rift.OutField >= 0) outputs.Add(rift.OutField);
            return rift;
        }

        private Term[] CompileTerms(List<TermDef> defs, string where)
        {
            if (defs == null || defs.Count == 0) return null;
            var t = new Term[defs.Count];
            for (int i = 0; i < t.Length; i++) t[i] = CompileTerm(defs[i], where);
            return t;
        }

        private Term CompileTerm(TermDef d, string where)
        {
            if (d == null) throw Error($"{where}: an empty term");
            var t = new Term { Scale = d.scale, Invert = d.invert, Terrain = this };
            int sources = (d.of != null ? 1 : 0) + (d.near != null ? 1 : 0) + (d.sum != null ? 1 : 0);
            if (sources != 1) throw Error($"{where}: a term needs exactly one of of, near or sum");
            if (d.near != null)
            {
                if (!_places.TryGetValue(d.near, out t.NearDir)) throw Error($"{where}: no flat area or place named \"{d.near}\"");
                t.Source = Src.Near;
            }
            else if (d.sum != null)
            {
                t.Source = Src.Sum;
                t.Sum = new Term[d.sum.Count];
                for (int i = 0; i < t.Sum.Length; i++) t.Sum[i] = CompileTerm(d.sum[i], where);
            }
            else
                switch (d.of)
                {
                    case "height": t.Source = Src.Height; break;
                    case "slope": t.Source = Src.Slope; break;
                    case "latitude": t.Source = Src.Latitude; break;
                    case "absLatitude": t.Source = Src.AbsLatitude; break;
                    case "longitude": t.Source = Src.Longitude; break;
                    case "shore": t.Source = Src.Shore; break;
                    default: t.Source = Src.Field; t.Field = FieldIndex(d.of, where); break;
                }
            if (d.jitter != null) t.Jitter = CompileNoise(d.jitter);
            t.Shape = ShapeOf(d.smooth, d.linear, d.range, d.below, d.atLeast, where, out t.A, out t.B, out t.HasMin, out t.HasMax);
            if (d.remap != null)
            {
                if (d.remap.Length != 2) throw Error($"{where}: remap needs [from, to]");
                t.Remap = true;
                t.R0 = d.remap[0];
                t.R1 = d.remap[1];
            }
            return t;
        }

        private Shape ShapeOf(double[] smooth, double[] linear, double?[] range, double? below, double? atLeast, string where,
            out double a, out double b, out bool hasMin, out bool hasMax)
        {
            a = b = 0;
            hasMin = hasMax = false;
            int n = (smooth != null ? 1 : 0) + (linear != null ? 1 : 0) + (range != null ? 1 : 0) + (below.HasValue ? 1 : 0) + (atLeast.HasValue ? 1 : 0);
            if (n > 1) throw Error($"{where}: use only one of smooth, linear, range, below and atLeast");
            if (smooth != null)
            {
                if (smooth.Length != 2 || smooth[0] == smooth[1]) throw Error($"{where}: smooth needs two different edges");
                a = smooth[0]; b = smooth[1];
                return Shape.Smooth;
            }
            if (linear != null)
            {
                if (linear.Length != 2 || linear[0] == linear[1]) throw Error($"{where}: linear needs two different edges");
                a = linear[0]; b = linear[1];
                return Shape.Linear;
            }
            if (range != null)
            {
                if (range.Length != 2) throw Error($"{where}: range needs [min, max] (either may be null)");
                hasMin = range[0].HasValue;
                hasMax = range[1].HasValue;
                a = range[0] ?? 0;
                b = range[1] ?? 0;
                return Shape.Range;
            }
            if (below.HasValue) { a = below.Value; return Shape.Below; }
            if (atLeast.HasValue) { a = atLeast.Value; return Shape.AtLeast; }
            return Shape.None;
        }

        private Color32 ParseColor(string hex, string where)
        {
            if (string.IsNullOrEmpty(hex)) throw Error($"{where} needs a colour");
            string s = hex.TrimStart('#');
            if (s.Length != 6 || !int.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int v))
                throw Error($"{where}: \"{hex}\" is not a #RRGGBB colour");
            return new Color32((byte)(v >> 16), (byte)(v >> 8), (byte)v, 255);
        }

        // ================================================================== evaluators

        internal struct Ctx
        {
            public Vector3d Dir;
            public double[] F;
            public int O;
            public double Height;
            /// <summary>Degrees; NaN until worked out (biomes), 0 while building heights.</summary>
            public double Slope;
            public double Lat;
            public double Shore;
        }

        private enum NoiseType { Fbm, Ridged, Billow, Perlin }
        private enum Shape { None, Smooth, Linear, Range, Below, AtLeast }
        private enum Src { Field, Height, Slope, Latitude, AbsLatitude, Longitude, Shore, Near, Sum }
        private enum LayerKind { Noise, Field, Constant, Craters, Rift, Canyons, Terraces, Dunes, Ellipsoid }
        private enum BlendMode { Add, Max, Min }

        private static double ApplyShape(Shape s, double v, double a, double b)
        {
            switch (s)
            {
                case Shape.Smooth: return MathD.SmoothStep(a, b, v);
                case Shape.Linear: return MathD.Clamp01((v - a) / (b - a));
                default: return v;
            }
        }

        private sealed class NoiseEval
        {
            public Noise3D Noise;
            public NoiseType Type;
            public double Frequency, Lacunarity, Gain, Amount;
            public int Octaves;
            public Vector3d Offset;
            public bool HasOffset;

            public double Eval(Vector3d d)
            {
                Vector3d p = d * Frequency;
                if (HasOffset) p = p + Offset;
                switch (Type)
                {
                    case NoiseType.Ridged: return Noise.Ridged(p, Octaves, Lacunarity, Gain);
                    case NoiseType.Billow: return Noise.Billow(p, Octaves, Lacunarity, Gain);
                    case NoiseType.Perlin: return Noise.Sample(p);
                    default: return Noise.Fbm(p, Octaves, Lacunarity, Gain);
                }
            }
        }

        private struct Spot
        {
            public Vector3d Dir;
            public double Radius, Amount, Toward, Strength, CosReach;
            public bool Pull;
        }

        private sealed class Field
        {
            public string Id;
            public NoiseEval Noise;
            public Term Term;
            public int From = -1;
            public int[] Sum;
            public double[] Weights;
            public int[] Sources;
            public double Scale = 1, Add;
            public Spot[] Spots;
            public Shape Shape;
            public double A, B;
            public bool AfterLayers, ForHeight;

            public double Eval(Vector3d d, double[] F, int O, LayeredTerrain t)
            {
                double v;
                if (Noise != null) v = Noise.Eval(d);
                else if (Term != null)
                {
                    var c = new Ctx { Dir = d, F = F, O = O, Lat = double.NaN, Shore = double.NaN };
                    v = Term.Eval(ref c);
                }
                else if (Sum != null)
                {
                    v = 0;
                    for (int i = 0; i < Sum.Length; i++) v += F[O + Sum[i]] * Weights[i];
                }
                else v = F[O + From];
                v = v * Scale + Add;
                if (Spots != null)
                    for (int i = 0; i < Spots.Length; i++)
                    {
                        ref var s = ref Spots[i];
                        if (Vector3d.Dot(d, s.Dir) < s.CosReach) continue;
                        double x = DetMath.Angle(d, s.Dir) * t.Radius / s.Radius;
                        double w = DetMath.Exp(-x * x);
                        if (s.Pull) v += (s.Toward - v) * MathD.Clamp01(w * s.Strength);
                        else v += s.Amount * w;
                    }
                return ApplyShape(Shape, v, A, B);
            }
        }

        private sealed class Term
        {
            public Src Source;
            public int Field = -1;
            public Vector3d NearDir;
            public Term[] Sum;
            public NoiseEval Jitter;
            public Shape Shape;
            public double A, B;
            public bool HasMin, HasMax;
            public bool Invert;
            public double Scale = 1;
            public bool Remap;
            public double R0, R1;
            public LayeredTerrain Terrain;

            public double Eval(ref Ctx c)
            {
                double v;
                switch (Source)
                {
                    case Src.Field: v = c.F[c.O + Field]; break;
                    case Src.Height: v = c.Height; break;
                    case Src.Slope:
                        if (double.IsNaN(c.Slope)) c.Slope = Terrain.SlopeAt(c.Dir, c.Height);
                        v = c.Slope;
                        break;
                    case Src.Latitude: v = Latitude(ref c); break;
                    case Src.AbsLatitude: v = Math.Abs(Latitude(ref c)); break;
                    case Src.Longitude: v = DetMath.Atan2(-c.Dir.z, c.Dir.x) * MathD.Rad2Deg; break;
                    case Src.Shore:
                        if (double.IsNaN(c.Shore)) c.Shore = Terrain.ShoreDistance(c.Dir, c.F, c.O);
                        v = c.Shore;
                        break;
                    case Src.Near: v = DetMath.Angle(c.Dir, NearDir) * Terrain.Radius; break;
                    default:
                        v = 0;
                        for (int i = 0; i < Sum.Length; i++) v += Sum[i].Eval(ref c);
                        break;
                }
                // Noise stays within ±1.04, so far from the shape's edges the jitter cannot change the result: skip it.
                if (Jitter != null && JitterMatters(v)) v += Jitter.Eval(c.Dir) * Jitter.Amount;
                switch (Shape)
                {
                    case Shape.Smooth: v = MathD.SmoothStep(A, B, v); break;
                    case Shape.Linear: v = MathD.Clamp01((v - A) / (B - A)); break;
                    case Shape.Range: v = (!HasMin || v >= A) && (!HasMax || v < B) ? 1 : 0; break;
                    case Shape.Below: v = v < A ? 1 : 0; break;
                    case Shape.AtLeast: v = v >= A ? 1 : 0; break;
                }
                if (Invert) v = 1 - v;
                if (Scale != 1) v *= Scale;
                if (Remap) v = R0 + (R1 - R0) * v;
                return v;
            }

            private bool JitterMatters(double v)
            {
                double m = 1.1 * Math.Abs(Jitter.Amount);
                switch (Shape)
                {
                    case Shape.Smooth:
                    case Shape.Linear: return v > Math.Min(A, B) - m && v < Math.Max(A, B) + m;
                    case Shape.Range: return (HasMin && Math.Abs(v - A) < m) || (HasMax && Math.Abs(v - B) < m);
                    case Shape.Below:
                    case Shape.AtLeast: return Math.Abs(v - A) < m;
                    default: return true;
                }
            }

            private static double Latitude(ref Ctx c)
            {
                // Geographic: north (−Y) positive.
                if (double.IsNaN(c.Lat)) c.Lat = DetMath.Asin(MathD.Clamp(-c.Dir.y, -1, 1)) * MathD.Rad2Deg;
                return c.Lat;
            }

            public void MarkNeeds(bool[] needed)
            {
                if (Source == Src.Field) needed[Field] = true;
                if (Sum != null) foreach (var s in Sum) s.MarkNeeds(needed);
            }

            public bool Reads(Src s)
            {
                if (Source == s) return true;
                if (Sum != null) foreach (var t in Sum) if (t.Reads(s)) return true;
                return false;
            }
        }

        private sealed class Layer
        {
            public LayerKind Kind;
            public NoiseEval Noise;
            public int From = -1;
            public double Constant;
            public Craters Craters;
            public Rift Rift;
            public Canyons Canyons;
            public Dunes Dunes;
            public double Step, Sharpness;
            /// <summary>Ellipsoid: the reciprocals of its semi-axes.</summary>
            public Vector3d Axes;
            public Shape Shape;
            public double A, B;
            public bool Abs;
            public double Power = 1, Amplitude = 1, Add;
            public Term[] Mask;
            public BlendMode Blend;
            public bool Detail;

            public void MarkNeeds(bool[] needed)
            {
                if (Kind == LayerKind.Field) needed[From] = true;
                if (Mask != null) foreach (var t in Mask) t.MarkNeeds(needed);
            }

            public double Apply(double h, double m, ref Ctx c, LayeredTerrain t)
            {
                double v;
                switch (Kind)
                {
                    case LayerKind.Terraces:
                    {
                        double x = h / Step, k = Math.Floor(x), f = x - k;
                        double terraced = (k + MathD.SmoothStep(Sharpness, 1, f)) * Step;
                        return h + (terraced - h) * m;
                    }
                    case LayerKind.Noise: v = Noise.Eval(c.Dir); break;
                    case LayerKind.Field: v = c.F[c.O + From]; break;
                    case LayerKind.Constant: v = Constant; break;
                    case LayerKind.Craters: v = Craters.Eval(c.Dir, c.F, c.O); break;
                    case LayerKind.Rift: v = Rift.Eval(c.Dir, c.F, c.O); break;
                    case LayerKind.Canyons: v = Canyons.Eval(c.Dir); break;
                    case LayerKind.Ellipsoid:
                    {
                        double x = c.Dir.x * Axes.x, y = c.Dir.y * Axes.y, z = c.Dir.z * Axes.z;
                        v = 1 / Math.Sqrt(x * x + y * y + z * z) - t.Radius;
                        break;
                    }
                    default: v = Dunes.Eval(c.Dir, t.Radius); break;
                }
                v = ApplyShape(Shape, v, A, B);
                if (Abs) v = Math.Abs(v);
                if (Power != 1) v = DetMath.Pow(Math.Max(0, v), Power);
                v = v * Amplitude + Add;
                switch (Blend)
                {
                    case BlendMode.Max: return h + (Math.Max(h, v) - h) * m;
                    case BlendMode.Min: return h + (Math.Min(h, v) - h) * m;
                    default: return h + v * m;
                }
            }
        }

        private sealed class Craters
        {
            public int Seed;
            public double[] Cells, Chances, Depths;
            public double RMin, RMax, Rim, RimIn, RimOut, Floor, Reach, Search, DepthScale, Fresh, Rays, RaysFrom, Erosion, FloorMinRadius, FreshMinRadius;
            public int RaysField = -1, FloorField = -1, FreshField = -1;
            public double Radius;

            public double Eval(Vector3d d, double[] F, int O)
            {
                double h = 0, rays = 0, floor = 0, fresh = 0;
                Vector3d p = d * Radius;
                for (int k = 0; k < Cells.Length; k++)
                {
                    double s = Cells[k];
                    double reach = Search * s;
                    long x0 = (long)Math.Floor((p.x - reach) / s), x1 = (long)Math.Floor((p.x + reach) / s);
                    long y0 = (long)Math.Floor((p.y - reach) / s), y1 = (long)Math.Floor((p.y + reach) / s);
                    long z0 = (long)Math.Floor((p.z - reach) / s), z1 = (long)Math.Floor((p.z + reach) / s);
                    int cs = unchecked(Seed * 31 + k * 7919);
                    for (long cx = x0; cx <= x1; cx++)
                    for (long cy = y0; cy <= y1; cy++)
                    for (long cz = z0; cz <= z1; cz++)
                    {
                        if (Noise3D.Hash01(cx, cy, cz, cs) > Chances[k]) continue;
                        double jx = Noise3D.Hash01(cx, cy, cz, cs + 1);
                        double jy = Noise3D.Hash01(cx, cy, cz, cs + 2);
                        double jz = Noise3D.Hash01(cx, cy, cz, cs + 3);
                        double rr = Noise3D.Hash01(cx, cy, cz, cs + 4);
                        double r = s * (RMin + (RMax - RMin) * rr);
                        double dx = p.x - (cx + jx) * s, dy = p.y - (cy + jy) * s, dz = p.z - (cz + jz) * s;
                        double xn = Math.Sqrt(dx * dx + dy * dy + dz * dz) / r;
                        if (xn > Reach) continue;
                        double depth = r * Depths[k] * DepthScale;
                        double rim = depth * Rim;
                        double ch;
                        if (xn < 1)
                        {
                            ch = Math.Max((xn * xn - 1) * depth, -depth * Floor);
                            double q = (xn - 1) / RimIn;
                            ch += rim * DetMath.Exp(-q * q);
                        }
                        else
                        {
                            double q = (xn - 1) / RimOut;
                            ch = rim * DetMath.Exp(-q * q);
                        }
                        // Age: the young keep bright rays, the old slump (with erosion) into soft, rimless dimples.
                        double age = 1 - Noise3D.Hash01(cx, cy, cz, cs + 5);
                        if (Erosion > 0)
                        {
                            double soft = -depth * 0.5 * DetMath.Exp(-2.2 * xn * xn);
                            ch += (soft - ch) * Erosion * age;
                        }
                        h += ch;
                        if (age < Fresh)
                        {
                            rays += (1 - MathD.SmoothStep(RaysFrom, Reach, xn)) * Rays;
                            if (r >= FreshMinRadius && FreshField >= 0) fresh = Math.Max(fresh, 1 - MathD.SmoothStep(1.0, Reach, xn));
                        }
                        if (r >= FloorMinRadius && FloorField >= 0) floor = Math.Max(floor, 1 - MathD.SmoothStep(0.75, 0.95, xn));
                    }
                }
                if (RaysField >= 0) F[O + RaysField] += rays;
                if (FloorField >= 0) F[O + FloorField] = Math.Max(F[O + FloorField], floor);
                if (FreshField >= 0) F[O + FreshField] = Math.Max(F[O + FreshField], fresh);
                return h;
            }
        }

        private sealed class Rift
        {
            public Vector3d[] Points;
            public double HalfWidth, Depth, Floor, Shoulders, Radius;
            public NoiseEval Wobble;
            public int OutField = -1;

            public double Eval(Vector3d d, double[] F, int O)
            {
                double best = double.MaxValue;
                for (int i = 0; i + 1 < Points.Length; i++) best = Math.Min(best, SegmentAngle(d, Points[i], Points[i + 1]));
                double dist = best * Radius;
                if (Wobble != null) dist += Wobble.Eval(d) * Wobble.Amount;
                double t = Math.Max(0, dist) / HalfWidth;
                double inside = 1 - MathD.SmoothStep(Floor, 1, t);
                if (OutField >= 0) F[O + OutField] = Math.Max(F[O + OutField], inside);
                double v = -Depth * inside;
                if (Shoulders != 0)
                {
                    double q = (t - 1.25) / 0.35;
                    v += Shoulders * Depth * DetMath.Exp(-q * q);
                }
                return v;
            }

            /// <summary>Angle (rad) from d to the great-circle arc a→b.</summary>
            private static double SegmentAngle(Vector3d d, Vector3d a, Vector3d b)
            {
                Vector3d n = Vector3d.Cross(a, b);
                double nl = n.magnitude;
                if (nl < 1e-12) return DetMath.Angle(d, a);
                n = n / nl;
                // The closest point on the full circle lies within the arc when it is on a's and b's inner sides.
                Vector3d p = d - n * Vector3d.Dot(d, n);
                if (Vector3d.Dot(Vector3d.Cross(a, p), n) >= 0 && Vector3d.Dot(Vector3d.Cross(p, b), n) >= 0)
                    return Math.Abs(DetMath.Atan2(Vector3d.Dot(d, n), p.magnitude));
                return Math.Min(DetMath.Angle(d, a), DetMath.Angle(d, b));
            }
        }

        private sealed class Canyons
        {
            public NoiseEval Noise;
            public double Width, Depth, Sharpness;

            public double Eval(Vector3d d)
            {
                double t = Math.Abs(Noise.Eval(d)) / Width;
                if (t >= 1) return 0;
                return -Depth * DetMath.Pow(1 - MathD.SmoothStep(0, 1, t), Sharpness);
            }
        }

        private sealed class Dunes
        {
            public Vector3d Axis;
            public double Wavelength, Height, Crest;
            public NoiseEval Warp;

            public double Eval(Vector3d d, double radius)
            {
                double phase = DetMath.Angle(d, Axis) * radius / Wavelength;
                if (Warp != null) phase += Warp.Eval(d) * Warp.Amount;
                double f = phase - Math.Floor(phase);
                double y = f < Crest ? MathD.SmoothStep(0, Crest, f) : 1 - MathD.SmoothStep(Crest, 1, f);
                return y * Height;
            }
        }

        private sealed class ColorRule
        {
            public Color32 Color;
            public Term[] Mask;
        }

        private sealed class Shading
        {
            public NoiseEval Noise;
            public double Amount, SlopeDarkening;
        }
    }
}
