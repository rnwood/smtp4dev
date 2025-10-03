using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Playwright;
using MimeKit;
using Rnwood.Smtp4dev.Tests.E2E.WebUI.PageModel;
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

namespace Rnwood.Smtp4dev.Tests.E2E.WebUI
{
    [Collection("E2ETests")]
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
                
                // Click the message to view it
                await messageRow.ClickAsync();
                await page.WaitForTimeoutAsync(2000); // Wait for message to load

                // Navigate to HTML view
                var messageView = homePage.MessageView;
                await messageView.ClickHtmlTabAsync();
                await messageView.WaitForHtmlFrameAsync();

                // Verify the email colors are correct based on dark mode support and UI mode
                await VerifyEmailColors(page, emailSupportsDarkMode, isDarkMode);

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
                    .email-content { background-color: #1a1a1a; color: #ffffff;}
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
    
    <div class=""test-section red-section"" style=""background-color: #FF0000; color: #FFFFFF; padding: 20px; margin: 10px; border: 2px solid #000; font-weight: bold; font-size: 16px;"">
        Red Section - This should be RED in light mode
    </div>
    
    <div class=""test-section green-section"" style=""background-color: #00FF00; color: #000000; padding: 20px; margin: 10px; border: 2px solid #000; font-weight: bold; font-size: 16px;"">
        Green Section - This should be GREEN in light mode
    </div>
    
    <div class=""test-section yellow-section"" style=""background-color: #FFFF00; color: #000000; padding: 20px; margin: 10px; border: 2px solid #000; font-weight: bold; font-size: 16px;"">
        Yellow Section - This should be YELLOW in light mode
    </div>
        
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
            // Set browser color scheme emulation to match UI dark mode state
            // This is essential for CSS @media (prefers-color-scheme: dark) to work in tests
            await page.EmulateMediaAsync(new PageEmulateMediaOptions
            {
                ColorScheme = isDarkMode ? ColorScheme.Dark : ColorScheme.Light
            });
            
            var homePage = new HomePage(page);
            await homePage.SetDarkModeAsync(isDarkMode);
            
            // Give time for both UI change and browser emulation to take effect
            await page.WaitForTimeoutAsync(1000);
            
            // Set browser color scheme emulation (reduced logging for CI)
        }

