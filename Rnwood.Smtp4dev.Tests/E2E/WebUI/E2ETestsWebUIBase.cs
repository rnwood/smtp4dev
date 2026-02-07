using Microsoft.Playwright;
using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Rnwood.Smtp4dev.Tests.E2E.WebUI
{
    public abstract class E2ETestsWebUIBase : E2ETests
    {
        protected E2ETestsWebUIBase(ITestOutputHelper output) : base(output)
        {
        }

        protected class UITestOptions : E2ETestOptions
        {
        }

        protected void RunUITestAsync(string testName, Func<IPage, Uri, int, Task> uitest, UITestOptions options = null)
        {
            options ??= new UITestOptions();

            RunE2ETest(context =>
            {
                RunPlaywrightTestAsync(testName, uitest, context).GetAwaiter().GetResult();
            }, options);
        }

        protected async Task RunPlaywrightTestAsync(string testName, Func<IPage, Uri, int, Task> uitest, E2ETestContext context)
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = !System.Diagnostics.Debugger.IsAttached,
                // Increase timeout for CI environments
                Timeout = 60000 // 60 seconds for browser launch
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
            
            // Set longer timeouts for CI environments
            page.SetDefaultTimeout(60000); // 60 seconds for page operations
            page.SetDefaultNavigationTimeout(60000); // 60 seconds for navigation
            
            try
            {
                await uitest(page, context.BaseUrl, context.SmtpPortNumber);
                
                // Stop tracing with successful completion
                await browserContext.Tracing.StopAsync(new TracingStopOptions
                {
                    Path = tracePath
                });
                
                output.WriteLine($"Trace saved: {tracePath}");
            }
            catch (Exception ex)
            {
                // Capture page structure dump on failure
                try
                {
                    output.WriteLine($"Test failed with error: {ex.Message}");
                    
                    // Capture accessibility snapshot
                    string snapshotPath = System.IO.Path.Combine(traceDir, $"{testName}_snapshot.txt");
                    var snapshot = await page.Locator("body").AriaSnapshotAsync();
                    await System.IO.File.WriteAllTextAsync(snapshotPath, snapshot);
                    output.WriteLine($"Accessibility snapshot saved: {snapshotPath}");
                    
                    // Capture screenshot
                    string screenshotPath = System.IO.Path.Combine(traceDir, $"{testName}_failure.png");
                    await page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath, FullPage = true });
                    output.WriteLine($"Screenshot saved: {screenshotPath}");
                    
                    // Capture HTML content
                    string htmlPath = System.IO.Path.Combine(traceDir, $"{testName}_content.html");
                    var htmlContent = await page.ContentAsync();
                    await System.IO.File.WriteAllTextAsync(htmlPath, htmlContent);
                    output.WriteLine($"HTML content saved: {htmlPath}");
                }
                catch (Exception dumpEx)
                {
                    output.WriteLine($"Failed to capture page structure dump: {dumpEx.Message}");
                }
                
                // Stop tracing and save on failure
                await browserContext.Tracing.StopAsync(new TracingStopOptions
                {
                    Path = tracePath
                });
                
                output.WriteLine($"Trace saved on failure: {tracePath}");
                throw;
            }
            finally
            {
                await browserContext.CloseAsync();
            }
        }
        
        protected string GetTraceDirectory()
        {
            // Use the same directory structure as the existing Playwright HTML report
            string baseReportDir = Environment.GetEnvironmentVariable("PLAYWRIGHT_HTML_REPORT") ?? System.IO.Path.GetTempPath();
            string traceDir = System.IO.Path.Combine(baseReportDir, "traces");
            
            // Debug output for CI troubleshooting
            output.WriteLine($"PLAYWRIGHT_HTML_REPORT environment variable: {Environment.GetEnvironmentVariable("PLAYWRIGHT_HTML_REPORT")} ");
            output.WriteLine($"Base report directory: {baseReportDir}");
            output.WriteLine($"Trace directory: {traceDir}");
            
            return traceDir;
        }

        protected async Task<T> WaitForAsync<T>(Func<Task<T>> getValue, int timeoutSeconds = 60) where T : class
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            T result = null;

            while (result == null && !timeout.Token.IsCancellationRequested)
            {
                try
                {
                    result = await getValue();
                }
                catch
                {
                    result = null;
                }
                
                if (result == null)
                {
                    try
                    {
                        await Task.Delay(100, timeout.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // Timeout expired, exit the loop
                        break;
                    }
                }
            }

            Assert.NotNull(result);
            return result;
        }

        protected static RemoteCertificateValidationCallback GetCertvalidationCallbackHandler()
        {
            return (s, c, h, e) => 
            {
                var cert = new X509Certificate2(c.GetRawCertData());
                // Simple validation - check if certificate subject contains localhost
                return cert.Subject.Contains("localhost");
            };
        }
    }
}