// <copyright file="EightBitMimeDataVerb.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer.Extensions
{
    using System;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="EightBitMimeDataVerb" />
    /// </summary>
    public class EightBitMimeDataVerb : DataVerb, IParameterProcessor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EightBitMimeDataVerb"/> class.
        /// </summary>
        public EightBitMimeDataVerb()
        {
        }

        /// <inheritdoc/>
        public override async Task Process(IConnection connection, SmtpCommand command)
        {
            if (connection.CurrentMessage != null && connection.CurrentMessage.EightBitTransport)
            {
                connection.SetReaderEncoding(Encoding.UTF8);
            }

            try
            {
                await base.Process(connection, command).ConfigureAwait(false);
            }
            finally
            {
                await connection.SetReaderEncodingToDefault().ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
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
