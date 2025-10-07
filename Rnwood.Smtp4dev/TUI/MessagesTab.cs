using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Terminal.Gui;
using Rnwood.Smtp4dev.Data;
using Rnwood.Smtp4dev.DbModel;
using Rnwood.Smtp4dev.Server;

namespace Rnwood.Smtp4dev.TUI
{
    /// <summary>
    /// Messages tab with split view: message list on left, details on right
    /// </summary>
    public class MessagesTab
    {
        private readonly IHost host;
        private FrameView container;
        private ListView messageListView;
        private FrameView detailsPanel;
        private TextView detailsTextView;
        private TextView bodyTextView;
        private TextView headersTextView;
        private TextView rawTextView;
        private Label statusLabel;
        private TextField searchField;
        private List<Message> messages = new List<Message>();
        private List<Message> filteredMessages = new List<Message>();
        private Message selectedMessage;
        private string searchFilter = string.Empty;

        public MessagesTab(IHost host)
        {
            this.host = host;
            CreateUI();
        }

        private void CreateUI()
        {
            // Main container
            container = new FrameView("Messages")
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            // Status label and search field at top
            statusLabel = new Label("Loading messages...")
            {
                X = 1,
                Y = 0,
                Width = Dim.Percent(50)
            };
            
            var searchLabel = new Label("Search:")
            {
                X = Pos.Right(statusLabel) + 2,
                Y = 0
            };
            
            searchField = new TextField("")
            {
                X = Pos.Right(searchLabel) + 1,
                Y = 0,
                Width = Dim.Fill() - 2
            };
            searchField.TextChanged += (old) => ApplyFilter();
            
            container.Add(statusLabel, searchLabel, searchField);

            // Left panel - Message list (40% width)
            var listFrame = new FrameView("Message List")
            {
                X = 0,
                Y = 1,
                Width = Dim.Percent(40),
                Height = Dim.Fill() - 1
            };

            messageListView = new ListView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill() - 2,
                AllowsMarking = false,
                CanFocus = true
            };

            messageListView.SelectedItemChanged += OnMessageSelected;

            var refreshButton = new Button("Refresh (F5)")
            {
                X = 0,
                Y = Pos.Bottom(messageListView),
                Width = 15
            };
            refreshButton.Clicked += () => Refresh();

            var deleteButton = new Button("Delete")
            {
                X = Pos.Right(refreshButton) + 1,
                Y = Pos.Bottom(messageListView),
                Width = 10
            };
            deleteButton.Clicked += () => DeleteSelected();

            var deleteAllButton = new Button("Delete All")
            {
                X = Pos.Right(deleteButton) + 1,
                Y = Pos.Bottom(messageListView),
                Width = 12
            };
            deleteAllButton.Clicked += () => DeleteAll();
            
            var composeButton = new Button("Compose")
            {
                X = Pos.Right(deleteAllButton) + 1,
                Y = Pos.Bottom(messageListView),
                Width = 10
            };
            composeButton.Clicked += () => ComposeMessage();

            listFrame.Add(messageListView, refreshButton, deleteButton, deleteAllButton, composeButton);
            container.Add(listFrame);

            // Right panel - Message details (60% width)
            detailsPanel = new FrameView("Message Details")
            {
                X = Pos.Right(listFrame),
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill() - 1
            };

            var tabView = new TabView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            // Overview tab
            var overviewView = CreateOverviewView();
            tabView.AddTab(new TabView.Tab("Overview", overviewView), false);

            // Body tab
            var bodyView = CreateBodyView();
            tabView.AddTab(new TabView.Tab("Body", bodyView), false);

            // Headers tab
            var headersView = CreateHeadersView();
            tabView.AddTab(new TabView.Tab("Headers", headersView), false);

            // Raw tab
            var rawView = CreateRawView();
            tabView.AddTab(new TabView.Tab("Raw Source", rawView), false);

            detailsPanel.Add(tabView);
            container.Add(detailsPanel);

            // Load initial data
            Refresh();
        }

