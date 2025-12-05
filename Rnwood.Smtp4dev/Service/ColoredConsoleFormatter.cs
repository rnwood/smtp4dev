using System;
using System.IO;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;

namespace Rnwood.Smtp4dev.Service
{
    /// <summary>
    /// A Serilog formatter that outputs log messages with colors and conditional emoji support.
    /// Emojis are only shown for Warning, Error, and Fatal levels, and only if emoji support is detected.
    /// Colors are disabled when output is redirected (e.g., captured by tests).
    /// </summary>
    public class ColoredConsoleFormatter : ITextFormatter
    {
        private readonly bool _useEmoji;
        private readonly bool _useColors;
        
        // Formatter that renders the message with :lj (literal, JSON-safe) to avoid quotes around string values
        private static readonly MessageTemplateTextFormatter _messageFormatter = 
            new MessageTemplateTextFormatter("{Message:lj}", null);

        // ANSI escape codes for colors
        private const string Reset = "\x1b[0m";
        private const string Gray = "\x1b[90m";        // Dark gray for Debug/Verbose
        private const string Cyan = "\x1b[36m";        // Cyan for Information
        private const string Yellow = "\x1b[33m";      // Yellow for Warning
        private const string Red = "\x1b[31m";         // Red for Error
        private const string BrightRed = "\x1b[91m";   // Bright red for Fatal

        /// <summary>
        /// Creates a new ColoredConsoleFormatter.
        /// Colors are automatically disabled when console output is redirected.
        /// </summary>
        public ColoredConsoleFormatter() : this(ConsoleHelper.IsEmojiSupported, !Console.IsOutputRedirected)
        {
        }

        /// <summary>
        /// Creates a new ColoredConsoleFormatter with explicit emoji control.
        /// Colors are automatically disabled when console output is redirected.
        /// </summary>
        /// <param name="useEmoji">Whether to use emoji prefixes for warnings and errors.</param>
        public ColoredConsoleFormatter(bool useEmoji) : this(useEmoji, !Console.IsOutputRedirected)
        {
        }

        /// <summary>
        /// Creates a new ColoredConsoleFormatter with explicit emoji and color control.
        /// </summary>
        /// <param name="useEmoji">Whether to use emoji prefixes for warnings and errors.</param>
        /// <param name="useColors">Whether to use ANSI color codes.</param>
        public ColoredConsoleFormatter(bool useEmoji, bool useColors)
        {
            _useEmoji = useEmoji;
            _useColors = useColors;
        }

        /// <summary>
        /// Format the log event to the output.
        /// </summary>
        public void Format(LogEvent logEvent, TextWriter output)
        {
            var (color, prefix) = GetLevelFormatting(logEvent.Level);
            
            // Write color code only if colors are enabled
            if (_useColors)
            {
                output.Write(color);
            }
            
            output.Write(prefix);
            
            // Render the message using :lj format (literal, no quotes around strings)
            using (var messageWriter = new StringWriter())
            {
                _messageFormatter.Format(logEvent, messageWriter);
                output.Write(messageWriter.ToString().TrimEnd());
            }
            
            if (_useColors)
            {
                output.Write(Reset);
            }
            output.WriteLine();
            
            // Write exception if present
            if (logEvent.Exception != null)
            {
                if (_useColors)
                {
                    output.Write(Red);
                }
                output.WriteLine(logEvent.Exception.ToString());
                if (_useColors)
                {
                    output.Write(Reset);
                }
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
