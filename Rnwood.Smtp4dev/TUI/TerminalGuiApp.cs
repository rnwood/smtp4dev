using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Terminal.Gui;
using Rnwood.Smtp4dev.Data;
using Rnwood.Smtp4dev.DbModel;
using Rnwood.Smtp4dev.Server;

namespace Rnwood.Smtp4dev.TUI
{
    /// <summary>
    /// Terminal.Gui-based TUI application that mirrors the web UI layout
    /// </summary>
    public class TerminalGuiApp
    {
        private readonly IHost host;
        private Window mainWindow;
        private TabView tabView;
        private MessagesTab messagesTab;
        private SessionsTab sessionsTab;
        private StatusBar statusBar;
        private bool running = true;

        public TerminalGuiApp(IHost host)
        {
            this.host = host;
        }

        public void Run()
        {
            Application.Init();

            try
            {
                // Create main window
                mainWindow = new Window("smtp4dev - Terminal UI")
                {
                    X = 0,
                    Y = 0,
                    Width = Dim.Fill(),
                    Height = Dim.Fill() - 1 // Leave space for status bar
                };

                // Create tab view
                tabView = new TabView()
                {
                    X = 0,
                    Y = 0,
                    Width = Dim.Fill(),
                    Height = Dim.Fill()
                };

                // Create tabs
                messagesTab = new MessagesTab(host);
                sessionsTab = new SessionsTab(host);

                var messagesTabPage = new TabView.Tab("Messages", messagesTab.GetView());
                var sessionsTabPage = new TabView.Tab("Sessions", sessionsTab.GetView());

                tabView.AddTab(messagesTabPage, false);
                tabView.AddTab(sessionsTabPage, false);

                mainWindow.Add(tabView);

                // Create status bar
                statusBar = new StatusBar(new StatusItem[] {
                    new StatusItem(Key.F1, "~F1~ Help", ShowHelp),
                    new StatusItem(Key.F5, "~F5~ Refresh", RefreshCurrentTab),
                    new StatusItem(Key.F9, "~F9~ Settings", ShowSettings),
                    new StatusItem(Key.F10, "~F10~ Quit", () => {
                        running = false;
                        Application.RequestStop();
                    })
                });

                Application.Top.Add(mainWindow, statusBar);

                // Start auto-refresh in background
                Task.Run(() => AutoRefreshLoop());

                Application.Run();
            }
            finally
            {
                running = false;
                Application.Shutdown();
            }
        }

        private void AutoRefreshLoop()
        {
            while (running)
            {
                Thread.Sleep(3000);
                if (running)
                {
                    Application.MainLoop.Invoke(() => {
                        RefreshCurrentTab();
                    });
                }
            }
        }

        private void RefreshCurrentTab()
        {
            var currentTab = tabView.SelectedTab;
            if (currentTab == 0) // Messages tab
            {
                messagesTab.Refresh();
            }
            else if (currentTab == 1) // Sessions tab
            {
                sessionsTab.Refresh();
            }
        }

        private void ShowHelp()
        {
            MessageBox.Query("Help", 
                "Keyboard Shortcuts:\n\n" +
                "F1  - Show this help\n" +
                "F5  - Refresh current tab\n" +
                "F9  - Open settings\n" +
                "F10 - Quit application\n\n" +
                "Tab - Switch between tabs\n" +
                "Arrow keys - Navigate lists\n" +
                "Enter - View details", 
                "OK");
        }

        private void ShowSettings()
        {
            var settingsDialog = new SettingsDialog(host);
            Application.Run(settingsDialog);
        }
    }
}
