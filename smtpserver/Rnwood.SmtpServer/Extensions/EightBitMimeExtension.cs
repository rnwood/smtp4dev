// <copyright file="EightBitMimeExtension.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer.Extensions
{
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="EightBitMimeExtension" />
    /// </summary>
    public class EightBitMimeExtension : IExtension
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EightBitMimeExtension"/> class.
        /// </summary>
        public EightBitMimeExtension()
        {
        }

        /// <inheritdoc/>
        public IExtensionProcessor CreateExtensionProcessor(IConnection connection)
        {
            return new EightBitMimeExtensionProcessor(connection);
        }

        /// <summary>
        /// Defines the <see cref="EightBitMimeExtensionProcessor" />
        /// </summary>
        private class EightBitMimeExtensionProcessor : ExtensionProcessor
        {
           /// <summary>
           /// Initializes a new instance of the <see cref="EightBitMimeExtensionProcessor"/> class.
           /// </summary>
           /// <param name="connection">The connection<see cref="IConnection"/></param>
            public EightBitMimeExtensionProcessor(IConnection connection)
                : base(connection)
            {
                EightBitMimeDataVerb verb = new EightBitMimeDataVerb();
                connection.VerbMap.SetVerbProcessor("DATA", verb);

                MailVerb mailVerbProcessor = connection.MailVerb;
                MailFromVerb mailFromProcessor = mailVerbProcessor.FromSubVerb;
                mailFromProcessor.ParameterProcessorMap.SetProcessor("BODY", verb);
            }

            /// <inheritdoc/>
            public override Task<string[]> GetEHLOKeywords()
            {
                return Task.FromResult(new[] { "8BITMIME" });
            }
        }
    }
}
