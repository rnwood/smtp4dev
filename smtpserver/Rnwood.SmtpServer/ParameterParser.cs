﻿// <copyright file="ParameterParser.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Defines the <see cref="ParameterParser" /> which implements parsing of A=1 B=2 type string for command parameters.
    /// </summary>
    public class ParameterParser
    {
        private readonly List<Parameter> parameters = new List<Parameter>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterParser"/> class.
        /// </summary>
        /// <param name="arguments">The arguments<see cref="string"/></param>
        public ParameterParser(params string[] arguments)
        {
            this.Parse(arguments);
        }

        /// <summary>
        /// Gets the parameters which have been parsed from the arguments.
        /// </summary>
        public IReadOnlyCollection<Parameter> Parameters => this.parameters.ToArray();

        private void Parse(string[] tokens)
        {
            foreach (string token in tokens)
            {
                string[] tokenParts = token.Split(new[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);
                string key = tokenParts[0];
                string value = tokenParts.Length > 1 ? tokenParts[1] : null;
                this.parameters.Add(new Parameter(key, value));
            }
        }
    }
}
