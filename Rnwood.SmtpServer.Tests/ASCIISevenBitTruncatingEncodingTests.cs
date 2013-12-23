using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rnwood.SmtpServer.Tests
{
    [TestClass]
    public class ASCIISevenBitTruncatingEncodingTests
    {
        [TestMethod]
        public void GetChars()
        {
            ASCIISevenBitTruncatingEncoding encoding = new ASCIISevenBitTruncatingEncoding();
            char[] chars = encoding.GetChars(new[] { (byte)'a', (byte)'b', (byte)'c' }, 0, 3);

            CollectionAssert.AreEqual(new[] { 'a', 'b', 'c' }, chars);
        }

        [TestMethod]
        public void GetBytes()
        {
            ASCIISevenBitTruncatingEncoding encoding = new ASCIISevenBitTruncatingEncoding();
            byte[] bytes = encoding.GetBytes(new[] { 'a', 'b', 'c' }, 0, 3);

            CollectionAssert.AreEqual(new[] { (byte)'a', (byte)'b', (byte)'c' }, bytes);
        }
    }
}
