#region

using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using Rnwood.AutoUpdate;
using Rnwood.Smtp4dev.Properties;

#endregion

namespace Rnwood.Smtp4dev
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(true);

            if (Settings.Default.SettingsUpgradeRequired)
            {
                Settings.Default.Upgrade();
                Settings.Default.SettingsUpgradeRequired = false;
                Settings.Default.Save();
            }

            CheckForUpdate();

            MainForm form = new MainForm();
            Application.Run(form);
        }

        private static void CheckForUpdate()
        {
            if (Settings.Default.EnableUpdateCheck)
            {
                if ((!Settings.Default.LastUpdateCheck.HasValue) || Settings.Default.LastUpdateCheck.Value.AddDays(Properties.Settings.Default.UpdateCheckInterval) < DateTime.Now)
                {
                    Settings.Default.LastUpdateCheck = DateTime.Now;
                    
                    try
                    {
                        CheckForUpdateCore();
                    }
                    catch (Exception e)
                    {
                        if (MessageBox.Show(string.Format("Failed to check for update ({0})\nPlease check Internet connection and proxy settings.\nWould you like smtp4dev to try again next time it is launched?", e.Message), "smtp4dev", MessageBoxButtons.YesNo) == DialogResult.No)
                        {
                            Settings.Default.EnableUpdateCheck = false;
                        }
                    }

                    Settings.Default.Save();
                }
            }
        }

        internal static bool CheckForUpdateCore()
        {
            UpdateChecker updateChecker = new UpdateChecker(new Uri(Properties.Settings.Default.UpdateURL), typeof(Program).Assembly.GetName().Version);
            return updateChecker.CheckForUpdate(Properties.Settings.Default.UpdateCheckIncludePrerelease);
        }
    }
}