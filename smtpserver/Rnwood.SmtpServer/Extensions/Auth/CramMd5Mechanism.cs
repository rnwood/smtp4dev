﻿// <copyright file="CramMd5Mechanism.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer.Extensions.Auth
{
    /// <summary>
    /// Defines the <see cref="CramMd5Mechanism" /> implementing the CRAM-MD5 auth mechanism.
    /// </summary>
    public class CramMd5Mechanism : IAuthMechanism
    {
        /// <inheritdoc/>
        public string Identifier => "CRAM-MD5";

        /// <inheritdoc/>
        public bool IsPlainText => false;

        /// <inheritdoc/>
        public IAuthMechanismProcessor CreateAuthMechanismProcessor(IConnection connection)
        {
            return new CramMd5MechanismProcessor(connection, new RandomIntegerGenerator(), new CurrentDateTimeProvider());
        }
    }
}
