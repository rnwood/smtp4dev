using Microsoft.Extensions.DependencyInjection;
using Rnwood.Smtp4dev.API;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.Model
{
    public static class ServiceExtensions
    {
        public static void UseSmtp4dev(this IServiceCollection services)
        {
            SettingsStore settingsStore = new SettingsStore();
            services.AddSingleton<ISettingsStore>(settingsStore);

            MessageStore messageStore = new MessageStore();
            services.AddSingleton<IMessageStore>(messageStore);

            services.AddTransient<ISmtp4devEngine, Smtp4devEngine>();
        }
    }
}