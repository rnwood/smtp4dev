using MimeKit;
using Rnwood.Smtp4dev.Data;
using Rnwood.Smtp4dev.Server.Settings;
using Rnwood.Smtp4dev.Server;
using Rnwood.Smtp4dev.Tests.DBMigrations.Helpers;
using Rnwood.SmtpServer;
using System.Threading.Tasks;
using System;
using System.IO;
using Xunit;
using Rnwood.Smtp4dev.Server.Imap;
using LumiSoft.Net.IMAP;
using System.Linq.Dynamic.Core;
using AwesomeAssertions;
using Rnwood.Smtp4dev.Migrations;
using Org.BouncyCastle.Bcpg;

namespace Rnwood.Smtp4dev.Tests
{

    public class ImapSearchTranslatorTests
    {
        [Fact]
        public void Unsupported()
        {

            ImapSearchTranslator imapSearchTranslator = new ImapSearchTranslator();

            Assert.Throws<ImapSearchCriteriaNotSupportedException>(() =>
            {
                imapSearchTranslator.Translate(new IMAP_Search_Key_Undeleted());
            });


        }

        [Fact]
        public async Task Subject()
        {
            SqliteInMemory m = new SqliteInMemory();

            DbModel.Message testMessage1 = await GetTestMessage("Message subject1");
            DbModel.Message testMessage2 = await GetTestMessage("Message subject2");
            DbModel.Message testMessage3 = await GetTestMessage("Message subject3");
            var sqlLiteForTesting = new SqliteInMemory();
            var context = new Smtp4devDbContext(sqlLiteForTesting.ContextOptions);

            context.AddRange(testMessage1, testMessage2, testMessage3);
            context.SaveChanges();

            ImapSearchTranslator imapSearchTranslator = new ImapSearchTranslator();

            var criteria = imapSearchTranslator.Translate(new IMAP_Search_Key_Subject("notfound"));

            var results = context.Messages.Where(criteria);
            Assert.Empty(results);

            criteria = imapSearchTranslator.Translate(new IMAP_Search_Key_Subject("subject2"));

            results = context.Messages.Where(criteria);
            results.Should().Contain([testMessage2]);

        }

        [Fact]
        public async Task To()
        {
            SqliteInMemory m = new SqliteInMemory();

            DbModel.Message testMessage1 = await GetTestMessage("Message subject1", to: "to1@to.com");
            DbModel.Message testMessage2 = await GetTestMessage("Message subject2", to: "to2@to.com");
            DbModel.Message testMessage3 = await GetTestMessage("Message subject3", to: "to3@to.com");
            var sqlLiteForTesting = new SqliteInMemory();
            var context = new Smtp4devDbContext(sqlLiteForTesting.ContextOptions);

            context.AddRange(testMessage1, testMessage2, testMessage3);
            context.SaveChanges();

            ImapSearchTranslator imapSearchTranslator = new ImapSearchTranslator();

            var criteria = imapSearchTranslator.Translate(new IMAP_Search_Key_To("notfound"));

            var results = context.Messages.Where(criteria);
            Assert.Empty(results);

            criteria = imapSearchTranslator.Translate(new IMAP_Search_Key_To("to2"));

            results = context.Messages.Where(criteria);
            results.Should().Contain([testMessage2]);

        }


        [Fact]
        public async Task From()
        {
            SqliteInMemory m = new SqliteInMemory();

            DbModel.Message testMessage1 = await GetTestMessage("Message subject1", from: "to1@to.com");
            DbModel.Message testMessage2 = await GetTestMessage("Message subject2", from: "to2@to.com");
            DbModel.Message testMessage3 = await GetTestMessage("Message subject3", from: "to3@to.com");
            var sqlLiteForTesting = new SqliteInMemory();
            var context = new Smtp4devDbContext(sqlLiteForTesting.ContextOptions);

            context.AddRange(testMessage1, testMessage2, testMessage3);
            context.SaveChanges();

            ImapSearchTranslator imapSearchTranslator = new ImapSearchTranslator();

            var criteria = imapSearchTranslator.Translate(new IMAP_Search_Key_From("notfound"));

            var results = context.Messages.Where(criteria);
            Assert.Empty(results);

            criteria = imapSearchTranslator.Translate(new IMAP_Search_Key_From("to2"));

            results = context.Messages.Where(criteria);
            results.Should().Contain([testMessage2]);

        }

