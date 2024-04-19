// <copyright file="RandomIntegerGenerator.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;

namespace Rnwood.SmtpServer;

/// <summary>
///     Defines the <see cref="RandomIntegerGenerator" />.
/// </summary>
public class RandomIntegerGenerator : IRandomIntegerGenerator
{
    private static readonly Random random = new();

    /// <inheritdoc />
    public int GenerateRandomInteger(int minValue, int maxValue) => random.Next(minValue, maxValue);
}
