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
			// Only advertise STLS for STARTTLS when Pop3TlsMode is explicitly configured for POP3; implicit TLS doesn't use the STLS command
			if (context.Options.Pop3TlsMode == TlsMode.StartTls)
			{
				context.Writer.Write("STLS\r\n");
			}
			context.Writer.Write(".\r\n");
			var flushTask = context.Writer.FlushAsync();
			return flushTask;
		}
	}
}
