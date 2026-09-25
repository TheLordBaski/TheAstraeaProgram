using System.Collections.Generic;
using TAP.Parts;
using TAP.Persistence;

namespace TAP.Construction
{
    /// <summary>
    /// Tested starter vehicles, assembled through CraftAssembler exactly like the editor does.
    ///  * Skylark  — single-stage solid suborbital hop with parachute recovery.
    ///  * Meridian — two-stage liquid orbiter with heat shield (LEO and back).
    ///  * Pathfinder — four-stage lunar mission: boosters + core, transfer stage (TLI/LOI),
    ///    landing/ascent stage with legs (landing, ascent, trans-Tellus injection), return capsule.
    /// </summary>
    public static class StarterCraft
    {
        public static List<CraftDesign> All(PartDatabase db) => new List<CraftDesign> { Skylark(db), Meridian(db), Pathfinder(db) };

        public static CraftDesign Skylark(PartDatabase db)
        {
            var d = new CraftDesign { name = "Skylark Suborbital", description = "Solid-fuel sounding rocket: one crew, one burn well above the 70 km edge of space, then a heat-shielded fall and a parachute landing. Stage 1: ignite, stage 2: separate capsule, stage 3: arm chute." };
            var a = new CraftAssembler(d, db);
            int pod = a.AddRoot("pod_kestrel");
            a.AttachStack(pod, "top", "chute_main", "bottom");
            int shield = a.AttachStack(pod, "bottom", "shield_s1", "top");
            int dec = a.AttachStack(shield, "bottom", "dec_s1", "top");
            int srb = a.AttachStack(dec, "bottom", "srb_kodiak", "top");
            d.parts[srb].settings = new Dictionary<string, double> { { "thrustLimit", 80 } };
            a.AttachRadial(srb, "fin_fs1", -2.9f, 0, 3);
            a.AutoStage();
            return d;
        }

        public static CraftDesign Meridian(PartDatabase db)
        {
            var d = new CraftDesign { name = "Meridian Orbiter", description = "Two-stage orbital rocket (~3,800 m/s). Gimballed lifter first stage, efficient vacuum upper stage, heat-shielded capsule." };
            var a = new CraftAssembler(d, db);
            int pod = a.AddRoot("pod_kestrel");
            a.AttachStack(pod, "top", "chute_main", "bottom");
            int shield = a.AttachStack(pod, "bottom", "shield_s1", "top");
            int dec1 = a.AttachStack(shield, "bottom", "dec_s1", "top");
            int t1 = a.AttachStack(dec1, "bottom", "tank_s1_long", "top");
            int e1 = a.AttachStack(t1, "bottom", "eng_ember", "top");
            int dec2 = a.AttachStack(e1, "bottom", "dec_s1", "top");
            int t2 = a.AttachStack(dec2, "bottom", "tank_s1_long", "top");
            int t3 = a.AttachStack(t2, "bottom", "tank_s1_long", "top");
            a.AttachStack(t3, "bottom", "eng_hornet", "top");
            a.AttachRadial(t3, "fin_fs1", -1.7f, 45, 4);
            a.AutoStage();
            return d;
        }

        public static CraftDesign Pathfinder(PartDatabase db)
        {
            var d = new CraftDesign
            {
                name = "Luma Pathfinder",
                description = "Complete crewed lunar mission. Four boosters and the core reach space, the transfer stage finishes the orbit " +
                              "and starts the transfer to Luma; the wide lander (legs on G) completes it, captures, lands, lifts off and " +
                              "burns home; the heat-shielded capsule returns under its main chute.",
            };
            var a = new CraftAssembler(d, db);
            int pod = a.AddRoot("pod_kestrel");
            a.AttachStack(pod, "top", "chute_main", "bottom");
            int shield = a.AttachStack(pod, "bottom", "shield_s1", "top");
            // Lander / return stage: squat and wide so it stands on Luma's slopes (centre of mass ~3.6 m up on a
            // 5 m stance, tips only beyond ~26°), ~3,400 m/s for the landing, the lunar ascent and the burn home.
            int decA = a.AttachStack(shield, "bottom", "dec_s1", "top");
            int rw = a.AttachStack(decA, "bottom", "rw_s1", "top");
            int batt = a.AttachStack(rw, "bottom", "battery_s1", "top");
            int lt = a.AttachStack(batt, "bottom", "adapter_s2_s1", "top");   // tapered: shields the wide tank's top face
            int ls = a.AttachStack(lt, "bottom", "tank_s2_short", "top");
            int le = a.AttachStack(ls, "bottom", "eng_ember", "top");
            a.AttachRadial(ls, "leg_ls5", -0.6f, 45, 4);
            // RCS blocks at 45° (above the legs), so none sits under the capsule hatch (180°): the way down is clear.
            a.AttachRadial(ls, "rcs_quad", 0.45f, 45, 4);
            // Transfer stage (2.5 m): finishes the climb to orbit and starts the transfer to Luma.
            int decB = a.AttachStack(le, "bottom", "dec_s1", "top");
            int ad1 = a.AttachStack(decB, "bottom", "adapter_s2_s1", "top");
            int tt = a.AttachStack(ad1, "bottom", "tank_s2_short", "top");
            int ad2 = a.AttachStack(tt, "bottom", "adapter_s2_s1", "bottom"); // inverted adapter: 2.5 m -> 1.25 m
            int te = a.AttachStack(ad2, "top", "eng_hornet", "top");
            // Core stage: TWR ~1.5 once the boosters are gone, carries the rocket to ~37 km.
            int decC = a.AttachStack(te, "bottom", "dec_s1", "top");
            int ad3 = a.AttachStack(decC, "bottom", "adapter_s2_s1", "top");
            int core = a.AttachStack(ad3, "bottom", "tank_s2_long", "top");
            int core2 = a.AttachStack(core, "bottom", "tank_s2_short", "top");
            a.AttachStack(core2, "bottom", "eng_goliath", "top");
            a.AttachRadial(core2, "fin_fs1", -0.5f, 45, 4);
            // Four strap-on boosters
            var rdecs = a.AttachRadial(core, "dec_radial", 1.0f, 90, 4);
            var boosters = a.AttachRadialToAll(rdecs, "srb_kodiak", 0f, 0f, 0.1f);
            a.AttachStackToAll(boosters, "top", "nose_s1", "bottom");
            // Fins on the boosters keep the empty boosters pointing into the wind after separation, so they fall
            // back cleanly instead of tumbling into the core stage.
            foreach (int b in boosters) a.AttachRadial(b, "fin_fs1", -2.9f, 90, 2);
            a.AutoStage();
            return d;
        }
    }
}
