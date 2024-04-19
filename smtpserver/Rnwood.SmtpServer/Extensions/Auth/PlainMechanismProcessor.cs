// <copyright file="PlainMechanismProcessor.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System.Threading.Tasks;

namespace Rnwood.SmtpServer.Extensions.Auth;

/// <summary>
///     Defines the <see cref="PlainMechanismProcessor" />.
/// </summary>
public class PlainMechanismProcessor : AuthMechanismProcessor, IAuthMechanismProcessor
{
    /// <summary>
    ///     Defines the States.
    /// </summary>
    public enum ProcessingState
    {
        /// <summary>
        ///     Defines the Initial
        /// </summary>
        Initial,

        /// <summary>
        ///     Defines the AwaitingResponse
        /// </summary>
        AwaitingResponse
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="PlainMechanismProcessor" /> class.
    /// </summary>
    /// <param name="connection">The connection<see cref="IConnection" />.</param>
    public PlainMechanismProcessor(IConnection connection)
        : base(connection)
    {
    }

    /// <summary>
    ///     Gets or sets the State.
    /// </summary>
    private ProcessingState State { get; set; }

    /// <inheritdoc />
    public override async Task<AuthMechanismProcessorStatus> ProcessResponse(string data)
    {
        if (string.IsNullOrEmpty(data))
        {
            if (State == ProcessingState.AwaitingResponse)
            {
                throw new SmtpServerException(new SmtpResponse(
                    StandardSmtpResponseCode.AuthenticationFailure,
                    "Missing auth data"));
            }

            await Connection
                .WriteResponse(new SmtpResponse(StandardSmtpResponseCode.AuthenticationContinue, string.Empty))
                .ConfigureAwait(false);
            State = ProcessingState.AwaitingResponse;
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

        Credentials = new PlainAuthenticationCredentials(username, password);

        AuthenticationResult result =
            await Connection.Server.Options.ValidateAuthenticationCredentials(Connection, Credentials)
                .ConfigureAwait(false);
        switch (result)
        {
            case AuthenticationResult.Success:
                return AuthMechanismProcessorStatus.Success;

            default:
                return AuthMechanismProcessorStatus.Failed;
        }
    }
}
