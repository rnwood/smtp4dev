using LumiSoft.Net;
using LumiSoft.Net.IMAP.Server;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace New.LumiSoft.Net.Tests
{
    public class ImapServerTests
    {
        [Fact]
        public void WhenStarted_AutomaticPortNumberAvailable()
        {

            IMAP_Server imapServer = new IMAP_Server()
            {
                Bindings = new[] { new IPBindInfo(Dns.GetHostName(), BindInfoProtocol.TCP, IPAddress.Loopback, 0) },
                GreetingText = "smtp4dev"
            };

            var errorTcs = new TaskCompletionSource<Error_EventArgs>();
            imapServer.Error += (s, ea) => errorTcs.SetResult(ea);
            var startedTcs = new TaskCompletionSource<EventArgs>();
            imapServer.Started += (s, ea) => startedTcs.SetResult(ea);

            imapServer.Start();

            var errorTask = errorTcs.Task;
            var startedTask = startedTcs.Task;


            int index = Task.WaitAny(startedTask, errorTask);
            Assert.Equal(0, index);

            Assert.NotEqual(0, ((IPEndPoint)imapServer.ListeningPoints.Single().Socket.LocalEndPoint).Port);

        }
    }
}
