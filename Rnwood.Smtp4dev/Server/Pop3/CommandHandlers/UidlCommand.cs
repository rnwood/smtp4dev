namespace Rnwood.Smtp4dev.Server.Pop3.CommandHandlers
{
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using Rnwood.Smtp4dev.Server.Pop3;

	internal class UidlCommand : ICommandHandler
	{
		public Task ExecuteAsync(Pop3SessionContext context, string argument, CancellationToken cancellationToken)
		{
			if (!context.Authenticated)
			{
				return context.WriteLineAsync("-ERR Not authenticated");
			}

			var mailbox = context.Username ?? Rnwood.Smtp4dev.Server.Settings.MailboxOptions.DEFAULTNAME;
			var messages = context.MessagesRepository.GetMessages(mailbox, "INBOX").ToList();
			if (string.IsNullOrWhiteSpace(argument))
			{
				context.Writer.Write($"+OK {messages.Count} messages\r\n");
				for (int i = 0; i < messages.Count; i++)
				{
					// Use message id and a simple fingerprint as UIDL
					var uid = messages[i].Id.ToString("N") + "-" + (messages[i].Data?.LongLength ?? 0);
					context.Writer.Write($"{i + 1} {uid}\r\n");
				}
				context.Writer.Write(".\r\n");
				return context.Writer.FlushAsync();
			}

			if (int.TryParse(argument.Trim(), out int id) && id > 0 && id <= messages.Count)
			{
				var uid = messages[id - 1].Id.ToString("N") + "-" + (messages[id - 1].Data?.LongLength ?? 0);
				return context.WriteLineAsync($"+OK {id} {uid}");
			}

			return context.WriteLineAsync("-ERR no such message");
		}
	}
}
