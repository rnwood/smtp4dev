using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Terminal.Gui;
using Rnwood.Smtp4dev.Data;
using Rnwood.Smtp4dev.Server;

namespace Rnwood.Smtp4dev.TUI
{
    public class TuiApp
    {
        private readonly IHost host;
        private readonly CancellationTokenSource cancellationTokenSource;
        private Window mainWindow;
        private TabView tabView;
        private MessageListView messageListView;
        private SessionListView sessionListView;
        private ServerStatusView serverStatusView;

        public TuiApp(IHost host)
        {
            this.host = host;
            this.cancellationTokenSource = new CancellationTokenSource();
        }

        public void Run()
        {
            // Initialize Terminal.Gui
            Application.Init();

            try
            {
                // Create main window
                mainWindow = new Window("smtp4dev - TUI")
                {
                    X = 0,
                    Y = 0,
                    Width = Dim.Fill(),
                    Height = Dim.Fill()
                };

                // Create menu bar
                var menuBar = new MenuBar(new MenuBarItem[]
                {
                    new MenuBarItem("_File", new MenuItem[]
                    {
                        new MenuItem("_Quit", "", () => { Application.RequestStop(); })
                    }),
                    new MenuBarItem("_Help", new MenuItem[]
                    {
                        new MenuItem("_About", "", () =>
                        {
                            MessageBox.Query("About", "smtp4dev TUI\nVersion 3.3.0-dev\nhttps://github.com/rnwood/smtp4dev", "OK");
                        })
                    })
                });

                // Create tab view
                tabView = new TabView()
                {
                    X = 0,
                    Y = 1,
                    Width = Dim.Fill(),
                    Height = Dim.Fill() - 1
                };

                // Create tabs
                var messagesTab = new TabView.Tab("Messages", CreateMessagesTab());
                var sessionsTab = new TabView.Tab("Sessions", CreateSessionsTab());
                var serverTab = new TabView.Tab("Server Status", CreateServerTab());

                tabView.AddTab(messagesTab, true);
                tabView.AddTab(sessionsTab, false);
                tabView.AddTab(serverTab, false);

                // Create status bar
                var statusBar = new StatusBar(new StatusItem[]
                {
                    new StatusItem(Key.F1, "~F1~ Help", () =>
                    {
                        MessageBox.Query("Help", "Use Tab to navigate between tabs\nUse arrow keys to navigate lists\nPress Enter to view details", "OK");
                    }),
                    new StatusItem(Key.F10, "~F10~ Quit", () => { Application.RequestStop(); })
                });

                mainWindow.Add(menuBar);
                mainWindow.Add(tabView);

                Application.Top.Add(mainWindow, statusBar);

                // Start refresh timer
                var timer = Application.MainLoop.AddTimeout(TimeSpan.FromSeconds(2), (_) =>
                {
                    RefreshViews();
                    return true;
                });

                // Run the application
                Application.Run();
            }
            finally
            {
                Application.Shutdown();
            }
        }

        private View CreateMessagesTab()
        {
            var container = new View()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            var dbContext = host.Services.GetRequiredService<Smtp4devDbContext>();
            messageListView = new MessageListView(dbContext)
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            container.Add(messageListView);
            return container;
        }

        private View CreateSessionsTab()
        {
            var container = new View()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            var dbContext = host.Services.GetRequiredService<Smtp4devDbContext>();
            sessionListView = new SessionListView(dbContext)
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            container.Add(sessionListView);
            return container;
        }

        private View CreateServerTab()
        {
            var container = new View()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            var server = host.Services.GetRequiredService<ISmtp4devServer>();
            var imapServer = host.Services.GetRequiredService<ImapServer>();
            serverStatusView = new ServerStatusView(server, imapServer)
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            container.Add(serverStatusView);
            return container;
        }

        private void RefreshViews()
        {
            try
            {
                messageListView?.Refresh();
                sessionListView?.Refresh();
                serverStatusView?.Refresh();
            }
            catch (Exception ex)
            {
                // Log error but don't crash the TUI
                System.Diagnostics.Debug.WriteLine($"Error refreshing views: {ex.Message}");
            }
        }
    }
}
