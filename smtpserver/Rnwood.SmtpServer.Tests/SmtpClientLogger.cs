// <copyright file="SmtpClientLogger.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using MailKit;
using Xunit.Abstractions;

namespace Rnwood.SmtpServer.Tests;

/// <summary>
///     Defines the <see cref="ClientTests" />
/// </summary>
public partial class ClientTests
{
    /// <summary>
    ///     Defines the <see cref="SmtpClientLogger" />
    /// </summary>
    internal class SmtpClientLogger : IProtocolLogger, IAuthenticationSecretDetector
    {
        /// <summary>
        ///     Defines the testOutput
        /// </summary>
        private readonly ITestOutputHelper testOutput;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SmtpClientLogger" /> class.
        /// </summary>
        /// <param name="testOutput">The testOutput<see cref="ITestOutputHelper" /></param>
        public SmtpClientLogger(ITestOutputHelper testOutput) => this.testOutput = testOutput;

        public IAuthenticationSecretDetector AuthenticationSecretDetector { get => this; set { } }

        public IList<AuthenticationSecret> DetectSecrets(byte[] buffer, int offset, int count) => new List<AuthenticationSecret>();

        /// <summary>
        /// </summary>
        public void Dispose() => testOutput.WriteLine("*** DISCONNECT");

        /// <summary>
        /// </summary>
        /// <param name="buffer">The buffer<see cref="byte" /></param>
        /// <param name="offset">The offset<see cref="int" /></param>
        /// <param name="count">The count<see cref="int" /></param>
        public void LogClient(byte[] buffer, int offset, int count) =>
            testOutput.WriteLine(">>> " + Encoding.UTF8.GetString(buffer, offset, count).Replace("\r", "\\r")
                .Replace("\n", "\\n\n"));

        /// <summary>
        /// </summary>
        /// <param name="uri">The uri<see cref="Uri" /></param>
        public void LogConnect(Uri uri) => testOutput.WriteLine($"*** CONNECT {uri}");

        /// <summary>
        /// </summary>
        /// <param name="buffer">The buffer<see cref="byte" /></param>
        /// <param name="offset">The offset<see cref="int" /></param>
        /// <param name="count">The count<see cref="int" /></param>
        public void LogServer(byte[] buffer, int offset, int count) =>
            testOutput.WriteLine("<<< " + Encoding.UTF8.GetString(buffer, offset, count).Replace("\r", "\\r")
                .Replace("\n", "\\n\n"));
    }
}
