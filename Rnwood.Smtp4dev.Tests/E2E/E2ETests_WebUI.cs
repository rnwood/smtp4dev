using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Rnwood.Smtp4dev.Tests.E2E.PageModel;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using Xunit;
using Xunit.Abstractions;

namespace Rnwood.Smtp4dev.Tests.E2E
{
    public class E2ETests_WebUI : E2ETests
    {
        public E2ETests_WebUI(ITestOutputHelper output) : base(output)
        {
            new DriverManager().SetUpDriver(new ChromeConfig());
        }

        [Theory]
        [InlineData("/", false)]
        [InlineData("/", true)]
        [InlineData("/smtp4dev", true)]
        public void CheckMessageIsReceivedAndDisplayed(string basePath, bool inMemoryDb)
        {
            RunUITest((browser, baseUrl, smtpPortNumber) =>
            {
                browser.Navigate().GoToUrl(baseUrl);
                HomePage homePage = new HomePage(browser);

                HomePage.MessageListControl messageList = WaitFor(() => homePage.MessageList);
                Assert.NotNull(messageList);

                string messageSubject = Guid.NewGuid().ToString();
                using (SmtpClient smtpClient = new SmtpClient())
                {
                    smtpClient.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
                    smtpClient.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    smtpClient.CheckCertificateRevocation = false;
                    MimeMessage message = new MimeMessage();
                    message.To.Add(MailboxAddress.Parse("to@to.com"));
                    message.From.Add(MailboxAddress.Parse("from@from.com"));

                    message.Subject = messageSubject;
                    message.Body = new TextPart()
                    {
                        Text = "Body of end to end test"
                    };

                    smtpClient.Connect("localhost", smtpPortNumber, SecureSocketOptions.StartTls,
                        new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
                    smtpClient.Send(message);
                    smtpClient.Disconnect(true, new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
                }

                HomePage.Grid.GridRow messageRow = WaitFor(() => messageList.Grid?.Rows?.SingleOrDefault());
                Assert.NotNull(messageRow);

                Assert.Contains(messageRow.Cells, c => c.Text.Contains(messageSubject));
            }, new UITestOptions
            {
                InMemoryDB = inMemoryDb,
                BasePath = basePath
            });
        }

        [Fact]
        public void CheckUTF8MessageIsReceivedAndDisplayed()
        {
            RunUITest((browser, baseUrl, smtpPortNumber) =>
            {
                browser.Navigate().GoToUrl(baseUrl);
                HomePage homePage = new HomePage(browser);

                HomePage.MessageListControl messageList = WaitFor(() => homePage.MessageList);
                Assert.NotNull(messageList);

                string messageSubject = Guid.NewGuid().ToString();
                using (SmtpClient smtpClient = new SmtpClient() { })
                {
                    smtpClient.SslProtocols = System.Security.Authentication.SslProtocols.None;
                    smtpClient.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    smtpClient.CheckCertificateRevocation = false;
                    MimeMessage message = new MimeMessage();

                    message.To.Add(MailboxAddress.Parse("ñఛ@example.com"));
                    message.From.Add(MailboxAddress.Parse("ñఛ@example.com"));

                    message.Subject = messageSubject;
                    message.Body = new TextPart()
                    {
                        Text = "Body of end to end test"
                    };

                    smtpClient.Connect("localhost", smtpPortNumber, SecureSocketOptions.StartTls,
                        new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);

                    FormatOptions formatOptions = FormatOptions.Default.Clone();
                    formatOptions.International = true;
                    smtpClient.Send(formatOptions, message);
                    smtpClient.Disconnect(true, new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
                }

                HomePage.Grid.GridRow messageRow = WaitFor(() => messageList.Grid?.Rows?.SingleOrDefault());
                Assert.NotNull(messageRow);

                Assert.Contains(messageRow.Cells, c => c.Text.Contains("ñఛ@example.com"));
            });
        }

        private T WaitFor<T>(Func<T> findElement) where T : class
        {
            T result = null;

            CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            while (result == null && !timeout.IsCancellationRequested)
            {
                result = findElement();
                Thread.Sleep(100);
            }

            Assert.NotNull(result);

            return result;
        }

        class UITestOptions : E2ETestOptions
        {
        }

        private void RunUITest(Action<IWebDriver, Uri, int> uitest, UITestOptions options = null)
        {
            options ??= new UITestOptions();

            RunE2ETest(context =>
                {
                    ChromeOptions chromeOptions = new ChromeOptions();
                    if (!Debugger.IsAttached)
                    {
                        chromeOptions.AddArgument("--headless");
                    }

                    using var browser = new ChromeDriver(chromeOptions);
                    try
                    {
                        uitest(browser, context.BaseUrl, context.SmtpPortNumber);
                    }
                    finally
                    {
                        browser.Quit();
                    }
                }, options
            );
        }
    }
}