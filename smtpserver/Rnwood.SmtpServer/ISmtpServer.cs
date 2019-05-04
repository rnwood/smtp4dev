// <copyright file="ISmtpServer.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer
{
	using System;

	/// <summary>
	/// Defines the <see cref="ISmtpServer" />.
	/// </summary>
	public interface ISmtpServer : IDisposable
	{
		/// <summary>
		/// Gets the Behaviour.
		/// </summary>
		IServerBehaviour Behaviour { get; }
	}
}
