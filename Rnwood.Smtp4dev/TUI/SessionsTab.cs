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
    /// Sessions tab with split view: session list on left, log details on right
    /// </summary>
    public class SessionsTab
    {
        private readonly IHost host;
        private FrameView container;
        private ListView sessionListView;
        private TextView logTextView;
        private Label statusLabel;
        private List<Session> sessions = new List<Session>();
        private Session selectedSession;

        public SessionsTab(IHost host)
        {
            this.host = host;
            CreateUI();
        }

        private void CreateUI()
        {
            // Main container
            container = new FrameView("Sessions")
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            // Status label at top
            statusLabel = new Label("Loading sessions...")
            {
                X = 1,
                Y = 0,
                Width = Dim.Fill() - 2
            };
            container.Add(statusLabel);

            // Left panel - Session list (40% width)
            var listFrame = new FrameView("Session List")
            {
                X = 0,
                Y = 1,
                Width = Dim.Percent(40),
                Height = Dim.Fill() - 1
            };

            sessionListView = new ListView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill() - 2,
                AllowsMarking = false,
                CanFocus = true
            };

            sessionListView.SelectedItemChanged += OnSessionSelected;

            var refreshButton = new Button("Refresh (F5)")
            {
                X = 0,
                Y = Pos.Bottom(sessionListView),
                Width = 15
            };
            refreshButton.Clicked += () => Refresh();

            var deleteButton = new Button("Delete")
            {
                X = Pos.Right(refreshButton) + 1,
                Y = Pos.Bottom(sessionListView),
                Width = 10
            };
            deleteButton.Clicked += () => DeleteSelected();

            var deleteAllButton = new Button("Delete All")
            {
                X = Pos.Right(deleteButton) + 1,
                Y = Pos.Bottom(sessionListView),
                Width = 12
            };
            deleteAllButton.Clicked += () => DeleteAll();

            listFrame.Add(sessionListView, refreshButton, deleteButton, deleteAllButton);
            container.Add(listFrame);

            // Right panel - Session log (60% width)
            var logFrame = new FrameView("Session Log")
            {
                X = Pos.Right(listFrame),
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill() - 1
            };

            logTextView = new TextView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                ReadOnly = true,
                WordWrap = false
            };

            logFrame.Add(logTextView);
            container.Add(logFrame);

            // Load initial data
            Refresh();
        }

        public View GetView()
        {
            return container;
        }

        public void Refresh()
        {
            var dbContext = host.Services.GetRequiredService<Smtp4devDbContext>();
            sessions = dbContext.Sessions
                .AsNoTracking()
                .Where(s => s.EndDate.HasValue)
                .OrderByDescending(s => s.StartDate)
                .Take(100)
                .ToList();

            var sessionStrings = sessions.Select(s =>
            {
                var status = string.IsNullOrEmpty(s.Error) ? "OK" : "ERROR";
                var messageCount = dbContext.Messages.Count(m => m.Session.Id == s.Id);
                return $"{s.StartDate:yyyy-MM-dd HH:mm} | {TruncateString(s.ClientAddress, 20)} | {status} | Msgs: {messageCount}";
            }).ToList();

            sessionListView.SetSource(sessionStrings);
            statusLabel.Text = $"Sessions: {sessions.Count}";
        }

        private void OnSessionSelected(ListViewItemEventArgs args)
        {
            if (args.Item >= 0 && args.Item < sessions.Count)
            {
                selectedSession = sessions[args.Item];
                ShowSessionLog();
            }
        }

        private void ShowSessionLog()
        {
            if (selectedSession != null)
            {
                var log = selectedSession.Log ?? "No log data available.";
                logTextView.Text = log;
            }
        }

        private void DeleteSelected()
        {
            if (selectedSession != null)
            {
                var result = MessageBox.Query("Delete Session",
                    $"Delete session from {selectedSession.ClientAddress}?",
                    "Yes", "No");

                if (result == 0) // Yes
                {
                    var server = host.Services.GetRequiredService<ISmtp4devServer>();
                    server.DeleteSession(selectedSession.Id).Wait();
                    Refresh();
                }
            }
        }

        private void DeleteAll()
        {
            var result = MessageBox.Query("Delete All Sessions",
                "Delete ALL sessions? This cannot be undone.",
                "Yes", "No");

            if (result == 0) // Yes
            {
                var server = host.Services.GetRequiredService<ISmtp4devServer>();
                server.DeleteAllSessions().Wait();
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
