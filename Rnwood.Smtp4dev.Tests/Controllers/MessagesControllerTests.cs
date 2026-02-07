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
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit.Encodings;
using NSubstitute;
using Rnwood.Smtp4dev.Data;
using Rnwood.Smtp4dev.Hubs;
using Rnwood.Smtp4dev.Tests.DBMigrations.Helpers;
using Xunit;
using Rnwood.Smtp4dev.Server.Settings;
using Rnwood.Smtp4dev.DbModel;

namespace Rnwood.Smtp4dev.Tests.Controllers
{
    public class MessagesControllerTests
    {
        private static IOptionsMonitor<Rnwood.Smtp4dev.Server.Settings.ServerOptions> CreateServerOptionsMock()
        {
            var serverOptions = new Rnwood.Smtp4dev.Server.Settings.ServerOptions { Users = new UserOptions[0] };
            var optionsMonitor = Substitute.For<IOptionsMonitor<Rnwood.Smtp4dev.Server.Settings.ServerOptions>>();
            optionsMonitor.CurrentValue.Returns(serverOptions);
            return optionsMonitor;
        }

        [Fact]
        public async Task GetMessage_ValidMime()
        {
            DateTime startDate = DateTime.Now;
            DbModel.Message testMessage1 = await GetTestMessage1();

            TestMessagesRepository messagesRepository = new TestMessagesRepository(testMessage1);
            MessagesController messagesController = new MessagesController(messagesRepository, null, new MimeProcessingService(), CreateServerOptionsMock());

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

            var dbMessage = await new MessageConverter(new MimeProcessingService()).ConvertAsync(message, ["to@envelope.com"]);
            return dbMessage;
        }

        private static async Task<DbModel.Message> GetTestMessage(string subject, string from = "from@from.com", string to = "to@to.com")
        {
            return await GetTestMessageWithExtras(subject, from, to, null, null, null, null);
        }

        private static async Task<DbModel.Message> GetTestMessageWithExtras(string subject, string from = "from@from.com", string to = "to@to.com", 
            string cc = null, string htmlBody = null, string textBody = null, string attachmentFileName = null)
        {
            MimeMessage mimeMessage = new MimeMessage();
            mimeMessage.From.Add(InternetAddress.Parse(from));
            mimeMessage.To.Add(InternetAddress.Parse(to));
            
            if (!string.IsNullOrEmpty(cc))
            {
                mimeMessage.Cc.Add(InternetAddress.Parse(cc));
            }

            mimeMessage.Subject = subject;
            BodyBuilder bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = htmlBody ?? "<html>Hi</html>";
            bodyBuilder.TextBody = textBody ?? "Hi";

            if (!string.IsNullOrEmpty(attachmentFileName))
            {
                bodyBuilder.Attachments.Add(attachmentFileName, 
                    new MemoryStream(System.Text.Encoding.UTF8.GetBytes("Test attachment content")), 
                    new ContentType("text", "plain"));
            }

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

            var dbMessage = await new MessageConverter(new MimeProcessingService()).ConvertAsync(message, [to]);
            dbMessage.Mailbox = new DbModel.Mailbox { Name = MailboxOptions.DEFAULTNAME };
            dbMessage.MailboxFolder = new DbModel.MailboxFolder { Name = MailboxFolder.INBOX, Mailbox = dbMessage.Mailbox };
          
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

            var dbMessage = await new MessageConverter(new MimeProcessingService()).ConvertAsync(message, ["to@message.com"]);
            return dbMessage;
        }

        [Fact]
        public async Task GetSummaries_NoSearch_AllMessagesReturned()
        {
            DbModel.Message testMessage1 = await GetTestMessage("Message subject1");
            DbModel.Message testMessage2 = await GetTestMessage("Message subject2");
            DbModel.Message testMessage3 = await GetTestMessage("Message subject3");
            TestMessagesRepository messagesRepository = new TestMessagesRepository(testMessage1, testMessage2, testMessage3);
            MessagesController messagesController = new MessagesController(messagesRepository, null, new MimeProcessingService(), CreateServerOptionsMock());

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
            MessagesController messagesController = new MessagesController(messagesRepository, null, new MimeProcessingService(), CreateServerOptionsMock());

            var result = messagesController.GetSummaries("sUbJect2");
            result.Results.Select(m => m.Id).Should().BeEquivalentTo(new[] { testMessage2.Id });
        }

