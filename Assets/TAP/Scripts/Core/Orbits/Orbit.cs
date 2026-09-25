using System;

namespace TAP.Core
{
    /// <summary>
    /// Immutable two-body (Kepler) orbit defined by a state vector at an epoch.
    ///
    /// Frame conventions (shared by the whole game):
    ///  * Positions/velocities are relative to the reference body's centre, in a non-rotating
    ///    frame whose axes coincide with Unity world axes.
    ///  * +Y is the celestial "north" axis (planet spin axis). Prograde motion about +Y carries
    ///    +X towards -Z (this matches Unity's Quaternion.AngleAxis(+angle, Vector3.up)).
    ///  * Longitude of ascending node is measured from +X towards -Z.
    ///
    /// Propagation uses universal variables (Stumpff functions) which are robust for elliptic,
    /// parabolic, hyperbolic and radial trajectories. Classical elements are derived for display,
    /// drawing and anomaly/time conversions.
    /// </summary>
    public sealed class Orbit
    {
        public static readonly Vector3d NorthAxis = Vector3d.up;
        public static readonly Vector3d ReferenceDirection = Vector3d.right;       // +X
        public static readonly Vector3d ReferenceDirection90 = new Vector3d(0, 0, -1); // -Z (prograde 90 deg from +X)

        public readonly double Mu;
        public readonly double Epoch;
        public readonly Vector3d R0;
        public readonly Vector3d V0;

        public readonly double Alpha;            // 1/a = 2/r - v^2/mu
        public readonly double SemiMajorAxis;    // negative for hyperbolic, +/-inf for parabolic
        public readonly double Eccentricity;
        public readonly double SemiLatusRectum;
        public readonly double Energy;           // specific orbital energy
        public readonly Vector3d H;              // specific angular momentum
        public readonly Vector3d EccentricityVector;
        public readonly Vector3d P, Q, W;        // perifocal basis (P -> periapsis, W -> normal)
        public readonly double Inclination;      // rad
        public readonly double LongitudeOfAscendingNode; // rad
        public readonly double ArgumentOfPeriapsis;      // rad
        public readonly double PeriapsisRadius;
        public readonly double ApoapsisRadius;   // +inf when not elliptic
        public readonly double Period;           // +inf when not elliptic
        public readonly double MeanMotion;       // rad/s (hyperbolic: sqrt(mu/(-a)^3))
        public readonly double MeanAnomalyAtEpoch;
        public readonly double TrueAnomalyAtEpoch;
        public readonly bool IsRadial;

        private readonly double _sqrtMu;
        private readonly double _r0Mag;
        private readonly double _sigma0; // r0.v0 / sqrt(mu)

        public bool IsElliptic => Eccentricity < 1.0 && Alpha > 0;
        public bool IsHyperbolic => Eccentricity > 1.0 || Alpha < 0;

