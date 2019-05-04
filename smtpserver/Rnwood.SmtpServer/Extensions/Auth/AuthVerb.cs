// <copyright file="AuthVerb.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer.Extensions.Auth
{
	using System.Linq;
	using System.Threading.Tasks;
	using Rnwood.SmtpServer.Verbs;

	/// <summary>
	/// Defines the <see cref="AuthVerb" />.
	/// </summary>
	public class AuthVerb : IVerb
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AuthVerb"/> class.
		/// </summary>
		/// <param name="authExtensionProcessor">The authExtensionProcessor<see cref="AuthExtensionProcessor"/>.</param>
		public AuthVerb(AuthExtensionProcessor authExtensionProcessor)
		{
			this.AuthExtensionProcessor = authExtensionProcessor;
		}

		/// <summary>
		/// Gets the AuthExtensionProcessor.
		/// </summary>
		public AuthExtensionProcessor AuthExtensionProcessor { get; private set; }

		/// <inheritdoc/>
		public async Task Process(IConnection connection, SmtpCommand command)
		{
			ArgumentsParser argumentsParser = new ArgumentsParser(command.ArgumentsText);

			if (argumentsParser.Arguments.Count > 0)
			{
				if (connection.Session.Authenticated)
				{
					throw new SmtpServerException(new SmtpResponse(
						StandardSmtpResponseCode.BadSequenceOfCommands,
						"Already authenticated"));
				}

				string mechanismId = argumentsParser.Arguments.First();
				IAuthMechanism mechanism = this.AuthExtensionProcessor.MechanismMap.Get(mechanismId);

				if (mechanism == null)
				{
					throw new SmtpServerException(
						new SmtpResponse(
							StandardSmtpResponseCode.CommandParameterNotImplemented,
							"Specified AUTH mechanism not supported"));
				}

				if (!await this.AuthExtensionProcessor.IsMechanismEnabled(mechanism).ConfigureAwait(false))
				{
					throw new SmtpServerException(
						new SmtpResponse(
							StandardSmtpResponseCode.AuthenticationFailure,
							"Specified AUTH mechanism not allowed right now (might require secure connection etc)"));
				}

				IAuthMechanismProcessor authMechanismProcessor =
					mechanism.CreateAuthMechanismProcessor(connection);

				string initialData = null;
				if (argumentsParser.Arguments.Count > 1)
				{
					initialData = string.Join(" ", argumentsParser.Arguments.Skip(1).ToArray());
				}

				AuthMechanismProcessorStatus status =
					await authMechanismProcessor.ProcessResponse(initialData).ConfigureAwait(false);
				while (status == AuthMechanismProcessorStatus.Continue)
				{
					string response = await connection.ReadLine().ConfigureAwait(false);

					if (response == "*")
					{
						await connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.SyntaxErrorInCommandArguments, "Authentication aborted")).ConfigureAwait(false);
						return;
					}

					status = await authMechanismProcessor.ProcessResponse(response).ConfigureAwait(false);
				}

				if (status == AuthMechanismProcessorStatus.Success)
				{
					await connection.WriteResponse(new SmtpResponse(
						StandardSmtpResponseCode.AuthenticationOK,
						"Authenticated OK")).ConfigureAwait(false);
					connection.Session.Authenticated = true;
					connection.Session.AuthenticationCredentials = authMechanismProcessor.Credentials;
				}
				else
				{
					await connection.WriteResponse(new SmtpResponse(
						StandardSmtpResponseCode.AuthenticationFailure,
						"Authentication failure")).ConfigureAwait(false);
				}
			}
			else
			{
				throw new SmtpServerException(new SmtpResponse(
					StandardSmtpResponseCode.SyntaxErrorInCommandArguments,
					"Must specify AUTH mechanism as a parameter"));
			}
		}
	}
}
