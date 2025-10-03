namespace Rnwood.Smtp4dev.Server.Pop3.CommandHandlers
{
	using System.Threading;
	using System.Threading.Tasks;
	using Rnwood.Smtp4dev.Server.Pop3;

	internal class UnknownCommand : ICommandHandler
	{
		public Task ExecuteAsync(Pop3SessionContext context, string argument, CancellationToken cancellationToken)
		{
			return context.WriteLineAsync("-ERR Unknown command");
		}
	}
}
