namespace Rnwood.Smtp4dev.Server.Pop3.CommandHandlers
{
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using Rnwood.Smtp4dev.Server.Pop3;
	using System;
	using Microsoft.Extensions.Logging;

	internal class ListCommand : ICommandHandler
	{
		public Task ExecuteAsync(Pop3SessionContext context, string argument, CancellationToken cancellationToken)
		{
			if (!context.Authenticated)
			{
				return context.WriteLineAsync("-ERR Not authenticated");
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
				context.Logger?.LogInformation("POP3 Diagnostic LIST: DB total messages={allCount}, mailbox '{mailboxName}' messages={mailCount}", allCount, mailbox, mailboxCount);
			}
			catch (Exception ex)
			{
				context.Logger?.LogWarning(ex, "POP3 diagnostic logging failed");
			}

			var messages = context.MessagesRepository.GetMessages(mailbox, "INBOX").ToList();
			if (string.IsNullOrWhiteSpace(argument))
			{
				// mult-line response
				context.Writer.Write($"+OK {messages.Count} messages:\r\n");
				for (int i = 0; i < messages.Count; i++)
				{
					context.Writer.Write($"{i + 1} {messages[i].Data?.LongLength ?? 0}\r\n");
				}
				context.Writer.Write(".\r\n");
				return context.Writer.FlushAsync();
			}

			// single message
			if (int.TryParse(argument.Trim(), out int id) && id > 0 && id <= messages.Count)
			{
				var size = messages[id - 1].Data?.LongLength ?? 0;
				return context.WriteLineAsync($"+OK {id} {size}");
			}

			return context.WriteLineAsync("-ERR no such message");
		}
	}
}
