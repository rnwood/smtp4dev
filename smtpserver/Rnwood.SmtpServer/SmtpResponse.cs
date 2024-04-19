// <copyright file="SmtpResponse.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Globalization;

namespace Rnwood.SmtpServer;

/// <summary>
///     Represents a SMTP response from server to client which is represented by a numeric code an optional descriptive
///     text.
/// </summary>
/// <seealso cref="System.IEquatable{T}" />
public sealed class SmtpResponse : IEquatable<SmtpResponse>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SmtpResponse" /> class using any code represented as a number.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <param name="messageFormatString">
    ///     The message format string. Including placeholders where <paramref name="args" /> will
    ///     be substituted in.
    /// </param>
    /// <param name="args">The arguments used to fill in placeholders in <paramref name="messageFormatString" />.</param>
    public SmtpResponse(int code, string messageFormatString, params object[] args)
    {
        Code = code;
        Message = string.Format(CultureInfo.InvariantCulture, messageFormatString, args);
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SmtpResponse" /> class using an enum of standard responses.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <param name="messageFormatString">The message format string.</param>
    /// <param name="args">The arguments.</param>
    public SmtpResponse(StandardSmtpResponseCode code, string messageFormatString, params object[] args)
        : this((int)code, messageFormatString, args)
    {
    }

    /// <summary>
    ///     Gets the Code.
    /// </summary>
    public int Code { get; }

    /// <summary>
    ///     Gets a value indicating whether this response represents an error.
    ///     Error responses have a <see cref="Code" /> in the range 500-599.
    /// </summary>
    public bool IsError => Code >= 500 && Code <= 599;

    /// <summary>
    ///     Gets a value indicating whether this response represent success.
    ///     Successful responses have a <see cref="Code" /> in the range 200-299.
    /// </summary>
    public bool IsSuccess => Code >= 200 && Code <= 299;

    /// <summary>
    ///     Gets the Message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    ///     Indicates whether the current response is equal to another. Both message and code must be equal.
    /// </summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <returns>
    ///     true if the current object is equal to the <paramref name="other">other</paramref> parameter; otherwise, false.
    /// </returns>
    public bool Equals(SmtpResponse other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return other.Code == Code && Equals(other.Message, Message);
    }

    /// <summary>
    ///     Determines whether the specified <see cref="object" />, is equal to this instance.
    ///     Objects are equal if they are both instances of <see cref="SmtpResponse" /> and  have the same <see cref="Code" />
    ///     and <see cref="Message" />.
    /// </summary>
    /// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
    /// <returns>
    ///     <c>true</c> if the specified <see cref="object" /> is equal to this instance; otherwise, <c>false</c>.
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

        if (!(obj is SmtpResponse))
        {
            return false;
        }

        return Equals((SmtpResponse)obj);
    }

    /// <summary>
    ///     Returns a hash code for this instance.
    /// </summary>
    /// <returns>
    ///     A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
    /// </returns>
    public override int GetHashCode()
    {
        unchecked
        {
            return (Code * 397) ^ (Message != null ? Message.GetHashCode() : 0);
        }
    }

    /// <summary>
    ///     Returns a <see cref="string" /> that represents the response.
    /// </summary>
    /// <returns>A <see cref="string" /> that represents the response.</returns>
    public override string ToString()
    {
        SmtpStringBuilder result = new SmtpStringBuilder();
        string[] lines = Message.Split(new[] { "\r\n" }, StringSplitOptions.None);

        for (int l = 0; l < lines.Length; l++)
        {
            string line = lines[l];

            if (l == lines.Length - 1)
            {
                result.AppendLine(Code + " " + line);
            }
            else
            {
                result.AppendLine(Code + "-" + line);
            }
        }

        return result.ToString();
    }
}
