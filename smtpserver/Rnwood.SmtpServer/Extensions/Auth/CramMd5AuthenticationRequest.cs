// <copyright file="CramMd5AuthenticationRequest.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer.Extensions.Auth
{
    /// <summary>
    /// Defines the <see cref="CramMd5AuthenticationRequest" />
    /// </summary>
    public class CramMd5AuthenticationRequest : IAuthenticationCredentials
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CramMd5AuthenticationRequest"/> class.
        /// </summary>
        /// <param name="username">The username<see cref="string"/></param>
        /// <param name="challenge">The challenge<see cref="string"/></param>
        /// <param name="challengeResponse">The challengeResponse<see cref="string"/></param>
        public CramMd5AuthenticationRequest(string username, string challenge, string challengeResponse)
        {
            this.Username = username;
            this.ChallengeResponse = challengeResponse;
            this.Challenge = challenge;
        }

        /// <summary>
        /// Gets the Challenge
        /// </summary>
        public string Challenge { get; private set; }

        /// <summary>
        /// Gets the ChallengeResponse
        /// </summary>
        public string ChallengeResponse { get; private set; }

        /// <summary>
        /// Gets the Username
        /// </summary>
        public string Username { get; private set; }
    }
}
