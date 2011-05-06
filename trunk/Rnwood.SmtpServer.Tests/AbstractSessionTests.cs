using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MbUnit.Framework;

namespace Rnwood.SmtpServer.Tests
{
    public abstract class AbstractSessionTests
    {
        protected abstract IEditableSession GetSession();

        [Test]
        public void AppendToLog()
        {
            IEditableSession session = GetSession();
            session.AppendToLog("Blah1");
            session.AppendToLog("Blah2");

            Assert.AreElementsEqual(new[] { "Blah1", "Blah2", "" },
                                    session.GetLog().ReadToEnd().Split(new string[] { "\r\n" }, StringSplitOptions.None));
        }
    }
}
