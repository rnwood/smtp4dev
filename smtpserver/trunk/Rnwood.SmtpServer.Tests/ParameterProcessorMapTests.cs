using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MbUnit.Framework;
using Moq;

namespace Rnwood.SmtpServer.Tests
{
    [TestFixture]
    public class ParameterProcessorMapTests
    {
        [Test]
        public void GetProcessor_NotRegistered_Null()
        {
            ParameterProcessorMap map = new ParameterProcessorMap();
            Assert.IsNull(map.GetProcessor("BLAH"));
        }

        [Test]
        public void GetProcessor_Registered_Returned()
        {
            Mock<IParameterProcessor> processor = new Mock<IParameterProcessor>();

            ParameterProcessorMap map = new ParameterProcessorMap();
            map.SetProcessor("BLAH", processor.Object);

            Assert.AreSame(processor.Object, map.GetProcessor("BLAH"));
        }


        [Test]
        public void GetProcessor_RegisteredDifferentCase_Returned()
        {
            Mock<IParameterProcessor> processor = new Mock<IParameterProcessor>();

            ParameterProcessorMap map = new ParameterProcessorMap();
            map.SetProcessor("blah", processor.Object);

            Assert.AreSame(processor.Object, map.GetProcessor("BLAH"));
        }

        [Test]
        [ExpectedException(typeof(SmtpServerException), "Parameter KEYA is not recognised")]
        public void Process_UnknownParameter_Throws()
        {
            Mocks mocks = new Mocks();

            ParameterProcessorMap map = new ParameterProcessorMap();
            map.Process(mocks.Connection.Object, new string[] {"KEYA=VALUEA"}, true);
        }

        [Test]
        public void Process_NoParameters_Accepted()
        {
            Mocks mocks = new Mocks();

            ParameterProcessorMap map = new ParameterProcessorMap();
            map.Process(mocks.Connection.Object, new string[] { }, true);
        }


        [Test]
        public void Process_KnownParameters_Processed()
        {
            Mocks mocks = new Mocks();
            Mock<IParameterProcessor> keyAProcessor = new Mock<IParameterProcessor>();
            Mock<IParameterProcessor> keyBProcessor = new Mock<IParameterProcessor>();

            ParameterProcessorMap map = new ParameterProcessorMap();
            map.SetProcessor("keya", keyAProcessor.Object);
            map.SetProcessor("keyb", keyBProcessor.Object);

            map.Process(mocks.Connection.Object, new string[] { "KEYA=VALUEA", "KEYB=VALUEB" }, true);

            keyAProcessor.Verify(p => p.SetParameter(mocks.Connection.Object, "KEYA", "VALUEA"));
            keyBProcessor.Verify(p => p.SetParameter(mocks.Connection.Object, "KEYB", "VALUEB"));
        }
    }
}
