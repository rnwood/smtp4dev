using Microsoft.AspNetCore.SignalR.Client;

namespace Rnwood.Smtp4dev.Client
{
    public class HubConnectionManager
    {
        private HubConnection _connection;
        public bool Connected { get; private set; } = false;

        public bool Started { get; private set; } = false;

        public Exception Error { get; private set; }

        private List<Func<Task>> connectedCallbacks = new List<Func<Task>>();


        public HubConnectionManager(string url)
        {
            this._connection = new HubConnectionBuilder().WithUrl(url).Build();

            this._connection.Closed += ConnectionClosed;
            this._connection.Reconnected += Connection_Reconnected;
        }

        private async Task ConnectionClosed(Exception e)
        {
            this.Connected = false;
            this.Started = false;
            this.Error = e;       
            await OnStatusChanged();
        }

        private async Task Connection_Reconnected(string arg)
        {
            foreach (var callback in connectedCallbacks)
            {
                await callback();
            }
        }

        private async Task AddOnConnectedCallbackAsync(Func<Task> handler)
        {
            this.connectedCallbacks.Add(handler);

            if (this.Connected)
            {
                await handler();
            }
        }


        public async Task StartAsync()
        {

            if (this.Started)
            {
                return;
            }

            this.Error = null;
            this.Started = true;
            try
            {
                await OnStatusChanged();
                await this._connection.StartAsync();
                this.Connected = true;

                foreach (var handler in connectedCallbacks)
                {
                    await handler();
                }
            }
            catch (Exception e)
            {
                this.Error = e;
            }

            await OnStatusChanged();
        }

        public async Task StopAsync()
        {
            this.Started = false;
            await OnStatusChanged();
            await this._connection.StopAsync();
        }

        public event Func<Task> StatusChanged;

        protected Task OnStatusChanged()
        {
            var result = StatusChanged?.Invoke();
            return result ?? Task.CompletedTask;
        } 

        public IDisposable On(string eventName, Func<Task> handler)
        {
            return this._connection.On(eventName, handler);
        }

    }
}
