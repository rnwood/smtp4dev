// <copyright file="AuthMechanismProcessor.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Text;
using System.Threading.Tasks;

namespace Rnwood.SmtpServer.Extensions.Auth;

/// <summary>
///     Defines the <see cref="AuthMechanismProcessor" />.
/// </summary>
public abstract class AuthMechanismProcessor : IAuthMechanismProcessor
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AuthMechanismProcessor" /> class.
    /// </summary>
    /// <param name="connection">The connection<see cref="IConnection" />.</param>
    protected AuthMechanismProcessor(IConnection connection) => Connection = connection;

    /// <summary>
    ///     Gets the connection this processor is for.
    /// </summary>
    /// <value>
    ///     The connection.
    /// </value>
    public IConnection Connection { get; private set; }

    /// <inheritdoc />
    public IAuthenticationCredentials Credentials { get; protected set; }

    /// <inheritdoc />
    public abstract Task<AuthMechanismProcessorStatus> ProcessResponse(string data);

    /// <summary>
    ///     Decodes a base64 encoded ASCII string and throws an exception if invalid.
    /// </summary>
    /// <param name="data">The data.</param>
    /// <returns>The decoded ASCII string.</returns>
    /// <exception cref="BadBase64Exception">If the base64 encoded string is invalid.</exception>
    protected static string DecodeBase64(string data)
    {
        try
        {
            return Encoding.ASCII.GetString(Convert.FromBase64String(data));
        }
        catch (FormatException)
        {
            throw new BadBase64Exception(new SmtpResponse(
                StandardSmtpResponseCode.AuthenticationFailure,
                "Bad Base64 data"));
        }
    }

    /// <summary>
    /// Base64 encodes the provided values using ASCII encoding
    /// </summary>
    /// <param name="asciiString"></param>
    /// <returns></returns>
    protected static string EncodeBase64(string asciiString) =>
        Convert.ToBase64String(Encoding.ASCII.GetBytes(asciiString));
}
