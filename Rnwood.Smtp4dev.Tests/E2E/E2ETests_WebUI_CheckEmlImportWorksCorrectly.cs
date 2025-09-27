using Microsoft.Playwright;
using Rnwood.Smtp4dev.Tests.E2E.PageModel;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Rnwood.Smtp4dev.Tests.E2E.WebUI
{
    public class E2ETests_WebUI_CheckEmlImportWorksCorrectly : E2ETestsWebUIBase
    {
        public E2ETests_WebUI_CheckEmlImportWorksCorrectly(ITestOutputHelper output) : base(output)
        {
        }


        [Fact]
        public void CheckEmlImportWorksCorrectly()
        {
            RunUITestAsync(nameof(CheckEmlImportWorksCorrectly), async (page, baseUrl, smtpPortNumber) =>
            {
                await page.GotoAsync(baseUrl.ToString());
                var homePage = new HomePage(page);

                var messageList = await WaitForAsync(async () => await homePage.GetMessageListAsync());
                Assert.NotNull(messageList);

                // Wait for the page to fully load by waiting for the mailbox selector to be populated
                await page.WaitForSelectorAsync(".el-select", new PageWaitForSelectorOptions { Timeout = 30000 });
                
                // Wait a bit more for Vue.js to finish initialization
                await page.WaitForTimeoutAsync(2000);
                
                // Find and ensure the Import button is visible and enabled
                var importButton = page.Locator("button[title='Import EML files']");
                await importButton.WaitForAsync(new LocatorWaitForOptions { Timeout = 30000 });
                
                // Additional wait to ensure the button is enabled (mailbox is selected)
                await WaitForAsync(async () => 
                {
                    var isEnabled = await importButton.IsEnabledAsync();
                    return isEnabled ? importButton : null;
                });

                // Create a test EML file content
                string testEmlContent = @"From: test-import@example.com
To: recipient@example.com
Subject: E2E Test Email Import
Date: Wed, 01 Jan 2025 12:00:00 +0000
Content-Type: text/plain; charset=utf-8

This is a test email for E2E import functionality.

It should be imported successfully into smtp4dev via the web UI.

Best regards,
E2E Test";

                // Write EML file to temp location
                string tempEmlFile = System.IO.Path.GetTempFileName() + ".eml";
                await System.IO.File.WriteAllTextAsync(tempEmlFile, testEmlContent);

                try
                {
                    // Prepare to intercept the file chooser event triggered by the Import button click
                    var fileChooserTask = page.WaitForFileChooserAsync();
                    
                    // Click Import button - this should trigger the file chooser directly (no dialog)
                    await importButton.ClickAsync();
                    
                    // Handle the file chooser that appears
                    var fileChooser = await fileChooserTask;
                    await fileChooser.SetFilesAsync(tempEmlFile);

                    // Wait for progress notification to appear
                    var progressNotification = page.Locator(".el-notification", new PageLocatorOptions { HasText = "Import in Progress" });
                    await progressNotification.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });

                    // Wait for success notification to appear
                    var successNotification = page.Locator(".el-notification", new PageLocatorOptions { HasText = "Import Complete" });
                    await successNotification.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });

                    // Wait a moment for the message list to refresh
                    await page.WaitForTimeoutAsync(1000);

                    // Check that the imported message appears in the message list
                    var grid = messageList.GetGrid();
                    var messageRow = await WaitForAsync(async () =>
                    {
                        var rows = await grid.GetRowsAsync();
                        return rows.FirstOrDefault(row => row.ContainsTextAsync("E2E Test Email Import").Result);
                    });

                    Assert.NotNull(messageRow);
                    Assert.True(await messageRow.ContainsTextAsync("E2E Test Email Import"));
                    Assert.True(await messageRow.ContainsTextAsync("test-import@example.com"));
                    Assert.True(await messageRow.ContainsTextAsync("recipient@example.com"));
                    
                    // Verify that the imported message is selected (should be highlighted/current)
                    // Check if the row has the current-row class or similar indicator
                    var isSelected = await messageRow.IsSelectedAsync();
                    Assert.True(isSelected, "Imported message should be selected");
                }
                finally
                {
                    // Clean up temp file
                    if (System.IO.File.Exists(tempEmlFile))
                    {
                        System.IO.File.Delete(tempEmlFile);
                    }
                }
            });
        }
    }
}