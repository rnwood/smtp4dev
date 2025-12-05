using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Rnwood.Smtp4dev.Service
{
    /// <summary>
    /// Helper class for console-related functionality including emoji support detection.
    /// </summary>
    public static class ConsoleHelper
    {
        private static bool? _emojiSupported;

        /// <summary>
        /// Gets whether emoji rendering is likely supported by the current console.
        /// </summary>
        public static bool IsEmojiSupported
        {
            get
            {
                if (_emojiSupported.HasValue)
                {
                    return _emojiSupported.Value;
                }

                _emojiSupported = DetectEmojiSupport();
                return _emojiSupported.Value;
            }
        }

        /// <summary>
        /// Detects whether the console is likely to support emoji rendering.
        /// This uses heuristics based on environment variables and terminal type.
        /// </summary>
        private static bool DetectEmojiSupport()
        {
            // Check for explicit environment variable to disable emojis
            var noEmoji = Environment.GetEnvironmentVariable("SMTP4DEV_NO_EMOJI");
            if (!string.IsNullOrEmpty(noEmoji) && 
                (noEmoji.Equals("1", StringComparison.OrdinalIgnoreCase) || 
                 noEmoji.Equals("true", StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            // Check for explicit environment variable to force emojis
            var forceEmoji = Environment.GetEnvironmentVariable("SMTP4DEV_FORCE_EMOJI");
            if (!string.IsNullOrEmpty(forceEmoji) && 
                (forceEmoji.Equals("1", StringComparison.OrdinalIgnoreCase) || 
                 forceEmoji.Equals("true", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // If not running in a console, emojis likely won't display correctly
            if (Console.IsInputRedirected || Console.IsOutputRedirected)
            {
                return false;
            }

            // Check for Windows Terminal, which supports emojis
            var wtSession = Environment.GetEnvironmentVariable("WT_SESSION");
            if (!string.IsNullOrEmpty(wtSession))
            {
                return true;
            }

            // Check for VS Code terminal
            var termProgram = Environment.GetEnvironmentVariable("TERM_PROGRAM");
            if (!string.IsNullOrEmpty(termProgram) && termProgram.Contains("vscode", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Check for common terminals that support emojis on Unix
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var term = Environment.GetEnvironmentVariable("TERM");
                if (!string.IsNullOrEmpty(term))
                {
                    // Most modern terminals on Linux/macOS support emojis
                    // xterm-256color, screen-256color, etc.
                    if (term.Contains("256color", StringComparison.OrdinalIgnoreCase) ||
                        term.Contains("truecolor", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                // Check for macOS Terminal.app which supports emojis
                var appleTerminal = Environment.GetEnvironmentVariable("TERM_PROGRAM");
                if (!string.IsNullOrEmpty(appleTerminal) && 
                    (appleTerminal.Contains("Apple_Terminal", StringComparison.OrdinalIgnoreCase) ||
                     appleTerminal.Contains("iTerm", StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }

                // On non-Windows systems with UTF-8 locale, emojis likely work
                var lang = Environment.GetEnvironmentVariable("LANG") ?? "";
                var lcAll = Environment.GetEnvironmentVariable("LC_ALL") ?? "";
                if (lang.Contains("UTF-8", StringComparison.OrdinalIgnoreCase) ||
                    lcAll.Contains("UTF-8", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            else
            {
                // Windows: Check console output encoding and mode
                try
                {
                    // Windows 10 1903+ with UTF-8 support
                    if (Console.OutputEncoding.WebName.Equals("utf-8", StringComparison.OrdinalIgnoreCase))
                    {
                        // Also check if we're in conhost or Windows Terminal
                        // Conhost has limited emoji support even with UTF-8
                        // The presence of WT_SESSION already checked above
                        
                        // For modern Windows console host, check for ConEmu or other enhanced terminals
                        var conEmuANSI = Environment.GetEnvironmentVariable("ConEmuANSI");
                        if (!string.IsNullOrEmpty(conEmuANSI))
                        {
                            return true;
                        }

                        // Default Windows console without Windows Terminal has poor emoji support
                        // Return false unless we detected a known good terminal above
                        return false;
                    }
                }
                catch
                {
                    // Error checking encoding, assume no emoji support
                    return false;
                }
            }

            // Default: assume no emoji support to be safe
            return false;
        }

        /// <summary>
        /// Sets the console output encoding to UTF-8 if not already set.
        /// Call this early in program startup.
        /// </summary>
        public static void EnsureUtf8Console()
        {
            try
            {
                Console.OutputEncoding = Encoding.UTF8;
            }
            catch
            {
                // Ignore errors, some environments don't allow changing encoding
            }
        }
    }
}
