using Rnwood.Smtp4dev.Server.Settings;

namespace Rnwood.Smtp4dev
{
    public class CommandLineOptions
    {
        public ServerOptionsSource ServerOptions { get; set; }
        public RelayOptionsSource RelayOptions { get; set; }

        public DesktopOptionsSource DesktopOptions { get; set; }

        public string Urls { get; set; }

        public bool NoUserSettings { get; set; }
        public bool DebugSettings { get; set; }
        public string BaseAppDataPath { get; set; }
        public string InstallPath { get; set; }
        public string ApplicationName { get; set; }
        public bool IsDesktopApp { get; set; }
    }
}