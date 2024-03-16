// <copyright file="RsetVerb.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System.Threading.Tasks;

namespace Rnwood.SmtpServer.Verbs;

/// <summary>
///     Defines the <see cref="RsetVerb" />.
/// </summary>
public class RsetVerb : IVerb
{
    /// <inheritdoc />
    public async Task Process(IConnection connection, SmtpCommand command)
    {
        await connection.AbortMessage().ConfigureAwait(false);
        await connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.OK, "Rset completed"))
            .ConfigureAwait(false);
    }
}
