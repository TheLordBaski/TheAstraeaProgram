using System;
using System.Diagnostics;
using Unity.Profiling;

namespace TAP.Core
{
    /// <summary>The steps of a frame that are timed (FND-13).</summary>
    public enum SimStep
    {
        /// <summary>Before the physics step: ground colliders, pad holds and each vessel's forces.</summary>
        PreStep,
        /// <summary>After the physics step: each vessel's state, heat, structure and the floating origin.</summary>
        PostStep,
        /// <summary>Time warp on rails: moving every vessel along its orbit.</summary>
        Rails,
        /// <summary>Per-frame part visuals and module updates.</summary>
        RenderUpdate,
        /// <summary>Planet terrain chunks, colliders, atmosphere and sky.</summary>
        Planets,
        /// <summary>Predicting the vessel's path for the map and the HUD.</summary>
        Trajectory,
        /// <summary>The map view's lines and markers.</summary>
        Map,
        /// <summary>The flight display's text and widgets.</summary>
        Hud,
        /// <summary>Exhaust, smoke and sparks.</summary>
        Effects,
    }

    /// <summary>
    /// Timing around each step of the simulation (FND-13). A scope shows up in the Unity profiler as <c>TAP.&lt;step&gt;</c> (development
    /// builds and the editor) and adds its time to <see cref="Ms"/> in every build, so the benchmark can report where a frame goes.
    /// </summary>
    public static class SimTimers
    {
        private static readonly int Count = Enum.GetValues(typeof(SimStep)).Length;
        private static readonly ProfilerMarker[] Markers = MakeMarkers();
        private static readonly long[] Ticks = new long[Count];

        /// <summary>Milliseconds spent in each step since <see cref="Reset"/> (the benchmark resets it every frame).</summary>
        public static double Ms(SimStep step) => Ticks[(int)step] * 1000.0 / Stopwatch.Frequency;

        public static void Reset() => Array.Clear(Ticks, 0, Ticks.Length);

        public static Scope Measure(SimStep step) => new Scope(step);

        private static ProfilerMarker[] MakeMarkers()
        {
            var m = new ProfilerMarker[Count];
            for (int i = 0; i < m.Length; i++) m[i] = new ProfilerMarker("TAP." + (SimStep)i);
            return m;
        }

        public readonly struct Scope : IDisposable
        {
            private readonly int _step;
            private readonly long _start;
            private readonly ProfilerMarker.AutoScope _auto;

            public Scope(SimStep step)
            {
                _step = (int)step;
                _auto = Markers[_step].Auto();
                _start = Stopwatch.GetTimestamp();
            }

            public void Dispose()
            {
                Ticks[_step] += Stopwatch.GetTimestamp() - _start;
                _auto.Dispose();
            }
        }
    }
}
