namespace Rnwood.Smtp4dev.Server.Pop3.CommandHandlers
{
	using System;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using Rnwood.Smtp4dev.Server.Pop3;
	using Microsoft.Extensions.Logging;

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

			string mailbox;
			if (!context.Options.AuthenticationRequired)
			{
				mailbox = Rnwood.Smtp4dev.Server.Settings.MailboxOptions.DEFAULTNAME;
			}
			else
			{
				var user = context.Options.Users?.FirstOrDefault(u => string.Equals(u.Username, context.Username ?? string.Empty, StringComparison.OrdinalIgnoreCase));
				mailbox = user?.DefaultMailbox ?? Rnwood.Smtp4dev.Server.Settings.MailboxOptions.DEFAULTNAME;
			}

			// Temporary diagnostics to investigate E2E failure where POP3 reports zero messages
			try
			{
				var allCount = context.MessagesRepository.GetAllMessages().Count();
				var mailboxCount = context.MessagesRepository.GetMessages(mailbox, "INBOX").Count();
				context.Logger?.LogInformation("POP3 Diagnostic RETR: DB total messages={allCount}, mailbox '{mailboxName}' messages={mailCount}", allCount, mailbox, mailboxCount);
			}
			catch (Exception ex)
			{
				context.Logger?.LogWarning(ex, "POP3 diagnostic logging failed");
			}

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
