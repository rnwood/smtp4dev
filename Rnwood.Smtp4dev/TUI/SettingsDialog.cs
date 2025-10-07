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
        private CheckBox requireAuthCheck;
        private TextField imapPortField;
        private TextField pop3PortField;
        private TextField relayServerField;
        private TextField relayPortField;
        private TextField relayUsernameField;
        private TextField relayPasswordField;
        private TextField messagesToKeepField;
        private TextField sessionsToKeepField;
        private TextField basePathField;

        public SettingsDialog(IHost host, string dataDir) : base("Settings", 90, 30)
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
                X = 25,
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
                X = 25,
                Y = y++,
                Width = 40
            };
            Add(hostnameField);

            Add(new Label("Base Path:")
            {
                X = 3,
                Y = y
            });
            basePathField = new TextField("")
            {
                X = 25,
                Y = y++,
                Width = 40
            };
            Add(basePathField);

            remoteConnectionsCheck = new CheckBox("Allow Remote Connections")
            {
                X = 3,
                Y = y++
            };
            Add(remoteConnectionsCheck);

            requireAuthCheck = new CheckBox("Require Authentication")
            {
                X = 3,
                Y = y++
            };
            Add(requireAuthCheck);

            y++; // Blank line

            // IMAP/POP3 Settings section
            var imapLabel = new Label("IMAP/POP3 Server Settings:")
            {
                X = 1,
                Y = y++,
                ColorScheme = Colors.TopLevel
            };
            Add(imapLabel);

            Add(new Label("IMAP Port:")
            {
                X = 3,
                Y = y
            });
            imapPortField = new TextField("")
            {
                X = 25,
                Y = y++,
                Width = 10
            };
            Add(imapPortField);

            Add(new Label("POP3 Port:")
            {
                X = 3,
                Y = y
            });
            pop3PortField = new TextField("")
            {
                X = 25,
                Y = y++,
                Width = 10
            };
            Add(pop3PortField);

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
                X = 25,
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
                X = 25,
                Y = y++,
                Width = 10
            };
            Add(relayPortField);

            Add(new Label("Username:")
            {
                X = 3,
                Y = y
            });
            relayUsernameField = new TextField("")
            {
                X = 25,
                Y = y++,
                Width = 30
            };
            Add(relayUsernameField);

            Add(new Label("Password:")
            {
                X = 3,
                Y = y
            });
            relayPasswordField = new TextField("")
            {
                X = 25,
                Y = y++,
                Width = 30,
                Secret = true
            };
            Add(relayPasswordField);

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
                X = 25,
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
                X = 25,
                Y = y++,
                Width = 10
            };
            Add(sessionsToKeepField);

            // Load current settings
            LoadSettings();

            // Main action buttons - prominently placed
            var usersButton = new Button("Manage Users")
            {
                X = Pos.Center() - 30,
                Y = Pos.Bottom(this) - 4
            };
            usersButton.Clicked += ManageUsers;
            AddButton(usersButton);

            var mailboxesButton = new Button("Manage Mailboxes")
            {
                X = Pos.Center() - 10,
                Y = Pos.Bottom(this) - 4
            };
            mailboxesButton.Clicked += ManageMailboxes;
            AddButton(mailboxesButton);

            var saveButton = new Button("Save")
            {
                X = Pos.Center() + 15,
                Y = Pos.Bottom(this) - 4,
                IsDefault = true
            };
            saveButton.Clicked += SaveSettings;
            AddButton(saveButton);

            var cancelButton = new Button("Cancel")
            {
                X = Pos.Center() + 25,
                Y = Pos.Bottom(this) - 4
            };
            cancelButton.Clicked += () => Application.RequestStop();
            AddButton(cancelButton);
        }

        private void LoadSettings()
        {
            var settingsManager = new SettingsManager(host, dataDir);
            var serverOptions = settingsManager.GetServerOptions();

            smtpPortField.Text = serverOptions.Port.ToString();
            hostnameField.Text = serverOptions.HostName ?? "";
            basePathField.Text = serverOptions.BasePath ?? "/";
            remoteConnectionsCheck.Checked = serverOptions.AllowRemoteConnections;
            requireAuthCheck.Checked = serverOptions.DisableMessageSanitisation; // Using as proxy for auth requirement
            imapPortField.Text = serverOptions.ImapPort.ToString();
            pop3PortField.Text = serverOptions.Pop3Port.ToString();

            var relayOptions = settingsManager.GetRelayOptions();
            relayServerField.Text = relayOptions.SmtpServer ?? "";
            relayPortField.Text = relayOptions.SmtpPort.ToString();
            relayUsernameField.Text = relayOptions.Login ?? "";
            relayPasswordField.Text = relayOptions.Password ?? "";

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
                serverOptions.BasePath = basePathField.Text.ToString();
                serverOptions.AllowRemoteConnections = remoteConnectionsCheck.Checked;
                serverOptions.DisableMessageSanitisation = requireAuthCheck.Checked;

                if (int.TryParse(imapPortField.Text.ToString(), out int imapPort))
                    serverOptions.ImapPort = imapPort;

                if (int.TryParse(pop3PortField.Text.ToString(), out int pop3Port))
                    serverOptions.Pop3Port = pop3Port;

                if (int.TryParse(messagesToKeepField.Text.ToString(), out int messagesToKeep))
                    serverOptions.NumberOfMessagesToKeep = messagesToKeep;

                if (int.TryParse(sessionsToKeepField.Text.ToString(), out int sessionsToKeep))
                    serverOptions.NumberOfSessionsToKeep = sessionsToKeep;

                // Update relay options
                relayOptions.SmtpServer = relayServerField.Text.ToString();
                if (int.TryParse(relayPortField.Text.ToString(), out int relayPort))
                    relayOptions.SmtpPort = relayPort;
                relayOptions.Login = relayUsernameField.Text.ToString();
                relayOptions.Password = relayPasswordField.Text.ToString();

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
