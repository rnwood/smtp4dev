using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Playwright;
using MimeKit;
using Rnwood.Smtp4dev.Tests.E2E.PageModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Rnwood.Smtp4dev.Tests.E2E
{
    public class E2ETests_DarkModeRendering : E2ETests
    {
        public E2ETests_DarkModeRendering(ITestOutputHelper output) : base(output)
        {
        }

        [Theory]
        [InlineData(false, false)] // Light UI, Email without dark mode support
        [InlineData(false, true)]  // Light UI, Email with dark mode support  
        [InlineData(true, false)]  // Dark UI, Email without dark mode support
        [InlineData(true, true)]   // Dark UI, Email with dark mode support
        public void CheckDarkModeEmailRendering(bool isDarkMode, bool emailSupportsDarkMode)
        {
            string testName = $"DarkModeRendering_UI{(isDarkMode ? "Dark" : "Light")}_Email{(emailSupportsDarkMode ? "WithDark" : "NoDark")}";
            
            RunUITestAsync(testName, async (page, baseUrl, smtpPortNumber) =>
            {
                var testResults = new TestResult { TestName = testName, IsDarkMode = isDarkMode, EmailSupportsDarkMode = emailSupportsDarkMode };
                
                // Navigate to the application
                await page.GotoAsync(baseUrl.ToString());
                var homePage = new HomePage(page);

                // Get the message list
                var messageList = await WaitForAsync(async () => await homePage.GetMessageListAsync());
                Assert.NotNull(messageList);

                // Send test email with distinctive colors
                string messageSubject = $"Test_{testName}_{Guid.NewGuid().ToString()[..8]}";
                await SendTestEmailAsync(smtpPortNumber, messageSubject, emailSupportsDarkMode);

                // Wait for the message to appear
                var grid = await WaitForAsync(() => Task.FromResult(messageList.GetGrid()));
                var messageRow = await WaitForAsync(async () =>
                {
                    var rows = await grid.GetRowsAsync();
                    return rows.FirstOrDefault(r => r.ContainsTextAsync(messageSubject).Result);
                });
                
                Assert.NotNull(messageRow);

                // Set UI dark/light mode
                await SetUIMode(page, isDarkMode);
                
                // Take screenshot of email list with UI mode applied
                testResults.Screenshots.Add(await TakeNamedScreenshotAsync(page, $"{testName}_1_EmailList"));
                
                // Click the message to view it
                await messageRow.ClickAsync();
                await page.WaitForTimeoutAsync(2000); // Wait for message to load

                // Navigate to HTML view
                var messageView = homePage.MessageView;
                await messageView.ClickHtmlTabAsync();
                await messageView.WaitForHtmlFrameAsync();
                
                // Take screenshot of the HTML view
                testResults.Screenshots.Add(await TakeNamedScreenshotAsync(page, $"{testName}_2_HTMLView"));

                // Verify the email colors are correct based on dark mode support and UI mode
                await VerifyEmailColors(page, emailSupportsDarkMode, isDarkMode);
                testResults.Passed = true;
                
                // Take final verification screenshot
                testResults.Screenshots.Add(await TakeNamedScreenshotAsync(page, $"{testName}_3_Final"));

                // Generate HTML report
                await GenerateTestReport(testResults);

            }, new UITestOptions());
        }

        private async Task SendTestEmailAsync(int smtpPortNumber, string subject, bool supportsDarkMode)
        {
            using var smtpClient = new SmtpClient();
            smtpClient.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            smtpClient.ServerCertificateValidationCallback = GetCertvalidationCallbackHandler();
            smtpClient.CheckCertificateRevocation = false;

            var message = new MimeMessage();
            message.To.Add(MailboxAddress.Parse("user@example.com"));
            message.From.Add(MailboxAddress.Parse("test@example.com"));
            message.Subject = subject;

            string htmlBody = CreateTestEmailHtml(supportsDarkMode);
            message.Body = new TextPart("html") { Text = htmlBody };

            smtpClient.Connect("localhost", smtpPortNumber, SecureSocketOptions.StartTls,
                new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
            smtpClient.Send(message);
            smtpClient.Disconnect(true, new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
        }

        private string CreateTestEmailHtml(bool supportsDarkMode)
        {
            string metaTags = supportsDarkMode ? 
                @"<meta name=""supported-color-schemes"" content=""light dark"">
                  <meta name=""color-scheme"" content=""light dark"">" : "";

            string darkModeCSS = supportsDarkMode ? 
                @"@media (prefers-color-scheme: dark) {
                    .dark-section { background-color: #2d2d2d; color: #ffffff; }
                    .email-content { background-color: #1a1a1a; }
                  }" : "";

            return $@"
<!DOCTYPE html>
<html>
<head>
    {metaTags}
    <style>
        .test-section {{
            padding: 20px;
            margin: 10px;
            border: 2px solid #000;
            font-weight: bold;
            font-size: 16px;
        }}
        .red-section {{ background-color: #FF0000; color: #FFFFFF; }}
        .green-section {{ background-color: #00FF00; color: #000000; }}
        .yellow-section {{ background-color: #FFFF00; color: #000000; }}
        .email-content {{ 
            background-color: #ffffff;
            color: #000000;
            padding: 20px;
        }}
        {darkModeCSS}
    </style>
</head>
<body class=""email-content"">
    <h1>Email {(supportsDarkMode ? "WITH" : "WITHOUT")} Dark Mode Support</h1>
    <p>This email {(supportsDarkMode ? "includes proper dark mode support with the meta tags and CSS media queries for dark mode." : "does NOT have dark mode meta tags or CSS media queries for dark mode.")}</p>
    
    <div class=""test-section red-section"">
        Red Section - This should be RED in light mode
    </div>
    
    <div class=""test-section green-section"">
        Green Section - This should be GREEN in light mode
    </div>
    
    <div class=""test-section yellow-section"">
        Yellow Section - This should be YELLOW in light mode
    </div>
    
    {(supportsDarkMode ? @"<div class=""test-section dark-section"">
        Dark Mode Section - This should have dark background in dark mode
    </div>" : "")}
    
    <p><strong>Expected behavior:</strong></p>
    <ul>
        <li><strong>Light UI:</strong> Both email types display with their original colors</li>
        <li><strong>Dark UI:</strong> 
            <ul>
                <li>Emails WITHOUT dark support are inverted (red→cyan, green→magenta, yellow→purple)</li>
                <li>Emails WITH dark support maintain their intended dark mode appearance</li>
            </ul>
        </li>
    </ul>
</body>
</html>";
        }

        private async Task SetUIMode(IPage page, bool isDarkMode)
        {
            var homePage = new HomePage(page);
            await homePage.SetDarkModeAsync(isDarkMode);
        }

        private async Task VerifyEmailColors(IPage page, bool emailSupportsDarkMode, bool isDarkMode)
        {
            // Wait for iframe to be ready
            await page.WaitForSelectorAsync("iframe.htmlview", new PageWaitForSelectorOptions { Timeout = 10000 });
            
            // Get the iframe content frame
            var iframe = page.FrameLocator("iframe.htmlview");
            
            // Wait for the test sections to be available in the iframe
            await iframe.Locator(".red-section").WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
            
            // Get the computed background colors of the test sections
            var redBgColor = await iframe.Locator(".red-section").EvaluateAsync<string>("el => getComputedStyle(el).backgroundColor");
            var greenBgColor = await iframe.Locator(".green-section").EvaluateAsync<string>("el => getComputedStyle(el).backgroundColor");
            var yellowBgColor = await iframe.Locator(".yellow-section").EvaluateAsync<string>("el => getComputedStyle(el).backgroundColor");
            
            // Define expected colors (RGB format)
            var originalRed = "rgb(255, 0, 0)";
            var originalGreen = "rgb(0, 255, 0)";
            var originalYellow = "rgb(255, 255, 0)";
            
            var invertedRed = "rgb(0, 255, 255)";    // Cyan
            var invertedGreen = "rgb(255, 0, 255)";  // Magenta 
            var invertedYellow = "rgb(0, 0, 255)";   // Blue/Purple
            
            // Determine if colors should be inverted
            bool shouldInvert = isDarkMode && !emailSupportsDarkMode;
            
            if (shouldInvert)
            {
                // Dark UI + Email without dark support: colors should be inverted
                Assert.Equal(invertedRed, redBgColor);
                Assert.Equal(invertedGreen, greenBgColor);
                Assert.Equal(invertedYellow, yellowBgColor);
            }
            else
            {
                // Light UI OR Dark UI + Email with dark support: colors should be original
                Assert.Equal(originalRed, redBgColor);
                Assert.Equal(originalGreen, greenBgColor);
                Assert.Equal(originalYellow, yellowBgColor);
            }
        }

        private async Task<string> TakeNamedScreenshotAsync(IPage page, string name)
        {
            string baseReportDir = Environment.GetEnvironmentVariable("PLAYWRIGHT_HTML_REPORT") ?? Path.GetTempPath();
            string screenshotDir = Path.Combine(baseReportDir, "screenshots");
            string screenshotPath = Path.Combine(screenshotDir, $"{name}.png");
            
            Directory.CreateDirectory(screenshotDir);
            
            await page.ScreenshotAsync(new PageScreenshotOptions 
            { 
                Path = screenshotPath, 
                FullPage = true 
            });
            
            // Also log for Azure DevOps artifact collection
            Console.WriteLine($"##vso[artifact.upload containerfolder=e2e-screenshots;artifactname={name}]{screenshotPath}");
            
            return screenshotPath;
        }

        private async Task GenerateTestReport(TestResult result)
        {
            string baseReportDir = Environment.GetEnvironmentVariable("PLAYWRIGHT_HTML_REPORT") ?? Path.GetTempPath();
            string reportPath = Path.Combine(baseReportDir, $"{result.TestName}_report.html");
            
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Dark Mode Rendering Test: {result.TestName}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .test-info {{ background: #f0f0f0; padding: 15px; border-radius: 5px; margin-bottom: 20px; }}
        .screenshot {{ margin: 10px 0; text-align: center; }}
        .screenshot img {{ max-width: 800px; border: 1px solid #ccc; }}
        .passed {{ color: green; }} .failed {{ color: red; }}
    </style>
</head>
<body>
    <h1>Dark Mode Email Rendering Test</h1>
    <div class=""test-info"">
        <h2>Test: {result.TestName}</h2>
        <p><strong>UI Mode:</strong> {(result.IsDarkMode ? "Dark" : "Light")}</p>
        <p><strong>Email Dark Mode Support:</strong> {(result.EmailSupportsDarkMode ? "Yes" : "No")}</p>
        <p><strong>Status:</strong> <span class=""{(result.Passed ? "passed" : "failed")}"">{(result.Passed ? "PASSED" : "FAILED")}</span></p>
        
        <h3>Expected Behavior:</h3>
        <ul>
            <li><strong>Light UI:</strong> Both email types display with original colors</li>
            <li><strong>Dark UI:</strong>
                <ul>
                    <li>Emails WITHOUT dark support: colors are inverted (red→cyan, green→magenta, yellow→purple)</li>
                    <li>Emails WITH dark support: maintain intended dark mode appearance (NOT inverted)</li>
                </ul>
            </li>
        </ul>
    </div>
    
    <h2>Screenshots</h2>";

            foreach (var screenshot in result.Screenshots)
            {
                var fileName = Path.GetFileName(screenshot);
                var relativePath = $"screenshots/{fileName}";
                html += $@"
    <div class=""screenshot"">
        <h3>{fileName}</h3>
        <img src=""{relativePath}"" alt=""{fileName}"" />
    </div>";
            }

            html += @"
</body>
</html>";

            await File.WriteAllTextAsync(reportPath, html);
        }

        private class TestResult
        {
            public string TestName { get; set; }
            public bool IsDarkMode { get; set; }
            public bool EmailSupportsDarkMode { get; set; }
            public bool Passed { get; set; }
            public List<string> Screenshots { get; set; } = new List<string>();
        }

        private static RemoteCertificateValidationCallback GetCertvalidationCallbackHandler()
        {
            return (s, c, h, e) => 
            {
                var cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(c.GetRawCertData());
                return cert.Subject.Contains("localhost");
            };
        }

        private async Task<T> WaitForAsync<T>(Func<Task<T>> findElement) where T : class
        {
            T result = null;
            var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));

            while (result == null && !timeout.IsCancellationRequested)
            {
                try
                {
                    result = await findElement();
                }
                catch
                {
                    result = null;
                }
                
                if (result == null)
                {
                    await Task.Delay(100, timeout.Token);
                }
            }

            Assert.NotNull(result);
            return result;
        }

        class UITestOptions : E2ETestOptions
        {
        }

        private void RunUITestAsync(string testName, Func<IPage, Uri, int, Task> uitest, UITestOptions options = null)
        {
            options ??= new UITestOptions();

            RunE2ETest(context =>
            {
                RunPlaywrightTestAsync(testName, uitest, context).GetAwaiter().GetResult();
            }, options);
        }

        private async Task RunPlaywrightTestAsync(string testName, Func<IPage, Uri, int, Task> uitest, E2ETestContext context)
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = !System.Diagnostics.Debugger.IsAttached,
                Timeout = 60000
            });

            var page = await browser.NewPageAsync();
            
            page.SetDefaultTimeout(60000);
            page.SetDefaultNavigationTimeout(60000);
            
            try
            {
                await uitest(page, context.BaseUrl, context.SmtpPortNumber);
            }
            catch (Exception)
            {
                // Take failure screenshot
                var failureScreenshot = await TakeNamedScreenshotAsync(page, $"{testName}_FAILURE");
                
                // Generate failure report
                var failureResult = new TestResult 
                { 
                    TestName = testName, 
                    Passed = false,
                    Screenshots = { failureScreenshot }
                };
                await GenerateTestReport(failureResult);
                
                throw;
            }
        }
    }
}