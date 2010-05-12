#region

using System.Linq;
using System.Reflection;
using Microsoft.Win32;

#endregion

namespace Rnwood.Smtp4dev
{
    public static class RegistrySettings
    {
        public static bool StartOnLogin
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