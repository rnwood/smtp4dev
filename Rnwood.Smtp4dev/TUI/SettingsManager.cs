using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Rnwood.Smtp4dev.Server.Settings;

namespace Rnwood.Smtp4dev.TUI
{
    /// <summary>
    /// Manages reading and writing settings to appsettings.json
    /// </summary>
    public class SettingsManager
    {
        private readonly string settingsPath;
        private readonly IOptionsMonitor<ServerOptions> serverOptionsMonitor;
        private readonly IOptionsMonitor<RelayOptions> relayOptionsMonitor;

        public SettingsManager(IHost host, string dataDir)
        {
            this.serverOptionsMonitor = host.Services.GetRequiredService<IOptionsMonitor<ServerOptions>>();
            this.relayOptionsMonitor = host.Services.GetRequiredService<IOptionsMonitor<RelayOptions>>();
            
            this.settingsPath = Path.Combine(dataDir, "appsettings.json");
        }

        public ServerOptions GetServerOptions() => serverOptionsMonitor.CurrentValue;
        public RelayOptions GetRelayOptions() => relayOptionsMonitor.CurrentValue;

        public async Task SaveSettings(ServerOptions serverOptions, RelayOptions relayOptions)
        {
            var settings = new
            {
                ServerOptions = serverOptions,
                RelayOptions = relayOptions
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(settings, options);
            await File.WriteAllTextAsync(settingsPath, json);
        }
    }
}
