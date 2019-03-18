// <copyright file="DataVerb.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Rnwood.SmtpServer.Verbs;

    /// <summary>
    /// Defines the <see cref="DataVerb" />
    /// </summary>
    public class DataVerb : IVerb
    {
        /// <inheritdoc/>
        public virtual async Task Process(IConnection connection, SmtpCommand command)
        {
            if (connection.CurrentMessage == null)
            {
                await connection.WriteResponse(new SmtpResponse(
                    StandardSmtpResponseCode.BadSequenceOfCommands,
                                                                   "Bad sequence of commands")).ConfigureAwait(false);
                return;
            }

            connection.CurrentMessage.SecureConnection = connection.Session.SecureConnection;

            connection.SetReaderEncoding(connection.CurrentMessage.EightBitTransport ?
                new UTF8Encoding(false, true) :
                await connection.Server.Behaviour.GetDefaultMessageEncoding(connection).ConfigureAwait(false));

            try
            {
                await connection.WriteResponse(new SmtpResponse(
                    StandardSmtpResponseCode.StartMailInputEndWithDot,
                                                                   "End message with period")).ConfigureAwait(false);

                using (SmtpStreamWriter writer = new SmtpStreamWriter(await connection.CurrentMessage.WriteData().ConfigureAwait(false), false))
                {
                    bool firstLine = true;

                    do
                    {
                        string line = await connection.ReadLine().ConfigureAwait(false);

                        if (line != ".")
                        {
                            line = this.ProcessLine(line);

                            if (!firstLine)
                            {
                                writer.Write("\r\n");
                            }

                            writer.Write(line);
                        }
                        else
                        {
                            break;
                        }

                        firstLine = false;
                    }
                    while (true);

                    writer.Flush();
                    long? maxMessageSize =
                        await connection.Server.Behaviour.GetMaximumMessageSize(connection).ConfigureAwait(false);

                    if (maxMessageSize.HasValue && writer.BaseStream.Length > maxMessageSize.Value)
                    {
                        await connection.WriteResponse(
                            new SmtpResponse(
                                StandardSmtpResponseCode.ExceededStorageAllocation,
                                             "Message exceeds fixed size limit")).ConfigureAwait(false);
                    }
                    else
                    {
                        writer.Dispose();
                        await connection.Server.Behaviour.OnMessageCompleted(connection).ConfigureAwait(false);
                        await connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.OK, "Mail accepted")).ConfigureAwait(false);
                        await connection.CommitMessage().ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                connection.SetReaderEncoding(new UTF8Encoding(false, true));
            }
        }

        /// <summary>
        /// Processes a line of data from the client removing the escaping of the special end of message character.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns>The line of data without escaping of the . character</returns>
        protected virtual string ProcessLine(string line)
        {
            // Remove escaping of end of message character
            if (line.StartsWith(".", StringComparison.Ordinal))
            {
                line = line.Remove(0, 1);
            }

            return line;
        }
    }
}
