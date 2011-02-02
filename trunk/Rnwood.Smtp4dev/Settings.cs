using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Win32;

namespace Rnwood.Smtp4dev.Properties
{
    partial class Settings
    {
        protected override void OnSettingsLoaded(object sender, SettingsLoadedEventArgs e)
        {
            StartOnLogin = StartOnLoginInternal;
        }

        public override void Save()
        {
            base.Save();

            StartOnLoginInternal = StartOnLogin;
        }

        public DirectoryInfo MessageFolder
        {
            get
            {
                if (!string.IsNullOrEmpty(CustomMessageFolder))
                {
                    return new DirectoryInfo(CustomMessageFolder);
                }

                return
                    new DirectoryInfo(
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                     "smtp4dev\\Messages"));
            }
        }

        public bool StartOnLogin
        {
            get;
            set;
        }

        private bool StartOnLoginInternal
        {
            get
            {
                return
                    Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run").GetValueNames().
                        Any(name => name == "smtp4dev");
            }

            set
            {
                if (value)
                {
                    Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run").SetValue(
                        "smtp4dev", Assembly.GetEntryAssembly().Location);
                }
                else
                {
                    Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run").DeleteValue(
                        "smtp4dev", false);
                }
            }
        }
    }
}
