using MimeKit;
using Rnwood.Smtp4dev.Controllers;
using Rnwood.Smtp4dev.Server;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Rnwood.Smtp4dev.Tests
{
	public class MessagesControllerTests
	{
		[Fact]
		public async Task GetMessage_ValidMime()
		{
			DateTime startDate = DateTime.Now;
			DbModel.Message testMessage1 = await GetTestMessage1();

			TestMessagesRepository messagesRepository = new TestMessagesRepository(testMessage1);
			MessagesController messagesController = new MessagesController(messagesRepository);

			ApiModel.Message result = messagesController.GetMessage(testMessage1.Id);

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


			Assert.All(allParts, p => {
				Assert.Equal(testMessage1.Id, p.MessageId);
				Assert.NotNull(p.Id);
				Assert.NotEqual("", p.Id);
			});

			//All parts have a unique Id
			Assert.Equal(allParts.Count, allParts.Select(p => p.Id).Distinct().Count());

		}

		private static readonly string testMessage1File1Content = "111";
		private static readonly string testMessage1File2Content = "222";

		private static async Task<DbModel.Message> GetTestMessage1()
		{
			MimeMessage message = new MimeMessage();
			message.From.Add(InternetAddress.Parse("from@message.com"));
			message.To.Add(InternetAddress.Parse("to@message.com"));
			message.Cc.Add(InternetAddress.Parse("cc@message.com"));

			message.Subject = "subject";
			BodyBuilder bodyBuilder = new BodyBuilder();
			bodyBuilder.HtmlBody = "<html>Hi</html>";
			bodyBuilder.TextBody = "Hi";
			bodyBuilder.Attachments.Add("file1", new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testMessage1File1Content)), new ContentType("text", "plain")).ContentId = "file1";
			bodyBuilder.Attachments.Add("file2", new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testMessage1File2Content)), new ContentType("text", "plain"));

			message.Body = bodyBuilder.ToMessageBody();

			var dbMessage = await new MessageConverter().ConvertAsync(
new MemoryStream(Encoding.UTF8.GetBytes(message.ToString())), "from@envelope.com", "to@envelope.com");
			return dbMessage;
		}

		[Fact]
		public async Task GetPartContent()
		{
			DbModel.Message testMessage1 = await GetTestMessage1();
			TestMessagesRepository messagesRepository = new TestMessagesRepository(testMessage1);
			MessagesController messagesController = new MessagesController(messagesRepository);

			var parts = messagesController.GetMessage(testMessage1.Id).Parts.Flatten(p => p.ChildParts).SelectMany(p => p.Attachments);

			var part = parts.First(p => p.FileName == "file2");

			var result = messagesController.GetPartContent(testMessage1.Id, part.Id);
			var stringResult = await new StreamReader(result.FileStream, Encoding.UTF8).ReadToEndAsync();

			Assert.Equal(testMessage1File2Content, stringResult);
		}
	}

}
