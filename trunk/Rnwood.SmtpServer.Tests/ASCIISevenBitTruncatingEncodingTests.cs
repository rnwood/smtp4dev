using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MbUnit.Framework;

namespace Rnwood.SmtpServer.Tests
{
    [TestFixture]
    public class ASCIISevenBitTruncatingEncodingTests
    {
        [Test]
        public void GetChars()
        {
            ASCIISevenBitTruncatingEncoding encoding = new ASCIISevenBitTruncatingEncoding();
            char[] chars = encoding.GetChars(new[] { (byte)'a', (byte)'b', (byte)'c' }, 0, 3);

            Assert.AreElementsEqual(new[] { 'a', 'b', 'c' }, chars);
        }

        [Test]
        public void GetBytes()
        {
            ASCIISevenBitTruncatingEncoding encoding = new ASCIISevenBitTruncatingEncoding();
            byte[] bytes = encoding.GetBytes(new[] { 'a', 'b', 'c' }, 0, 3);

            Assert.AreElementsEqual(new[] { (byte)'a', (byte)'b', (byte)'c' }, bytes);
        }
    }
}
