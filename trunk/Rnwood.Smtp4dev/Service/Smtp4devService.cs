using Rnwood.SmtpServer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;

namespace Rnwood.Smtp4dev.Service
{
    class Smtp4devService : ServiceBase
    {
        class FirstInstanceServer : MarshalByRefObject
        {

        }

        public static void Start()
        {
            Smtp4devService service = new Smtp4devService();

            if (Environment.UserInteractive)
            {
                service.Run(false);
            }
            else
            {
                ServiceBase.Run(service);
            }
        }

        private Server server;
        private TraceSource traceSource;

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);
            Run(true);
        }


        private void Run(bool async)
        {
            traceSource = new TraceSource("smtp4dev.Service", SourceLevels.All);

            SingleInstanceManager<FirstInstanceServer> sim = new SingleInstanceManager<FirstInstanceServer>("{42D54EA0-0BF2-4E13-8D37-6C653DE10E59}", () => new FirstInstanceServer());

            Trace(TraceEventType.Information, EventId.ServerStarted, "Server starting");

            IServerSettings serviceSettings = new ServiceSettings();
            ServerBehaviour behaviour = new ServerBehaviour(serviceSettings);
            behaviour.SessionStarted += OnSessionStarted;
            behaviour.SessionCompleted += OnSessionCompleted;
            server = new Server(behaviour);

            if (async)
            {
                server.Start();
                traceSource.TraceInformation("Server is listening at {0} port {1}", server.Behaviour.IpAddress, server.PortNumber);
            }
            else
            {
                server.Run();
            }

        }

        private void OnSessionStarted(object sender, SessionEventArgs e)
        {
            Trace(TraceEventType.Information, EventId.SessionCompleted, "Session from {0} started", e.Session.ClientAddress);       
        }

        void Trace(TraceEventType eventType, EventId eventId, string format, params object[] args)
        {
            traceSource.TraceEvent(eventType, (int)eventId, format, args);
        }

        void OnSessionCompleted(object sender, SessionEventArgs e)
        {
            Trace(TraceEventType.Information, EventId.SessionCompleted, "Session from {0} completed. {1} messages received", e.Session.ClientAddress, e.Session.GetMessages().Length);       

            if (e.Session.SessionError != null)
            {
                Trace(TraceEventType.Error, EventId.SessionError, "Session completed with error:\n{0}", e.Session.SessionError.ToString());
            }
       
        }

        protected override void OnStop()
        {
            base.OnStop();
            server.Stop();
            Trace(TraceEventType.Information, EventId.ServerStopped, "Server stopped");
        }

        public static void Install()
        {
            ManagedInstallerClass.InstallHelper(new[] { typeof(Smtp4devService).Assembly.Location });
        }

        public static void Remove()
        {
            ManagedInstallerClass.InstallHelper(new[] {"/u", typeof(Smtp4devService).Assembly.Location });
        }
    }
}
