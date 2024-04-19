// <copyright file="NoopVerb.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System.Threading.Tasks;

namespace Rnwood.SmtpServer.Verbs;

/// <summary>
///     Defines the <see cref="NoopVerb" />.
/// </summary>
public class NoopVerb : IVerb
{
    /// <inheritdoc />
    public async Task Process(IConnection connection, SmtpCommand command) =>
        await connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.OK, "Successfully did nothing"))
            .ConfigureAwait(false);
}
