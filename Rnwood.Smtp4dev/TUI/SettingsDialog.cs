using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Terminal.Gui;
using Rnwood.Smtp4dev.Server.Settings;

namespace Rnwood.Smtp4dev.TUI
{
    /// <summary>
    /// Settings dialog with form-like layout for editing configuration
    /// </summary>
    public class SettingsDialog : Dialog
    {
        private readonly IHost host;
        private readonly string dataDir;
        private TextField smtpPortField;
        private TextField hostnameField;
        private CheckBox remoteConnectionsCheck;
        private TextField imapPortField;
        private TextField relayServerField;
        private TextField relayPortField;
        private TextField messagesToKeepField;
        private TextField sessionsToKeepField;

        public SettingsDialog(IHost host, string dataDir) : base("Settings", 80, 25)
        {
            this.host = host;
            this.dataDir = dataDir;
            CreateUI();
        }

        private void CreateUI()
        {
            var y = 0;

            // SMTP Settings section
            var smtpLabel = new Label("SMTP Server Settings:")
            {
                X = 1,
                Y = y++,
                ColorScheme = Colors.TopLevel
            };
            Add(smtpLabel);

            Add(new Label("Port:")
            {
                X = 3,
                Y = y
            });
            smtpPortField = new TextField("")
            {
                X = 20,
                Y = y++,
                Width = 10
            };
            Add(smtpPortField);

            Add(new Label("Hostname:")
            {
                X = 3,
                Y = y
            });
            hostnameField = new TextField("")
            {
                X = 20,
                Y = y++,
                Width = 40
            };
            Add(hostnameField);

            remoteConnectionsCheck = new CheckBox("Allow Remote Connections")
            {
                X = 3,
                Y = y++
            };
            Add(remoteConnectionsCheck);

            y++; // Blank line

            // IMAP Settings section
            var imapLabel = new Label("IMAP Server Settings:")
            {
                X = 1,
                Y = y++,
                ColorScheme = Colors.TopLevel
            };
            Add(imapLabel);

            Add(new Label("Port:")
            {
                X = 3,
                Y = y
            });
            imapPortField = new TextField("")
            {
                X = 20,
                Y = y++,
                Width = 10
            };
            Add(imapPortField);

            y++; // Blank line

            // Relay Settings section
            var relayLabel = new Label("Relay Settings:")
            {
                X = 1,
                Y = y++,
                ColorScheme = Colors.TopLevel
            };
            Add(relayLabel);

            Add(new Label("Server:")
            {
                X = 3,
                Y = y
            });
            relayServerField = new TextField("")
            {
                X = 20,
                Y = y++,
                Width = 40
            };
            Add(relayServerField);

            Add(new Label("Port:")
            {
                X = 3,
                Y = y
            });
            relayPortField = new TextField("")
            {
                X = 20,
                Y = y++,
                Width = 10
            };
            Add(relayPortField);

            y++; // Blank line

            // Storage Settings section
            var storageLabel = new Label("Storage Settings:")
            {
                X = 1,
                Y = y++,
                ColorScheme = Colors.TopLevel
            };
            Add(storageLabel);

            Add(new Label("Messages to Keep:")
            {
                X = 3,
                Y = y
            });
            messagesToKeepField = new TextField("")
            {
                X = 20,
                Y = y++,
                Width = 10
            };
            Add(messagesToKeepField);

            Add(new Label("Sessions to Keep:")
            {
                X = 3,
                Y = y
            });
            sessionsToKeepField = new TextField("")
            {
                X = 20,
                Y = y++,
                Width = 10
            };
            Add(sessionsToKeepField);

            // Load current settings
            LoadSettings();

            // Buttons
            var saveButton = new Button("Save")
            {
                X = Pos.Center() - 10,
                Y = Pos.Bottom(this) - 4,
                IsDefault = true
            };
            saveButton.Clicked += SaveSettings;
            AddButton(saveButton);

            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += () => Application.RequestStop();
            AddButton(cancelButton);

            var usersButton = new Button("Manage Users")
            {
                X = 3,
                Y = Pos.Bottom(this) - 4
            };
            usersButton.Clicked += ManageUsers;
            Add(usersButton);

            var mailboxesButton = new Button("Manage Mailboxes")
            {
                X = Pos.Right(usersButton) + 2,
                Y = Pos.Bottom(this) - 4
            };
            mailboxesButton.Clicked += ManageMailboxes;
            Add(mailboxesButton);
        }

        private void LoadSettings()
        {
            var settingsManager = new SettingsManager(host, dataDir);
            var serverOptions = settingsManager.GetServerOptions();

            smtpPortField.Text = serverOptions.Port.ToString();
            hostnameField.Text = serverOptions.HostName ?? "";
            remoteConnectionsCheck.Checked = serverOptions.AllowRemoteConnections;
            imapPortField.Text = serverOptions.ImapPort.ToString();

            var relayOptions = settingsManager.GetRelayOptions();
            relayServerField.Text = relayOptions.SmtpServer ?? "";
            relayPortField.Text = relayOptions.SmtpPort.ToString();

            messagesToKeepField.Text = serverOptions.NumberOfMessagesToKeep.ToString();
            sessionsToKeepField.Text = serverOptions.NumberOfSessionsToKeep.ToString();
        }

        private void SaveSettings()
        {
            try
            {
                var settingsManager = new SettingsManager(host, dataDir);
                var serverOptions = settingsManager.GetServerOptions();
                var relayOptions = settingsManager.GetRelayOptions();

                // Update server options
                if (int.TryParse(smtpPortField.Text.ToString(), out int smtpPort))
                    serverOptions.Port = smtpPort;
                
                serverOptions.HostName = hostnameField.Text.ToString();
                serverOptions.AllowRemoteConnections = remoteConnectionsCheck.Checked;

                if (int.TryParse(imapPortField.Text.ToString(), out int imapPort))
                    serverOptions.ImapPort = imapPort;

                if (int.TryParse(messagesToKeepField.Text.ToString(), out int messagesToKeep))
                    serverOptions.NumberOfMessagesToKeep = messagesToKeep;

                if (int.TryParse(sessionsToKeepField.Text.ToString(), out int sessionsToKeep))
                    serverOptions.NumberOfSessionsToKeep = sessionsToKeep;

                // Update relay options
                relayOptions.SmtpServer = relayServerField.Text.ToString();
                if (int.TryParse(relayPortField.Text.ToString(), out int relayPort))
                    relayOptions.SmtpPort = relayPort;

                // Save settings
                settingsManager.SaveSettings(serverOptions, relayOptions).Wait();

                MessageBox.Query("Settings Saved", 
                    "Settings have been saved successfully.\n\n" +
                    "Note: Server restart required for changes to take effect.",
                    "OK");

                Application.RequestStop();
            }
            catch (Exception ex)
            {
                MessageBox.ErrorQuery("Error", $"Failed to save settings: {ex.Message}", "OK");
            }
        }

        private void ManageUsers()
        {
            var usersDialog = new UsersDialog(host, dataDir);
            Application.Run(usersDialog);
        }

        private void ManageMailboxes()
        {
            var mailboxesDialog = new MailboxesDialog(host, dataDir);
            Application.Run(mailboxesDialog);
        }
    }
}
