using PhotinoNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.Desktop
{


    internal static class DesktopApp
    {
        private static PhotinoWindow _mainWindow;

        internal static void Run(string[] args, string workingDir, Uri baseUrl)
        {
            var iconFile = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
    ? "app/icon.ico"
    : "app/icon.svg";

            _mainWindow = new PhotinoWindow()
                .SetIconFile(Path.Combine(workingDir, iconFile))
                .SetTitle($"smtp4dev")
                .Load(baseUrl)
                .SetDevToolsEnabled(false)
                .SetContextMenuEnabled(false);


            _mainWindow.WaitForClose();
  
        }

        internal static void Exit()
        {
            _mainWindow.Close();
        }
    }
}
