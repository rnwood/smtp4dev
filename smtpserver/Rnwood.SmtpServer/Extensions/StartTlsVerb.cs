// <copyright file="StartTlsVerb.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer.Extensions
{
	using System.Net.Security;
	using System.Security.Authentication;
	using System.Security.Cryptography.X509Certificates;
	using System.Threading.Tasks;
	using Rnwood.SmtpServer.Verbs;

	/// <summary>
	/// Defines the <see cref="StartTlsVerb" />.
	/// </summary>
	public class StartTlsVerb : IVerb
	{
		/// <inheritdoc/>
		public async Task Process(IConnection connection, SmtpCommand command)
		{
			X509Certificate certificate = await connection.Server.Behaviour.GetSSLCertificate(connection).ConfigureAwait(false);

			if (certificate == null)
			{
				await connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.CommandNotImplemented, "TLS configuration error - no certificate")).ConfigureAwait(false);
				return;
			}

			await connection.WriteResponse(new SmtpResponse(
				StandardSmtpResponseCode.ServiceReady,
				"Ready to start TLS")).ConfigureAwait(false);

#pragma warning disable 0618
			var sslProtos = SslProtocols.Ssl2 | SslProtocols.Ssl3 | SslProtocols.Tls;
#pragma warning restore 0618

			await connection.ApplyStreamFilter(async stream =>
													 {
														 SslStream sslStream = new SslStream(stream);
														 await sslStream.AuthenticateAsServerAsync(
															 certificate,
															 false,
															 sslProtos,
															 false).ConfigureAwait(false);
														 return sslStream;
													 }).ConfigureAwait(false);

			connection.Session.SecureConnection = true;
		}
	}
}
