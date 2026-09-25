namespace TAP.Simulation
{
    /// <summary>
    /// Developer / cheat switches set from the developer window (Alt+F12 in flight), like KSP's cheat menu.
    /// Off by default and not saved: they last until the game is closed.
    /// </summary>
    public static class DevCheats
    {
        /// <summary>Engines, RCS and the EVA pack draw from their tanks without emptying them (empty tanks supply too).</summary>
        public static bool InfinitePropellant;
        /// <summary>Electric charge is never used up.</summary>
        public static bool InfiniteElectricity;
        /// <summary>No part is destroyed by impacts, hard landings or splashdowns; landing legs don't break.</summary>
        public static bool NoCrashDamage;
        /// <summary>Parts heat up as usual but never burn up; parachutes don't burn.</summary>
        public static bool IgnoreHeat;
        /// <summary>Joints never fail from structural loads; parachutes don't tear.</summary>
        public static bool UnbreakableJoints;

        public static bool Any => InfinitePropellant || InfiniteElectricity || NoCrashDamage || IgnoreHeat || UnbreakableJoints;
    }
}
