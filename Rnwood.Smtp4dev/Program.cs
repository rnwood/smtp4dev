using System.Diagnostics;
using System.IO;
using System.Linq;

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

using Rnwood.Smtp4dev.Service;

namespace Rnwood.Smtp4dev
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var isService = !Debugger.IsAttached && args.Contains("--service");
            var builder = CreateWebHost(args.Where(arg => arg != "--service").ToArray());
            if (isService)
            {
                var pathToExe = Process.GetCurrentProcess().MainModule.FileName;
                var pathToContentRoot = Path.GetDirectoryName(pathToExe);
                builder.UseContentRoot(pathToContentRoot);
            }

            var host = builder.Build();

            if (isService)
            {
                host.RunAsSmtp4devService();
            }
            else
            {
                host.Run();
            }
        }

        public static IWebHostBuilder CreateWebHost(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddCommandLine(args)
                .Build();

            return WebHost.CreateDefaultBuilder(args).UseConfiguration(config).UseStartup<Startup>();
        }
    }
}
