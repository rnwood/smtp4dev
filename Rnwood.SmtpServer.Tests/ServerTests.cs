#region

using System;
using System.Net.Sockets;
using System.Threading;
using MbUnit.Framework;

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
            return new DefaultServer(Ports.AssignAutomatically);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Run_AlreadyRunning_Throws()
        {
            Server server = StartServer();
            try
            {
                server.Run();
            }
            finally
            {
                server.Stop();
            }
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
        [ExpectedException(typeof(InvalidOperationException))]
        public void Start_AlreadyRunning_Throws()
        {
            Server server = StartServer();
            try
            {
                server.Start();
            }
            finally
            {
                server.Stop();
            }
        }

        [Test]
        [ExpectedException(typeof(SocketException))]
        public void StartOnInusePort_StartupExceptionThrown()
        {
            Server server1 = new DefaultServer(Ports.AssignAutomatically);
            server1.Start();

            Server server2 = new DefaultServer(server1.PortNumber);
            server2.Start();

            server1.Stop();
        }

        [Test]
        public void Stop_NotIsRunning()
        {
            Server server = StartServer();
            server.Stop();
            Assert.IsFalse(server.IsRunning);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Stop_NotRunning_Throws()
        {
            Server server = NewServer();
            server.Stop();
        }     

        [Test]
        public void Start_CanConnect()
        {
            Server server = StartServer();

            TcpClient client = new TcpClient();
            client.Connect("localhost", server.PortNumber);
            client.Close();

            server.Stop();
        }
    }
}