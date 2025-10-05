using System;
using System.Collections.Generic;
using System.Linq;

namespace Rnwood.Smtp4dev.TUI
{
    /// <summary>
    /// Manages keyboard shortcuts for TUI navigation
    /// </summary>
    public class KeyboardShortcuts
    {
        private readonly Dictionary<ConsoleKey, Action> shortcuts = new Dictionary<ConsoleKey, Action>();
        private readonly Dictionary<ConsoleKey, string> descriptions = new Dictionary<ConsoleKey, string>();

        public void Register(ConsoleKey key, Action action, string description)
        {
            shortcuts[key] = action;
            descriptions[key] = description;
        }

        public bool HandleKey(ConsoleKey key)
        {
            if (shortcuts.TryGetValue(key, out var action))
            {
                action();
                return true;
            }
            return false;
        }

        public string GetHelp()
        {
            var help = "Keyboard Shortcuts:\n";
            foreach (var kvp in descriptions.OrderBy(k => k.Key))
            {
                help += $"  {kvp.Key}: {kvp.Value}\n";
            }
            return help;
        }

        public static KeyboardShortcuts CreateDefault()
        {
            var shortcuts = new KeyboardShortcuts();
            // Common shortcuts will be registered by TuiApp
            return shortcuts;
        }
    }
}
