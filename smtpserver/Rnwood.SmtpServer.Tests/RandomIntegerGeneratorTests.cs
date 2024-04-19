// <copyright file="RandomIntegerGeneratorTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using Xunit;

namespace Rnwood.SmtpServer.Tests;

/// <summary>
///     Defines the <see cref="RandomIntegerGeneratorTests" />
/// </summary>
public class RandomIntegerGeneratorTests
{
    /// <summary>
    /// </summary>
    [Fact]
    public void GenerateRandomInteger()
    {
        RandomIntegerGenerator randomNumberGenerator = new RandomIntegerGenerator();
        int randomNumber = randomNumberGenerator.GenerateRandomInteger(-100, 100);
        Assert.True(randomNumber >= -100);
        Assert.True(randomNumber <= 100);
    }
}
