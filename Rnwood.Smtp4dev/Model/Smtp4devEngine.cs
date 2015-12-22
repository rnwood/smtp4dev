using Rnwood.SmtpServer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.Model
{
    public class Smtp4devEngine : ISmtp4devEngine
    {
        private Server _server;
        private ISettingsStore _settingsStore;
        private IMessageStore _messageStore;

        public Smtp4devEngine(ISettingsStore settingsStore, IMessageStore messageStore)
        {
            _settingsStore = settingsStore;
            _messageStore = messageStore;
            TryStart();
        }

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
                _server = new Server(new Smtp4devServerBehaviour(settings, OnMessageReceived));
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

        private void OnMessageReceived(ISmtp4devMessage message)
        {
            _messageStore.AddMessage(message);
        }
    }
}