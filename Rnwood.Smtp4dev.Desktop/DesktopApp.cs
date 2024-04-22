using H.NotifyIcon.Core;
using Microsoft.Extensions.Options;
using PhotinoNET;
using Rnwood.Smtp4dev.Server.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Rnwood.Smtp4dev.Desktop
{


    internal class DesktopApp
    {
        private PhotinoWindow mainWindow;

        private bool isHidden = false;
        private bool consoleVisible = false;
        private Uri baseUrl;
        private IOptionsMonitor<DesktopOptions> desktopOptions;
        private string workingDir;
        private TrayIconWithContextMenu trayIcon;
        private PopupMenuItem toggleVisibleMenuItem, toggleConsoleMenuItem;
        internal void Run(string workingDir, Uri baseUrl, Microsoft.Extensions.Options.IOptionsMonitor<Server.Settings.DesktopOptions> desktopOptions)
        {
            this.workingDir = workingDir;
            this.baseUrl = baseUrl;
            this.desktopOptions = desktopOptions;


            try
            {


                trayIcon = new TrayIconWithContextMenu(Guid.NewGuid().ToString());
                trayIcon.Icon = new Icon(File.OpenRead(Path.Combine(AppContext.BaseDirectory, "app/icon.ico"))).Handle;
                trayIcon.ToolTip = "smtp4dev";

                toggleVisibleMenuItem = new PopupMenuItem("Hide", (_, _) => SetVisibility(isHidden));
                toggleConsoleMenuItem = new PopupMenuItem("Show console", (_, _) => UpdateConsoleVisbilty(!consoleVisible));


                trayIcon.ContextMenu = new PopupMenu
                {
                    Items = {
                         toggleVisibleMenuItem,
                         toggleConsoleMenuItem ,
                    new PopupMenuItem("Exit", (_, _) => mainWindow?.Close())
                }
                };
                trayIcon.MessageWindow.MouseEventReceived += (sender, e) =>
                {
                    if (e.MouseEvent == MouseEvent.IconLeftMouseUp)
                    {
                        SetVisibility(isHidden);
                    }
                };
                trayIcon.Create();

                mainWindow = CreateWindow(workingDir, baseUrl.ToString());
                mainWindow.RegisterWebMessageReceivedHandler((s, m) =>
                {
                    switch (m)
                    {
                        case "close":
                            mainWindow.Close();
                            break;
                    }
                });

                UpdateTrayIconState(true);

                desktopOptions.OnChange((_, _) => UpdateTrayIconState(false));


                mainWindow.WindowMinimizedHandler = (_, _) =>
                {
                    if (this.desktopOptions.CurrentValue.MinimiseToTrayIcon)
                    {
                        SetVisibility(false);
                    }
                };

                UpdateConsoleVisbilty(false);



                mainWindow.WaitForClose();

            }
            finally
            {
                trayIcon?.Dispose();
                mainWindow?.Close();
            }

        }

        private void UpdateConsoleVisbilty(bool v)
        {
            if (v)
            {
                toggleConsoleMenuItem.Text = "Hide console";
                WindowUtilities.ShowWindow(GetConsoleWindow());
            }
            else
            {
                toggleConsoleMenuItem.Text = "Show console";
                HideConsole();
            }
            

            consoleVisible = v;
        }

        internal static void HideConsole()
        {
            WindowUtilities.HideWindow(GetConsoleWindow());
        }

        private void UpdateTrayIconState(bool initial)
        {
            if (this.desktopOptions.CurrentValue.MinimiseToTrayIcon)
            {
                this.trayIcon.Visibility = IconVisibility.Visible;

                if (!initial)
                {
                    this.trayIcon.Create();
                }
            }
            else
            {
                this.trayIcon.Visibility = IconVisibility.Hidden;

                if (this.trayIcon.IsCreated) { 
                    this.trayIcon.Remove();
            }

                if (this.isHidden)
                {
                    SetVisibility(true);
                }
                
            }
        }

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(nint hWnd, int cmd);


        private void SetVisibility(bool visible)
        {

            if (visible)
            {

                if(mainWindow.Maximized)
                {
                    ShowWindow(mainWindow.WindowHandle, (int)SW.SHOWMAXIMIZED);
                } else
                {
                    ShowWindow(mainWindow.WindowHandle, (int)SW.RESTORE);
                }
                WindowUtilities.SetForegroundWindow(mainWindow.WindowHandle);

                toggleVisibleMenuItem.Text = "Hide";


            }
            else
            {
                WindowUtilities.HideWindow(mainWindow.WindowHandle);
                toggleVisibleMenuItem.Text = "Show";



            }

            isHidden = !visible;
        }

        public enum SW : int
        {
            HIDE = 0,
            SHOWNORMAL = 1,
            SHOWMINIMIZED = 2,
            SHOWMAXIMIZED = 3,
            SHOWNOACTIVATE = 4,
            SHOW = 5,
            MINIMIZE = 6,
            SHOWMINNOACTIVE = 7,
            SHOWNA = 8,
            RESTORE = 9,
            SHOWDEFAULT = 10
        }

        [DllImport("kernel32.dll")]
        static extern nint GetConsoleWindow();


        internal static void ShowFatalError(string workingDir, string title, string error)
        {
            Console.Error.WriteLine(error);


            var window = CreateWindow(workingDir, "about:blank", $"smtp4dev - {title}");
            window.LoadRawString($"<html><h1>{HttpUtility.HtmlEncode(title)}</h1><pre>{HttpUtility.HtmlEncode(error)}</pre><button type='button' onclick='window.close()'>Close</button></body></html>");
            window.WaitForClose();

            Environment.Exit(1);
        }

        private static PhotinoWindow CreateWindow(string workingDir, string baseUrl, string title = "smtp4dev")
        {
            var iconFile = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
    ? "app/icon.ico"
    : "app/icon.svg";

            var result = new PhotinoWindow()
            {
                StartUrl = baseUrl
            }
                .SetIconFile(Path.Combine(workingDir, iconFile))
                .SetTitle(title)
                .SetDevToolsEnabled(Debugger.IsAttached)
                .SetUseOsDefaultLocation(false)
                .SetMinSize(800, 600)
                .SetMaximized(true)
                .SetUseOsDefaultSize(false)
                .SetContextMenuEnabled(false);


            return result;
        }

        internal void Exit()
        {
            trayIcon?.Dispose();
            mainWindow?.Close();
        }
    }
}
