using Moq;
using System.Threading.Tasks;
using Xunit;

namespace Rnwood.SmtpServer.Tests
{
    public class ParameterProcessorMapTests
    {
        [Fact]
        public void GetProcessor_NotRegistered_Null()
        {
            ParameterProcessorMap map = new ParameterProcessorMap();
            Assert.Null(map.GetProcessor("BLAH"));
        }

        [Fact]
        public void GetProcessor_Registered_Returned()
        {
            Mock<IParameterProcessor> processor = new Mock<IParameterProcessor>();

            ParameterProcessorMap map = new ParameterProcessorMap();
            map.SetProcessor("BLAH", processor.Object);

            Assert.Same(processor.Object, map.GetProcessor("BLAH"));
        }

        [Fact]
        public void GetProcessor_RegisteredDifferentCase_Returned()
        {
            Mock<IParameterProcessor> processor = new Mock<IParameterProcessor>();

            ParameterProcessorMap map = new ParameterProcessorMap();
            map.SetProcessor("blah", processor.Object);

            Assert.Same(processor.Object, map.GetProcessor("BLAH"));
        }

        [Fact]
        public async Task Process_UnknownParameter_Throws()
        {
            SmtpServerException e = await Assert.ThrowsAsync<SmtpServerException>(async () =>
           {
               Mocks mocks = new Mocks();

               ParameterProcessorMap map = new ParameterProcessorMap();
               await map.ProcessAsync(mocks.Connection.Object, new string[] { "KEYA=VALUEA" }, true);
           });

            Assert.Equal("Parameter KEYA is not recognised", e.Message);
        }

        [Fact]
        public async Task Process_NoParameters_Accepted()
        {
            Mocks mocks = new Mocks();

            ParameterProcessorMap map = new ParameterProcessorMap();
            await map.ProcessAsync(mocks.Connection.Object, new string[] { }, true);
        }

        [Fact]
        public async Task Process_KnownParameters_Processed()
        {
            Mocks mocks = new Mocks();
            Mock<IParameterProcessor> keyAProcessor = new Mock<IParameterProcessor>();
            Mock<IParameterProcessor> keyBProcessor = new Mock<IParameterProcessor>();

            ParameterProcessorMap map = new ParameterProcessorMap();
            map.SetProcessor("keya", keyAProcessor.Object);
            map.SetProcessor("keyb", keyBProcessor.Object);

            await map.ProcessAsync(mocks.Connection.Object, new string[] { "KEYA=VALUEA", "KEYB=VALUEB" }, true);

            keyAProcessor.Verify(p => p.SetParameter(mocks.Connection.Object, "KEYA", "VALUEA"));
            keyBProcessor.Verify(p => p.SetParameter(mocks.Connection.Object, "KEYB", "VALUEB"));
        }
    }
}