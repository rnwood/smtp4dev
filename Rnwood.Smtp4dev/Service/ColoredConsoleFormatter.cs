using System;
using System.IO;
using Serilog.Events;
using Serilog.Formatting;

namespace Rnwood.Smtp4dev.Service
{
    /// <summary>
    /// A Serilog formatter that outputs log messages with colors and conditional emoji support.
    /// Emojis are only shown for Warning, Error, and Fatal levels, and only if emoji support is detected.
    /// </summary>
    public class ColoredConsoleFormatter : ITextFormatter
    {
        private readonly bool _useEmoji;

        // ANSI escape codes for colors
        private const string Reset = "\x1b[0m";
        private const string Gray = "\x1b[90m";        // Dark gray for Debug/Verbose
        private const string Cyan = "\x1b[36m";        // Cyan for Information
        private const string Yellow = "\x1b[33m";      // Yellow for Warning
        private const string Red = "\x1b[31m";         // Red for Error
        private const string BrightRed = "\x1b[91m";   // Bright red for Fatal

        /// <summary>
        /// Creates a new ColoredConsoleFormatter.
        /// </summary>
        public ColoredConsoleFormatter() : this(ConsoleHelper.IsEmojiSupported)
        {
        }

        /// <summary>
        /// Creates a new ColoredConsoleFormatter with explicit emoji control.
        /// </summary>
        /// <param name="useEmoji">Whether to use emoji prefixes for warnings and errors.</param>
        public ColoredConsoleFormatter(bool useEmoji)
        {
            _useEmoji = useEmoji;
        }

        /// <summary>
        /// Format the log event to the output.
        /// </summary>
        public void Format(LogEvent logEvent, TextWriter output)
        {
            var (color, prefix) = GetLevelFormatting(logEvent.Level);
            
            // Write the prefix (emoji or plain) with color
            output.Write(color);
            output.Write(prefix);
            
            // Render the message
            logEvent.RenderMessage(output);
            output.Write(Reset);
            output.WriteLine();
            
            // Write exception if present
            if (logEvent.Exception != null)
            {
                output.Write(Red);
                output.WriteLine(logEvent.Exception.ToString());
                output.Write(Reset);
            }
        }

        private (string color, string prefix) GetLevelFormatting(LogEventLevel level)
        {
            return level switch
            {
                LogEventLevel.Verbose => (Gray, "[VRB] "),
                LogEventLevel.Debug => (Gray, "[DBG] "),
                LogEventLevel.Information => (Cyan, "[INF] "),
                LogEventLevel.Warning => _useEmoji ? (Yellow, "⚠️  ") : (Yellow, "[WRN] "),
                LogEventLevel.Error => _useEmoji ? (Red, "❌ ") : (Red, "[ERR] "),
                LogEventLevel.Fatal => _useEmoji ? (BrightRed, "💀 ") : (BrightRed, "[FTL] "),
                _ => ("", "")
            };
        }
    }
}
