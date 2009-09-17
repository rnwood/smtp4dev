using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rnwood.SmtpServer.Example
{
    class Program
    {
        static void Main(string[] args)
        {
            DefaultServerBehaviour serverBehaviour = new DefaultServerBehaviour();
            serverBehaviour.SessionStarted += SessionStarted;
            serverBehaviour.SessionCompleted += SessionCompleted;
            serverBehaviour.MessageReceived += MessageReceived;
            
            Server server = new Server(serverBehaviour);
            server.Start();

            Console.WriteLine("Server running. Press ENTER to stop and exit");
            Console.ReadLine();
            server.Stop();

        }

        static void SessionCompleted(object sender, SessionEventArgs e)
        {
            Console.WriteLine(string.Format("SESSION END - Address:{0} NoOfMessages:{1} Error:{2}", e.Session.ClientAddress, e.Session.Messages.Count, e.Session.SessionError));
        }

        static void SessionStarted(object sender, SessionEventArgs e)
        {
            Console.WriteLine(string.Format("SESSION START - Address:{0}", e.Session.ClientAddress));
        }

        private static void MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Console.WriteLine(string.Format("MESSAGE RECEIVED - Envelope From:{0} Envelope To:{1}", e.Message.From, string.Join(", ", e.Message.To)));
        }
    }
}