        public Orbit(Vector3d r, Vector3d v, double epoch, double mu)
        {
            if (mu <= 0) throw new ArgumentException("mu must be positive");
            Mu = mu;
            Epoch = epoch;
            R0 = r;
            V0 = v;
            _sqrtMu = Math.Sqrt(mu);
            _r0Mag = r.magnitude;
            if (_r0Mag < 1e-3) _r0Mag = 1e-3; // degenerate: at body centre
            double v2 = v.sqrMagnitude;
            _sigma0 = Vector3d.Dot(r, v) / _sqrtMu;

            Alpha = 2.0 / _r0Mag - v2 / mu;
            Energy = v2 * 0.5 - mu / _r0Mag;
            SemiMajorAxis = Math.Abs(Alpha) > 1e-300 ? 1.0 / Alpha : double.PositiveInfinity;

            H = Vector3d.Cross(r, v);
            double hMag = H.magnitude;
            double speed = Math.Sqrt(v2);
            IsRadial = hMag <= 1e-9 * _r0Mag * Math.Max(speed, 1e-9) || hMag < 1e-6;

            Vector3d ecc = ((v2 - mu / _r0Mag) * r - Vector3d.Dot(r, v) * v) / mu;
            double e = ecc.magnitude;

            if (!IsRadial)
            {
                W = H / hMag;
                SemiLatusRectum = hMag * hMag / mu;
            }
            else
            {
                // Radial (rectilinear) trajectory: choose an arbitrary normal.
                Vector3d rn = r.normalized;
                Vector3d trial = Math.Abs(Vector3d.Dot(rn, NorthAxis)) < 0.9 ? NorthAxis : ReferenceDirection;
                W = Vector3d.Cross(trial, rn).normalized;
                SemiLatusRectum = 0;
                e = 1.0;
            }

            if (e < 1e-9 && !IsRadial)
            {
                e = 0;
                ecc = Vector3d.zero;
            }
            Eccentricity = e;
            EccentricityVector = ecc;

            // Node vector (points to ascending node).
            Vector3d n = Vector3d.Cross(NorthAxis, H);
            double nMag = n.magnitude;
            Inclination = IsRadial ? 0 : Math.Acos(MathD.Clamp(W.y, -1, 1));
            Vector3d refDir;
            if (nMag > 1e-9 * Math.Max(hMag, 1e-12))
            {
                Vector3d nn = n / nMag;
                LongitudeOfAscendingNode = MathD.WrapTwoPi(Math.Atan2(Vector3d.Dot(nn, ReferenceDirection90), Vector3d.Dot(nn, ReferenceDirection)));
                refDir = nn;
            }
            else
            {
                LongitudeOfAscendingNode = 0;
                // Equatorial: use +X projected into the orbit plane (or any in-plane vector).
                refDir = Vector3d.ProjectOnPlane(ReferenceDirection, W);
                if (refDir.sqrMagnitude < 1e-12) refDir = Vector3d.ProjectOnPlane(ReferenceDirection90, W);
                refDir = refDir.normalized;
            }

            if (IsRadial)
            {
                P = r.normalized;
                Q = Vector3d.Cross(W, P).normalized;
                ArgumentOfPeriapsis = 0;
            }
            else if (e > 0)
            {
                P = ecc / e;
                Q = Vector3d.Cross(W, P);
                Vector3d refPerp = Vector3d.Cross(W, refDir);
                ArgumentOfPeriapsis = MathD.WrapTwoPi(Math.Atan2(Vector3d.Dot(P, refPerp), Vector3d.Dot(P, refDir)));
            }
            else
            {
                P = refDir;
                Q = Vector3d.Cross(W, P);
                ArgumentOfPeriapsis = 0;
            }

            if (IsRadial)
            {
                PeriapsisRadius = 0;
                ApoapsisRadius = Alpha > 0 ? 2.0 / Alpha : double.PositiveInfinity;
                TrueAnomalyAtEpoch = 0;
                MeanAnomalyAtEpoch = double.NaN;
                MeanMotion = Alpha > 0 ? Math.Sqrt(mu * Alpha * Alpha * Alpha) : double.NaN;
                Period = Alpha > 0 ? MathD.TwoPi / MeanMotion : double.PositiveInfinity;
                return;
            }

            PeriapsisRadius = SemiLatusRectum / (1 + e);
            TrueAnomalyAtEpoch = Math.Atan2(Vector3d.Dot(r, Q), Vector3d.Dot(r, P));

            if (e < 1.0 && Alpha > 0)
            {
                ApoapsisRadius = SemiLatusRectum / (1 - e);
                MeanMotion = Math.Sqrt(mu * Alpha * Alpha * Alpha);
                Period = MathD.TwoPi / MeanMotion;
                MeanAnomalyAtEpoch = MeanAnomalyFromTrue(TrueAnomalyAtEpoch);
            }
            else
            {
                ApoapsisRadius = double.PositiveInfinity;
                Period = double.PositiveInfinity;
                if (Alpha < 0)
                    MeanMotion = Math.Sqrt(mu * -Alpha * -Alpha * -Alpha);
                else
                    MeanMotion = Math.Sqrt(mu / Math.Max(SemiLatusRectum * SemiLatusRectum * SemiLatusRectum, 1e-30)); // parabolic
                MeanAnomalyAtEpoch = MeanAnomalyFromTrue(TrueAnomalyAtEpoch);
            }
        }

