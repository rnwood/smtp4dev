using System;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.Server
{
    public interface ISmtp4devServer
    {
        Exception Exception { get; }
        bool IsRunning { get; }
        int PortNumber { get; }
        void TryStart();
        void Stop();
        Task DeleteSession(Guid id);
        Task DeleteAllSessions();
    }
}