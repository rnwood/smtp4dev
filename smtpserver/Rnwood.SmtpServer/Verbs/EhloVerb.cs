// <copyright file="EhloVerb.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer
{
    using System.Text;
    using System.Threading.Tasks;
    using Rnwood.SmtpServer.Verbs;

    /// <summary>
    /// Defines the <see cref="EhloVerb" />
    /// </summary>
    public class EhloVerb : IVerb
    {
        /// <inheritdoc/>
        public async Task Process(IConnection connection, SmtpCommand command)
        {
            if (!string.IsNullOrEmpty(connection.Session.ClientName))
            {
                await connection.WriteResponse(
                    new SmtpResponse(
                        StandardSmtpResponseCode.BadSequenceOfCommands,
                        "You already said HELO")).ConfigureAwait(false);
                return;
            }

            connection.Session.ClientName = command.ArgumentsText ?? string.Empty;

            StringBuilder text = new StringBuilder();
            text.AppendLine("Nice to meet you.");

            foreach (Extensions.IExtensionProcessor extensionProcessor in connection.ExtensionProcessors)
            {
                foreach (string ehloKeyword in await extensionProcessor.GetEHLOKeywords().ConfigureAwait(false))
                {
                    text.AppendLine(ehloKeyword);
                }
            }

            await connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.OK, text.ToString().TrimEnd())).ConfigureAwait(false);
        }
    }
}
