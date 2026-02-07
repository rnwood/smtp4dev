using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using System.Net.Security;

namespace Rnwood.Smtp4dev.Tests.E2E.WebUI.PageModel
{
    public class HomePage
    {
        private readonly IPage page;

        public HomePage(IPage page)
        {
            this.page = page;
        }

        #region Navigation and Routing Methods

        /// <summary>
        /// Navigate to a specific tab by name
        /// </summary>
        public async Task NavigateToTabAsync(string tabName)
        {
            var tab = page.Locator("#maintabs .el-tabs__item").Filter(new() { HasText = tabName });
            await tab.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        /// <summary>
        /// Navigate to Messages tab
        /// </summary>
        public async Task NavigateToMessagesAsync()
        {
            await NavigateToTabAsync("Messages");
        }

        /// <summary>
        /// Navigate to Sessions tab
        /// </summary>
        public async Task NavigateToSessionsAsync()
        {
            await NavigateToTabAsync("Sessions");
        }

        /// <summary>
        /// Navigate to Server Log tab
        /// </summary>
        public async Task NavigateToServerLogAsync()
        {
            await NavigateToTabAsync("Server Log");
        }

        /// <summary>
        /// Get the name of the currently active tab
        /// </summary>
        public async Task<string> GetActiveTabNameAsync()
        {
            var activeTab = await page.Locator("#maintabs .el-tabs__item.is-active").First.TextContentAsync();
            return activeTab ?? "";
        }

        /// <summary>
        /// Navigate to a specific URL
        /// </summary>
        public async Task NavigateToUrlAsync(string url)
        {
            await page.GotoAsync(url);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        /// <summary>
        /// Wait for the main tabs to be visible
        /// </summary>
        public async Task WaitForTabsAsync()
        {
            await page.WaitForSelectorAsync("#maintabs");
        }

        /// <summary>
        /// Get the current page URL
        /// </summary>
        public string GetCurrentUrl()
        {
            return page.Url;
        }

        /// <summary>
        /// Navigate back using browser back button
        /// </summary>
        public async Task GoBackAsync()
        {
            await page.GoBackAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        /// <summary>
        /// Navigate forward using browser forward button
        /// </summary>
        public async Task GoForwardAsync()
        {
            await page.GoForwardAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        #endregion

        #region Test Email Helpers

        /// <summary>
        /// Send a test email via SMTP to create a message/session
        /// </summary>
        public static async Task SendTestEmailAsync(
            int smtpPort, 
            string subject = null, 
            string from = "test@example.com", 
            string to = "user@example.com",
            string body = "Test email body",
            RemoteCertificateValidationCallback certValidationCallback = null)
        {
            subject ??= Guid.NewGuid().ToString();
            
            using var smtpClient = new SmtpClient();
            smtpClient.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            smtpClient.ServerCertificateValidationCallback = certValidationCallback ?? ((s, c, h, e) => true);
            smtpClient.CheckCertificateRevocation = false;
            
            var message = new MimeMessage();
            message.To.Add(MailboxAddress.Parse(to));
            message.From.Add(MailboxAddress.Parse(from));
            message.Subject = subject;
            message.Body = new TextPart() { Text = body };

            await smtpClient.ConnectAsync("localhost", smtpPort, SecureSocketOptions.StartTls,
                new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
            await smtpClient.SendAsync(message);
            await smtpClient.DisconnectAsync(true, new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
        }

        #endregion

        #region Message and Session Selection

        /// <summary>
        /// Wait for a message row with specific text to appear
        /// </summary>
        public async Task<Grid.GridRow> WaitForMessageRowAsync(string text, int timeoutSeconds = 30)
        {
            var messageList = await GetMessageListAsync();
            var grid = messageList.GetGrid();
            
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            Grid.GridRow foundRow = null;
            
            while (foundRow == null && !timeout.Token.IsCancellationRequested)
            {
                try
                {
                    var rows = await grid.GetRowsAsync();
                    foreach (var row in rows)
                    {
                        if (await row.ContainsTextAsync(text))
                        {
                            foundRow = row;
                            break;
                        }
                    }
                }
                catch
                {
                    // Keep trying
                }
                
                if (foundRow == null)
                {
                    try
                    {
                        await Task.Delay(100, timeout.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
            
            return foundRow;
        }

        /// <summary>
        /// Wait for any session row to appear
        /// </summary>
        public async Task<ILocator> WaitForSessionRowAsync(int timeoutSeconds = 30)
        {
            await page.WaitForSelectorAsync(".sessionlist");
            await page.WaitForSelectorAsync(".sessionlist table.el-table__body", 
                new() { State = WaitForSelectorState.Visible });
            
            var sessionRow = page.Locator(".sessionlist table.el-table__body tr:not(.el-table__empty-text)").First;
            await sessionRow.WaitForAsync(new() { Timeout = timeoutSeconds * 1000 });
            return sessionRow;
        }

        /// <summary>
        /// Get the selected session row
        /// </summary>
        public ILocator GetSelectedSessionRow()
        {
            return page.Locator(".sessionlist table.el-table__body tr.current-row");
        }

        /// <summary>
        /// Extract session ID from current URL
        /// </summary>
        public string ExtractSessionIdFromUrl()
        {
            var currentUrl = page.Url;
            var match = System.Text.RegularExpressions.Regex.Match(currentUrl, @"#/sessions/session/([^/?]+)");
            return match.Success ? match.Groups[1].Value : null;
        }

        /// <summary>
        /// Get a message row by its data-message-id attribute
        /// </summary>
        public ILocator GetMessageRowById(string messageId)
        {
            return page.Locator($".messagelist table.el-table__body tr[data-message-id='{messageId}']");
        }

        /// <summary>
        /// Get a session row by its data-session-id attribute
        /// </summary>
        public ILocator GetSessionRowById(string sessionId)
        {
            return page.Locator($".sessionlist table.el-table__body tr[data-session-id='{sessionId}']");
        }

        #endregion

        public async Task<MessageListControl> GetMessageListAsync()
        {
            var messageListElement = page.Locator(".messagelist").First;
            await messageListElement.WaitForAsync();
            return new MessageListControl(messageListElement);
        }

        public ILocator GetSettingsButton()
        {
            return page.Locator("button[title='Settings']");
        }

        public async Task OpenSettingsAsync()
        {
            var settingsButton = GetSettingsButton();
            await settingsButton.ClickAsync();
        }

        public SettingsDialog SettingsDialog => new SettingsDialog(page);

        public MessageView MessageView => new MessageView(page);

        public ILocator GetDarkModeToggleButton()
        {
            // The dark mode toggle is the first circular button in the header
            // Since it's the only icon-only circular button in the header after the logo
            return page.Locator("header button.el-button.is-circle").First;
        }

        public async Task ToggleDarkModeAsync()
        {
            var darkModeButton = GetDarkModeToggleButton();
            await darkModeButton.ClickAsync();
        }

        public async Task<bool> IsDarkModeActiveAsync()
        {
            // Check if the html element has the 'dark' class
            var htmlElement = page.Locator("html");
            var classes = await htmlElement.GetAttributeAsync("class");
            bool hasDarkClass = classes?.Contains("dark") == true;
            
            // Also check using JavaScript as a fallback
            if (!hasDarkClass)
            {
                hasDarkClass = await page.EvaluateAsync<bool>("() => document.documentElement.classList.contains('dark')");
            }
            
            return hasDarkClass;
        }

        public async Task SetDarkModeAsync(bool isDarkMode)
        {
            // For E2E testing, apply the dark class directly since the UI toggle
            // may use different mechanisms (VueUse, Element Plus, etc.)
            if (isDarkMode)
            {
                await page.EvaluateAsync("() => document.documentElement.classList.add('dark')");
            }
            else
            {
                await page.EvaluateAsync("() => document.documentElement.classList.remove('dark')");
            }
            
            await page.WaitForTimeoutAsync(500); // Allow CSS to take effect
        }

        public class MessageListControl
        {
            private readonly ILocator element;

            public MessageListControl(ILocator element)
            {
                this.element = element;
            }

            public Grid GetGrid()
            {
                var gridElement = element.Locator("table.el-table__body");
                return new Grid(gridElement);
            }
        }

        public class Grid
        {
            private readonly ILocator element;

            public Grid(ILocator element)
            {
                this.element = element;
            }

            public async Task<IReadOnlyList<GridRow>> GetRowsAsync()
            {
                var rowElements = await element.Locator("tr").AllAsync();
                return rowElements.Select(e => new GridRow(e)).ToList();
            }

            public class GridRow
            {
                private readonly ILocator element;

                public GridRow(ILocator element)
                {
                    this.element = element;
                }

                public async Task<IReadOnlyList<ILocator>> GetCellsAsync()
                {
                    return await element.Locator("td div.cell").AllAsync();
                }

                public async Task ClickAsync()
                {
                    await element.ClickAsync();
                }

                public async Task<bool> ContainsTextAsync(string text)
                {
                    var cells = await GetCellsAsync();
                    foreach (var cell in cells)
                    {
                        var cellText = await cell.TextContentAsync();
                        if (cellText?.Contains(text) == true)
                        {
                            return true;
                        }
                    }
                    return false;
                }

                public async Task<bool> IsSelectedAsync()
                {
                    var className = await element.GetAttributeAsync("class");
                    return className?.Contains("current-row") == true;
                }
            }
        }
    }
}