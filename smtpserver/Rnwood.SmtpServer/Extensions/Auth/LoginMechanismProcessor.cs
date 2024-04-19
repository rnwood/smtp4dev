// <copyright file="LoginMechanismProcessor.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Text;
using System.Threading.Tasks;

namespace Rnwood.SmtpServer.Extensions.Auth;

/// <summary>
///     Defines the <see cref="LoginMechanismProcessor" />.
/// </summary>
public class LoginMechanismProcessor : AuthMechanismProcessor
{
    /// <summary>
    ///     Defines the username.
    /// </summary>
    private string username;

    /// <summary>
    ///     Initializes a new instance of the <see cref="LoginMechanismProcessor" /> class.
    /// </summary>
    /// <param name="connection">The connection<see cref="IConnection" />.</param>
    public LoginMechanismProcessor(IConnection connection)
        : base(connection) =>
        State = States.Initial;

    private States State { get; set; }

    /// <inheritdoc />
    public override async Task<AuthMechanismProcessorStatus> ProcessResponse(string data)
    {
        if (State == States.Initial && data != null)
        {
            State = States.WaitingForUsername;
        }

        switch (State)
        {
            case States.Initial:
                await Connection.WriteResponse(new SmtpResponse(
                    StandardSmtpResponseCode.AuthenticationContinue,
                    Convert.ToBase64String(
                        Encoding.ASCII.GetBytes("Username:")))).ConfigureAwait(false);
                State = States.WaitingForUsername;
                return AuthMechanismProcessorStatus.Continue;

            case States.WaitingForUsername:

                username = DecodeBase64(data);

                await Connection.WriteResponse(new SmtpResponse(
                    StandardSmtpResponseCode.AuthenticationContinue,
                    Convert.ToBase64String(
                        Encoding.ASCII.GetBytes("Password:")))).ConfigureAwait(false);
                State = States.WaitingForPassword;
                return AuthMechanismProcessorStatus.Continue;

            case States.WaitingForPassword:
                string password = DecodeBase64(data);
                State = States.Completed;

                Credentials = new LoginAuthenticationCredentials(username, password);

                AuthenticationResult result =
                    await Connection.Server.Options.ValidateAuthenticationCredentials(
                        Connection,
                        Credentials).ConfigureAwait(false);

                switch (result)
                {
                    case AuthenticationResult.Success:
                        return AuthMechanismProcessorStatus.Success;

                    default:
                        return AuthMechanismProcessorStatus.Failed;
                }

            default:
                throw new NotImplementedException();
        }
    }

    /// <summary>
    ///     Defines the States.
    /// </summary>
    private enum States
    {
        /// <summary>
        ///     Defines the Initial
        /// </summary>
        Initial,

        /// <summary>
        ///     Defines the WaitingForUsername
        /// </summary>
        WaitingForUsername,

        /// <summary>
        ///     Defines the WaitingForPassword
        /// </summary>
        WaitingForPassword,

        /// <summary>
        ///     Defines the Completed
        /// </summary>
        Completed
    }
}
