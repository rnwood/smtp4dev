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
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Rnwood.Smtp4dev.Desktop
{
    class Program
    {

        [STAThread]
        static async Task Main(string[] args)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                StaTaskScheduler scheduler = new StaTaskScheduler(64);
                await Task.Factory.StartNew(() => Run(args), CancellationToken.None, TaskCreationOptions.None, scheduler);
            }
            else
            {
                Run(args);
            }
        }

        private static async Task Run(string[] args)
        {
            Rnwood.Smtp4dev.Program.SetupStaticLogger();
            string origWorkingDir = AppContext.BaseDirectory;

            try
            {


                var host =
                       await Rnwood.Smtp4dev.Program.StartApp(args, true, o => o.Urls = "http://127.0.0.1:0");


                var addressesFeature = host.ServerFeatures.Get<IServerAddressesFeature>();
                var urls = addressesFeature.Addresses;
                var appUrl = new Uri(urls.First());

                DesktopApp.Run(origWorkingDir, appUrl);

                await host.StopAsync();


            }
            catch (CommandLineOptionsException ex)
            {
                DesktopApp.ShowFatalError(origWorkingDir, "Command Line " + (ex.IsHelpRequest ? "Help" : "Error"), ex.Message);
                Environment.Exit(1);
                return;
            }
            finally
            {

                DesktopApp.Exit();
            }
        }

    }



}
