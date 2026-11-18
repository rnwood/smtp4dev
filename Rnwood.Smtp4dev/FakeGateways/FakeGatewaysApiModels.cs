#nullable enable
using System.Collections.Generic;

namespace Rnwood.Smtp4dev.FakeGateways
{
    public class FakeGatewaysHealth
    {
        /// <summary>"ok" or "disabled".</summary>
        public string Status { get; set; } = "";
        public List<string> Features { get; set; } = new List<string>();
        public string? Version { get; set; }
    }

    public class FakeGatewaysError
    {
        public FakeGatewaysError(string error)
        {
            Error = error;
        }

        public string Error { get; set; }
    }
}
