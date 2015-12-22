using Microsoft.Extensions.DependencyInjection;
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
            services.AddInstance<ISettingsStore>(settingsStore);

            string messagesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Smtp4dev", "Messages");

            MessageStore messageStore = new MessageStore(new DirectoryInfo(messagesPath));
            services.AddInstance<IMessageStore>(messageStore);

            Smtp4devEngine server = new Smtp4devEngine(settingsStore, messageStore);
            services.AddInstance<ISmtp4devEngine>(server);
        }
    }
}