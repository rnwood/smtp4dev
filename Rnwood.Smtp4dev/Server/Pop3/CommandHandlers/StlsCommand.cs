namespace Rnwood.Smtp4dev.Server.Pop3.CommandHandlers
{
	using System;
	using System.IO;
	using System.Net.Security;
	using System.Security.Authentication;
	using System.Security.Cryptography.X509Certificates;
	using System.Threading;
	using System.Threading.Tasks;
	using Microsoft.Extensions.Logging;
	using Rnwood.Smtp4dev.Server.Pop3;

	internal class StlsCommand : ICommandHandler
	{
		private readonly ILogger logger;

		public StlsCommand(ILogger logger = null)
		{
			this.logger = logger;
		}

		public async Task ExecuteAsync(Pop3SessionContext context, string argument, CancellationToken cancellationToken)
		{
			// Only allow STLS if POP3 is configured to use STARTTLS
			if (context.Options.Pop3TlsMode != TlsMode.StartTls)
			{
				await context.WriteLineAsync("-ERR STLS not supported");
				return;
			}

			await context.WriteLineAsync("+OK Begin TLS negotiation");

			// Upgrade stream to SSL
			var ssl = new SslStream(context.Stream, leaveInnerStreamOpen: false);
			try
			{
				var cert = Rnwood.Smtp4dev.Server.CertificateHelper.GetTlsCertificate(context.Options, Serilog.Log.ForContext<StlsCommand>());
				if (cert == null)
				{
					await context.WriteLineAsync("-ERR TLS certificate unavailable");
					return;
				}

				var sslOptions = new SslServerAuthenticationOptions
				{
					ServerCertificate = cert,
					EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
					CertificateRevocationCheckMode = System.Security.Cryptography.X509Certificates.X509RevocationMode.NoCheck
				};
				await ssl.AuthenticateAsServerAsync(sslOptions, cancellationToken);

				context.Stream = ssl; // update session stream to SSL stream
				context.Writer = new StreamWriter(ssl, System.Text.Encoding.ASCII) { NewLine = "\r\n", AutoFlush = true };
				context.Reader = new StreamReader(ssl, System.Text.Encoding.ASCII);
				context.IsSecure = true;
			}
			catch (Exception ex)
			{
				logger?.LogWarning(ex, "STLS handshake failed");
				await context.WriteLineAsync("-ERR TLS handshake failed");
			}
		}
	}
}
