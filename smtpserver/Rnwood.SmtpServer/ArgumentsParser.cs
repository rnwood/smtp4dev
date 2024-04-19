// <copyright file="ArgumentsParser.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
using System.Text;

namespace Rnwood.SmtpServer;

/// <summary>
///     Parses SMTP command arguments into an array of arguments.
///     Arguments are separated by spaces or are enclosed within &lt;&gt;s which may contain spaces and balanced &lt;&gt;s.
///     Example:
///     <code>&lt;Robert Wood&lt;rob@rnwood.co.uk&gt;&gt; ARG2 ARG3</code>
///     Results in 3 arguments.
/// </summary>
public class ArgumentsParser
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ArgumentsParser" /> class.
    /// </summary>
    /// <param name="text">The text to parse<see cref="string" />.</param>
    public ArgumentsParser(string text)
    {
        Text = text;
        Arguments = ParseArguments(text);
    }

    /// <summary>
    ///     Gets the arguments parsed from the text.
    /// </summary>
    /// <value>
    ///     The arguments.
    /// </value>
    public IReadOnlyCollection<string> Arguments { get; private set; }

    /// <summary>
    ///     Gets the Text which was parsed.
    /// </summary>
    public string Text { get; private set; }

    private static string[] ParseArguments(string argumentsText)
    {
        int ltCount = 0;
        List<string> arguments = new List<string>();
        StringBuilder currentArgument = new StringBuilder();
        foreach (char character in argumentsText)
        {
            switch (character)
            {
                case '<':
                    ltCount++;
                    goto default;
                case '>':
                    ltCount--;
                    goto default;
                case ' ':
                    if (ltCount == 0)
                    {
                        arguments.Add(currentArgument.ToString());
                        currentArgument = new StringBuilder();
                    }
                    else
                    {
                        goto default;
                    }

                    break;

                default:
                    currentArgument.Append(character);
                    break;
            }
        }

        if (currentArgument.Length != 0)
        {
            arguments.Add(currentArgument.ToString());
        }

        return arguments.ToArray();
    }
}