        /// <summary>Builds an orbit from classical elements (angles in radians).</summary>
        public static Orbit FromElements(double semiMajorAxis, double eccentricity, double inclination, double lan,
            double argPe, double meanAnomalyAtEpoch, double epoch, double mu)
        {
            double e = eccentricity;
            double a = semiMajorAxis;
            // Solve Kepler's equation for the true anomaly at epoch.
            double nu;
            if (e < 1)
            {
                double M = MathD.WrapPi(meanAnomalyAtEpoch);
                double E = e < 0.8 ? M : Math.PI;
                for (int i = 0; i < 50; i++)
                {
                    double f = E - e * Math.Sin(E) - M;
                    double d = f / (1 - e * Math.Cos(E));
                    E -= d;
                    if (Math.Abs(d) < 1e-15) break;
                }
                nu = 2 * Math.Atan2(Math.Sqrt(1 + e) * Math.Sin(E / 2), Math.Sqrt(1 - e) * Math.Cos(E / 2));
            }
            else
            {
                double M = meanAnomalyAtEpoch;
                double F = MathD.Asinh(M / e);
                for (int i = 0; i < 80; i++)
                {
                    double f = e * Math.Sinh(F) - F - M;
                    double d = f / (e * Math.Cosh(F) - 1);
                    F -= d;
                    if (Math.Abs(d) < 1e-15) break;
                }
                nu = 2 * Math.Atan(Math.Sqrt((e + 1) / (e - 1)) * Math.Tanh(F / 2));
            }

            double p = a * (1 - e * e);
            Vector3d N = new Vector3d(Math.Cos(lan), 0, -Math.Sin(lan));
            Vector3d M90 = new Vector3d(-Math.Sin(lan), 0, -Math.Cos(lan));
            Vector3d Mi = Math.Cos(inclination) * M90 + Math.Sin(inclination) * NorthAxis;
            Vector3d Pv = Math.Cos(argPe) * N + Math.Sin(argPe) * Mi;
            Vector3d Qv = -Math.Sin(argPe) * N + Math.Cos(argPe) * Mi;
            double rMag = p / (1 + e * Math.Cos(nu));
            Vector3d r = rMag * (Math.Cos(nu) * Pv + Math.Sin(nu) * Qv);
            double k = Math.Sqrt(mu / p);
            Vector3d v = k * (-Math.Sin(nu) * Pv + (e + Math.Cos(nu)) * Qv);
            return new Orbit(r, v, epoch, mu);
        }

        // ------------------------------------------------------------------ propagation

        /// <summary>State (position, velocity) relative to the reference body at universal time <paramref name="ut"/>.</summary>
        public void GetStateAtUT(double ut, out Vector3d r, out Vector3d v)
        {
            double dt = ut - Epoch;
            if (Alpha > 0 && !double.IsInfinity(Period) && Math.Abs(dt) > Period * 0.5)
            {
                double k = Math.Round(dt / Period);
                dt -= k * Period;
            }
            if (Math.Abs(dt) < 1e-9)
            {
                r = R0; v = V0; return;
            }

            double chi = SolveUniversalAnomaly(dt);
            double z = Alpha * chi * chi;
            double C = MathD.StumpffC(z);
            double S = MathD.StumpffS(z);
            double chi2 = chi * chi;
            double chi3 = chi2 * chi;

            double f = 1 - chi2 / _r0Mag * C;
            double g = dt - chi3 * S / _sqrtMu;
            r = f * R0 + g * V0;
            double rm = r.magnitude;
            if (rm < 1e-6) rm = 1e-6;
            double fdot = _sqrtMu / (rm * _r0Mag) * (Alpha * chi3 * S - chi);
            double gdot = 1 - chi2 / rm * C;
            v = fdot * R0 + gdot * V0;
        }

        public Vector3d GetPositionAtUT(double ut)
        {
            GetStateAtUT(ut, out Vector3d r, out _);
            return r;
        }

        public Vector3d GetVelocityAtUT(double ut)
        {
            GetStateAtUT(ut, out _, out Vector3d v);
            return v;
        }

