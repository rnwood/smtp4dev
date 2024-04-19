// <copyright file="ASCIISevenBitTruncatingEncodingTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using Xunit;

namespace Rnwood.SmtpServer.Tests;

/// <summary>
///     Defines the <see cref="ASCIISevenBitTruncatingEncodingTests" />
/// </summary>
public class ASCIISevenBitTruncatingEncodingTests
{
    /// <summary>
    ///     The GetBytes_ASCIIChar_ReturnsOriginal
    /// </summary>
    [Fact]
    public void GetBytes_ASCIIChar_ReturnsOriginal()
    {
        AsciiSevenBitTruncatingEncoding encoding = new AsciiSevenBitTruncatingEncoding();
        byte[] bytes = encoding.GetBytes(new[] { 'a', 'b', 'c' }, 0, 3);

        Assert.Equal(new[] { (byte)'a', (byte)'b', (byte)'c' }, bytes);
    }

    /// <summary>
    ///     The GetBytes_ExtendedChar_ReturnsTruncated
    /// </summary>
    [Fact]
    public void GetBytes_ExtendedChar_ReturnsTruncated()
    {
        AsciiSevenBitTruncatingEncoding encoding = new AsciiSevenBitTruncatingEncoding();
        byte[] bytes = encoding.GetBytes(new[] { (char)250 }, 0, 1);

        Assert.Equal(new[] { (byte)'z' }, bytes);
    }

    /// <summary>
    ///     The GetChars_ASCIIChar_ReturnsOriginal
    /// </summary>
    [Fact]
    public void GetChars_ASCIIChar_ReturnsOriginal()
    {
        AsciiSevenBitTruncatingEncoding encoding = new AsciiSevenBitTruncatingEncoding();
        char[] chars = encoding.GetChars(new[] { (byte)'a', (byte)'b', (byte)'c' }, 0, 3);

        Assert.Equal(new[] { 'a', 'b', 'c' }, chars);
    }

    /// <summary>
    ///     The GetChars_ExtendedChar_ReturnsTruncated
    /// </summary>
    [Fact]
    public void GetChars_ExtendedChar_ReturnsTruncated()
    {
        AsciiSevenBitTruncatingEncoding encoding = new AsciiSevenBitTruncatingEncoding();
        char[] chars = encoding.GetChars(new[] { (byte)250 }, 0, 1);

        Assert.Equal(new[] { 'z' }, chars);
    }
}
