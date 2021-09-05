using System.Collections.Generic;

namespace Rnwood.Smtp4dev.RulesEngine
{
    /// <summary>
    /// Properties that configure additional relay settings when rule is successful
    /// </summary>
    public class RelayMessageProps
    {
        /// <summary>
        /// List of additional email addresses to dispatch relay messaged to
        /// </summary>
        public List<string> RelayAdditionalAddress { get; set; } = new List<string>();
        /// <summary>
        /// Should the relay forward to the original recipient?
        /// </summary>
        public bool RelayToOriginalAddress { get; set; }
        /// <summary>
        /// Relay to additional recipients listed in RelayAdditionalAddress
        /// </summary>
        public bool RelayToAdditionalAddress { get; set; }
    }
}