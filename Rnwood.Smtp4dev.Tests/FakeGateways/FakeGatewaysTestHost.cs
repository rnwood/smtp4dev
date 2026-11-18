using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rnwood.Smtp4dev.FakeGateways;

namespace Rnwood.Smtp4dev.Tests.FakeGateways
{
    /// <summary>
    /// Minimal real Kestrel host (random loopback port) containing only the fake gateway controllers,
    /// not the whole smtp4dev application.
    /// </summary>
    public sealed class FakeGatewaysTestHost : IAsyncDisposable
    {
        private readonly WebApplication app;

        private FakeGatewaysTestHost(WebApplication app, HttpClient client, CapturingLoggerProvider logs)
        {
            this.app = app;
            Client = client;
            Logs = logs;
        }

        public HttpClient Client { get; }
        public CapturingLoggerProvider Logs { get; }
        public IServiceProvider Services => app.Services;
        public SmsStore SmsStore => app.Services.GetRequiredService<SmsStore>();
        public TargitoStore TargitoStore => app.Services.GetRequiredService<TargitoStore>();

        public class Options
        {
            public bool Enabled { get; set; } = true;
            public Dictionary<string, string> Settings { get; } = new Dictionary<string, string>();

            /// <summary>Adds a fallback policy requiring an authenticated user (like Startup does for web authentication).</summary>
            public bool RequireAuthentication { get; set; }
            public Action<IServiceCollection> ConfigureServices { get; set; }
        }

        public static async Task<FakeGatewaysTestHost> StartAsync(Action<Options> configure = null)
        {
            var options = new Options();
            configure?.Invoke(options);

            var logs = new CapturingLoggerProvider();
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Logging.ClearProviders();
            builder.Logging.AddProvider(logs);
            builder.Logging.SetMinimumLevel(LogLevel.Trace);

            var settings = new Dictionary<string, string> { ["FakeGateways:Enabled"] = options.Enabled.ToString() };
            foreach (var pair in options.Settings)
            {
                settings[pair.Key] = pair.Value;
            }
            builder.Configuration.AddInMemoryCollection(settings);

            builder.Services.AddControllers().ConfigureApplicationPartManager(m =>
            {
                m.ApplicationParts.Clear();
                m.ApplicationParts.Add(new TypesPart(typeof(FakeGatewaysOptions).Assembly.GetTypes()
                    .Where(t => t.Namespace == typeof(FakeGatewaysOptions).Namespace && typeof(ControllerBase).IsAssignableFrom(t))));
            });
            builder.Services.AddSignalR();
            builder.Services.AddFakeGateways(builder.Configuration);

            if (options.RequireAuthentication)
            {
                builder.Services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, DenyAllAuthHandler>("Test", null);
                builder.Services.AddAuthorization(o => o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
            }

            options.ConfigureServices?.Invoke(builder.Services);

            var app = builder.Build();
            app.UseRouting();
            if (options.RequireAuthentication)
            {
                app.UseAuthentication();
            }
            app.UseAuthorization();
            app.MapControllers();
            await app.StartAsync();

            string address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>().Addresses.First();
            var client = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(30) };
            return new FakeGatewaysTestHost(app, client, logs);
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.StopAsync();
            await app.DisposeAsync();
        }

        private class TypesPart : ApplicationPart, IApplicationPartTypeProvider
        {
            public TypesPart(IEnumerable<Type> types)
            {
                Types = types.Select(t => t.GetTypeInfo()).ToList();
            }

            public override string Name => "FakeGatewaysTestControllers";
            public IEnumerable<TypeInfo> Types { get; }
        }

        private class DenyAllAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
        {
            public DenyAllAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
                : base(options, logger, encoder)
            {
            }

            protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.NoResult());
        }
    }

    public class CapturingLoggerProvider : ILoggerProvider
    {
        private readonly List<string> entries = new List<string>();

        public IReadOnlyList<string> Entries
        {
            get { lock (entries) { return entries.ToList(); } }
        }

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(this, categoryName);

        public void Dispose() { }

        private class CapturingLogger : ILogger
        {
            private readonly CapturingLoggerProvider provider;
            private readonly string category;

            public CapturingLogger(CapturingLoggerProvider provider, string category)
            {
                this.provider = provider;
                this.category = category;
            }

            public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                string text = $"{logLevel} {category}: {formatter(state, exception)} {exception}";
                lock (provider.entries)
                {
                    provider.entries.Add(text);
                }
            }
        }
    }
}
