using System;
using System.Threading.Tasks;
using Esprima.Ast;
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
";

        /// <summary>
        /// Shows a splash screen as a modal dialog within an existing Application context
        /// </summary>
        /// <param name="durationMs">Duration to show the splash in milliseconds</param>
        public static void Show(int durationMs = 2000)
        {
            var dialog = new Dialog("smtp4dev", 80, 15)
            {
                Border = new Border()
                {
                    BorderStyle = BorderStyle.Double
                }
            };

            // Create a label with the logo centered
            var logoLabel = new Label(Logo)
            {
                X = Pos.Center(),
                Y = 0,
                TextAlignment = TextAlignment.Centered
            };

            dialog.Add(logoLabel);

            var loadingLabel = new Label("Loading...")
            {
                X = Pos.Center(),
                Y = Pos.Bottom(dialog) - 3,
                TextAlignment = TextAlignment.Centered
            };

            dialog.Add(loadingLabel);

            // Use a timeout to auto-close the splash
            var timeout = Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(durationMs), (_) =>
            {
                Application.Top.Remove(dialog);
                return false; // Don't repeat
            });

            Application.Top.Add(dialog);
        }
    }
}
