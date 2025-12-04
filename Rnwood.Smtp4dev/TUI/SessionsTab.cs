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
    /// Sessions tab with split view: session list on left, log details on right
    /// </summary>
    public class SessionsTab
    {
        private readonly IHost host;
        private View container;
        private TableView sessionTableView;
        private TextView logTextView;
        private Label statusLabel;
        private TextField searchField;
        private CheckBox errorOnlyCheckbox;
        private List<Session> sessions = new List<Session>();
        private List<Session> filteredSessions = new List<Session>();
        private Session selectedSession;
        private string searchFilter = string.Empty;
        private bool showErrorsOnly = false;
        private int lastSelectedRow = -1;

        public SessionsTab(IHost host)
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

            // Status label at top
            statusLabel = new Label("Loading sessions...")
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill()
            };
            
            container.Add(statusLabel);

            // Left panel - Session table (40% width) - no frame
            sessionTableView = new TableView()
            {
                X = 0,
                Y = 1,
                Width = Dim.Percent(40),
                Height = Dim.Fill() - 5,
                FullRowSelect = true
            };

            // Setup table columns
            var table = new DataTable();
            table.Columns.Add("Err", typeof(string)); // Error indicator
            table.Columns.Add("Date", typeof(string));
            table.Columns.Add("Client", typeof(string));
            table.Columns.Add("Msgs", typeof(string));
            sessionTableView.Table = table;
            
            sessionTableView.SelectedCellChanged += OnSessionSelected;
            sessionTableView.KeyPress += (e) => {
                if (e.KeyEvent.Key == Key.DeleteChar || e.KeyEvent.Key == Key.Backspace)
                {
                    DeleteSelected();
                    e.Handled = true;
                }
            };

            container.Add(sessionTableView);

            // Search box and filter below the list
            var searchLabel = new Label("Search:")
            {
                X = 0,
                Y = Pos.Bottom(sessionTableView)
            };
            
            searchField = new TextField("")
            {
                X = Pos.Right(searchLabel) + 1,
                Y = Pos.Bottom(sessionTableView),
                Width = Dim.Percent(30)
            };
            searchField.TextChanged += (old) => ApplyFilter();
            
            errorOnlyCheckbox = new CheckBox("Errors Only")
            {
                X = Pos.Right(searchField) + 2,
                Y = Pos.Bottom(sessionTableView)
            };
            errorOnlyCheckbox.Toggled += (old) => {
                showErrorsOnly = errorOnlyCheckbox.Checked;
                ApplyFilter();
            };
            
            container.Add(searchLabel, searchField, errorOnlyCheckbox);

            // Action buttons below search
            var deleteButton = new Button("Delete")
            {
                X = 0,
                Y = Pos.Bottom(searchField),
                Width = 10
            };
            deleteButton.Clicked += () => DeleteSelected();

            var deleteAllButton = new Button("Delete All")
            {
                X = Pos.Right(deleteButton) + 1,
                Y = Pos.Bottom(searchField),
                Width = 12
            };
            deleteAllButton.Clicked += () => DeleteAll();

            container.Add(deleteButton, deleteAllButton);

            // Right panel - Session log (60% width) - no frame
            logTextView = new TextView()
            {
                X = Pos.Right(sessionTableView) + 1,
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill() - 1,
                ReadOnly = true,
                WordWrap = false
            };

            container.Add(logTextView);

            // Load initial data
            Refresh();
        }

        public View GetView()
        {
            return container;
        }

        public void Refresh()
        {
            // Save current selection
            if (sessionTableView.Table != null && sessionTableView.SelectedRow >= 0)
            {
                lastSelectedRow = sessionTableView.SelectedRow;
            }
            
            var dbContext = host.Services.GetRequiredService<Smtp4devDbContext>();
            sessions = dbContext.Sessions
                .AsNoTracking()
                .Where(s => s.EndDate.HasValue)
                .OrderByDescending(s => s.StartDate)
                .Take(100)
                .ToList();

            ApplyFilter();
        }
        
        private void ApplyFilter()
        {
            searchFilter = searchField?.Text?.ToString() ?? string.Empty;
            
            filteredSessions = sessions;
            
            // Apply error filter
            if (showErrorsOnly)
            {
                filteredSessions = filteredSessions.Where(s => !string.IsNullOrEmpty(s.SessionError)).ToList();
            }
            
            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchFilter))
            {
                filteredSessions = filteredSessions.Where(s =>
                    (s.ClientAddress != null && s.ClientAddress.Contains(searchFilter, StringComparison.OrdinalIgnoreCase)) ||
                    (s.ClientName != null && s.ClientName.Contains(searchFilter, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            var dbContext = host.Services.GetRequiredService<Smtp4devDbContext>();
            
            var table = new DataTable();
            table.Columns.Add("", typeof(string)); // Error indicator
            table.Columns.Add("Date", typeof(string));
            table.Columns.Add("Client", typeof(string));
            table.Columns.Add("Msgs", typeof(string));

            foreach (var session in filteredSessions)
            {
                var errorIndicator = !string.IsNullOrEmpty(session.SessionError) ? "[ERR]" : "";
                var messageCount = dbContext.Messages.Count(m => m.Session.Id == session.Id);
                table.Rows.Add(
                    errorIndicator,
                    session.StartDate.ToString("yyyy-MM-dd HH:mm"),
                    TruncateString(session.ClientAddress, 20),
                    messageCount.ToString()
                );
            }

            sessionTableView.Table = table;
            
            statusLabel.Text = $"Sessions: {filteredSessions.Count}/{sessions.Count}";
            
            // Restore selection if possible
            if (lastSelectedRow >= 0 && lastSelectedRow < filteredSessions.Count)
            {
                sessionTableView.SelectedRow = lastSelectedRow;
            }
        }

        private void OnSessionSelected(TableView.SelectedCellChangedEventArgs args)
        {
            if (args.NewRow >= 0 && args.NewRow < filteredSessions.Count)
            {
                lastSelectedRow = args.NewRow;
                selectedSession = filteredSessions[args.NewRow];
                ShowSessionLog();
            }
        }

        private void ShowSessionLog()
        {
            if (selectedSession != null)
            {
                var logText = $"Session Details:\n" +
                             $"Client: {selectedSession.ClientAddress}\n" +
                             $"Client Name: {selectedSession.ClientName}\n" +
                             $"Start: {selectedSession.StartDate}\n" +
                             $"End: {selectedSession.EndDate}\n" +
                             $"Duration: {(selectedSession.EndDate - selectedSession.StartDate).Value.TotalSeconds:F2}s\n";
                             
                if (!string.IsNullOrEmpty(selectedSession.SessionError))
                {
                    logText += $"\nERROR: {selectedSession.SessionError}\n";
                }
                
                var dbContext = host.Services.GetRequiredService<Smtp4devDbContext>();
                var messageCount = dbContext.Messages.Count(m => m.Session.Id == selectedSession.Id);
                logText += $"Messages: {messageCount}\n";
                logText += "\n" + new string('-', 60) + "\n";
                logText += "Session Log:\n";
                logText += new string('-', 60) + "\n\n";
                logText += selectedSession.Log ?? "No log data available.";
                
                logTextView.Text = logText;
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
