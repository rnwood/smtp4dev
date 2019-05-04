// <copyright file="AuthenticationCredentialsValidationEventArgs.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer
{
	using System;
	using Rnwood.SmtpServer.Extensions.Auth;

	/// <summary>
	/// Defines the <see cref="AuthenticationCredentialsValidationEventArgs" />.
	/// </summary>
	public class AuthenticationCredentialsValidationEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AuthenticationCredentialsValidationEventArgs"/> class.
		/// </summary>
		/// <param name="credentials">The credentials<see cref="IAuthenticationCredentials"/>.</param>
		public AuthenticationCredentialsValidationEventArgs(IAuthenticationCredentials credentials)
		{
			this.Credentials = credentials;
		}

		/// <summary>
		/// Gets or sets the AuthenticationResult.
		/// </summary>
		public AuthenticationResult AuthenticationResult { get; set; }

		/// <summary>
		/// Gets the Credentials.
		/// </summary>
		public IAuthenticationCredentials Credentials { get; private set; }
	}
}
