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
using Microsoft.AspNetCore.Mvc;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MimeKit.Encodings;
using NSubstitute;
using Rnwood.Smtp4dev.Data;
using Rnwood.Smtp4dev.Hubs;
using Rnwood.Smtp4dev.Tests.DBMigrations.Helpers;
using Xunit;
using Rnwood.Smtp4dev.Server.Settings;

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
            Assert.Equal(new[]{"to@message.com"}, result.To);
            Assert.Equal(new[]{"to@envelope.com"}, result.Bcc);
            Assert.Equal(new[]{"cc@message.com"}, result.Cc);
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
        private static readonly string message1HtmlBody = "<html>Hi</html>";
        private static readonly string message1TextBody = "Hi";

        private static async Task<DbModel.Message> GetTestMessage1(bool includeHtmlBody=true, bool includeTextBody=true)
        {
            MimeMessage mimeMessage = new MimeMessage();
            mimeMessage.From.Add(InternetAddress.Parse("from@message.com"));
            mimeMessage.To.Add(InternetAddress.Parse("to@message.com"));
            mimeMessage.Cc.Add(InternetAddress.Parse("cc@message.com"));

            mimeMessage.Subject = "subject";
            BodyBuilder bodyBuilder = new BodyBuilder();
            if (includeHtmlBody)
            {
                bodyBuilder.HtmlBody = message1HtmlBody;
            }

            if (includeTextBody)
            {
                bodyBuilder.TextBody = message1TextBody;
            }

            bodyBuilder.Attachments.Add("file1", new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testMessage1File1Content)), new ContentType("text", "plain")).ContentId = "file1";
            bodyBuilder.Attachments.Add("file2", new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testMessage1File2Content)), new ContentType("text", "plain"));

            mimeMessage.Body = bodyBuilder.ToMessageBody();

            MemoryMessageBuilder memoryMessageBuilder = new MemoryMessageBuilder();
            memoryMessageBuilder.Recipients.Add("to@envelope.com");
            memoryMessageBuilder.From = "from@envelope.com";
            memoryMessageBuilder.ReceivedDate = DateTime.Now;
            using (var messageData = await memoryMessageBuilder.WriteData())
            {
                mimeMessage.WriteTo(messageData);
            }

            IMessage message = await memoryMessageBuilder.ToMessage();

            var dbMessage = await new MessageConverter().ConvertAsync(message, ["to@envelope.com"]);
            return dbMessage;
        }

        private static async Task<DbModel.Message> GetTestMessage(string subject, string from = "from@from.com", string to = "to@to.com")
        {
            MimeMessage mimeMessage = new MimeMessage();
            mimeMessage.From.Add(InternetAddress.Parse(from));
            mimeMessage.To.Add(InternetAddress.Parse(to));

            mimeMessage.Subject = subject;
            BodyBuilder bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = "<html>Hi</html>";
            bodyBuilder.TextBody = "Hi";

            mimeMessage.Body = bodyBuilder.ToMessageBody();

            MemoryMessageBuilder memoryMessageBuilder = new MemoryMessageBuilder();
            memoryMessageBuilder.Recipients.Add(to);
            memoryMessageBuilder.From = from;
            memoryMessageBuilder.ReceivedDate = DateTime.Now;
            using (var messageData = await memoryMessageBuilder.WriteData())
            {
                mimeMessage.WriteTo(messageData);
            }

            IMessage message = await memoryMessageBuilder.ToMessage();

            var dbMessage = await new MessageConverter().ConvertAsync(message, [to]);
            dbMessage.Mailbox = new DbModel.Mailbox { Name = MailboxOptions.DEFAULTNAME };
          
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

            var dbMessage = await new MessageConverter().ConvertAsync(message, ["to@message.com"]);
            return dbMessage;
        }

        [Fact]
        public async Task GetSummaries_NoSearch_AllMessagesReturned()
        {
            DbModel.Message testMessage1 = await GetTestMessage("Message subject1");
            DbModel.Message testMessage2 = await GetTestMessage("Message subject2");
            DbModel.Message testMessage3 = await GetTestMessage("Message subject3");
            TestMessagesRepository messagesRepository = new TestMessagesRepository(testMessage1, testMessage2, testMessage3);
            MessagesController messagesController = new MessagesController(messagesRepository, null);

            var result = messagesController.GetSummaries(null);
            result.Results.Select(m => m.Id).Should().BeEquivalentTo(new[] { testMessage1.Id, testMessage2.Id, testMessage3.Id });
        }

        [Fact]
        public async Task GetSummaries_Search_MatchingMessagesReturned()
        {
            DbModel.Message testMessage1 = await GetTestMessage("Message subject1");
            DbModel.Message testMessage2 = await GetTestMessage("Message subject2");
            DbModel.Message testMessage3 = await GetTestMessage("Message subject3");
            var sqlLiteForTesting = new SqliteInMemory();
            var context = new Smtp4devDbContext(sqlLiteForTesting.ContextOptions);
            MessagesRepository messagesRepository =
                new MessagesRepository(Substitute.For<ITaskQueue>(), Substitute.For<NotificationsHub>(), context);
            messagesRepository.DbContext.Messages.AddRange(testMessage1, testMessage2, testMessage3);
            await messagesRepository.DbContext.SaveChangesAsync();
            MessagesController messagesController = new MessagesController(messagesRepository, null);

            var result = messagesController.GetSummaries("sUbJect2");
            result.Results.Select(m => m.Id).Should().BeEquivalentTo(new[] { testMessage2.Id });
        }

        [Fact]
        public async Task GetHtmlBody()
        {
            DbModel.Message testMessage1 = await GetTestMessage1();
            TestMessagesRepository messagesRepository = new TestMessagesRepository(testMessage1);
            MessagesController messagesController = new MessagesController(messagesRepository, null);

            var result = await messagesController.GetMessageHtml(testMessage1.Id);
            Assert.Equal(message1HtmlBody, result.Value);
        }
        
        [Fact]
        public async Task GetTextBody()
        {
            DbModel.Message testMessage1 = await GetTestMessage1();
            TestMessagesRepository messagesRepository = new TestMessagesRepository(testMessage1);
            MessagesController messagesController = new MessagesController(messagesRepository, null);

            string text = (await messagesController.GetMessagePlainText(testMessage1.Id)).Value;
            Assert.Equal(message1TextBody, text);
        }
        
        [Fact]
        public async Task GetHtmlBody_WhenThereIsntOne_ReturnsNotFound()
        {
            DbModel.Message testMessage1 = await GetTestMessage1(includeHtmlBody:false);
            TestMessagesRepository messagesRepository = new TestMessagesRepository(testMessage1);
            MessagesController messagesController = new MessagesController(messagesRepository, null);

            var result = await messagesController.GetMessageHtml(testMessage1.Id);
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }
        
        [Fact]
        public async Task GetTextBody_WhenThereIsntOne_ReturnsNotFound()
        {
            DbModel.Message testMessage1 = await GetTestMessage1(includeTextBody:false);
            TestMessagesRepository messagesRepository = new TestMessagesRepository(testMessage1);
            MessagesController messagesController = new MessagesController(messagesRepository, null);

            var result= await messagesController.GetMessagePlainText(testMessage1.Id);
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }
        [Fact]
        public async Task GetNewSummaries_NoBookmark_AllMessagesReturned()
        {
            DbModel.Message testMessage1 = await GetTestMessage1();
            DbModel.Message testMessage2 = await GetTestMessage1();
            DbModel.Message testMessage3 = await GetTestMessage1();
            TestMessagesRepository messagesRepository = new TestMessagesRepository(testMessage1, testMessage2, testMessage3);
            
            MessagesController messagesController = new MessagesController(messagesRepository, null);

            var result = messagesController.GetNewSummaries(null);
            result.Select(m => m.Id).Should().BeEquivalentTo(new[] { testMessage1.Id, testMessage2.Id, testMessage3.Id });
        }

        [Fact]
        public async Task GetNewSummaries_NoBookmark_NewerMessagesReturned()
        {
            DbModel.Message testMessage1 = await GetTestMessage1();
            DbModel.Message testMessage2 = await GetTestMessage1();
            DbModel.Message testMessage3 = await GetTestMessage1();
            TestMessagesRepository messagesRepository = new TestMessagesRepository(testMessage1, testMessage2, testMessage3);
            MessagesController messagesController = new MessagesController(messagesRepository, null);

            var result = messagesController.GetNewSummaries(testMessage2.Id);
            result.Select(m => m.Id).Should().BeEquivalentTo(new[] { testMessage3.Id });
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
