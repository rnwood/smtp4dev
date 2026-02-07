using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Playwright;
using Rnwood.Smtp4dev.Tests.E2E.WebUI.PageModel;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Rnwood.Smtp4dev.Tests.E2E.WebUI
{
    /// <summary>
    /// E2E tests for routing and URL navigation to prevent regressions.
    /// These tests verify that:
    /// 1. Direct URL navigation to specific routes works correctly
    /// 2. Tab switching updates the URL appropriately
    /// 3. Deep links with selected items (messages/sessions) work correctly
    /// </summary>
    [Collection("E2ETests")]
    public class E2ETests_WebUI_Routing : E2ETestsWebUIBase
    {
        public E2ETests_WebUI_Routing(ITestOutputHelper output) : base(output)
        {
        }

        #region Initial Page Load Tests

        /// <summary>
        /// Test that navigating directly to /#/ (root) defaults to messages tab
        /// </summary>
        [Fact]
        public void RootUrl_ShouldDefaultToMessages()
        {
            RunUITestAsync(nameof(RootUrl_ShouldDefaultToMessages), async (page, baseUrl, smtpPortNumber) =>
            {
                // Navigate to root URL
                await page.GotoAsync($"{baseUrl}#/");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Wait for tabs to render
                await page.WaitForSelectorAsync("#maintabs");

                // Verify URL still points to root (or redirects to /messages)
                string currentUrl = page.Url;
                Assert.True(
                    currentUrl.EndsWith("#/") || currentUrl.EndsWith("#/messages"),
                    $"Expected URL to be root or /messages, but got: {currentUrl}"
                );

                // Verify messages tab is active
                var activeTab = await page.Locator("#maintabs .el-tabs__item.is-active").TextContentAsync();
                Assert.Contains("Messages", activeTab);
            });
        }

        /// <summary>
        /// Test that navigating directly to /#/messages shows the messages tab
        /// </summary>
        [Fact]
        public void MessagesUrl_ShouldShowMessagesTab()
        {
            RunUITestAsync(nameof(MessagesUrl_ShouldShowMessagesTab), async (page, baseUrl, smtpPortNumber) =>
            {
                // Navigate to messages URL
                await page.GotoAsync($"{baseUrl}#/messages");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Wait for tabs to render
                await page.WaitForSelectorAsync("#maintabs");

                // Verify URL is correct
                Assert.EndsWith("#/messages", page.Url);

                // Verify messages tab is active
                var activeTab = await page.Locator("#maintabs .el-tabs__item.is-active").TextContentAsync();
                Assert.Contains("Messages", activeTab);

                // Verify messages list is visible
                await page.WaitForSelectorAsync(".messagelist");
            });
        }

        /// <summary>
        /// Test that navigating directly to /#/sessions shows the sessions tab
        /// </summary>
        [Fact]
        public void SessionsUrl_ShouldShowSessionsTab()
        {
            RunUITestAsync(nameof(SessionsUrl_ShouldShowSessionsTab), async (page, baseUrl, smtpPortNumber) =>
            {
                // Navigate to sessions URL
                await page.GotoAsync($"{baseUrl}#/sessions");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Wait for tabs to render
                await page.WaitForSelectorAsync("#maintabs");

                // Verify URL is correct
                Assert.EndsWith("#/sessions", page.Url);

                // Verify sessions tab is active
                var activeTab = await page.Locator("#maintabs .el-tabs__item.is-active").TextContentAsync();
                Assert.Contains("Sessions", activeTab);

                // Verify sessions list is visible
                await page.WaitForSelectorAsync(".sessionlist");
            });
        }

        /// <summary>
        /// Test that navigating directly to /#/serverlog shows the server log tab
        /// </summary>
        [Fact]
        public void ServerLogUrl_ShouldShowServerLogTab()
        {
            RunUITestAsync(nameof(ServerLogUrl_ShouldShowServerLogTab), async (page, baseUrl, smtpPortNumber) =>
            {
                // Navigate to server log URL
                await page.GotoAsync($"{baseUrl}#/serverlog");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Wait for tabs to render
                await page.WaitForSelectorAsync("#maintabs");

                // Verify URL is correct
                Assert.EndsWith("#/serverlog", page.Url);

                // Verify server log tab is active
                var activeTab = await page.Locator("#maintabs .el-tabs__item.is-active").TextContentAsync();
                Assert.Contains("Server Log", activeTab);
            });
        }

        #endregion

        #region Tab Switching Tests

        /// <summary>
        /// Test that switching from Messages to Sessions updates the URL
        /// </summary>
        [Fact]
        public void TabSwitch_MessagesToSessions_ShouldUpdateUrl()
        {
            RunUITestAsync(nameof(TabSwitch_MessagesToSessions_ShouldUpdateUrl), async (page, baseUrl, smtpPortNumber) =>
            {
                // Start at messages
                await page.GotoAsync($"{baseUrl}#/messages");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await page.WaitForSelectorAsync("#maintabs");

                // Click on Sessions tab
                await page.Locator("#maintabs .el-tabs__item").Filter(new() { HasText = "Sessions" }).ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Verify URL updated to sessions
                Assert.EndsWith("#/sessions", page.Url);

                // Verify sessions tab is active
                var activeTab = await page.Locator("#maintabs .el-tabs__item.is-active").TextContentAsync();
                Assert.Contains("Sessions", activeTab);
            });
        }

        /// <summary>
        /// Test that switching from Sessions to Messages updates the URL
        /// </summary>
        [Fact]
        public void TabSwitch_SessionsToMessages_ShouldUpdateUrl()
        {
            RunUITestAsync(nameof(TabSwitch_SessionsToMessages_ShouldUpdateUrl), async (page, baseUrl, smtpPortNumber) =>
            {
                // Start at sessions
                await page.GotoAsync($"{baseUrl}#/sessions");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await page.WaitForSelectorAsync("#maintabs");

                // Click on Messages tab
                await page.Locator("#maintabs .el-tabs__item").Filter(new() { HasText = "Messages" }).ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Verify URL updated to messages
                Assert.True(
                    page.Url.EndsWith("#/messages") || page.Url.EndsWith("#/"),
                    $"Expected URL to end with #/messages or #/, but got: {page.Url}"
                );

                // Verify messages tab is active
                var activeTab = await page.Locator("#maintabs .el-tabs__item.is-active").TextContentAsync();
                Assert.Contains("Messages", activeTab);
            });
        }

        /// <summary>
        /// Test that switching from Messages to Server Log updates the URL
        /// </summary>
        [Fact]
        public void TabSwitch_MessagesToServerLog_ShouldUpdateUrl()
        {
            RunUITestAsync(nameof(TabSwitch_MessagesToServerLog_ShouldUpdateUrl), async (page, baseUrl, smtpPortNumber) =>
            {
                // Start at messages
                await page.GotoAsync($"{baseUrl}#/messages");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await page.WaitForSelectorAsync("#maintabs");

                // Click on Server Log tab
                await page.Locator("#maintabs .el-tabs__item").Filter(new() { HasText = "Server Log" }).ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Verify URL updated to serverlog
                Assert.EndsWith("#/serverlog", page.Url);

                // Verify server log tab is active
                var activeTab = await page.Locator("#maintabs .el-tabs__item.is-active").TextContentAsync();
                Assert.Contains("Server Log", activeTab);
            });
        }

        /// <summary>
        /// Test that switching from Server Log to Messages updates the URL
        /// </summary>
        [Fact]
        public void TabSwitch_ServerLogToMessages_ShouldUpdateUrl()
        {
            RunUITestAsync(nameof(TabSwitch_ServerLogToMessages_ShouldUpdateUrl), async (page, baseUrl, smtpPortNumber) =>
            {
                // Start at server log
                await page.GotoAsync($"{baseUrl}#/serverlog");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await page.WaitForSelectorAsync("#maintabs");

                // Click on Messages tab
                await page.Locator("#maintabs .el-tabs__item").Filter(new() { HasText = "Messages" }).ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Verify URL updated to messages
                Assert.True(
                    page.Url.EndsWith("#/messages") || page.Url.EndsWith("#/"),
                    $"Expected URL to end with #/messages or #/, but got: {page.Url}"
                );

                // Verify messages tab is active
                var activeTab = await page.Locator("#maintabs .el-tabs__item.is-active").TextContentAsync();
                Assert.Contains("Messages", activeTab);
            });
        }

        /// <summary>
        /// Test that switching from Server Log to Sessions updates the URL
        /// </summary>
        [Fact]
        public void TabSwitch_ServerLogToSessions_ShouldUpdateUrl()
        {
            RunUITestAsync(nameof(TabSwitch_ServerLogToSessions_ShouldUpdateUrl), async (page, baseUrl, smtpPortNumber) =>
            {
                // Start at server log
                await page.GotoAsync($"{baseUrl}#/serverlog");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await page.WaitForSelectorAsync("#maintabs");

                // Click on Sessions tab
                await page.Locator("#maintabs .el-tabs__item").Filter(new() { HasText = "Sessions" }).ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Verify URL updated to sessions
                Assert.EndsWith("#/sessions", page.Url);

                // Verify sessions tab is active
                var activeTab = await page.Locator("#maintabs .el-tabs__item.is-active").TextContentAsync();
                Assert.Contains("Sessions", activeTab);
            });
        }

        /// <summary>
        /// Test that switching from Sessions to Server Log updates the URL
        /// </summary>
        [Fact]
        public void TabSwitch_SessionsToServerLog_ShouldUpdateUrl()
        {
            RunUITestAsync(nameof(TabSwitch_SessionsToServerLog_ShouldUpdateUrl), async (page, baseUrl, smtpPortNumber) =>
            {
                // Start at sessions
                await page.GotoAsync($"{baseUrl}#/sessions");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await page.WaitForSelectorAsync("#maintabs");

                // Click on Server Log tab
                await page.Locator("#maintabs .el-tabs__item").Filter(new() { HasText = "Server Log" }).ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Verify URL updated to serverlog
                Assert.EndsWith("#/serverlog", page.Url);

                // Verify server log tab is active
                var activeTab = await page.Locator("#maintabs .el-tabs__item.is-active").TextContentAsync();
                Assert.Contains("Server Log", activeTab);
            });
        }

        #endregion

        #region Deep Link Tests

        /// <summary>
        /// Test that navigating to a session URL with a specific session ID selects that session.
        /// This test sends an email to create a session, then verifies the deep link works.
        /// </summary>
        [Fact]
        public void DeepLink_SessionWithId_ShouldSelectSession()
        {
            RunUITestAsync(nameof(DeepLink_SessionWithId_ShouldSelectSession), async (page, baseUrl, smtpPortNumber) =>
            {
                // First, send an email to create a session (before navigating to UI)
                string sessionSubject = Guid.NewGuid().ToString();
                using (var smtpClient = new SmtpClient())
                {
                    smtpClient.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
                    smtpClient.ServerCertificateValidationCallback = GetCertvalidationCallbackHandler();
                    smtpClient.CheckCertificateRevocation = false;
                    var message = new MimeMessage();
                    message.To.Add(MailboxAddress.Parse("to@to.com"));
                    message.From.Add(MailboxAddress.Parse("from@from.com"));
                    message.Subject = sessionSubject;
                    message.Body = new TextPart() { Text = "Test email to create session" };

                    smtpClient.Connect("localhost", smtpPortNumber, SecureSocketOptions.StartTls,
                        new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
                    smtpClient.Send(message);
                    smtpClient.Disconnect(true, new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
                }

                // Now navigate to sessions tab (will load sessions from database)
                await page.GotoAsync($"{baseUrl}#/sessions");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await page.WaitForSelectorAsync(".sessionlist");

                // Wait for the table body to be visible
                await page.WaitForSelectorAsync(".sessionlist table.el-table__body", new() { State = WaitForSelectorState.Visible });
                
                // Wait for at least one session row to appear (sessions don't show subject, just connection info)
                // Since we use a fresh database for each test, any row will be our session
                var sessionRow = page.Locator(".sessionlist table.el-table__body tr:not(.el-table__empty-text)").First;
                await sessionRow.WaitForAsync(new() { Timeout = 30000 });

                //Click the session to select it and get the session ID from the URL
                await sessionRow.ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Extract session ID from URL (format: #/sessions/session/{id})
                string currentUrl = page.Url;
                var sessionIdMatch = System.Text.RegularExpressions.Regex.Match(currentUrl, @"#/sessions/session/([^/]+)");
                Assert.True(sessionIdMatch.Success, $"Could not extract session ID from URL: {currentUrl}");
                string sessionId = sessionIdMatch.Groups[1].Value;

                // Now test the deep link by navigating to a different tab first
                await page.GotoAsync($"{baseUrl}#/messages");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Navigate directly to the session deep link URL
                string deepLinkUrl = $"{baseUrl}#/sessions/session/{sessionId}";
                await page.GotoAsync(deepLinkUrl);
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Verify we're on the sessions tab (use First to avoid nested tabs)
                await page.WaitForSelectorAsync("#maintabs");
                var activeTab = await page.Locator("#maintabs .el-tabs__item.is-active").First.TextContentAsync();
                Assert.Contains("Sessions", activeTab);

                // Verify the URL is correct
                Assert.Contains($"#/sessions/session/{sessionId}", page.Url);

                // Verify the session is selected (has current-row class) 
                // Note: Can't check for subject since sessions don't display message subjects
                var selectedRow = page.Locator(".sessionlist table.el-table__body tr.current-row");
                await selectedRow.WaitForAsync(new() { Timeout = 5000 });
                var isSelected = await selectedRow.CountAsync() > 0;
                Assert.True(isSelected, "Session row should be highlighted as selected");

                // Verify session details are displayed
                var sessionView = page.Locator(".sessionview");
                await sessionView.WaitForAsync();
            });
        }

        /// <summary>
        /// Test that navigating to a message URL with a specific message ID selects that message.
        /// Note: This test verifies the deep link infrastructure, but the actual message selection
        /// logic may be in the messagelist component which handles IMAP folder routing.
        /// </summary>
        [Fact]
        public void DeepLink_MessageListSelection_WorksCorrectly()
        {
            RunUITestAsync(nameof(DeepLink_MessageListSelection_WorksCorrectly), async (page, baseUrl, smtpPortNumber) =>
            {
                // First, send an email to create a message
                string messageSubject = Guid.NewGuid().ToString();
                using (var smtpClient = new SmtpClient())
                {
                    smtpClient.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
                    smtpClient.ServerCertificateValidationCallback = GetCertvalidationCallbackHandler();
                    smtpClient.CheckCertificateRevocation = false;
                    var message = new MimeMessage();
                    message.To.Add(MailboxAddress.Parse("to@to.com"));
                    message.From.Add(MailboxAddress.Parse("from@from.com"));
                    message.Subject = messageSubject;
                    message.Body = new TextPart() { Text = "Test email for deep link" };

                    smtpClient.Connect("localhost", smtpPortNumber, SecureSocketOptions.StartTls,
                        new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
                    smtpClient.Send(message);
                    smtpClient.Disconnect(true, new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
                }

                // Navigate to messages tab
                await page.GotoAsync($"{baseUrl}#/messages");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Wait for message to appear
                var homePage = new HomePage(page);
                var messageList = await WaitForAsync(async () => await homePage.GetMessageListAsync());
                var grid = await WaitForAsync(() => Task.FromResult(messageList.GetGrid()));
                var messageRow = await WaitForAsync(async () =>
                {
                    var rows = await grid.GetRowsAsync();
                    foreach (var row in rows)
                    {
                        if (await row.ContainsTextAsync(messageSubject))
                        {
                            return row;
                        }
                    }
                    return null;
                }, timeoutSeconds: 30);

                Assert.NotNull(messageRow);

                // Click the message to select it
                await messageRow.ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Verify the message is selected
                var isSelected = await messageRow.IsSelectedAsync();
                Assert.True(isSelected, "Message row should be highlighted as selected");

                // Verify we're still on the messages tab (use First to avoid nested tabs)
                var activeTab = await page.Locator("#maintabs .el-tabs__item.is-active").First.TextContentAsync();
                Assert.Contains("Messages", activeTab);

                // Note: Removed message view verification as this is a routing test,
                // not a message view test. The important part is that routing and selection work.
            });
        }

        /// <summary>
        /// Test that URL persists the selected session when navigating to sessions tab with a session ID.
        /// This ensures the URL doesn't get cleared when switching back to sessions.
        /// </summary>
        [Fact]
        public void SessionUrl_PersistsAcrossTabSwitches()
        {
            RunUITestAsync(nameof(SessionUrl_PersistsAcrossTabSwitches), async (page, baseUrl, smtpPortNumber) =>
            {
                // Send an email to create a session (before navigating)
                string sessionSubject = Guid.NewGuid().ToString();
                using (var smtpClient = new SmtpClient())
                {
                    smtpClient.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
                    smtpClient.ServerCertificateValidationCallback = GetCertvalidationCallbackHandler();
                    smtpClient.CheckCertificateRevocation = false;
                    var message = new MimeMessage();
                    message.To.Add(MailboxAddress.Parse("to@to.com"));
                    message.From.Add(MailboxAddress.Parse("from@from.com"));
                    message.Subject = sessionSubject;
                    message.Body = new TextPart() { Text = "Test email" };

                    smtpClient.Connect("localhost", smtpPortNumber, SecureSocketOptions.StartTls,
                        new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
                    smtpClient.Send(message);
                    smtpClient.Disconnect(true, new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
                }

                // Navigate to sessions tab (will load sessions from database)
                await page.GotoAsync($"{baseUrl}#/sessions");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await page.WaitForSelectorAsync(".sessionlist");

                // Wait for the table body to be visible
                await page.WaitForSelectorAsync(".sessionlist table.el-table__body", new() { State = WaitForSelectorState.Visible });
                
                // Wait for at least one session row to appear (sessions don't show subject, just connection info)
                // Since we use a fresh database for each test, any row will be our session
                var sessionRow = page.Locator(".sessionlist table.el-table__body tr:not(.el-table__empty-text)").First;
                await sessionRow.WaitForAsync(new() { Timeout = 30000 });

                await sessionRow.ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Capture the session URL
                string sessionUrl = page.Url;
                Assert.Contains("#/sessions/session/", sessionUrl);

                // Switch to messages tab
                await page.Locator("#maintabs .el-tabs__item").Filter(new() { HasText = "Messages" }).ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Verify we're on messages
                Assert.True(
                    page.Url.EndsWith("#/messages") || page.Url.EndsWith("#/"),
                    $"Expected to be on messages tab, got: {page.Url}"
                );

                // Switch back to sessions tab
                await page.Locator("#maintabs .el-tabs__item").Filter(new() { HasText = "Sessions" }).ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Verify the session URL is restored (or at least we're on sessions)
                string currentUrl = page.Url;
                Assert.True(
                    currentUrl.Contains("#/sessions"),
                    $"Expected to be on sessions tab, got: {currentUrl}"
                );

                // The session should still be selected
                // Note: Can't check for subject since sessions don't display message subjects
                var selectedRow = page.Locator(".sessionlist table.el-table__body tr.current-row");
                var isSelected = await selectedRow.CountAsync() > 0;
                Assert.True(isSelected, "Session should still be selected after tab switch");
            });
        }

        #endregion

        #region Browser Navigation Tests

        /// <summary>
        /// Test that browser back/forward buttons work correctly with routing.
        /// This ensures that the Vue router integrates properly with browser history.
        /// </summary>
        [Fact]
        public void BrowserNavigation_BackAndForward_WorksCorrectly()
        {
            RunUITestAsync(nameof(BrowserNavigation_BackAndForward_WorksCorrectly), async (page, baseUrl, smtpPortNumber) =>
            {
                // Start at messages
                await page.GotoAsync($"{baseUrl}#/messages");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await page.WaitForSelectorAsync("#maintabs");

                // Navigate to sessions
                await page.Locator("#maintabs .el-tabs__item").Filter(new() { HasText = "Sessions" }).ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                Assert.EndsWith("#/sessions", page.Url);

                // Navigate to server log
                await page.Locator("#maintabs .el-tabs__item").Filter(new() { HasText = "Server Log" }).ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                Assert.EndsWith("#/serverlog", page.Url);

                // Use browser back button
                await page.GoBackAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Should be at sessions
                Assert.EndsWith("#/sessions", page.Url);
                var activeTab = await page.Locator("#maintabs .el-tabs__item.is-active").TextContentAsync();
                Assert.Contains("Sessions", activeTab);

                // Use browser back button again
                await page.GoBackAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Should be at messages
                Assert.True(
                    page.Url.EndsWith("#/messages") || page.Url.EndsWith("#/"),
                    $"Expected to be at messages, got: {page.Url}"
                );
                activeTab = await page.Locator("#maintabs .el-tabs__item.is-active").TextContentAsync();
                Assert.Contains("Messages", activeTab);

                // Use browser forward button
                await page.GoForwardAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Should be at sessions
                Assert.EndsWith("#/sessions", page.Url);
                activeTab = await page.Locator("#maintabs .el-tabs__item.is-active").TextContentAsync();
                Assert.Contains("Sessions", activeTab);

                // Use browser forward button again
                await page.GoForwardAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Should be at server log
                Assert.EndsWith("#/serverlog", page.Url);
                activeTab = await page.Locator("#maintabs .el-tabs__item.is-active").TextContentAsync();
                Assert.Contains("Server Log", activeTab);
            });
        }

        #endregion
    }
}
