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
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="MemorySession" />
    /// </summary>
    public class MemorySession : AbstractSession
    {
        private readonly SmtpStreamWriter log;

        private readonly MemoryStream logStream = new MemoryStream();

        /// <summary>
        /// Initializes a new instance of the <see cref="MemorySession"/> class.
        /// </summary>
        /// <param name="clientAddress">The clientAddress<see cref="IPAddress"/></param>
        /// <param name="startDate">The startDate<see cref="DateTime"/></param>
        public MemorySession(IPAddress clientAddress, DateTime startDate)
            : base(clientAddress, startDate)
        {
            this.log = new SmtpStreamWriter(this.logStream, false);
        }

        /// <inheritdoc />
        public override Task AppendLineToSessionLog(string text)
        {
            this.log.WriteLine(text);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override Task<TextReader> GetLog()
        {
            this.log.Flush();
            return Task.FromResult<TextReader>(new SmtpStreamReader(new MemoryStream(this.logStream.ToArray(), false), new UTF8Encoding(false, true), false));
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
        }
    }
}