        [Fact]
        public async Task GetSummaries_SearchInCC_MatchingMessagesReturned()
        {
            DbModel.Message testMessage1 = await GetTestMessageWithExtras("Subject1", cc: "ccuser@example.com");
            DbModel.Message testMessage2 = await GetTestMessageWithExtras("Subject2", cc: "anothercc@example.com");
            DbModel.Message testMessage3 = await GetTestMessage("Subject3");
            var sqlLiteForTesting = new SqliteInMemory();
            var context = new Smtp4devDbContext(sqlLiteForTesting.ContextOptions);
            MessagesRepository messagesRepository =
                new MessagesRepository(Substitute.For<ITaskQueue>(), Substitute.For<NotificationsHub>(), context);
            messagesRepository.DbContext.Messages.AddRange(testMessage1, testMessage2, testMessage3);
            await messagesRepository.DbContext.SaveChangesAsync();
            MessagesController messagesController = new MessagesController(messagesRepository, null, new MimeProcessingService(), CreateServerOptionsMock());

            var result = messagesController.GetSummaries("ccuser");
            result.Results.Select(m => m.Id).Should().BeEquivalentTo(new[] { testMessage1.Id });
        }

        [Fact]
        public async Task GetSummaries_SearchInBodyContent_MatchingMessagesReturned()
        {
            DbModel.Message testMessage1 = await GetTestMessageWithExtras("Subject1", htmlBody: "<html>Unique search content here</html>", textBody: "Plain text");
            DbModel.Message testMessage2 = await GetTestMessageWithExtras("Subject2", htmlBody: "<html>Different content</html>", textBody: "Also different");
            DbModel.Message testMessage3 = await GetTestMessageWithExtras("Subject3", htmlBody: "<html>Normal</html>", textBody: "Unique search content here in plain text");
            var sqlLiteForTesting = new SqliteInMemory();
            var context = new Smtp4devDbContext(sqlLiteForTesting.ContextOptions);
            MessagesRepository messagesRepository =
                new MessagesRepository(Substitute.For<ITaskQueue>(), Substitute.For<NotificationsHub>(), context);
            messagesRepository.DbContext.Messages.AddRange(testMessage1, testMessage2, testMessage3);
            await messagesRepository.DbContext.SaveChangesAsync();
            MessagesController messagesController = new MessagesController(messagesRepository, null, new MimeProcessingService(), CreateServerOptionsMock());

            var result = messagesController.GetSummaries("Unique search content");
            result.Results.Select(m => m.Id).Should().BeEquivalentTo(new[] { testMessage1.Id, testMessage3.Id });
        }

        [Fact]
        public async Task GetSummaries_SearchInAttachmentFilenames_MatchingMessagesReturned()
        {
            DbModel.Message testMessage1 = await GetTestMessageWithExtras("Subject1", attachmentFileName: "important-document.pdf");
            DbModel.Message testMessage2 = await GetTestMessageWithExtras("Subject2", attachmentFileName: "regular-file.txt");
            DbModel.Message testMessage3 = await GetTestMessage("Subject3");
            var sqlLiteForTesting = new SqliteInMemory();
            var context = new Smtp4devDbContext(sqlLiteForTesting.ContextOptions);
            MessagesRepository messagesRepository =
                new MessagesRepository(Substitute.For<ITaskQueue>(), Substitute.For<NotificationsHub>(), context);
            messagesRepository.DbContext.Messages.AddRange(testMessage1, testMessage2, testMessage3);
            await messagesRepository.DbContext.SaveChangesAsync();
            MessagesController messagesController = new MessagesController(messagesRepository, null, new MimeProcessingService(), CreateServerOptionsMock());

            var result = messagesController.GetSummaries("important-document");
            result.Results.Select(m => m.Id).Should().BeEquivalentTo(new[] { testMessage1.Id });
        }

