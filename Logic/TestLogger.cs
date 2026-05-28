namespace TripsAndTriads
{
    /// <summary>
    /// Drop-in replacement for GD.Print / GD.PrintErr in the extracted logic layer.
    /// In tests, output is captured via <see cref="LastMessage"/> or suppressed entirely.
    /// In production Godot code the original GD.Print calls remain in the Scripts/ folder.
    /// </summary>
    public static class TestLogger
    {
        /// <summary>The last message that was logged. Useful for assertion in tests.</summary>
        public static string LastMessage { get; private set; } = string.Empty;

        /// <summary>All messages logged since the last <see cref="Clear"/> call.</summary>
        public static System.Collections.Generic.List<string> Messages { get; }
            = new System.Collections.Generic.List<string>();

        /// <summary>When true, writes to Console.WriteLine in addition to the buffer.</summary>
        public static bool WriteToConsole { get; set; } = false;

        public static void Log(string message)
        {
            LastMessage = message;
            Messages.Add(message);
            if (WriteToConsole) System.Console.WriteLine(message);
        }

        public static void Error(string message)
        {
            string prefixed = $"[ERR] {message}";
            LastMessage = prefixed;
            Messages.Add(prefixed);
            if (WriteToConsole) System.Console.Error.WriteLine(prefixed);
        }

        /// <summary>Clear the message buffer between tests.</summary>
        public static void Clear()
        {
            Messages.Clear();
            LastMessage = string.Empty;
        }
    }
}