        private View CreateOverviewView()
        {
            detailsTextView = new TextView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                ReadOnly = true,
                WordWrap = true
            };
            return detailsTextView;
        }

        private View CreateBodyView()
        {
            bodyTextView = new TextView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                ReadOnly = true,
                WordWrap = true
            };
            return bodyTextView;
        }

        private View CreateHeadersView()
        {
            headersTextView = new TextView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                ReadOnly = true,
                WordWrap = false
            };
            return headersTextView;
        }

        private View CreateRawView()
        {
            rawTextView = new TextView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                ReadOnly = true,
                WordWrap = false
            };
            return rawTextView;
        }

        public View GetView()
        {
            return container;
        }

        public void Refresh()
        {
            var dbContext = host.Services.GetRequiredService<Smtp4devDbContext>();
            messages = dbContext.Messages
                .AsNoTracking()
                .OrderByDescending(m => m.ReceivedDate)
                .Take(100)
                .ToList();

            ApplyFilter();
        }
        
        private void ApplyFilter()
        {
            searchFilter = searchField?.Text?.ToString() ?? string.Empty;
            
            if (string.IsNullOrWhiteSpace(searchFilter))
            {
                filteredMessages = messages;
            }
            else
            {
                filteredMessages = messages.Where(m =>
                    (m.From != null && m.From.Contains(searchFilter, StringComparison.OrdinalIgnoreCase)) ||
                    (m.To != null && m.To.Contains(searchFilter, StringComparison.OrdinalIgnoreCase)) ||
                    (m.Subject != null && m.Subject.Contains(searchFilter, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            var messageStrings = filteredMessages.Select(m => 
                $"{m.ReceivedDate:yyyy-MM-dd HH:mm} | {TruncateString(m.From ?? "", 25)} | {TruncateString(m.Subject ?? "", 40)}"
            ).ToList();

            messageListView.SetSource(messageStrings);
            statusLabel.Text = $"Messages: {filteredMessages.Count}/{messages.Count}";
        }

        private void OnMessageSelected(ListViewItemEventArgs args)
        {
            if (args.Item >= 0 && args.Item < filteredMessages.Count)
            {
                selectedMessage = filteredMessages[args.Item];
                ShowMessageDetails();
            }
        }

        private void ShowMessageDetails()
        {
            if (selectedMessage == null) return;

            try
            {
                // Overview tab
                var details = $"From: {selectedMessage.From}\n" +
                             $"To: {selectedMessage.To}\n" +
                             $"Subject: {selectedMessage.Subject}\n" +
                             $"Date: {selectedMessage.ReceivedDate}\n" +
                             $"Size: {selectedMessage.Data?.Length ?? 0} bytes\n" +
                             $"Attachments: {selectedMessage.AttachmentCount}\n" +
                             $"Secure: {selectedMessage.SecureConnection}\n" +
                             $"Is Unread: {selectedMessage.IsUnread}\n";
                detailsTextView.Text = details;

                // Body tab - use BodyText property
                if (!string.IsNullOrEmpty(selectedMessage.BodyText))
                {
                    bodyTextView.Text = selectedMessage.BodyText;
                }
                else if (selectedMessage.Data != null)
                {
                    try
                    {
                        var messageText = System.Text.Encoding.UTF8.GetString(selectedMessage.Data);
                        // Try to extract body from raw message
                        var bodyStart = messageText.IndexOf("\r\n\r\n");
                        if (bodyStart > 0)
                        {
                            bodyTextView.Text = messageText.Substring(bodyStart + 4);
                        }
                        else
                        {
                            bodyTextView.Text = messageText;
                        }
                    }
                    catch
                    {
                        bodyTextView.Text = "[Unable to decode message body]";
                    }
                }
                else
                {
                    bodyTextView.Text = "[No body content]";
                }

                // Headers tab - parse from raw data
                var headersText = string.Empty;
                if (selectedMessage.MimeParseError != null)
                {
                    headersText = $"MIME Parse Error: {selectedMessage.MimeParseError}\n\n";
                }
                
                if (selectedMessage.Data != null)
                {
                    try
                    {
                        var messageText = System.Text.Encoding.UTF8.GetString(selectedMessage.Data);
                        var headerEnd = messageText.IndexOf("\r\n\r\n");
                        if (headerEnd > 0)
                        {
                            headersText += messageText.Substring(0, headerEnd);
                        }
                        else
                        {
                            headersText += "Unable to parse headers";
                        }
                    }
                    catch
                    {
                        headersText += "Error parsing headers";
                    }
                }
                headersTextView.Text = headersText;

                // Raw source tab
                if (selectedMessage.Data != null)
                {
                    rawTextView.Text = System.Text.Encoding.UTF8.GetString(selectedMessage.Data);
                }
                else
                {
                    rawTextView.Text = "[No raw data available]";
                }
            }
            catch (Exception ex)
            {
                detailsTextView.Text = $"Error loading message details: {ex.Message}";
            }
        }

        private void DeleteSelected()
        {
            if (selectedMessage != null)
            {
                var result = MessageBox.Query("Delete Message", 
                    $"Delete message '{selectedMessage.Subject}'?", 
                    "Yes", "No");
                
                if (result == 0) // Yes
                {
                    var messagesRepo = host.Services.GetRequiredService<IMessagesRepository>();
                    messagesRepo.DeleteMessage(selectedMessage.Id).Wait();
                    Refresh();
                }
            }
        }

        private void DeleteAll()
        {
            var result = MessageBox.Query("Delete All Messages", 
                "Delete ALL messages? This cannot be undone.", 
                "Yes", "No");
            
            if (result == 0) // Yes
            {
                var messagesRepo = host.Services.GetRequiredService<IMessagesRepository>();
                var dbContext = host.Services.GetRequiredService<Smtp4devDbContext>();
                var mailboxName = dbContext.Mailboxes.FirstOrDefault()?.Name ?? "Default";
                messagesRepo.DeleteAllMessages(mailboxName).Wait();
                Refresh();
            }
        }

        private void ComposeMessage()
        {
            var fromLabel = new Label("From:") { X = 1, Y = 1 };
            var fromField = new TextField("test@example.com")
            {
                X = 8,
                Y = 1,
                Width = Dim.Fill() - 2
            };
            
            var toLabel = new Label("To:") { X = 1, Y = 3 };
            var toField = new TextField("")
            {
                X = 8,
                Y = 3,
                Width = Dim.Fill() - 2
            };
            
            var subjectLabel = new Label("Subject:") { X = 1, Y = 5 };
            var subjectField = new TextField("")
            {
                X = 8,
                Y = 5,
                Width = Dim.Fill() - 2
            };
            
            var bodyLabel = new Label("Body:") { X = 1, Y = 7 };
            var bodyView = new TextView()
            {
                X = 8,
                Y = 7,
                Width = Dim.Fill() - 2,
                Height = Dim.Fill() - 10
            };

            var dialog = new Dialog("Compose Message", 80, 25);
            dialog.Add(fromLabel, fromField, toLabel, toField, subjectLabel, subjectField, bodyLabel, bodyView);

            var sendButton = new Button("Send");
            sendButton.Clicked += () => {
                try
                {
                    var server = host.Services.GetRequiredService<ISmtp4devServer>();
                    var headers = new Dictionary<string, string>();
                    
                    server.Send(
                        headers,
                        new[] { toField.Text.ToString() },
                        Array.Empty<string>(),
                        fromField.Text.ToString(),
                        new[] { toField.Text.ToString() },
                        subjectField.Text.ToString(),
                        bodyView.Text.ToString()
                    );
                    
                    Application.RequestStop();
                    MessageBox.Query("Success", "Message sent successfully!", "OK");
                    Refresh();
                }
                catch (Exception ex)
                {
                    MessageBox.ErrorQuery("Error", $"Failed to send message: {ex.Message}", "OK");
                }
            };

            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += () => Application.RequestStop();

            dialog.AddButton(sendButton);
            dialog.AddButton(cancelButton);

            Application.Run(dialog);
        }

        private string TruncateString(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength - 3) + "...";
        }
    }
}
