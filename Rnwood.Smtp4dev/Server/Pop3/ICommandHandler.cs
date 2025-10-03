namespace Rnwood.Smtp4dev.Server.Pop3
{
	using System.Threading;
	using System.Threading.Tasks;

	internal interface ICommandHandler
	{
		/// <summary>
		/// Execute a single POP3 command.
		/// </summary>
		/// <param name="context">The session context.</param>
		/// <param name="argument">The argument string (may be null or empty).</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		Task ExecuteAsync(Pop3SessionContext context, string argument, CancellationToken cancellationToken);
	}
}
