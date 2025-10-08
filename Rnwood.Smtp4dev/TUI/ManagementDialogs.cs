using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Hosting;
using Terminal.Gui;
using Rnwood.Smtp4dev.Server.Settings;

namespace Rnwood.Smtp4dev.TUI
{
    /// <summary>
    /// Dialog for managing SMTP users
    /// </summary>
    public class UsersDialog : Dialog
    {
        private readonly IHost host;
        private readonly string dataDir;
        private ListView userListView;
        private SettingsManager settingsManager;
        private ServerOptions serverOptions;

        public UsersDialog(IHost host, string dataDir) : base("Manage Users", 60, 20)
        {
            this.host = host;
            this.dataDir = dataDir;
            this.settingsManager = new SettingsManager(host, dataDir);
            this.serverOptions = settingsManager.GetServerOptions();
            CreateUI();
        }

        private void CreateUI()
        {
            userListView = new ListView()
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill() - 2,
                Height = Dim.Fill() - 6
            };
            Add(userListView);

            var addButton = new Button("Add User")
            {
                X = 1,
                Y = Pos.Bottom(userListView) + 1
            };
            addButton.Clicked += AddUser;
            Add(addButton);

            var removeButton = new Button("Remove")
            {
                X = Pos.Right(addButton) + 2,
                Y = Pos.Bottom(userListView) + 1
            };
            removeButton.Clicked += RemoveUser;
            Add(removeButton);

            var closeButton = new Button("Close")
            {
                X = Pos.Center(),
                Y = Pos.Bottom(this) - 3
            };
            closeButton.Clicked += () => Application.RequestStop();
            AddButton(closeButton);

            RefreshList();
        }

        private void RefreshList()
        {
            var users = serverOptions.Users?.Select(u => u.Username).ToList() ?? new System.Collections.Generic.List<string>();
            userListView.SetSource(users);
        }

        private void AddUser()
        {
            var usernameField = new TextField() { X = 15, Y = 1, Width = 30 };
            var passwordField = new TextField() { X = 15, Y = 3, Width = 30, Secret = true };

            var dialog = new Dialog("Add User", 50, 10);
            dialog.Add(new Label("Username:") { X = 1, Y = 1 });
            dialog.Add(usernameField);
            dialog.Add(new Label("Password:") { X = 1, Y = 3 });
            dialog.Add(passwordField);

            var okButton = new Button("OK") { IsDefault = true };
            okButton.Clicked += () =>
            {
                var username = usernameField.Text.ToString();
                var password = passwordField.Text.ToString();

                if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                {
                    var usersList = serverOptions.Users?.ToList() ?? new List<UserOptions>();
                    
                    usersList.Add(new UserOptions
                    {
                        Username = username,
                        Password = password
                    });

                    serverOptions.Users = usersList.ToArray();
                    RefreshList();
                    Application.RequestStop();
                }
                else
                {
                    MessageBox.ErrorQuery("Error", "Username and password are required", "OK");
                }
            };
            dialog.AddButton(okButton);

            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += () => Application.RequestStop();
            dialog.AddButton(cancelButton);

            Application.Run(dialog);
        }

        private void RemoveUser()
        {
            if (userListView.SelectedItem >= 0)
            {
                if (serverOptions.Users != null && userListView.SelectedItem < serverOptions.Users.Length)
                {
                    var username = serverOptions.Users[userListView.SelectedItem].Username;
                    var result = MessageBox.Query("Remove User",
                        $"Remove user '{username}'?",
                        "Yes", "No");

                    if (result == 0)
                    {
                        var usersList = serverOptions.Users.ToList();
                        usersList.RemoveAt(userListView.SelectedItem);
                        serverOptions.Users = usersList.ToArray();
                        RefreshList();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Dialog for managing mailboxes
    /// </summary>
    public class MailboxesDialog : Dialog
    {
        private readonly IHost host;
        private readonly string dataDir;
        private ListView mailboxListView;
        private SettingsManager settingsManager;
        private ServerOptions serverOptions;

        public MailboxesDialog(IHost host, string dataDir) : base("Manage Mailboxes", 60, 20)
        {
            this.host = host;
            this.dataDir = dataDir;
            this.settingsManager = new SettingsManager(host, dataDir);
            this.serverOptions = settingsManager.GetServerOptions();
            CreateUI();
        }

        private void CreateUI()
        {
            mailboxListView = new ListView()
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill() - 2,
                Height = Dim.Fill() - 6
            };
            Add(mailboxListView);

            var addButton = new Button("Add Mailbox")
            {
                X = 1,
                Y = Pos.Bottom(mailboxListView) + 1
            };
            addButton.Clicked += AddMailbox;
            Add(addButton);

            var removeButton = new Button("Remove")
            {
                X = Pos.Right(addButton) + 2,
                Y = Pos.Bottom(mailboxListView) + 1
            };
            removeButton.Clicked += RemoveMailbox;
            Add(removeButton);

            var closeButton = new Button("Close")
            {
                X = Pos.Center(),
                Y = Pos.Bottom(this) - 3
            };
            closeButton.Clicked += () => Application.RequestStop();
            AddButton(closeButton);

            RefreshList();
        }

        private void RefreshList()
        {
            var mailboxes = serverOptions.Mailboxes?.Select(m => $"{m.Name} ({m.Recipients})").ToList() 
                ?? new System.Collections.Generic.List<string>();
            mailboxListView.SetSource(mailboxes);
        }

        private void AddMailbox()
        {
            var nameField = new TextField() { X = 20, Y = 1, Width = 30 };
            var patternField = new TextField() { X = 20, Y = 3, Width = 30 };

            var dialog = new Dialog("Add Mailbox", 55, 10);
            dialog.Add(new Label("Mailbox Name:") { X = 1, Y = 1 });
            dialog.Add(nameField);
            dialog.Add(new Label("Recipient Pattern:") { X = 1, Y = 3 });
            dialog.Add(patternField);

            var okButton = new Button("OK") { IsDefault = true };
            okButton.Clicked += () =>
            {
                var name = nameField.Text.ToString();
                var pattern = patternField.Text.ToString();

                if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(pattern))
                {
                    var mailboxesList = serverOptions.Mailboxes?.ToList() ?? new List<MailboxOptions>();
                    
                    mailboxesList.Add(new MailboxOptions
                    {
                        Name = name,
                        Recipients = pattern
                    });

                    serverOptions.Mailboxes = mailboxesList.ToArray();
                    RefreshList();
                    Application.RequestStop();
                }
                else
                {
                    MessageBox.ErrorQuery("Error", "Name and pattern are required", "OK");
                }
            };
            dialog.AddButton(okButton);

            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += () => Application.RequestStop();
            dialog.AddButton(cancelButton);

            Application.Run(dialog);
        }

        private void RemoveMailbox()
        {
            if (mailboxListView.SelectedItem >= 0)
            {
                if (serverOptions.Mailboxes != null && mailboxListView.SelectedItem < serverOptions.Mailboxes.Length)
                {
                    var mailbox = serverOptions.Mailboxes[mailboxListView.SelectedItem];
                    var result = MessageBox.Query("Remove Mailbox",
                        $"Remove mailbox '{mailbox.Name}'?",
                        "Yes", "No");

                    if (result == 0)
                    {
                        var mailboxesList = serverOptions.Mailboxes.ToList();
                        mailboxesList.RemoveAt(mailboxListView.SelectedItem);
                        serverOptions.Mailboxes = mailboxesList.ToArray();
                        RefreshList();
                    }
                }
            }
        }
    }
}
