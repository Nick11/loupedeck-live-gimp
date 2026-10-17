namespace Loupedeck.GimpPlugin
{
    using System;

    // Static access to the plugin's log file (Logi Plugin Service: Logs/plugin_logs/).
    internal static class PluginLog
    {
        private static PluginLogFile _log;

        public static void Init(PluginLogFile log) => _log = log;

        public static void Verbose(String text) => _log?.Verbose(text);

        public static void Info(String text) => _log?.Info(text);

        public static void Warning(String text) => _log?.Warning(text);

        public static void Error(Exception ex, String text) => _log?.Error(ex, text);
    }
}
