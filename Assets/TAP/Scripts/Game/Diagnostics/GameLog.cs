using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TAP.Persistence;
using UnityEngine;

namespace TAP.Game
{
    /// <summary>
    /// The game's own log (FND-12): <c>Logs/tap-&lt;date&gt;.log</c> in the user folder, one file per run, the last five kept. It starts with
    /// what a bug report needs first (version, OS, CPU, GPU, the settings) and then holds everything Unity logs, with stack traces.
    /// Unhandled exceptions are also queued for the notice on screen (<see cref="Exceptions"/>).
    /// </summary>
    public static class GameLog
    {
        public const int KeptFiles = 5;
        /// <summary>Where players are sent with a report until the community channels exist (REL-05).</summary>
        public const string FeedbackUrl = "https://github.com/TheLordBaski/TheAstraeaProgram/issues/new";

        public static string Folder => Path.Combine(SaveStorage.Root, "Logs");
        public static string CurrentFile { get; private set; }

        public struct Entry { public DateTime When; public string Message, Stack; }

        /// <summary>The unhandled exceptions of this run, newest last (at most 30).</summary>
        public static readonly List<Entry> Exceptions = new List<Entry>();
        /// <summary>Raised on the main thread for an exception that was not seen in the last minute.</summary>
        public static event Action<Entry> ExceptionRaised;

        private static StreamWriter _writer;
        private static readonly object Gate = new object();
        private static readonly ConcurrentQueue<Entry> Pending = new ConcurrentQueue<Entry>();
        private static readonly Dictionary<string, DateTime> LastSeen = new Dictionary<string, DateTime>();

        /// <summary>Opens this run's file and writes the header. Call once, after the settings are loaded.</summary>
        public static void Begin()
        {
            if (_writer != null) return;
            try
            {
                Directory.CreateDirectory(Folder);
                var old = Directory.GetFiles(Folder, "tap-*.log").OrderBy(f => f, StringComparer.Ordinal).ToList();
                for (int i = 0; i < old.Count - (KeptFiles - 1); i++) { try { File.Delete(old[i]); } catch (IOException) { } }
                CurrentFile = Path.Combine(Folder, $"tap-{DateTime.Now:yyyyMMdd-HHmmss}.log");
                _writer = new StreamWriter(new FileStream(CurrentFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite), new UTF8Encoding(false));
                WriteHeader();
                Application.logMessageReceivedThreaded += OnLog;
            }
            catch (Exception e)
            {
                Debug.LogWarning("The game log could not be started: " + e.Message);
                _writer = null;
            }
        }

        private static void WriteHeader()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"The Astraea Program {Application.version}");
            sb.AppendLine($"Started {DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}");
            sb.AppendLine(SystemDescription());
            sb.AppendLine("Command line: " + string.Join(" ", Environment.GetCommandLineArgs().Skip(1)));
            sb.AppendLine("Settings: " + SaveStorage.ToJson(GameSettings.Current).Replace("\r", "").Replace("\n", " "));
            sb.AppendLine(new string('-', 60));
            lock (Gate) _writer.Write(sb.ToString());
        }

        /// <summary>The machine and the build, one fact a line.</summary>
        public static string SystemDescription()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Version: {Application.version} ({Application.platform}, {(Debug.isDebugBuild ? "development" : "release")} build, Unity {Application.unityVersion})");
            sb.AppendLine($"OS: {SystemInfo.operatingSystem}");
            sb.AppendLine($"CPU: {SystemInfo.processorType}, {SystemInfo.processorCount} threads, {SystemInfo.systemMemorySize} MB memory");
            sb.AppendLine($"GPU: {SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsDeviceType}, {SystemInfo.graphicsMemorySize} MB), driver {SystemInfo.graphicsDeviceVersion}");
            sb.Append($"Screen: {Screen.width}x{Screen.height} {Screen.fullScreenMode}, language {Application.systemLanguage}");
            return sb.ToString();
        }

        private static void OnLog(string message, string stack, LogType type)
        {
            if (_writer == null) return;
            var when = DateTime.Now;
            lock (Gate)
            {
                if (_writer == null) return;
                _writer.WriteLine($"{when:HH:mm:ss.fff} [{type}] {message}");
                if (!string.IsNullOrEmpty(stack) && type != LogType.Log) _writer.WriteLine(Indent(stack));
                if (type == LogType.Exception || type == LogType.Error) _writer.Flush(); // a crash must not lose what led to it
            }
            if (type == LogType.Exception) Pending.Enqueue(new Entry { When = when, Message = message, Stack = stack });
        }

        private static string Indent(string stack) => "    " + stack.TrimEnd().Replace("\n", "\n    ");

        /// <summary>Called every frame on the main thread: raises the exceptions logged since the last call.</summary>
        public static void Pump()
        {
            while (Pending.TryDequeue(out var e))
            {
                Exceptions.Add(e);
                if (Exceptions.Count > 30) Exceptions.RemoveAt(0);
                string key = e.Message.Split('\n')[0];
                // The same exception every frame is one notice, not hundreds.
                if (LastSeen.TryGetValue(key, out var seen) && (e.When - seen).TotalSeconds < 60) continue;
                LastSeen[key] = e.When;
                ExceptionRaised?.Invoke(e);
            }
        }

        public static void Flush()
        {
            lock (Gate) { try { _writer?.Flush(); } catch (IOException) { } }
        }

        public static void End()
        {
            lock (Gate)
            {
                Application.logMessageReceivedThreaded -= OnLog;
                try { _writer?.Flush(); _writer?.Dispose(); } catch (IOException) { }
                _writer = null;
            }
        }

        /// <summary>Test hook: forget this run's file so the next <see cref="Begin"/> starts again.</summary>
        public static void ResetForTests()
        {
            End();
            CurrentFile = null;
            Exceptions.Clear();
            LastSeen.Clear();
            while (Pending.TryDequeue(out _)) { }
        }
    }
}
