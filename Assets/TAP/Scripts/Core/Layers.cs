using UnityEngine;

namespace TAP.Core
{
    /// <summary>Physics / render layer indices (named in ProjectSettings/TagManager by the editor setup).</summary>
    public static class Layers
    {
        public const int Default = 0;
        public const int UI = 5;
        public const int Parts = 8;
        public const int Terrain = 9;
        public const int EVA = 10;
        public const int Scaled = 11;
        public const int Map = 12;
        public const int Effects = 14;
        public const int EditorParts = 15;
        public const int Ghost = 16;

        public static int Mask(params int[] layers)
        {
            int m = 0;
            foreach (var l in layers) m |= 1 << l;
            return m;
        }

        public static readonly int GroundMask = 1 << Terrain;
        public static readonly int PhysicsWorldMask = (1 << Parts) | (1 << Terrain) | (1 << EVA);

        /// <summary>Configures the collision matrix at runtime (idempotent).</summary>
        public static void ConfigureCollisionMatrix()
        {
            for (int a = 0; a < 32; a++)
                for (int b = a; b < 32; b++)
                    Physics.IgnoreLayerCollision(a, b, true);
            void Enable(int a, int b) => Physics.IgnoreLayerCollision(a, b, false);
            Enable(Parts, Parts);
            Enable(Parts, Terrain);
            Enable(Parts, EVA);
            Enable(EVA, Terrain);
            Enable(EVA, EVA);
            Enable(Default, Default);
            Enable(Default, Parts);
            Enable(Default, Terrain);
            Enable(Default, EVA);
            Enable(EditorParts, EditorParts);
        }
    }
}
