using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Terminal.Gui;
using Rnwood.Smtp4dev.Data;
using Rnwood.Smtp4dev.DbModel;

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
        private Label statusLabel;
        private List<Message> messages = new List<Message>();
        private Message selectedMessage;

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

            // Status label at top
            statusLabel = new Label("Loading messages...")
            {
                X = 1,
                Y = 0,
                Width = Dim.Fill() - 2
            };
            container.Add(statusLabel);

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

            listFrame.Add(messageListView, refreshButton, deleteButton, deleteAllButton);
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
            var bodyTextView = new TextView()
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
            var headersTextView = new TextView()
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
            var rawTextView = new TextView()
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

            var messageStrings = messages.Select(m => 
                $"{m.ReceivedDate:yyyy-MM-dd HH:mm} | {TruncateString(m.From ?? "", 25)} | {TruncateString(m.Subject ?? "", 40)}"
            ).ToList();

            messageListView.SetSource(messageStrings);
            statusLabel.Text = $"Messages: {messages.Count}";
        }

        private void OnMessageSelected(ListViewItemEventArgs args)
        {
            if (args.Item >= 0 && args.Item < messages.Count)
            {
                selectedMessage = messages[args.Item];
                ShowMessageDetails();
            }
        }

        private void ShowMessageDetails()
        {
            if (selectedMessage != null)
            {
                var details = $"From: {selectedMessage.From}\n" +
                             $"To: {selectedMessage.To}\n" +
                             $"Subject: {selectedMessage.Subject}\n" +
                             $"Date: {selectedMessage.ReceivedDate}\n" +
                             $"Size: {selectedMessage.Data?.Length ?? 0} bytes\n";

                detailsTextView.Text = details;
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

        private string TruncateString(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength - 3) + "...";
        }
    }
}
