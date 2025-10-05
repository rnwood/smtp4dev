using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Terminal.Gui;
using Rnwood.Smtp4dev.Data;
using Rnwood.Smtp4dev.DbModel;

namespace Rnwood.Smtp4dev.TUI
{
    public class MessageListView : View
    {
        private readonly Smtp4devDbContext dbContext;
        private ListView listView;
        private Label countLabel;
        private Window detailWindow;

        public MessageListView(Smtp4devDbContext dbContext)
        {
            this.dbContext = dbContext;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // Create toolbar
            countLabel = new Label("Messages: 0")
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = 1
            };

            var deleteButton = new Button("Delete All")
            {
                X = Pos.Right(countLabel) - 15,
                Y = 0
            };
            deleteButton.Clicked += OnDeleteAll;

            var refreshButton = new Button("Refresh")
            {
                X = Pos.Left(deleteButton) - 12,
                Y = 0
            };
            refreshButton.Clicked += () => Refresh();

            // Create list view
            listView = new ListView()
            {
                X = 0,
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                AllowsMarking = false
            };

            listView.OpenSelectedItem += OnOpenSelectedItem;

            Add(countLabel, refreshButton, deleteButton, listView);

            // Load initial data
            Refresh();
        }

        public void Refresh()
        {
            try
            {
                var messages = dbContext.Messages
                    .AsNoTracking()
                    .OrderByDescending(m => m.ReceivedDate)
                    .Take(100)
                    .Select(m => new
                    {
                        m.Id,
                        m.From,
                        m.To,
                        m.Subject,
                        ReceivedDate = m.ReceivedDate.ToString("yyyy-MM-dd HH:mm:ss")
                    })
                    .ToList();

                var displayItems = messages
                    .Select(m => $"{m.ReceivedDate} | From: {m.From} | To: {m.To} | {m.Subject}")
                    .ToList();

                listView.SetSource(displayItems);
                countLabel.Text = $"Messages: {messages.Count}";
            }
            catch (Exception ex)
            {
                MessageBox.ErrorQuery("Error", $"Failed to load messages: {ex.Message}", "OK");
            }
        }

        private void OnOpenSelectedItem(ListViewItemEventArgs args)
        {
            try
            {
                var messages = dbContext.Messages
                    .AsNoTracking()
                    .OrderByDescending(m => m.ReceivedDate)
                    .Take(100)
                    .ToList();

                if (args.Item >= 0 && args.Item < messages.Count)
                {
                    var message = messages[args.Item];
                    ShowMessageDetail(message);
                }
            }
            catch (Exception ex)
            {
                MessageBox.ErrorQuery("Error", $"Failed to load message details: {ex.Message}", "OK");
            }
        }

        private void ShowMessageDetail(Message message)
        {
            if (detailWindow != null)
            {
                Application.Top.Remove(detailWindow);
                detailWindow.Dispose();
            }

            detailWindow = new Window("Message Details")
            {
                X = 2,
                Y = 2,
                Width = Dim.Fill() - 4,
                Height = Dim.Fill() - 4,
                Modal = true
            };

            var textView = new TextView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill() - 1,
                ReadOnly = true,
                WordWrap = true
            };

            var content = $"From: {message.From}\n";
            content += $"To: {message.To}\n";
            content += $"Subject: {message.Subject}\n";
            content += $"Received: {message.ReceivedDate:yyyy-MM-dd HH:mm:ss}\n";
            content += $"\n--- Headers ---\n";
            content += message.MimeMetadata ?? "";
            content += $"\n\n--- Body ---\n";
            
            if (!string.IsNullOrEmpty(message.BodyText))
            {
                content += message.BodyText;
            }
            else if (message.Data != null && message.Data.Length > 0)
            {
                content += System.Text.Encoding.UTF8.GetString(message.Data);
            }
            else
            {
                content += "(No content)";
            }

            textView.Text = content;

            var closeButton = new Button("Close")
            {
                X = Pos.Center(),
                Y = Pos.Bottom(textView)
            };
            closeButton.Clicked += () =>
            {
                Application.Top.Remove(detailWindow);
                detailWindow.Dispose();
                detailWindow = null;
            };

            detailWindow.Add(textView, closeButton);
            Application.Top.Add(detailWindow);
            detailWindow.SetFocus();
        }

        private void OnDeleteAll()
        {
            var result = MessageBox.Query("Confirm", "Delete all messages?", "Yes", "No");
            if (result == 0)
            {
                try
                {
                    var messages = dbContext.Messages.ToList();
                    dbContext.Messages.RemoveRange(messages);
                    dbContext.SaveChanges();
                    Refresh();
                    MessageBox.Query("Success", "All messages deleted", "OK");
                }
                catch (Exception ex)
                {
                    MessageBox.ErrorQuery("Error", $"Failed to delete messages: {ex.Message}", "OK");
                }
            }
        }
    }
}