        [Fact]
        public async Task Unseen()
        {
            SqliteInMemory m = new SqliteInMemory();

            DbModel.Message testMessage1 = await GetTestMessage("Message subject1", unread: false);
            DbModel.Message testMessage2 = await GetTestMessage("Message subject2", unread: true);
            DbModel.Message testMessage3 = await GetTestMessage("Message subject3", unread: false);
            var sqlLiteForTesting = new SqliteInMemory();
            var context = new Smtp4devDbContext(sqlLiteForTesting.ContextOptions);

            context.AddRange(testMessage1, testMessage2, testMessage3);
            context.SaveChanges();

            ImapSearchTranslator imapSearchTranslator = new ImapSearchTranslator();

            var criteria = imapSearchTranslator.Translate(new IMAP_Search_Key_Unseen());
            var results = context.Messages.Where(criteria);
            results.Should().Contain([testMessage2]);

        }

        [Fact]
        public async Task Seen()
        {
            SqliteInMemory m = new SqliteInMemory();

            DbModel.Message testMessage1 = await GetTestMessage("Message subject1", unread: false);
            DbModel.Message testMessage2 = await GetTestMessage("Message subject2", unread: true);
            DbModel.Message testMessage3 = await GetTestMessage("Message subject3", unread: false);
            var sqlLiteForTesting = new SqliteInMemory();
            var context = new Smtp4devDbContext(sqlLiteForTesting.ContextOptions);

            context.AddRange(testMessage1, testMessage2, testMessage3);
            context.SaveChanges();

            ImapSearchTranslator imapSearchTranslator = new ImapSearchTranslator();

            var criteria = imapSearchTranslator.Translate(new IMAP_Search_Key_Seen());
            var results = context.Messages.Where(criteria);
            results.Should().Contain([testMessage1, testMessage3]);

        }

        [Fact]
        public async Task Not()
        {
            SqliteInMemory m = new SqliteInMemory();

            DbModel.Message testMessage1 = await GetTestMessage("Message subject1");
            DbModel.Message testMessage2 = await GetTestMessage("Message subject2");
            DbModel.Message testMessage3 = await GetTestMessage("Message subject3");
            var sqlLiteForTesting = new SqliteInMemory();
            var context = new Smtp4devDbContext(sqlLiteForTesting.ContextOptions);

            context.AddRange(testMessage1, testMessage2, testMessage3);
            context.SaveChanges();

            ImapSearchTranslator imapSearchTranslator = new ImapSearchTranslator();

            var criteria = imapSearchTranslator.Translate(new IMAP_Search_Key_Not(new IMAP_Search_Key_Subject("subject2")));

            var results = context.Messages.Where(criteria);
            results.Should().Contain([testMessage1, testMessage3]);
        }

        [Fact]
        public async Task Or()
        {
            SqliteInMemory m = new SqliteInMemory();

            DbModel.Message testMessage1 = await GetTestMessage("Message subject1");
            DbModel.Message testMessage2 = await GetTestMessage("Message subject2");
            DbModel.Message testMessage3 = await GetTestMessage("Message subject3");
            var sqlLiteForTesting = new SqliteInMemory();
            var context = new Smtp4devDbContext(sqlLiteForTesting.ContextOptions);

            context.AddRange(testMessage1, testMessage2, testMessage3);
            context.SaveChanges();

            ImapSearchTranslator imapSearchTranslator = new ImapSearchTranslator();

            var criteria = imapSearchTranslator.Translate(new IMAP_Search_Key_Or(new IMAP_Search_Key_Subject("subject1"), new IMAP_Search_Key_Subject("subject2")));

            var results = context.Messages.Where(criteria);
            results.Should().Contain([testMessage1, testMessage2]);

            criteria = imapSearchTranslator.Translate(new IMAP_Search_Key_Or(new IMAP_Search_Key_Subject("subject1"), new IMAP_Search_Key_Subject("notfound")));

            results = context.Messages.Where(criteria);
            results.Should().Contain([testMessage1]);
        }

        [Fact]
        public async Task And()
        {
            SqliteInMemory m = new SqliteInMemory();

            DbModel.Message testMessage1 = await GetTestMessage("Message subject1");
            DbModel.Message testMessage2 = await GetTestMessage("Message subject2");
            DbModel.Message testMessage3 = await GetTestMessage("Message subject3");
            var sqlLiteForTesting = new SqliteInMemory();
            var context = new Smtp4devDbContext(sqlLiteForTesting.ContextOptions);

            context.AddRange(testMessage1, testMessage2, testMessage3);
            context.SaveChanges();

            ImapSearchTranslator imapSearchTranslator = new ImapSearchTranslator();

            var group = new IMAP_Search_Key_Group();
            group.Keys.AddRange([new IMAP_Search_Key_Subject("subject"), new IMAP_Search_Key_Subject("1")]);
            var criteria = imapSearchTranslator.Translate(group);
            var results = context.Messages.Where(criteria);
            results.Should().Contain([testMessage1]);

            group = new IMAP_Search_Key_Group();
            group.Keys.AddRange([new IMAP_Search_Key_Subject("subject"), new IMAP_Search_Key_Subject("notfound")]);
            criteria = imapSearchTranslator.Translate(group);
            results = context.Messages.Where(criteria);
            Assert.Empty(results);
        }


