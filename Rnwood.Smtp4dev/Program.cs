using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Rnwood.Smtp4dev
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(true);

            LaunchInfo launchInfo = new LaunchInfo(Environment.CurrentDirectory, args);
            SingleInstanceManager sim = new SingleInstanceManager(string.Format("SMTP4DEV{0}-65D4ED7D-1942-4f39-9F04-66875C67F04A-SESSION{1}", typeof(Program).Assembly.GetName().Version, Process.GetCurrentProcess().SessionId));
            if (sim.IsFirstInstance)
            {
                if (Properties.Settings.Default.SettingsUpgradeRequired)
                {
                    Properties.Settings.Default.Upgrade();
                    Properties.Settings.Default.SettingsUpgradeRequired = false;
                    Properties.Settings.Default.Save();
                }
                
                MainForm form = new MainForm(launchInfo);
                sim.LaunchInfoReceived += ((s, ea) => form.Invoke( (MethodInvoker) (() => form.ProcessLaunchInfo(ea.LaunchInfo, false))));
                sim.ListenForLaunches();

                Application.Run(form);
            } else
            {
                try
                {
                    sim.SendLaunchInfoToFirstInstance(new LaunchInfo(Environment.CurrentDirectory, args));
                }
                catch (Exception)
                {
                    MessageBox.Show("Failed to process command line", "smtp4dev", MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);
                }

            }
        }
    }
}
