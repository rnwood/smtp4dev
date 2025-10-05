using System;
using Terminal.Gui;
using Rnwood.Smtp4dev.Server;

namespace Rnwood.Smtp4dev.TUI
{
    public class ServerStatusView : View
    {
        private readonly ISmtp4devServer smtpServer;
        private readonly ImapServer imapServer;
        private TextView statusTextView;

        public ServerStatusView(ISmtp4devServer smtpServer, ImapServer imapServer)
        {
            this.smtpServer = smtpServer;
            this.imapServer = imapServer;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            var label = new Label("Server Status")
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = 1
            };

            var refreshButton = new Button("Refresh")
            {
                X = Pos.Right(label) - 12,
                Y = 0
            };
            refreshButton.Clicked += () => Refresh();

            statusTextView = new TextView()
            {
                X = 0,
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                ReadOnly = true,
                WordWrap = true
            };

            Add(label, refreshButton, statusTextView);

            // Load initial data
            Refresh();
        }

        public void Refresh()
        {
            try
            {
                var content = "=== SMTP4DEV SERVER STATUS ===\n\n";

                content += "SMTP Server:\n";
                content += $"  Status: {(smtpServer.IsRunning ? "Running" : "Stopped")}\n";
                
                if (smtpServer.Exception != null)
                {
                    content += $"  Error: {smtpServer.Exception.Message}\n";
                }

                if (smtpServer.ListeningEndpoints != null && smtpServer.ListeningEndpoints.Length > 0)
                {
                    content += "  Listening on:\n";
                    foreach (var endpoint in smtpServer.ListeningEndpoints)
                    {
                        content += $"    - {endpoint}\n";
                    }
                }
                else
                {
                    content += "  Not listening on any endpoints\n";
                }

                content += "\nIMAP Server:\n";
                content += $"  Status: {(imapServer.IsRunning ? "Running" : "Stopped")}\n";

                content += "\n\nPress F1 for help or F10 to quit\n";
                content += "Switch tabs with Tab key\n";
                content += "Refresh this view with the Refresh button\n";

                statusTextView.Text = content;
            }
            catch (Exception ex)
            {
                statusTextView.Text = $"Error loading server status: {ex.Message}";
            }
        }
    }
}
