namespace Rnwood.Smtp4dev.Server.Pop3.CommandHandlers
{
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using System;
	using Rnwood.Smtp4dev.Server.Pop3;
	using Microsoft.Extensions.Logging;

	internal class StatCommand : ICommandHandler
	{
		public Task ExecuteAsync(Pop3SessionContext context, string argument, CancellationToken cancellationToken)
		{
			if (!context.Authenticated)
			{
				return context.WriteLineAsync("-ERR Not authenticated");
			}

			// Determine mailbox consistent with IMAP behavior: if server requires authentication, map authenticated username to configured user's DefaultMailbox; otherwise use the Default mailbox
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
				var allCountBefore = context.MessagesRepository.GetAllMessages().Count();
				var mailboxCountBefore = context.MessagesRepository.GetMessages(mailbox, "INBOX").Count();
				context.Logger?.LogInformation("POP3 Diagnostic BEFORE query: DB total messages={allCount}, mailbox '{mailboxName}' messages={mailCount}", allCountBefore, mailbox, mailboxCountBefore);
			}
			catch (Exception ex)
			{
				// ignore diagnostics failures
				context.Logger?.LogWarning(ex, "POP3 diagnostic logging failed");
			}

			var messages = context.MessagesRepository.GetMessages(mailbox, "INBOX").ToList();
			var totalSize = messages.Sum(m => m.Data?.LongLength ?? 0);

			// Additional diagnostics after query
			try
			{
				var allCountAfter = context.MessagesRepository.GetAllMessages().Count();
				var mailboxCountAfter = context.MessagesRepository.GetMessages(mailbox, "INBOX").Count();
				context.Logger?.LogInformation("POP3 Diagnostic AFTER query: DB total messages={allCount}, mailbox '{mailboxName}' messages={mailCount}", allCountAfter, mailbox, mailboxCountAfter);
			}
			catch (Exception ex)
			{
				context.Logger?.LogWarning(ex, "POP3 diagnostic logging failed");
			}

			return context.WriteLineAsync($"+OK {messages.Count} {totalSize}");
		}
	}
}
