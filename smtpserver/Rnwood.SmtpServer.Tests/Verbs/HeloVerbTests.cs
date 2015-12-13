using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Rnwood.SmtpServer.Tests.Verbs
{
    [TestClass]
    public class HeloVerbTests
    {
        [TestMethod]
        public void SayHelo()
        {
            Mocks mocks = new Mocks();

            HeloVerb verb = new HeloVerb();
            verb.Process(mocks.Connection.Object, new SmtpCommand("HELO foo.blah"));

            mocks.VerifyWriteResponse(StandardSmtpResponseCode.OK);
            mocks.Session.VerifySet(s => s.ClientName, "foo.bar");
        }

        [TestMethod]
        public void SayHeloTwice_ReturnsError()
        {
            Mocks mocks = new Mocks();
            mocks.Session.SetupGet(s => s.ClientName).Returns("already.said.helo");
            mocks.Session.SetupSet(s => s.ClientName).Never();

            HeloVerb verb = new HeloVerb();
            verb.Process(mocks.Connection.Object, new SmtpCommand("HELO foo.blah"));

            mocks.VerifyWriteResponse(StandardSmtpResponseCode.BadSequenceOfCommands);
        }

        [TestMethod]
        public void SayHelo_NoName()
        {
            Mocks mocks = new Mocks();

            HeloVerb verb = new HeloVerb();
            verb.Process(mocks.Connection.Object, new SmtpCommand("HELO"));

            mocks.VerifyWriteResponse(StandardSmtpResponseCode.OK);
            mocks.Session.VerifySet(s => s.ClientName, "");
        }
    }
}
