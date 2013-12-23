﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Rnwood.SmtpServer.Verbs;

namespace Rnwood.SmtpServer.Tests.Verbs
{
    [TestClass]
    public class RsetVerbTests
    {
        [TestMethod]
        public void Process()
        {
            Mocks mocks = new Mocks();

            RsetVerb verb = new RsetVerb();
            verb.Process(mocks.Connection.Object, new SmtpCommand("RSET"));

            mocks.VerifyWriteResponse(StandardSmtpResponseCode.OK);
            mocks.Connection.Verify(c => c.AbortMessage());
        }
    }
}
