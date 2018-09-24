using System.ServiceProcess;

using Microsoft.AspNetCore.Hosting;

namespace Rnwood.Smtp4dev.Service
{
    public static class WebHostExtensions
    {
        public static void RunAsSmtp4devService(this IWebHost host)
        {
            var webHostService = new Smtp4devWebHostService(host);
            ServiceBase.Run(webHostService);
        }
    }
}
