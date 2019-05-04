// <copyright file="AuthMechanismMap.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer.Extensions.Auth
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Defines the <see cref="AuthMechanismMap" />.
	/// </summary>
	public class AuthMechanismMap
	{
		/// <summary>
		/// Defines the map.
		/// </summary>
		private readonly Dictionary<string, IAuthMechanism> map = new Dictionary<string, IAuthMechanism>(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// Adds an auth mechanism to the map.
		/// </summary>
		/// <param name="mechanism">The mechanism<see cref="IAuthMechanism"/>.</param>
		public void Add(IAuthMechanism mechanism)
		{
			this.map[mechanism.Identifier] = mechanism;
		}

		/// <summary>
		/// Gets the auth mechanism which has been registered for the given identifier.
		/// </summary>
		/// <param name="identifier">The identifier<see cref="string"/>.</param>
		/// <returns>The <see cref="IAuthMechanism"/>.</returns>
		public IAuthMechanism Get(string identifier)
		{
			this.map.TryGetValue(identifier, out IAuthMechanism result);

			return result;
		}

		/// <summary>
		/// Gets all registered auth mechanisms.
		/// </summary>
		/// <returns>The <see cref="IEnumerable{T}"/>.</returns>
		public IEnumerable<IAuthMechanism> GetAll()
		{
			return this.map.Values;
		}
	}
}
