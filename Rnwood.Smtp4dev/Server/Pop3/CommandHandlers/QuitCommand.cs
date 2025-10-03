namespace Rnwood.Smtp4dev.Server.Pop3.CommandHandlers
{
	using System;
	using System.Threading;
	using System.Threading.Tasks;
	using Rnwood.Smtp4dev.Server.Pop3;

	internal class QuitCommand : ICommandHandler
	{
		public async Task ExecuteAsync(Pop3SessionContext context, string argument, CancellationToken cancellationToken)
		{
			// Send final acknowledgement then end session
			await context.WriteLineAsync("+OK Goodbye");
			throw new OperationCanceledException();
		}
	}
}
