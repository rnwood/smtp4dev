namespace Rnwood.Smtp4dev.Server.Pop3.CommandHandlers
{
	using System;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using Rnwood.Smtp4dev.Server.Pop3;

	internal class RetrCommand : ICommandHandler
	{
		public async Task ExecuteAsync(Pop3SessionContext context, string argument, CancellationToken cancellationToken)
		{
			if (!context.Authenticated)
			{
				await context.WriteLineAsync("-ERR Not authenticated");
				return;
			}

			if (!int.TryParse(argument?.Trim(), out int id))
			{
				await context.WriteLineAsync("-ERR Invalid message id");
				return;
			}

			var mailbox = context.Username ?? Rnwood.Smtp4dev.Server.Settings.MailboxOptions.DEFAULTNAME;
			var messages = context.MessagesRepository.GetMessages(mailbox, "INBOX").ToList();
			if (id < 1 || id > messages.Count)
			{
				await context.WriteLineAsync("-ERR No such message");
				return;
			}

			var msg = messages[id - 1];
			await context.WriteLineAsync($"+OK {msg.Data?.LongLength ?? 0} octets");
			var data = msg.Data ?? Array.Empty<byte>();
			await Rnwood.Smtp4dev.Server.Pop3ProtocolHelper.WriteDotStuffedMessageAsync(context.Stream, data, cancellationToken);
		}
	}
}
