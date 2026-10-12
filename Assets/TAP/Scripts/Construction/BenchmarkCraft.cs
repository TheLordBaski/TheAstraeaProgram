using TAP.Parts;
using TAP.Persistence;

namespace TAP.Construction
{
    /// <summary>Craft for the benchmark (FND-13): a rocket with an exact number of parts, and a small lander for a base.</summary>
    public static class BenchmarkCraft
    {
        /// <summary>
        /// A capsule on a long stack of tanks, with a ring of four fins every tenth tank, and one engine: exactly
        /// <paramref name="parts"/> parts. It stands for a big rocket, not a good one.
        /// </summary>
        public static CraftDesign Rocket(PartDatabase db, int parts)
        {
            var d = new CraftDesign { name = $"Benchmark {parts}", description = $"A {parts}-part stack for timing the simulation." };
            var a = new CraftAssembler(d, db);
            int top = a.AddRoot("pod_kestrel");
            a.AttachStack(top, "top", "chute_main", "bottom");
            int tail = a.AttachStack(top, "bottom", "shield_s1", "top");
            tail = a.AttachStack(tail, "bottom", "dec_s1", "top");
            int tanks = 0;
            // one part is left for the engine
            while (d.parts.Count < parts - 1)
            {
                tail = a.AttachStack(tail, "bottom", "tank_s1_short", "top");
                tanks++;
                if (tanks % 10 == 0 && d.parts.Count + 4 <= parts - 1) a.AttachRadial(tail, "fin_fs1", -0.2f, 45, 4);
            }
            a.AttachStack(tail, "bottom", "eng_hornet", "top");
            a.AutoStage();
            return d;
        }

        /// <summary>A squat module with four legs: ten of them make a landed base.</summary>
        public static CraftDesign Lander(PartDatabase db)
        {
            var d = new CraftDesign { name = "Benchmark lander", description = "A small module for a base." };
            var a = new CraftAssembler(d, db);
            int pod = a.AddRoot("pod_kestrel");
            int rw = a.AttachStack(pod, "bottom", "rw_s1", "top");
            int battery = a.AttachStack(rw, "bottom", "battery_s1", "top");
            int tank = a.AttachStack(battery, "bottom", "tank_s1_short", "top");
            a.AttachStack(tank, "bottom", "eng_ember", "top");
            a.AttachRadial(tank, "leg_ls5", -0.4f, 45, 4);
            a.AutoStage();
            return d;
        }
    }
}
