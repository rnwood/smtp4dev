#region

using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Threading;

#endregion

namespace Rnwood.Smtp4dev
{
    public class SingleInstanceManager : MarshalByRefObject, IFirstInstanceServer
    {
        private Mutex _mutex;

        public SingleInstanceManager(string applicationId)
        {
            ApplicationId = applicationId;

            bool firstInstance;
            _mutex = new Mutex(true, ApplicationId, out firstInstance);
            IsFirstInstance = firstInstance;
        }

        public bool IsFirstInstance { get; private set; }

        public string ApplicationId { get; private set; }

        #region IFirstInstanceServer Members

        void IFirstInstanceServer.ProcessLaunchInfo(LaunchInfo launchInfo)
        {
            OnLaunchInfoReceived(launchInfo);
        }

        #endregion

        public void SendLaunchInfoToFirstInstance(LaunchInfo launchInfo)
        {
            if (IsFirstInstance)
            {
                throw new InvalidOperationException();
            }

            IpcClientChannel channel = new IpcClientChannel(ApplicationId, null);
            ChannelServices.RegisterChannel(channel);

            IFirstInstanceServer server =
                (IFirstInstanceServer)
                Activator.GetObject(typeof (IFirstInstanceServer), string.Format("ipc://{0}/{0}", ApplicationId));
            server.ProcessLaunchInfo(launchInfo);
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public event EventHandler<LaunchInfoReceivedEventArgs> LaunchInfoReceived;

        public void ListenForLaunches()
        {
            if (!IsFirstInstance)
            {
                throw new InvalidOperationException();
            }

            IpcServerChannel channel = new IpcServerChannel(ApplicationId);
            ChannelServices.RegisterChannel(channel);

            RemotingServices.Marshal(this, ApplicationId, typeof (IFirstInstanceServer));
        }

        protected virtual void OnLaunchInfoReceived(LaunchInfo launchInfo)
        {
            if (LaunchInfoReceived != null)
            {
                LaunchInfoReceived(this, new LaunchInfoReceivedEventArgs(launchInfo));
            }
        }
    }

    public interface IFirstInstanceServer
    {
        void ProcessLaunchInfo(LaunchInfo launchInfo);
    }

    public class LaunchInfoReceivedEventArgs : EventArgs
    {
        public LaunchInfoReceivedEventArgs(LaunchInfo launchInfo)
        {
            LaunchInfo = launchInfo;
        }

        public LaunchInfo LaunchInfo { get; private set; }
    }

    [Serializable]
    public class LaunchInfo
    {
        public LaunchInfo(string workingDirectory, string[] arguments)
        {
            WorkingDirectory = workingDirectory;
            Arguments = arguments;
        }

        public string WorkingDirectory { get; private set; }
        public string[] Arguments { get; private set; }
    }
}