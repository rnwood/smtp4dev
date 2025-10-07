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
        private View container;
        private ListView sessionListView;
        private TextView logTextView;
        private Label statusLabel;
        private TextField searchField;
        private CheckBox errorOnlyCheckbox;
        private List<Session> sessions = new List<Session>();
        private List<Session> filteredSessions = new List<Session>();
        private Session selectedSession;
        private string searchFilter = string.Empty;
        private bool showErrorsOnly = false;
        private int lastSelectedIndex = -1;

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

            // Status label, search field, and filter checkbox at top
            statusLabel = new Label("Loading sessions...")
            {
                X = 0,
                Y = 0,
                Width = Dim.Percent(30)
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
                Width = Dim.Percent(30)
            };
            searchField.TextChanged += (old) => ApplyFilter();
            
            errorOnlyCheckbox = new CheckBox("Errors Only")
            {
                X = Pos.Right(searchField) + 2,
                Y = 0
            };
            errorOnlyCheckbox.Toggled += (old) => {
                showErrorsOnly = errorOnlyCheckbox.Checked;
                ApplyFilter();
            };
            
            container.Add(statusLabel, searchLabel, searchField, errorOnlyCheckbox);

            // Left panel - Session list (40% width) - no frame
            sessionListView = new ListView()
            {
                X = 0,
                Y = 1,
                Width = Dim.Percent(40),
                Height = Dim.Fill() - 3,
                AllowsMarking = false,
                CanFocus = true
            };

            sessionListView.SelectedItemChanged += OnSessionSelected;

            var deleteButton = new Button("Delete")
            {
                X = 0,
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

            container.Add(sessionListView, deleteButton, deleteAllButton);

            // Right panel - Session log (60% width) - no frame
            logTextView = new TextView()
            {
                X = Pos.Right(sessionListView) + 1,
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
            lastSelectedIndex = sessionListView.SelectedItem;
            
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
            var sessionStrings = filteredSessions.Select(s =>
            {
                var errorIndicator = !string.IsNullOrEmpty(s.SessionError) ? "[ERR] " : "      ";
                var messageCount = dbContext.Messages.Count(m => m.Session.Id == s.Id);
                return $"{errorIndicator}{s.StartDate:yyyy-MM-dd HH:mm} | {TruncateString(s.ClientAddress, 20)} | Msgs: {messageCount}";
            }).ToList();

            sessionListView.SetSource(sessionStrings);
            statusLabel.Text = $"Sessions: {filteredSessions.Count}/{sessions.Count}";
            
            // Restore selection if possible
            if (lastSelectedIndex >= 0 && lastSelectedIndex < filteredSessions.Count)
            {
                sessionListView.SelectedItem = lastSelectedIndex;
            }
        }

        private void OnSessionSelected(ListViewItemEventArgs args)
        {
            if (args.Item >= 0 && args.Item < filteredSessions.Count)
            {
                lastSelectedIndex = args.Item;
                selectedSession = filteredSessions[args.Item];
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
