using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Rnwood.Smtp4dev.Tests.E2E.WebUI.PageModel;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Rnwood.Smtp4dev.Tests.E2E.WebUI
{
    [Collection("E2ETests")]
    public class E2ETests_WebUI_CheckMessageIsReceivedAndDisplayed : E2ETestsWebUIBase
    {
        public E2ETests_WebUI_CheckMessageIsReceivedAndDisplayed(ITestOutputHelper output) : base(output)
        {
        }

        [Theory]
        [InlineData("", "", false)]
        [InlineData("", "/", true)]
        [InlineData("/smtp4dev", "/smtp4dev", true)]
        [InlineData("/smtp4dev", "/smtp4dev/", true)]
        [InlineData("/smtp4dev", "", true)]
        [InlineData("/smtp4dev", "/", true)]
        public void CheckMessageIsReceivedAndDisplayed(string basePath, string testPath, bool inMemoryDb)
        {
            RunUITestAsync($"{nameof(CheckMessageIsReceivedAndDisplayed)}-{basePath}-{inMemoryDb}", async (page, baseUrl, smtpPortNumber) =>
            {
                await page.GotoAsync(baseUrl.ToString());
                var homePage = new HomePage(page);

                var messageList = await WaitForAsync(async () => await homePage.GetMessageListAsync());
                Assert.NotNull(messageList);

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
                    message.Body = new TextPart()
                    {
                        Text = "Body of end to end test"
                    };

                    smtpClient.Connect("localhost", smtpPortNumber, SecureSocketOptions.StartTls,
                        new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
                    smtpClient.Send(message);
                    smtpClient.Disconnect(true, new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
                }

                var grid = await WaitForAsync(() => Task.FromResult(messageList.GetGrid()));
                var messageRow = await WaitForAsync(async () =>
                {
                    var rows = await grid.GetRowsAsync();
                    return rows.FirstOrDefault();
                });

                Assert.NotNull(messageRow);
                Assert.True(await messageRow.ContainsTextAsync(messageSubject));
            }, new UITestOptions
            {
                InMemoryDB = inMemoryDb,
                BasePath = basePath,
                TestPath = testPath
            });
        }
    }
}