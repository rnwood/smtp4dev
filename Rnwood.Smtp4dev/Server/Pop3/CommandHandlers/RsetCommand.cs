namespace Rnwood.Smtp4dev.Server.Pop3.CommandHandlers
{
	using System.Threading;
	using System.Threading.Tasks;
	using Rnwood.Smtp4dev.Server.Pop3;

	internal class RsetCommand : ICommandHandler
	{
		public Task ExecuteAsync(Pop3SessionContext context, string argument, CancellationToken cancellationToken)
		{
			// For our simple in-memory test repo we'll assume DELE actually removed items immediately,
			// so RSET is a no-op but still allowed.
			return context.WriteLineAsync("+OK Reset state");
		}
	}
}
