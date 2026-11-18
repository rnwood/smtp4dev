#nullable enable
using System;
using System.Collections.Generic;

namespace Rnwood.Smtp4dev.FakeGateways
{
    /// <summary>
    /// Configuration of the fake gateways (section "FakeGateways").
    /// </summary>
    /// <remarks>
    /// Do not put this section into the user settings file (the one smtp4dev rewrites from the web UI settings page),
    /// use appsettings.json next to the binary, environment variables (FakeGateways__Enabled=true) or the
    /// --fakegateways command line switch.
    /// </remarks>
    public class FakeGatewaysOptions
    {
        public const string SectionName = "FakeGateways";

        /// <summary>Master switch. When false, fake endpoints and the API return 404.</summary>
        public bool Enabled { get; set; }

        /// <summary>Maximum number of items kept in memory per gateway (oldest are dropped).</summary>
        public int MaxStoredItems { get; set; } = 1000;

        public KonzultaOptions Konzulta { get; set; } = new KonzultaOptions();

        public TargitoOptions Targito { get; set; } = new TargitoOptions();

        public int EffectiveMaxStoredItems => MaxStoredItems < 1 ? 1000 : MaxStoredItems;
    }

    public class KonzultaOptions
    {
        /// <summary>Glob patterns of phone numbers (normalised, e.g. +420700000099 or +42070000*) which will be answered with Status=1.</summary>
        public string[] RefuseNumbers { get; set; } = Array.Empty<string>();
    }

    public class TargitoOptions
    {
        /// <summary>When set, JSON-RPC methods that are not simulated are forwarded to this URL.</summary>
        public string? UpstreamUrl { get; set; }

        /// <summary>Map of templateId to a human readable template name shown in the UI.</summary>
        public Dictionary<string, string> TemplateNames { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Command line binding source for <see cref="FakeGatewaysOptions"/> (--fakegateways).
    /// </summary>
    public record FakeGatewaysOptionsSource
    {
        public bool? Enabled { get; set; }
    }
}
