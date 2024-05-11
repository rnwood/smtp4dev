using MimeKit;
using Rnwood.Smtp4dev.Data;
using Rnwood.Smtp4dev.Server.Settings;
using Rnwood.Smtp4dev.Server;
using Rnwood.Smtp4dev.Tests.DBMigrations.Helpers;
using Rnwood.SmtpServer;
using System.Threading.Tasks;
using System;
using Xunit;
using Rnwood.Smtp4dev.Server.Imap;
using LumiSoft.Net.IMAP;
using System.Linq.Dynamic.Core;
using FluentAssertions;
using Rnwood.Smtp4dev.Migrations;
using Org.BouncyCastle.Bcpg;

namespace Rnwood.Smtp4dev.Tests
{

    public class ImapSearchTranslatorTests
    {
        [Fact]
        public async Task Unsupported()
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

        private static async Task<DbModel.Message> GetTestMessage(string subject, string from = "from@from.com", string to = "to@to.com", Boolean unread = true)
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
            dbMessage.IsUnread = unread;

            return dbMessage;
        }
    }
}
