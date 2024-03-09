using PhotinoNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Rnwood.Smtp4dev.Desktop
{


    internal static class DesktopApp
    {
        private static PhotinoWindow _mainWindow;

        internal static void Run(string workingDir, Uri baseUrl)
        {
            _mainWindow = CreateWindow(workingDir);
            _mainWindow.Load(baseUrl);
            _mainWindow.WaitForClose();
        }

        internal static void ShowFatalError(string workingDir, string title, string error)
        {
            _mainWindow = CreateWindow(workingDir, $"smtp4dev - {title}");
            _mainWindow.LoadRawString($"<html><h1>{HttpUtility.HtmlEncode(title)}</h1><pre>{HttpUtility.HtmlEncode(error)}</pre><button type='button' onclick='window.location = \'https://www.google.co.uk\''>Close</button></body></html>");
            _mainWindow.WaitForClose();
        }

        private static PhotinoWindow CreateWindow(string workingDir, string title = "smtp4dev")
        {
            var iconFile = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
    ? "app/icon.ico"
    : "app/icon.svg";

            var result = new PhotinoWindow()
                .SetIconFile(Path.Combine(workingDir, iconFile))
                .SetTitle(title)
                .SetDevToolsEnabled(true)
                .SetUseOsDefaultLocation(true)
                .SetMinSize(800,600)
                .SetUseOsDefaultSize(true)
                .SetContextMenuEnabled(false);

            result.RegisterWebMessageReceivedHandler((s, m) =>
            {
                switch (m)
                {
                    case "close":
                        result.Close();
                        break;
                }
            });

            return result;
        }

        internal static void Exit()
        {
            _mainWindow?.Close();
        }
    }
}
