using AntDesign.ProLayout;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Rnwood.Smtp4dev.Client;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");

        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
        builder.Services.AddAntDesign();
        builder.Services.Configure<ProSettings>(builder.Configuration.GetSection("ProSettings"));
        builder.Services.AddScoped<MessagesClient>();
        builder.Services.AddScoped<InfoClient>();
        builder.Services.AddScoped(sp =>
        {
            var result = new HubConnectionManager(builder.HostEnvironment.BaseAddress + "hubs/notifications");
            var notAwaited = result.StartAsync();
            return result;
        });

        await builder.Build().RunAsync();
    }
}