// <copyright file="RcptVerb.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System.Threading.Tasks;
using Rnwood.SmtpServer.Verbs;

namespace Rnwood.SmtpServer;

/// <summary>
///     Defines the <see cref="RcptVerb" />.
/// </summary>
public class RcptVerb : IVerb
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="RcptVerb" /> class.
    /// </summary>
    public RcptVerb()
    {
        SubVerbMap = new VerbMap();
        SubVerbMap.SetVerbProcessor("TO", new RcptToVerb());
    }

    /// <summary>
    ///     Gets the <see cref="VerbMap" /> for subcommands.
    /// </summary>
    public VerbMap SubVerbMap { get; }

    /// <inheritdoc />
    public async Task Process(IConnection connection, SmtpCommand command)
    {
        SmtpCommand subrequest = new SmtpCommand(command.ArgumentsText);
        IVerb verbProcessor = SubVerbMap.GetVerbProcessor(subrequest.Verb);

        if (verbProcessor != null)
        {
            await verbProcessor.Process(connection, subrequest).ConfigureAwait(false);
        }
        else
        {
            await connection.WriteResponse(
                new SmtpResponse(
                    StandardSmtpResponseCode.CommandParameterNotImplemented,
                    "Subcommand {0} not implemented",
                    subrequest.Verb)).ConfigureAwait(false);
        }
    }
}
