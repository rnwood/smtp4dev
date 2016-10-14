using System;

namespace Rnwood.SmtpServer
{
    public interface IServer : IDisposable
    {
        IServerBehaviour Behaviour { get; }
    }
}