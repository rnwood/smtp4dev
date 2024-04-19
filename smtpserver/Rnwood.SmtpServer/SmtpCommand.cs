// <copyright file="SmtpCommand.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Text.RegularExpressions;

namespace Rnwood.SmtpServer;

/// <summary>
///     Defines the <see cref="SmtpCommand" /> which implements parsing of an SMTP command received from client to server.
/// </summary>
public sealed class SmtpCommand : IEquatable<SmtpCommand>
{
    private static readonly Regex COMMANDREGEX = new("(?'verb'[^ :]+)[ :]*(?'arguments'.*)");

    /// <summary>
    ///     Initializes a new instance of the <see cref="SmtpCommand" /> class.
    /// </summary>
    /// <param name="text">The text<see cref="string" />.</param>
    public SmtpCommand(string text)
    {
        Text = text;

        IsValid = false;
        IsEmpty = true;

        if (!string.IsNullOrEmpty(text))
        {
            Match match = COMMANDREGEX.Match(text);

            if (match.Success)
            {
                Verb = match.Groups["verb"].Value;
                ArgumentsText = match.Groups["arguments"].Value ?? string.Empty;
                IsValid = true;
            }
        }
    }

    /// <summary>
    ///     Gets the arguments supplied after the VERB in the command as a single string.
    /// </summary>
    public string ArgumentsText { get; private set; }

    /// <summary>
    ///     Gets a value indicating whether IsEmpty.
    /// </summary>
    public bool IsEmpty { get; private set; }

    /// <summary>
    ///     Gets a value indicating whether this command is valid - i.e. matching the pattern allowed.
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    ///     Gets the Text.
    /// </summary>
    public string Text { get; }

    /// <summary>
    ///     Gets the Verb.
    /// </summary>
    public string Verb { get; private set; }

    /// <summary>
    ///     Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">The other<see cref="SmtpCommand" />.</param>
    /// <returns>
    ///     The <see cref="bool" />.
    /// </returns>
    public bool Equals(SmtpCommand other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Equals(other.Text, Text);
    }

    /// <summary>
    ///     Determines whether the specified <see cref="object" />, is equal to this instance.
    /// </summary>
    /// <param name="obj">The obj<see cref="object" />.</param>
    /// <returns>
    ///     The <see cref="bool" />.
    /// </returns>
    public override bool Equals(object obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (!(obj is SmtpCommand))
        {
            return false;
        }

        return Equals((SmtpCommand)obj);
    }

    /// <summary>Converts to string.</summary>
    /// <returns>A <see cref="string" /> that represents this instance.</returns>
    public override string ToString() => Text;

    /// <summary>
    ///     Returns a hash code for this instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="int" />.
    /// </returns>
    public override int GetHashCode() => Text != null ? Text.GetHashCode() : 0;
}