        private double UniversalF(double chi, double target, out double derivative)
        {
            double z = Alpha * chi * chi;
            double C = MathD.StumpffC(z);
            double S = MathD.StumpffS(z);
            double chi2 = chi * chi;
            double oneMinusAR = 1 - Alpha * _r0Mag;
            double F = _sigma0 * chi2 * C + oneMinusAR * chi2 * chi * S + _r0Mag * chi - target;
            derivative = _sigma0 * chi * (1 - z * S) + oneMinusAR * chi2 * C + _r0Mag; // = r(chi) > 0
            return F;
        }

        private double SolveUniversalAnomaly(double dt)
        {
            double target = _sqrtMu * dt;
            double chi;
            if (Alpha > 1e-12)
            {
                chi = _sqrtMu * dt * Alpha;
            }
            else if (Alpha < -1e-12)
            {
                double a = 1.0 / Alpha;
                double num = -2.0 * Mu * Alpha * dt;
                double den = Vector3d.Dot(R0, V0) + Math.Sign(dt) * Math.Sqrt(-Mu * a) * (1 - _r0Mag * Alpha);
                chi = Math.Sign(dt) * Math.Sqrt(-a) * Math.Log(Math.Abs(num / den));
                if (double.IsNaN(chi) || double.IsInfinity(chi)) chi = _sqrtMu * dt / _r0Mag;
            }
            else
            {
                chi = _sqrtMu * dt / _r0Mag;
            }

            // Bracket the root; F is strictly increasing (dF/dchi = r > 0).
            double lo, hi;
            double fChi = UniversalF(chi, target, out _);
            if (fChi < 0)
            {
                lo = chi;
                double step = Math.Max(Math.Abs(chi), 1.0);
                hi = chi + step;
                int guard = 0;
                while (UniversalF(hi, target, out _) < 0 && guard++ < 200)
                {
                    lo = hi;
                    step *= 2;
                    hi = chi + step;
                }
            }
            else
            {
                hi = chi;
                double step = Math.Max(Math.Abs(chi), 1.0);
                lo = chi - step;
                int guard = 0;
                while (UniversalF(lo, target, out _) > 0 && guard++ < 200)
                {
                    hi = lo;
                    step *= 2;
                    lo = chi - step;
                }
            }

            chi = MathD.Clamp(chi, lo, hi);
            for (int i = 0; i < 100; i++)
            {
                double f = UniversalF(chi, target, out double fp);
                if (f < 0) lo = chi; else hi = chi;
                double next = chi - f / fp;
                if (!(next > lo && next < hi)) next = 0.5 * (lo + hi);
                double delta = Math.Abs(next - chi);
                chi = next;
                if (delta <= 1e-14 * Math.Max(1.0, Math.Abs(chi)) || hi - lo <= 1e-15 * Math.Max(1.0, Math.Abs(chi)))
                    break;
            }
            return chi;
        }

        // ------------------------------------------------------------------ anomalies & times

        /// <summary>Mean anomaly (or its hyperbolic/parabolic analogue) for a true anomaly.</summary>
        public double MeanAnomalyFromTrue(double nu)
        {
            double e = Eccentricity;
            if (IsRadial) return double.NaN;
            if (e < 1.0 && Alpha > 0)
            {
                double E = 2 * Math.Atan2(Math.Sqrt(1 - e) * Math.Sin(nu / 2), Math.Sqrt(1 + e) * Math.Cos(nu / 2));
                return E - e * Math.Sin(E);
            }
            if (e > 1.0 + 1e-9)
            {
                double t = Math.Sqrt((e - 1) / (e + 1)) * Math.Tan(nu / 2);
                if (t >= 1) t = 1 - 1e-15;
                if (t <= -1) t = -1 + 1e-15;
                double F = 2 * MathD.Atanh(t);
                return e * Math.Sinh(F) - F;
            }
            // Parabolic (Barker): M = (D + D^3/3)/2 with n = sqrt(mu/p^3)
            double D = Math.Tan(nu / 2);
            return 0.5 * (D + D * D * D / 3.0);
        }

