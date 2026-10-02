using System;
using TAP.Core;

namespace TAP.Game
{
    public static class PlanetVisualMath
    {
        // Use volumes close to the cloud deck; orbital and distant views use a filtered texture. Above ~0.07 radii
        // (42 km on Tellus) a volume's billows are subpixel and the ray march would only cost time.
        public const double VolumeFullAltitude = .02, VolumeZeroAltitude = .07;
        public static float CloudVolumeBlend(double distance, double radius)
        {
            if(radius<=0) return 0;
            double t=Math.Max(0,Math.Min(1,((distance-radius)/radius-VolumeFullAltitude)/(VolumeZeroAltitude-VolumeFullAltitude)));
            return (float)(1-t*t*(3-2*t));
        }
        public static bool BodyInFrustum(UnityEngine.Plane[] planes,UnityEngine.Vector3 center,float radius)
        {
            foreach(var plane in planes) if(plane.GetDistanceToPoint(center)<-radius) return false;
            return true;
        }
        public static UnityEngine.Matrix4x4 ViewProjection(UnityEngine.Camera camera)
        {
            var view=camera.worldToCameraMatrix;
            view.SetColumn(3,new UnityEngine.Vector4(0,0,0,1));
            return UnityEngine.GL.GetGPUProjectionMatrix(camera.projectionMatrix,true)*view;
        }
        // Longitude convention matches PlanetMaps, including Repeat at the dateline.
        public static void SurfaceUv(Vector3d direction, out double u, out double v)
        {
            var d = direction.normalized;
            u = (Math.Atan2(-d.z, d.x) / (2 * Math.PI) + 0.5);
            v = Math.Asin(Math.Max(-1, Math.Min(1, d.y))) / Math.PI + 0.5;
        }

        public static double WindOffset(double ut, double speed, double period)
        {
            if (period <= 0 || double.IsNaN(ut) || double.IsInfinity(ut)) return 0;
            double cycle = period / Math.Max(Math.Abs(speed), 1e-9);
            double value = (ut % cycle) * speed / period;
            return value - Math.Floor(value);
        }

        /// <summary>Stable ray/sphere chord in physical units; ray direction must be normalized.</summary>
        public static bool RaySphere(Vector3d origin, Vector3d ray, double radius, out double entry, out double exit)
        {
            double b = Vector3d.Dot(origin, ray);
            double c = (origin.magnitude - radius) * (origin.magnitude + radius);
            double h = b * b - c;
            if (h < 0) { entry = exit = 0; return false; }
            double s = Math.Sqrt(h);
            entry = -b - s; exit = -b + s;
            return exit >= 0;
        }
    }
}
