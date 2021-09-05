using System.Collections.Generic;

namespace Rnwood.Smtp4dev.RulesEngine
{
    public class RelayMessageProps
    {
        public List<string> RelayAdditionalAddress { get; set; }
        public bool RelayToOriginalAddress { get; set; }
        public bool RelayToAdditionalAddress { get; set; }
    }
}