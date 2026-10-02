using System;
using System.Threading.Tasks;
using TAP.Core;
using UnityEngine;

namespace TAP.Simulation
{
    /// <summary>
    /// Sky lookup tables shared by the editor bake and runtime fallbacks: sunlight transmittance (Bruneton's
    /// parametrisation, 256x64) and the isotropic multiple-scattering term (Hillaire 2020, 32x32). Presentation only;
    /// physics uses the body's own atmosphere model.
    /// </summary>
    public static class AtmosphereLuts
    {
        public const int TransmittanceWidth = 256, TransmittanceHeight = 64, MultiSize = 32;
        public const double GroundAlbedo = 0.3;

        public struct Params
        {
            public double Bottom, Top, RayleighHeight, Mie, MieHeight;
            public Vector3d Rayleigh, Ozone;
            public static Params Of(CelestialBody body, PlanetVisualProfile p) => new Params
            {
                Bottom = body.Radius, Top = body.Radius + (body.Atmosphere != null ? body.Atmosphere.Height : 0),
                Rayleigh = new Vector3d(p.Rayleigh.r, p.Rayleigh.g, p.Rayleigh.b), RayleighHeight = p.RayleighHeight,
                Mie = p.Mie, MieHeight = p.MieHeight, Ozone = new Vector3d(p.Ozone.r, p.Ozone.g, p.Ozone.b),
            };
        }

        static void Density(in Params a, double altitude, out Vector3d scattering, out Vector3d extinction)
        {
            altitude = Math.Max(0, altitude);
            double rd = Math.Exp(-altitude / a.RayleighHeight), md = Math.Exp(-altitude / a.MieHeight);
            double oz = Math.Max(0, 1 - Math.Abs(altitude - 25000) / 15000);
            scattering = a.Rayleigh * rd + new Vector3d(a.Mie, a.Mie, a.Mie) * md;
            // Matches the shader: Mie absorbs 11% beyond its scattering; ozone only absorbs.
            extinction = scattering + new Vector3d(a.Mie, a.Mie, a.Mie) * (0.11 * md) + a.Ozone * oz;
        }

        static double Coord(double x, int size) => 0.5 / size + x * (1 - 1.0 / size);
        static double Uncoord(double u, int size) => (u - 0.5 / size) / (1 - 1.0 / size);

        /// <summary>Lookup coordinate of the sunlight transmittance at radius r and cos(zenith) mu, as in PlanetAtmosphere.hlsl.</summary>
        public static Vector2 TransmittanceUv(double r, double mu, double bottom, double top)
        {
            double H = Math.Sqrt(Math.Max(0, top * top - bottom * bottom));
            double rho = Math.Sqrt(Math.Max(0, r * r - bottom * bottom));
            double disc = r * r * (mu * mu - 1) + top * top;
            double d = Math.Max(0, -r * mu + Math.Sqrt(Math.Max(0, disc)));
            double dMin = top - r, dMax = rho + H;
            double xMu = (d - dMin) / Math.Max(1e-3, dMax - dMin), xR = rho / Math.Max(1e-3, H);
            return new Vector2((float)Coord(Clamp01(xMu), TransmittanceWidth), (float)Coord(Clamp01(xR), TransmittanceHeight));
        }

        /// <summary>The planet's shadow softened over the sun's apparent radius (as in the shader).</summary>
        public static double SunVisibility(double r, double mu, double bottom)
        {
            double s = Clamp01(bottom / Math.Max(r, bottom));
            double horizon = -Math.Sqrt(Math.Max(0, 1 - s * s));
            double t = Clamp01((mu - horizon + 0.0045) / 0.009);
            return t * t * (3 - 2 * t);
        }

        static double Clamp01(double v) => v < 0 ? 0 : v > 1 ? 1 : v;

        public static Color[] Transmittance(Params a)
        {
            var result = new Color[TransmittanceWidth * TransmittanceHeight];
            double H = Math.Sqrt(a.Top * a.Top - a.Bottom * a.Bottom);
            Parallel.For(0, TransmittanceHeight, y =>
            {
                double rho = H * Uncoord((y + 0.5) / TransmittanceHeight, TransmittanceHeight);
                double r = Math.Sqrt(rho * rho + a.Bottom * a.Bottom);
                for (int x = 0; x < TransmittanceWidth; x++)
                {
                    double dMin = a.Top - r, dMax = rho + H;
                    double d = dMin + Uncoord((x + 0.5) / TransmittanceWidth, TransmittanceWidth) * (dMax - dMin);
                    double mu = d <= 0 ? 1 : Math.Max(-1, Math.Min(1, (H * H - rho * rho - d * d) / (2 * r * d)));
                    // Optical depth to the top of the atmosphere (trapezoid rule, dense near the start).
                    const int steps = 160;
                    var depth = new Vector3d(0, 0, 0);
                    double prevT = 0; Density(a, r - a.Bottom, out _, out var prev);
                    for (int s = 1; s <= steps; s++)
                    {
                        double k = (double)s / steps, t = d * k * k;
                        double alt = Math.Sqrt(r * r + t * t + 2 * r * mu * t) - a.Bottom;
                        Density(a, alt, out _, out var ext);
                        depth += (prev + ext) * (0.5 * (t - prevT));
                        prev = ext; prevT = t;
                    }
                    result[y * TransmittanceWidth + x] = new Color((float)Math.Exp(-depth.x), (float)Math.Exp(-depth.y), (float)Math.Exp(-depth.z), 1);
                }
            });
            return result;
        }

