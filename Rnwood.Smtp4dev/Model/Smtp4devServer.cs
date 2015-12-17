using Rnwood.SmtpServer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.Model
{
    public class Smtp4devServer : ISmtp4devServer
    {
        private DefaultServer _server;
        private ObservableCollection<IMessage> _messages = new ObservableCollection<IMessage>();
        private ISettingsStore _settingsStore;

        public Smtp4devServer(ISettingsStore settingsStore)
        {
            _settingsStore = settingsStore;

            _messages.CollectionChanged += (s, ea) =>
            {
                MessagesChanged?.Invoke(this, new EventArgs());
            };

            TryStart();
        }

        public IEnumerable<IMessage> Messages
        {
            get
            {
                return _messages.ToList();
            }
        }

        public event EventHandler<EventArgs> MessagesChanged;

        public Exception ServerError { get; set; }

        public bool IsRunning
        {
            get
            {
                return _server.IsRunning;
            }
        }

        public void ApplySettings(Settings settings)
        {
            if (_server.IsRunning)
            {
                Stop();
            }

            TryStart();
        }

        public void TryStart()
        {
            ServerError = null;
            Settings settings = _settingsStore.Load();

            try
            {
                _server = new DefaultServer(settings.Port);
                _server.MessageReceived += MessageReceived;
                _server.Start();
            }
            catch (Exception e)
            {
                ServerError = e;
            }
        }

        private void Stop()
        {
            _server.Stop(true);
        }

        private void MessageReceived(object sender, MessageEventArgs e)
        {
            _messages.Add(e.Message);
        }
    }
}