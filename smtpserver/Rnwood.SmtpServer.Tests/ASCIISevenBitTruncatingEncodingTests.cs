using Xunit;

namespace Rnwood.SmtpServer.Tests
{
    public class ASCIISevenBitTruncatingEncodingTests
    {
        [Fact]
        public void GetChars_ASCIIChar_ReturnsOriginal()
        {
            ASCIISevenBitTruncatingEncoding encoding = new ASCIISevenBitTruncatingEncoding();
            char[] chars = encoding.GetChars(new[] { (byte)'a', (byte)'b', (byte)'c' }, 0, 3);

            Assert.Equal(new[] { 'a', 'b', 'c' }, chars);
        }

        [Fact]
        public void GetBytes_ASCIIChar_ReturnsOriginal()
        {
            ASCIISevenBitTruncatingEncoding encoding = new ASCIISevenBitTruncatingEncoding();
            byte[] bytes = encoding.GetBytes(new[] { 'a', 'b', 'c' }, 0, 3);

            Assert.Equal(new[] { (byte)'a', (byte)'b', (byte)'c' }, bytes);
        }

        [Fact]
        public void GetChars_ExtendedChar_ReturnsTruncated()
        {
            ASCIISevenBitTruncatingEncoding encoding = new ASCIISevenBitTruncatingEncoding();
            char[] chars = encoding.GetChars(new[] { (byte)250 }, 0, 1);

            Assert.Equal(new[] { 'z' }, chars);
        }

        [Fact]
        public void GetBytes_ExtendedChar_ReturnsTruncated()
        {
            ASCIISevenBitTruncatingEncoding encoding = new ASCIISevenBitTruncatingEncoding();
            byte[] bytes = encoding.GetBytes(new[] { (char)250 }, 0, 1);

            Assert.Equal(new[] { (byte)'z' }, bytes);
        }
    }
}