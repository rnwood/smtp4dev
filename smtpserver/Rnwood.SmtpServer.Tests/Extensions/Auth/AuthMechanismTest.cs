// <copyright file="AuthMechanismTest.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer.Tests.Extensions.Auth
{
    using System;
    using System.Text;

    /// <summary>
    /// Defines the <see cref="AuthMechanismTest" />
    /// </summary>
    public class AuthMechanismTest
    {
        /// <summary>
        /// The EncodeBase64
        /// </summary>
        /// <param name="asciiString">The asciiString<see cref="string"/></param>
        /// <returns>The <see cref="string"/></returns>
        protected static string EncodeBase64(string asciiString)
        {
            return Convert.ToBase64String(Encoding.ASCII.GetBytes(asciiString));
        }

        /// <summary>
        /// The VerifyBase64Response
        /// </summary>
        /// <param name="base64">The base64<see cref="string"/></param>
        /// <param name="expectedString">The expectedString<see cref="string"/></param>
        /// <returns>The <see cref="bool"/></returns>
        protected static bool VerifyBase64Response(string base64, string expectedString)
        {
            string decodedString = Encoding.ASCII.GetString(Convert.FromBase64String(base64));
            return decodedString.Equals(expectedString);
        }
    }
}
