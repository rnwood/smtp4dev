﻿// <copyright file="AnonymousMechanismProcessor.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer.Extensions.Auth
{
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="AnonymousMechanismProcessor" />
    /// </summary>
    public class AnonymousMechanismProcessor : IAuthMechanismProcessor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AnonymousMechanismProcessor"/> class.
        /// </summary>
        /// <param name="connection">The connection<see cref="IConnection"/></param>
        public AnonymousMechanismProcessor(IConnection connection)
        {
            this.Connection = connection;
        }

        /// <inheritdoc/>
        public IAuthenticationCredentials Credentials { get; private set; }

        /// <summary>
        /// Gets the connection this processor is for.
        /// </summary>
        /// <value>
        /// The connection.
        /// </value>
        protected IConnection Connection { get; private set; }

        /// <inheritdoc/>
        public async Task<AuthMechanismProcessorStatus> ProcessResponse(string data)
        {
            this.Credentials = new AnonymousAuthenticationCredentials();

            AuthenticationResult result =
                await this.Connection.Server.Behaviour.ValidateAuthenticationCredentials(this.Connection, this.Credentials).ConfigureAwait(false);

            switch (result)
            {
                case AuthenticationResult.Success:
                    return AuthMechanismProcessorStatus.Success;

                default:
                    return AuthMechanismProcessorStatus.Failed;
            }
        }
    }
}
