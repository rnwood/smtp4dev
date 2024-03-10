using MimeKit;
using Rnwood.Smtp4dev.Controllers;
using Rnwood.Smtp4dev.Server;
using Rnwood.SmtpServer;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Io;
using MimeKit.Encodings;
using Xunit;

namespace Rnwood.Smtp4dev.Tests.Controllers
{
    public class MessagesControllerTests
    {
        [Fact]
        public async Task GetMessage_ValidMime()
        {
            DateTime startDate = DateTime.Now;
            DbModel.Message testMessage1 = await GetTestMessage1();

            TestMessagesRepository messagesRepository = new TestMessagesRepository(testMessage1);
            MessagesController messagesController = new MessagesController(messagesRepository, null);

            ApiModel.Message result = await messagesController.GetMessage(testMessage1.Id);

            Assert.Null(result.MimeParseError);
            Assert.Equal(testMessage1.Id, result.Id);
            Assert.InRange(result.ReceivedDate, startDate, DateTime.Now);
            Assert.Equal("from@message.com", result.From);
            Assert.Equal("to@message.com", result.To);
            Assert.Equal("to@envelope.com", result.Bcc);
            Assert.Equal("cc@message.com", result.Cc);
            Assert.Equal("subject", result.Subject);

            var allParts = result.Parts.Flatten(p => p.ChildParts).ToList();
            Assert.Equal(6, allParts.Count);


            Assert.All(allParts, p =>
            {
                Assert.Equal(testMessage1.Id, p.MessageId);
                Assert.NotNull(p.Id);
                Assert.NotEqual("", p.Id);
            });

            //All parts have a unique Id
            Assert.Equal(allParts.Count, allParts.Select(p => p.Id).Distinct().Count());

        }

        private const string QPMESSAGE_BODY = "Homines in indicaverunt nam purus quáestionem sentiri unum.";
        private static readonly string testMessage1File1Content = "111";
        private static readonly string testMessage1File2Content = "222";

        private static async Task<DbModel.Message> GetTestMessage1()
        {
            MimeMessage mimeMessage = new MimeMessage();
            mimeMessage.From.Add(InternetAddress.Parse("from@message.com"));
            mimeMessage.To.Add(InternetAddress.Parse("to@message.com"));
            mimeMessage.Cc.Add(InternetAddress.Parse("cc@message.com"));

            mimeMessage.Subject = "subject";
            BodyBuilder bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = "<html>Hi</html>";
            bodyBuilder.TextBody = "Hi";
            bodyBuilder.Attachments.Add("file1", new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testMessage1File1Content)), new ContentType("text", "plain")).ContentId = "file1";
            bodyBuilder.Attachments.Add("file2", new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testMessage1File2Content)), new ContentType("text", "plain"));

            mimeMessage.Body = bodyBuilder.ToMessageBody();

            MemoryMessageBuilder memoryMessageBuilder = new MemoryMessageBuilder();
            memoryMessageBuilder.Recipients.Add("to@envelope.com");
            memoryMessageBuilder.From = "from@envelope.com";
            using (var messageData = await memoryMessageBuilder.WriteData())
            {
                mimeMessage.WriteTo(messageData);
            }
            IMessage message = await memoryMessageBuilder.ToMessage();

            var dbMessage = await new MessageConverter().ConvertAsync(message);
            return dbMessage;
        }

        private static async Task<DbModel.Message> GetTestMessage_QuotedPrintable(Encoding encoding)
        {
            MimeMessage mimeMessage = new MimeMessage();
            mimeMessage.From.Add(InternetAddress.Parse("from@message.com"));
            mimeMessage.To.Add(InternetAddress.Parse("to@message.com"));
            mimeMessage.Cc.Add(InternetAddress.Parse("cc@message.com"));

            mimeMessage.Subject = "subject";
            MimePart body = new MimePart( new ContentType("text", "html"));
            body.ContentTransferEncoding = ContentEncoding.QuotedPrintable;
            body.ContentType.CharsetEncoding = encoding;
            body.Content = new MimeContent(new MemoryStream(encoding.GetBytes(QPMESSAGE_BODY)));
            mimeMessage.Body = body;

            MemoryMessageBuilder memoryMessageBuilder = new MemoryMessageBuilder();
            memoryMessageBuilder.Recipients.Add("to@envelope.com");
            memoryMessageBuilder.From = "from@envelope.com";
            using (var messageData = await memoryMessageBuilder.WriteData())
            {
                mimeMessage.Prepare(EncodingConstraint.SevenBit);
                mimeMessage.WriteTo(messageData);
            }
            IMessage message = await memoryMessageBuilder.ToMessage();

            var dbMessage = await new MessageConverter().ConvertAsync(message);
            return dbMessage;
        }



        [Fact]
        public async Task GetPartContent()
        {
            DbModel.Message testMessage1 = await GetTestMessage1();
            TestMessagesRepository messagesRepository = new TestMessagesRepository(testMessage1);
            MessagesController messagesController = new MessagesController(messagesRepository, null);

            var parts = (await messagesController.GetMessage(testMessage1.Id)).Parts.Flatten(p => p.ChildParts).SelectMany(p => p.Attachments);

            var part = parts.First(p => p.FileName == "file2");

            var result = await messagesController.GetPartContent(testMessage1.Id, part.Id);
            var stringResult = await new StreamReader(result.FileStream, Encoding.UTF8).ReadToEndAsync();

            Assert.Equal(testMessage1File2Content, stringResult);
        }

        [Theory]
        [InlineData("utf-8")]
        [InlineData("iso-8859-1")]
        public async Task GetMessageSource_QPMessage_ReturnsNotHeadersDecodedContent(string encodingName)
        {
            DbModel.Message testMessage2 = await GetTestMessage_QuotedPrintable(Encoding.GetEncoding(encodingName));
            TestMessagesRepository messagesRepository = new TestMessagesRepository(testMessage2);
            MessagesController messagesController = new MessagesController(messagesRepository, null);


            string result = await messagesController.GetMessageSource(testMessage2.Id);
            Assert.DoesNotContain("From: from@message.com", result);
            Assert.Equal(QPMESSAGE_BODY, result);
        }

        [Theory]
        [InlineData("utf-8")]
        [InlineData("iso-8859-1")]
        public async Task GetMessageRaw_QPMessage_ReturnsHeadersAndQPContent(string encodingName)
        {
            var encoding = Encoding.GetEncoding(encodingName);
            DbModel.Message testMessage2 = await GetTestMessage_QuotedPrintable(encoding);
            TestMessagesRepository messagesRepository = new TestMessagesRepository(testMessage2);
            MessagesController messagesController = new MessagesController(messagesRepository, null);


            string result = await messagesController.GetMessageSourceRaw(testMessage2.Id);
            Assert.Contains("From: from@message.com", result);

            QuotedPrintableEncoder e = new QuotedPrintableEncoder();
            var bytes = encoding.GetBytes(QPMESSAGE_BODY);
            byte[] output = new byte[e.EstimateOutputLength(bytes.Length)];
            int outputLen = e.Encode(bytes, 0, bytes.Length, output);
            string qpResult = Encoding.ASCII.GetString(output, 0, outputLen);
            
            Assert.Contains(qpResult, result);
        }
    }

}