        [Fact]
        public async Task All()
        {
            SqliteInMemory m = new SqliteInMemory();

            DbModel.Message testMessage1 = await GetTestMessage("Message subject1");
            DbModel.Message testMessage2 = await GetTestMessage("Message subject2");
            DbModel.Message testMessage3 = await GetTestMessage("Message subject3");
            var sqlLiteForTesting = new SqliteInMemory();
            var context = new Smtp4devDbContext(sqlLiteForTesting.ContextOptions);

            context.AddRange(testMessage1, testMessage2, testMessage3);
            context.SaveChanges();

            ImapSearchTranslator imapSearchTranslator = new ImapSearchTranslator();

            var criteria = imapSearchTranslator.Translate(new IMAP_Search_Key_All());

            var results = context.Messages.Where(criteria);
            results.Should().Contain([testMessage1, testMessage2, testMessage3]);


        }

        [Fact]
        public async Task Younger()
        {
            SqliteInMemory m = new SqliteInMemory();

            // Create test messages with different received dates
            DbModel.Message oldMessage = await GetTestMessage("Old message", receivedDate: DateTime.Now.AddHours(-2));
            DbModel.Message recentMessage = await GetTestMessage("Recent message", receivedDate: DateTime.Now.AddMinutes(-30));
            DbModel.Message veryRecentMessage = await GetTestMessage("Very recent message", receivedDate: DateTime.Now.AddMinutes(-5));
            
            var sqlLiteForTesting = new SqliteInMemory();
            var context = new Smtp4devDbContext(sqlLiteForTesting.ContextOptions);

            context.AddRange(oldMessage, recentMessage, veryRecentMessage);
            context.SaveChanges();

            ImapSearchTranslator imapSearchTranslator = new ImapSearchTranslator();

            // Test YOUNGER 3600 (1 hour) - should return messages from last hour
            var criteria = imapSearchTranslator.Translate(new IMAP_Search_Key_Younger(3600));
            var results = context.Messages.Where(criteria);
            results.Should().Contain([recentMessage, veryRecentMessage]);
            results.Should().NotContain(oldMessage);

            // Test YOUNGER 600 (10 minutes) - should return only very recent message
            criteria = imapSearchTranslator.Translate(new IMAP_Search_Key_Younger(600));
            results = context.Messages.Where(criteria);
            results.Should().Contain([veryRecentMessage]);
            results.Should().NotContain([oldMessage, recentMessage]);

            // Test YOUNGER 10 (10 seconds) - should return no messages
            criteria = imapSearchTranslator.Translate(new IMAP_Search_Key_Younger(10));
            results = context.Messages.Where(criteria);
            Assert.Empty(results);
        }

        [Fact]
        public async Task Older()
        {
            SqliteInMemory m = new SqliteInMemory();

            // Create test messages with different received dates
            DbModel.Message oldMessage = await GetTestMessage("Old message", receivedDate: DateTime.Now.AddHours(-2));
            DbModel.Message recentMessage = await GetTestMessage("Recent message", receivedDate: DateTime.Now.AddMinutes(-30));
            DbModel.Message veryRecentMessage = await GetTestMessage("Very recent message", receivedDate: DateTime.Now.AddMinutes(-5));
            
            var sqlLiteForTesting = new SqliteInMemory();
            var context = new Smtp4devDbContext(sqlLiteForTesting.ContextOptions);

            context.AddRange(oldMessage, recentMessage, veryRecentMessage);
            context.SaveChanges();

            ImapSearchTranslator imapSearchTranslator = new ImapSearchTranslator();

            // Test OLDER 3600 (1 hour) - should return messages older than 1 hour
            var criteria = imapSearchTranslator.Translate(new IMAP_Search_Key_Older(3600));
            var results = context.Messages.Where(criteria);
            results.Should().Contain([oldMessage]);
            results.Should().NotContain([recentMessage, veryRecentMessage]);

            // Test OLDER 600 (10 minutes) - should return messages older than 10 minutes
            criteria = imapSearchTranslator.Translate(new IMAP_Search_Key_Older(600));
            results = context.Messages.Where(criteria);
            results.Should().Contain([oldMessage, recentMessage]);
            results.Should().NotContain([veryRecentMessage]);

            // Test OLDER 10800 (3 hours) - should return no messages
            criteria = imapSearchTranslator.Translate(new IMAP_Search_Key_Older(10800));
            results = context.Messages.Where(criteria);
            Assert.Empty(results);
        }

