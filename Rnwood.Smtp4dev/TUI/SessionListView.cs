using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Terminal.Gui;
using Rnwood.Smtp4dev.Data;
using Rnwood.Smtp4dev.DbModel;

namespace Rnwood.Smtp4dev.TUI
{
    public class SessionListView : View
    {
        private readonly Smtp4devDbContext dbContext;
        private ListView listView;
        private Label countLabel;
        private Window detailWindow;

        public SessionListView(Smtp4devDbContext dbContext)
        {
            this.dbContext = dbContext;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // Create toolbar
            countLabel = new Label("Sessions: 0")
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
                var sessions = dbContext.Sessions
                    .AsNoTracking()
                    .Where(s => s.EndDate.HasValue)
                    .OrderByDescending(s => s.StartDate)
                    .Take(100)
                    .Select(s => new
                    {
                        s.Id,
                        s.ClientAddress,
                        StartDate = s.StartDate.ToString("yyyy-MM-dd HH:mm:ss"),
                        EndDate = s.EndDate.HasValue ? s.EndDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : "N/A",
                        s.NumberOfMessages,
                        HasError = !string.IsNullOrEmpty(s.SessionError)
                    })
                    .ToList();

                var displayItems = sessions
                    .Select(s => $"{s.StartDate} | {s.ClientAddress} | Messages: {s.NumberOfMessages}{(s.HasError ? " [ERROR]" : "")}")
                    .ToList();

                listView.SetSource(displayItems);
                countLabel.Text = $"Sessions: {sessions.Count}";
            }
            catch (Exception ex)
            {
                MessageBox.ErrorQuery("Error", $"Failed to load sessions: {ex.Message}", "OK");
            }
        }

        private void OnOpenSelectedItem(ListViewItemEventArgs args)
        {
            try
            {
                var sessions = dbContext.Sessions
                    .AsNoTracking()
                    .Where(s => s.EndDate.HasValue)
                    .OrderByDescending(s => s.StartDate)
                    .Take(100)
                    .ToList();

                if (args.Item >= 0 && args.Item < sessions.Count)
                {
                    var session = sessions[args.Item];
                    ShowSessionDetail(session);
                }
            }
            catch (Exception ex)
            {
                MessageBox.ErrorQuery("Error", $"Failed to load session details: {ex.Message}", "OK");
            }
        }

        private void ShowSessionDetail(Session session)
        {
            if (detailWindow != null)
            {
                Application.Top.Remove(detailWindow);
                detailWindow.Dispose();
            }

            detailWindow = new Window("Session Details")
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

            var content = $"Client Address: {session.ClientAddress}\n";
            content += $"Start Date: {session.StartDate:yyyy-MM-dd HH:mm:ss}\n";
            content += $"End Date: {(session.EndDate.HasValue ? session.EndDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : "N/A")}\n";
            content += $"Number of Messages: {session.NumberOfMessages}\n";
            
            if (!string.IsNullOrEmpty(session.SessionError))
            {
                content += $"\n--- ERROR ---\n{session.SessionError}\n";
            }

            content += $"\n--- Log ---\n";
            content += session.Log ?? "(No log available)";

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
            var result = MessageBox.Query("Confirm", "Delete all sessions?", "Yes", "No");
            if (result == 0)
            {
                try
                {
                    var sessions = dbContext.Sessions.ToList();
                    dbContext.Sessions.RemoveRange(sessions);
                    dbContext.SaveChanges();
                    Refresh();
                    MessageBox.Query("Success", "All sessions deleted", "OK");
                }
                catch (Exception ex)
                {
                    MessageBox.ErrorQuery("Error", $"Failed to delete sessions: {ex.Message}", "OK");
                }
            }
        }
    }
}
