// <copyright file="AuthMechanismMap.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;

namespace Rnwood.SmtpServer.Extensions.Auth;

/// <summary>
///     Defines the <see cref="AuthMechanismMap" />.
/// </summary>
public class AuthMechanismMap
{
    /// <summary>
    ///     Defines the map.
    /// </summary>
    private readonly Dictionary<string, IAuthMechanism> map = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Adds an auth mechanism to the map.
    /// </summary>
    /// <param name="mechanism">The mechanism<see cref="IAuthMechanism" />.</param>
    public void Add(IAuthMechanism mechanism) => map[mechanism.Identifier] = mechanism;

    /// <summary>
    ///     Gets the auth mechanism which has been registered for the given identifier.
    /// </summary>
    /// <param name="identifier">The identifier<see cref="string" />.</param>
    /// <returns>The <see cref="IAuthMechanism" />.</returns>
    public IAuthMechanism Get(string identifier)
    {
        map.TryGetValue(identifier, out IAuthMechanism result);

        return result;
    }

    /// <summary>
    ///     Gets all registered auth mechanisms.
    /// </summary>
    /// <returns>The <see cref="IEnumerable{T}" />.</returns>
    public IEnumerable<IAuthMechanism> GetAll() => map.Values;
}
