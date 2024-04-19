// <copyright file="ASCIISevenBitTruncatingEncoding.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System.Linq;
using System.Text;

namespace Rnwood.SmtpServer;

/// <summary>
///     An ASCII encoding where the highest order bit is zeroed.
/// </summary>
/// <seealso cref="System.Text.Encoding" />
public class AsciiSevenBitTruncatingEncoding : Encoding
{
    private readonly Encoding asciiEncoding;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsciiSevenBitTruncatingEncoding" /> class.
    /// </summary>
    public AsciiSevenBitTruncatingEncoding() =>
        asciiEncoding = GetEncoding("ASCII", new EncodingFallback(), new DecodingFallback());

    /// <summary>
    ///     When overridden in a derived class, calculates the number of bytes produced by encoding a set of characters from
    ///     the specified character array.
    /// </summary>
    /// <param name="chars">The character array containing the set of characters to encode.</param>
    /// <param name="index">The index of the first character to encode.</param>
    /// <param name="count">The number of characters to encode.</param>
    /// <returns>
    ///     The number of bytes produced by encoding the specified characters.
    /// </returns>
    public override int GetByteCount(char[] chars, int index, int count) =>
        asciiEncoding.GetByteCount(chars, index, count);

    /// <summary>
    ///     When overridden in a derived class, encodes a set of characters from the specified character array into the
    ///     specified byte array.
    /// </summary>
    /// <param name="chars">The character array containing the set of characters to encode.</param>
    /// <param name="charIndex">The index of the first character to encode.</param>
    /// <param name="charCount">The number of characters to encode.</param>
    /// <param name="bytes">The byte array to contain the resulting sequence of bytes.</param>
    /// <param name="byteIndex">The index at which to start writing the resulting sequence of bytes.</param>
    /// <returns>
    ///     The actual number of bytes written into <paramref name="bytes">bytes</paramref>.
    /// </returns>
    public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex) =>
        asciiEncoding.GetBytes(chars, charIndex, charCount, bytes, byteIndex);

    /// <summary>
    ///     When overridden in a derived class, calculates the number of characters produced by decoding a sequence of bytes
    ///     from the specified byte array.
    /// </summary>
    /// <param name="bytes">The byte array containing the sequence of bytes to decode.</param>
    /// <param name="index">The index of the first byte to decode.</param>
    /// <param name="count">The number of bytes to decode.</param>
    /// <returns>
    ///     The number of characters produced by decoding the specified sequence of bytes.
    /// </returns>
    public override int GetCharCount(byte[] bytes, int index, int count) =>
        asciiEncoding.GetCharCount(bytes, index, count);

    /// <summary>
    ///     When overridden in a derived class, decodes a sequence of bytes from the specified byte array into the specified
    ///     character array.
    /// </summary>
    /// <param name="bytes">The byte array containing the sequence of bytes to decode.</param>
    /// <param name="byteIndex">The index of the first byte to decode.</param>
    /// <param name="byteCount">The number of bytes to decode.</param>
    /// <param name="chars">The character array to contain the resulting set of characters.</param>
    /// <param name="charIndex">The index at which to start writing the resulting set of characters.</param>
    /// <returns>
    ///     The actual number of characters written into <paramref name="chars">chars</paramref>.
    /// </returns>
    public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) =>
        asciiEncoding.GetChars(bytes, byteIndex, byteCount, chars, charIndex);

    /// <summary>
    ///     When overridden in a derived class, calculates the maximum number of bytes produced by encoding the specified
    ///     number of characters.
    /// </summary>
    /// <param name="charCount">The number of characters to encode.</param>
    /// <returns>
    ///     The maximum number of bytes produced by encoding the specified number of characters.
    /// </returns>
    public override int GetMaxByteCount(int charCount) => asciiEncoding.GetMaxByteCount(charCount);

    /// <summary>
    ///     When overridden in a derived class, calculates the maximum number of characters produced by decoding the specified
    ///     number of bytes.
    /// </summary>
    /// <param name="byteCount">The number of bytes to decode.</param>
    /// <returns>
    ///     The maximum number of characters produced by decoding the specified number of bytes.
    /// </returns>
    public override int GetMaxCharCount(int byteCount) => asciiEncoding.GetMaxCharCount(byteCount);

    private sealed class DecodingFallback : DecoderFallback
    {
        /// <summary>
        ///     Gets the maximum number of characters the current <see cref="System.Text.DecoderFallback"></see> object can return.
        /// </summary>
        public override int MaxCharCount => 1;

        /// <summary>
        ///     Initializes a new instance of the <see cref="System.Text.DecoderFallbackBuffer"></see> class.
        /// </summary>
        /// <returns>
        ///     An object that provides a fallback buffer for a decoder.
        /// </returns>
        public override DecoderFallbackBuffer CreateFallbackBuffer() => new Buffer();

        private sealed class Buffer : DecoderFallbackBuffer
        {
            private int fallbackIndex;

            private string fallbackString;

            public override int Remaining => fallbackString.Length - fallbackIndex;

            public override bool Fallback(byte[] bytesUnknown, int index)
            {
                fallbackString = ASCII.GetString(bytesUnknown.Select(b => (byte)(b & 127)).ToArray());
                fallbackIndex = 0;

                return true;
            }

            public override char GetNextChar()
            {
                if (Remaining > 0)
                {
                    return fallbackString[fallbackIndex++];
                }

                return '\0';
            }

            public override bool MovePrevious()
            {
                if (fallbackIndex > 0)
                {
                    fallbackIndex--;
                    return true;
                }

                return false;
            }
        }
    }

    private sealed class EncodingFallback : EncoderFallback
    {
        public override int MaxCharCount => 1;

        public override EncoderFallbackBuffer CreateFallbackBuffer() => new Buffer();

        private sealed class Buffer : EncoderFallbackBuffer
        {
            private char @char;

            private bool charRead;

            public override int Remaining => !charRead ? 1 : 0;

            public override bool Fallback(char charUnknown, int index)
            {
                @char = FallbackChar(charUnknown);
                charRead = false;
                return true;
            }

            public override bool Fallback(char charUnknownHigh, char charUnknownLow, int index)
            {
                @char = FallbackChar(charUnknownLow);
                charRead = false;
                return true;
            }

            public override char GetNextChar()
            {
                if (!charRead)
                {
                    charRead = true;
                    return @char;
                }

                return '\0';
            }

            public override bool MovePrevious()
            {
                if (charRead)
                {
                    charRead = false;
                    return true;
                }

                return false;
            }

            private static char FallbackChar(char charUnknown) => (char)(charUnknown & 127);
        }
    }
}