        private async Task VerifyEmailColors(IPage page, bool emailSupportsDarkMode, bool isDarkMode)
        {
            // Wait for iframe to be ready
            await page.WaitForSelectorAsync("iframe.htmlview", new PageWaitForSelectorOptions { Timeout = 10000 });
            
            // Get the iframe content frame
            var frameLocator = page.FrameLocator("iframe.htmlview");
            
            // Wait for the test sections to be available in the iframe
            await frameLocator.Locator(".red-section").WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
            
            // Wait for dark mode processing to complete by giving time for Vue reactivity
            await page.WaitForTimeoutAsync(2000);
            
            // DEBUG: Check if our dark mode CSS injection worked by examining iframe content
            if (isDarkMode && emailSupportsDarkMode) {
                var iframeElement = await page.QuerySelectorAsync("iframe.htmlview");
                var iframeContent = await iframeElement.EvaluateAsync<string>(@"
                    (iframe) => {
                        const doc = iframe.contentDocument;
                        if (!doc) return 'No content document';
                        
                        // Check if our injected style exists
                        const injectedStyles = doc.querySelectorAll('style');
                        let foundInjectedStyle = false;
                        for (const style of injectedStyles) {
                            if (style.textContent && style.textContent.includes('smtp4dev')) {
                                foundInjectedStyle = true;
                                break;
                            }
                        }
                        
                        // Check computed color-scheme
                        const htmlElement = doc.documentElement;
                        const computedStyle = getComputedStyle(htmlElement);
                        
                        return JSON.stringify({
                            injectedStyleFound: foundInjectedStyle,
                            computedColorScheme: computedStyle.colorScheme,
                            htmlInnerHTML: doc.documentElement.innerHTML.substring(0, 500)
                        }, null, 2);
                    }
                ");
                // Debug iframe content (comment out for CI to reduce output)
                // Console.WriteLine($"🔍 DEBUG iframe content analysis: {iframeContent}");
            }
            
            // Get the iframe element for filter inspection
            var iframe = await page.QuerySelectorAsync("iframe.htmlview");
            Assert.NotNull(iframe);
            
            // Check the iframe's computed filter style using getComputedStyle
            var iframeFilter = await iframe.EvaluateAsync<string>("el => getComputedStyle(el).filter");
            
            // Test scenario logging (reduced for CI)
            // Console.WriteLine($"📋 Test scenario: UI Mode = {(isDarkMode ? "Dark" : "Light")}, Email Dark Mode Support = {emailSupportsDarkMode}");
            // Console.WriteLine($"🎨 Iframe computed filter: '{iframeFilter}'");
            
            // Determine if inversion should be applied based on UI mode and email dark mode support
            bool shouldInvert = isDarkMode && !emailSupportsDarkMode;
            bool hasInvertFilter = iframeFilter.Contains("invert");
            
            // Console.WriteLine($"📊 Filter verification: expected invert = {shouldInvert}, actual invert = {hasInvertFilter}");
            
            // Verify the iframe has the correct CSS filter
            if (shouldInvert)
            {
                Assert.True(hasInvertFilter, $"Dark UI + Email without dark support should have invert filter. Computed filter: {iframeFilter}");
                Console.WriteLine("✅ Invert filter correctly applied");
            }
            else
            {
                Assert.False(hasInvertFilter, $"Light UI OR Dark UI + Email with dark support should NOT have invert filter. Computed filter: {iframeFilter}");
                Console.WriteLine("✅ Invert filter correctly NOT applied");
            }
            
            // Now check actual background colors of email sections
            // IMPORTANT: CSS filter inversion happens at the iframe container level,
            // so computed styles from inside the iframe always return the original values,
            // even when the colors are visually inverted for the user.
            await VerifyEmailSectionColors(frameLocator, shouldInvert, emailSupportsDarkMode, isDarkMode);
            
            Console.WriteLine($"✅ Dark mode behavior and color verification completed successfully");
        }
        
        private async Task VerifyEmailSectionColors(IFrameLocator frameLocator, bool shouldInvert, bool emailSupportsDarkMode, bool isDarkMode)
        {
            // Determine if dark mode media query colors should be active
            bool expectDarkModeMediaQueryColors = emailSupportsDarkMode && isDarkMode && !shouldInvert;
            
            // Console.WriteLine($"🔍 Dark mode media query colors expected: {expectDarkModeMediaQueryColors}");
            
            // Define expected colors - different sets based on whether dark mode media query should be active
            var baseColors = new Dictionary<string, (string original, string inverted)>
            {
                ["overall-background"] = ("rgb(255, 255, 255)", "rgb(0, 0, 0)"), // White -> Black
                ["red-section"] = ("rgb(255, 0, 0)", "rgb(0, 255, 255)"),         // Red -> Cyan  
                ["green-section"] = ("rgb(0, 255, 0)", "rgb(255, 0, 255)"),       // Green -> Magenta
                ["yellow-section"] = ("rgb(255, 255, 0)", "rgb(0, 0, 255)")       // Yellow -> Blue
            };
            
            // For emails with dark mode support in dark UI, some colors should be different due to media query
            var darkModeMediaQueryColors = new Dictionary<string, string>
            {
                ["overall-background"] = "rgb(26, 26, 26)", // #1a1a1a from dark mode CSS
                ["dark-section"] = "rgb(45, 45, 45)"        // #2d2d2d from dark mode CSS
            };
            
            // Check overall background (body or email-content)
            var overallBackground = await GetComputedBackgroundColor(frameLocator, ".email-content");
            if (string.IsNullOrEmpty(overallBackground))
            {
                overallBackground = await GetComputedBackgroundColor(frameLocator, "body");
            }
            
            // Console.WriteLine($"🎨 Overall background color: {overallBackground}");
            
            if (expectDarkModeMediaQueryColors && darkModeMediaQueryColors.ContainsKey("overall-background"))
            {
                // For emails with dark mode support in dark UI, expect the dark mode media query background color
                var expectedDarkBg = darkModeMediaQueryColors["overall-background"];
                Console.WriteLine($"🔍 Expecting dark mode media query background: {expectedDarkBg}");
                VerifyExactColorMatch("overall-background (dark mode media query)", overallBackground, expectedDarkBg);
            }
            else
            {
                // Use regular color matching logic for other cases
                VerifyColorMatch("overall-background", overallBackground, baseColors["overall-background"], shouldInvert);
            }
            
            // Check red section (always follows base color rules - no dark mode override)
            var redBackground = await GetComputedBackgroundColor(frameLocator, ".red-section");
            Console.WriteLine($"🎨 Red section background color: {redBackground}");
            VerifyColorMatch("red-section", redBackground, baseColors["red-section"], shouldInvert);
            
            // Check green section (always follows base color rules - no dark mode override) 
            var greenBackground = await GetComputedBackgroundColor(frameLocator, ".green-section");
            Console.WriteLine($"🎨 Green section background color: {greenBackground}");
            VerifyColorMatch("green-section", greenBackground, baseColors["green-section"], shouldInvert);
            
            // Check yellow section (always follows base color rules - no dark mode override)
            var yellowBackground = await GetComputedBackgroundColor(frameLocator, ".yellow-section");
            Console.WriteLine($"🎨 Yellow section background color: {yellowBackground}");
            VerifyColorMatch("yellow-section", yellowBackground, baseColors["yellow-section"], shouldInvert);
            
            // Check dark section if it exists (only in emails with dark mode support)
            try
            {
                var darkSectionExists = await frameLocator.Locator(".dark-section").CountAsync() > 0;
                if (darkSectionExists)
                {
                    var darkBackground = await GetComputedBackgroundColor(frameLocator, ".dark-section");
                    Console.WriteLine($"🎨 Dark section background color: {darkBackground}");
                    
                    if (expectDarkModeMediaQueryColors)
                    {
                        // For emails with dark mode support in dark UI, expect the dark mode media query color
                        var expectedDarkSectionBg = darkModeMediaQueryColors["dark-section"];
                        Console.WriteLine($"🔍 Expecting dark mode media query dark section: {expectedDarkSectionBg}");
                        VerifyExactColorMatch("dark-section (dark mode media query)", darkBackground, expectedDarkSectionBg);
                    }
                    else
                    {
                        // Use regular color matching logic - dark section should be dark gray originally
                        var darkSectionColors = ("rgb(45, 45, 45)", "rgb(210, 210, 210)"); // Dark gray -> Light gray when inverted
                        VerifyColorMatch("dark-section", darkBackground, darkSectionColors, shouldInvert);
                    }
                }
                else if (emailSupportsDarkMode)
                {
                    Console.WriteLine($"⚠️ Dark section expected but not found for email with dark mode support");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ℹ️ Dark section not found or not accessible: {ex.Message}");
            }
        }
        
        private async Task<string> GetComputedBackgroundColor(IFrameLocator frameLocator, string selector)
        {
            try
            {
                return await frameLocator.Locator(selector).EvaluateAsync<string>("el => getComputedStyle(el).backgroundColor");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Could not get background color for {selector}: {ex.Message}");
                return "";
            }
        }
        
        private void VerifyExactColorMatch(string sectionName, string actualColor, string expectedColor)
        {
            Console.WriteLine($"🔍 {sectionName}: expected={expectedColor}, actual={actualColor}");
            
            // Normalize color values for comparison (handle different RGB formats)
            var normalizedActual = NormalizeRgbColor(actualColor);
            var normalizedExpected = NormalizeRgbColor(expectedColor);
            
            if (normalizedActual == normalizedExpected)
            {
                Console.WriteLine($"✅ {sectionName} color matches expected value");
            }
            else
            {
                Assert.Fail($"{sectionName} color mismatch - got {actualColor} but expected {expectedColor}");
            }
        }
        
        private void VerifyColorMatch(string sectionName, string actualColor, (string original, string inverted) expectedColors, bool shouldInvert)
        {
            // IMPORTANT: When CSS filter: invert() is applied to the iframe container,
            // the colors are visually inverted for the user, but computed styles from 
            // inside the iframe still return the ORIGINAL values.
            // So we always expect the original colors from inside the iframe.
            var expectedColor = expectedColors.original;
            
            Console.WriteLine($"🔍 {sectionName}: expected={expectedColor}, actual={actualColor}, shouldInvert={shouldInvert}");
            Console.WriteLine($"ℹ️  Note: CSS filter inversion happens at container level, so computed styles inside iframe always return original values");
            
            // Normalize color values for comparison (handle different RGB formats)
            var normalizedActual = NormalizeRgbColor(actualColor);
            var normalizedExpected = NormalizeRgbColor(expectedColor);
            
            if (normalizedActual == normalizedExpected)
            {
                Console.WriteLine($"✅ {sectionName} color matches expected value (original color from inside iframe)");
            }
            else
            {
                Assert.Fail($"{sectionName} color mismatch - got {actualColor} but expected {expectedColor}. " +
                                 $"Note: We expect original colors from inside iframe even when CSS filter is applied to container.");
            }
        }
        
        private string NormalizeRgbColor(string color)
        {
            if (string.IsNullOrEmpty(color)) return "";
            
            // Handle rgba to rgb conversion and normalize spacing
            return color.Trim()
                       .Replace("rgba(", "rgb(")
                       .Replace(", 1)", ")")  // Remove alpha channel if it's 1
                       .Replace(" ", "")      // Remove spaces
                       .ToLowerInvariant();
        }

        private string GetTraceDirectory()
        {
            // Use the same directory structure as the existing Playwright HTML report
            string baseReportDir = Environment.GetEnvironmentVariable("PLAYWRIGHT_HTML_REPORT") ?? System.IO.Path.GetTempPath();
            return System.IO.Path.Combine(baseReportDir, "traces");
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

            // Create browser context with tracing enabled
            var browserContext = await browser.NewContextAsync();
            
            // Start tracing for the entire test
            string traceDir = GetTraceDirectory();
            string tracePath = System.IO.Path.Combine(traceDir, $"{testName}.zip");
            System.IO.Directory.CreateDirectory(traceDir);
            
            await browserContext.Tracing.StartAsync(new TracingStartOptions
            {
                Screenshots = true,
                Snapshots = true,
                Sources = true
            });

            var page = await browserContext.NewPageAsync();
            
            page.SetDefaultTimeout(60000);
            page.SetDefaultNavigationTimeout(60000);
            
            try
            {
                await uitest(page, context.BaseUrl, context.SmtpPortNumber);
                
                // Stop tracing with successful completion
                await browserContext.Tracing.StopAsync(new TracingStopOptions
                {
                    Path = tracePath
                });
                
                Console.WriteLine($"Trace saved: {tracePath}");
            }
            catch (Exception)
            {
                // Stop tracing and save on failure
                await browserContext.Tracing.StopAsync(new TracingStopOptions
                {
                    Path = tracePath
                });
                
                Console.WriteLine($"Trace saved on failure: {tracePath}");
                throw;
            }
            finally
            {
                await browserContext.CloseAsync();
            }
        }
    }
}