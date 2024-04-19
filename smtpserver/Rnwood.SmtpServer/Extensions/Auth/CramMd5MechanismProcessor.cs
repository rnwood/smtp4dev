// <copyright file="CramMd5MechanismProcessor.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace Rnwood.SmtpServer.Extensions.Auth;

/// <summary>
///     Defines the <see cref="CramMd5MechanismProcessor" />.
/// </summary>
public class CramMd5MechanismProcessor : AuthMechanismProcessor
{
    /// <summary>
    ///     Defines the dateTimeProvider.
    /// </summary>
    private readonly ICurrentDateTimeProvider dateTimeProvider;

    /// <summary>
    ///     Defines the random.
    /// </summary>
    private readonly IRandomIntegerGenerator random;

    /// <summary>
    ///     Defines the challenge.
    /// </summary>
    private string challenge;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CramMd5MechanismProcessor" /> class.
    /// </summary>
    /// <param name="connection">The connection<see cref="IConnection" />.</param>
    /// <param name="random">The random<see cref="IRandomIntegerGenerator" />.</param>
    /// <param name="dateTimeProvider">The dateTimeProvider<see cref="ICurrentDateTimeProvider" />.</param>
    public CramMd5MechanismProcessor(IConnection connection, IRandomIntegerGenerator random,
        ICurrentDateTimeProvider dateTimeProvider)
        : base(connection)
    {
        this.random = random;
        this.dateTimeProvider = dateTimeProvider;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="CramMd5MechanismProcessor" /> class.
    /// </summary>
    /// <param name="connection">The connection<see cref="IConnection" />.</param>
    /// <param name="random">The random<see cref="IRandomIntegerGenerator" />.</param>
    /// <param name="dateTimeProvider">The dateTimeProvider<see cref="ICurrentDateTimeProvider" />.</param>
    /// <param name="challenge">The challenge<see cref="string" />.</param>
    public CramMd5MechanismProcessor(IConnection connection, IRandomIntegerGenerator random,
        ICurrentDateTimeProvider dateTimeProvider, string challenge)
        : this(connection, random, dateTimeProvider) =>
        this.challenge = challenge;

    /// <inheritdoc />
    public override async Task<AuthMechanismProcessorStatus> ProcessResponse(string data)
    {
        if (this.challenge == null)
        {
            StringBuilder challengeStringBuilder = new StringBuilder();
            challengeStringBuilder.Append(random.GenerateRandomInteger(0, short.MaxValue));
            challengeStringBuilder.Append(".");
            challengeStringBuilder.Append(dateTimeProvider.GetCurrentDateTime().Ticks.ToString(CultureInfo.InvariantCulture));
            challengeStringBuilder.Append("@");
            challengeStringBuilder.Append(Connection.Server.Options.DomainName);
            this.challenge = challengeStringBuilder.ToString();

            string base64Challenge = Convert.ToBase64String(Encoding.ASCII.GetBytes(challengeStringBuilder.ToString()));
            await Connection.WriteResponse(new SmtpResponse(
                StandardSmtpResponseCode.AuthenticationContinue,
                base64Challenge)).ConfigureAwait(false);
            return AuthMechanismProcessorStatus.Continue;
        }

        string response = DecodeBase64(data);
        string[] responseparts = response.Split(' ');

        if (responseparts.Length != 2)
        {
            throw new SmtpServerException(new SmtpResponse(
                StandardSmtpResponseCode.AuthenticationFailure,
                "Response in incorrect format - should be USERNAME RESPONSE"));
        }

        string username = responseparts[0];
        string hash = responseparts[1];

        Credentials = new CramMd5AuthenticationCredentials(username, challenge, hash);

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
