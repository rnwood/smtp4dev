// <copyright file="UsernameAndPasswordAuthenticationRequest.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer.Extensions.Auth
{
    /// <summary>
    /// Defines the <see cref="UsernameAndPasswordAuthenticationRequest" />
    /// </summary>
    public class UsernameAndPasswordAuthenticationRequest : IAuthenticationCredentials
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UsernameAndPasswordAuthenticationRequest"/> class.
        /// </summary>
        /// <param name="username">The username<see cref="string"/></param>
        /// <param name="password">The password<see cref="string"/></param>
        public UsernameAndPasswordAuthenticationRequest(string username, string password)
        {
            this.Username = username;
            this.Password = password;
        }

        /// <summary>
        /// Gets the Password
        /// </summary>
        public string Password { get; private set; }

        /// <summary>
        /// Gets the Username
        /// </summary>
        public string Username { get; private set; }
    }
}
