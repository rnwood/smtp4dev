#nullable enable
using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Rnwood.Smtp4dev.FakeGateways
{
    public static class ServiceCollectionExtensions
    {
        public const string TargitoUpstreamHttpClientName = "FakeGateways.TargitoUpstream";

        /// <summary>
        /// Registers options (section "FakeGateways"), in-memory stores, the SignalR notifier and the upstream HTTP client.
        /// Controllers are discovered automatically by AddControllers (same assembly).
        /// </summary>
        public static IServiceCollection AddFakeGateways(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<FakeGatewaysOptions>(configuration.GetSection(FakeGatewaysOptions.SectionName));

            services.TryAddSingleton(TimeProvider.System);
            services.AddSingleton(sp => new SmsStore(sp.GetRequiredService<IOptions<FakeGatewaysOptions>>().Value.EffectiveMaxStoredItems));
            services.AddSingleton(sp => new TargitoStore(sp.GetRequiredService<IOptions<FakeGatewaysOptions>>().Value.EffectiveMaxStoredItems));

            services.AddHttpClient(TargitoUpstreamHttpClientName, c => c.Timeout = TimeSpan.FromSeconds(30));

            services.AddHostedService<FakeGatewaysNotifier>();
            return services;
        }
    }
}
