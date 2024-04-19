// <copyright file="StartTlsExtension.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Threading.Tasks;

namespace Rnwood.SmtpServer.Extensions;

/// <summary>
///     Defines the <see cref="StartTlsExtension" />.
/// </summary>
public class StartTlsExtension : IExtension
{
    /// <inheritdoc />
    public IExtensionProcessor CreateExtensionProcessor(IConnection connection) =>
        new StartTlsExtensionProcessor(connection);

    /// <summary>
    ///     Defines the <see cref="StartTlsExtensionProcessor" />.
    /// </summary>
    private sealed class StartTlsExtensionProcessor : IExtensionProcessor
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="StartTlsExtensionProcessor" /> class.
        /// </summary>
        /// <param name="connection">The connection<see cref="IConnection" />.</param>
        public StartTlsExtensionProcessor(IConnection connection)
        {
            Connection = connection;
            Connection.VerbMap.SetVerbProcessor("STARTTLS", new StartTlsVerb());
        }

        /// <summary>
        ///     Gets the connection this processor is for.
        /// </summary>
        /// <value>
        ///     The connection.
        /// </value>
        public IConnection Connection { get; }

        /// <inheritdoc />
        public Task<string[]> GetEHLOKeywords()
        {
            string[] result;

            if (!Connection.Session.SecureConnection)
            {
                result = new[] { "STARTTLS" };
            }
            else
            {
                result = Array.Empty<string>();
            }

            return Task.FromResult(result);
        }
    }
}
