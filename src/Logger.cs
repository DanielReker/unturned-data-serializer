using SDG.Unturned;

namespace UnturnedDataSerializer {
    internal static class Logger {
        private static string FormatMessage(string message) {
            return $"UnturnedDataSerializer | {message}";
        }
        
        public static void Log(string message) {
            CommandWindow.Log(FormatMessage(message));
        }
        
        public static void LogError(string message) {
            CommandWindow.LogError(FormatMessage(message));
        }
    }
}