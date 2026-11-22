using Microsoft.Playwright;
using Rnwood.Smtp4dev.Tests.E2E.WebUI.PageModel;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using static Microsoft.Playwright.Assertions;

namespace Rnwood.Smtp4dev.Tests.E2E.WebUI
{
    /// <summary>
    /// UI tests for the Fake Gateways tabs (SMS = Konzulta, Targito). This is also the test coverage of
    /// the link handling (safeHttpUrl): only absolute http(s) values may become links.
    /// </summary>
    [Collection("E2ETests")]
    public class E2ETests_WebUI_FakeGateways : E2ETestsWebUIBase
    {
        public E2ETests_WebUI_FakeGateways(ITestOutputHelper output) : base(output)
        {
        }

        private static UITestOptions Enabled()
        {
            var options = new UITestOptions();
            options.EnvironmentVariables["FakeGateways__Enabled"] = "true";
            return options;
        }

        [SkipInDockerFact]
        public void Tabs_WhenEnabled_AreVisible()
        {
            RunUITestAsync(nameof(Tabs_WhenEnabled_AreVisible), async (page, baseUrl, smtpPortNumber) =>
            {
                var home = new HomePage(page);
                var gateways = new FakeGatewaysPage(page, baseUrl);

                await home.NavigateToUrlAsync($"{baseUrl}#/");
                await home.WaitForTabsAsync();

                await Expect(gateways.SmsTab).ToBeVisibleAsync();
                await Expect(gateways.TargitoTab).ToBeVisibleAsync();

                await gateways.OpenSmsTabAsync();
                await Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("#/sms$"));
                await gateways.OpenTargitoTabAsync();
                await Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("#/targito$"));
            }, Enabled());
        }

        [SkipInDockerFact]
        public void Tabs_WhenDisabled_AreHiddenAndRestOfUiWorks()
        {
            RunUITestAsync(nameof(Tabs_WhenDisabled_AreHiddenAndRestOfUiWorks), async (page, baseUrl, smtpPortNumber) =>
            {
                var home = new HomePage(page);
                var gateways = new FakeGatewaysPage(page, baseUrl);

                await home.NavigateToUrlAsync($"{baseUrl}#/");
                await home.WaitForTabsAsync();
                await Expect(gateways.MessagesTab).ToBeVisibleAsync();

                // Give the health check time to answer, the tabs must still not exist.
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await Expect(gateways.SmsTab).ToHaveCountAsync(0);
                await Expect(gateways.TargitoTab).ToHaveCountAsync(0);

                // A deep link to a hidden tab falls back to Messages (start from another tab so the fallback is observable).
                await home.NavigateToUrlAsync($"{baseUrl}#/serverlog");
                await Expect(gateways.ActiveTab).ToContainTextAsync("Server Log");
                await home.NavigateToUrlAsync($"{baseUrl}#/sms/does-not-exist");
                await Expect(gateways.ActiveTab).ToContainTextAsync("Messages");
                await Expect(gateways.SmsTab).ToHaveCountAsync(0);
                await Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("#/messages"));
            });
        }

        [SkipInDockerFact]
        public void Sms_PostedWhileTabIsOpen_AppearsWithoutReloadAndShowsText()
        {
            RunUITestAsync(nameof(Sms_PostedWhileTabIsOpen_AppearsWithoutReloadAndShowsText), async (page, baseUrl, smtpPortNumber) =>
            {
                var home = new HomePage(page);
                var gateways = new FakeGatewaysPage(page, baseUrl);

                await home.NavigateToUrlAsync($"{baseUrl}#/");
                await home.WaitForTabsAsync();
                await gateways.OpenSmsTabAsync();
                await Expect(gateways.SmsRows).ToHaveCountAsync(0);

                string text = "Dobry den, overovaci kod: 123456. <b>not bold</b> & more";
                await gateways.PostSmsAsync("+420700000001", text, "987654");

                // No reload: the row arrives through SignalR
                await Expect(gateways.SmsRows).ToHaveCountAsync(1);
                await Expect(gateways.SmsRows.First).ToContainTextAsync("+420700000001");

                await gateways.SmsRows.First.ClickAsync();
                await Expect(gateways.SmsViewText).ToHaveTextAsync(text);
                await Expect(gateways.SmsViewTo).ToContainTextAsync("+420700000001");

                // Text is rendered as text, never as HTML
                await Expect(gateways.SmsViewText.Locator("b")).ToHaveCountAsync(0);

                string[] ids = await gateways.GetSmsIdsAsync();
                Assert.Single(ids);
                await Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("#/sms/" + ids[0] + "$"));
            }, Enabled());
        }

        [SkipInDockerFact]
        public void Targito_ConfirmUrlIsLink_AndUnsafeValuesArePlainText()
        {
            RunUITestAsync(nameof(Targito_ConfirmUrlIsLink_AndUnsafeValuesArePlainText), async (page, baseUrl, smtpPortNumber) =>
            {
                var home = new HomePage(page);
                var gateways = new FakeGatewaysPage(page, baseUrl);

                await home.NavigateToUrlAsync($"{baseUrl}#/");
                await home.WaitForTabsAsync();
                await gateways.OpenTargitoTabAsync();

                string confirmUrl = "https://app.example.com/verify?gah=G&code=C";
                await gateways.PostTargitoAsync("e2e.a@example.com",
                    "{\"CONFIRM_URL\":\"" + confirmUrl + "\",\"EVIL_JS\":\"javascript:alert(1)\",\"EVIL_DATA\":\"data:text/html,<script>alert(1)</script>\","
                    + "\"EVIL_RELATIVE\":\"/relative/path\",\"EVIL_PROTOCOL_RELATIVE\":\"//evil.example.com/x\",\"PLAIN\":\"Jan\",\"ISO_DATE\":\"2026-11-17T10:15:30.123Z\"}");

                await Expect(gateways.TargitoRows).ToHaveCountAsync(1);
                await gateways.TargitoRows.First.ClickAsync();

                // Absolute https URL: clickable, safe rel/target
                var link = gateways.TargitoDataRow("CONFIRM_URL").Locator("a");
                await Expect(link).ToHaveCountAsync(1);
                await Expect(link).ToHaveAttributeAsync("href", confirmUrl);
                await Expect(link).ToHaveAttributeAsync("target", "_blank");
                await Expect(link).ToHaveAttributeAsync("rel", new System.Text.RegularExpressions.Regex("noopener"));
                await Expect(link).ToHaveAttributeAsync("rel", new System.Text.RegularExpressions.Regex("noreferrer"));

                // Everything else is plain text, never a link
                foreach (var (key, value) in new[]
                {
                    ("EVIL_JS", "javascript:alert(1)"),
                    ("EVIL_DATA", "data:text/html,<script>alert(1)</script>"),
                    ("EVIL_RELATIVE", "/relative/path"),
                    ("EVIL_PROTOCOL_RELATIVE", "//evil.example.com/x"),
                    ("PLAIN", "Jan"),
                    // must be shown verbatim, not converted to a Date by the global axios interceptor
                    ("ISO_DATE", "2026-11-17T10:15:30.123Z"),
                })
                {
                    var row = gateways.TargitoDataRow(key);
                    await Expect(row.Locator("a")).ToHaveCountAsync(0);
                    await Expect(row.Locator(".datatext")).ToHaveTextAsync(value);
                }
                await Expect(page.Locator("a[href^='javascript:']")).ToHaveCountAsync(0);
            }, Enabled());
        }

        [SkipInDockerFact]
        public void Sms_DeepLink_SurvivesReload()
        {
            RunUITestAsync(nameof(Sms_DeepLink_SurvivesReload), async (page, baseUrl, smtpPortNumber) =>
            {
                var home = new HomePage(page);
                var gateways = new FakeGatewaysPage(page, baseUrl);

                await gateways.PostSmsAsync("+420700000001", "first message", "1");
                await gateways.PostSmsAsync("+420700000002", "second message", "2");
                string[] ids = await gateways.GetSmsIdsAsync();
                Assert.Equal(2, ids.Length);
                string idOfFirst = ids[1]; // newest first

                await home.NavigateToUrlAsync($"{baseUrl}#/sms/{idOfFirst}");
                await home.WaitForTabsAsync();
                await Expect(gateways.SmsViewText).ToHaveTextAsync("first message");

                await page.ReloadAsync();
                await home.WaitForTabsAsync();

                await Expect(gateways.ActiveTab).ToContainTextAsync("SMS");
                await Expect(gateways.SmsViewText).ToHaveTextAsync("first message");
                await Expect(gateways.SmsRowById(idOfFirst)).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("current-row"));
                await Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("#/sms/" + idOfFirst + "$"));
            }, Enabled());
        }

        [SkipInDockerFact]
        public void Sms_CodeButtonAndFilterEmptyState()
        {
            RunUITestAsync(nameof(Sms_CodeButtonAndFilterEmptyState), async (page, baseUrl, smtpPortNumber) =>
            {
                var home = new HomePage(page);
                var gateways = new FakeGatewaysPage(page, baseUrl);

                await gateways.PostSmsAsync("+420700000001", "pouzijte overovaci kod: 123456.", "1");

                await home.NavigateToUrlAsync($"{baseUrl}#/sms");
                await home.WaitForTabsAsync();
                await Expect(gateways.SmsRows).ToHaveCountAsync(1);
                await gateways.SmsRows.First.ClickAsync();

                // The code is found on the client although the API was not asked for codeRegex
                await Expect(page.GetByTestId("sms-view-copy-code")).ToContainTextAsync("123456");

                // The filter hides the selected row: the empty state says so and the detail is cleared
                await page.GetByTestId("sms-filter").FillAsync("zzz-no-match");
                await Expect(gateways.SmsRows).ToHaveCountAsync(0);
                await Expect(gateways.SmsList).ToContainTextAsync("No SMS match the filter");
                await Expect(gateways.SmsView).ToContainTextAsync("No SMS selected");
            }, Enabled());
        }

        [SkipInDockerFact]
        public void Sms_DeepLinkToUnknownId_ShowsNoDetail()
        {
            RunUITestAsync(nameof(Sms_DeepLinkToUnknownId_ShowsNoDetail), async (page, baseUrl, smtpPortNumber) =>
            {
                var home = new HomePage(page);
                var gateways = new FakeGatewaysPage(page, baseUrl);

                await gateways.PostSmsAsync("+420700000001", "some message", "1");

                await home.NavigateToUrlAsync($"{baseUrl}#/sms/does-not-exist");
                await home.WaitForTabsAsync();
                await Expect(gateways.ActiveTab).ToContainTextAsync("SMS");
                await Expect(gateways.SmsRows).ToHaveCountAsync(1);
                await Expect(gateways.SmsView).ToContainTextAsync("No SMS selected");
                await Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("#/sms/does-not-exist$"));
            }, Enabled());
        }

        [SkipInDockerFact]
        public void Sms_Delete_RemovesItem()
        {
            RunUITestAsync(nameof(Sms_Delete_RemovesItem), async (page, baseUrl, smtpPortNumber) =>
            {
                var home = new HomePage(page);
                var gateways = new FakeGatewaysPage(page, baseUrl);

                await gateways.PostSmsAsync("+420700000001", "to be deleted", "1");
                await gateways.PostSmsAsync("+420700000002", "to stay", "2");

                await home.NavigateToUrlAsync($"{baseUrl}#/sms");
                await home.WaitForTabsAsync();
                await Expect(gateways.SmsRows).ToHaveCountAsync(2);

                await gateways.SmsRows.Filter(new() { HasText = "to be deleted" }).ClickAsync();
                await Expect(gateways.SmsViewText).ToHaveTextAsync("to be deleted");
                await gateways.SmsViewDelete.ClickAsync();

                await Expect(gateways.SmsRows).ToHaveCountAsync(1);
                await Expect(gateways.SmsRows.First).ToContainTextAsync("to stay");
                await Expect(gateways.SmsViewText).ToHaveCountAsync(0);
                Assert.Single(await gateways.GetSmsIdsAsync());
            }, Enabled());
        }

        [SkipInDockerFact]
        public void Targito_Delete_RemovesItem()
        {
            RunUITestAsync(nameof(Targito_Delete_RemovesItem), async (page, baseUrl, smtpPortNumber) =>
            {
                var home = new HomePage(page);
                var gateways = new FakeGatewaysPage(page, baseUrl);

                await gateways.PostTargitoAsync("e2e.a@example.com", "{\"CONFIRM_URL\":\"https://app.example.com/x\"}");

                await home.NavigateToUrlAsync($"{baseUrl}#/targito");
                await home.WaitForTabsAsync();
                await Expect(gateways.TargitoRows).ToHaveCountAsync(1);

                await gateways.TargitoRows.First.ClickAsync();
                await gateways.TargitoViewDelete.ClickAsync();

                await Expect(gateways.TargitoRows).ToHaveCountAsync(0);
                Assert.Equal(0, await gateways.GetTargitoCountAsync());
            }, Enabled());
        }
    }
}
