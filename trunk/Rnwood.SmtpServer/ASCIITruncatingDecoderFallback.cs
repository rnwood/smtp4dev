#region

using System.Text;

#endregion

namespace Rnwood.SmtpServer
{
    public class ASCIITruncatingDecoderFallback : DecoderFallback
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
                byte unknownChar = bytesUnknown[0];
                _fallbackString = Encoding.ASCII.GetString(new[] {(byte) (unknownChar & (2 ^ 8) - 1)});
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