        [Fact]
        public void GetSummaries_MessageWithNullToField_DoesNotThrow()
        {
            // Arrange - create a message with null To field to simulate edge cases
            var mailbox = new DbModel.Mailbox { Name = MailboxOptions.DEFAULTNAME };
            var messageWithNullTo = new DbModel.Message
            {
                Id = Guid.NewGuid(),
                Subject = "Test Subject",
                From = "from@test.com",
                To = null, // This is the edge case that was causing NullReferenceException
                ReceivedDate = DateTime.Now,
                Mailbox = mailbox,
                MailboxFolder = new DbModel.MailboxFolder { Name = MailboxFolder.INBOX, Mailbox = mailbox }
            };
            
            TestMessagesRepository messagesRepository = new TestMessagesRepository(messageWithNullTo);
            MessagesController messagesController = new MessagesController(messagesRepository, null, new MimeProcessingService(), CreateServerOptionsMock());

            // Act & Assert - should not throw NullReferenceException
            var result = messagesController.GetSummaries(null);
            result.Results.Should().HaveCount(1);
            result.Results[0].To.Should().BeEmpty();
        }

        [Fact]
        public async Task GetHtmlBody()
        {
            DbModel.Message testMessage1 = await GetTestMessage1();
            TestMessagesRepository messagesRepository = new TestMessagesRepository(testMessage1);
            MessagesController messagesController = new MessagesController(messagesRepository, null, new MimeProcessingService(), CreateServerOptionsMock());

            var result = await messagesController.GetMessageHtml(testMessage1.Id);
            Assert.Equal(message1HtmlBody, result.Value);
        }
        
        [Fact]
        public async Task GetTextBody()
        {
            DbModel.Message testMessage1 = await GetTestMessage1();
            TestMessagesRepository messagesRepository = new TestMessagesRepository(testMessage1);
            MessagesController messagesController = new MessagesController(messagesRepository, null, new MimeProcessingService(), CreateServerOptionsMock());

            string text = (await messagesController.GetMessagePlainText(testMessage1.Id)).Value;
            Assert.Equal(message1TextBody, text);
        }
        
        [Fact]
        public async Task GetHtmlBody_WhenThereIsntOne_ReturnsNotFound()
        {
            DbModel.Message testMessage1 = await GetTestMessage1(includeHtmlBody:false);
            TestMessagesRepository messagesRepository = new TestMessagesRepository(testMessage1);
            MessagesController messagesController = new MessagesController(messagesRepository, null, new MimeProcessingService(), CreateServerOptionsMock());

            var result = await messagesController.GetMessageHtml(testMessage1.Id);
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }
        
        [Fact]
        public async Task GetTextBody_WhenThereIsntOne_ReturnsNotFound()
        {
            DbModel.Message testMessage1 = await GetTestMessage1(includeTextBody:false);
            TestMessagesRepository messagesRepository = new TestMessagesRepository(testMessage1);
            MessagesController messagesController = new MessagesController(messagesRepository, null, new MimeProcessingService(), CreateServerOptionsMock());

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
            
            MessagesController messagesController = new MessagesController(messagesRepository, null, new MimeProcessingService(), CreateServerOptionsMock());

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
            MessagesController messagesController = new MessagesController(messagesRepository, null, new MimeProcessingService(), CreateServerOptionsMock());

            var result = messagesController.GetNewSummaries(testMessage2.Id);
            result.Select(m => m.Id).Should().BeEquivalentTo(new[] { testMessage3.Id });
        }

