// <copyright file="SizeExtension.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Rnwood.SmtpServer.Extensions;

/// <summary>
///     Defines the <see cref="SizeExtension" />.
/// </summary>
public class SizeExtension : IExtension
{
    /// <inheritdoc />
    public IExtensionProcessor CreateExtensionProcessor(IConnection connection) =>
        new SizeExtensionProcessor(connection);

    /// <summary>
    ///     Defines the <see cref="SizeExtensionProcessor" />.
    /// </summary>
    private sealed class SizeExtensionProcessor : IExtensionProcessor, IParameterProcessor
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SizeExtensionProcessor" /> class.
        /// </summary>
        /// <param name="connection">The connection<see cref="IConnection" />.</param>
        public SizeExtensionProcessor(IConnection connection)
        {
            Connection = connection;
            Connection.MailVerb.FromSubVerb.ParameterProcessorMap.SetProcessor("SIZE", this);
        }

        /// <summary>
        ///     Gets the connection this processor is for.
        /// </summary>
        /// <value>
        ///     The connection.
        /// </value>
        public IConnection Connection { get; }

        /// <inheritdoc />
        public async Task<string[]> GetEHLOKeywords()
        {
            long? maxMessageSize =
                await Connection.Server.Options.GetMaximumMessageSize(Connection).ConfigureAwait(false);

            if (maxMessageSize.HasValue)
            {
                return new[] { string.Format(CultureInfo.InvariantCulture, "SIZE={0}", maxMessageSize.Value) };
            }

            return new[] { "SIZE" };
        }

        /// <inheritdoc />
        public async Task SetParameter(IConnection connection, string key, string value)
        {
            if (key.Equals("SIZE", StringComparison.OrdinalIgnoreCase))
            {
                if (long.TryParse(value, out long messageSize) && messageSize > 0)
                {
                    long? maxMessageSize = await Connection.Server.Options.GetMaximumMessageSize(Connection)
                        .ConfigureAwait(false);
                    connection.CurrentMessage.DeclaredMessageSize = messageSize;

                    if (maxMessageSize.HasValue && messageSize > maxMessageSize)
                    {
                        throw new SmtpServerException(
                            new SmtpResponse(
                                StandardSmtpResponseCode.ExceededStorageAllocation,
                                "Message exceeds fixes size limit"));
                    }
                }
                else
                {
                    throw new SmtpServerException(new SmtpResponse(
                        StandardSmtpResponseCode.SyntaxErrorInCommandArguments, "Bad message size specified"));
                }
            }
        }
    }
}
