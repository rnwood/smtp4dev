namespace Rnwood.Smtp4dev.Server.Pop3.CommandHandlers
{
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using Rnwood.Smtp4dev.Server.Pop3;

	internal class DeleCommand : ICommandHandler
	{
		public Task ExecuteAsync(Pop3SessionContext context, string argument, CancellationToken cancellationToken)
		{
			if (!context.Authenticated)
			{
				return context.WriteLineAsync("-ERR Not authenticated");
			}

			if (!int.TryParse(argument?.Trim(), out int id))
			{
				return context.WriteLineAsync("-ERR Invalid message id");
			}

			var mailbox = context.Username ?? Rnwood.Smtp4dev.Server.Settings.MailboxOptions.DEFAULTNAME;
			var messages = context.MessagesRepository.GetMessages(mailbox, "INBOX").ToList();
			if (id < 1 || id > messages.Count)
			{
				return context.WriteLineAsync("-ERR No such message");
			}

			var message = messages[id - 1];
			context.MessagesRepository.DeleteMessage(message.Id);
			return context.WriteLineAsync($"+OK message {id} deleted");
		}
	}
}
