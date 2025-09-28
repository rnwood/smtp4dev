namespace Rnwood.Smtp4dev.Server.Pop3.CommandHandlers
{
	using System;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Linq;
	using Microsoft.Extensions.Logging;
	using Rnwood.Smtp4dev.Server.Pop3;

	internal class CapaCommand : ICommandHandler
	{
		public Task ExecuteAsync(Pop3SessionContext context, string argument, CancellationToken cancellationToken)
		{
			// Advertise basic capabilities. STLS and UIDL advertised depending on options.
			context.Writer.Write("+OK Capability list follows\r\n");
			context.Writer.Write("USER\r\n");
			context.Writer.Write("TOP\r\n");
			context.Writer.Write("UIDL\r\n");
			var effectivePop3 = context.Options.Pop3TlsMode ?? context.Options.TlsMode;
			if (effectivePop3 != TlsMode.None)
			{
				context.Writer.Write("STLS\r\n");
			}
			context.Writer.Write(".\r\n");
			var flushTask = context.Writer.FlushAsync();
			return flushTask;
		}
	}
}
