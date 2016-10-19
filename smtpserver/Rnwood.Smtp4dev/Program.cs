using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

#if NET461
using Microsoft.AspNetCore.Hosting.WindowsServices;
#endif

namespace Rnwood.Smtp4dev
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            if (args.Contains("--service", StringComparer.OrdinalIgnoreCase))
            {
#if NET461
                host.RunAsService();
#else
                Console.Error.WriteLine("ERROR --service not supported on this platform");
                Environment.Exit(1);
#endif
            }
            else
            {
                host.Run();
            }
        }
    }
}