        [Fact]
        public async Task Uid_SingleUid()
        {
            SqliteInMemory m = new SqliteInMemory();

            DbModel.Message testMessage1 = await GetTestMessage("Message subject1");
            DbModel.Message testMessage2 = await GetTestMessage("Message subject2");
            DbModel.Message testMessage3 = await GetTestMessage("Message subject3");
            var sqlLiteForTesting = new SqliteInMemory();
            var context = new Smtp4devDbContext(sqlLiteForTesting.ContextOptions);

            context.AddRange(testMessage1, testMessage2, testMessage3);
            context.SaveChanges();

            // Assign IMAP UIDs
            testMessage1.ImapUid = 1;
            testMessage2.ImapUid = 2;
            testMessage3.ImapUid = 3;
            context.SaveChanges();

            ImapSearchTranslator imapSearchTranslator = new ImapSearchTranslator();

            // Test UID 2 - should return only message 2
            var criteria = imapSearchTranslator.Translate(new IMAP_Search_Key_Uid(IMAP_t_SeqSet.Parse("2")));
            var results = context.Messages.Where(criteria);
            results.Should().Contain([testMessage2]);
            results.Should().NotContain([testMessage1, testMessage3]);

            // Test UID 5 - should return no messages (non-existent UID)
            criteria = imapSearchTranslator.Translate(new IMAP_Search_Key_Uid(IMAP_t_SeqSet.Parse("5")));
            results = context.Messages.Where(criteria);
            Assert.Empty(results);
        }

        [Fact]
        public async Task Uid_Range()
        {
            SqliteInMemory m = new SqliteInMemory();

            DbModel.Message testMessage1 = await GetTestMessage("Message subject1");
            DbModel.Message testMessage2 = await GetTestMessage("Message subject2");
            DbModel.Message testMessage3 = await GetTestMessage("Message subject3");
            DbModel.Message testMessage4 = await GetTestMessage("Message subject4");
            DbModel.Message testMessage5 = await GetTestMessage("Message subject5");
            var sqlLiteForTesting = new SqliteInMemory();
            var context = new Smtp4devDbContext(sqlLiteForTesting.ContextOptions);

            context.AddRange(testMessage1, testMessage2, testMessage3, testMessage4, testMessage5);
            context.SaveChanges();

            // Assign IMAP UIDs
            testMessage1.ImapUid = 1;
            testMessage2.ImapUid = 2;
            testMessage3.ImapUid = 3;
            testMessage4.ImapUid = 4;
            testMessage5.ImapUid = 5;
            context.SaveChanges();

            ImapSearchTranslator imapSearchTranslator = new ImapSearchTranslator();

            // Test UID 2:4 - should return messages 2, 3, 4
            var criteria = imapSearchTranslator.Translate(new IMAP_Search_Key_Uid(IMAP_t_SeqSet.Parse("2:4")));
            var results = context.Messages.Where(criteria);
            results.Should().Contain([testMessage2, testMessage3, testMessage4]);
            results.Should().NotContain([testMessage1, testMessage5]);

            // Test UID 1:2 - should return messages 1, 2
            criteria = imapSearchTranslator.Translate(new IMAP_Search_Key_Uid(IMAP_t_SeqSet.Parse("1:2")));
            results = context.Messages.Where(criteria);
            results.Should().Contain([testMessage1, testMessage2]);
            results.Should().NotContain([testMessage3, testMessage4, testMessage5]);
        }

