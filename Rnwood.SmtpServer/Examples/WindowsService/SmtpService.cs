using Rnwood.SmtpServer;
using System.ServiceProcess;

namespace WindowsService
{
    public partial class SmtpService : ServiceBase
    {
        public SmtpService()
        {
            InitializeComponent();

            _behaviour = new DefaultServerBehaviour();
            _behaviour.MessageReceived += OnMessageReceived;
            _server = new Server(_behaviour);
        }

        private void OnMessageReceived(object sender, MessageEventArgs e)
        {
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