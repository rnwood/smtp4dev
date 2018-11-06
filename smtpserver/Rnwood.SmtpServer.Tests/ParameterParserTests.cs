// <copyright file="ParameterParserTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer.Tests
{
    using System.Linq;
    using Xunit;

    /// <summary>
    /// Defines the <see cref="ParameterParserTests" />
    /// </summary>
    public class ParameterParserTests
    {
        /// <summary>
        ///
        /// </summary>
        [Fact]
        public void MultipleParameters()
        {
            ParameterParser parameterParser = new ParameterParser("KEYA=VALUEA", "KEYB=VALUEB");

            Assert.Equal(2, parameterParser.Parameters.Count);
            Assert.Equal(new Parameter("KEYA", "VALUEA"), parameterParser.Parameters.First());
            Assert.Equal(new Parameter("KEYB", "VALUEB"), parameterParser.Parameters.ElementAt(1));
        }

        /// <summary>
        ///
        /// </summary>
        [Fact]
        public void NoParameters()
        {
            ParameterParser parameterParser = new ParameterParser(new string[0]);

            Assert.Empty(parameterParser.Parameters);
        }

        /// <summary>
        ///
        /// </summary>
        [Fact]
        public void SingleParameter()
        {
            ParameterParser parameterParser = new ParameterParser("KEYA=VALUEA");

            Assert.Single(parameterParser.Parameters);
            Assert.Equal(new Parameter("KEYA", "VALUEA"), parameterParser.Parameters.First());
        }
    }
}
