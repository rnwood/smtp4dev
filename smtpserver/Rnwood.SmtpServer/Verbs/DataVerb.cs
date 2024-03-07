// <copyright file="DataVerb.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System.Linq;

namespace Rnwood.SmtpServer
{
	using System;
	using System.IO;
	using System.Text;
	using System.Threading.Tasks;
	using Rnwood.SmtpServer.Verbs;

	/// <summary>
	/// Defines the <see cref="DataVerb" />.
	/// </summary>
	public class DataVerb : IVerb
	{
		private readonly byte[] CRLF_BYTES = Encoding.ASCII.GetBytes("\r\n");
		
		/// <inheritdoc/>
		public virtual async Task Process(IConnection connection, SmtpCommand command)
		{
			if (connection.CurrentMessage == null)
			{
				await connection.WriteResponse(new SmtpResponse(
					StandardSmtpResponseCode.BadSequenceOfCommands,
					"Bad sequence of commands")).ConfigureAwait(false);
				return;
			}

			connection.CurrentMessage.SecureConnection = connection.Session.SecureConnection;

			await connection.WriteResponse(new SmtpResponse(
				StandardSmtpResponseCode.StartMailInputEndWithDot,
				"End message with period")).ConfigureAwait(false);

			using (Stream messageStream = await connection.CurrentMessage.WriteData().ConfigureAwait(false))
			{
				bool firstLine = true;
				long messageSize = 0;

				do
				{
					byte[] data = await connection.ReadLineBytes().ConfigureAwait(false);

					if (!Encoding.ASCII.GetBytes(".").SequenceEqual(data))
					{
						data = this.ProcessLine(data);

						if (!firstLine)
						{
							messageSize += CRLF_BYTES.Length;
							messageStream.Write(CRLF_BYTES, 0, CRLF_BYTES.Length);
						}

						messageSize += data.Length;
						messageStream.Write(data, 0, data.Length);
					}
					else
					{
						break;
					}

					firstLine = false;
				}
				while (true);

				await messageStream.FlushAsync().ConfigureAwait(false);
				long? maxMessageSize =
					await connection.Server.Behaviour.GetMaximumMessageSize(connection).ConfigureAwait(false);

				if (maxMessageSize.HasValue && messageSize > maxMessageSize.Value)
				{
					await connection.WriteResponse(
						new SmtpResponse(
							StandardSmtpResponseCode.ExceededStorageAllocation,
							"Message exceeds fixed size limit")).ConfigureAwait(false);
				}
				else
				{
					messageStream.Dispose();
					await connection.Server.Behaviour.OnMessageCompleted(connection).ConfigureAwait(false);
					await connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.OK, "Mail accepted")).ConfigureAwait(false);
					await connection.CommitMessage().ConfigureAwait(false);
				}
			}
		}

		/// <summary>
		/// Processes a line of data from the client removing the escaping of the special end of message character.
		/// </summary>
		/// <param name="data">The line.</param>
		/// <returns>The line of data without escaping of the . character.</returns>
		protected virtual byte[] ProcessLine(byte[] data)
		{
			// Remove escaping of end of message character
			if (data.Length > 0 && data[0] == '.')
			{
				return data.Skip(1).ToArray();
			}

			return data;
		}
	}
}
