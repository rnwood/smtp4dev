// <copyright file="MailFromVerb.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rnwood.SmtpServer.Verbs;

namespace Rnwood.SmtpServer;

/// <summary>
///     Defines the <see cref="MailFromVerb" />.
/// </summary>
public class MailFromVerb : IVerb
{
    /// <summary>
    ///     Defines the currentDateTimeProvider.
    /// </summary>
    private readonly ICurrentDateTimeProvider currentDateTimeProvider;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MailFromVerb" /> class.
    /// </summary>
    public MailFromVerb()
        : this(new CurrentDateTimeProvider())
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="MailFromVerb" /> class.
    /// </summary>
    /// <param name="currentDateTimeProvider">The currentDateTimeProvider<see cref="ICurrentDateTimeProvider" />.</param>
    public MailFromVerb(ICurrentDateTimeProvider currentDateTimeProvider)
    {
        ParameterProcessorMap = new ParameterProcessorMap();
        this.currentDateTimeProvider = currentDateTimeProvider;
    }

    /// <summary>
    ///     Gets the ParameterProcessorMap.
    /// </summary>
    public ParameterProcessorMap ParameterProcessorMap { get; }

    /// <inheritdoc />
    public async Task Process(IConnection connection, SmtpCommand command)
    {
        if (connection.CurrentMessage != null)
        {
            await connection.WriteResponse(new SmtpResponse(
                StandardSmtpResponseCode.BadSequenceOfCommands,
                "You already told me who the message was from")).ConfigureAwait(false);
            return;
        }

        if (command.ArgumentsText.Length == 0)
        {
            await connection.WriteResponse(
                new SmtpResponse(
                    StandardSmtpResponseCode.SyntaxErrorInCommandArguments,
                    "Must specify from address or <>")).ConfigureAwait(false);
            return;
        }

        ArgumentsParser argumentsParser = new ArgumentsParser(command.ArgumentsText);
        IReadOnlyCollection<string> arguments = argumentsParser.Arguments;

        string from = arguments.First();
        if (from.StartsWith("<", StringComparison.OrdinalIgnoreCase))
        {
            from = from.Remove(0, 1);
        }

        if (from.EndsWith(">", StringComparison.OrdinalIgnoreCase))
        {
            from = from.Remove(from.Length - 1, 1);
        }

        await connection.Server.Options.OnMessageStart(connection, from).ConfigureAwait(false);
        await connection.NewMessage().ConfigureAwait(false);
        connection.CurrentMessage.ReceivedDate = currentDateTimeProvider.GetCurrentDateTime();
        connection.CurrentMessage.From = from;

        try
        {
            await ParameterProcessorMap.Process(connection, arguments.Skip(1).ToArray(), true).ConfigureAwait(false);
            await connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.OK, "New message started"))
                .ConfigureAwait(false);
        }
        catch
        {
            await connection.AbortMessage().ConfigureAwait(false);
            throw;
        }
    }
}
