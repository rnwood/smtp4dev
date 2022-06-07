using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text;
using PhotinoNET;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server.Features;
using System.Reflection;
using System.Threading.Tasks.Schedulers;

namespace Rnwood.Smtp4dev.Desktop
{
    class Program
    {
        [STAThread]
        static async Task Main(string[] args)
        {
            Console.Write("smtp4dev Desktop");


            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                StaTaskScheduler scheduler = new StaTaskScheduler(1);
                await Task.Factory.StartNew(Run, CancellationToken.None, TaskCreationOptions.None, scheduler);
            } else
            {
                await Run();
            }
        }

        private static async Task Run()
        {
            try
            {
                string origWorkingDir = AppContext.BaseDirectory;

                Environment.CurrentDirectory = Path.Join(origWorkingDir, "server");
                var host = await
                     Rnwood.Smtp4dev.Program.StartApp(new string[] { "--urls=http://127.0.0.1:0" }).ConfigureAwait(true);

                var addressesFeature = host.ServerFeatures.Get<IServerAddressesFeature>();
                var urls = addressesFeature.Addresses;
                var appUrl = new Uri(urls.First());


                DesktopApp.Run(args, origWorkingDir, appUrl);

                await host.StopAsync().ConfigureAwait(true);

            }
            finally
            {

                DesktopApp.Exit();
            }
        }

        private static void ShowFatalError(string title, string details)
        {
            Console.Error.WriteLine(title);
            Console.Error.WriteLine(details);
            Environment.Exit(1);
        }


    }



}
