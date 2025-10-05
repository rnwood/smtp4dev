using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Rendering;
using Rnwood.Smtp4dev.Data;
using Rnwood.Smtp4dev.DbModel;

namespace Rnwood.Smtp4dev.TUI
{
    /// <summary>
    /// Provides split-screen views for enhanced navigation
    /// </summary>
    public class SplitScreenView
    {
        private readonly Smtp4devDbContext dbContext;
        private readonly AutoRefreshService autoRefreshService;
        private bool isRunning = true;
        private int selectedMessageIndex = 0;
        private List<MessageSummary> messages = new List<MessageSummary>();

        public SplitScreenView(Smtp4devDbContext dbContext)
        {
            this.dbContext = dbContext;
            this.autoRefreshService = new AutoRefreshService();
        }

        public void ShowMessageSplitView()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            
            // Start auto-refresh
            autoRefreshService.Start(() => {
                RefreshMessages();
            }, 3);

            try
            {
                AnsiConsole.Live(CreateLayout())
                    .AutoClear(true)
                    .Overflow(VerticalOverflow.Ellipsis)
                    .Start(ctx =>
                    {
                        while (isRunning)
                        {
                            var layout = CreateMessageSplitLayout();
                            ctx.UpdateTarget(layout);
                            
                            // Check for keyboard input (non-blocking)
                            if (Console.KeyAvailable)
                            {
                                var key = Console.ReadKey(true);
                                HandleKeyPress(key);
                            }
                            
                            Thread.Sleep(100); // Small delay to prevent CPU spinning
                        }
                    });
            }
            finally
            {
                autoRefreshService.Stop();
            }
        }

        private void RefreshMessages()
        {
            try
            {
                messages = dbContext.Messages
                    .OrderByDescending(m => m.ReceivedDate)
                    .Take(50)
                    .Select(m => new MessageSummary
                    {
                        Id = m.Id,
                        From = m.From,
                        To = m.To,
                        Subject = m.Subject,
                        ReceivedDate = m.ReceivedDate
                    })
                    .ToList();
            }
            catch
            {
                // Silently handle errors
            }
        }

        private void HandleKeyPress(ConsoleKeyInfo key)
        {
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    if (selectedMessageIndex > 0)
                        selectedMessageIndex--;
                    break;
                case ConsoleKey.DownArrow:
                    if (selectedMessageIndex < messages.Count - 1)
                        selectedMessageIndex++;
                    break;
                case ConsoleKey.Escape:
                case ConsoleKey.Q:
                    isRunning = false;
                    break;
            }
        }

        private Layout CreateMessageSplitLayout()
        {
            var layout = new Layout("Root")
                .SplitColumns(
                    new Layout("Left"),
                    new Layout("Right"));

            layout["Left"].Size(40);

            // Left panel: Message list
            var table = new Table();
            table.Border = TableBorder.Rounded;
            table.AddColumn("Time");
            table.AddColumn("From");
            table.AddColumn("Subject");

            for (int i = 0; i < Math.Min(messages.Count, 20); i++)
            {
                var msg = messages[i];
                var style = i == selectedMessageIndex ? "[bold yellow on blue]" : "";
                table.AddRow(
                    $"{style}{msg.ReceivedDate:HH:mm:ss}[/]",
                    $"{style}{msg.From?.EscapeMarkup() ?? ""}[/]",
                    $"{style}{(msg.Subject?.Length > 20 ? msg.Subject.Substring(0, 20) + "..." : msg.Subject)?.EscapeMarkup() ?? ""}[/]"
                );
            }

            layout["Left"].Update(
                new Panel(table)
                    .Header("[blue]Messages[/]")
                    .Border(BoxBorder.Double));

            // Right panel: Message preview
            if (messages.Any() && selectedMessageIndex < messages.Count)
            {
                var selectedMessage = messages[selectedMessageIndex];
                var fullMessage = dbContext.Messages.FirstOrDefault(m => m.Id == selectedMessage.Id);
                
                if (fullMessage != null)
                {
                    var grid = new Grid();
                    grid.AddColumn();
                    grid.AddRow($"[bold]From:[/] {fullMessage.From?.EscapeMarkup() ?? ""}");
                    grid.AddRow($"[bold]To:[/] {fullMessage.To?.EscapeMarkup() ?? ""}");
                    grid.AddRow($"[bold]Subject:[/] {fullMessage.Subject?.EscapeMarkup() ?? ""}");
                    grid.AddRow($"[bold]Date:[/] {fullMessage.ReceivedDate:yyyy-MM-dd HH:mm:ss}");
                    grid.AddRow("");
                    grid.AddRow($"[bold]Body:[/]");
                    
                    var body = fullMessage.BodyText ?? "";
                    if (string.IsNullOrEmpty(body) && fullMessage.Data != null)
                    {
                        body = System.Text.Encoding.UTF8.GetString(fullMessage.Data);
                    }
                    
                    // Limit body preview
                    if (body.Length > 500)
                        body = body.Substring(0, 500) + "...";
                    
                    grid.AddRow(body.EscapeMarkup());

                    layout["Right"].Update(
                        new Panel(grid)
                            .Header("[blue]Preview[/]")
                            .Border(BoxBorder.Double));
                }
            }
            else
            {
                layout["Right"].Update(
                    new Panel("[dim]No message selected[/]")
                        .Header("[blue]Preview[/]")
                        .Border(BoxBorder.Double));
            }

            return layout;
        }

        private Layout CreateLayout()
        {
            return new Layout("Root")
                .SplitColumns(
                    new Layout("Left"),
                    new Layout("Right"));
        }

        public class MessageSummary
        {
            public Guid Id { get; set; }
            public string From { get; set; }
            public string To { get; set; }
            public string Subject { get; set; }
            public DateTime ReceivedDate { get; set; }
        }
    }
}
