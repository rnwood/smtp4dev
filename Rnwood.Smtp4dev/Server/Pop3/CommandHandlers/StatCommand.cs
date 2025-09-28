namespace Rnwood.Smtp4dev.Server.Pop3.CommandHandlers
{
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using Rnwood.Smtp4dev.Server.Pop3;

	internal class StatCommand : ICommandHandler
	{
		public Task ExecuteAsync(Pop3SessionContext context, string argument, CancellationToken cancellationToken)
		{
			if (!context.Authenticated)
			{
				return context.WriteLineAsync("-ERR Not authenticated");
			}

			var mailbox = context.Username ?? Rnwood.Smtp4dev.Server.Settings.MailboxOptions.DEFAULTNAME;
			var messages = context.MessagesRepository.GetMessages(mailbox, "INBOX").ToList();
			var totalSize = messages.Sum(m => m.Data?.LongLength ?? 0);
			return context.WriteLineAsync($"+OK {messages.Count} {totalSize}");
		}
	}
}
