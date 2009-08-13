using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rnwood.SmtpServer
{
    public class ASCIITruncatingDecoderFallback : DecoderFallback
    {
        public ASCIITruncatingDecoderFallback()
        {
        }

        public override DecoderFallbackBuffer CreateFallbackBuffer()
        {
            return new Buffer();
        }

        public override int MaxCharCount
        {
            get { return 1; }
        }

        class Buffer : DecoderFallbackBuffer
        {
            public override bool Fallback(byte[] bytesUnknown, int index)
            {
                byte unknownChar = bytesUnknown[0];
                _fallbackString = Encoding.ASCII.GetString(new []{(byte)(unknownChar & (2^8)-1)});
                _fallbackIndex = 0;

                return true;
            }

            private string _fallbackString;
            private int _fallbackIndex;

            public override char GetNextChar()
            {
                if (Remaining > 0)
                {
                    return _fallbackString[_fallbackIndex++];
                } else
                {
                    return '\0';
                }
            }

            public override bool MovePrevious()
            {
                if (_fallbackIndex >0)
                {
                    _fallbackIndex--;
                    return true;
                }

                return false;
            }

            public override int Remaining
            {
                get { return _fallbackString.Length - _fallbackIndex; }
            }
        }
    }
}
