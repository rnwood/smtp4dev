﻿// <copyright file="CramMd5AuthenticationCredentials.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer.Extensions.Auth
{
    using System;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// Defines the <see cref="CramMd5AuthenticationCredentials" />
    /// </summary>
    public class CramMd5AuthenticationCredentials : IAuthenticationCredentials
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CramMd5AuthenticationCredentials"/> class.
        /// </summary>
        /// <param name="username">The username<see cref="string"/></param>
        /// <param name="challenge">The challenge<see cref="string"/></param>
        /// <param name="challengeResponse">The challengeResponse<see cref="string"/></param>
        public CramMd5AuthenticationCredentials(string username, string challenge, string challengeResponse)
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

        /// <summary>
        /// Validates the response sent by the client against a password specified in clear text.
        /// </summary>
        /// <param name="password">The password<see cref="string" /></param>
        /// <returns>
        /// The <see cref="bool" />
        /// </returns>
        public bool ValidateResponse(string password)
        {
#pragma warning disable CA5351
            HMACMD5 hmacmd5 = new HMACMD5(ASCIIEncoding.ASCII.GetBytes(password));
            string expectedResponse = BitConverter.ToString(hmacmd5.ComputeHash(ASCIIEncoding.ASCII.GetBytes(this.Challenge))).Replace("-", string.Empty);
#pragma warning restore CA5351

            return string.Equals(expectedResponse, this.ChallengeResponse, StringComparison.OrdinalIgnoreCase);
        }
    }
}
