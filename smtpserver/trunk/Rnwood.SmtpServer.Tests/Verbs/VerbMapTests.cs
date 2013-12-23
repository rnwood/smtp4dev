using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Rnwood.SmtpServer.Verbs;

namespace Rnwood.SmtpServer.Tests.Verbs
{
    [TestClass]
    public class VerbMapTests
    {
        [TestMethod]
        public void GetVerbProcessor_RegisteredVerb_ReturnsVerb()
        {
            VerbMap verbMap = new VerbMap();
            Mock<IVerb> verbMock = new Mock<IVerb>();

            verbMap.SetVerbProcessor("verb", verbMock.Object);

            Assert.AreSame(verbMock.Object, verbMap.GetVerbProcessor("verb"));
        }

        [TestMethod]
        public void GetVerbProcessor_RegisteredVerbWithDifferentCase_ReturnsVerb()
        {
            VerbMap verbMap = new VerbMap();
            Mock<IVerb> verbMock = new Mock<IVerb>();

            verbMap.SetVerbProcessor("vErB", verbMock.Object);

            Assert.AreSame(verbMock.Object, verbMap.GetVerbProcessor("VERB"));
        }

        [TestMethod]
        public void GetVerbProcessor_NoRegisteredVerb_ReturnsNull()
        {
            VerbMap verbMap = new VerbMap();

            Assert.IsNull(verbMap.GetVerbProcessor("VERB"));
        }

        [TestMethod]
        public void SetVerbProcessor_RegisteredVerbAgainWithNull_ClearsRegistration()
        {
            VerbMap verbMap = new VerbMap();
            Mock<IVerb> verbMock = new Mock<IVerb>();
            verbMap.SetVerbProcessor("verb", verbMock.Object);

            verbMap.SetVerbProcessor("verb", null);

            Assert.IsNull(verbMap.GetVerbProcessor("verb"));
        }

        [TestMethod]
        public void SetVerbProcessor_RegisteredVerbAgainDifferentCaseWithNull_ClearsRegistration()
        {
            VerbMap verbMap = new VerbMap();
            Mock<IVerb> verbMock = new Mock<IVerb>();
            verbMap.SetVerbProcessor("verb", verbMock.Object);

            verbMap.SetVerbProcessor("vErb", null);

            Assert.IsNull(verbMap.GetVerbProcessor("verb"));
        }


        [TestMethod]
        public void SetVerbProcessor_RegisteredVerbAgain_UpdatesRegistration()
        {
            VerbMap verbMap = new VerbMap();
            Mock<IVerb> verbMock1 = new Mock<IVerb>();
            Mock<IVerb> verbMock2 = new Mock<IVerb>();
            verbMap.SetVerbProcessor("verb", verbMock1.Object);

            verbMap.SetVerbProcessor("veRb", verbMock2.Object);

            Assert.AreSame(verbMock2.Object, verbMap.GetVerbProcessor("verb"));
        }
    }
}
