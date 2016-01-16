using Rnwood.Smtp4dev.API;
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

        public event EventHandler StateChanged;

        public Smtp4devEngine(ISettingsStore settingsStore, IMessageStore messageStore)
        {
            _settingsStore = settingsStore;
            _settingsStore.Saved += OnSettingsChanged;
            _messageStore = messageStore;

            TryStart();
        }

        private void OnSettingsChanged(object sender, EventArgs e)
        {
            ApplySettings();
        }

        public Exception ServerError { get; set; }

        public bool IsRunning
        {
            get
            {
                return _server != null && _server.IsRunning;
            }
        }

        private void ApplySettings()
        {
            if (_server != null)
            {
                if (_server.IsRunning)
                {
                    Stop();
                }

                if (_server != null)
                {
                    _server.IsRunningChanged -= OnServerStateChanged;
                }

                _server = null;
            }

            TryStart();
        }

        public void TryStart()
        {
            ServerError = null;
            Settings settings = _settingsStore.Load();

            if (settings.IsEnabled)
            {
                try
                {
                    _server = new Server(new Smtp4devServerBehaviour(settings, OnMessageReceived));
                    _server.IsRunningChanged += OnServerStateChanged;
                    _server.Start();
                }
                catch (Exception e)
                {
                    ServerError = e;
                }
            }
        }

        private void Stop()
        {
            if (_server != null)
            {
                _server.Stop(true);
            }
        }

        private void OnServerStateChanged(object sender, EventArgs eventArgs)
        {
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnMessageReceived(ISmtp4devMessage message)
        {
            _messageStore.AddMessage(message);
        }
    }
}