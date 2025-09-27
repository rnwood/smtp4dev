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
    public class E2ETests_WebUI_CheckUTF8MessageIsReceivedAndDisplayed : E2ETestsWebUIBase
    {
        public E2ETests_WebUI_CheckUTF8MessageIsReceivedAndDisplayed(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void CheckUTF8MessageIsReceivedAndDisplayed()
        {
            RunUITestAsync(nameof(CheckUTF8MessageIsReceivedAndDisplayed), async (page, baseUrl, smtpPortNumber) =>
            {
                await page.GotoAsync(baseUrl.ToString());
                var homePage = new HomePage(page);

                var messageList = await WaitForAsync(async () => await homePage.GetMessageListAsync());
                Assert.NotNull(messageList);

                string messageSubject = Guid.NewGuid().ToString();
                using (SmtpClient smtpClient = new SmtpClient() { })
                {
                    smtpClient.SslProtocols = System.Security.Authentication.SslProtocols.None;
                    smtpClient.ServerCertificateValidationCallback = GetCertvalidationCallbackHandler();
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

                var grid = await WaitForAsync(() => Task.FromResult(messageList.GetGrid()));
                var messageRow = await WaitForAsync(async () =>
                {
                    var rows = await grid.GetRowsAsync();
                    return rows.FirstOrDefault();
                });

                Assert.NotNull(messageRow);
                Assert.True(await messageRow.ContainsTextAsync("ñఛ@example.com"));
            });
        }
    }
}