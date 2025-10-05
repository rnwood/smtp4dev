using System;
using System.Threading;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.TUI
{
    /// <summary>
    /// Manages background auto-refresh for TUI views
    /// </summary>
    public class AutoRefreshService
    {
        private readonly CancellationTokenSource cancellationTokenSource;
        private Task refreshTask;
        private Action refreshAction;
        private int intervalSeconds;
        private bool isRunning;

        public AutoRefreshService()
        {
            cancellationTokenSource = new CancellationTokenSource();
        }

        public void Start(Action action, int intervalSeconds = 2)
        {
            if (isRunning)
                Stop();

            this.refreshAction = action;
            this.intervalSeconds = intervalSeconds;
            this.isRunning = true;

            refreshTask = Task.Run(async () =>
            {
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), cancellationTokenSource.Token);
                        refreshAction?.Invoke();
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                    catch (Exception)
                    {
                        // Silently handle errors
                    }
                }
            }, cancellationTokenSource.Token);
        }

        public void Stop()
        {
            isRunning = false;
            cancellationTokenSource.Cancel();
            refreshTask?.Wait(TimeSpan.FromSeconds(1));
        }

        public void Dispose()
        {
            Stop();
            cancellationTokenSource?.Dispose();
        }
    }
}
