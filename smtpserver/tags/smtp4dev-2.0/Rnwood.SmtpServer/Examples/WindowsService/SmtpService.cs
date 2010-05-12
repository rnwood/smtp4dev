using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using Rnwood.SmtpServer;

namespace WindowsService
{
    public partial class SmtpService : ServiceBase
    {
        public SmtpService()
        {
            InitializeComponent();

            _behaviour = new DefaultServerBehaviour();
            _server = new Server(_behaviour);
        }

        private DefaultServerBehaviour _behaviour;
        private Server _server;

        protected override void OnStart(string[] args)
        {
            _server.Start();
        }

        protected override void OnStop()
        {
            _server.Stop();
        }
    }
}
