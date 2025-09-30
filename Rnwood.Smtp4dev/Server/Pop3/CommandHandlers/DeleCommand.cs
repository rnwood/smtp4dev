namespace Rnwood.Smtp4dev.Server.Pop3.CommandHandlers
{
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using Rnwood.Smtp4dev.Server.Pop3;
	using System;
	using Microsoft.Extensions.DependencyInjection;

	internal class DeleCommand : ICommandHandler
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

			var messages = context.MessagesRepository.GetMessages(mailbox, "INBOX").ToList();
			if (id < 1 || id > messages.Count)
			{
				await context.WriteLineAsync("-ERR No such message");
				return;
			}

			var message = messages[id - 1];
			// Use a short-lived repository instance from a new DI scope so delete runs with a fresh DbContext
			if (context.ScopeFactory == null)
			{
				// Fallback to session repository if no scope factory available (should not happen in DI-enabled server)
				await context.MessagesRepository.DeleteMessage(message.Id);
			}
			else
			{
				using var scope = context.ScopeFactory.CreateScope();
				var repo = scope.ServiceProvider.GetRequiredService<Rnwood.Smtp4dev.Data.IMessagesRepository>();
				await repo.DeleteMessage(message.Id);
			}
			await context.WriteLineAsync($"+OK message {id} deleted");
			return;
		}
	}
}
