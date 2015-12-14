using System.ServiceProcess;

namespace WindowsService
{
    /// <summary>
    /// Shows how to host Rnwood.SmtpServer as a Windows service.
    /// To install this service use installutil
    /// (http://msdn.microsoft.com/en-us/library/sd8zc8ha(VS.80).aspx)
    /// then start the service (the service name is "smtpserver").
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new SmtpService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}