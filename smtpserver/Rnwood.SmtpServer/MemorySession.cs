// <copyright file="MemorySession.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer
{
    using System;
    using System.IO;
    using System.Net;
    using System.Text;

    /// <summary>
    /// Defines the <see cref="MemorySession" />
    /// </summary>
    public class MemorySession : AbstractSession
    {
        /// <summary>
        /// Defines the log
        /// </summary>
        private readonly StringBuilder log = new StringBuilder();

        /// <summary>
        /// Initializes a new instance of the <see cref="MemorySession"/> class.
        /// </summary>
        /// <param name="clientAddress">The clientAddress<see cref="IPAddress"/></param>
        /// <param name="startDate">The startDate<see cref="DateTime"/></param>
        public MemorySession(IPAddress clientAddress, DateTime startDate)
            : base(clientAddress, startDate)
        {
        }

        /// <inheritdoc />
        public override void AppendToLog(string text)
        {
            this.log.AppendLine(text);
        }

        /// <inheritdoc />
        public override TextReader GetLog()
        {
            return new StringReader(this.log.ToString());
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
        }
    }
}
