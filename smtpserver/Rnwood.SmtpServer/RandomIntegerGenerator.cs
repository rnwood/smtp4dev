// <copyright file="RandomIntegerGenerator.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer
{
    using System;

    /// <summary>
    /// Defines the <see cref="RandomIntegerGenerator" />
    /// </summary>
    public class RandomIntegerGenerator : IRandomIntegerGenerator
    {
        private static Random random = new Random();

        /// <inheritdoc/>
        public int GenerateRandomInteger(int minValue, int maxValue)
        {
            return random.Next(minValue, maxValue);
        }
    }
}