        [Fact]
        public async Task Uid_MultipleValues()
        {
            SqliteInMemory m = new SqliteInMemory();

            DbModel.Message testMessage1 = await GetTestMessage("Message subject1");
            DbModel.Message testMessage2 = await GetTestMessage("Message subject2");
            DbModel.Message testMessage3 = await GetTestMessage("Message subject3");
            DbModel.Message testMessage4 = await GetTestMessage("Message subject4");
            DbModel.Message testMessage5 = await GetTestMessage("Message subject5");
            var sqlLiteForTesting = new SqliteInMemory();
            var context = new Smtp4devDbContext(sqlLiteForTesting.ContextOptions);

            context.AddRange(testMessage1, testMessage2, testMessage3, testMessage4, testMessage5);
            context.SaveChanges();

            // Assign IMAP UIDs
            testMessage1.ImapUid = 1;
            testMessage2.ImapUid = 2;
            testMessage3.ImapUid = 3;
            testMessage4.ImapUid = 4;
            testMessage5.ImapUid = 5;
            context.SaveChanges();

            ImapSearchTranslator imapSearchTranslator = new ImapSearchTranslator();

            // Test UID 2,4,5 - should return messages 2, 4, 5
            var criteria = imapSearchTranslator.Translate(new IMAP_Search_Key_Uid(IMAP_t_SeqSet.Parse("2,4,5")));
            var results = context.Messages.Where(criteria);
            results.Should().Contain([testMessage2, testMessage4, testMessage5]);
            results.Should().NotContain([testMessage1, testMessage3]);

            // Test UID 1,3 - should return messages 1, 3
            criteria = imapSearchTranslator.Translate(new IMAP_Search_Key_Uid(IMAP_t_SeqSet.Parse("1,3")));
            results = context.Messages.Where(criteria);
            results.Should().Contain([testMessage1, testMessage3]);
            results.Should().NotContain([testMessage2, testMessage4, testMessage5]);
        }

        [Fact]
        public async Task Uid_MixedRangeAndValues()
        {
            SqliteInMemory m = new SqliteInMemory();

            DbModel.Message testMessage1 = await GetTestMessage("Message subject1");
            DbModel.Message testMessage2 = await GetTestMessage("Message subject2");
            DbModel.Message testMessage3 = await GetTestMessage("Message subject3");
            DbModel.Message testMessage4 = await GetTestMessage("Message subject4");
            DbModel.Message testMessage5 = await GetTestMessage("Message subject5");
            DbModel.Message testMessage6 = await GetTestMessage("Message subject6");
            var sqlLiteForTesting = new SqliteInMemory();
            var context = new Smtp4devDbContext(sqlLiteForTesting.ContextOptions);

            context.AddRange(testMessage1, testMessage2, testMessage3, testMessage4, testMessage5, testMessage6);
            context.SaveChanges();

            // Assign IMAP UIDs
            testMessage1.ImapUid = 1;
            testMessage2.ImapUid = 2;
            testMessage3.ImapUid = 3;
            testMessage4.ImapUid = 4;
            testMessage5.ImapUid = 5;
            testMessage6.ImapUid = 6;
            context.SaveChanges();

            ImapSearchTranslator imapSearchTranslator = new ImapSearchTranslator();

            // Test UID 2:4,6 - should return messages 2, 3, 4, 6
            var criteria = imapSearchTranslator.Translate(new IMAP_Search_Key_Uid(IMAP_t_SeqSet.Parse("2:4,6")));
            var results = context.Messages.Where(criteria);
            results.Should().Contain([testMessage2, testMessage3, testMessage4, testMessage6]);
            results.Should().NotContain([testMessage1, testMessage5]);

            // Test UID 1,3:5 - should return messages 1, 3, 4, 5
            criteria = imapSearchTranslator.Translate(new IMAP_Search_Key_Uid(IMAP_t_SeqSet.Parse("1,3:5")));
            results = context.Messages.Where(criteria);
            results.Should().Contain([testMessage1, testMessage3, testMessage4, testMessage5]);
            results.Should().NotContain([testMessage2, testMessage6]);
        }

        private static async Task<DbModel.Message> GetTestMessage(string subject, string from = "from@from.com", string to = "to@to.com", Boolean unread = true, DateTime? receivedDate = null)
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
            memoryMessageBuilder.ReceivedDate = receivedDate ?? DateTime.Now;
            using (var messageData = await memoryMessageBuilder.WriteData())
            {
                mimeMessage.WriteTo(messageData);
            }

            IMessage message = await memoryMessageBuilder.ToMessage();

            var dbMessage = await new MessageConverter(new MimeProcessingService()).ConvertAsync(message, [to]);
            dbMessage.Mailbox = new DbModel.Mailbox { Name = MailboxOptions.DEFAULTNAME };
            dbMessage.IsUnread = unread;
            
            // Set the received date directly on the db message if specified
            if (receivedDate.HasValue)
            {
                dbMessage.ReceivedDate = receivedDate.Value;
            }

            return dbMessage;
        }
    }
}
