namespace Rnwood.Smtp4dev.Server.Pop3.CommandHandlers
{
	using System.Threading;
	using System.Threading.Tasks;
	using Rnwood.Smtp4dev.Server.Pop3;

	internal class PassCommand : ICommandHandler
	{
		public Task ExecuteAsync(Pop3SessionContext context, string argument, CancellationToken cancellationToken)
		{
			// In tests and simple server the PASS command will mark session authenticated if a username is present.
			if (string.IsNullOrEmpty(context.Username) || string.IsNullOrEmpty(argument))
			{
				return context.WriteLineAsync("-ERR Authentication failed");
			}

			context.Authenticated = true;
			return context.WriteLineAsync("+OK Authentication successful");
		}
	}
}
