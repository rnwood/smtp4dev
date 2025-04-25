// <copyright file="SizeExtension.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Rnwood.SmtpServer.Extensions;

/// <summary>
///     Defines the <see cref="SizeExtension" /> representing the SMTP Service Extension for Size Declaration.
/// </summary>
/// <seealso href="https://datatracker.ietf.org/doc/html/rfc1870"/>
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

            // If the max message size is non-negative, place it in the optional parameter of the SIZE keyword
            bool shouldReportMaximumMessageSize = maxMessageSize.HasValue && maxMessageSize >= 0;
            if (shouldReportMaximumMessageSize)
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
                    connection.CurrentMessage.DeclaredMessageSize = messageSize;
                    
                    long? maxMessageSize = await Connection.Server.Options.GetMaximumMessageSize(Connection)
                        .ConfigureAwait(false);

                    bool shouldValidateMessageSize = maxMessageSize.HasValue && maxMessageSize > 0;
                    if (shouldValidateMessageSize && messageSize > maxMessageSize)
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
