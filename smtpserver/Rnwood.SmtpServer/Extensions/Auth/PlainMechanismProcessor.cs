// <copyright file="PlainMechanismProcessor.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer.Extensions.Auth
{
	using System.Threading.Tasks;

	/// <summary>
	/// Defines the <see cref="PlainMechanismProcessor" />.
	/// </summary>
	public class PlainMechanismProcessor : AuthMechanismProcessor, IAuthMechanismProcessor
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PlainMechanismProcessor"/> class.
		/// </summary>
		/// <param name="connection">The connection<see cref="IConnection"/>.</param>
		public PlainMechanismProcessor(IConnection connection)
			: base(connection)
		{
		}

		/// <summary>
		/// Defines the States.
		/// </summary>
		public enum ProcessingState
		{
			/// <summary>
			/// Defines the Initial
			/// </summary>
			Initial,

			/// <summary>
			/// Defines the AwaitingResponse
			/// </summary>
			AwaitingResponse,
		}

		/// <summary>
		/// Gets or sets the State.
		/// </summary>
		private ProcessingState State { get; set; }

		/// <inheritdoc/>
		public override async Task<AuthMechanismProcessorStatus> ProcessResponse(string data)
		{
			if (string.IsNullOrEmpty(data))
			{
				if (this.State == ProcessingState.AwaitingResponse)
				{
					throw new SmtpServerException(new SmtpResponse(
						StandardSmtpResponseCode.AuthenticationFailure,
						"Missing auth data"));
				}

				await this.Connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.AuthenticationContinue, string.Empty)).ConfigureAwait(false);
				this.State = ProcessingState.AwaitingResponse;
				return AuthMechanismProcessorStatus.Continue;
			}

			string decodedData = DecodeBase64(data);
			string[] decodedDataParts = decodedData.Split('\0');

			if (decodedDataParts.Length != 3)
			{
				throw new SmtpServerException(new SmtpResponse(
					StandardSmtpResponseCode.AuthenticationFailure,
					"Auth data in incorrect format"));
			}

			string username = decodedDataParts[1];
			string password = decodedDataParts[2];

			this.Credentials = new PlainAuthenticationCredentials(username, password);

			AuthenticationResult result =
				await this.Connection.Server.Behaviour.ValidateAuthenticationCredentials(this.Connection, this.Credentials).ConfigureAwait(false);
			switch (result)
			{
				case AuthenticationResult.Success:
					return AuthMechanismProcessorStatus.Success;

				default:
					return AuthMechanismProcessorStatus.Failed;
			}
		}
	}
}
