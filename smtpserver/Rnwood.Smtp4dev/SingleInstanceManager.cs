#region

using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Threading;

#endregion

namespace Rnwood.Smtp4dev
{
    public class SingleInstanceManager<TInstance> where TInstance : MarshalByRefObject
    {
        private Mutex _mutex;

        public SingleInstanceManager(string applicationId, Func<TInstance> firstInstanceCreator)
        {
            ApplicationId = applicationId;

            bool isFirstInstance;
            _mutex = new Mutex(true, ApplicationId, out isFirstInstance);
            IsFirstInstance = isFirstInstance;

            if (IsFirstInstance)
            {
                firstInstance =  firstInstanceCreator();

                IpcServerChannel channel = new IpcServerChannel(ApplicationId);
                ChannelServices.RegisterChannel(channel);
                RemotingServices.Marshal(firstInstance, ApplicationId, typeof(TInstance));
            }
        }

        private TInstance firstInstance;

        public bool IsFirstInstance { get; private set; }

        public string ApplicationId { get; private set; }

        public TInstance GetFirstInstance()
        {
            if (IsFirstInstance)
            {
                return firstInstance;
            }

            IpcClientChannel channel = new IpcClientChannel(ApplicationId, null);
            ChannelServices.RegisterChannel(channel);

            TInstance server =
                (TInstance)
                Activator.GetObject(typeof(TInstance), string.Format("ipc://{0}/{0}", ApplicationId));
            return server;
        }

    }


    class FirstInstanceServer : MarshalByRefObject
    {

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public void ProcessLaunchInfo(LaunchInfo launchInfo)
        {
            OnLaunchInfoReceived(launchInfo);

        }

        protected virtual void OnLaunchInfoReceived(LaunchInfo launchInfo)
        {
            if (LaunchInfoReceived != null)
            {
                LaunchInfoReceived(this, new LaunchInfoReceivedEventArgs(launchInfo));
            }
        }

        public event EventHandler<LaunchInfoReceivedEventArgs> LaunchInfoReceived;
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