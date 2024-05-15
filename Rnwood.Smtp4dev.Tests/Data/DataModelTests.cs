using MimeKit;
using NSubstitute;
using Rnwood.Smtp4dev.Controllers;
using Rnwood.Smtp4dev.Data;
using Rnwood.Smtp4dev.Hubs;
using Rnwood.Smtp4dev.Server;
using Rnwood.Smtp4dev.Server.Settings;
using Rnwood.Smtp4dev.Tests.DBMigrations.Helpers;
using Rnwood.SmtpServer;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Rnwood.Smtp4dev.Tests.Data
{
    public class DataModelTests
    {
        [Fact]
        public async Task CanDeleteSessionWhenMessageExist()
        {
            var sqlLiteForTesting = new SqliteInMemory();
            var context = new Smtp4devDbContext(sqlLiteForTesting.ContextOptions);

            DbModel.Session session = new DbModel.Session();
            context.Add(session);
            DbModel.Message testMessage1 = await GetTestMessage("Message subject1");
            testMessage1.Session = session;

            context.Add(testMessage1);
            await context.SaveChangesAsync();

            context.Remove(session);
            await context.SaveChangesAsync();

            Assert.Null(testMessage1.Session);
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
    }
}