        [Fact]
        public async Task GetPartContent()
        {
            DbModel.Message testMessage1 = await GetTestMessage1();
            TestMessagesRepository messagesRepository = new TestMessagesRepository(testMessage1);
            MessagesController messagesController = new MessagesController(messagesRepository, null, new MimeProcessingService(), CreateServerOptionsMock());

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
            MessagesController messagesController = new MessagesController(messagesRepository, null, new MimeProcessingService(), CreateServerOptionsMock());


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
            MessagesController messagesController = new MessagesController(messagesRepository, null, new MimeProcessingService(), CreateServerOptionsMock());


            string result = await messagesController.GetMessageSourceRaw(testMessage2.Id);
            Assert.Contains("From: from@message.com", result);

            QuotedPrintableEncoder e = new QuotedPrintableEncoder();
            var bytes = encoding.GetBytes(QPMESSAGE_BODY);
            byte[] output = new byte[e.EstimateOutputLength(bytes.Length)];
            int outputLen = e.Encode(bytes, 0, bytes.Length, output);
            string qpResult = Encoding.ASCII.GetString(output, 0, outputLen);
            
            Assert.Contains(qpResult, result);
        }

        [Fact]
        public async Task AttachmentUrl_ShouldIncludeDownloadParameter()
        {
            // Arrange - Create a message with an attachment
            DbModel.Message dbMessage = await GetTestMessageWithExtras("Test Subject", attachmentFileName: "Confirmation.pdf");
            
            // Act - Convert to API model
            var apiMessage = new ApiModel.Message(dbMessage);
            
            // Assert - Check that attachment URL includes download=true
            var allAttachments = apiMessage.Parts.Flatten(p => p.ChildParts)
                .SelectMany(p => p.Attachments)
                .ToList();
            var attachment = allAttachments.FirstOrDefault(a => a.FileName == "Confirmation.pdf");
            
            Assert.NotNull(attachment);
            Assert.Contains("download=true", attachment.Url);
        }
      
        public async Task GetSummaries_MultipleRecipients_EmailsShouldNotHaveLeadingSpaces()
        {
            // Arrange - create a message with multiple recipients that will be joined with ", "
            MimeMessage mimeMessage = new MimeMessage();
            mimeMessage.From.Add(InternetAddress.Parse("from@example.com"));
            mimeMessage.To.Add(InternetAddress.Parse("test@example.com"));
            mimeMessage.To.Add(InternetAddress.Parse("admin@example.com"));
            mimeMessage.Subject = "Test Subject";
            
            BodyBuilder bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = "<html>Test</html>";
            mimeMessage.Body = bodyBuilder.ToMessageBody();

            MemoryMessageBuilder memoryMessageBuilder = new MemoryMessageBuilder();
            memoryMessageBuilder.Recipients.Add("test@example.com");
            memoryMessageBuilder.Recipients.Add("admin@example.com");
            memoryMessageBuilder.From = "from@example.com";
            memoryMessageBuilder.ReceivedDate = DateTime.Now;
            using (var messageData = await memoryMessageBuilder.WriteData())
            {
                mimeMessage.WriteTo(messageData);
            }

            IMessage message = await memoryMessageBuilder.ToMessage();
            var dbMessage = await new MessageConverter(new MimeProcessingService()).ConvertAsync(message, new[] { "test@example.com", "admin@example.com" });
            dbMessage.Mailbox = new DbModel.Mailbox { Name = MailboxOptions.DEFAULTNAME };
            dbMessage.MailboxFolder = new DbModel.MailboxFolder { Name = MailboxFolder.INBOX, Mailbox = dbMessage.Mailbox };

            TestMessagesRepository messagesRepository = new TestMessagesRepository(dbMessage);
            MessagesController messagesController = new MessagesController(messagesRepository, null, new MimeProcessingService(), CreateServerOptionsMock());

            // Act
            var result = messagesController.GetSummaries(null);

            // Assert
            Assert.Single(result.Results);
            Assert.Equal(2, result.Results[0].To.Length);
            Assert.Equal("test@example.com", result.Results[0].To[0]);
            Assert.Equal("admin@example.com", result.Results[0].To[1]); // Should NOT have leading space
            Assert.DoesNotContain(result.Results[0].To, email => email.StartsWith(" "));
        }
    }

}
