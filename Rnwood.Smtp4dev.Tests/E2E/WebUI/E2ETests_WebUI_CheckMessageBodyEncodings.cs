using System;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Rnwood.Smtp4dev.Tests.E2E.WebUI.PageModel;
using Xunit;
using Xunit.Abstractions;

namespace Rnwood.Smtp4dev.Tests.E2E.WebUI
{
    [Collection("E2ETests")]
    public class E2ETests_WebUI_CheckMessageBodyEncodings : E2ETestsWebUIBase
    {
        public E2ETests_WebUI_CheckMessageBodyEncodings(ITestOutputHelper output) : base(output)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        [Theory]
        [InlineData("utf-8", "Hello мир 世界")]
        [InlineData("windows-1251", "Привет, мир")]
        [InlineData("windows-1252", "Hello €uro")]
        [InlineData("iso-8859-1", "Bonjour café")]
        public void CheckMessageBodyEncodingIsDisplayed(string charset, string expectedBody)
        {
            RunUITestAsync($"{nameof(CheckMessageBodyEncodingIsDisplayed)}-{charset}", async (page, baseUrl, smtpPortNumber) =>
            {
                await page.GotoAsync(baseUrl.ToString());
                var homePage = new HomePage(page);
                var messageList = await WaitForAsync(async () => await homePage.GetMessageListAsync());
                string messageSubject = $"{charset}-{Guid.NewGuid()}";

                await SendRawMessageAsync(smtpPortNumber, messageSubject, charset, expectedBody);

                var messageRow = await WaitForAsync(async () =>
                {
                    var rows = await messageList.GetGrid().GetRowsAsync();
                    foreach (var row in rows)
                    {
                        if (await row.ContainsTextAsync(messageSubject))
                        {
                            return row;
                        }
                    }

                    return null;
                });

                await messageRow.ClickAsync();
                var messageView = homePage.MessageView;
                await messageView.ClickHtmlTabAsync();
                await messageView.WaitForHtmlFrameAsync();

                Assert.Contains(expectedBody, await messageView.GetHtmlFrameContentAsync());
            });
        }

        private static async Task SendRawMessageAsync(int smtpPortNumber, string subject, string charset, string body)
        {
            using var client = new TcpClient();
            await client.ConnectAsync("localhost", smtpPortNumber);
            using var networkStream = client.GetStream();
            using var reader = new StreamReader(networkStream, Encoding.ASCII, leaveOpen: true);
            await ReadResponseAsync(reader);
            await WriteCommandAsync(networkStream, reader, "EHLO localhost");
            await WriteCommandAsync(networkStream, reader, "STARTTLS");

            using var tlsStream = new SslStream(networkStream, leaveInnerStreamOpen: false,
                (_, _, _, _) => true);
            await tlsStream.AuthenticateAsClientAsync("localhost");
            using var tlsReader = new StreamReader(tlsStream, Encoding.ASCII, leaveOpen: true);

            await WriteCommandAsync(tlsStream, tlsReader, "EHLO localhost");
            await WriteCommandAsync(tlsStream, tlsReader, "MAIL FROM:<from@example.com>");
            await WriteCommandAsync(tlsStream, tlsReader, "RCPT TO:<to@example.com>");
            await WriteCommandAsync(tlsStream, tlsReader, "DATA");

            string headers =
                $"From: from@example.com\r\n" +
                $"To: to@example.com\r\n" +
                $"Subject: {subject}\r\n" +
                $"Content-Type: text/html; charset={charset}\r\n" +
                "Content-Transfer-Encoding: 8bit\r\n\r\n";
            byte[] messageBytes = Encoding.ASCII.GetBytes(headers)
                .Concat(Encoding.GetEncoding(charset).GetBytes($"<html><body>{body}</body></html>\r\n"))
                .Concat(Encoding.ASCII.GetBytes(".\r\n"))
                .ToArray();
            await tlsStream.WriteAsync(messageBytes);
            await ReadResponseAsync(tlsReader);
            await WriteCommandAsync(tlsStream, tlsReader, "QUIT");
        }

        private static async Task WriteCommandAsync(Stream stream, StreamReader reader, string command)
        {
            byte[] commandBytes = Encoding.ASCII.GetBytes(command + "\r\n");
            await stream.WriteAsync(commandBytes);
            await ReadResponseAsync(reader);
        }

        private static async Task ReadResponseAsync(StreamReader reader)
        {
            string response;
            do
            {
                response = await reader.ReadLineAsync() ?? throw new EndOfStreamException();
            }
            while (response.Length > 3 && response[3] == '-');
        }
    }
}
