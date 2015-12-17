using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.Model
{
    public static class ServiceExtensions
    {
        public static void UseSmtp4dev(this IServiceCollection services)
        {
            SettingsStore settingsStore = new SettingsStore();
            services.AddInstance<ISettingsStore>(settingsStore);

            Smtp4devServer server = new Smtp4devServer(settingsStore);
            services.AddInstance<ISmtp4devServer>(server);
        }
    }
}