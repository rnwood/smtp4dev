using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MbUnit.Framework;
using Moq;
using Rnwood.SmtpServer.Verbs;

namespace Rnwood.SmtpServer.Tests.Verbs
{
    [TestFixture]
    public class HeloVerbTests
    {
        [Test]
        public void Process()
        {
            Mocks mocks = new Mocks();

            HeloVerb verb = new HeloVerb();
            verb.Process(mocks.Connection.Object, new SmtpCommand("HELO foo.blah"));

            mocks.VerifyWriteResponse(StandardSmtpResponseCode.OK);
        }

        [Test]
        public void Process_NoArguments_Accepted()
        {
            Mocks mocks = new Mocks();

            HeloVerb verb = new HeloVerb();
            verb.Process(mocks.Connection.Object, new SmtpCommand("HELO"));

            mocks.VerifyWriteResponse(StandardSmtpResponseCode.OK);
        }


        [Test]
        public void Process_RecordsClientName()
        {
            Mocks mocks = new Mocks();

            HeloVerb verb = new HeloVerb();
            verb.Process(mocks.Connection.Object, new SmtpCommand("HELO foo.blah"));

            mocks.Session.VerifySet(s => s.ClientName = "foo.blah");
        }

        [Test]
        public void Process_SaidHeloAlready_Allowed()
        {
            Mocks mocks = new Mocks();
            
            HeloVerb verb = new HeloVerb();
            verb.Process(mocks.Connection.Object, new SmtpCommand("HELO foo.blah"));
            verb.Process(mocks.Connection.Object, new SmtpCommand("HELO foo.blah"));

            mocks.VerifyWriteResponse(StandardSmtpResponseCode.OK);
        }
    }
}
