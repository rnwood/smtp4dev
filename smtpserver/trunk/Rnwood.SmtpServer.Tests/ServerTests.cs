#region

using System.Net.Sockets;
using System.Threading;
using NUnit.Framework;

#endregion

namespace Rnwood.SmtpServer.Tests
{
    [TestFixture]
    public class ServerTests
    {
        private Server StartServer()
        {
            Server server = NewServer();
            server.Start();
            return server;
        }

        private Server NewServer()
        {
            return new Server(new DefaultServerBehaviour(2525));
        }

        [Test]
        public void Run_Blocks()
        {
            Server server = NewServer();
            bool shouldBeRunning = true;
            Thread runThread = new Thread(() =>
                                              {
                                                  server.Run();

                                                  //If we get here before Stop) is called then fail
                                                  Assert.IsFalse(shouldBeRunning);
                                              });
            runThread.Start();

            while (!server.IsRunning)
            {
                //spin!
            }
            Assert.IsTrue(runThread.IsAlive);
            shouldBeRunning = false;
            server.Stop();
            runThread.Join();
        }

        [Test]
        public void Start_IsRunning()
        {
            Server server = StartServer();
            Assert.IsTrue(server.IsRunning);
            server.Stop();
        }

        [Test]
        [ExpectedException(typeof (SocketException),
            ExpectedMessage =
                "Only one usage of each socket address (protocol/network address/port) is normally permitted")]
        public void Start_StartupExceptionThrown()
        {
            Server server1 = StartServer();
            Server server2 = StartServer();

            server1.Stop();
        }

        [Test]
        public void Stop_NotRunning()
        {
            Server server = StartServer();
            server.Stop();
            Assert.IsFalse(server.IsRunning);
        }
    }
}