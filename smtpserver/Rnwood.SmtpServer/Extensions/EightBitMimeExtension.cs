// <copyright file="EightBitMimeExtension.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Threading.Tasks;

namespace Rnwood.SmtpServer.Extensions;

/// <summary>
///     Defines the <see cref="EightBitMimeExtension" />.
/// </summary>
public class EightBitMimeExtension : IExtension
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="EightBitMimeExtension" /> class.
    /// </summary>
    public EightBitMimeExtension()
    {
    }

    /// <inheritdoc />
    public IExtensionProcessor CreateExtensionProcessor(IConnection connection) =>
        new EightBitMimeExtensionProcessor(connection);

    /// <summary>
    ///     Defines the <see cref="EightBitMimeExtensionProcessor" />.
    /// </summary>
    private sealed class EightBitMimeExtensionProcessor : ExtensionProcessor
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="EightBitMimeExtensionProcessor" /> class.
        /// </summary>
        /// <param name="connection">The connection<see cref="IConnection" />.</param>
        public EightBitMimeExtensionProcessor(IConnection connection)
            : base(connection)
        {
            MailVerb mailVerbProcessor = connection.MailVerb;
            MailFromVerb mailFromProcessor = mailVerbProcessor.FromSubVerb;
            mailFromProcessor.ParameterProcessorMap.SetProcessor("BODY", new EightBitMimeBodyParameter());
        }

        /// <inheritdoc />
        public override Task<string[]> GetEHLOKeywords() => Task.FromResult(new[] { "8BITMIME" });
    }

    private sealed class EightBitMimeBodyParameter : IParameterProcessor
    {
        public Task SetParameter(IConnection connection, string key, string value)
        {
            if (key.Equals("BODY", StringComparison.OrdinalIgnoreCase))
            {
                if (value.Equals("8BITMIME", StringComparison.CurrentCultureIgnoreCase))
                {
                    connection.CurrentMessage.EightBitTransport = true;
                }
                else if (value.Equals("7BIT", StringComparison.OrdinalIgnoreCase))
                {
                    connection.CurrentMessage.EightBitTransport = false;
                }
                else
                {
                    throw new SmtpServerException(
                        new SmtpResponse(
                            StandardSmtpResponseCode.SyntaxErrorInCommandArguments,
                            "BODY parameter value invalid - must be either 7BIT or 8BITMIME"));
                }
            }

            return Task.CompletedTask;
        }
    }
}
