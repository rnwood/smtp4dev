using System;
using System.Text;
using System.Linq;

namespace Rnwood.SmtpServer
{
    public class ASCIISevenBitTruncatingEncoding : Encoding
    {
        public ASCIISevenBitTruncatingEncoding()
        {
            _asciiEncoding = Encoding.GetEncoding("ASCII", new EncodingFallback(),
                                                  new DecodingFallback());
        }

        private Encoding _asciiEncoding;

        public override int GetByteCount(char[] chars, int index, int count)
        {
            return _asciiEncoding.GetByteCount(chars, index, count);
        }

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            return _asciiEncoding.GetBytes(chars, charIndex, charCount, bytes, byteIndex);
        }

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            return _asciiEncoding.GetCharCount(bytes, index, count);
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            return _asciiEncoding.GetChars(bytes, byteIndex, byteCount, chars, charIndex);
        }

        public override int GetMaxByteCount(int charCount)
        {
            return _asciiEncoding.GetMaxByteCount(charCount);
        }

        public override int GetMaxCharCount(int byteCount)
        {
            return _asciiEncoding.GetMaxCharCount(byteCount);
        }

        class EncodingFallback : EncoderFallback
        {
            public override int MaxCharCount
            {
                get { return 1; }
            }

            public override EncoderFallbackBuffer CreateFallbackBuffer()
            {
                return new Buffer();
            }

            class Buffer : EncoderFallbackBuffer
            {
                public override bool Fallback(char charUnknown, int index)
                {
                    _char = FallbackChar(charUnknown);
                    _charRead = false;
                    return true;
                }

                public override bool Fallback(char charUnknownHigh, char charUnknownLow, int index)
                {
                    _char = FallbackChar(charUnknownLow);
                    _charRead = false;
                    return true;
                }

                private char FallbackChar(char charUnknown)
                {
                   return (char)(charUnknown & 127);
                }

                public override char GetNextChar()
                {
                    if (!_charRead)
                    {
                        _charRead = true;
                        return _char;
                    }

                    return '\0';
                }

                public override bool MovePrevious()
                {
                    if (_charRead)
                    {
                        _charRead = false;
                        return true;
                    }

                    return false;
                }

                private char _char;
                private bool _charRead;

                public override int Remaining
                {
                    get { return !_charRead ? 1 : 0; }
                }
            }

        }

        class DecodingFallback : DecoderFallback
        {
            public override int MaxCharCount
            {
                get { return 1; }
            }

            public override DecoderFallbackBuffer CreateFallbackBuffer()
            {
                return new Buffer();
            }

            #region Nested type: Buffer

            private class Buffer : DecoderFallbackBuffer
            {
                private int _fallbackIndex;
                private string _fallbackString;

                public override int Remaining
                {
                    get { return _fallbackString.Length - _fallbackIndex; }
                }

                public override bool Fallback(byte[] bytesUnknown, int index)
                {
                    _fallbackString = Encoding.ASCII.GetString(bytesUnknown.Select(b => (byte)( b & 127)).ToArray());
                    _fallbackIndex = 0;

                    return true;
                }

                public override char GetNextChar()
                {
                    if (Remaining > 0)
                    {
                        return _fallbackString[_fallbackIndex++];
                    }
                    else
                    {
                        return '\0';
                    }
                }

                public override bool MovePrevious()
                {
                    if (_fallbackIndex > 0)
                    {
                        _fallbackIndex--;
                        return true;
                    }

                    return false;
                }
            }

            #endregion
        }
    }
}