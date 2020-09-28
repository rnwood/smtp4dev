using MailKit.Net.Smtp;
using MailKit.Security;
using Medallion.Shell;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using MimeKit;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.PageObjects;
using Rnwood.Smtp4dev.Tests.E2E.PageModel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Rnwood.Smtp4dev.Tests.E2E
{
    public class E2ETests
    {
        private ITestOutputHelper output;

        public E2ETests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Theory]
        [InlineData("/", false)]
        [InlineData("/", true)]
        [InlineData("/smtp4dev", true)]
        public void CheckMessageIsReceivedAndDisplayed(string basePath, bool inMemoryDb)
        {
            RunE2ETest((browser, baseUrl, smtpPortNumber) =>
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
                    message.To.Add(new MailboxAddress("to@to.com"));
                    message.From.Add(new MailboxAddress("from@from.com"));

                    message.Subject = messageSubject;
                    message.Body = new TextPart()
                    {
                        Text = "Body of end to end test"
                    };

                    smtpClient.Connect("localhost", smtpPortNumber, SecureSocketOptions.StartTls, new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
                    smtpClient.Send(message);
                    smtpClient.Disconnect(true, new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
                }

                HomePage.Grid.GridRow messageRow = WaitFor(() => messageList.Grid?.Rows?.SingleOrDefault());
                Assert.NotNull(messageRow);

                Assert.Contains(messageRow.Cells, c => c.Text.Contains(messageSubject));
            }, inMemoryDb, basePath);
        }

        [Fact]
        public void CheckUTF8MessageIsReceivedAndDisplayed()
        {
            RunE2ETest((browser, baseUrl, smtpPortNumber) =>
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

                    message.To.Add(new MailboxAddress("ñఛ@example.com"));
                    message.From.Add(new MailboxAddress("ñఛ@example.com"));

                    message.Subject = messageSubject;
                    message.Body = new TextPart()
                    {
                        Text = "Body of end to end test"
                    };

                    smtpClient.Connect("localhost", smtpPortNumber, SecureSocketOptions.StartTls, new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);

                    FormatOptions formatOptions = FormatOptions.Default.Clone();
                    formatOptions.International = true;
                    smtpClient.Send(formatOptions, message);
                    smtpClient.Disconnect(true, new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
                }

                HomePage.Grid.GridRow messageRow = WaitFor(() => messageList.Grid?.Rows?.SingleOrDefault());
                Assert.NotNull(messageRow);

                Assert.Contains(messageRow.Cells, c => c.Text.Contains("ñఛ@example.com"));
            }, true);
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


        private void RunE2ETest(Action<IWebDriver, Uri, int> test, bool inMemoryDb, string basePath = null)
        {
            string workingDir = Environment.GetEnvironmentVariable("SMTP4DEV_E2E_WORKINGDIR");
            string mainModule = Environment.GetEnvironmentVariable("SMTP4DEV_E2E_BINARY");

            if (string.IsNullOrEmpty(workingDir))
            {
                workingDir = Path.GetFullPath("../../../../Rnwood.Smtp4dev");
            }

            if (string.IsNullOrEmpty(mainModule))
            {
                mainModule = Path.GetFullPath("../../../../Rnwood.Smtp4dev/bin/Debug/netcoreapp3.1/Rnwood.Smtp4dev.dll");
            }

            CancellationToken timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60)).Token;
            Thread outputThread = null;


            List<string> args = new List<string> { mainModule, inMemoryDb ? "--db=" : "--recreatedb", "--nousersettings", "--urls=http://*:0", "--smtpport=0", "--tlsmode=StartTls" };
            if (basePath != null)
            {
                args.Add($"--basepath={basePath}");
            }

            using (Command serverProcess = Command.Run("dotnet", args, o => o.DisposeOnExit(false).WorkingDirectory(workingDir).CancellationToken(timeout)))
            {
                try
                {
                    IEnumerator<string> serverOutput = serverProcess.GetOutputAndErrorLines().GetEnumerator();

                    Uri baseUrl = null;
                    int? smtpPortNumber = 0;


                    while ((baseUrl == null || !smtpPortNumber.HasValue) && serverOutput.MoveNext())
                    {
                        string newLine = serverOutput.Current;
                        output.WriteLine(newLine);

                        if (newLine.StartsWith("Now listening on: http://"))
                        {
                            int portNumber = int.Parse(Regex.Replace(newLine, @".*http://[^\s]+:(\d+)", "$1"));
                            baseUrl = new Uri($"http://localhost:{portNumber}{basePath ?? ""}");
                        }

                        if (newLine.StartsWith("SMTP Server is listening on port"))
                        {
                            smtpPortNumber = int.Parse(Regex.Replace(newLine, @"SMTP Server is listening on port (\d+).*", "$1"));
                        }
                    }

                    Assert.False(serverProcess.Process.HasExited, "Server process failed");

                    outputThread = new Thread(() =>
                    {
                        while (serverOutput.MoveNext())
                        {
                            string newLine = serverOutput.Current;
                            output.WriteLine(newLine);
                        }
                    });
                    outputThread.Start();


                    ChromeOptions chromeOptions = new ChromeOptions();
                    if (!Debugger.IsAttached)
                    {
                        chromeOptions.AddArgument("--headless");
                    }

                    using (ChromeDriver browser = new ChromeDriver(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), chromeOptions))
                    {
                        try
                        {
                            test(browser, baseUrl, smtpPortNumber.Value);

                        }
                        finally
                        {
                            browser.Quit();
                        }
                    }

                }
                finally
                {
                    serverProcess.Kill();

                    if (outputThread != null)
                    {
                        if (!outputThread.Join(TimeSpan.FromSeconds(5)))
                        {
                            outputThread.Abort();
                        }
                    }


                }
            }
        }

    }
}

