namespace Rnwood.Smtp4dev.Server.Pop3.CommandHandlers
{
	using System.Threading;
	using System.Threading.Tasks;
	using Rnwood.Smtp4dev.Server.Pop3;

	internal class UserCommand : ICommandHandler
	{
		public Task ExecuteAsync(Pop3SessionContext context, string argument, CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(argument))
			{
				return context.WriteLineAsync("-ERR Missing username");
			}

			context.Username = argument.Trim();
			return context.WriteLineAsync($"+OK user {context.Username} accepted");
		}
	}
}
