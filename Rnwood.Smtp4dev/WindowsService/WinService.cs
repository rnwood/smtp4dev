using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Hosting.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.WindowsService
{
    public class WinService : ServiceBase
    {
        private IApplication _webApplication;

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);

            Run();
        }

        protected override void OnStop()
        {
            base.OnStop();

            Shutdown();
        }

        private void Shutdown()
        {
            _webApplication.Dispose();
        }

        internal void Run()
        {
            try
            {
                IServiceProvider serviceProvider = CallContextServiceLocator.Locator.ServiceProvider;

                var tempBuilder = new ConfigurationBuilder();
                var tempConfig = tempBuilder.Build();

                var appBasePath = serviceProvider.GetRequiredService<IApplicationEnvironment>().ApplicationBasePath;
                var builder = new ConfigurationBuilder();
                builder.SetBasePath(appBasePath);
                builder.AddEnvironmentVariables();
                var config = builder.Build();

                var host = new WebHostBuilder(config).UseServer("Microsoft.AspNet.Server.Kestrel").Build();
                _webApplication = host.Start();
            }
            catch (Exception e)
            {
                File.WriteAllText(@"c:\error.txt", e.ToString());
            }
        }

        public static void Main(string[] args)
        {
            WinService service = new WinService();

            service.Run();
            Console.WriteLine("Running. Press ENTER to quit");
            Console.ReadLine();
        }
    }
}