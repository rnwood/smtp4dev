// <copyright file="EhloVerb.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System.Threading.Tasks;
using Rnwood.SmtpServer.Extensions;
using Rnwood.SmtpServer.Verbs;

namespace Rnwood.SmtpServer;

/// <summary>
///     Defines the <see cref="EhloVerb" />.
/// </summary>
public class EhloVerb : IVerb
{
    /// <inheritdoc />
    public async Task Process(IConnection connection, SmtpCommand command)
    {
        connection.Session.ClientName = command.ArgumentsText ?? string.Empty;

        SmtpStringBuilder text = new SmtpStringBuilder();
        text.AppendLine("Nice to meet you.");

        foreach (IExtensionProcessor extensionProcessor in connection.ExtensionProcessors)
        {
            foreach (string ehloKeyword in await extensionProcessor.GetEHLOKeywords().ConfigureAwait(false))
            {
                text.AppendLine(ehloKeyword);
            }
        }

        await connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.OK, text.ToString().TrimEnd()))
            .ConfigureAwait(false);
    }
}
