// <copyright file="AsyncEventHandler.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer
{
	using System;
	using System.Threading.Tasks;

	/// <summary>
	/// Represents an async event handler which accepts an <see cref="object"/> parameter and a <typeparamref name="T"/> parameter and returns a <see cref="Task"/>.
	/// </summary>
	/// <typeparam name="T">The type of the second param.</typeparam>
	/// <param name="sender">The sender.</param>
	/// <param name="e">The e.</param>
	/// <returns>A task representing the async operation.</returns>
	public delegate Task AsyncEventHandler<T>(object sender, T e)
		where T : EventArgs;
}
