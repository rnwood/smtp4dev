namespace Rnwood.Smtp4dev.Server.Pop3.CommandHandlers
{
	using System;
	using System.Threading;
	using System.Threading.Tasks;
	using Rnwood.Smtp4dev.Server.Pop3;

	internal class NoopCommand : ICommandHandler
	{
		public async Task ExecuteAsync(Pop3SessionContext context, string argument, CancellationToken cancellationToken)
		{
			if (!context.Authenticated)
			{
				await context.WriteLineAsync("-ERR Not authenticated");
				return;
			}

			await context.WriteLineAsync("+OK");
		}
	}
}