        /// <summary>Bilinear CPU lookup into a transmittance table made by <see cref="Transmittance"/>.</summary>
        public static Vector3d SampleTransmittance(Color[] lut, double r, double mu, in Params a)
        {
            r = Math.Max(a.Bottom, Math.Min(a.Top, r));
            var uv = TransmittanceUv(r, mu, a.Bottom, a.Top);
            double fx = uv.x * TransmittanceWidth - 0.5, fy = uv.y * TransmittanceHeight - 0.5;
            int x0 = Math.Max(0, Math.Min(TransmittanceWidth - 2, (int)Math.Floor(fx))), y0 = Math.Max(0, Math.Min(TransmittanceHeight - 2, (int)Math.Floor(fy)));
            double tx = Clamp01(fx - x0), ty = Clamp01(fy - y0);
            Color c00 = lut[y0 * TransmittanceWidth + x0], c10 = lut[y0 * TransmittanceWidth + x0 + 1];
            Color c01 = lut[(y0 + 1) * TransmittanceWidth + x0], c11 = lut[(y0 + 1) * TransmittanceWidth + x0 + 1];
            Color c = Color.Lerp(Color.Lerp(c00, c10, (float)tx), Color.Lerp(c01, c11, (float)tx), (float)ty);
            return new Vector3d(c.r, c.g, c.b) * SunVisibility(r, mu, a.Bottom);
        }

        /// <summary>
        /// Isotropic multiple scattering, Psi_ms = L2 / (1 - f_ms) over 64 directions, sun illuminance 1, including
        /// light reflected by the ground. The sky shader multiplies it by the local scattering coefficient.
        /// </summary>
        public static Color[] MultiScattering(Params a, Color[] transmittance)
        {
            var result = new Color[MultiSize * MultiSize];
            const int directions = 64, steps = 24;
            var dirs = new Vector3d[directions];
            for (int i = 0; i < directions; i++)
            {
                double z = 1 - (i + 0.5) * 2.0 / directions, rr = Math.Sqrt(Math.Max(0, 1 - z * z)), phi = i * 2.399963229728653;
                dirs[i] = new Vector3d(rr * Math.Cos(phi), z, rr * Math.Sin(phi));
            }
            Params p = a;
            Parallel.For(0, MultiSize, y =>
            {
                double r = p.Bottom + Math.Max(1, Math.Min(p.Top - p.Bottom - 1, Uncoord((y + 0.5) / MultiSize, MultiSize) * (p.Top - p.Bottom)));
                for (int x = 0; x < MultiSize; x++)
                {
                    double muS = Uncoord((x + 0.5) / MultiSize, MultiSize) * 2 - 1;
                    var sun = new Vector3d(Math.Sqrt(Math.Max(0, 1 - muS * muS)), muS, 0);
                    var origin = new Vector3d(0, r, 0);
                    var L = new double[3]; var F = new double[3];
                    var throughput = new double[3]; var scat = new double[3]; var ext = new double[3]; var sunT = new double[3];
                    foreach (var dir in dirs)
                    {
                        // Distance to the top of the atmosphere or the ground.
                        double b = Vector3d.Dot(origin, dir);
                        double cTop = r * r - p.Top * p.Top, discTop = b * b - cTop;
                        double tMax = -b + Math.Sqrt(Math.Max(0, discTop));
                        double cGround = r * r - p.Bottom * p.Bottom, discGround = b * b - cGround;
                        bool ground = b < 0 && discGround > 0;
                        if (ground) tMax = -b - Math.Sqrt(discGround);
                        throughput[0] = throughput[1] = throughput[2] = 1;
                        double dt = tMax / steps;
                        for (int s = 0; s < steps; s++)
                        {
                            var pos = origin + dir * ((s + 0.5) * dt);
                            double pr = pos.magnitude;
                            Density(p, pr - p.Bottom, out var sv, out var ev);
                            Split(sv, scat); Split(ev, ext);
                            Split(SampleTransmittance(transmittance, pr, Vector3d.Dot(pos / pr, sun), p), sunT);
                            const double iso = 1 / (4 * Math.PI);
                            for (int c = 0; c < 3; c++)
                            {
                                double e = Math.Max(ext[c], 1e-12), segment = Math.Exp(-e * dt);
                                double absorbed = (1 - segment) / e;
                                L[c] += throughput[c] * scat[c] * iso * sunT[c] * absorbed;
                                F[c] += throughput[c] * scat[c] * absorbed;
                                throughput[c] *= segment;
                            }
                        }
                        if (ground)
                        {
                            var n = (origin + dir * tMax).normalized;
                            double ndl = Math.Max(0, Vector3d.Dot(n, sun));
                            Split(SampleTransmittance(transmittance, p.Bottom, Vector3d.Dot(n, sun), p), sunT);
                            for (int c = 0; c < 3; c++) L[c] += throughput[c] * sunT[c] * ndl * GroundAlbedo / Math.PI;
                        }
                    }
                    for (int c = 0; c < 3; c++) { L[c] /= directions; F[c] = Math.Min(F[c] / directions, 0.99); }
                    result[y * MultiSize + x] = new Color((float)(L[0] / (1 - F[0])), (float)(L[1] / (1 - F[1])), (float)(L[2] / (1 - F[2])), 1);
                }
            });
            return result;
        }

        static void Split(Vector3d v, double[] into) { into[0] = v.x; into[1] = v.y; into[2] = v.z; }

        public static Texture2D ToTexture(Color[] pixels, int width, int height, string name)
        {
            var t = new Texture2D(width, height, TextureFormat.RGBAHalf, false, true)
            { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            t.SetPixels(pixels); t.Apply(false, false);
            return t;
        }
    }
}
