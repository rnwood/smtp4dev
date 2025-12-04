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
        private readonly string dataDir;
        private TabView tabView;
        private MessagesTab messagesTab;
        private SessionsTab sessionsTab;
        private StatusBar statusBar;
        private bool running = true;

        public TerminalGuiApp(IHost host, string dataDir)
        {
            this.host = host;
            this.dataDir = dataDir;
        }

        public void Run()
        {

            try
            {
                           Application.Init();
            
                // Create tab view directly (no outer window frame)
                tabView = new TabView()
                {
                    X = 0,
                    Y = 0,
                    Width = Dim.Fill(),
                    Height = Dim.Fill() - 1 // Leave space for status bar
                };

                // Create tabs
                messagesTab = new MessagesTab(host);
                sessionsTab = new SessionsTab(host);

                var messagesTabPage = new TabView.Tab("Messages", messagesTab.GetView());
                var sessionsTabPage = new TabView.Tab("Sessions", sessionsTab.GetView());

                tabView.AddTab(messagesTabPage, false);
                tabView.AddTab(sessionsTabPage, false);

                // Create status bar
                statusBar = new StatusBar(new StatusItem[] {
                    new StatusItem(Key.F1, "~F1~ Help", ShowHelp),
                    new StatusItem(Key.F5, "~F5~ Refresh", RefreshCurrentTab),
                    new StatusItem(Key.F8, "~F8~ Theme", ChangeTheme),
                    new StatusItem(Key.F9, "~F9~ Settings", ShowSettings),
                    new StatusItem(Key.F10, "~F10~ Quit", () => {
                        running = false;
                        Application.RequestStop();
                    }),
                    new StatusItem(Key.Null, "smtp4dev", null)
                });

                Application.Top.Add(tabView, statusBar);
                                SplashScreen.Show(2000);

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
            var tabs = tabView.Tabs.ToList();
            if (tabs.Count > 0 && currentTab == tabs[0]) // Messages tab
            {
                messagesTab.Refresh();
            }
            else if (tabs.Count > 1 && currentTab == tabs[1]) // Sessions tab
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
                "F8  - Change theme\n" +
                "F9  - Open settings\n" +
                "F10 - Quit application\n\n" +
                "Tab - Switch between tabs\n" +
                "Arrow keys - Navigate lists\n" +
                "Enter - View details", 
                "OK");
        }

        private void ChangeTheme()
        {
            var themes = new List<string> { "Base (Blue/Cyan)", "Dark (Green)", "Light (Classic)" };
            var dialog = new Dialog("Select Theme", 50, 10);
            
            var listView = new ListView(themes)
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill() - 2,
                Height = Dim.Fill() - 4
            };
            
            dialog.Add(listView);
            
            var okButton = new Button("OK");
            okButton.Clicked += () => {
                var selected = listView.SelectedItem;
                switch (selected)
                {
                    case 0: // Base
                        Colors.Base.Normal = Application.Driver.MakeAttribute(Color.White, Color.Black);
                        Colors.Base.Focus = Application.Driver.MakeAttribute(Color.Black, Color.BrightCyan);
                        Colors.Base.HotNormal = Application.Driver.MakeAttribute(Color.BrightCyan, Color.Black);
                        Colors.Base.HotFocus = Application.Driver.MakeAttribute(Color.BrightYellow, Color.BrightCyan);
                        Colors.TopLevel.Normal = Application.Driver.MakeAttribute(Color.White, Color.Black);
                        Colors.TopLevel.Focus = Application.Driver.MakeAttribute(Color.Black, Color.BrightCyan);
                        break;
                    case 1: // Dark
                        Colors.Base.Normal = Application.Driver.MakeAttribute(Color.BrightGreen, Color.Black);
                        Colors.Base.Focus = Application.Driver.MakeAttribute(Color.Black, Color.BrightGreen);
                        Colors.Base.HotNormal = Application.Driver.MakeAttribute(Color.BrightYellow, Color.Black);
                        Colors.Base.HotFocus = Application.Driver.MakeAttribute(Color.BrightYellow, Color.BrightGreen);
                        Colors.TopLevel.Normal = Application.Driver.MakeAttribute(Color.BrightGreen, Color.Black);
                        Colors.TopLevel.Focus = Application.Driver.MakeAttribute(Color.Black, Color.BrightGreen);
                        break;
                    case 2: // Light
                        Colors.Base.Normal = Application.Driver.MakeAttribute(Color.Black, Color.Gray);
                        Colors.Base.Focus = Application.Driver.MakeAttribute(Color.White, Color.BrightBlue);
                        Colors.Base.HotNormal = Application.Driver.MakeAttribute(Color.BrightBlue, Color.Gray);
                        Colors.Base.HotFocus = Application.Driver.MakeAttribute(Color.BrightYellow, Color.BrightBlue);
                        Colors.TopLevel.Normal = Application.Driver.MakeAttribute(Color.Black, Color.Gray);
                        Colors.TopLevel.Focus = Application.Driver.MakeAttribute(Color.White, Color.BrightBlue);
                        break;
                }
                Application.Top.SetNeedsDisplay();
                Application.RequestStop();
            };
            dialog.AddButton(okButton);
            
            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += () => Application.RequestStop();
            dialog.AddButton(cancelButton);
            
            Application.Run(dialog);
        }

        private void ShowSettings()
        {
            var settingsDialog = new SettingsDialog(host, dataDir);
            Application.Run(settingsDialog);
        }
    }
}
