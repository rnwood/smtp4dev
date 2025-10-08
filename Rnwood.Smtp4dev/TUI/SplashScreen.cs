using System;
using System.Threading;
using Terminal.Gui;

namespace Rnwood.Smtp4dev.TUI
{
    /// <summary>
    /// Displays a splash screen with ANSI art logo
    /// </summary>
    public class SplashScreen
    {
        private const string Logo = @"
     ███████╗███╗   ███╗████████╗██████╗ ██╗  ██╗██████╗ ███████╗██╗   ██╗
     ██╔════╝████╗ ████║╚══██╔══╝██╔══██╗██║  ██║██╔══██╗██╔════╝██║   ██║
     ███████╗██╔████╔██║   ██║   ██████╔╝███████║██║  ██║█████╗  ██║   ██║
     ╚════██║██║╚██╔╝██║   ██║   ██╔═══╝ ╚════██║██║  ██║██╔══╝  ╚██╗ ██╔╝
     ███████║██║ ╚═╝ ██║   ██║   ██║          ██║██████╔╝███████╗ ╚████╔╝ 
     ╚══════╝╚═╝     ╚═╝   ╚═╝   ╚═╝          ╚═╝╚═════╝ ╚══════╝  ╚═══╝  
                                                                            
                Fake SMTP Server for Development & Testing
                            Version 3.x
";

        public static void Show(int durationMs = 2000)
        {
            Application.Init();

            try
            {
                var win = new Window("smtp4dev")
                {
                    X = 0,
                    Y = 0,
                    Width = Dim.Fill(),
                    Height = Dim.Fill()
                };

                // Create a label with the logo centered
                var logoLabel = new Label(Logo)
                {
                    X = Pos.Center(),
                    Y = Pos.Center() - 5,
                    ColorScheme = Colors.Base
                };

                win.Add(logoLabel);

                var loadingLabel = new Label("Loading...")
                {
                    X = Pos.Center(),
                    Y = Pos.Center() + 5,
                    ColorScheme = Colors.Base
                };

                win.Add(loadingLabel);

                Application.Top.Add(win);
                Application.Refresh();

                // Show splash for specified duration
                Thread.Sleep(durationMs);
            }
            finally
            {
                Application.Shutdown();
            }
        }
    }
}
