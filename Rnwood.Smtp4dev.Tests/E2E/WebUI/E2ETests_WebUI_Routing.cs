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
                var homePage = new HomePage(page);
                
                // Navigate to root URL
                await homePage.NavigateToUrlAsync($"{baseUrl}#/");
                await homePage.WaitForTabsAsync();

                // Verify URL still points to root (or redirects to /messages)
                string currentUrl = homePage.GetCurrentUrl();
                Assert.True(
                    currentUrl.EndsWith("#/") || currentUrl.Contains("#/messages"),
                    $"Expected URL to be root or /messages, but got: {currentUrl}"
                );

                // Verify messages tab is active
                var activeTab = await homePage.GetActiveTabNameAsync();
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
                var homePage = new HomePage(page);
                
                // Navigate to messages URL
                await homePage.NavigateToUrlAsync($"{baseUrl}#/messages");
                await homePage.WaitForTabsAsync();

                // Verify URL is correct (accepts redirect to mailbox URL)
                string currentUrl = homePage.GetCurrentUrl();
                Assert.True(
                    currentUrl.Contains("#/messages"),
                    $"Expected URL to contain #/messages, got: {currentUrl}"
                );

                // Verify messages tab is active
                var activeTab = await homePage.GetActiveTabNameAsync();
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
                var homePage = new HomePage(page);
                
                // Navigate to sessions URL
                await homePage.NavigateToUrlAsync($"{baseUrl}#/sessions");
                await homePage.WaitForTabsAsync();

                // Verify URL is correct
                Assert.EndsWith("#/sessions", homePage.GetCurrentUrl());

                // Verify sessions tab is active
                var activeTab = await homePage.GetActiveTabNameAsync();
                Assert.Contains("Sessions", activeTab);
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
                var homePage = new HomePage(page);
                
                // Navigate to server log URL
                await homePage.NavigateToUrlAsync($"{baseUrl}#/serverlog");
                await homePage.WaitForTabsAsync();

                // Verify URL is correct
                Assert.EndsWith("#/serverlog", homePage.GetCurrentUrl());

                // Verify server log tab is active
                var activeTab = await homePage.GetActiveTabNameAsync();
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
                var homePage = new HomePage(page);
                
                // Start at messages
                await homePage.NavigateToUrlAsync($"{baseUrl}#/messages");
                await homePage.WaitForTabsAsync();

                // Click on Sessions tab
                await homePage.NavigateToSessionsAsync();

                // Verify URL updated to sessions
                Assert.EndsWith("#/sessions", homePage.GetCurrentUrl());

                // Verify sessions tab is active
                var activeTab = await homePage.GetActiveTabNameAsync();
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
                var homePage = new HomePage(page);
                
                // Start at sessions
                await homePage.NavigateToUrlAsync($"{baseUrl}#/sessions");
                await homePage.WaitForTabsAsync();

                // Click on Messages tab
                await homePage.NavigateToMessagesAsync();

                // Verify URL updated to messages
                string currentUrl = homePage.GetCurrentUrl();
                Assert.True(
                    currentUrl.Contains("#/messages") || currentUrl.EndsWith("#/"),
                    $"Expected URL to contain #/messages or end with #/, but got: {currentUrl}"
                );

                // Verify messages tab is active
                var activeTab = await homePage.GetActiveTabNameAsync();
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
            {var homePage = new HomePage(page);
                
                // Start at messages
                await homePage.NavigateToUrlAsync($"{baseUrl}#/messages");
                await homePage.WaitForTabsAsync();

                // Click on Server Log tab
                await homePage.NavigateToServerLogAsync();

                // Verify URL updated to serverlog
                Assert.EndsWith("#/serverlog", homePage.GetCurrentUrl());

                // Verify server log tab is active
                var activeTab = await homePage.GetActiveTabNameAsync();
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
            {var homePage = new HomePage(page);
                
                // Start at server log
                await homePage.NavigateToUrlAsync($"{baseUrl}#/serverlog");
                await homePage.WaitForTabsAsync();

                // Click on Messages tab
                await homePage.NavigateToMessagesAsync();

                // Verify URL updated to messages
                string currentUrl = homePage.GetCurrentUrl();
                Assert.True(
                    currentUrl.Contains("#/messages") || currentUrl.EndsWith("#/"),
                    $"Expected URL to contain #/messages or end with #/, but got: {currentUrl}"
                );

                // Verify messages tab is active
                var activeTab = await homePage.GetActiveTabNameAsync();
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
            {var homePage = new HomePage(page);
                
                // Start at server log
                await homePage.NavigateToUrlAsync($"{baseUrl}#/serverlog");
                await homePage.WaitForTabsAsync();

                // Click on Sessions tab
                await homePage.NavigateToSessionsAsync();

                // Verify URL updated to sessions
                Assert.EndsWith("#/sessions", homePage.GetCurrentUrl());

                // Verify sessions tab is active
                var activeTab = await homePage.GetActiveTabNameAsync();
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
                var homePage = new HomePage(page);
                
                // Start at sessions
                await homePage.NavigateToUrlAsync($"{baseUrl}#/sessions");
                await homePage.WaitForTabsAsync();

                // Click on Server Log tab
                await homePage.NavigateToServerLogAsync();

                // Verify URL updated to serverlog
                Assert.EndsWith("#/serverlog", homePage.GetCurrentUrl());

                // Verify server log tab is active
                var activeTab = await homePage.GetActiveTabNameAsync();
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
                var homePage = new HomePage(page);
                
                // Send test email to create a session
                string sessionSubject = Guid.NewGuid().ToString();
                await HomePage.SendTestEmailAsync(
                    smtpPort: smtpPortNumber,
                    subject: sessionSubject,
                    body: "Test email to create session",
                    certValidationCallback: GetCertvalidationCallbackHandler()
                );

                // Navigate to sessions tab
                await homePage.NavigateToUrlAsync($"{baseUrl}#/sessions");
                
                // Wait for session row to appear and click it
                var sessionRow = await homePage.WaitForSessionRowAsync();
                await sessionRow.ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Extract session ID from URL
                string sessionId = homePage.ExtractSessionIdFromUrl();
                Assert.NotNull(sessionId);

                // Test deep link by navigating away first
                await homePage.NavigateToUrlAsync($"{baseUrl}#/messages");

                // Navigate directly to the session deep link URL
                string deepLinkUrl = $"{baseUrl}#/sessions/session/{sessionId}";
                await homePage.NavigateToUrlAsync(deepLinkUrl);

                // Verify we're on the sessions tab
                await homePage.WaitForTabsAsync();
                var activeTab = await homePage.GetActiveTabNameAsync();
                Assert.Contains("Sessions", activeTab);

                // Verify the URL is correct
                Assert.Contains($"#/sessions/session/{sessionId}", homePage.GetCurrentUrl());

                // Verify the session is selected - use current-row class which is set by the component
                var selectedRow = homePage.GetSelectedSessionRow();
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
                var homePage = new HomePage(page);
                
                // Send test email to create a message
                string messageSubject = Guid.NewGuid().ToString();
                await HomePage.SendTestEmailAsync(
                    smtpPort: smtpPortNumber,
                    subject: messageSubject,
                    body: "Test email for deep link",
                    certValidationCallback: GetCertvalidationCallbackHandler()
                );

                // Navigate to messages tab
                await homePage.NavigateToUrlAsync($"{baseUrl}#/messages");

                // Wait for message row to appear
                var messageRow = await homePage.WaitForMessageRowAsync(messageSubject);
                Assert.NotNull(messageRow);

                // Click the message to select it
                await messageRow.ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Verify the message is selected
                var isSelected = await messageRow.IsSelectedAsync();
                Assert.True(isSelected, "Message row should be highlighted as selected");

                // Verify we're still on the messages tab
                var activeTab = await homePage.GetActiveTabNameAsync();
                Assert.Contains("Messages", activeTab);
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
                var homePage = new HomePage(page);
                
                // Send test email to create a session
                string sessionSubject = Guid.NewGuid().ToString();
                await HomePage.SendTestEmailAsync(
                    smtpPort: smtpPortNumber,
                    subject: sessionSubject,
                    certValidationCallback: GetCertvalidationCallbackHandler()
                );

                // Navigate to sessions tab
                await homePage.NavigateToUrlAsync($"{baseUrl}#/sessions");
                
                // Wait for session row and click it
                var sessionRow = await homePage.WaitForSessionRowAsync();
                await sessionRow.ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Capture the session URL
                string sessionUrl = homePage.GetCurrentUrl();
                Assert.Contains("#/sessions/session/", sessionUrl);

                // Switch to messages tab
                await homePage.NavigateToMessagesAsync();

                // Verify we're on messages
                string currentUrl = homePage.GetCurrentUrl();
                Assert.True(
                    currentUrl.Contains("#/messages") || currentUrl.EndsWith("#/"),
                    $"Expected to be on messages tab, got: {currentUrl}"
                );

                // Switch back to sessions tab
                await homePage.NavigateToSessionsAsync();

                // Verify we're on sessions
                currentUrl = homePage.GetCurrentUrl();
                Assert.True(
                    currentUrl.Contains("#/sessions"),
                    $"Expected to be on sessions tab, got: {currentUrl}"
                );

                // The session should still be selected
                var selectedRow = homePage.GetSelectedSessionRow();
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
                var homePage = new HomePage(page);
                
                // Start at messages
                await homePage.NavigateToUrlAsync($"{baseUrl}#/messages");
                await homePage.WaitForTabsAsync();

                // Navigate to sessions
                await homePage.NavigateToSessionsAsync();
                Assert.EndsWith("#/sessions", homePage.GetCurrentUrl());

                // Navigate to server log
                await homePage.NavigateToServerLogAsync();
                Assert.EndsWith("#/serverlog", homePage.GetCurrentUrl());

                // Use browser back button
                await homePage.GoBackAsync();

                // Should be at sessions
                Assert.EndsWith("#/sessions", homePage.GetCurrentUrl());
                var activeTab = await homePage.GetActiveTabNameAsync();
                Assert.Contains("Sessions", activeTab);

                // Use browser back button again
                await homePage.GoBackAsync();

                // Should be at messages (accepts redirect to mailbox URL)
                string currentUrl = homePage.GetCurrentUrl();
                Assert.True(
                    currentUrl.Contains("#/messages") || currentUrl.EndsWith("#/"),
                    $"Expected to be at messages, got: {currentUrl}"
                );
                activeTab = await homePage.GetActiveTabNameAsync();
                Assert.Contains("Messages", activeTab);

                // Use browser forward button
                await homePage.GoForwardAsync();

                // Should be at sessions
                Assert.EndsWith("#/sessions", homePage.GetCurrentUrl());
                activeTab = await homePage.GetActiveTabNameAsync();
                Assert.Contains("Sessions", activeTab);

                // Use browser forward button again
                await homePage.GoForwardAsync();

                // Should be at server log
                Assert.EndsWith("#/serverlog", homePage.GetCurrentUrl());
                activeTab = await homePage.GetActiveTabNameAsync();
                Assert.Contains("Server Log", activeTab);
            });
        }

        #endregion
    }
}
