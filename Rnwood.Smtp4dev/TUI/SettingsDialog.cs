using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Terminal.Gui;
using Rnwood.Smtp4dev.Server;
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
        
        // Server settings
        private TextField smtpPortField;
        private TextField hostnameField;
        private CheckBox remoteConnectionsCheck;
        private TextField imapPortField;
        private TextField pop3PortField;
        private TextField basePathField;
        private TextField bindAddressField;
        private CheckBox disableIPv6Check;
        
        // Storage settings
        private TextField messagesToKeepField;
        private TextField sessionsToKeepField;
        private TextField databaseField;
        
        // TLS settings
        private ComboBox tlsModeCombo;
        private TextField tlsCertificateField;
        private TextField tlsCertificatePasswordField;
        private CheckBox secureConnectionRequiredCheck;
        
        // Authentication settings
        private CheckBox authenticationRequiredCheck;
        private CheckBox smtpAllowAnyCredentialsCheck;
        private CheckBox webAuthenticationRequiredCheck;
        
        // Message processing settings
        private CheckBox disableMessageSanitisationCheck;
        private CheckBox disableHtmlValidationCheck;
        private CheckBox disableHtmlCompatibilityCheckCheck;
        private TextField maxMessageSizeField;
        
        // Relay settings
        private TextField relayServerField;
        private TextField relayPortField;
        private TextField relayUsernameField;
        private TextField relayPasswordField;
        private TextField relaySenderAddressField;
        private ComboBox relayTlsModeCombo;

        public SettingsDialog(IHost host, string dataDir) : base("Settings", 100, 40)
        {
            this.host = host;
            this.dataDir = dataDir;
            CreateUI();
        }

        private void CreateUI()
        {
            // Create a scrollable view for all settings
            var scrollView = new ScrollView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill() - 4,
                ContentSize = new Size(90, 60),
                ShowVerticalScrollIndicator = true,
                ShowHorizontalScrollIndicator = false
            };

            var contentView = new View()
            {
                X = 0,
                Y = 0,
                Width = 90,
                Height = 60
            };

            var y = 0;

            // SMTP Server Settings section
            contentView.Add(new Label("═══ SMTP Server Settings ═══")
            {
                X = 1,
                Y = y++,
                ColorScheme = Colors.Dialog
            });

            contentView.Add(new Label("Port:")
            {
                X = 3,
                Y = y
            });
            smtpPortField = new TextField("")
            {
                X = 30,
                Y = y++,
                Width = 10
            };
            contentView.Add(smtpPortField);

            contentView.Add(new Label("Hostname:")
            {
                X = 3,
                Y = y
            });
            hostnameField = new TextField("")
            {
                X = 30,
                Y = y++,
                Width = 40
            };
            contentView.Add(hostnameField);

            contentView.Add(new Label("Base Path:")
            {
                X = 3,
                Y = y
            });
            basePathField = new TextField("")
            {
                X = 30,
                Y = y++,
                Width = 40
            };
            contentView.Add(basePathField);

            contentView.Add(new Label("Bind Address:")
            {
                X = 3,
                Y = y
            });
            bindAddressField = new TextField("")
            {
                X = 30,
                Y = y++,
                Width = 30
            };
            contentView.Add(bindAddressField);

            remoteConnectionsCheck = new CheckBox("Allow Remote Connections")
            {
                X = 3,
                Y = y++
            };
            contentView.Add(remoteConnectionsCheck);

            disableIPv6Check = new CheckBox("Disable IPv6")
            {
                X = 3,
                Y = y++
            };
            contentView.Add(disableIPv6Check);

            y++; // Blank line

            // IMAP/POP3 Settings section
            contentView.Add(new Label("═══ IMAP/POP3 Settings ═══")
            {
                X = 1,
                Y = y++,
                ColorScheme = Colors.Dialog
            });

            contentView.Add(new Label("IMAP Port:")
            {
                X = 3,
                Y = y
            });
            imapPortField = new TextField("")
            {
                X = 30,
                Y = y++,
                Width = 10
            };
            contentView.Add(imapPortField);

            contentView.Add(new Label("POP3 Port:")
            {
                X = 3,
                Y = y
            });
            pop3PortField = new TextField("")
            {
                X = 30,
                Y = y++,
                Width = 10
            };
            contentView.Add(pop3PortField);

            y++; // Blank line

            // TLS Settings section
            contentView.Add(new Label("═══ TLS/Security Settings ═══")
            {
                X = 1,
                Y = y++,
                ColorScheme = Colors.Dialog
            });

            contentView.Add(new Label("TLS Mode:")
            {
                X = 3,
                Y = y
            });
            tlsModeCombo = new ComboBox()
            {
                X = 30,
                Y = y++,
                Width = 20,
                Height = 5
            };
            tlsModeCombo.SetSource(new List<string> { "None", "StartTls", "ImplicitTls" });
            contentView.Add(tlsModeCombo);

            contentView.Add(new Label("TLS Certificate:")
            {
                X = 3,
                Y = y
            });
            tlsCertificateField = new TextField("")
            {
                X = 30,
                Y = y++,
                Width = 40
            };
            contentView.Add(tlsCertificateField);

            contentView.Add(new Label("TLS Certificate Password:")
            {
                X = 3,
                Y = y
            });
            tlsCertificatePasswordField = new TextField("")
            {
                X = 30,
                Y = y++,
                Width = 30,
                Secret = true
            };
            contentView.Add(tlsCertificatePasswordField);

            secureConnectionRequiredCheck = new CheckBox("Require Secure Connection")
            {
                X = 3,
                Y = y++
            };
            contentView.Add(secureConnectionRequiredCheck);

            y++; // Blank line

            // Authentication Settings section
            contentView.Add(new Label("═══ Authentication Settings ═══")
            {
                X = 1,
                Y = y++,
                ColorScheme = Colors.Dialog
            });

            authenticationRequiredCheck = new CheckBox("Authentication Required (SMTP/IMAP)")
            {
                X = 3,
                Y = y++
            };
            contentView.Add(authenticationRequiredCheck);

            smtpAllowAnyCredentialsCheck = new CheckBox("SMTP Allow Any Credentials")
            {
                X = 3,
                Y = y++
            };
            contentView.Add(smtpAllowAnyCredentialsCheck);

            webAuthenticationRequiredCheck = new CheckBox("Web Authentication Required")
            {
                X = 3,
                Y = y++
            };
            contentView.Add(webAuthenticationRequiredCheck);

            y++; // Blank line

            // Storage Settings section
            contentView.Add(new Label("═══ Storage Settings ═══")
            {
                X = 1,
                Y = y++,
                ColorScheme = Colors.Dialog
            });

            contentView.Add(new Label("Database File:")
            {
                X = 3,
                Y = y
            });
            databaseField = new TextField("")
            {
                X = 30,
                Y = y++,
                Width = 40
            };
            contentView.Add(databaseField);

            contentView.Add(new Label("Messages to Keep:")
            {
                X = 3,
                Y = y
            });
            messagesToKeepField = new TextField("")
            {
                X = 30,
                Y = y++,
                Width = 10
            };
            contentView.Add(messagesToKeepField);

            contentView.Add(new Label("Sessions to Keep:")
            {
                X = 3,
                Y = y
            });
            sessionsToKeepField = new TextField("")
            {
                X = 30,
                Y = y++,
                Width = 10
            };
            contentView.Add(sessionsToKeepField);

            y++; // Blank line

            // Message Processing Settings section
            contentView.Add(new Label("═══ Message Processing Settings ═══")
            {
                X = 1,
                Y = y++,
                ColorScheme = Colors.Dialog
            });

            contentView.Add(new Label("Max Message Size (bytes):")
            {
                X = 3,
                Y = y
            });
            maxMessageSizeField = new TextField("")
            {
                X = 30,
                Y = y++,
                Width = 15
            };
            contentView.Add(maxMessageSizeField);

            disableMessageSanitisationCheck = new CheckBox("Disable Message Sanitisation")
            {
                X = 3,
                Y = y++
            };
            contentView.Add(disableMessageSanitisationCheck);

            disableHtmlValidationCheck = new CheckBox("Disable HTML Validation")
            {
                X = 3,
                Y = y++
            };
            contentView.Add(disableHtmlValidationCheck);

            disableHtmlCompatibilityCheckCheck = new CheckBox("Disable HTML Compatibility Check")
            {
                X = 3,
                Y = y++
            };
            contentView.Add(disableHtmlCompatibilityCheckCheck);

            y++; // Blank line

            // Relay Settings section
            contentView.Add(new Label("═══ Relay Settings ═══")
            {
                X = 1,
                Y = y++,
                ColorScheme = Colors.Dialog
            });

            contentView.Add(new Label("Server:")
            {
                X = 3,
                Y = y
            });
            relayServerField = new TextField("")
            {
                X = 30,
                Y = y++,
                Width = 40
            };
            contentView.Add(relayServerField);

            contentView.Add(new Label("Port:")
            {
                X = 3,
                Y = y
            });
            relayPortField = new TextField("")
            {
                X = 30,
                Y = y++,
                Width = 10
            };
            contentView.Add(relayPortField);

            contentView.Add(new Label("TLS Mode:")
            {
                X = 3,
                Y = y
            });
            relayTlsModeCombo = new ComboBox()
            {
                X = 30,
                Y = y++,
                Width = 20,
                Height = 5
            };
            relayTlsModeCombo.SetSource(new List<string> { "None", "Auto", "SslOnConnect", "StartTls", "StartTlsWhenAvailable" });
            contentView.Add(relayTlsModeCombo);

            contentView.Add(new Label("Username:")
            {
                X = 3,
                Y = y
            });
            relayUsernameField = new TextField("")
            {
                X = 30,
                Y = y++,
                Width = 30
            };
            contentView.Add(relayUsernameField);

            contentView.Add(new Label("Password:")
            {
                X = 3,
                Y = y
            });
            relayPasswordField = new TextField("")
            {
                X = 30,
                Y = y++,
                Width = 30,
                Secret = true
            };
            contentView.Add(relayPasswordField);

            contentView.Add(new Label("Sender Address:")
            {
                X = 3,
                Y = y
            });
            relaySenderAddressField = new TextField("")
            {
                X = 30,
                Y = y++,
                Width = 40
            };
            contentView.Add(relaySenderAddressField);

            scrollView.Add(contentView);
            Add(scrollView);

            // Load current settings
            LoadSettings();

            // Main action buttons at bottom
            var usersButton = new Button("Manage Users")
            {
                X = 2,
                Y = Pos.Bottom(this) - 3
            };
            usersButton.Clicked += ManageUsers;
            AddButton(usersButton);

            var mailboxesButton = new Button("Manage Mailboxes")
            {
                X = Pos.Right(usersButton) + 2,
                Y = Pos.Bottom(this) - 3
            };
            mailboxesButton.Clicked += ManageMailboxes;
            AddButton(mailboxesButton);

            var saveButton = new Button("Save")
            {
                X = Pos.Right(mailboxesButton) + 5,
                Y = Pos.Bottom(this) - 3,
                IsDefault = true
            };
            saveButton.Clicked += SaveSettings;
            AddButton(saveButton);

            var cancelButton = new Button("Cancel")
            {
                X = Pos.Right(saveButton) + 2,
                Y = Pos.Bottom(this) - 3
            };
            cancelButton.Clicked += () => Application.RequestStop();
            AddButton(cancelButton);
        }

        private void LoadSettings()
        {
            var settingsManager = new SettingsManager(host, dataDir);
            var serverOptions = settingsManager.GetServerOptions();

            // Server settings
            smtpPortField.Text = serverOptions.Port.ToString();
            hostnameField.Text = serverOptions.HostName ?? "";
            basePathField.Text = serverOptions.BasePath ?? "/";
            bindAddressField.Text = serverOptions.BindAddress ?? "";
            remoteConnectionsCheck.Checked = serverOptions.AllowRemoteConnections;
            disableIPv6Check.Checked = serverOptions.DisableIPv6;
            imapPortField.Text = serverOptions.ImapPort?.ToString() ?? "143";
            pop3PortField.Text = serverOptions.Pop3Port?.ToString() ?? "110";

            // TLS settings
            tlsModeCombo.SelectedItem = (int)serverOptions.TlsMode;
            tlsCertificateField.Text = serverOptions.TlsCertificate ?? "";
            tlsCertificatePasswordField.Text = serverOptions.TlsCertificatePassword ?? "";
            secureConnectionRequiredCheck.Checked = serverOptions.SecureConnectionRequired;

            // Authentication settings
            authenticationRequiredCheck.Checked = serverOptions.AuthenticationRequired;
            smtpAllowAnyCredentialsCheck.Checked = serverOptions.SmtpAllowAnyCredentials;
            webAuthenticationRequiredCheck.Checked = serverOptions.WebAuthenticationRequired;

            // Storage settings
            databaseField.Text = serverOptions.Database ?? "database.db";
            messagesToKeepField.Text = serverOptions.NumberOfMessagesToKeep.ToString();
            sessionsToKeepField.Text = serverOptions.NumberOfSessionsToKeep.ToString();

            // Message processing settings
            maxMessageSizeField.Text = serverOptions.MaxMessageSize?.ToString() ?? "";
            disableMessageSanitisationCheck.Checked = serverOptions.DisableMessageSanitisation;
            disableHtmlValidationCheck.Checked = serverOptions.DisableHtmlValidation;
            disableHtmlCompatibilityCheckCheck.Checked = serverOptions.DisableHtmlCompatibilityCheck;

            // Relay settings
            var relayOptions = settingsManager.GetRelayOptions();
            relayServerField.Text = relayOptions.SmtpServer ?? "";
            relayPortField.Text = relayOptions.SmtpPort.ToString();
            relayTlsModeCombo.SelectedItem = (int)relayOptions.TlsMode;
            relayUsernameField.Text = relayOptions.Login ?? "";
            relayPasswordField.Text = relayOptions.Password ?? "";
            relaySenderAddressField.Text = relayOptions.SenderAddress ?? "";
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
                serverOptions.BindAddress = string.IsNullOrWhiteSpace(bindAddressField.Text.ToString()) ? null : bindAddressField.Text.ToString();
                serverOptions.AllowRemoteConnections = remoteConnectionsCheck.Checked;
                serverOptions.DisableIPv6 = disableIPv6Check.Checked;

                if (int.TryParse(imapPortField.Text.ToString(), out int imapPort))
                    serverOptions.ImapPort = imapPort;

                if (int.TryParse(pop3PortField.Text.ToString(), out int pop3Port))
                    serverOptions.Pop3Port = pop3Port;

                // TLS settings
                serverOptions.TlsMode = (TlsMode)tlsModeCombo.SelectedItem;
                serverOptions.TlsCertificate = tlsCertificateField.Text.ToString();
                serverOptions.TlsCertificatePassword = tlsCertificatePasswordField.Text.ToString();
                serverOptions.SecureConnectionRequired = secureConnectionRequiredCheck.Checked;

                // Authentication settings
                serverOptions.AuthenticationRequired = authenticationRequiredCheck.Checked;
                serverOptions.SmtpAllowAnyCredentials = smtpAllowAnyCredentialsCheck.Checked;
                serverOptions.WebAuthenticationRequired = webAuthenticationRequiredCheck.Checked;

                // Storage settings
                serverOptions.Database = databaseField.Text.ToString();
                if (int.TryParse(messagesToKeepField.Text.ToString(), out int messagesToKeep))
                    serverOptions.NumberOfMessagesToKeep = messagesToKeep;

                if (int.TryParse(sessionsToKeepField.Text.ToString(), out int sessionsToKeep))
                    serverOptions.NumberOfSessionsToKeep = sessionsToKeep;

                // Message processing settings
                if (long.TryParse(maxMessageSizeField.Text.ToString(), out long maxMessageSize))
                    serverOptions.MaxMessageSize = maxMessageSize;
                else if (string.IsNullOrWhiteSpace(maxMessageSizeField.Text.ToString()))
                    serverOptions.MaxMessageSize = null;

                serverOptions.DisableMessageSanitisation = disableMessageSanitisationCheck.Checked;
                serverOptions.DisableHtmlValidation = disableHtmlValidationCheck.Checked;
                serverOptions.DisableHtmlCompatibilityCheck = disableHtmlCompatibilityCheckCheck.Checked;

                // Update relay options
                relayOptions.SmtpServer = relayServerField.Text.ToString();
                if (int.TryParse(relayPortField.Text.ToString(), out int relayPort))
                    relayOptions.SmtpPort = relayPort;
                relayOptions.TlsMode = (MailKit.Security.SecureSocketOptions)relayTlsModeCombo.SelectedItem;
                relayOptions.Login = relayUsernameField.Text.ToString();
                relayOptions.Password = relayPasswordField.Text.ToString();
                relayOptions.SenderAddress = relaySenderAddressField.Text.ToString();

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
