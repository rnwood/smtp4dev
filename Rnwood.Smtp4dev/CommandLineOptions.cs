using Rnwood.Smtp4dev.Server;

namespace Rnwood.Smtp4dev
{
    public class CommandLineOptions
    {
        public ServerOptions ServerOptions { get; set; }
        public RelayOptions RelayOptions { get; set; }

        public string Urls { get; set; }

        public bool NoUserSettings { get; set; }
        public bool DebugSettings { get; set; }
        public string BaseAppDataPath { get; set; }
        public string InstallPath { get; set; }
        public string ApplicationName { get; set; }
    }
}