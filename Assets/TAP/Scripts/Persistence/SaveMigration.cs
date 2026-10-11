using System;
using Newtonsoft.Json.Linq;

namespace TAP.Persistence
{
    /// <summary>A save that can't be loaded (damaged with no usable backup, or made by a newer game).</summary>
    public class SaveException : Exception
    {
        public SaveException(string message, Exception inner = null) : base(message, inner) { }
    }

    /// <summary>The save is from a newer version of the game. Not damage: backups would be no better.</summary>
    public sealed class SaveTooNewException : SaveException
    {
        public SaveTooNewException(int version) : base($"This save was made by a newer version of the game (format {version}); this one reads up to {SaveMigration.Current}.") { }
    }

    /// <summary>
    /// Upgrades save JSON one format version at a time, so a save from any earlier version loads (the Early Access
    /// promise, D-13). To change the format: bump <see cref="Current"/>, append one step, add a corpus save of the old
    /// format to Assets/TAP/Tests/Saves. Never edit a step that has shipped.
    /// </summary>
    public static class SaveMigration
    {
        public const int Current = 2;

        // Steps[i] turns format i+1 into format i+2.
        private static readonly Action<JObject>[] Steps =
        {
            // 1 -> 2: game mode and play time. Every save from the vertical slice is a sandbox game.
            o => { o["mode"] = "sandbox"; o["playTime"] = 0; },
        };

        public static JObject Upgrade(JObject save)
        {
            int version = save.Value<int?>("version") ?? 1;
            if (version > Current) throw new SaveTooNewException(version);
            for (int v = Math.Max(version, 1); v < Current; v++) Steps[v - 1](save);
            save["version"] = Current;
            return save;
        }
    }
}
