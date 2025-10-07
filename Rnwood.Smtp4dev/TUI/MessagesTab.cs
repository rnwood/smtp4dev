using System;
using System.Collections.Generic;
using System.Data;
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
        private View container;
        private ListView messageListView;
        private View detailsPanel;
        private TextView overviewBodyTextView;
        private TableView headersTableView;
        private TextView rawTextView;
        private ListView partsListView;
        private ListView attachmentsListView;
        private Label statusLabel;
        private TextField searchField;
        private List<Message> messages = new List<Message>();
        private List<Message> filteredMessages = new List<Message>();
        private Message selectedMessage;
        private string searchFilter = string.Empty;
        private int lastSelectedIndex = -1;

        public MessagesTab(IHost host)
        {
            this.host = host;
            CreateUI();
        }

        private void CreateUI()
        {
            // Main container (no frame)
            container = new View()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            // Status label and search field at top
            statusLabel = new Label("Loading messages...")
            {
                X = 0,
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
                Width = Dim.Fill() - 1
            };
            searchField.TextChanged += (old) => ApplyFilter();
            
            container.Add(statusLabel, searchLabel, searchField);

            // Left panel - Message list (40% width) - no frame
            messageListView = new ListView()
            {
                X = 0,
                Y = 1,
                Width = Dim.Percent(40),
                Height = Dim.Fill() - 3,
                AllowsMarking = false,
                CanFocus = true
            };

            messageListView.SelectedItemChanged += OnMessageSelected;

            var deleteButton = new Button("Delete")
            {
                X = 0,
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

            container.Add(messageListView, deleteButton, deleteAllButton, composeButton);

            // Right panel - Message details (60% width) - no frame
            detailsPanel = new View()
            {
                X = Pos.Right(messageListView) + 1,
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

            // Overview/Body combined tab
            var overviewBodyView = CreateOverviewBodyView();
            tabView.AddTab(new TabView.Tab("Message", overviewBodyView), false);

            // Headers tab (table view)
            var headersView = CreateHeadersTableView();
            tabView.AddTab(new TabView.Tab("Headers", headersView), false);

            // Parts tab
            var partsView = CreatePartsView();
            tabView.AddTab(new TabView.Tab("Parts", partsView), false);

            // Attachments tab
            var attachmentsView = CreateAttachmentsView();
            tabView.AddTab(new TabView.Tab("Attachments", attachmentsView), false);

            // Raw tab
            var rawView = CreateRawView();
            tabView.AddTab(new TabView.Tab("Raw Source", rawView), false);

            detailsPanel.Add(tabView);
            container.Add(detailsPanel);

            // Load initial data
            Refresh();
        }

        private View CreateOverviewBodyView()
        {
            overviewBodyTextView = new TextView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                ReadOnly = true,
                WordWrap = true
            };
            return overviewBodyTextView;
        }

        private View CreateHeadersTableView()
        {
            headersTableView = new TableView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            
            var table = new DataTable();
            table.Columns.Add("Header");
            table.Columns.Add("Value");
            headersTableView.Table = table;
            
            return headersTableView;
        }

        private View CreatePartsView()
        {
            partsListView = new ListView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            return partsListView;
        }

        private View CreateAttachmentsView()
        {
            var view = new View()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            attachmentsListView = new ListView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill() - 2
            };
            
            var saveButton = new Button("Save Selected")
            {
                X = 0,
                Y = Pos.Bottom(attachmentsListView)
            };
            saveButton.Clicked += () => SaveAttachment();
            
            view.Add(attachmentsListView, saveButton);
            return view;
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
            // Save current selection
            lastSelectedIndex = messageListView.SelectedItem;
            
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
            {
                var unreadIndicator = m.IsUnread ? "* " : "  ";
                return $"{unreadIndicator}{m.ReceivedDate:yyyy-MM-dd HH:mm} | {TruncateString(m.From ?? "", 20)} | {TruncateString(m.Subject ?? "", 35)}";
            }).ToList();

            messageListView.SetSource(messageStrings);
            statusLabel.Text = $"Messages: {filteredMessages.Count}/{messages.Count}";
            
            // Restore selection if possible
            if (lastSelectedIndex >= 0 && lastSelectedIndex < filteredMessages.Count)
            {
                messageListView.SelectedItem = lastSelectedIndex;
            }
        }

        private void OnMessageSelected(ListViewItemEventArgs args)
        {
            if (args.Item >= 0 && args.Item < filteredMessages.Count)
            {
                lastSelectedIndex = args.Item;
                selectedMessage = filteredMessages[args.Item];
                ShowMessageDetails();
            }
        }

        private void ShowMessageDetails()
        {
            if (selectedMessage == null) return;

            try
            {
                // Combined Overview and Body (compact overview)
                var details = $"From: {selectedMessage.From}  |  To: {selectedMessage.To}  |  Date: {selectedMessage.ReceivedDate:yyyy-MM-dd HH:mm}\n" +
                             $"Subject: {selectedMessage.Subject}\n" +
                             $"Size: {selectedMessage.Data?.Length ?? 0}b | Attachments: {selectedMessage.AttachmentCount} | Unread: {(selectedMessage.IsUnread ? "Yes" : "No")}\n" +
                             $"{new string('-', 80)}\n\n";
                
                // Append body
                if (!string.IsNullOrEmpty(selectedMessage.BodyText))
                {
                    details += selectedMessage.BodyText;
                }
                else if (selectedMessage.Data != null)
                {
                    try
                    {
                        var messageText = System.Text.Encoding.UTF8.GetString(selectedMessage.Data);
                        var bodyStart = messageText.IndexOf("\r\n\r\n");
                        if (bodyStart > 0)
                        {
                            details += messageText.Substring(bodyStart + 4);
                        }
                        else
                        {
                            details += messageText;
                        }
                    }
                    catch
                    {
                        details += "[Unable to decode message body]";
                    }
                }
                else
                {
                    details += "[No body content]";
                }
                
                overviewBodyTextView.Text = details;

                // Headers tab - use table view
                var table = new DataTable();
                table.Columns.Add("Header");
                table.Columns.Add("Value");
                
                if (selectedMessage.Data != null)
                {
                    try
                    {
                        var messageText = System.Text.Encoding.UTF8.GetString(selectedMessage.Data);
                        var headerEnd = messageText.IndexOf("\r\n\r\n");
                        if (headerEnd > 0)
                        {
                            var headersText = messageText.Substring(0, headerEnd);
                            var headerLines = headersText.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                            
                            foreach (var line in headerLines)
                            {
                                var colonIndex = line.IndexOf(':');
                                if (colonIndex > 0)
                                {
                                    var headerName = line.Substring(0, colonIndex).Trim();
                                    var headerValue = line.Substring(colonIndex + 1).Trim();
                                    table.Rows.Add(headerName, headerValue);
                                }
                            }
                        }
                    }
                    catch
                    {
                        table.Rows.Add("Error", "Failed to parse headers");
                    }
                }
                headersTableView.Table = table;

                // Parts list
                var partsList = new List<string>();
                if (selectedMessage.MimeParseError != null)
                {
                    partsList.Add($"MIME Parse Error: {selectedMessage.MimeParseError}");
                }
                // Add basic parts info from content type if available
                if (selectedMessage.Data != null)
                {
                    try
                    {
                        var messageText = System.Text.Encoding.UTF8.GetString(selectedMessage.Data);
                        if (messageText.Contains("Content-Type:"))
                        {
                            partsList.Add("Message structure (from headers):");
                            var lines = messageText.Split('\n');
                            foreach (var line in lines.Take(50))
                            {
                                if (line.Contains("Content-Type:") || line.Contains("Content-Transfer-Encoding:"))
                                {
                                    partsList.Add("  " + line.Trim());
                                }
                            }
                        }
                    }
                    catch { }
                }
                if (partsList.Count == 0)
                {
                    partsList.Add("No MIME parts information available");
                }
                partsListView.SetSource(partsList);

                // Attachments list
                var attachmentsList = new List<string>();
                if (selectedMessage.AttachmentCount > 0)
                {
                    // Try to extract attachment info from message
                    if (selectedMessage.Data != null)
                    {
                        try
                        {
                            var messageText = System.Text.Encoding.UTF8.GetString(selectedMessage.Data);
                            var lines = messageText.Split('\n');
                            for (int i = 0; i < lines.Length; i++)
                            {
                                if (lines[i].Contains("Content-Disposition:") && lines[i].Contains("attachment"))
                                {
                                    // Look for filename
                                    var filenameLine = lines[i];
                                    var filenameIndex = filenameLine.IndexOf("filename=");
                                    if (filenameIndex >= 0)
                                    {
                                        var filename = filenameLine.Substring(filenameIndex + 9).Trim().Trim('"', ';');
                                        attachmentsList.Add(filename);
                                    }
                                    else
                                    {
                                        attachmentsList.Add($"Attachment {attachmentsList.Count + 1}");
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                    
                    if (attachmentsList.Count == 0)
                    {
                        for (int i = 0; i < selectedMessage.AttachmentCount; i++)
                        {
                            attachmentsList.Add($"Attachment {i + 1}");
                        }
                    }
                }
                else
                {
                    attachmentsList.Add("No attachments");
                }
                attachmentsListView.SetSource(attachmentsList);

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
                overviewBodyTextView.Text = $"Error loading message details: {ex.Message}";
            }
        }

        private void SaveAttachment()
        {
            if (selectedMessage == null || selectedMessage.AttachmentCount == 0) return;
            
            MessageBox.Query("Save Attachment", 
                "Attachment saving functionality requires access to the message repository.\n\n" +
                "Currently only viewing is supported in TUI mode.\n" +
                "Please use the web UI to save attachments.", 
                "OK");
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
