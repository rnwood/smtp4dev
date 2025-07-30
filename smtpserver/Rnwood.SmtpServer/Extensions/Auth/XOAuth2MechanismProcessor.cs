// <copyright file="XOauth2MechanismProcessor.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System.Threading.Tasks;

namespace Rnwood.SmtpServer.Extensions.Auth;

/// <summary>
///     Defines the <see cref="XOauth2MechanismProcessor" />.
/// </summary>
public class XOauth2MechanismProcessor : AuthMechanismProcessor, IAuthMechanismProcessor
{
    private const string SCHEMA_PREFIX = "auth=Bearer ";
    private const string USER_PREFIX = "user=";

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
    public XOauth2MechanismProcessor(IConnection connection)
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
        string[] decodedDataParts = decodedData.Split((char)1);

        string username = null;
        string accessToken = null;
        if (decodedDataParts.Length == 4)
        {
            string usernamePart = decodedDataParts[0];
            if (usernamePart.StartsWith(USER_PREFIX) && usernamePart.Length > USER_PREFIX.Length)
            {
                username = usernamePart[USER_PREFIX.Length..];
            }

            string accessTokenPart = decodedDataParts[1];
            if (accessTokenPart.StartsWith(SCHEMA_PREFIX) && accessTokenPart.Length > SCHEMA_PREFIX.Length)
            {
                accessToken = accessTokenPart[SCHEMA_PREFIX.Length..];
            }
        }

        if (username == null || accessToken == null)
        {
            throw new SmtpServerException(new SmtpResponse(
                StandardSmtpResponseCode.AuthenticationFailure,
                "Auth data in incorrect format"));
        }


        Credentials = new XOAuth2AuthenticationCredentials(username, accessToken);

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