        public double MeanAnomalyAtUT(double ut)
        {
            double M = MeanAnomalyAtEpoch + MeanMotion * (ut - Epoch);
            if (IsElliptic) M = MathD.WrapTwoPi(M);
            return M;
        }

        /// <summary>True anomaly at UT computed geometrically from the propagated position.</summary>
        public double TrueAnomalyAtUT(double ut)
        {
            Vector3d r = GetPositionAtUT(ut);
            return Math.Atan2(Vector3d.Dot(r, Q), Vector3d.Dot(r, P));
        }

        public double TrueAnomalyOfPosition(Vector3d r) => Math.Atan2(Vector3d.Dot(r, Q), Vector3d.Dot(r, P));

        /// <summary>
        /// Next UT (at or after <paramref name="afterUT"/>) at which the body passes true anomaly nu.
        /// For open orbits the unique crossing time is returned (possibly before afterUT).
        /// </summary>
        public double UTAtTrueAnomaly(double nu, double afterUT)
        {
            if (IsRadial) return double.NaN;
            double Mnu = MeanAnomalyFromTrue(nu);
            if (IsElliptic)
            {
                double Mnow = MeanAnomalyAtUT(afterUT);
                double dM = MathD.WrapTwoPi(Mnu - Mnow);
                return afterUT + dM / MeanMotion;
            }
            return Epoch + (Mnu - MeanAnomalyAtEpoch) / MeanMotion;
        }

        public double TimeToPeriapsis(double ut)
        {
            if (IsRadial) return double.NaN;
            if (IsElliptic)
            {
                double M = MeanAnomalyAtUT(ut);
                return MathD.WrapTwoPi(-M) / MeanMotion;
            }
            double Mh = MeanAnomalyAtEpoch + MeanMotion * (ut - Epoch);
            return -Mh / MeanMotion;
        }

        public double TimeToApoapsis(double ut)
        {
            if (!IsElliptic || IsRadial) return double.NaN;
            double M = MeanAnomalyAtUT(ut);
            return MathD.WrapTwoPi(Math.PI - M) / MeanMotion;
        }

        public double RadiusAtTrueAnomaly(double nu)
        {
            if (IsRadial) return double.NaN;
            return SemiLatusRectum / (1 + Eccentricity * Math.Cos(nu));
        }

        public Vector3d PositionAtTrueAnomaly(double nu)
        {
            double r = RadiusAtTrueAnomaly(nu);
            return r * (Math.Cos(nu) * P + Math.Sin(nu) * Q);
        }

        public Vector3d VelocityAtTrueAnomaly(double nu)
        {
            double k = Math.Sqrt(Mu / SemiLatusRectum);
            return k * (-Math.Sin(nu) * P + (Eccentricity + Math.Cos(nu)) * Q);
        }

        /// <summary>
        /// Positive true anomaly (0..pi) at which the orbit reaches radius r, or NaN if never reached.
        /// The descending-branch crossing is the negative of this value.
        /// </summary>
        public double TrueAnomalyAtRadius(double r)
        {
            if (IsRadial) return double.NaN;
            double e = Eccentricity;
            if (e < 1e-12) return double.NaN; // circular: radius constant
            double c = (SemiLatusRectum / r - 1) / e;
            if (c > 1 || c < -1) return double.NaN;
            return Math.Acos(c);
        }

        /// <summary>For hyperbolic orbits: the asymptotic true anomaly limit.</summary>
        public double MaxTrueAnomaly => Eccentricity > 1 ? Math.Acos(-1.0 / Eccentricity) : Math.PI;

        /// <summary>Returns a copy of this orbit re-anchored at the given UT (limits numeric drift of very old epochs).</summary>
        public Orbit Reanchored(double ut)
        {
            GetStateAtUT(ut, out Vector3d r, out Vector3d v);
            return new Orbit(r, v, ut, Mu);
        }

        public override string ToString()
        {
            return $"Orbit(a={SemiMajorAxis:F0}, e={Eccentricity:F5}, i={Inclination * MathD.Rad2Deg:F2}deg, Pe={PeriapsisRadius:F0}, Ap={ApoapsisRadius:F0}, T={Period:F1})";
        }
    }
}
