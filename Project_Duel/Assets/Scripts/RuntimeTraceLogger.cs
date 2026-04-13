using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace JunzhenDuijue
{
    public static class RuntimeTraceLogger
    {
        private static readonly object SyncRoot = new object();
        [ThreadStatic] private static bool _isWriting;

        private static bool _isInitialized;
        private static bool _sessionStarted;
        private static string _logPath;

        public static string LogPath
        {
            get
            {
                Initialize();
                return _logPath;
            }
        }

        public static void Initialize()
        {
            if (_isInitialized)
                return;

            _logPath = ResolveLogPath();
            try
            {
                string directory = Path.GetDirectoryName(_logPath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);
            }
            catch { }

            Application.logMessageReceivedThreaded -= OnUnityLogMessageReceived;
            Application.logMessageReceivedThreaded += OnUnityLogMessageReceived;
            _isInitialized = true;
        }

        public static void MarkSessionStart(string source)
        {
            Initialize();
            if (_sessionStarted)
                return;

            _sessionStarted = true;
            WriteLine("SESSION", source, "=== Session started ===");
            WriteLine("SESSION", source, "TraceLogPath=" + _logPath);
            WriteLine("SESSION", source, "UnityVersion=" + Application.unityVersion + ", Platform=" + Application.platform + ", DataPath=" + Application.dataPath);
        }

        public static void Trace(string source, string message)
        {
            Initialize();
            WriteLine("TRACE", source, message);
        }

        public static void Exception(string source, Exception exception)
        {
            if (exception == null)
                return;

            Initialize();
            WriteLine("EXCEPTION", source, exception.ToString());
        }

        private static void OnUnityLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            string message = string.IsNullOrWhiteSpace(stackTrace)
                ? condition
                : condition + Environment.NewLine + stackTrace;
            WriteLine(type.ToString().ToUpperInvariant(), "Unity", message);
        }

        private static void WriteLine(string level, string source, string message)
        {
            if (_isWriting)
                return;

            try
            {
                _isWriting = true;
                string logPath = _logPath ?? ResolveLogPath();
                string directory = Path.GetDirectoryName(logPath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                string line = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "]"
                    + " [" + (level ?? "TRACE") + "]"
                    + " [" + (source ?? "Unknown") + "] "
                    + (message ?? string.Empty);

                lock (SyncRoot)
                {
                    File.AppendAllText(logPath, line + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch { }
            finally
            {
                _isWriting = false;
            }
        }

        private static string ResolveLogPath()
        {
#if UNITY_EDITOR
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "client-runtime.log"));
#else
            return Path.Combine(Application.persistentDataPath, "Logs", "client-runtime.log");
#endif
        }
    }
}
